using Engine.Contracts;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// ADR-0102 item 2 (phase P9g, TASK-0316): what a dialog of the system was opened for. The result of the dialog gives it back,
// so a late result of "Import STL..." never opens a project, and a late result of "Export STL..." never saves one.
internal enum DialogPurpose
{
    ImportStl = 0,
    OpenProject = 1,
    SaveProject = 2,
    ExportStl = 3,
}

// The dialogs of the system, apart from the window so that a test gives the results as samples.
internal interface IFileDialogs
{
    // Opens the dialog for one file with the filter; false when a dialog is open already.
    bool Open(string filterName, string pattern, DialogPurpose purpose);

    // Opens the save dialog with the filter at the default location, or where the system chooses with null; false when a
    // dialog is open already.
    bool Save(string filterName, string pattern, DialogPurpose purpose, string? location);
}

// The dialogs of SDL in the window (ADR-0102 item 2).
internal sealed class WindowDialogs(Window window) : IFileDialogs
{
    public bool Open(string filterName, string pattern, DialogPurpose purpose) => window.OpenFile(filterName, pattern, (int)purpose);

    public bool Save(string filterName, string pattern, DialogPurpose purpose, string? location) => window.SaveFile(filterName, pattern, (int)purpose, location);
}

// The result of a step of the project path: the line of the document, or null to keep it, and a refusal that the home screen
// shows as its message (finding V127, TASK-0271).
internal readonly record struct FileOutcome(string? Line, (string Code, string Text)? Refusal = null);

// ADR-0102 (phase P9g, TASK-0316): the project path of the window, with no terminal. A person opens a project in each folder,
// saves it in the folder that the person chooses, and exports an STL file apart from the project. The surface of an agent
// keeps the document folder of ADR-0030 item 2.
internal sealed class ProjectFiles(ISessionHost host, IDocumentFiles files, IFileDialogs dialogs)
{
    public const string ProjectFilter = "3DEngine project";
    public const string ProjectPattern = "3de";
    public const string StlFilter = "STL file";
    public const string StlPattern = "stl";
    public const string Busy = "A file dialog is open already.";

    private static readonly FileOutcome BusyOutcome = new(Busy, (string.Empty, Busy));

    // Item 5 (Q3 A): the change of project that waits for the answer, or null.
    private Func<FileOutcome>? _waiting;

    // True while the question before a change of project waits for its answer.
    public bool Asking { get; private set; }

    // Item 5 (Q3 A): a change of project asks first while the document has no file and holds at least one command.
    public bool MustAsk()
        => host.FileName is null && host.Session.Read((document, _) => document.Version, CancellationToken.None).GetAwaiter().GetResult() > 0;

    // A change of project: an open, a recent row, a new part or an import. It runs now, or after the answer to the question.
    public FileOutcome Change(Func<FileOutcome> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (!MustAsk())
            return change();
        (_waiting, Asking) = (change, true);
        return default;
    }

    // Item 5: "Save as..." opens the save dialog and keeps the change until the save; "Discard" leaves the journal in the folder
    // of unsaved documents (ADR-0030 item 6) and changes; "Cancel" drops the change.
    public FileOutcome Answer(ChangeAnswer answer)
    {
        Asking = false;
        var waiting = _waiting;
        switch (answer)
        {
            case ChangeAnswer.Discard:
                _waiting = null;
                return waiting?.Invoke() ?? default;
            case ChangeAnswer.SaveAs:
                var outcome = SaveAs();
                if (outcome.Line is not null)
                    _waiting = null;
                return outcome;
            default:
                _waiting = null;
                return default;
        }
    }

    // Item 1: "Open project..." in each folder, and "Import STL..." as before (phase P9c).
    public FileOutcome OpenProject() => dialogs.Open(ProjectFilter, ProjectPattern, DialogPurpose.OpenProject) ? default : BusyOutcome;

    public FileOutcome ImportStl() => dialogs.Open(StlFilter, StlPattern, DialogPurpose.ImportStl) ? default : BusyOutcome;

    // Items 3 and 4: the save dialog starts at the file of the project, and the export dialog at its name with ".stl".
    public FileOutcome SaveAs()
        => dialogs.Save(ProjectFilter, ProjectPattern, DialogPurpose.SaveProject, Project()) ? default : BusyOutcome;

    public FileOutcome Export()
        => dialogs.Save(StlFilter, StlPattern, DialogPurpose.ExportStl, Project() is { } project ? Path.ChangeExtension(project, ".stl") : null) ? default : BusyOutcome;

    // The path of the file of the project, or null for a document with no file.
    private string? Project() => host.FileName is not null ? host.Session.FilePath : null;

    // Item 1: the check of ADR-0030 item 5 and the next session (R14b), for a path in each folder.
    public FileOutcome OpenPath(string path)
    {
        var error = files.Open(path, CancellationToken.None).GetAwaiter().GetResult();
        return error is null
            ? new FileOutcome($"Opening {Path.GetFileName(path)}")
            : new FileOutcome($"{error.Code}: {error.Message}", (error.Code, $"{Path.GetFileName(path)}: {error.Message}"));
    }

    // Item 2: the result of a dialog that is not an import. A dialog that the person closes gives nothing; a dialog that
    // fails gives its reason (V131).
    public FileOutcome Take(string? path, string? failure, DialogPurpose purpose)
    {
        if (path is null)
        {
            if (purpose == DialogPurpose.SaveProject)
                _waiting = null;
            return failure is null ? default : new FileOutcome($"The file dialog did not open: {failure}", (string.Empty, $"The file dialog did not open: {failure}"));
        }

        switch (purpose)
        {
            case DialogPurpose.OpenProject:
                return OpenPath(path);
            case DialogPurpose.SaveProject:
                var (saved, done) = Save(path);
                var waiting = _waiting;
                _waiting = null;
                return done && waiting is not null && waiting() is { Line: not null } next ? next : saved;
            case DialogPurpose.ExportStl:
                return Write(path);
            default:
                return default;
        }
    }

    // Item 3: the session moves its journal to the path (ADR-0015 item 9), and the recent projects remember it.
    private (FileOutcome Outcome, bool Done) Save(string path)
    {
        path = WithExtension(path, ".3de");
        var warnings = new List<Diagnostic>();
        var error = host.Session.SaveAs(path, warnings).GetAwaiter().GetResult();
        if (error is not null)
            return (new FileOutcome($"{error.Code}: {error.Message}", (error.Code, error.Message)), false);
        files.Remember(path);
        var saved = $"Saved as {Path.GetFileName(path)}";
        return (new FileOutcome(warnings.Count == 0 ? saved : $"{saved}. {string.Join(" ", warnings.Select(w => $"{w.Code}: {w.Message}"))}"), true);
    }

    // Item 4: the live bodies as one STL file at the path (ADR-0048 item 6); the file of the project does not change.
    private FileOutcome Write(string path)
    {
        path = WithExtension(path, ".stl");
        var (content, error) = host.Session.ReadGeometry(Engine.Core.Export.StlExport.Write).GetAwaiter().GetResult();
        if (error is not null)
            return new FileOutcome($"{error.Code}: {error.Message}", (error.Code, error.Message));
        try
        {
            File.WriteAllBytes(path, content!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new FileOutcome($"{Engine.Core.DiagnosticCodes.IoSaveFailed}: {ex.Message}", (Engine.Core.DiagnosticCodes.IoSaveFailed, ex.Message));
        }

        return new FileOutcome($"Exported {(content!.Length - 84) / 50} triangles to {Path.GetFileName(path)}");
    }

    // The path with the extension, which a dialog of the system can leave out.
    internal static string WithExtension(string path, string extension)
        => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
}
