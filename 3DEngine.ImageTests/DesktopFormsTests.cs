using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// The findings of the desktop in the codebase review of 2026-10-30 (TASK-0118): V46 a pad that
// shows a stale value, V47 an Enter in the frame where the scope of a pad changes, V48 one mesh for
// each body and frame, V49 a pixel of a large magnitude, and V50 the fault text in the panel.
[Collection(DearImGui.Collection)]
public sealed class DesktopFormsTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Guid SketchId = Guid.Parse("50000000-0000-0000-0000-000000000000");
    private static readonly Guid A = Guid.Parse("a0000000-0000-0000-0000-000000000000");

    private static SketchInfo Sketch(double ax, double ay) => new(SketchId, Datums.XY.Id, null, [new(SketchId, 0, 0), new(A, ax, ay)], [], [], [], [], true, 2);

    private static SketchPanel Selected(double ax, double ay, SketchToolKind tool = SketchToolKind.Select)
        => new([Sketch(ax, ay)], Sketch(ax, ay), tool, false, [A], null);

    private static void Step(DesktopUi ui, SketchPanel? panel, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, null, panel);
    }

    private static DesktopUi Opened(SketchPanel panel)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui, panel);
        Step(ui, panel);
        return ui;
    }

    private static void Tap(DesktopUi ui, string item, SketchPanel panel)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The user interface has no control {item}.");
        var (x, y) = ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
        Step(ui, panel, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, panel, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, panel);
        Step(ui, panel);
    }

    // V46: the pad Move point closes when the point that it shows moves, and stays open when the
    // point stays.
    [Fact]
    public void A_Pad_Closes_When_A_Value_That_It_Shows_Changes()
    {
        using var ui = Opened(Selected(3, 4));
        Tap(ui, DesktopUi.SketchItem(DesktopUi.MovePointLabel), Selected(3, 4));
        Assert.Equal(DesktopUi.MovePointLabel, ui.Pad?.Title);

        Step(ui, Selected(3, 4));
        Assert.NotNull(ui.Pad);

        Step(ui, Selected(7, 8));
        Assert.Null(ui.Pad);
    }

    // V47: an Enter in the frame where the chain ends gives no action of the Next point pad.
    [Fact]
    public void An_Enter_In_The_Frame_Of_A_New_Scope_Gives_No_Action()
    {
        var open = new SketchPanel([Sketch(3, 4)], Sketch(3, 4), SketchToolKind.Line, true, [], null);
        using var ui = Opened(open);
        ui.OpenNextPointPad(chainOpen: true);
        Step(ui, open);
        ui.TakeActions();

        Step(ui, open with { ChainOpen = false }, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));

        Assert.Null(ui.Pad);
        Assert.DoesNotContain(ui.TakeActions(), action => action is NextPointAction);
    }

    // V47: the control: an Enter in a frame with the same scope applies the pad.
    [Fact]
    public void An_Enter_In_The_Frame_Of_The_Same_Scope_Applies_The_Pad()
    {
        var open = new SketchPanel([Sketch(3, 4)], Sketch(3, 4), SketchToolKind.Line, true, [], null);
        using var ui = Opened(open);
        ui.OpenNextPointPad(chainOpen: true);
        Step(ui, open);
        ui.TakeActions();

        Step(ui, open, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));

        Assert.Contains(ui.TakeActions(), action => action is NextPointAction);
    }

    // V49: a point at 3e37 gives no vertex beyond the limit. Since V52 (TASK-0124) a line through the
    // window draws its part in the window, and a line beside the window draws nothing.
    [Fact]
    public void A_Point_Far_Beyond_The_Window_Gives_No_Vertex_Beyond_The_Limit()
    {
        var (far, beside) = (Guid.NewGuid(), Guid.NewGuid());
        var sketch = new SketchInfo(SketchId, Datums.XY.Id, null, [new(SketchId, -3e37, -3e37), new(far, 3e37, 3e37), new(beside, 3e37, 4e37)],
            [new SketchLine(Guid.NewGuid(), SketchId, far)], [], [], [], true, 2);
        var aside = sketch with { Points = [new(SketchId, 0, 0), new(far, 3e37, 3e37), new(beside, 3e37, 4e37)], Lines = [new SketchLine(Guid.NewGuid(), far, beside)] };
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 100, Target = (0, 0, 0) };

        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y, new SketchOverlay.Focus(SketchId, [], null));

        var (a, b, _) = Assert.Single(strokes.Lines);
        Assert.All(new[] { a, b }, end => Assert.True(Math.Abs(end.X) <= 2 * Display.X && Math.Abs(end.Y) <= 2 * Display.Y, $"{end}"));
        Assert.All(strokes.Points, spot => Assert.True(Math.Abs(spot.At.X) <= SketchOverlay.PixelLimit && Math.Abs(spot.At.Y) <= SketchOverlay.PixelLimit));
        Assert.Empty(SketchOverlay.Of([aside], camera, Display.X, Display.Y).Lines);
    }

    // V52 (TASK-0124): a line on the plane XZ that passes beside the eye and behind it keeps the part
    // that is in the window. The cut at the near distance gave a pixel beyond the limit of V49, and
    // the overlay dropped the line.
    [Theory]
    [InlineData(5.0)]
    [InlineData(20.0)]
    public void A_Line_That_Passes_Behind_The_Eye_Keeps_Its_Visible_Part(double side)
    {
        var (start, end) = (Guid.NewGuid(), Guid.NewGuid());
        var sketch = new SketchInfo(SketchId, Datums.XZ.Id, null, [new(SketchId, 0, 0), new(start, side, -30), new(end, side, 20)],
            [new SketchLine(Guid.NewGuid(), start, end)], [], [], [], true, 2);
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 10, Target = (0, 0, 0) };

        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y);

        var (a, b, _) = Assert.Single(strokes.Lines);
        Assert.True(a.X >= 0 && a.X <= Display.X || b.X >= 0 && b.X <= Display.X, $"{a} {b}");
    }

    // V50: the fault of a selected body takes one line, so Show all stays in the panel on a display
    // of 1024 × 768 with two bodies. ADR-0099 item 1 (TASK-0313): on a landscape display the rows take the place of the
    // panel, so the test uses the display of 768 × 1024. ADR-0131 item 1 (phase R18k, TASK-0360): the rows of the selected
    // body show on this display too, and their first row gives the code of the fault on one line under the name, with the
    // reason in its tooltip. Until TASK-0360 the panel gave a line "Fault". Show all is in the panel with no selection.
    // ADR-0174 item 3 (TASK-0419): "Show all" is in the open structure view while a body is hidden.
    [Fact]
    public void The_Fault_Takes_One_Line_And_Show_All_Stays_In_The_Panel()
    {
        var small = new Vector2(768, 1024);
        var fault = new BodyFault("W-SKETCH-REBUILD-FAILED", "The body '0fe4ab0b-8d7a-5381-a0e5-152a7ab115b6' keeps its last solid: the point 'a0000000-0000-0000-0000-000000000000' is the end of 1 line, and a closed profile has 2 lines at each point.");
        LiveBody[] bodies = [new(Guid.NewGuid(), "Extrude", "Plate", true, fault), new(Guid.NewGuid(), "Box", null, true)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, small, Vector2.One, bodies, bodies[0].Id);
        }

        var line = ui.ItemRectangle(DesktopUi.RowsTitleItem)!.Value;
        Assert.True(line.W - line.Y < 2 * ui.ControlHeight, $"The name and the fault take {line.W - line.Y} pixels.");
        Assert.Contains(ui.RowTitle(bodies[0]).State, part => part.Red && part.Text.StartsWith("failed W-SKETCH", StringComparison.Ordinal));

        ui.OpenStructure();
        LiveBody[] hidden = [bodies[0], bodies[1] with { Visible = false }];
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, small, Vector2.One, hidden, null);
        }

        var showAll = ui.ItemRectangle(DesktopUi.ShowAllLabel)!.Value;
        Assert.True(showAll.W <= ui.PanelRectangles.First().W, $"Show all ends at {showAll.W}, below the panel at {ui.PanelRectangles.First().W}.");
    }

    // V48: twenty changes of one body in one frame give one mesh.
    [VulkanImageFact]
    public async Task Twenty_Changes_Of_One_Body_In_One_Frame_Give_One_Mesh()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Output(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Output(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        for (var i = 0; i < points.Count; i++)
            await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] });
        await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 5 });

        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        using var target = new OffscreenTarget(device, new VkExtent2D(320, 240));
        using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
        renderer.Update(scene, camera, 4f / 3);
        var before = renderer.Meshes;

        for (var i = 0; i < 20; i++)
            await host.Session.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 11 + i, Y = 6 });
        renderer.Update(scene, camera, 4f / 3);

        Assert.Equal(before + 1, renderer.Meshes);
        Assert.Equal(30.0, renderer.Bounds!.Value.Max.X);
    }

    // V55 (TASK-0124): an extrude and five changes of its body before the first update give one mesh,
    // of the last solid. The creation took the key of its event, and the last change meshed again.
    [VulkanImageFact]
    public async Task A_Body_Created_And_Changed_In_One_Frame_Gives_One_Mesh()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        using var target = new OffscreenTarget(device, new VkExtent2D(320, 240));
        using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
        renderer.Update(scene, camera, 4f / 3);
        var before = renderer.Meshes;

        var sketch = Output(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Output(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        for (var i = 0; i < points.Count; i++)
            await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] });
        await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 5 });
        for (var i = 0; i < 5; i++)
            await host.Session.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 11 + i, Y = 6 });
        renderer.Update(scene, camera, 4f / 3);

        Assert.Equal(before + 1, renderer.Meshes);
        Assert.Equal(15.0, renderer.Bounds!.Value.Max.X);
    }

    // ADR-0191 item 8 (phase P11.0, TASK-0474): a PlaceBody keeps the geometry key of the body and moves it, so the window
    // takes its triangles again in the new place, and the section view knows the new mesh by its key and its placement. A
    // rename takes no new mesh. Finding T205 of the codebase review of 2026-11-28 (TASK-0477): the section view runs, with
    // cuts that end at once, and it starts a new cut after the placement and none after the rename.
    [VulkanImageFact]
    public async Task A_Placed_Body_Gives_One_New_Mesh_In_Its_Place()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = Output(await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }), "bodyId");
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        var scene = SessionScene.Create();
        var meshes = new Dictionary<string, GpuMesh>();
        var cuts = new Dictionary<string, GpuMesh>();
        using var bodies = new SessionBodies(new HostedSession(host));
        var view = new SectionView { RunCut = cut => Task.FromResult(cut()) };
        var plane = new SectionCut((0, 0, 0), (1, 0, 0));
        bool Follow() => view.Follow(plane, bodies.SectionBodies, bodies.Bounds, cuts, device);
        try
        {
            bodies.Apply(scene, meshes, device);
            var (count, key) = (bodies.Meshes, Assert.Single(bodies.SectionBodies).Geometry);
            Follow();
            Follow();
            Assert.Equal(1, view.Started);
            Assert.True(cuts.ContainsKey(SectionView.KeptId(box)));
            await host.Session.Apply(new RenameBodyCommand { BodyId = box, NewName = "Plate" });
            bodies.Apply(scene, meshes, device);
            Assert.Equal(count, bodies.Meshes);
            Follow();
            Assert.Equal(1, view.Started);

            // A move of 2 mm, so the plane x = 0 cuts the box in its old place and in its new one.
            var placed = await host.Session.Apply(new PlaceBodyCommand { BodyId = box, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 2, Dy = 0, Dz = 0 });
            Assert.Equal(CommandStatus.Applied, placed.Status);
            bodies.Apply(scene, meshes, device);
            Assert.Equal(count + 1, bodies.Meshes);
            Assert.Equal((-3.0, 7.0), (bodies.Bounds!.Value.Min.X, bodies.Bounds!.Value.Max.X));
            Assert.NotEqual(key, Assert.Single(bodies.SectionBodies).Geometry);
            // The cut of the old place leaves the GPU at once, and the cut of the new place comes at the next frame.
            Follow();
            Assert.Equal(2, view.Started);
            Assert.False(cuts.ContainsKey(SectionView.KeptId(box)));
            Follow();
            Assert.True(cuts.ContainsKey(SectionView.KeptId(box)));
        }
        finally
        {
            foreach (var mesh in meshes.Values.Concat(cuts.Values))
                mesh.Dispose();
        }
    }

    // Finding V239 of the codebase review of 2026-11-28 (TASK-0477): the window keeps the faces and the edges of the selected
    // body under the key of its mesh, which a PlaceBody changes, so the selected face and its edges come in the new place.
    // Until TASK-0477 the key was the geometry key alone, and the face stayed at x = 0.
    [VulkanImageFact]
    public async Task The_Selected_Face_Of_A_Placed_Body_Is_In_Its_Place()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = Output(await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }), "bodyId");
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        var scene = SessionScene.Create();
        var meshes = new Dictionary<string, GpuMesh>();
        using var bodies = new SessionBodies(new HostedSession(host));
        var actions = new BodyActions(host.Session);
        try
        {
            bodies.Apply(scene, meshes, device);
            actions.Select(box);
            Assert.True(actions.Apply(new SelectIntent(1, 1), (_, _) => new PickHit(box, 1) { Point = (0, 0, 5), Normal = (0, 0, 1) }, bodies.Live));
            Assert.Equal(0, actions.SelectedFace(bodies.KeyOf(box))!.Point.X, 9);

            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new PlaceBodyCommand { BodyId = box, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 20, Dy = 0, Dz = 0 })).Status);
            bodies.Apply(scene, meshes, device);
            var face = actions.SelectedFace(bodies.KeyOf(box));
            Assert.Equal("+z", face!.Reference.Element);
            Assert.Equal(20, face.Point.X, 9);
            var ends = actions.EdgesOfFace(bodies.KeyOf(box)).SelectMany(edge => edge.Segments).SelectMany(segment => new[] { segment.Start.X, segment.End.X }).ToList();
            Assert.NotEmpty(ends);
            Assert.All(ends, x => Assert.InRange(x, 15 - 1e-9, 25 + 1e-9));
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
        }
    }

    // Finding V240 of the codebase review of 2026-11-28 (TASK-0477): a creation and a PlaceBody of the body in one frame give one
    // mesh, as the rule of V55 says; until TASK-0477 the creation did not take the placement of the change, and the change
    // put the mesh up a second time.
    [VulkanImageFact]
    public async Task A_Creation_And_A_Placement_In_One_Frame_Give_One_Mesh()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        var scene = SessionScene.Create();
        var meshes = new Dictionary<string, GpuMesh>();
        using var bodies = new SessionBodies(new HostedSession(host));
        try
        {
            var box = Output(await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }), "bodyId");
            await host.Session.Apply(new PlaceBodyCommand { BodyId = box, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 20, Dy = 0, Dz = 0 });
            bodies.Apply(scene, meshes, device);
            Assert.Equal(1, bodies.Meshes);
            Assert.Equal((15.0, 25.0), (bodies.Bounds!.Value.Min.X, bodies.Bounds!.Value.Max.X));
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
        }
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");
}
