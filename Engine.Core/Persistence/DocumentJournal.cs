using System.Text;
using Engine.Contracts;

namespace Engine.Core.Persistence;

// The file of one document, which the bus writes in the commit (ADR-0015 items 4, 9
// and 13). Each line is one write call, then a flush to disk, so each applied command
// is in the file before its result. A failed write cuts the file back to its last
// complete line, and the journal is broken: the bus then refuses each later command.
public sealed class DocumentJournal : IDisposable
{
    private Stream? _stream;
    private readonly string _lengthUnit;
    private readonly string _kind;
    private readonly Func<string, FileMode, Stream> _open;
    private readonly Action<string> _delete;

    // ADR-0071 item 4: the kind goes into the header of a new file.
    private DocumentJournal(string path, string lengthUnit, Stream? stream, Func<string, FileMode, Stream>? open = null, string kind = Document.Part, Action<string>? delete = null)
    {
        FilePath = path;
        _lengthUnit = lengthUnit;
        _kind = kind;
        _stream = stream;
        _open = open ?? OpenFile;
        _delete = delete ?? File.Delete;
    }

    // The path of the file. For a document with no name, the file exists from the
    // first command.
    public string FilePath { get; private set; }

    // True after a failed write or flush (E-IO-WRITE-FAILED).
    public bool Broken { get; private set; }

    // A new file with its header, written now.
    public static DocumentJournal Create(string path, string lengthUnit, string kind = Document.Part)
    {
        var journal = new DocumentJournal(path, lengthUnit, OpenFile(path, FileMode.CreateNew), kind: kind);
        try
        {
            journal.Append(EntryCodec.Header(lengthUnit, kind));
        }
        catch
        {
            journal.Dispose();
            throw;
        }

        return journal;
    }

    // A document with no name: the file is made in the folder at the first command
    // (ADR-0015 item 9).
    public static DocumentJournal Unsaved(string folder, Guid documentId, string lengthUnit, string kind = Document.Part)
        => new(Path.Combine(folder, documentId.ToString("D") + DocumentFile.Extension), lengthUnit, null, kind: kind);

    // The file of an open, held with the access and the share of a journal before the
    // open reads it, so that no other program writes to it while the entries apply
    // (finding E83 of the second codebase review of 2026-10-28).
    internal static FileStream Hold(string path) => OpenFile(path, FileMode.Open);

    // The file that an open held and read, with its length cut to the last valid line.
    internal static DocumentJournal Resume(FileStream stream, string path, string lengthUnit, long length, string kind = Document.Part)
    {
        if (stream.Length != length)
        {
            stream.SetLength(length);
            Flush(stream);
        }

        stream.Seek(0, SeekOrigin.End);
        return new DocumentJournal(path, lengthUnit, stream, kind: kind);
    }

    // A journal on a stream that a test gives, for a write that fails, and an optional
    // function that opens a file in place of the file system, for an open that fails, and an optional function that
    // deletes a file, for a delete that fails.
    internal static DocumentJournal ForTest(string path, string lengthUnit, Stream? stream, Func<string, FileMode, Stream>? open = null, Action<string>? delete = null)
        => new(path, lengthUnit, stream, open, delete: delete);

    // Writes one line and its line feed in one call, then flushes to disk. A failure
    // throws an IOException, after the file is cut back and the journal is broken.
    internal void Append(string line)
    {
        if (Broken)
            throw new IOException("An earlier write of this document file failed.");

        if (_stream is null)
        {
            // A failure of the first open breaks the journal too. Until TASK-0099 it came
            // before the try, so the journal stayed whole, and an UnauthorizedAccessException
            // left the bus (finding E84 of the second codebase review of 2026-10-28).
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                _stream = _open(FilePath, FileMode.CreateNew);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                Broken = true;
                throw new IOException($"The document file {FilePath} cannot be made: {ex.Message}", ex);
            }

            Append(EntryCodec.Header(_lengthUnit, _kind));
        }

        var length = _stream.Length;
        try
        {
            _stream.Write(Encoding.UTF8.GetBytes(line + "\n"));
            Flush(_stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Broken = true;
            try
            {
                _stream.SetLength(length);
                Flush(_stream);
            }
            catch (Exception cut) when (cut is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // The file can keep the line of this command. A part of a line is a torn
                // line, but a complete line is an entry that an open applies, although the
                // client saw the command rejected (E84). The message says so.
                throw new IOException(
                    $"The write of the document file {FilePath} failed: {ex.Message} The cut back to the last complete line "
                    + $"failed too ({cut.Message}), so the file can keep the line of this command, and an open then applies it.",
                    ex);
            }

            throw new IOException($"The write of the document file {FilePath} failed: {ex.Message}", ex);
        }
    }

    // Moves the file to a new name; later lines go there. The target must not exist.
    // A journal whose file does not exist yet makes the file at the new name.
    //
    // Finding E162 of the codebase review of 2026-11-06 (d) (TASK-0241), Q1 A: until TASK-0241 the journal closed its
    // file, moved it and opened it again, so for a moment no handle held the document, and a second host could append a
    // line there; the next line of this journal then gave a version two times, and the document did not open. Now the
    // journal makes the file at the new name with the share of a journal, copies each line into it and flushes it, and
    // only then closes and deletes the old file. A handle holds the document at each moment. A failure before the switch
    // deletes the new file and leaves the journal whole at its old name.
    //
    // Finding E164 of the codebase review of 2026-11-06 (e) (TASK-0251): it gives the path of the old file when the delete
    // of that file fails, and null otherwise, so that the save gives the warning W-IO-OLD-FILE-KEPT; until TASK-0251 the
    // old file stayed with no report, and it opens as a second document. A failure before the switch cuts the new file
    // to length 0 before it deletes it, so a new file that the delete leaves holds no part of a document and does not open.
    //
    // Finding E168 of the codebase review of 2026-11-07 (TASK-0263): when the cut and the delete both fail, the new file
    // keeps the lines of the copy and opens as a second document, so the failure names that file.
    internal string? MoveTo(string target)
    {
        if (File.Exists(target))
            throw new IOException($"The file {target} exists. A save chooses a new name only.");

        // The document folder of a new account does not exist before the first save (ADR-0030 item 2).
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        if (_stream is null)
        {
            _stream = _open(target, FileMode.CreateNew);
            FilePath = target;
            Append(EntryCodec.Header(_lengthUnit, _kind));
            return null;
        }

        Flush(_stream);
        var copy = _open(target, FileMode.CreateNew);
        try
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.CopyTo(copy);
            Flush(copy);
        }
        catch (Exception ex)
        {
            var cut = CutQuietly(copy);
            copy.Dispose();
            var gone = DeleteQuietly(target);
            _stream.Seek(0, SeekOrigin.End);
            if (!cut && !gone && ex is IOException or UnauthorizedAccessException)
                throw new IOException($"{ex.Message} The new file {target} stays with a part of the document, and it opens as a second document.", ex);
            throw;
        }

        var old = FilePath;
        _stream.Dispose();
        _stream = copy;
        FilePath = target;

        // The document is whole at its new name. A program that holds the old file keeps it there, with no effect on
        // this document.
        return DeleteQuietly(old) ? null : old;
    }

    // True when the file is gone.
    private bool DeleteQuietly(string path)
    {
        try
        {
            _delete(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The file stays; nothing of the journal depends on it.
            return false;
        }
    }

    // True when the file has length 0.
    private static bool CutQuietly(Stream stream)
    {
        try
        {
            stream.SetLength(0);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // The delete that comes next can still remove the file.
            return false;
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }

    // No share for a write, so a second host cannot write to the file; on Linux and
    // macOS the lock is advisory (ADR-0015 item 13).
    private static FileStream OpenFile(string path, FileMode mode)
        => new(path, mode, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.None);

    // FileStream.Flush(true) flushes to disk; on macOS .NET uses F_FULLFSYNC.
    private static void Flush(Stream stream)
    {
        if (stream is FileStream file)
            file.Flush(flushToDisk: true);
        else
            stream.Flush();
    }
}
