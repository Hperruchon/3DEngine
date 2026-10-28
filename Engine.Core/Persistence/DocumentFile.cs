using System.Text;
using Engine.Contracts;

namespace Engine.Core.Persistence;

// The open of a document file, and the start of a file for a new document (ADR-0015
// items 8 to 10). A host calls one of these at its start, before its surface serves
// a request.
public static class DocumentFile
{
    public const string Extension = ".3de";

    // The folder of unsaved documents: <local application data>/3DEngine/unsaved.
    public static string DefaultUnsavedFolder
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DEngine", "unsaved");

    // The length unit of the header of a file, so that a host makes its Document
    // with that unit before the open.
    public static string ReadLengthUnit(string path)
    {
        var lines = Lines(path, out _);
        if (lines.Count == 0)
            throw DocumentFileException.Load($"The file {path} has no complete header line.");
        return EntryCodec.ReadHeader(lines[0].Text);
    }

    // A new file with its header at the path; the session writes each command to it.
    public static void Create(DocumentSession session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.Attach(DocumentJournal.Create(path, session.LengthUnit));
    }

    // The session writes into the folder of unsaved documents from its first command.
    public static void Unsaved(DocumentSession session, string folder)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.Attach(DocumentJournal.Unsaved(folder, session.DocumentId, session.LengthUnit));
    }

    // Applies each entry of the file through the bus of the session, with no write,
    // then the session writes each later command to the file. The session must be
    // new, with the unit of the header. It gives the warnings of the open.
    public static async Task<IReadOnlyList<Diagnostic>> Open(DocumentSession session, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var diagnostics = new List<Diagnostic>();
        var lines = Lines(path, out var torn);
        if (lines.Count == 0)
            throw DocumentFileException.Load($"The file {path} has no complete header line.");

        var unit = EntryCodec.ReadHeader(lines[0].Text);
        if (unit != session.LengthUnit)
            throw DocumentFileException.Load($"The file gives the length unit {unit}, and the Document has {session.LengthUnit}.");

        var validLength = lines[^1].End;
        var backend = session.BackendIdentity;
        var versionWarned = false;
        for (var index = 1; index < lines.Count; index++)
        {
            DecodedEntry entry;
            try
            {
                entry = EntryCodec.ReadEntry(lines[index].Text, session.CommandRegistry);
            }
            catch (DocumentFileException) when (index == lines.Count - 1 && torn is null)
            {
                // A last line that does not parse is a torn line too (ADR-0015 item 8).
                torn = lines[index].Text;
                validLength = lines[index - 1].End;
                break;
            }

            if (entry.Version != index)
                throw DocumentFileException.Load($"Line {index + 1} gives the version {entry.Version}, and the entries count from 1 with no gap, so it must be {index}.");

            if (entry.Backend.Name != backend.Name)
                throw new DocumentFileException(
                    DiagnosticCodes.IoBackendMismatch,
                    $"The entry of version {entry.Version} records the backend '{entry.Backend.Name}', and this host has the backend '{backend.Name}'. A replay uses the recorded backend (anti-objective 10).");

            if (entry.Backend.Version != backend.Version && !versionWarned)
            {
                versionWarned = true;
                diagnostics.Add(new Diagnostic(
                    Severity.Warning,
                    DiagnosticCodes.IoBackendVersion,
                    $"The entry of version {entry.Version} records the version {entry.Backend.Version} of the backend '{backend.Name}', and this host has the version {backend.Version}. The open continues."));
            }

            var result = await session.Apply(entry.Command, ct).ConfigureAwait(false);
            if (result.Status != CommandStatus.Applied)
                throw DocumentFileException.Load(
                    $"The entry of version {entry.Version} ({entry.Command.Name}) gave {result.Status}"
                    + (result.Error is { } error ? $", with {error.Code}: {error.Message}" : string.Empty)
                    + ". Each entry was applied one time, so an open must apply it too.");
        }

        if (torn is not null)
            diagnostics.Add(new Diagnostic(
                Severity.Warning,
                DiagnosticCodes.IoTruncated,
                $"The last line of {path} is not complete or does not parse, and the open cut it off ({Encoding.UTF8.GetByteCount(torn)} bytes)."));

        session.Attach(DocumentJournal.Resume(path, unit, validLength));
        return diagnostics;
    }

    // The complete lines of a file, each with the offset after its line feed, and the
    // text after the last line feed, or null when the file ends with a line feed.
    private static List<(string Text, long End)> Lines(string path, out string? torn)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw DocumentFileException.Load($"The file {path} cannot be read: {ex.Message}");
        }

        var lines = new List<(string, long)>();
        var start = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n')
                continue;
            lines.Add((Encoding.UTF8.GetString(bytes, start, index - start), index + 1));
            start = index + 1;
        }

        torn = start < bytes.Length ? Encoding.UTF8.GetString(bytes, start, bytes.Length - start) : null;
        return lines;
    }
}

// A failure of the open of a document file, with its code (ADR-0015 item 8).
public sealed class DocumentFileException(string code, string message) : IOException($"{code}: {message}")
{
    public string Code { get; } = code;

    internal static DocumentFileException Load(string message) => new(DiagnosticCodes.IoLoadFailed, message);
}
