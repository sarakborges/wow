using System.Diagnostics;

namespace Wow.Core.Content;

/// <summary>
/// Performs a bounded, read-only inspection of a local WoW installation.
/// CASC payload directories are deliberately not traversed because they may
/// contain hundreds of thousands of opaque data files.
/// </summary>
public sealed class SourceInstallationInspector
{
    private const int MaximumArchiveSearchDepth = 4;

    private static readonly string[] ExecutableCandidates =
    [
        "Wow.exe",
        "Wow-64.exe",
        "WowClassic.exe",
        "WowT.exe"
    ];

    public SourceInspectionManifest Inspect(
        string rootPath,
        SourceRole role = SourceRole.Unspecified)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var fullRootPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullRootPath))
        {
            throw new DirectoryNotFoundException(
                $"The source directory does not exist: {fullRootPath}");
        }

        var buildInfoPath = FindFile(fullRootPath, ".build.info");
        var dataDirectory = FindDirectory(fullRootPath, "Data");
        var cascDataDirectory = dataDirectory is null
            ? null
            : FindDirectory(dataDirectory, "data");
        var cascIndicesDirectory = dataDirectory is null
            ? null
            : FindDirectory(dataDirectory, "indices");

        var hasBuildInfo = buildInfoPath is not null;
        var hasCascDataDirectory = cascDataDirectory is not null;
        var hasCascIndicesDirectory = cascIndicesDirectory is not null;

        var archives = FindMpqArchives(
            fullRootPath,
            dataDirectory,
            cascDataDirectory is not null);

        var containerFormat = hasBuildInfo || hasCascDataDirectory || hasCascIndicesDirectory
            ? SourceContainerFormat.Casc
            : archives.Count > 0
                ? SourceContainerFormat.Mpq
                : SourceContainerFormat.Unknown;

        var confidence = containerFormat switch
        {
            SourceContainerFormat.Casc when hasBuildInfo => "high",
            SourceContainerFormat.Casc => "medium",
            SourceContainerFormat.Mpq => "medium",
            _ => "low"
        };

        var executablePath = FindExecutable(fullRootPath);
        var executableName = executablePath is null
            ? null
            : Path.GetFileName(executablePath);
        var executableVersion = ReadExecutableVersion(executablePath);

        var markers = new List<string>();
        if (hasBuildInfo)
        {
            markers.Add(".build.info");
        }

        if (dataDirectory is not null)
        {
            markers.Add("Data/");
        }

        if (hasCascDataDirectory)
        {
            markers.Add("Data/data/");
        }

        if (hasCascIndicesDirectory)
        {
            markers.Add("Data/indices/");
        }

        if (executableName is not null)
        {
            markers.Add(executableName);
        }

        markers.AddRange(archives.Select(archive => archive.RelativePath));

        var notes = BuildNotes(containerFormat, hasBuildInfo, hasCascDataDirectory, archives.Count);

        return new SourceInspectionManifest(
            SchemaVersion: 1,
            RootPath: fullRootPath,
            Role: role,
            ContainerFormat: containerFormat,
            DetectionConfidence: confidence,
            HasBuildInfo: hasBuildInfo,
            HasCascDataDirectory: hasCascDataDirectory,
            HasCascIndicesDirectory: hasCascIndicesDirectory,
            ExecutableName: executableName,
            ExecutableVersion: executableVersion,
            MpqArchives: archives,
            Markers: markers,
            Notes: notes);
    }

    private static IReadOnlyList<SourceFileEntry> FindMpqArchives(
        string rootPath,
        string? dataDirectory,
        bool hasCascDataDirectory)
    {
        var results = new List<SourceFileEntry>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<(string Path, int Depth)>();

        pending.Push((rootPath, 0));

        if (dataDirectory is not null &&
            !string.Equals(rootPath, dataDirectory, StringComparison.OrdinalIgnoreCase))
        {
            pending.Push((dataDirectory, 0));
        }

        while (pending.Count > 0)
        {
            var (currentPath, depth) = pending.Pop();
            if (!visited.Add(currentPath))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentPath, "*", SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var filePath in files)
            {
                if (!string.Equals(Path.GetExtension(filePath), ".mpq", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var relativePath = Path.GetRelativePath(rootPath, filePath)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    results.Add(new SourceFileEntry(relativePath, new FileInfo(filePath).Length));
                }
                catch (IOException)
                {
                    // The file may have been removed while the inventory was running.
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip files that cannot be stat'ed.
                }
            }

            if (depth >= MaximumArchiveSearchDepth)
            {
                continue;
            }

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(
                    currentPath,
                    "*",
                    SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                var name = Path.GetFileName(directory);

                if (hasCascDataDirectory &&
                    string.Equals(name, "data", StringComparison.OrdinalIgnoreCase) &&
                    dataDirectory is not null &&
                    string.Equals(
                        Path.GetFullPath(currentPath),
                        Path.GetFullPath(dataDirectory),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                pending.Push((directory, depth + 1));
            }
        }

        return results
            .OrderBy(archive => archive.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? FindFile(string directoryPath, string fileName)
    {
        try
        {
            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileName(path),
                    fileName,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? FindDirectory(string directoryPath, string directoryName)
    {
        try
        {
            return Directory.EnumerateDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileName(path),
                    directoryName,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? FindExecutable(string rootPath)
    {
        foreach (var candidate in ExecutableCandidates)
        {
            var result = FindFile(rootPath, candidate);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static string? ReadExecutableVersion(string? executablePath)
    {
        if (executablePath is null)
        {
            return null;
        }

        try
        {
            var version = FileVersionInfo.GetVersionInfo(executablePath).FileVersion;
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> BuildNotes(
        SourceContainerFormat format,
        bool hasBuildInfo,
        bool hasCascDataDirectory,
        int archiveCount)
    {
        var notes = new List<string>
        {
            "Metadata-only inspection: no archive payloads were read, extracted, changed, or copied."
        };

        switch (format)
        {
            case SourceContainerFormat.Casc:
                notes.Add(hasBuildInfo
                    ? "Found .build.info; this is a strong indicator of a CASC-based installation."
                    : "Found a CASC-like Data directory structure, but no .build.info marker.");
                if (hasCascDataDirectory)
                {
                    notes.Add("The Data/data payload directory was not recursively scanned.");
                }

                notes.Add("A CASC container alone does not identify the game product or expansion. Confirm the source build separately.");
                break;

            case SourceContainerFormat.Mpq:
                notes.Add($"Found {archiveCount} MPQ archive(s). Archive contents were not inspected.");
                notes.Add("Confirm the exact client build before using these archives as a source of gameplay or world data.");
                break;

            default:
                notes.Add("No known CASC or MPQ markers were found. This may not be a supported game installation root.");
                break;
        }

        return notes;
    }
}
