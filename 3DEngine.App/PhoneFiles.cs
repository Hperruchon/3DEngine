using Engine.Contracts;

namespace ThreeDEngine.Desktop;

// ADR-0083 D1 and D6 (phase P9d, TASK-0260): the files of the phone. The phone has no document folder yet (the test sheet
// of the phone, step 9), so it has no recent file and opens no document file; it opens an STL file through the document
// picker of Android, and a new part is a new session in the same window. One object lives for the run of the program, so
// the import and the home screen pass from one session to the next.
internal sealed class PhoneFiles : IDocumentFiles
{
    private StlImport? _import;
    private bool _requested;

    public string? Resolve(string name, out string? refusal)
    {
        refusal = "The phone has no document folder yet.";
        return null;
    }

    public Task<ErrorDetail?> Open(string path, CancellationToken ct)
        => Task.FromResult<ErrorDetail?>(new ErrorDetail(Engine.Core.DiagnosticCodes.IoLoadFailed, "The phone has no document folder yet."));

    public IReadOnlyList<string> Recent(int count) => [];

    public void RequestNew(StlImport? import)
    {
        Volatile.Write(ref _import, import);
        Volatile.Write(ref _requested, true);
    }

    // True one time after a request of a new part; the loop of the phone then makes a new session.
    public bool TakeRequestedNew() => Interlocked.Exchange(ref _requested, false);

    public StlImport? TakeImport() => Interlocked.Exchange(ref _import, null);

    public bool Home { get; set; } = true;

    public bool HasFolder => false;
}
