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
        var ui = new DesktopUi("http://127.0.0.1:5180");
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

    [Fact]
    public void The_File_Controls_Give_A_Save_And_An_Open_Of_The_Typed_Name()
    {
        using var ui = Opened();

        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.FileNameLabel));
        Step(ui, new TextSample("plate", 0));
        Step(ui);
        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.SaveAsLabel));
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.OpenLabel));

        Assert.Equal([new SaveDocumentAction("plate"), new OpenDocumentAction("plate")], ui.TakeActions());
    }

    // ADR-0027 item 6 for the controls of ADR-0030 item 3.
    [Fact]
    public void Each_File_Control_Is_At_Least_44_Pixels_High()
    {
        using var ui = Opened();

        foreach (var label in new[] { DesktopUi.FileNameLabel, DesktopUi.SaveAsLabel, DesktopUi.OpenLabel })
        {
            var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{label} is {rectangle.W - rectangle.Y} pixels high.");
        }
    }

    [Fact]
    public void A_Panel_With_No_Document_Line_Has_No_File_Controls()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180");
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);

        Assert.Null(ui.ItemRectangle(DesktopUi.SaveAsLabel));
    }

    // ADR-0030 item 4: the program starts again with the same arguments and the new file.
    [Theory]
    [InlineData(new[] { "--port", "5181" }, new[] { "--port", "5181", "--file", "b.3de" })]
    [InlineData(new[] { "--file", "a.3de", "--port", "0" }, new[] { "--port", "0", "--file", "b.3de" })]
    [InlineData(new[] { "--file=a.3de", "--Desktop:SynchronizationValidation=true" }, new[] { "--Desktop:SynchronizationValidation=true", "--file", "b.3de" })]
    public void The_Program_Starts_Again_With_Its_Arguments_And_The_New_File(string[] args, string[] expected)
    {
        Assert.Equal(expected, DesktopDocument.Arguments(args, "b.3de"));
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
