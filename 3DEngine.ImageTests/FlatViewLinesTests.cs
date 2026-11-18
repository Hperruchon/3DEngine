using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0166 (phase R30a, TASK-0406): the lines of the sheet in a flat view of the window, and the switch "Lines" in the net of
// the views and on the key L. ADR-0166 names this file in its enforced-by field. The tests with no GPU check the frame, the
// strokes, the dashes, the read off the frame thread and the switch; the picture checks the paper, the ink and the dashes.
[Collection(DearImGui.Collection)]
public sealed class FlatViewLinesTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    private static readonly LiveBody Body = new(Guid.Parse("7b000000-0000-0000-0000-000000000001"), "Plate", "Extrude", true);

    // Item 2: the frame of each flat view of the camera of the session is the frame of the standard view of the engine
    // (ADR-0142 item 2), nine whole numbers; a view with perspective has none.
    [Theory]
    [InlineData(FlatView.Front, new[] { 1.0, 0, 0, 0, 0, 1, 0, -1, 0 })]
    [InlineData(FlatView.Back, new[] { -1.0, 0, 0, 0, 0, 1, 0, 1, 0 })]
    [InlineData(FlatView.Right, new[] { 0.0, 1, 0, 0, 0, 1, 1, 0, 0 })]
    [InlineData(FlatView.Left, new[] { 0.0, -1, 0, 0, 0, 1, -1, 0, 0 })]
    [InlineData(FlatView.Top, new[] { 1.0, 0, 0, 0, 1, 0, 0, 0, 1 })]
    [InlineData(FlatView.Bottom, new[] { 1.0, 0, 0, 0, -1, 0, 0, 0, -1 })]
    public void The_Frame_Of_A_Flat_View_Is_The_Frame_Of_The_Engine(FlatView view, double[] frame)
    {
        var camera = SessionScene.Create().Cameras[0];
        Assert.Null(ViewLines.FrameOf(camera));
        camera.ShowFlat(view);
        Assert.Equal(frame, ViewLines.FrameOf(camera));
        Assert.All(ViewLines.FrameOf(camera)!, value => Assert.False(double.IsNegative(value) && value == 0, "A frame has -0."));
    }

    // Item 2: an arc goes counter-clockwise on the view from its start to its end, also on an ellipse whose minor half axis
    // turns the other way; a start equal to the end is the whole curve. Each point is on the curve.
    [Fact]
    public void An_Arc_Goes_Counter_Clockwise_From_Its_Start_To_Its_End()
    {
        var quarter = ViewLines.Path(new ViewLineInfo(Guid.Empty, 5, 0, 0, 5, true, "edge") { Circle = new ViewCircleInfo(0, 0, 5) });
        Assert.Equal((5.0, 0.0), quarter[0]);
        Assert.Equal((0.0, 5.0), quarter[^1]);
        Assert.All(quarter, point => Assert.True(point.U >= -1e-9 && point.V >= -1e-9, $"{point} is not in the first quarter."));
        Assert.All(quarter, point => Assert.Equal(5, Math.Sqrt((point.U * point.U) + (point.V * point.V)), 9));

        var whole = ViewLines.Path(new ViewLineInfo(Guid.Empty, 5, 0, 5, 0, true, "edge") { Circle = new ViewCircleInfo(0, 0, 5) });
        Assert.Equal(ViewLines.WholeCurveParts + 1, whole.Count);
        Assert.Contains(whole, point => point.U < -4.99);

        // Major (4, 0) and minor (0, -2): a growing angle turns clockwise, so the arc from (4, 0) to (0, 2) counter-clockwise is a
        // quarter through the upper right.
        var ellipse = ViewLines.Path(new ViewLineInfo(Guid.Empty, 4, 0, 0, 2, false, "edge") { Ellipse = new ViewEllipseInfo(0, 0, 4, 0, 0, -2) });
        Assert.Equal((0.0, 2.0), ellipse[^1]);
        Assert.True(ellipse.Count < (ViewLines.WholeCurveParts / 2) + 2, $"The arc has {ellipse.Count} points, more than a quarter.");
        Assert.All(ellipse, point => Assert.True(point.U >= -1e-9 && point.V >= -1e-9, $"{point} is not in the first quarter."));
        Assert.All(ellipse, point => Assert.Equal(1, ((point.U / 4) * (point.U / 4)) + ((point.V / 2) * (point.V / 2)), 9));
    }

    // Item 3: the dashes of a hidden line go along the whole path, across a corner.
    [Fact]
    public void The_Dashes_Go_On_Across_A_Corner()
    {
        var dashes = ViewLines.Dashes([new Vector2(0, 0), new Vector2(8, 0), new Vector2(8, 12)], 6f, 4f);

        Assert.Equal([(new Vector2(0, 0), new Vector2(6, 0)), (new Vector2(8, 2), new Vector2(8, 8))], dashes);
    }

    // Item 2: one read at a time, off the frame thread; the last lines stay until the new lines come, and the cache shows
    // that a read runs.
    [Fact]
    public void The_Cache_Keeps_The_Last_Lines_Until_The_New_Lines_Come()
    {
        var cache = new ViewLinesCache();
        double[] front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
        var first = new TaskCompletionSource<ViewLinesInfo?>();
        var second = new TaskCompletionSource<ViewLinesInfo?>();
        var reads = new Queue<TaskCompletionSource<ViewLinesInfo?>>([first, second]);
        Task<ViewLinesInfo?> Read() => reads.Dequeue().Task;

        Assert.Equal((null, true), cache.Lines(1, front, Read));
        Assert.Equal((null, true), cache.Lines(2, front, Read));
        Assert.Equal(1, cache.Computed);

        var old = new ViewLinesInfo(front, [], []);
        first.SetResult(old);
        var (lines, pending) = cache.Lines(2, front, Read);
        Assert.Same(old, lines);
        Assert.True(pending);
        Assert.Equal(2, cache.Computed);

        var fresh = new ViewLinesInfo(front, [], []);
        second.SetResult(fresh);
        Assert.Equal((fresh, false), cache.Lines(2, front, Read));
        Assert.Equal((fresh, false), cache.Lines(2, front, Read));
        Assert.Equal(2, cache.Computed);
    }

    private static void Frame(DesktopUi ui, Vector2 display, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    private static void Tap(DesktopUi ui, Vector2 display, Vector4 target)
    {
        var at = new Vector2((target.X + target.Z) / 2f, (target.Y + target.W) / 2f);
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 50_000_000));
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 100_000_000));
        Frame(ui, display);
        Frame(ui, display);
    }

    // Item 3: the net of the views has the switch "Lines" at its lower right, inside the display and as high as a cell; a tap
    // changes it and closes the net, and a second tap changes it back.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void The_Net_Has_The_Switch_Lines(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display);
        Frame(ui, display);
        Assert.False(ui.LinesOn);

        foreach (var expected in new[] { true, false })
        {
            Tap(ui, display, ui.ItemRectangle(DesktopUi.ViewsLabel) ?? throw new InvalidOperationException("No Views."));
            var cell = ui.ItemRectangle(DesktopUi.LinesItem) ?? throw new InvalidOperationException("No switch Lines.");
            var front = ui.ItemRectangle(DesktopUi.ViewItem(FlatView.Front)) ?? throw new InvalidOperationException("No Front.");
            Assert.True(cell.X >= 0f && cell.Z <= width && cell.Y >= 0f && cell.W <= height, $"{width}x{height}: Lines {cell} is out of the display.");
            Assert.Equal(front.W - front.Y, cell.W - cell.Y, 1);
            Assert.True(cell.X > front.Z && cell.Y > front.W, $"Lines {cell} is not at the lower right of Front {front}.");
            Tap(ui, display, cell);
            Assert.Equal(expected, ui.LinesOn);
            Assert.False(ui.ViewsOpen, "The net stays open after the switch.");
        }
    }

    // Item 3: L changes the switch; L with Control, L while the command line takes the keys, and L on the home screen do not.
    [Fact]
    public void The_Key_L_Changes_The_Switch_Unless_A_Field_Takes_The_Keys()
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display);
        KeySample Key(InputKey key, KeyModifiers modifiers = KeyModifiers.None) => new(key, true, modifiers, 0);

        Frame(ui, display, Key(InputKey.L), new KeySample(InputKey.L, false, KeyModifiers.None, 1));
        Assert.True(ui.LinesOn);
        Frame(ui, display, Key(InputKey.L, KeyModifiers.Control));
        Assert.True(ui.LinesOn);
        Frame(ui, display, Key(InputKey.L));
        Assert.False(ui.LinesOn);

        Frame(ui, display, Key(InputKey.K, KeyModifiers.Control), Key(InputKey.L));
        Frame(ui, display, Key(InputKey.L));
        Frame(ui, display);
        Frame(ui, display, Key(InputKey.L), new TextSample("l", 2));
        Assert.False(ui.LinesOn);

        using var home = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        home.ShowHome(["plate"]);
        Frame(home, display, Key(InputKey.L));
        Assert.False(home.LinesOn);
    }

    // Item 3: with lines, the paper lies on the free part of the scene of the last frame, right of the panel and above the
    // anchors, and the outline of a sketch does not show; with no lines there is no paper and the sketch shows.
    [Fact]
    public void The_Paper_Lies_On_The_Free_Part_And_Hides_The_Sketch()
    {
        var display = new Vector2(Width, Height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var sketch = new SketchOverlay.Strokes([new(new Vector2(700, 300), new Vector2(900, 300), SketchOverlay.Mark.Active)], [], [], []);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null), sketch);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null), sketch);
        Assert.Null(ui.PaperShown);
        Assert.True(ui.SketchesShown, "The sketch does not show with no lines.");

        ui.ShowLines([new ViewLines.Stroke([new Vector2(700, 400), new Vector2(900, 400)], true)], pending: true);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null), sketch);

        var paper = ui.PaperShown ?? throw new InvalidOperationException("No paper.");
        var free = ui.FreePart();
        Assert.Equal((free.X0 + 1) / 2 * Width, paper.X, 1);
        Assert.Equal((free.X1 + 1) / 2 * Width, paper.Z, 1);
        Assert.Equal((1 - free.Y1) / 2 * Height, paper.Y, 1);
        Assert.Equal((1 - free.Y0) / 2 * Height, paper.W, 1);
        Assert.True(paper.X > Width / 4f && paper.W <= Height - 64f, $"The paper {paper} is not right of the panel and above the anchors.");
        Assert.False(ui.SketchesShown, "The outline of a sketch shows over the lines.");

        ui.ShowLines(null);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null), sketch);
        Assert.Null(ui.PaperShown);
        Assert.True(ui.SketchesShown, "The sketch does not show with no lines.");
    }

    // The plate of 2026-11-17: 60 x 40 x 10 from a sketch on XY, with a through-hole of radius 8 at (20, 20).
    private static async Task<Guid> Plate(EngineHost host)
    {
        async Task<Guid> Applied(Command command, string output)
        {
            var result = await host.Session.Apply(command);
            Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
            return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
        }

        var sketch = await Applied(new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var points = new List<Guid>();
        foreach (var (x, y) in new[] { (0.0, 0.0), (60.0, 0.0), (60.0, 40.0), (0.0, 40.0) })
            points.Add(await Applied(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < 4; i++)
            await Applied(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }, "lineId");
        var plate = await Applied(new ExtrudeCommand { SketchId = sketch, Distance = 10 }, "bodyId");
        var tool = await Applied(new CreateCylinderCommand { Radius = 8, Height = 20 }, "bodyId");
        tool = await Applied(new TranslateCommand { BodyId = tool, Dx = 20, Dy = 20, Dz = 5 }, "bodyId");
        return await Applied(new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }, "bodyId");
    }

    // Criterion C1 of the design file: the front view of the plate with "Lines" on. The paper is light where the shaded plate
    // was, each visible line is ink, and each wall of the hole is a dashed line of ink and paper. With "Lines" off, the same
    // place shows the shaded plate, not the paper.
    [VulkanImageFact]
    public async Task The_Front_View_Of_The_Plate_Shows_The_Lines_Of_The_Sheet()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        await Plate(host);
        var display = new Vector2(Width, Height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] lined, shaded;
        IReadOnlyList<ViewLines.Stroke> strokes;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);
            for (var frame = 0; frame < 2; frame++)
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies);
            camera.ShowFlat(FlatView.Front);
            var box = renderer.Bounds ?? throw new InvalidOperationException("No bounds.");
            camera.Frame(box.Min, box.Max, (float)Width / Height, ui.FreePart());
            renderer.Update(scene, camera, (float)Width / Height);

            var frameOf = ViewLines.FrameOf(camera)!;
            var read = await host.Session.Query<ViewLinesInfo>(ViewLines.Query(frameOf));
            strokes = ViewLines.Of(read.Result!, camera, Width, Height);

            byte[] Picture(IReadOnlyList<ViewLines.Stroke>? lines)
            {
                ui.ShowLines(lines);
                var data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies);
                return target.Render((commandBuffer, framebuffer, frameExtent) =>
                    renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                        overlay => panelRenderer.Record(overlay, frameExtent, data, 0), null));
            }

            lined = Picture(strokes);
            shaded = Picture(null);
        }

        var picture = Save(lined, "flat-view-lines.png");
        Save(shaded, "flat-view-shaded.png");
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        Vector2 Pixel((double X, double Y, double Z) point)
            => (SketchOverlay.Segment(camera, Width, Height, point, point) ?? throw new InvalidOperationException("No pixel.")).A;
        (byte R, byte G, byte B) At(byte[] pixels, Vector2 p)
        {
            var index = (((int)MathF.Round(p.Y) * (int)Width) + (int)MathF.Round(p.X)) * 4;
            return (pixels[index], pixels[index + 1], pixels[index + 2]);
        }

        static bool Near((byte R, byte G, byte B) a, uint abgr)
            => Math.Abs(a.R - (int)(abgr & 0xFF)) <= 24 && Math.Abs(a.G - (int)((abgr >> 8) & 0xFF)) <= 24 && Math.Abs(a.B - (int)((abgr >> 16) & 0xFF)) <= 24;

        // Six lines: four visible lines of the plate and the two hidden walls of the hole; and the axis of the hole (finding
        // E220, TASK-0410).
        Assert.Equal(4, strokes.Count(stroke => stroke.Visible && !stroke.Centre));
        Assert.Equal(2, strokes.Count(stroke => !stroke.Visible));
        Assert.Equal(1, strokes.Count(stroke => stroke.Centre));

        // The plate fills 0.9 of the free part on its tighter side (ADR-0166 item 4).
        var (left, right) = (Pixel((0, 0, 5)), Pixel((60, 0, 5)));
        var free = ui.FreePart();
        var freeWidth = (free.X1 - free.X0) / 2 * Width;
        Assert.True(right.X - left.X > 0.85 * freeWidth, $"The picture {picture}: the plate is {right.X - left.X} pixels wide in a free part of {freeWidth}.");

        // Inside the plate, away from each line: paper with the lines, the shaded plate without them.
        var inside = Pixel((44, 0, 5));
        Assert.True(Near(At(lined, inside), Theme.Paper), $"The picture {picture}: {At(lined, inside)} at {inside} is not the paper.");
        Assert.False(Near(At(shaded, inside), Theme.Paper), $"The shaded picture: {At(shaded, inside)} at {inside} is the paper.");

        // The bottom edge and the left edge are ink, solid along their middle part.
        foreach (var (from, to) in new[] { ((10.0, 0.0, 0.0), (50.0, 0.0, 0.0)), ((0.0, 0.0, 2.0), (0.0, 0.0, 8.0)) })
        {
            var (a, b) = (Pixel(from), Pixel(to));
            for (var t = 0f; t <= 1f; t += 0.05f)
            {
                var p = Vector2.Lerp(a, b, t);
                Assert.True(Darkest(lined, p) is var ink && Near(ink, Theme.Ink), $"The picture {picture}: the visible line at {p} is {ink}.");
            }
        }

        // Each wall of the hole is dashed: along it, some pixels are dark and some are paper. A line of 1 pixel between two
        // pixels gives each of them about half of the ink, so a dark pixel is darker than the middle of the ink and the paper.
        foreach (var x in new[] { 12.0, 28.0 })
        {
            var (a, b) = (Pixel((x, 0, 0.5)), Pixel((x, 0, 9.5)));
            var samples = Enumerable.Range(0, (int)Vector2.Distance(a, b)).Select(i => Vector2.Lerp(a, b, i / Vector2.Distance(a, b))).ToList();
            var ink = samples.Count(p => Darkest(lined, p) is var c && c.R + c.G + c.B < 550);
            var paper = samples.Count(p => Near(Darkest(lined, p), Theme.Paper));
            Assert.True(ink > samples.Count / 4 && paper > samples.Count / 5, $"The picture {picture}: the wall at x = {x} has {ink} ink and {paper} paper of {samples.Count}.");
        }

        // Finding E220 (TASK-0410): the axis of the hole at x = 20 is a chain past the plate: ink and paper along it, and ink
        // 2 mm under the bottom edge.
        var (axisLow, axisHigh) = (Pixel((20, 0, 0.5)), Pixel((20, 0, 9.5)));
        var axis = Enumerable.Range(0, (int)Vector2.Distance(axisLow, axisHigh)).Select(i => Vector2.Lerp(axisLow, axisHigh, i / Vector2.Distance(axisLow, axisHigh))).ToList();
        var axisInk = axis.Count(p => Darkest(lined, p) is var c && c.R + c.G + c.B < 550);
        var axisPaper = axis.Count(p => Near(Darkest(lined, p), Theme.Paper));
        Assert.True(axisInk > axis.Count / 2 && axisPaper > 0, $"The picture {picture}: the axis has {axisInk} ink and {axisPaper} paper of {axis.Count}.");
        Assert.True(Enumerable.Range(1, 4).Any(i => Darkest(lined, Pixel((20, 0, -0.5 * i))) is var c && c.R + c.G + c.B < 550), $"The picture {picture}: the axis does not go past the bottom edge.");

        // The darkest pixel within one pixel across, so a line of 1 pixel between two pixels is found.
        (byte R, byte G, byte B) Darkest(byte[] pixels, Vector2 p)
        {
            var best = At(pixels, p);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var c = At(pixels, p + new Vector2(dx, dy));
                    if (c.R + c.G + c.B < best.R + best.G + best.B)
                        best = c;
                }
            }

            return best;
        }
    }

    private static string Save(byte[] pixels, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }

    // Findings V196 and V201 of the codebase review of 2026-11-18 (TASK-0410): an event of a sheet does not start a new read of
    // the lines, and each other event does, so a new view of a sheet does not read the lines of the bodies again. The frame keys
    // the read on the count of those events, and gives the paper the notes of the skipped bodies; the test reads the file of the
    // frame for that, because the frame needs a window.
    [Fact]
    public void An_Event_Of_A_Sheet_Does_Not_Start_A_New_Read()
    {
        foreach (var kind in new[] { "sheet.created", "sheet.changed", "sheet.removed" })
            Assert.False(ViewLines.ChangesLines(kind), kind);
        foreach (var kind in new[] { "body.created", "body.changed", "sketch.changed", "sheets" })
            Assert.True(ViewLines.ChangesLines(kind), kind);

        var frame = FrameSource();
        Assert.Matches(@"if \(ViewLines\.ChangesLines\(record\.Kind\)\)\s*Interlocked\.Increment\(ref _lineChanges\);", frame);
        Assert.Contains("_viewLines.Lines(Interlocked.Read(ref _lineChanges), lineFrame,", frame, StringComparison.Ordinal);
        Assert.Contains("ViewLines.Notes(lines, _renderer.LiveBodies)", frame, StringComparison.Ordinal);
    }

    private static string FrameSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "3DEngine.sln")))
            directory = directory.Parent;
        return File.ReadAllText(Path.Combine(directory?.FullName ?? throw new InvalidOperationException("No 3DEngine.sln."), "3DEngine.App", "NativeThreeDEngine.cs"));
    }

    // Finding V198 of the codebase review of 2026-11-18 (TASK-0410): the paper shows with the switch "Lines" on in a flat view
    // only, and the frame shows no gizmo then. The test reads the file of the frame for the guard of the gizmo, because the frame
    // needs a window.
    [Fact]
    public void The_Gizmo_Does_Not_Show_While_The_Paper_Shows()
    {
        var camera = SessionScene.Create().Cameras[0];
        Assert.False(ViewLines.PaperShows(true, camera));
        camera.ShowFlat(FlatView.Front);
        Assert.False(ViewLines.PaperShows(false, camera));
        Assert.True(ViewLines.PaperShows(true, camera));

        var frame = FrameSource();
        var start = frame.IndexOf("private GizmoLayout? GizmoLayout()", StringComparison.Ordinal);
        Assert.True(start >= 0, "NativeThreeDEngine.cs has no method GizmoLayout.");
        var body = frame[start..frame.IndexOf("\n    }", start, StringComparison.Ordinal)];
        Assert.Matches(@"if \(ViewLines\.PaperShows\(_ui\?\.LinesOn == true, Camera\)\)\s*return null;", body);
    }

    // Findings E220 and V201 of the codebase review of 2026-11-18 (TASK-0410): a centre line of the read is a stroke with Centre
    // true, and the paper names each body that the view skips, with the name of the body and the reason of the engine; a body
    // with no live record is "A body". With no lines there is no note.
    [Fact]
    public void The_Paper_Has_The_Centre_Lines_And_Names_Each_Skipped_Body()
    {
        var camera = SessionScene.Create().Cameras[0];
        camera.ShowFlat(FlatView.Front);
        double[] front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
        var scan = new LiveBody(Guid.NewGuid(), Engine.Core.Commands.ImportStlCommandHandler.MeshKind, "Scan", true);
        var lines = new ViewLinesInfo(front,
            [new ViewLineInfo(Body.Id, 0, 0, 10, 0, true, "edge"), new ViewLineInfo(Body.Id, 5, -3, 5, 13, true, "centre")],
            [new ViewSkippedBody(scan.Id, "a mesh body has no faces of the kernel"), new ViewSkippedBody(Guid.NewGuid(), "a fault")]);

        var strokes = ViewLines.Of(lines, camera, Width, Height);
        Assert.Equal([false, true], strokes.Select(stroke => stroke.Centre));
        var notes = ViewLines.Notes(lines, [Body, scan]);
        Assert.Equal(["Scan gives no lines: a mesh body has no faces of the kernel", "A body gives no lines: a fault"], notes);

        var display = new Vector2(Width, Height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        ui.ShowLines(strokes, notes: notes);
        Frame(ui, display);
        Assert.Equal(notes, ui.PaperNotes);
        ui.ShowLines(null, notes: notes);
        Frame(ui, display);
        Assert.Empty(ui.PaperNotes);
    }

    // Finding E220 (TASK-0410): the chain of a centre line: a long dash, a gap, a short dash and a gap, along the whole path. A
    // pattern with no pair of lengths greater than zero is refused.
    [Fact]
    public void A_Centre_Line_Is_A_Chain()
    {
        var dashes = ViewLines.Dashes([new Vector2(0, 0), new Vector2(30, 0)], DesktopUi.CentreChain);

        Assert.Equal([(new Vector2(0, 0), new Vector2(12, 0)), (new Vector2(15, 0), new Vector2(17, 0)), (new Vector2(20, 0), new Vector2(30, 0))], dashes);
        Assert.Throws<ArgumentException>(() => ViewLines.Dashes([new Vector2(0, 0), new Vector2(30, 0)], [12f, 3f, 2f]));
        Assert.Throws<ArgumentException>(() => ViewLines.Dashes([new Vector2(0, 0), new Vector2(30, 0)], [12f, 0f]));
    }
}
