using Engine.Contracts;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Import;

namespace ThreeDEngine.Desktop;

// ADR-0083 D1 (phases P9c and P9d, TASK-0259): the import of a chosen STL file, for the next session: the name of its
// asset in the store and the name of the file.
internal sealed record StlImport(string Asset, string Name);

// ADR-0083 D1 and D3: the open of an STL file that the person chose. The client reads the bytes, checks them with the
// reader of the engine, and puts them into the store; a fault gives its code and no new document. The engine reads the
// same bytes again at the import, so the check of the client is a convenience and the engine stays the authority.
internal static class StlOpening
{
    public static async Task<(StlImport? Import, ErrorDetail? Error)> Prepare(string path, byte[] content, AssetStore store, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(store);
        var (_, fault) = StlReader.Read(content);
        if (fault is not null)
            return (null, fault);

        using var stream = new MemoryStream(content, writable: false);
        var (asset, _, error) = await store.Put(stream, ct).ConfigureAwait(false);
        return asset is null ? (null, error) : (new StlImport(asset, FileName(path)), null);
    }

    // ADR-0083 D1: the one import command of a new part document; the body, or the refusal of the engine.
    public static async Task<(Guid? Body, ErrorDetail? Error)> Import(DocumentSession session, StlImport import, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(import);
        var result = await session.Apply(new ImportStlCommand { Asset = import.Asset, BodyName = import.Name }, ct).ConfigureAwait(false);
        return result.Status == CommandStatus.Applied && result.Outputs.TryGet<Guid>("bodyId", out var body)
            ? (body, null)
            : (null, result.Error ?? new ErrorDetail(DiagnosticCodes.StlAsset, $"The import of {import.Name} was {result.Status}."));
    }

    // The name of the file of a path, or of a content URI of the document picker of Android, whose last segment holds the
    // folder and the name with %2F for each separator ("content://.../document/primary%3ADownload%2Fbracket.stl").
    public static string FileName(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var text = path.StartsWith("content:", StringComparison.Ordinal) ? Uri.UnescapeDataString(path) : path;
        var cut = text.LastIndexOfAny(['/', '\\', ':']);
        return cut < 0 ? text : text[(cut + 1)..];
    }
}
