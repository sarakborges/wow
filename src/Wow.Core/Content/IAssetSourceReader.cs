namespace Wow.Core.Content;

/// <summary>
/// Read-only access to source data. Implementations must not modify the
/// original game installation or silently rewrite source identifiers.
/// </summary>
public interface IAssetSourceReader
{
    ContentSource Source { get; }

    ValueTask<Stream> OpenAssetAsync(
        SourceAssetReference reference,
        CancellationToken cancellationToken = default);
}
