using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Engine.Contracts;

namespace Engine.Core.Assets;

// ADR-0044 (phase V1b, TASK-0147): the store of the assets of an account, for example the image of a
// photo. The name of an asset is the SHA-256 of its bytes in 64 lower-case hexadecimal digits, so one
// name holds one content, and a later command records the name only (V2). The store is outside each
// document session: a save, a Save as and an open do not change it (the answer of the owner of
// 2026-10-31). It uses no transcendental function, so a name is the same on each platform.
public sealed partial class AssetStore
{
    // ADR-0044 item 3: an asset holds at most 16 MiB, under the request limit of Kestrel (30 MB).
    public const int MaximumBytes = 16 * 1024 * 1024;

    public AssetStore(string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        Folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
    }

    // <local application data of the account>/3DEngine/assets.
    public static string Default
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DEngine", "assets");

    public string Folder { get; }

    // The name of a content: the SHA-256 of its bytes, in lower-case hexadecimal.
    public static string NameOf(ReadOnlySpan<byte> content) => Convert.ToHexStringLower(SHA256.HashData(content));

    public static bool IsName(string? name) => name is not null && Name().IsMatch(name);

    // ADR-0044 item 2: reads the stream up to the limit, and puts its bytes under their name. The write
    // goes to a scratch file of the folder first, then moves to the name, so a reader never sees a
    // part of an asset. A name that the store holds already is not written again.
    public async Task<(string? Name, long Size, ErrorDetail? Error)> Put(Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaximumBytes)
                return (null, 0, new ErrorDetail(DiagnosticCodes.AssetTooLarge, $"An asset holds at most {MaximumBytes} bytes (16 MiB), and this one holds more (ADR-0044 item 3)."));
            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
        var name = NameOf(bytes.Span);
        var path = Path.Combine(Folder, name);
        if (File.Exists(path))
            return (name, bytes.Length, null);

        var scratch = Path.Combine(Folder, $"{name}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(Folder);
            await using (var file = new FileStream(scratch, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.WriteAsync(bytes, ct).ConfigureAwait(false);
                file.Flush(flushToDisk: true);
            }

            try
            {
                File.Move(scratch, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Another put of the same content came first; the content of the name is the same.
                File.Delete(scratch);
            }

            return (name, bytes.Length, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(scratch);
            return (null, 0, new ErrorDetail(DiagnosticCodes.AssetWriteFailed, $"The asset {name} could not be written to {Folder}: {ex.Message}"));
        }
    }

    // ADR-0044 item 2: the bytes of a name. The store computes the name of the bytes again, so a file
    // that changed on the disk gives an error and not a wrong image.
    public async Task<(byte[]? Content, ErrorDetail? Error)> Read(string name, CancellationToken ct = default)
    {
        if (!IsName(name))
            return (null, new ErrorDetail(DiagnosticCodes.AssetBadName, $"'{name}' is not the name of an asset: 64 lower-case hexadecimal digits (ADR-0044 item 1)."));

        var path = Path.Combine(Folder, name);
        byte[] content;
        try
        {
            content = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return (null, new ErrorDetail(DiagnosticCodes.AssetNotFound, $"The store at {Folder} holds no asset {name}."));
        }

        return NameOf(content) == name
            ? (content, null)
            : (null, new ErrorDetail(DiagnosticCodes.AssetCorrupt, $"The file of the asset {name} in {Folder} holds other bytes; the store does not give them."));
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The scratch file stays; its name ends with .tmp, so no read takes it.
        }
    }

    // Finding E113 of the codebase review of 2026-10-31 (c) (TASK-0152): $ also matches before a
    // final line feed, so a name with one passed, and a read then threw. \z ends at the end only.
    [GeneratedRegex(@"^[0-9a-f]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Name();
}
