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

    // A new file with its header at the path; the session writes each command to it. A
    // file that cannot be made gives E-IO-LOAD-FAILED, as a file that does not open. Until
    // TASK-0099 a path of a folder gave an UnauthorizedAccessException, and engine-api-http
    // stopped with exit code 5, not 6 (finding E82 of the second codebase review of 2026-10-28).
    public static void Create(DocumentSession session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        DocumentJournal journal;
        try
        {
            journal = DocumentJournal.Create(path, session.LengthUnit);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw DocumentFileException.Load($"The file {path} cannot be made: {ex.Message}");
        }

        session.Attach(journal);
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
    public static Task<IReadOnlyList<Diagnostic>> Open(DocumentSession session, string path, CancellationToken ct = default)
        => Load(session, path, attach: true, hold: true, ct);

    // ADR-0030 item 5: opens the file in a scratch session on the given backend, with
    // no write, and gives null or the error of the open. A host calls this before it
    // starts again with the file, so that a file that does not open leaves the person
    // with the window. The check holds the file with the access and the share of a
    // journal, so a file that another program holds fails it; hold is false only for the
    // file that the host itself holds, which it frees before it starts again. Until
    // TASK-0099 the check read with the share of a write and passed for a file of another
    // desktop, and the new program then stopped (finding V31 of the second codebase review
    // of 2026-10-28).
    public static async Task<ErrorDetail?> Check(string path, Engine.Contracts.Geometry.IGeometryBackend backend, bool hold = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        try
        {
            var session = new DocumentSession(Hosting.EngineHosting.CreateDefault(backend, ReadLengthUnit(path)));
            try
            {
                await Load(session, path, attach: false, hold, ct).ConfigureAwait(false);
                return null;
            }
            finally
            {
                session.Close();
            }
        }
        catch (DocumentFileException ex)
        {
            return new ErrorDetail(ex.Code, ex.Message);
        }
    }

    private static async Task<IReadOnlyList<Diagnostic>> Load(DocumentSession session, string path, bool attach, bool hold, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        // The versions of the entries count from 1, so the session must be new. An open
        // into a session with commands wrote a file that did not open again (finding
        // E75 of the second codebase review of 2026-10-28).
        var start = await session.Read((document, _) => document.Version, ct).ConfigureAwait(false);
        if (start != 0)
            throw DocumentFileException.Load($"An open needs a new session, and this session is at the version {start}.");

        // An open holds the file from its first read, so a file that another session holds
        // fails before an entry applies. Until TASK-0099 the open read with the share of a
        // write, applied each entry, and only then found the lock, with no code (E83).
        FileStream? held = null;
        if (attach || hold)
        {
            try
            {
                held = DocumentJournal.Hold(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw DocumentFileException.Load($"The file {path} cannot be held for a write: {ex.Message}");
            }
        }

        try
        {
            var (warnings, validLength) = await Apply(session, path, held, ct).ConfigureAwait(false);
            if (attach && held is not null)
            {
                DocumentJournal journal;
                try
                {
                    journal = DocumentJournal.Resume(held, path, session.LengthUnit, validLength);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw DocumentFileException.Load($"The file {path} cannot be cut to its last complete line: {ex.Message}");
                }

                held = null;
                session.Attach(journal);
            }

            return warnings;
        }
        finally
        {
            held?.Dispose();
        }
    }

    // Applies each entry of the file, and gives the warnings and the length of the valid lines.
    private static async Task<(IReadOnlyList<Diagnostic> Warnings, long ValidLength)> Apply(DocumentSession session, string path, FileStream? held, CancellationToken ct)
    {
        var diagnostics = new List<Diagnostic>();
        var lines = Lines(path, held, out var torn);
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
            catch (DocumentFileException) when (index == lines.Count - 1 && torn is null && !EntryCodec.IsJson(lines[index].Text))
            {
                // A last line that is not JSON is a torn line too (ADR-0015 item 8). A complete
                // line that is JSON is not a torn write, because Append writes a line and its line
                // feed in one call, so each other failure refuses the open and the file keeps the
                // line. Until TASK-0099 the open cut it, for example a command of a newer engine
                // (finding E76 of the second codebase review of 2026-10-28).
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

            // ADR-0015 item 8: each entry must be applied and give the version of the
            // entry. The open does not look in the idempotency cache, so an entry whose
            // CommandId came before is applied again, as the live session applied it (E75).
            // An exception of a handler or of the backend gives the code of an open too (E82).
            CommandResult result;
            try
            {
                result = await session.ApplyEntry(entry.Command, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw DocumentFileException.Load($"The entry of version {entry.Version} ({entry.Command.Name}) threw {ex.GetType().Name}: {ex.Message}");
            }

            if (result.Status != CommandStatus.Applied)
                throw DocumentFileException.Load(
                    $"The entry of version {entry.Version} ({entry.Command.Name}) gave {result.Status}"
                    + (result.Error is { } error ? $", with {error.Code}: {error.Message}" : string.Empty)
                    + ". Each entry was applied one time, so an open must apply it too.");
            if (result.DocumentVersion != entry.Version)
                throw DocumentFileException.Load(
                    $"The entry of version {entry.Version} ({entry.Command.Name}) gave the version {result.DocumentVersion}. "
                    + "Each entry gives the version of the entry, so the Document after the open differs from the file.");
        }

        if (torn is not null)
            diagnostics.Add(new Diagnostic(
                Severity.Warning,
                DiagnosticCodes.IoTruncated,
                $"The last line of {path} is not complete or does not parse, and the open cut it off ({Encoding.UTF8.GetByteCount(torn)} bytes)."));

        return (diagnostics, validLength);
    }

    // The complete lines of a file, each with the offset after its line feed, and the
    // text after the last line feed, or null when the file ends with a line feed. An open
    // reads through the stream that holds the file.
    private static List<(string Text, long End)> Lines(string path, out string? torn) => Lines(path, null, out torn);

    private static List<(string Text, long End)> Lines(string path, FileStream? held, out string? torn)
    {
        byte[] bytes;
        try
        {
            if (held is not null)
            {
                held.Seek(0, SeekOrigin.Begin);
                bytes = new byte[held.Length];
                held.ReadExactly(bytes);
            }
            else
            {
                // The reader shares the write access, so a check reads a file that a
                // session of this process holds open (ADR-0030 item 5).
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                bytes = new byte[stream.Length];
                stream.ReadExactly(bytes);
            }
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
