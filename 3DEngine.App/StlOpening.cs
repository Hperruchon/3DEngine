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
    // Finding N2 of the review of Codex after v0.243 (TASK-0272): the largest file that the client reads, so that a file
    // much larger than the limit of triangles of ADR-0083 item 6 gives its code before the read and not an end of the
    // session for want of memory. Finding E183 of the codebase review of 2026-11-07 (c) (TASK-0281): the limit is the limit
    // of the store, because the store refuses a larger file after the read; a binary file of the limit of triangles has
    // 15,000,084 bytes, which is less. Until TASK-0281 the limit was 153,600,084 bytes, and the reader of a text file near it
    // took 2,438 MiB.
    public const long MaximumBytes = AssetStore.MaximumBytes;

    // E-ASSET-TOO-LARGE for a file of more than MaximumBytes, or null. Finding E190 of the codebase review of 2026-11-08
    // (TASK-0295): the file becomes an asset, and E-ASSET-TOO-LARGE means more than 16 MiB in one asset; until TASK-0295
    // the client gave E-STL-TOO-LARGE, whose meaning is more triangles than the limit.
    public static ErrorDetail? TooLarge(long size)
        => size > MaximumBytes ? OverLimit($"The file has {size} bytes") : null;

    // E-ASSET-TOO-LARGE for a read that stopped at MaximumBytes, when SDL did not know the size before the read.
    public static ErrorDetail OverLimit(string fileHas)
        => new(DiagnosticCodes.AssetTooLarge, $"{fileHas}, and the program reads an STL file of at most {MaximumBytes} bytes (16 MiB), the limit of an asset (ADR-0083 item 6, ADR-0044 item 3).");

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
