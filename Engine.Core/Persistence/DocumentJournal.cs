using System.Text;

namespace Engine.Core.Persistence;

// The file of one document, which the bus writes in the commit (ADR-0015 items 4, 9
// and 13). Each line is one write call, then a flush to disk, so each applied command
// is in the file before its result. A failed write cuts the file back to its last
// complete line, and the journal is broken: the bus then refuses each later command.
public sealed class DocumentJournal : IDisposable
{
    private Stream? _stream;
    private readonly string _lengthUnit;

    private DocumentJournal(string path, string lengthUnit, Stream? stream)
    {
        FilePath = path;
        _lengthUnit = lengthUnit;
        _stream = stream;
    }

    // The path of the file. For a document with no name, the file exists from the
    // first command.
    public string FilePath { get; private set; }

    // True after a failed write or flush (E-IO-WRITE-FAILED).
    public bool Broken { get; private set; }

    // A new file with its header, written now.
    public static DocumentJournal Create(string path, string lengthUnit)
    {
        var journal = new DocumentJournal(path, lengthUnit, OpenFile(path, FileMode.CreateNew));
        journal.Append(EntryCodec.Header(lengthUnit));
        return journal;
    }

    // A document with no name: the file is made in the folder at the first command
    // (ADR-0015 item 9).
    public static DocumentJournal Unsaved(string folder, Guid documentId, string lengthUnit)
        => new(Path.Combine(folder, documentId.ToString("D") + DocumentFile.Extension), lengthUnit, null);

    // A file that an open read, with its length cut to the last valid line.
    internal static DocumentJournal Resume(string path, string lengthUnit, long length)
    {
        var stream = OpenFile(path, FileMode.Open);
        if (stream.Length != length)
        {
            stream.SetLength(length);
            Flush(stream);
        }

        stream.Seek(0, SeekOrigin.End);
        return new DocumentJournal(path, lengthUnit, stream);
    }

    // A journal on a stream that a test gives, for a write that fails.
    internal static DocumentJournal ForTest(string path, string lengthUnit, Stream stream) => new(path, lengthUnit, stream);

    // Writes one line and its line feed in one call, then flushes to disk. A failure
    // throws an IOException, after the file is cut back and the journal is broken.
    internal void Append(string line)
    {
        if (Broken)
            throw new IOException("An earlier write of this document file failed.");

        if (_stream is null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            _stream = OpenFile(FilePath, FileMode.CreateNew);
            Append(EntryCodec.Header(_lengthUnit));
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
                // The file keeps a part of the line; an open cuts it as a torn last line.
            }

            throw new IOException($"The write of the document file {FilePath} failed: {ex.Message}", ex);
        }
    }

    // Moves the file to a new name; later lines go there. The target must not exist.
    // A journal whose file does not exist yet makes the file at the new name.
    internal void MoveTo(string target)
    {
        if (File.Exists(target))
            throw new IOException($"The file {target} exists. A save chooses a new name only.");

        // The document folder of a new account does not exist before the first save (ADR-0030 item 2).
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        if (_stream is null)
        {
            _stream = OpenFile(target, FileMode.CreateNew);
            FilePath = target;
            Append(EntryCodec.Header(_lengthUnit));
            return;
        }

        Flush(_stream);
        _stream.Dispose();
        _stream = null;
        try
        {
            File.Move(FilePath, target);
            FilePath = target;
        }
        finally
        {
            // The file is at its new name or still at the old one; the journal writes to it.
            _stream = OpenFile(FilePath, FileMode.Open);
            _stream.Seek(0, SeekOrigin.End);
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
