using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0100 (phase R17a, TASK-0314): the path line from the document to the selection, Escape up to the document, and the
// rows of the document. ADR-0100 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class DocumentPathTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly Vector2 Landscape = new(1440, 900);
    private static readonly DocumentLine Bracket = new("bracket.3de", null, Kind: Document.Part);

    private static readonly LiveBody Plate = new(A, "Box", "Plate", true);
    private static readonly LiveBody Broken = new(B, "Extrude", "Rib", true, new BodyFault("E-KERNEL-EXTRUDE-OPEN", "The profile is not closed."));

    private static FaceLine FaceTop(bool edge = false)
        => new("Face +z: 100 square mm", true, [], edge ? "Edge +x | +z: 10 mm" : null, null, "Face +z", edge ? "Edge +x | +z" : null);

    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, Guid? selection, DocumentLine? document, FaceLine? face = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies, selection, document, face: face);
    }

    private static Vector4 Rectangle(DesktopUi ui, string item)
        => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static PointerSample Mouse(double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    // A press and a release in the middle of an item, in two frames, and the actions that they gave.
    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, Guid? selection, DocumentLine? document, FaceLine? face, string item)
    {
        ui.TakeActions();
        var at = Rectangle(ui, item);
        var (x, y) = ((at.X + at.Z) / 2, (at.Y + at.W) / 2);
        Step(ui, display, bodies, selection, document, face, Mouse(x, y, PointerPhase.Move, 0));
        Step(ui, display, bodies, selection, document, face, Mouse(x, y, PointerPhase.Down, 10));
        Step(ui, display, bodies, selection, document, face, Mouse(x, y, PointerPhase.Up, 20));
        return ui.TakeActions();
    }

    // Item 1: the path gives the file, then the body, the face and the edge, each as one segment of at least 44 pixels high,
    // in the order of the levels from the left to the right. With nothing selected the path is the file only.
    [Fact]
    public void The_Path_Gives_Each_Level_From_The_File_To_The_Edge()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, [Plate], null, Bracket);
        Step(ui, Landscape, [Plate], null, Bracket);
        Assert.Equal([("bracket.3de", PathLevel.Document)], ui.PathSegments(Bracket, null));
        Assert.Null(ui.ItemRectangle(DesktopUi.PathSegmentItem(PathLevel.Body)));

        Step(ui, Landscape, [Plate], A, Bracket, FaceTop(edge: true));
        Step(ui, Landscape, [Plate], A, Bracket, FaceTop(edge: true));
        Assert.Equal(
            [("bracket.3de", PathLevel.Document), ("Plate", PathLevel.Body), ("Face +z", PathLevel.Face), ("Edge +x | +z", PathLevel.Edge)],
            ui.PathSegments(Bracket, Plate));
        var segments = new[] { PathLevel.Document, PathLevel.Body, PathLevel.Face, PathLevel.Edge }
            .Select(level => Rectangle(ui, DesktopUi.PathSegmentItem(level))).ToArray();
        for (var i = 0; i < segments.Length; i++)
        {
            Assert.True(segments[i].W - segments[i].Y >= DesktopUi.MinimumControlHeight, $"The segment {i} at {segments[i]} is lower than 44 pixels.");
            if (i > 0)
                Assert.True(segments[i].X >= segments[i - 1].Z, $"The segment {i} at {segments[i]} is not right of the segment before it.");
        }

        // The top bar keeps its text in its place, and it ends above the panel or the rows.
        var title = Rectangle(ui, DesktopUi.TitleItem);
        Assert.Equal((16f, 16f), (title.X, title.Y));
        Assert.True(segments.All(segment => segment.W <= 16f + DesktopUi.TopBarHeight + 8f), "A segment goes into the rows under the top bar.");

        // A host with no files gives "Document", and a body with no name gives its kind.
        Assert.Equal([("Document", PathLevel.Document), ("Box", PathLevel.Body), ("Face +z", PathLevel.Face), ("Edge +x | +z", PathLevel.Edge)],
            ui.PathSegments(new DocumentLine(null, null, Files: false), Plate with { Name = null }));
    }

    // Item 2: a tap on a segment that is not the last one gives the action of its level; the last segment takes no tap, and
    // a tap on the top bar does not reach the scene.
    [Fact]
    public void A_Tap_On_A_Segment_Selects_Its_Level()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var face = FaceTop(edge: true);
        Step(ui, Landscape, [Plate], A, Bracket, face);
        Step(ui, Landscape, [Plate], A, Bracket, face);

        foreach (var level in new[] { PathLevel.Document, PathLevel.Body, PathLevel.Face })
            Assert.Equal([new PathAction(level)], Tap(ui, Landscape, [Plate], A, Bracket, face, DesktopUi.PathSegmentItem(level)));
        Assert.Empty(Tap(ui, Landscape, [Plate], A, Bracket, face, DesktopUi.PathSegmentItem(PathLevel.Edge)));

        var file = Rectangle(ui, DesktopUi.PathSegmentItem(PathLevel.Document));
        Assert.True(ui.OverPanel((file.X + file.Z) / 2, file.Y + 1), "A tap on the top of a segment reaches the scene.");
    }

    // Item 2: the action of each level on the selection. The file clears the body, the face and the edge; the body clears the
    // face and the edge; the face clears the edge.
    [Fact]
    public async Task Each_Level_Clears_The_Levels_Under_It()
    {
        using var host = await OneBox();
        var actions = new BodyActions(host.Session);
        var edge = (First: Face("+x"), Second: Face("+z"));

        void SelectEdge()
        {
            actions.Apply(new SelectIntent(0, 0), Top, [], Near);
            actions.Apply(new SelectIntent(0, 0), Top, [], Near);
            actions.Apply(new SelectIntent(200, 0), Top, [], Near);
            Assert.Equal((A, Face("+z"), ((DatumReference, DatumReference)?)edge), (actions.Selection, actions.Face, actions.Edge));
        }

        (DatumReference, DatumReference)? Near(double x, double y) => x > 100 ? edge : null;

        SelectEdge();
        actions.Up(PathLevel.Face);
        Assert.Equal((A, Face("+z"), ((DatumReference, DatumReference)?)null), (actions.Selection, actions.Face, actions.Edge));

        actions.Up(PathLevel.Body);
        Assert.Equal((A, (DatumReference?)null), (actions.Selection, actions.Face));

        SelectEdge();
        actions.Up(PathLevel.Body);
        Assert.Equal((A, (DatumReference?)null, ((DatumReference, DatumReference)?)null), (actions.Selection, actions.Face, actions.Edge));

        SelectEdge();
        actions.Up(PathLevel.Document);
        Assert.Equal(((Guid?)null, (DatumReference?)null, ((DatumReference, DatumReference)?)null), (actions.Selection, actions.Face, actions.Edge));
    }

    // Item 3: Escape goes up one level each time, from the edge to the document, and with nothing selected it gives nothing.
    // The test fails when Escape stops at the body again (ADR-0075 item 2 and ADR-0077 item 3 before this record).
    [Fact]
    public async Task Escape_Goes_Up_From_The_Edge_To_The_Document()
    {
        using var host = await OneBox();
        var actions = new BodyActions(host.Session);
        var edge = (First: Face("+x"), Second: Face("+z"));
        (DatumReference, DatumReference)? Near(double x, double y) => x > 100 ? edge : null;
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        actions.Apply(new SelectIntent(200, 0), Top, [], Near);

        var escape = Assert.Single(new IntentMachine().Step([new KeySample(InputKey.Escape, true, KeyModifiers.None, 0)]));
        Assert.True(actions.Apply(escape, Top, []));
        Assert.Equal((A, Face("+z"), ((DatumReference, DatumReference)?)null), (actions.Selection, actions.Face, actions.Edge));
        Assert.True(actions.Apply(escape, Top, []));
        Assert.Equal((A, (DatumReference?)null), (actions.Selection, actions.Face));
        Assert.True(actions.Apply(escape, Top, []));
        Assert.Null(actions.Selection);
        Assert.False(actions.Apply(escape, Top, []));
    }

    // Item 4: on a landscape display with nothing selected, the first row gives the kind, the unit and the counts, with the
    // count that failed in red; each body has a row with its state, and a failed body shows "failed" and its code in red. A
    // tap on a body row selects the body. While a body is selected the rows of the document do not show.
    [Fact]
    public void The_Rows_Of_The_Document_Give_The_Kind_The_Unit_The_Counts_And_The_Faults()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        LiveBody[] bodies = [Plate, Broken];
        Step(ui, Landscape, bodies, null, Bracket);
        Step(ui, Landscape, bodies, null, Bracket);

        Assert.Equal("Part", DesktopUi.DocumentKind(Bracket));
        Assert.Equal("Assembly", DesktopUi.DocumentKind(Bracket with { Kind = Document.Assembly }));
        Assert.Equal([("mm · 2 bodies · ", false), ("1 failed", true)], ui.DocumentState(bodies));
        Assert.Equal([("mm · 1 body · all correct", false)], ui.DocumentState([Plate]));

        var first = Rectangle(ui, DesktopUi.DocumentRowItem);
        var panel = ui.PanelRectangles[0];
        Assert.True(first.Y - panel.Y <= 20f, $"The first row of the document at {first} is not at the top of the panel at {panel}.");
        var rows = bodies.Select(body => Rectangle(ui, DesktopUi.RowText(body))).ToArray();
        Assert.All(rows, row => Assert.True(row.W - row.Y >= DesktopUi.RowHeight, $"The row at {row} is lower than a row."));
        Assert.True(rows[0].Y >= first.W && rows[1].Y >= rows[0].W, "The rows of the bodies are not under the first row, in the order of the bodies.");
        Assert.True(Rectangle(ui, DesktopUi.FilterLabel).W <= rows[0].Y, "The filter is not above the rows of the bodies.");
        Assert.True(Rectangle(ui, DesktopUi.OpenProjectLabel).Y >= rows[1].W, "The controls of the panel are not under the rows of the document.");
        Assert.Null(ui.ItemRectangle(DesktopUi.RowsTitleItem));

        Assert.Equal([new SelectBodyAction(B)], Tap(ui, Landscape, bodies, null, Bracket, null, DesktopUi.RowText(Broken)));

        Step(ui, Landscape, bodies, A, Bracket);
        Step(ui, Landscape, bodies, A, Bracket);
        Assert.Null(ui.ItemRectangle(DesktopUi.DocumentRowItem));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RowsTitleItem));
    }

    // Item 4: the filter keeps the rows of the bodies whose text holds it, as in the list of the panel.
    [Fact]
    public void The_Filter_Keeps_The_Rows_Of_The_Document_That_Hold_It()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        LiveBody[] bodies = [Plate, Broken];
        Step(ui, Landscape, bodies, null, Bracket);
        Step(ui, Landscape, bodies, null, Bracket);
        Assert.Equal(bodies.Select(DesktopUi.RowText), ui.ShownBodies);

        var filter = Rectangle(ui, DesktopUi.FilterLabel);
        var (x, y) = ((filter.X + filter.Z) / 2, (filter.Y + filter.W) / 2);
        Step(ui, Landscape, bodies, null, Bracket, null, Mouse(x, y, PointerPhase.Down, 0));
        Step(ui, Landscape, bodies, null, Bracket, null, Mouse(x, y, PointerPhase.Up, 10));
        Step(ui, Landscape, bodies, null, Bracket, null, new TextSample("rib", 20));
        Step(ui, Landscape, bodies, null, Bracket);

        Assert.Equal([DesktopUi.RowText(Broken)], ui.ShownBodies);
    }

    // Item 1: a long path gives "..." for the middle segments first and keeps the last segment whole, and the top bar ends 12
    // pixels left of the command line, at 1440 by 900 and at 844 by 390. At 1440 the file name stays whole; at 844 the path
    // with "..." is still too wide, so the file name is cut to the room that is left, and it keeps at least 48 pixels.
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(844, 390)]
    public void A_Long_Path_Cuts_The_Middle_Segments_First(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var long_ = Plate with { Name = "Bracket of the second floor of the workshop, left side, version 4" };
        var face = FaceTop(edge: true);
        Step(ui, display, [long_], A, Bracket, face);
        Step(ui, display, [long_], A, Bracket, face);

        var title = Rectangle(ui, DesktopUi.TitleItem);
        var command = Rectangle(ui, DesktopUi.CommandItem);
        Assert.True(title.Z <= command.X - 12f + 0.5f, $"The top bar ends at {title.Z}, over the command line that starts at {command.X}.");

        var file = Rectangle(ui, DesktopUi.PathSegmentItem(PathLevel.Document));
        var last = Rectangle(ui, DesktopUi.PathSegmentItem(PathLevel.Edge));
        if (width >= 1440)
            Assert.True(Math.Abs((file.Z - file.X) - ui.TextSize("bracket.3de").X) < 1f, $"The file name at {file} is cut.");
        else
            Assert.True(file.Z - file.X >= 48f - 0.5f, $"The file name at {file} is narrower than 48 pixels.");
        // The middle segments give "..." from the second one, until the path fits: at 1440 the face stays whole.
        foreach (var (middle, cut) in new[] { (PathLevel.Body, true), (PathLevel.Face, width < 1440) })
        {
            var at = Rectangle(ui, DesktopUi.PathSegmentItem(middle));
            var expected = ui.TextSize(cut ? "..." : "Face +z").X;
            Assert.True(Math.Abs((at.Z - at.X) - expected) < 1f, $"The middle segment {middle} at {at} is not {(cut ? "\"...\"" : "whole")}.");
        }

        Assert.True(Math.Abs((last.Z - last.X) - ui.TextSize("Edge +x | +z").X) < 1f, $"The last segment at {last} is cut.");
    }

    // Item 5: on a portrait display the path line is the first line under the top bar, the panel is under it, and a tap on a
    // segment acts as on the desktop. ADR-0131 item 1 (phase R18k, TASK-0360): with a selected body the rows under the part
    // take the place of the panel, so they are under the path line. Until TASK-0360 the panel showed with a selected body.
    [Fact]
    public void On_A_Portrait_Display_The_Path_Line_Is_Under_The_Top_Bar()
    {
        var portrait = new Vector2(390, 844);
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var phone = new DocumentLine(null, null, Files: false);
        Step(ui, portrait, [Plate], A, phone, FaceTop());
        Step(ui, portrait, [Plate], A, phone, FaceTop());

        var title = Rectangle(ui, DesktopUi.TitleItem);
        var path = Rectangle(ui, DesktopUi.PathItem);
        var rows = ui.RowsArea ?? throw new InvalidOperationException("No rows.");
        Assert.Empty(ui.PanelRectangles);
        Assert.True(path.Y >= title.W - 0.5f && path.W <= rows.Y, $"The path line at {path} is not between the top bar at {title} and the rows at {rows}.");
        Assert.True(path.W - path.Y >= DesktopUi.MinimumControlHeight, $"The path line at {path} is lower than 44 pixels.");
        Assert.True(path.Z <= portrait.X - 16f, $"The path line at {path} goes past the right margin.");
        Assert.Null(ui.ItemRectangle(DesktopUi.DocumentRowItem));
        Assert.Equal([new PathAction(PathLevel.Body)], Tap(ui, portrait, [Plate], A, phone, FaceTop(), DesktopUi.PathSegmentItem(PathLevel.Body)));
    }

    // Item 5: on a portrait display the panel under the path line ends above the ring of the selected body, so a tap on Move
    // opens its pad. With no such rule the path line moved the panel onto Move at 390 by 844; register entry R-0058 gives
    // the same fault at 360 by 780. ADR-0131 item 1 (phase R18k, TASK-0360): with a selected body the rows under the part show
    // in the place of the panel, and they end 8 pixels above the ring too.
    [Theory]
    [InlineData(360, 780)]
    [InlineData(390, 844)]
    [InlineData(393, 873)]
    [InlineData(412, 915)]
    public void On_A_Portrait_Display_The_Panel_Ends_Above_The_Ring(int width, int height)
    {
        var portrait = new Vector2(width, height);
        var centre = new Vector2(width / 2f, height * 0.6f);
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var phone = new DocumentLine(null, null, Files: false);
        void Touch(params InputSample[] frame)
        {
            ui.Route(frame);
            ui.Frame(1f / 60f, portrait, Vector2.One, [Plate], A, phone, ringCentre: centre);
        }

        Touch();
        Touch();
        var top = ui.RingTop() ?? throw new InvalidOperationException("No ring.");
        var move = Rectangle(ui, DesktopUi.RingItem(DesktopUi.MoveLabel));
        var (x, y) = ((move.X + move.Z) / 2, (move.Y + move.W) / 2);
        foreach (var panel in ui.PanelRectangles.Append(ui.RowsArea ?? throw new InvalidOperationException("No rows.")))
        {
            Assert.False(panel.W > top - 8f + 0.5f && panel.Y < top, $"{width}x{height}: the panel {panel} goes past the top {top} of the ring.");
            Assert.False(x >= panel.X && x < panel.Z && y >= panel.Y && y < panel.W, $"{width}x{height}: Move at ({x}, {y}) is under the panel {panel}.");
        }

        Touch(new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Touch(new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 10_000_000));
        Touch();
        Assert.NotNull(ui.Pad);
    }

    // Items 1 and 4: the pictures for the owner, at 1440 by 900 and at 844 by 390: the rows of the document with nothing
    // selected, and the path to a face of the selected body.
    [VulkanImageFact]
    public async Task The_Pictures_Show_The_Path_Line_And_The_Rows()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var plate = Out(await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 6 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = plate, NewName = "Plate" });
        var post = Out(await host.Session.Apply(new CreateCylinderCommand { Radius = 4, Height = 20 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = post, NewName = "Post" });

        foreach (var (width, height) in new[] { (1440u, 900u), (844u, 390u) })
        {
            foreach (var selected in new[] { false, true })
            {
                using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
                var scene = SessionScene.Create();
                var camera = scene.Cameras[0];
                var display = new Vector2(width, height);
                byte[] pixels;
                using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
                {
                    using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                    using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
                    using var panelRenderer = new UiRenderer(device, target.RenderPass);
                    var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                    ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                    renderer.Update(scene, camera, (float)width / height);
                    var data = UiDrawData.Empty;
                    for (var frame = 0; frame < 3; frame++)
                    {
                        ui.Route([]);
                        data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, selected ? plate : null, Bracket, face: selected ? FaceTop() : null);
                    }

                    Assert.NotNull(ui.ItemRectangle(selected ? DesktopUi.PathSegmentItem(PathLevel.Face) : DesktopUi.DocumentRowItem));
                    pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                        renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                            overlay => panelRenderer.Record(overlay, frameExtent, data, 0), selected ? plate : null));
                }

                SavePicture(pixels, width, height, $"path-{(selected ? "face" : "document")}-{width}x{height}.png");
            }
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
    }

    private static PickHit? Top(double x, double y) => new PickHit(A, 1) { Point = (0, 0, 0.5), Normal = (0, 0, 1) };

    private static DatumReference Face(string element) => new(A, element, null);

    private static async Task<EngineHost> OneBox()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = A, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        return host;
    }

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static void SavePicture(byte[] pixels, uint width, uint height, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        Png.Write(Path.Combine(folder, name), (int)width, (int)height, pixels);
    }
}
