using System.Numerics;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Microsoft.Extensions.Configuration;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0102 (phase P9g, TASK-0316): the project path of the window, with no terminal, with no GPU. The dialogs of the system
// are samples: a test records each dialog that the path asks for and gives its result as the person would. ADR-0102 names
// this file in its enforced-by field. The open of a file ends the home screen of the process (a static field of
// DesktopDocument), so these tests run in the collection of the other tests of that state.
[Collection(DearImGui.Collection)]
public sealed class ProjectPathTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-project-path-" + Guid.NewGuid().ToString("N"));

    public ProjectPathTests() => Directory.CreateDirectory(Projects);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A folder that is not the document folder, as the folder of the shelf of the owner.
    private string Projects => Path.Combine(_folder, "EntryHallShelf");

    private IConfiguration Settings(string? file = null) => new ConfigurationBuilder().AddCommandLine(Arguments(file)).Build();

    // The arguments of the settings, which the surface of the desktop takes too (E197, TASK-0324).
    private string[] Arguments(string? file = null) =>
    [
        "--port", "0",
        .. file is null ? Array.Empty<string>() : ["--file", file],
        $"--{DesktopSurface.DocumentFolderSetting}={Path.Combine(_folder, "documents")}",
        $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(_folder, "unsaved")}",
        $"--Engine:AssetFolder={Path.Combine(_folder, "assets")}",
    ];

    // The dialogs that the path asks for, and an open dialog that refuses a second one.
    private sealed class Dialogs : IFileDialogs
    {
        public List<(string Filter, string Pattern, DialogPurpose Purpose, string? Location, bool Save)> Asked { get; } = [];

        public bool Busy { get; set; }

        public bool Open(string filterName, string pattern, DialogPurpose purpose)
        {
            if (Busy)
                return false;
            Asked.Add((filterName, pattern, purpose, null, false));
            return true;
        }

        public bool Save(string filterName, string pattern, DialogPurpose purpose, string? location)
        {
            if (Busy)
                return false;
            Asked.Add((filterName, pattern, purpose, location, true));
            return true;
        }
    }

    private static DesktopSession Started(IConfiguration settings)
    {
        var (session, code, message) = DesktopSession.Open(settings);
        Assert.True(session is not null, $"{code}: {message}");
        return session;
    }

    // The bytes of a file that the session holds: the journal shares the read only with a writer allowed.
    private static byte[] Bytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static async Task<int> Bodies(DesktopSession session) => await session.Host.Session.Read((document, _) => document.Bodies.Count);

    // Steps 1 and 2: a new part has no file and no folder in the top bar; "Save as..." asks for the save dialog of a project
    // with no default location, and the chosen path, with no extension, gives the file <name>.3de there, its name, a recent
    // project and the line "Saved as". A save onto a file that exists is refused, and that file stays.
    [Fact]
    public async Task Save_As_Puts_A_New_Part_In_The_Folder_That_The_Person_Chooses()
    {
        using var session = Started(Settings());
        var host = new HostedSession(session.Host);
        Assert.Null(host.FileName);
        using (var ui = new DesktopUi(string.Empty, Document.Millimetre))
            Assert.Equal(["3DEngine", "unsaved", "mm"], ui.TitleParts(NativeThreeDEngine.LineOf(session.Document, host.FileName, null, Projects)));
        await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1200, SizeY = 18, SizeZ = 370 });
        var dialogs = new Dialogs();
        var projects = new ProjectFiles(host, session.Document, dialogs);

        Assert.Equal(default, projects.SaveAs());
        Assert.Equal([(ProjectFiles.ProjectFilter, "3de", DialogPurpose.SaveProject, (string?)null, true)], dialogs.Asked);
        var outcome = projects.Take(Path.Combine(Projects, "EntryHallShelf"), null, DialogPurpose.SaveProject);

        var file = Path.Combine(Projects, "EntryHallShelf.3de");
        Assert.Equal("Saved as EntryHallShelf.3de", outcome.Line);
        Assert.True(File.Exists(file));
        Assert.Equal("EntryHallShelf.3de", host.FileName);
        Assert.Equal([Path.GetFullPath(file)], session.Document.Recent(8));
        var line = NativeThreeDEngine.LineOf(session.Document, host.FileName, null, Path.GetDirectoryName(session.Host.Session.FilePath));
        Assert.Equal(Path.GetFullPath(Projects), line.Folder);

        // ADR-0102 item 3: the session does not replace a file that exists.
        var other = Path.Combine(Projects, "Other.3de");
        File.WriteAllText(other, "kept");
        Assert.Equal(DiagnosticCodes.IoSaveFailed, projects.Take(other, null, DialogPurpose.SaveProject).Refusal?.Code);
        Assert.Equal("kept", File.ReadAllText(other));
        Assert.Equal("EntryHallShelf.3de", host.FileName);
    }

    // Steps 3 to 6: after a start again with no file, the home screen holds the project; "Open project..." asks for the
    // dialog of a project, and its result opens the project for the next session; a change in that session is in the file
    // after a start again, with no save.
    [Fact]
    public async Task A_Start_Again_Shows_The_Project_And_A_Change_Stays_With_No_Save()
    {
        var file = Path.Combine(Projects, "EntryHallShelf.3de");
        using (var first = Started(Settings(file)))
            await first.Host.Session.Apply(new CreateBoxCommand { SizeX = 1200, SizeY = 18, SizeZ = 370 });

        using (var again = Started(Settings()))
        {
            Assert.Equal([Path.GetFullPath(file)], again.Document.Recent(8));
            var dialogs = new Dialogs();
            var projects = new ProjectFiles(new HostedSession(again.Host), again.Document, dialogs);
            Assert.Equal(default, projects.Change(projects.OpenProject));
            Assert.Equal([(ProjectFiles.ProjectFilter, "3de", DialogPurpose.OpenProject, (string?)null, false)], dialogs.Asked);

            Assert.Equal("Opening EntryHallShelf.3de", projects.Take(file, null, DialogPurpose.OpenProject).Line);
            Assert.Equal(file, again.Document.RequestedOpen);
        }

        using (var opened = Started(Settings(file)))
        {
            Assert.Equal(1, await Bodies(opened));
            await opened.Host.Session.Apply(new CreateBoxCommand { SizeX = 400, SizeY = 18, SizeZ = 370 });
        }

        using var last = Started(Settings(file));
        Assert.Equal(2, await Bodies(last));
    }

    // Step 7: "Export STL..." asks for the save dialog of an STL file at the name of the project; the export writes <name>.stl
    // in the chosen folder, says "Exported", and the file of the project keeps its bytes.
    [Fact]
    public async Task Export_Writes_An_Stl_File_And_The_Project_Keeps_Its_Bytes()
    {
        var file = Path.Combine(Projects, "EntryHallShelf.3de");
        using var session = Started(Settings(file));
        await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1200, SizeY = 18, SizeZ = 370 });
        var before = Bytes(file);
        var dialogs = new Dialogs();
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, dialogs);

        Assert.Equal(default, projects.Export());
        Assert.Equal([(ProjectFiles.StlFilter, "stl", DialogPurpose.ExportStl, (string?)Path.ChangeExtension(Path.GetFullPath(file), ".stl"), true)], dialogs.Asked);
        var exported = Path.Combine(_folder, "export");
        var outcome = projects.Take(exported, null, DialogPurpose.ExportStl);

        Assert.Equal("Exported 12 triangles to export.stl", outcome.Line);
        Assert.True(File.Exists(exported + ".stl"));
        Assert.Equal(before, Bytes(file));
    }

    // Step 8: a dialog that the person closes changes nothing and shows nothing; a dialog that fails gives its reason; a
    // second dialog while one is open gives "A file dialog is open already".
    [Fact]
    public void Cancel_In_Each_Dialog_Changes_Nothing_And_Shows_No_Message()
    {
        using var session = Started(Settings());
        var dialogs = new Dialogs();
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, dialogs);

        foreach (var purpose in new[] { DialogPurpose.OpenProject, DialogPurpose.SaveProject, DialogPurpose.ExportStl })
            Assert.Equal(default, projects.Take(null, null, purpose));
        Assert.Null(session.Document.RequestedOpen);
        Assert.Empty(Directory.GetFiles(Projects));
        Assert.Equal("The file dialog did not open: no portal", projects.Take(null, "no portal", DialogPurpose.OpenProject).Line);

        dialogs.Busy = true;
        Assert.Equal(ProjectFiles.Busy, projects.OpenProject().Line);
        Assert.Equal(ProjectFiles.Busy, projects.SaveAs().Line);
    }

    // Steps 9 and 10: a text file and an STL file with the extension .3de each give the code of an open that fails, and the
    // current document stays; "Import STL..." asks for the filter stl only, so it offers no project.
    [Fact]
    public void A_File_That_Is_Not_A_Project_Gives_Its_Code_And_The_Document_Stays()
    {
        using var session = Started(Settings());
        var dialogs = new Dialogs();
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, dialogs);
        var text = Path.Combine(Projects, "notes.3de");
        File.WriteAllText(text, "a shopping list");
        var stl = Path.Combine(Projects, "bracket.3de");
        File.WriteAllBytes(stl, new byte[84]);

        foreach (var path in new[] { text, stl })
        {
            var outcome = projects.Take(path, null, DialogPurpose.OpenProject);
            Assert.True(outcome.Refusal is { } refusal && refusal.Code is DiagnosticCodes.IoLoadFailed or DiagnosticCodes.IoBackendMismatch, $"{path}: {outcome.Line}");
            Assert.Null(session.Document.RequestedOpen);
        }

        projects.Change(projects.ImportStl);
        Assert.Equal([(ProjectFiles.StlFilter, "stl", DialogPurpose.ImportStl, (string?)null, false)], dialogs.Asked);
    }

    // Step 11 (Q3 A): while the document has no file and holds a command, a change of project asks first. "Cancel" drops the
    // change; "Discard" makes it; "Save as..." asks for the save dialog, and the change follows the save. With no command, or
    // with a file, no question comes.
    [Fact]
    public async Task A_Change_Of_Project_Asks_While_The_Document_Has_No_File()
    {
        using var session = Started(Settings());
        var dialogs = new Dialogs();
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, dialogs);
        var changes = 0;
        FileOutcome Change()
        {
            changes++;
            return new FileOutcome("changed");
        }

        Assert.Equal("changed", projects.Change(Change).Line);
        await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });

        Assert.Equal(default, projects.Change(Change));
        Assert.True(projects.Asking);
        Assert.Equal(default, projects.Answer(ChangeAnswer.Cancel));
        Assert.False(projects.Asking);
        Assert.Equal(1, changes);

        projects.Change(Change);
        Assert.Equal("changed", projects.Answer(ChangeAnswer.Discard).Line);
        Assert.Equal(2, changes);

        projects.Change(Change);
        Assert.Equal(default, projects.Answer(ChangeAnswer.SaveAs));
        Assert.Equal(DialogPurpose.SaveProject, dialogs.Asked[^1].Purpose);
        Assert.Equal(2, changes);
        Assert.Equal("changed", projects.Take(Path.Combine(Projects, "plate.3de"), null, DialogPurpose.SaveProject).Line);
        Assert.Equal(3, changes);

        Assert.Equal("changed", projects.Change(Change).Line);
        Assert.False(projects.Asking);
    }

    // Step 11: a save dialog that the person closes after "Save as..." drops the change.
    [Fact]
    public async Task A_Closed_Save_Dialog_After_The_Question_Drops_The_Change()
    {
        using var session = Started(Settings());
        await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, new Dialogs());
        var changes = 0;

        projects.Change(() => new FileOutcome($"changed {++changes}"));
        projects.Answer(ChangeAnswer.SaveAs);
        projects.Take(null, null, DialogPurpose.SaveProject);
        projects.Take(Path.Combine(Projects, "late.3de"), null, DialogPurpose.SaveProject);

        Assert.Equal(0, changes);
    }

    // Finding V155 of the codebase review of 2026-11-09 (TASK-0324): a save dialog that the panel or the command line opened
    // while the question is open keeps the waiting change and the question. A closed dialog drops nothing, so "Discard" then
    // makes the change; a save saves only, and "Discard" then makes the change.
    [Fact]
    public async Task A_Save_From_The_Panel_Keeps_The_Waiting_Change_And_The_Question()
    {
        using var session = Started(Settings());
        await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        var projects = new ProjectFiles(new HostedSession(session.Host), session.Document, new Dialogs());
        var changes = 0;
        FileOutcome Change() => new($"changed {++changes}");

        projects.Change(Change);
        Assert.Equal(default, projects.SaveAs());
        Assert.Equal(default, projects.Take(null, null, DialogPurpose.SaveProject));
        Assert.True(projects.Asking);
        Assert.Equal("changed 1", projects.Answer(ChangeAnswer.Discard).Line);

        projects.Change(Change);
        Assert.True(projects.Asking);
        Assert.Equal(default, projects.SaveAs());
        Assert.Equal("Saved as panel.3de", projects.Take(Path.Combine(Projects, "panel.3de"), null, DialogPurpose.SaveProject).Line);
        Assert.Equal(1, changes);
        Assert.True(projects.Asking);
        Assert.Equal("changed 2", projects.Answer(ChangeAnswer.Discard).Line);
    }

    // Finding E197 of the codebase review of 2026-11-09 (TASK-0324): a save of the surface, POST /document/save, puts the file
    // into the recent projects, as a save from the window does (ADR-0102 item 6).
    [Fact]
    public async Task A_Save_Of_The_Surface_Goes_Into_The_Recent_Projects()
    {
        var (surface, code, message) = DesktopSurface.Start(Arguments());
        Assert.True(surface is not null, $"{code}: {message}");
        using (surface.Session)
        using (surface)
        {
            await surface.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
            Assert.Empty(surface.Document.Recent(8));
            using var client = new HttpClient { BaseAddress = new Uri(surface.Address) };
            using var body = new StringContent("{\"name\":\"agent\"}", System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/document/save", body);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal([Path.GetFullPath(Path.Combine(_folder, "documents", "agent.3de"))], surface.Document.Recent(8));
        }
    }

    // ADR-0102 item 6 (Q2 A): the list keeps eight paths, newest first, one time each with no regard to the case on Windows
    // and macOS; a path that no longer exists leaves the list.
    [Fact]
    public void The_Recent_Projects_Keep_Eight_Paths_That_Exist()
    {
        var recent = new RecentProjects(Path.Combine(_folder, RecentProjects.FileName));
        var paths = Enumerable.Range(0, 10).Select(i => Path.Combine(Projects, $"p{i}.3de")).ToList();
        foreach (var path in paths)
        {
            File.WriteAllText(path, string.Empty);
            recent.Remember(path);
        }

        recent.Remember(paths[5]);
        Assert.Equal([paths[5], paths[9], paths[8], paths[7], paths[6], paths[4], paths[3], paths[2]], recent.Paths(8));
        File.Delete(paths[9]);
        Assert.Equal([paths[5], paths[8], paths[7]], recent.Paths(3));
        Assert.Equal(7, File.ReadAllLines(recent.File).Length);
        Assert.Equal(Path.Combine(_folder, RecentProjects.FileName), RecentProjects.Beside(Path.Combine(_folder, "unsaved")));
    }

    // ADR-0102 item 7: the top bar shows the folder after the name at 1440 x 900, and no folder at 640 x 360, where the
    // room left of the command line is less than 48 pixels.
    [Fact]
    public void The_Top_Bar_Shows_The_Folder_Of_The_Project()
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var folder = Path.Combine("C:", "Users", "Henri", "Documents", "3DProjects", "EntryHallShelf");
        var line = new DocumentLine("EntryHallShelf.3de", null, Files: true, Folder: folder);
        foreach (var display in new[] { new Vector2(1440, 900), new Vector2(640, 360) })
        {
            for (var frame = 0; frame < 2; frame++)
            {
                ui.Route([]);
                ui.Frame(1f / 60f, display, Vector2.One, [], null, line);
            }

            var shown = ui.ItemRectangle(DesktopUi.TitleFolderItem);
            if (display.X == 1440)
            {
                var title = ui.ItemRectangle(DesktopUi.TitleItem)!.Value;
                var command = ui.ItemRectangle(DesktopUi.CommandItem)!.Value;
                Assert.True(shown is { } at && at.Z <= command.X - 12f + 0.5f && title.Z <= command.X - 12f + 0.5f, $"The folder at {shown} is over the command line at {command.X}.");
            }
            else
            {
                Assert.Null(shown);
            }
        }
    }

    // Finding V154 of the codebase review of 2026-11-09 (TASK-0324): at 1440 x 900 the folded window "Images" starts under the
    // top bar, so the folder of the top bar does not run under it.
    [Fact]
    public void The_Folder_Of_The_Top_Bar_Is_Not_Under_The_Window_Of_The_Images()
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var folder = Path.Combine("C:", "Users", "Henri", "Documents", "3DProjects", "EntryHallShelf");
        var line = new DocumentLine("EntryHallShelf.3de", null, Files: true, Folder: folder);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, new Vector2(1440, 900), Vector2.One, [], null, line, imagePanel: new ImagePanel([], null, 0, null));
        }

        var shown = ui.ItemRectangle(DesktopUi.TitleFolderItem) ?? throw new InvalidOperationException("No folder in the top bar.");
        var images = ui.PanelRectangles.Single(window => Math.Abs(window.X - 492f) < 1f);
        var apart = shown.Z <= images.X || shown.X >= images.Z || shown.W <= images.Y || shown.Y >= images.W;
        Assert.True(apart, $"The folder at {shown} is under the window of the images at {images}.");
    }

    // Finding V156 of the codebase review of 2026-11-09 (TASK-0324): a portrait display has no command line, so the top bar
    // keeps its width: at 600 x 900 the folder shows after the name, inside the display.
    [Fact]
    public void A_Portrait_Display_Gives_The_Top_Bar_The_Room_Of_The_Command_Line()
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var display = new Vector2(600, 900);
        var folder = Path.Combine("C:", "Users", "Henri", "Documents", "3DProjects", "EntryHallShelf");
        var line = new DocumentLine("EntryHallShelf.3de", null, Files: true, Folder: folder);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [], null, line);
        }

        Assert.Null(ui.ItemRectangle(DesktopUi.CommandItem));
        var shown = ui.ItemRectangle(DesktopUi.TitleFolderItem);
        Assert.True(shown is { } at && at.Z <= display.X - 16f - 12f + 0.5f, $"The folder at {shown} does not show inside {display}.");
    }

    // ADR-0102 item 1: the home screen of a host with no files (the phone) shows "Import STL..." and no project.
    [Fact]
    public void A_Host_With_No_Files_Shows_No_Project_On_The_Home_Screen()
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        ui.ShowHome([], projects: false);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, new Vector2(390, 844), Vector2.One, []);
        }

        Assert.NotNull(ui.ItemRectangle(DesktopUi.ImportStlLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.OpenProjectLabel));
    }

    // ADR-0102 items 1 and 5: the command line holds the four controls of a file while the host has files, and the question
    // gives its answer as an action.
    [Fact]
    public void The_Command_Line_And_The_Question_Give_Their_Actions()
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        ui.ShowQuestion(true);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, new Vector2(1440, 900), Vector2.One, [], null, new DocumentLine(null, null));
        }

        var names = ui.Commands([], null).Select(command => command.Name).ToList();
        Assert.Contains(DesktopUi.OpenProjectLabel, names);
        Assert.Contains(DesktopUi.SaveAsLabel, names);
        Assert.Contains(DesktopUi.ExportStlLabel, names);
        Assert.Contains(DesktopUi.ImportStlLabel, names);

        var discard = ui.ItemRectangle($"{DesktopUi.QuestionItem}:{DesktopUi.DiscardLabel}") ?? throw new InvalidOperationException("No question.");
        var (x, y) = ((discard.X + discard.Z) / 2, (discard.Y + discard.W) / 2);
        foreach (var phase in new[] { Core.Input.PointerPhase.Down, Core.Input.PointerPhase.Up })
        {
            ui.Route([new Core.Input.PointerSample(Core.Input.DeviceKind.Touch, 1, 3, x, y, 1, Core.Input.PointerButtons.None, Core.Input.KeyModifiers.None, phase, 0)]);
            ui.Frame(1f / 60f, new Vector2(1440, 900), Vector2.One, [], null, new DocumentLine(null, null));
        }

        ui.Route([]);
        ui.Frame(1f / 60f, new Vector2(1440, 900), Vector2.One, [], null, new DocumentLine(null, null));
        Assert.Equal([new AnswerAction(ChangeAnswer.Discard)], ui.TakeActions());
        Assert.False(ui.QuestionShown);
    }
}
