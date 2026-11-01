namespace Wow.Core.Content;

public enum SourceContainerFormat
{
    Unknown,
    Mpq,
    Casc
}

public enum SourceRole
{
    Unspecified,
    ClassicWorld,
    WotlkGameplay,
    MidnightCharacters
}

public sealed record SourceFileEntry(
    string RelativePath,
    long SizeBytes);

/// <summary>
/// Metadata-only inventory of a locally installed game source.
/// The manifest intentionally contains paths and file metadata only; it never
/// contains file payloads or extracted archive contents.
/// </summary>
public sealed record SourceInspectionManifest(
    int SchemaVersion,
    string RootPath,
    SourceRole Role,
    SourceContainerFormat ContainerFormat,
    string DetectionConfidence,
    bool HasBuildInfo,
    bool HasCascDataDirectory,
    bool HasCascIndicesDirectory,
    string? ExecutableName,
    string? ExecutableVersion,
    IReadOnlyList<SourceFileEntry> MpqArchives,
    IReadOnlyList<string> Markers,
    IReadOnlyList<string> Notes);
