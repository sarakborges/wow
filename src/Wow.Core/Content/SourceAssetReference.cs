namespace Wow.Core.Content;

/// <summary>
/// Stable reference to an input record or file in a particular source build.
/// Identifier is intentionally a string because source generations use
/// different identifier schemes (paths, numeric IDs, FileDataIDs, and so on).
/// </summary>
public sealed record SourceAssetReference(
    ContentSource Source,
    string SourceBuild,
    ContentCategory Category,
    string Identifier,
    string? Locale = null);

/// <summary>
/// Provenance and normalized output location for an imported runtime asset.
/// Output paths are relative to the configured import root.
/// </summary>
public sealed record ImportedAssetDescriptor(
    string RuntimeId,
    ContentCategory Category,
    string RelativeOutputPath,
    int RuntimeSchemaVersion,
    SourceAssetReference Source,
    string SourceSha256,
    string ImporterName,
    string ImporterVersion);
