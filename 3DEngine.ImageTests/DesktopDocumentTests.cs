using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0030 items 1, 3 and 5 (TASK-0092): the document part of the desktop. The panel
// gives a save and an open of the typed name, an open checks the file before the
// program starts again, and a file that does not open stops the desktop with code 6.
[Collection(DearImGui.Collection)]
public sealed class DesktopDocumentTests : IDisposable
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly LiveBody[] Bodies = [new(Guid.Parse("11111111-0000-0000-0000-000000000000"), "Box", null, true)];

    private static readonly DocumentLine Line = new("plate.3de", null);

    private readonly string _folder = Directory.CreateTempSubdirectory("3de-desktop-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static void Step(DesktopUi ui, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies, null, Line);
    }

    private static DesktopUi Opened()
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui);
        Step(ui);
        return ui;
    }

    private static (double X, double Y) Centre(DesktopUi ui, string label)
    {
        var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static void Tap(DesktopUi ui, DeviceKind kind, (double X, double Y) at)
    {
        var (device, pointer, buttons) = kind == DeviceKind.Mouse ? (1UL, 0UL, PointerButtons.Left) : (1UL, 3UL, PointerButtons.None);
        Step(ui, new PointerSample(kind, device, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, new PointerSample(kind, device, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui);
        Step(ui);
    }

    // ADR-0102 item 1 (phase P9g, TASK-0316): the four controls of a file each give the action of their dialog. Until
    // TASK-0316 a field took a name, and Save as and Open used it in the document folder.
    [Fact]
    public void The_File_Controls_Give_The_Actions_Of_Their_Dialogs()
    {
        using var ui = Opened();

        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.OpenProjectLabel));
        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.SaveAsLabel));
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.ExportStlLabel));
        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.ImportStlLabel));

        Assert.Equal([new OpenProjectAction(), new SaveAsAction(), new ExportStlAction(), new ImportStlAction()], ui.TakeActions());
    }

    // ADR-0031 item 11 (TASK-0093): a tap and a click give an undo and a redo.
    [Fact]
    public void The_Undo_And_Redo_Buttons_Give_Their_Actions_By_Touch_And_By_Mouse()
    {
        using var ui = Opened();

        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.UndoLabel));
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.RedoLabel));
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.UndoLabel));

        Assert.Equal([new UndoAction(), new RedoAction(), new UndoAction()], ui.TakeActions());
    }

    // ADR-0027 item 6 for the controls of ADR-0030 item 3 and ADR-0031 item 11.
    [Fact]
    public void Each_File_Control_Is_At_Least_44_Pixels_High()
    {
        using var ui = Opened();

        foreach (var label in new[] { DesktopUi.OpenProjectLabel, DesktopUi.SaveAsLabel, DesktopUi.ExportStlLabel, DesktopUi.ImportStlLabel, DesktopUi.UndoLabel, DesktopUi.RedoLabel })
        {
            var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{label} is {rectangle.W - rectangle.Y} pixels high.");
        }
    }

    [Fact]
    public void A_Panel_With_No_Document_Line_Has_No_File_Controls()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);

        Assert.Null(ui.ItemRectangle(DesktopUi.SaveAsLabel));
    }

    // ADR-0030 item 5: a file that does not open leaves the window; a file that opens
    // ends the window loop.
    [Fact]
    public async Task An_Open_Checks_The_File_Before_The_Program_Starts_Again()
    {
        var good = Path.Combine(_folder, "good.3de");
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = new DocumentSession(EngineHosting.CreateDefault(backend));
            DocumentFile.Create(session, good);
            await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
            session.Close();
        }

        var bad = Path.Combine(_folder, "bad.3de");
        File.WriteAllText(bad, "{\"format\":\"another\"}\n");
        var document = new DesktopDocument(_folder);

        var refused = await document.Folder.Reopen!(bad, CancellationToken.None);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, refused!.Code);
        Assert.Null(document.RequestedOpen);

        Assert.Null(await document.Folder.Reopen!(good, CancellationToken.None));
        Assert.Equal(good, document.RequestedOpen);
    }

    // Finding E167 of the codebase review of 2026-11-07 (TASK-0262): the check of an open reads the asset of an import from the
    // store of the desktop, so a saved document with a mesh body opens; with no store the check refuses it with the code of
    // the import, which was the result for each such document until TASK-0262.
    [Fact]
    public async Task The_Check_Of_An_Open_Reads_The_Asset_Of_An_Import_From_The_Store()
    {
        var part = Path.Combine(_folder, "bracket.3de");
        var store = new Engine.Core.Assets.AssetStore(Path.Combine(_folder, "assets"));
        using (var backend = new ManifoldGeometryBackend())
        {
            var stl = await Engine.Core.Determinism.CrossPlatformProbe.BracketStl(backend);
            var (asset, _, _) = await store.Put(new MemoryStream(stl));
            var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: store));
            DocumentFile.Create(session, part);
            Assert.Equal(Engine.Contracts.CommandStatus.Applied, (await session.Apply(new ImportStlCommand { Asset = asset!, BodyName = "bracket.stl" })).Status);
            session.Close();
        }

        var withStore = new DesktopDocument(_folder, store);
        var withNone = new DesktopDocument(_folder);

        Assert.Null(await withStore.Folder.Reopen!(part, CancellationToken.None));
        Assert.Equal(part, withStore.RequestedOpen);
        var refused = await withNone.Folder.Reopen!(part, CancellationToken.None);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, refused?.Code);
        Assert.Contains(DiagnosticCodes.StlAsset, refused!.Message, StringComparison.Ordinal);
    }

    // V31 of the second codebase review of 2026-10-28: the check holds the file as a journal
    // does, so a file that another desktop holds fails it and the person keeps the window. The
    // file that this desktop holds passes, because the old process frees it before the start.
    // Only Windows refuses the hold: on Linux and macOS .NET takes no lock for a write with the
    // share of a read (ADR-0030, Consequences).
    [Fact]
    public async Task The_Check_Refuses_A_File_That_Another_Desktop_Holds()
    {
        var path = Path.Combine(_folder, "held.3de");
        using var backend = new ManifoldGeometryBackend();
        var holder = new DocumentSession(EngineHosting.CreateDefault(backend));
        DocumentFile.Create(holder, path);
        await holder.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        try
        {
            var other = new DesktopDocument(_folder);
            var refused = await other.Folder.Open(path, CancellationToken.None);
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(DiagnosticCodes.IoLoadFailed, refused!.Code);
                Assert.Null(other.RequestedOpen);
                Assert.False(other.Folder.OpenAccepted);
            }

            var own = new DesktopDocument(_folder) { HeldFile = () => holder.FilePath };
            Assert.Null(await own.Folder.Open(path, CancellationToken.None));
            Assert.Equal(path, own.RequestedOpen);
            Assert.True(own.Folder.OpenAccepted);
        }
        finally
        {
            holder.Close();
        }
    }

    // ADR-0030 item 1: the check comes before the window, so it runs on each runner.
    [Fact]
    public async Task A_Desktop_Whose_File_Does_Not_Open_Stops_With_Exit_Code_6()
    {
        var bad = Path.Combine(_folder, "bad.3de");
        File.WriteAllText(bad, "{\"format\":\"another\"}\n");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        foreach (var argument in new[] { Path.Combine(AppContext.BaseDirectory, "3DEngine.dll"), "--port", "0", "--file", bad, "--Desktop:UnsavedFolder", _folder })
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The desktop did not exit within 60 seconds.");
        }

        Assert.True(process.ExitCode == DesktopSurface.ExitFileNotOpened, $"The exit code is {process.ExitCode}. The error output: {await error}");
        Assert.Contains(DiagnosticCodes.IoLoadFailed, await error, StringComparison.Ordinal);
        GC.KeepAlive(output);
    }
}
