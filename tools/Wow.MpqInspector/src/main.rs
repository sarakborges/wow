use std::{
    collections::BTreeMap,
    fs,
    io::{BufWriter, Write},
    path::{Component, Path, PathBuf},
    time::{SystemTime, UNIX_EPOCH},
};

use clap::{Parser, Subcommand};
use serde::Serialize;
use wow_mpq::{Archive, FileEntry};

#[derive(Debug, Parser)]
#[command(
    name = "wow-mpq-inspector",
    version,
    about = "Generate a read-only catalog of files exposed by WoW MPQ archives"
)]
struct Cli {
    #[command(subcommand)]
    command: Command,
}

#[derive(Debug, Subcommand)]
enum Command {
    /// Inspect MPQ archives beneath a local WoW installation root.
    Inspect {
        /// Installation root containing Wow.exe and Data.
        #[arg(long)]
        path: PathBuf,

        /// Locale folder to include alongside global archives (default: enus).
        #[arg(long, default_value = "enus", conflicts_with = "all_locales")]
        locale: String,

        /// Include all detected locale folders instead of filtering to --locale.
        #[arg(long)]
        all_locales: bool,

        /// Include anonymous table entries under synthetic archive-specific paths.
        #[arg(long)]
        include_anonymous: bool,

        /// Maximum directory depth below the installation root.
        #[arg(long, default_value_t = 4)]
        max_depth: usize,

        /// Destination JSON file. Keep this outside the source client directory.
        #[arg(long)]
        json: PathBuf,
    },
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct Catalog {
    schema_version: u32,
    generated_at_unix_seconds: u64,
    source_root: String,
    locale_filter: Option<String>,
    includes_all_locales: bool,
    includes_anonymous_entries: bool,
    archives: Vec<ArchiveRecord>,
    files: Vec<FileRecord>,
    summary: CatalogSummary,
    notes: Vec<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct ArchiveRecord {
    path: String,
    size_bytes: u64,
    layer_category: String,
    status: String,
    has_listfile: bool,
    enumerated_entries: usize,
    named_entries: usize,
    anonymous_entries: usize,
    anonymous_entries_included: usize,
    error: Option<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct FileRecord {
    path: String,
    normalized_path: String,
    extension: Option<String>,
    providers: Vec<FileProvider>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct FileProvider {
    archive_path: String,
    uncompressed_size_bytes: u64,
    compressed_size_bytes: u64,
    flags: u32,
    is_compressed: bool,
    is_encrypted: bool,
    is_mpq_patch_file: bool,
    name_source: String,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct CatalogSummary {
    total_archives: usize,
    archives_opened: usize,
    archives_failed: usize,
    archives_with_listfile: usize,
    archives_without_listfile: usize,
    enumerated_entries: usize,
    named_entries: usize,
    anonymous_entries: usize,
    anonymous_entries_included: usize,
    anonymous_entries_skipped: usize,
    unique_paths: usize,
    duplicate_paths: usize,
    provider_records: usize,
    files_by_extension: BTreeMap<String, usize>,
}

#[derive(Debug, Default)]
struct FileRecordBuilder {
    path: String,
    extension: Option<String>,
    providers: Vec<FileProvider>,
}

fn main() {
    if let Err(error) = run() {
        eprintln!("error: {error:#}");
        std::process::exit(1);
    }
}

fn run() -> anyhow_result::Result<()> {
    let cli = Cli::parse();

    match cli.command {
        Command::Inspect {
            path,
            locale,
            all_locales,
            include_anonymous,
            max_depth,
            json,
        } => inspect(
            &path,
            if all_locales { None } else { Some(&locale) },
            all_locales,
            include_anonymous,
            max_depth,
            &json,
        ),
    }
}

mod anyhow_result {
    pub type Result<T> = std::result::Result<T, Box<dyn std::error::Error>>;
}

fn inspect(
    source_root: &Path,
    locale_filter: Option<&str>,
    includes_all_locales: bool,
    include_anonymous: bool,
    max_depth: usize,
    output_path: &Path,
) -> anyhow_result::Result<()> {
    if let Some(locale) = locale_filter
        && (locale.len() != 4 || !locale.bytes().all(|value| value.is_ascii_alphabetic()))
    {
        return Err("Locale must be a four-letter code such as enus.".into());
    }

    let source_root = source_root.canonicalize()?;
    if !source_root.is_dir() {
        return Err(format!("Source root is not a directory: {}", source_root.display()).into());
    }

    let output_path = absolute_output_path(output_path)?;
    let archive_paths = discover_archives(&source_root, locale_filter, max_depth)?;
    if archive_paths.is_empty() {
        return Err(format!(
            "No MPQ archives were found beneath {}. Check that --path points to the client root.",
            source_root.display()
        )
        .into());
    }

    let source_root_text = source_root.to_string_lossy().into_owned();
    let mut archives = Vec::with_capacity(archive_paths.len());
    let mut file_groups: BTreeMap<String, FileRecordBuilder> = BTreeMap::new();

    let mut enumerated_entries = 0usize;
    let mut named_entries = 0usize;
    let mut anonymous_entries = 0usize;
    let mut anonymous_entries_included = 0usize;
    let mut anonymous_entries_skipped = 0usize;
    let mut files_by_extension: BTreeMap<String, usize> = BTreeMap::new();
    let mut archives_opened = 0usize;
    let mut archives_failed = 0usize;
    let mut archives_with_listfile = 0usize;
    let mut archives_without_listfile = 0usize;

    for archive_path in archive_paths {
        let relative_path = archive_path
            .strip_prefix(&source_root)?
            .to_string_lossy()
            .replace('\\', "/");
        let size_bytes = fs::metadata(&archive_path)
            .map(|metadata| metadata.len())
            .unwrap_or_default();
        let layer_category = classify_layer(Path::new(&relative_path), locale_filter).to_owned();

        let mut record = ArchiveRecord {
            path: relative_path.clone(),
            size_bytes,
            layer_category,
            status: "failed".to_owned(),
            has_listfile: false,
            enumerated_entries: 0,
            named_entries: 0,
            anonymous_entries: 0,
            anonymous_entries_included: 0,
            error: None,
        };

        let mut archive = match Archive::open(&archive_path) {
            Ok(archive) => {
                archives_opened += 1;
                archive
            }
            Err(error) => {
                archives_failed += 1;
                record.error = Some(error.to_string());
                archives.push(record);
                continue;
            }
        };

        let listfile_check = archive.find_file("(listfile)");
        record.has_listfile = matches!(listfile_check, Ok(Some(_)));
        if record.has_listfile {
            archives_with_listfile += 1;
        } else {
            archives_without_listfile += 1;
        }

        if let Err(error) = listfile_check {
            record.error = Some(format!("Could not inspect internal (listfile): {error}"));
        }

        let entries = match archive.list() {
            Ok(entries) => entries,
            Err(error) => {
                archives_failed += 1;
                record.status = "opened-but-listing-failed".to_owned();
                append_error(&mut record.error, error.to_string());
                archives.push(record);
                continue;
            }
        };

        record.status = "opened".to_owned();
        record.enumerated_entries = entries.len();
        enumerated_entries += entries.len();

        for entry in entries {
            let is_anonymous = !record.has_listfile || is_synthetic_name(&entry.name);
            let name_source = if is_anonymous {
                anonymous_entries += 1;
                record.anonymous_entries += 1;
                "anonymous-table-index"
            } else {
                named_entries += 1;
                record.named_entries += 1;
                "internal-listfile"
            };

            if is_anonymous && !include_anonymous {
                anonymous_entries_skipped += 1;
                continue;
            }

            let display_path = if is_anonymous {
                anonymous_entries_included += 1;
                record.anonymous_entries_included += 1;
                format!("@anonymous/{relative_path}/{}", entry.name)
            } else {
                let extension = file_extension(&entry.name);
                if let Some(extension) = &extension {
                    *files_by_extension.entry(extension.clone()).or_default() += 1;
                }
                entry.name.clone()
            };

            add_entry(
                &mut file_groups,
                &display_path,
                &relative_path,
                &entry,
                name_source,
            );
        }

        archives.push(record);
    }

    let provider_records = file_groups
        .values()
        .map(|group| group.providers.len())
        .sum::<usize>();
    let duplicate_paths = file_groups
        .values()
        .filter(|group| group.providers.len() > 1)
        .count();

    let files = file_groups
        .into_iter()
        .map(|(normalized_path, group)| FileRecord {
            path: group.path,
            normalized_path,
            extension: group.extension,
            providers: group.providers,
        })
        .collect::<Vec<_>>();

    let summary = CatalogSummary {
        total_archives: archives.len(),
        archives_opened,
        archives_failed,
        archives_with_listfile,
        archives_without_listfile,
        enumerated_entries,
        named_entries,
        anonymous_entries,
        anonymous_entries_included,
        anonymous_entries_skipped,
        unique_paths: files.len(),
        duplicate_paths,
        provider_records,
        files_by_extension,
    };

    let catalog = Catalog {
        schema_version: 1,
        generated_at_unix_seconds: SystemTime::now()
            .duration_since(UNIX_EPOCH)?
            .as_secs(),
        source_root: source_root_text,
        locale_filter: locale_filter.map(str::to_ascii_lowercase),
        includes_all_locales,
        includes_anonymous_entries: include_anonymous,
        archives,
        files,
        summary,
        notes: vec![
            "This catalog reads MPQ tables and internal (listfile) metadata; it does not extract game-file payloads or modify source archives.".to_owned(),
            "Only filenames exposed by an archive's usable internal (listfile) can be compared as real paths. Unnamed table entries are counted as anonymous.".to_owned(),
            "Archive layer categories are descriptive. No effective client load order or winning archive is inferred.".to_owned(),
            "Duplicate paths are grouped case-insensitively after normalizing slash direction.".to_owned(),
        ],
    };

    if let Some(parent) = output_path.parent()
        && !parent.as_os_str().is_empty()
    {
        fs::create_dir_all(parent)?;
    }

    let output_file = fs::File::create(&output_path)?;
    let mut writer = BufWriter::new(output_file);
    serde_json::to_writer_pretty(&mut writer, &catalog)?;
    writer.write_all(b"\n")?;
    writer.flush()?;

    println!("WoW MPQ Inspector");
    println!("=================");
    println!("Root:              {}", source_root.display());
    println!("Locale filter:     {}", locale_filter.unwrap_or("all locales"));
    println!("Archives:          {}", catalog.summary.total_archives);
    println!("Opened:            {}", catalog.summary.archives_opened);
    println!("Failed:            {}", catalog.summary.archives_failed);
    println!("Named file entries:{}", catalog.summary.named_entries);
    println!("Anonymous entries: {}", catalog.summary.anonymous_entries);
    println!("Unique paths:      {}", catalog.summary.unique_paths);
    println!("Duplicate paths:   {}", catalog.summary.duplicate_paths);
    println!("JSON:              {}", output_path.display());

    Ok(())
}

fn discover_archives(
    root: &Path,
    locale_filter: Option<&str>,
    max_depth: usize,
) -> std::io::Result<Vec<PathBuf>> {
    let mut pending = vec![(root.to_path_buf(), 0usize)];
    let mut archives = Vec::new();

    while let Some((directory, depth)) = pending.pop() {
        let entries = match fs::read_dir(&directory) {
            Ok(entries) => entries,
            Err(error) if directory == root => return Err(error),
            Err(_) => continue,
        };

        for entry in entries {
            let entry = match entry {
                Ok(entry) => entry,
                Err(_) => continue,
            };
            let file_type = match entry.file_type() {
                Ok(file_type) => file_type,
                Err(_) => continue,
            };

            if file_type.is_symlink() {
                continue;
            }

            let path = entry.path();
            if file_type.is_file() {
                if path
                    .extension()
                    .is_some_and(|extension| extension.eq_ignore_ascii_case("mpq"))
                {
                    archives.push(path);
                }
                continue;
            }

            if !file_type.is_dir() || depth >= max_depth {
                continue;
            }

            let relative_directory = path.strip_prefix(root).unwrap_or(&path);
            if is_casc_payload_directory(relative_directory)
                || should_skip_locale_directory(relative_directory, locale_filter)
            {
                continue;
            }

            pending.push((path, depth + 1));
        }
    }

    archives.sort_by(|left, right| {
        left.to_string_lossy()
            .to_ascii_lowercase()
            .cmp(&right.to_string_lossy().to_ascii_lowercase())
    });
    Ok(archives)
}

fn should_skip_locale_directory(relative_directory: &Path, locale_filter: Option<&str>) -> bool {
    let Some(locale_filter) = locale_filter else {
        return false;
    };
    let components = path_components(relative_directory);
    components.len() >= 2
        && components[0].eq_ignore_ascii_case("data")
        && looks_like_locale(&components[1])
        && !components[1].eq_ignore_ascii_case(locale_filter)
}

fn is_casc_payload_directory(relative_directory: &Path) -> bool {
    let components = path_components(relative_directory);
    components.len() >= 2
        && components[0].eq_ignore_ascii_case("data")
        && components[1].eq_ignore_ascii_case("data")
}

fn path_components(path: &Path) -> Vec<String> {
    path.components()
        .filter_map(|component| match component {
            Component::Normal(value) => Some(value.to_string_lossy().into_owned()),
            _ => None,
        })
        .collect()
}

fn looks_like_locale(value: &str) -> bool {
    value.len() == 4 && value.bytes().all(|byte| byte.is_ascii_alphabetic())
}

fn classify_layer(relative_path: &Path, locale_filter: Option<&str>) -> &'static str {
    let components = path_components(relative_path);
    let Some(file_name) = components.last() else {
        return "other";
    };
    let file_name = file_name.to_ascii_lowercase();

    let is_locale_archive = components.len() >= 3
        && components[0].eq_ignore_ascii_case("data")
        && looks_like_locale(&components[1])
        && locale_filter.is_none_or(|locale| components[1].eq_ignore_ascii_case(locale));

    if is_locale_archive {
        if file_name.starts_with("patch") {
            "locale-patch"
        } else {
            "locale-base"
        }
    } else if file_name.starts_with("patch") {
        "global-patch"
    } else if matches!(
        file_name.as_str(),
        "common.mpq" | "common-2.mpq" | "expansion.mpq" | "lichking.mpq"
    ) {
        "global-base"
    } else {
        "other"
    }
}

fn is_synthetic_name(name: &str) -> bool {
    let Some(stem) = Path::new(name).file_stem().and_then(|stem| stem.to_str()) else {
        return false;
    };
    let Some(index) = stem.strip_prefix("file_") else {
        return false;
    };
    index.len() == 8 && index.bytes().all(|byte| byte.is_ascii_digit())
}

fn file_extension(path: &str) -> Option<String> {
    Path::new(path)
        .extension()
        .and_then(|extension| extension.to_str())
        .filter(|extension| !extension.is_empty())
        .map(str::to_ascii_lowercase)
}

fn normalize_path(path: &str) -> String {
    path.replace('\\', "/")
        .trim_start_matches('/')
        .to_ascii_lowercase()
}

fn add_entry(
    groups: &mut BTreeMap<String, FileRecordBuilder>,
    path: &str,
    archive_path: &str,
    entry: &FileEntry,
    name_source: &str,
) {
    let normalized_path = normalize_path(path);
    let extension = if name_source == "internal-listfile" {
        file_extension(path)
    } else {
        None
    };

    let group = groups
        .entry(normalized_path)
        .or_insert_with(|| FileRecordBuilder {
            path: path.to_owned(),
            extension,
            providers: Vec::new(),
        });

    group.providers.push(FileProvider {
        archive_path: archive_path.to_owned(),
        uncompressed_size_bytes: entry.size,
        compressed_size_bytes: entry.compressed_size,
        flags: entry.flags,
        is_compressed: entry.is_compressed(),
        is_encrypted: entry.is_encrypted(),
        is_mpq_patch_file: entry.is_patch_file(),
        name_source: name_source.to_owned(),
    });
}

fn append_error(target: &mut Option<String>, message: String) {
    match target {
        Some(existing) => {
            existing.push_str("; ");
            existing.push_str(&message);
        }
        None => *target = Some(message),
    }
}

fn absolute_output_path(path: &Path) -> std::io::Result<PathBuf> {
    if path.is_absolute() {
        Ok(path.to_path_buf())
    } else {
        std::env::current_dir().map(|directory| directory.join(path))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn normalizes_case_and_slash_direction() {
        assert_eq!(
            normalize_path("DBFilesClient\\Spell.dbc"),
            "dbfilesclient/spell.dbc"
        );
    }

    #[test]
    fn classifies_global_base_archives() {
        assert_eq!(
            classify_layer(Path::new("Data/common-2.mpq"), Some("enus")),
            "global-base"
        );
        assert_eq!(
            classify_layer(Path::new("Data/lichking.mpq"), Some("enus")),
            "global-base"
        );
    }

    #[test]
    fn classifies_global_patches() {
        assert_eq!(
            classify_layer(Path::new("Data/patch-6.mpq"), Some("enus")),
            "global-patch"
        );
    }

    #[test]
    fn classifies_selected_locale_patches() {
        assert_eq!(
            classify_layer(
                Path::new("Data/enUS/patch-enUS-Z.MPQ"),
                Some("enus")
            ),
            "locale-patch"
        );
    }

    #[test]
    fn ignores_unselected_locale_for_layer_classification() {
        assert_eq!(
            classify_layer(Path::new("Data/dede/patch-dede.mpq"), Some("enus")),
            "global-patch"
        );
    }

    #[test]
    fn detects_synthetic_table_names() {
        assert!(is_synthetic_name("file_00001234.dat"));
        assert!(!is_synthetic_name("file_123.dat"));
        assert!(!is_synthetic_name("Creature.m2"));
    }

    #[test]
    fn identifies_locale_directories() {
        assert!(looks_like_locale("enus"));
        assert!(looks_like_locale("deDE"));
        assert!(!looks_like_locale("patch"));
        assert!(!looks_like_locale("data"));
    }
}
