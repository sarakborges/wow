# WoW MPQ Inspector

A read-only catalog generator for MPQ-based World of Warcraft installations. It is the next stage after `Wow.SourceInspector`: it opens archive tables and reads internal `(listfile)` metadata to identify file paths, file sizes, compression flags and duplicate paths across archives.

It does **not** extract game files, modify archives, or copy source payloads.

## Requirements

- Rust 1.92 or newer (the current stable toolchain is suitable).
- The source client inventory has already identified the installation as MPQ-based.

Install Rust from [rustup.rs](https://rustup.rs/), then verify:

```powershell
rustc --version
cargo --version
```

## Build and run

From the repository root:

```powershell
cargo run --release --manifest-path tools/Wow.MpqInspector/Cargo.toml -- inspect --path "D:\Games\WoW-HD" --locale enus --json "C:\wow-manifests\wotlk-hd-catalog.json"
```

Replace both paths with the actual local paths. `--path` must point to the client root containing `Wow.exe` and `Data`; `--json` should point outside the game installation and preferably outside this repository.

By default, the tool includes root/global archives and only the selected locale folder (default: `enus`). This avoids flooding the first comparison with duplicated locale data. To inspect every locale folder:

```powershell
cargo run --release --manifest-path tools/Wow.MpqInspector/Cargo.toml -- inspect --path "D:\Games\WoW-HD" --all-locales --json "C:\wow-manifests\wotlk-hd-all-locales.json"
```

Entries without usable filenames in an archive's internal `(listfile)` are counted as anonymous table entries and excluded from path comparisons by default. Use `--include-anonymous` to include them under archive-specific synthetic paths; these synthetic paths are not real game filenames.

## Catalog contents

The JSON catalog contains:

- Archive paths, sizes, whether an internal `(listfile)` exists, and per-archive inspection errors.
- Named files and every archive that provides each path.
- Uncompressed/compressed sizes, MPQ flags, and compressed/encrypted/patch-file indicators.
- Duplicate paths, grouped case-insensitively with slash normalization.
- File-extension and archive summary counts.
- Broad archive categories: global base, locale base, global patch, locale patch, or other.

## Important interpretation limits

- A filename is only discoverable when it is present in the archive's internal `(listfile)`. Archives without a usable listfile can still be counted, but their filenames cannot be reconstructed by this pass.
- Archive categories are descriptive only. The tool does **not** claim an effective client load order or select a winning archive for duplicate paths. This HD client contains many non-standard patch names, so winner selection must wait until the actual loader order is verified.
- The catalog is metadata, not an asset export. No M2, BLP, WMO, ADT, DBC or other payload is extracted.
- Catalogs can reveal local installation paths. Keep them private and do not commit them.

## Dependency

MPQ parsing is provided by [warcraft-rs / wow-mpq](https://github.com/wowemulation-dev/warcraft-rs), a separately maintained library released under MIT or Apache-2.0. This project references the published crate and does not vendor or copy its implementation.
