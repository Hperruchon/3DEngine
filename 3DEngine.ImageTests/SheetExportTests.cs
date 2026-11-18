using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Export;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0170 (phase R30d, TASK-0412): "Export" of the sheet page and of the command line: the formats, the save dialog, the file
// of the engine written through SDL, and the texts. ADR-0170 names this file in its enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetExportTests : IDisposable
{
    private static readonly SheetPlacement.Box Plate = new((0, 0, 0), (60, 40, 10));
    private static readonly LiveBody Body = new(Guid.Parse("7e000000-0000-0000-0000-000000000001"), "Plate", "Extrude", true);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-export-" + Guid.NewGuid().ToString("N"));

    public SheetExportTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static async Task<(EngineHost Host, SheetRecord Sheet)> Setup()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        await host.Session.Apply(new CreateBoxCommand { SizeX = 60, SizeY = 40, SizeZ = 10 });
        new SheetActions(host.Session).Create("Sheet 1", "A3", true, (2, 1), SheetPlacement.Standard, Plate);
        return (host, await host.Session.Read((document, _) => document.Sheets.Single()));
    }

    // Item 1: PDF first, then SVG and DXF, each with its word, its filter and a purpose of its own that gives the format back.
    [Fact]
    public void The_Formats_Are_Pdf_Svg_And_Dxf()
    {
        Assert.Equal([SheetFormat.Pdf, SheetFormat.Svg, SheetFormat.Dxf], SheetExports.Formats);
        Assert.Equal(["PDF", "SVG", "DXF"], SheetExports.Formats.Select(SheetExports.Label));
        Assert.Equal(["pdf", "svg", "dxf"], SheetExports.Formats.Select(format => SheetExports.Filter(format).Pattern));
        Assert.Equal(3, SheetExports.Formats.Select(SheetExports.Purpose).Distinct().Count());
        foreach (var format in SheetExports.Formats)
            Assert.Equal(format, SheetExports.FormatOf(SheetExports.Purpose(format)));
        foreach (var other in new[] { DialogPurpose.ImportStl, DialogPurpose.OpenProject, DialogPurpose.SaveProject, DialogPurpose.ExportStl })
            Assert.Null(SheetExports.FormatOf(other));
        Assert.Equal(["Export PDF", "Export SVG", "Export DXF"], SheetExports.Formats.Select(DesktopUi.ExportLabel));
    }

    // Item 2: the dialog starts beside the project at "<project> <sheet>.<format>", with "_" for a character that a file name does
    // not permit; a document with no file lets the system choose.
    [Fact]
    public void The_Dialog_Starts_Beside_The_Project()
    {
        var project = Path.Combine(_folder, "plate.3de");
        Assert.Equal(Path.Combine(_folder, "plate Sheet 1.pdf"), SheetExports.Location(project, "Sheet 1", SheetFormat.Pdf));
        Assert.Equal(Path.Combine(_folder, "plate A_B_.dxf"), SheetExports.Location(project, "A/B?", SheetFormat.Dxf));
        Assert.Null(SheetExports.Location(null, "Sheet 1", SheetFormat.Svg));
    }

    // Item 3: each format writes the bytes of the engine (the file of the HTTP route and of the command line) at the path with its
    // extension, and the text names the sheet, the format, the file and the size. A content URI keeps its name.
    [Theory]
    [InlineData("Pdf")]
    [InlineData("Svg")]
    [InlineData("Dxf")]
    public async Task Each_Format_Writes_The_File_Of_The_Engine(string name)
    {
        var format = Enum.Parse<SheetFormat>(name);
        var (host, sheet) = await Setup();
        using var _ = host;
        var expected = await host.Session.Read((document, _) => format switch
        {
            SheetFormat.Pdf => PdfExport.Write(document, sheet.Id).Content,
            SheetFormat.Svg => SvgExport.Write(document, sheet.Id).Content,
            _ => DxfExport.Write(document, sheet.Id).Content,
        });
        var written = new List<(string Path, byte[] Content)>();
        string? Save(string path, byte[] content)
        {
            written.Add((path, content));
            return null;
        }

        var text = SheetExports.Write(host.Session, sheet.Id, format, Path.Combine(_folder, "out"), Save);

        var pattern = SheetExports.Filter(format).Pattern;
        Assert.Equal($"Exported Sheet 1 as {SheetExports.Label(format)} to out.{pattern}, {expected!.Length} bytes.", text);
        var (path, content) = Assert.Single(written);
        Assert.Equal(Path.Combine(_folder, $"out.{pattern}"), path);
        Assert.Equal(expected, content);

        SheetExports.Write(host.Session, sheet.Id, format, "content://com.android.providers.downloads.documents/document/42", Save);
        Assert.Equal("content://com.android.providers.downloads.documents/document/42", written[^1].Path);
    }

    // Item 3: a sheet that is not live gives the code of the engine and writes nothing; a write that fails gives
    // E-EXPORT-WRITE-FAILED with the reason.
    [Fact]
    public async Task A_Failure_Gives_Its_Code_And_Its_Reason()
    {
        var (host, sheet) = await Setup();
        using var _ = host;
        var calls = 0;

        var missing = SheetExports.Write(host.Session, Guid.NewGuid(), SheetFormat.Pdf, Path.Combine(_folder, "x.pdf"), (_, _) => { calls++; return null; });
        var refused = SheetExports.Write(host.Session, sheet.Id, SheetFormat.Pdf, Path.Combine(_folder, "x.pdf"), (_, _) => "The disk is full.");

        Assert.StartsWith($"{DiagnosticCodes.SheetNotFound}: ", missing);
        Assert.Equal(0, calls);
        Assert.Equal($"{DiagnosticCodes.ExportWriteFailed}: The disk is full.", refused);
    }

    // Item 3: SDL writes the bytes at the path, and a path in a folder that does not exist gives the text of SDL.
    [Fact]
    public void Sdl_Writes_The_Bytes()
    {
        var path = Path.Combine(_folder, "sdl.bin");
        byte[] content = [0x25, 0x50, 0x44, 0x46, 0, 255, 10, 13];

        Assert.Null(Window.WriteFile(path, content));
        Assert.Equal(content, File.ReadAllBytes(path));
        Assert.Null(Window.WriteFile(path, []));
        Assert.Empty(File.ReadAllBytes(path));
        Assert.False(string.IsNullOrEmpty(Window.WriteFile(Path.Combine(_folder, "no", "such", "folder", "x.pdf"), content)));
    }

    private static void Frame(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record, params InputSample[] samples)
    {
        ui.ShowSheets([record]);
        ui.ShowPage(sheet, false);
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    private static void Tap(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record, Vector4 target)
    {
        var at = new Vector2((target.X + target.Z) / 2f, (target.Y + target.W) / 2f);
        Frame(ui, display, sheet, record, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 50_000_000));
        Frame(ui, display, sheet, record, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 100_000_000));
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // Item 1: "Export" on the page opens the row of the formats, PDF first, each 44 pixels high; a format gives its action and
    // closes the row; Escape closes the row before the page. On a phone the row fits the display.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public async Task Export_On_The_Page_Gives_A_Format(int width, int height)
    {
        var (host, record) = await Setup();
        using var _ = host;
        var sheet = (await host.Session.Query<SheetInfo>(new GetSheetQuery { SheetId = record.Id })).Result!;
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();

        Tap(ui, display, sheet, record, Item(ui, DesktopUi.PageItem(DesktopUi.ExportSheetLabel)));
        Assert.True(ui.ExportOpen);
        var boxes = SheetExports.Formats.Select(format => Item(ui, DesktopUi.PageItem(DesktopUi.ExportLabel(format)))).ToList();
        Assert.All(boxes, box => Assert.True(box.X >= 0 && box.Z <= width && box.W - box.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"{width}x{height}: {box} is out of the display or low."));
        Assert.True(boxes[0].X < boxes[1].X || boxes[0].Y < boxes[1].Y, "PDF is not first.");

        Tap(ui, display, sheet, record, boxes[1]);
        Assert.Equal([new SheetExportAction(record.Id, SheetFormat.Svg)], ui.TakeActions());
        Assert.False(ui.ExportOpen);

        Tap(ui, display, sheet, record, Item(ui, DesktopUi.PageItem(DesktopUi.ExportSheetLabel)));
        Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, true, KeyModifiers.None, 0));
        Assert.False(ui.ExportOpen);
        Assert.True(ui.PageOpen);
    }

    // Item 1: the command line has "Export PDF", "Export SVG" and "Export DXF" with a live sheet, and none with no sheet.
    [Fact]
    public async Task The_Command_Line_Has_The_Words_Of_The_Export()
    {
        var (host, record) = await Setup();
        using var _ = host;
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Assert.DoesNotContain(ui.Commands([Body], null), command => command.Name.StartsWith("Export ", StringComparison.Ordinal) && command.Name != DesktopUi.ExportStlLabel);

        ui.ShowSheets([record]);
        var commands = ui.Commands([Body], null);
        ui.TakeActions();
        foreach (var format in SheetExports.Formats)
            commands.Single(command => command.Name == DesktopUi.ExportLabel(format)).Run();
        Assert.Equal(SheetExports.Formats.Select(format => (PanelAction)new SheetExportAction(record.Id, format)), ui.TakeActions());
    }
}
