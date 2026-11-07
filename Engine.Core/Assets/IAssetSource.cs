using Engine.Contracts;

namespace Engine.Core.Assets;

// ADR-0083 item 2 (phase P9b, TASK-0258): the bytes of an asset by its name, for the handler of ImportStl, the first
// handler that reads the store (ADR-0044, amended). The store of an account is one source; a test and the probe of
// ADR-0074 give the bytes from memory.
public interface IAssetSource
{
    Task<(byte[]? Content, ErrorDetail? Error)> Read(string name, CancellationToken ct = default);
}

// The bytes of a few assets in memory, each under the SHA-256 of its bytes, as the store names it.
public sealed class AssetBytes : IAssetSource
{
    private readonly Dictionary<string, byte[]> _assets = new(StringComparer.Ordinal);

    public string Add(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var name = AssetStore.NameOf(content);
        _assets[name] = content;
        return name;
    }

    public Task<(byte[]? Content, ErrorDetail? Error)> Read(string name, CancellationToken ct = default)
        => Task.FromResult(_assets.TryGetValue(name, out var content)
            ? ((byte[]?)content, (ErrorDetail?)null)
            : (null, new ErrorDetail(DiagnosticCodes.AssetNotFound, $"No asset has the name '{name}'.")));
}
