using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
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

    // V49: a point at 3e37 gives no stroke, because its pixels are beyond the limit.
    [Fact]
    public void A_Point_Far_Beyond_The_Window_Gives_No_Stroke()
    {
        var far = Guid.NewGuid();
        var sketch = new SketchInfo(SketchId, Datums.XY.Id, null, [new(SketchId, -3e37, -3e37), new(far, 3e37, 3e37)],
            [new SketchLine(Guid.NewGuid(), SketchId, far)], [], [], [], true, 2);
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 100, Target = (0, 0, 0) };

        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y, new SketchOverlay.Focus(SketchId, [], null));

        Assert.Empty(strokes.Lines);
        Assert.All(strokes.Points, spot => Assert.True(Math.Abs(spot.At.X) <= SketchOverlay.PixelLimit && Math.Abs(spot.At.Y) <= SketchOverlay.PixelLimit));
    }

    // V50: the fault of a selected body takes one line, so Show all stays in the panel on a display
    // of 1024 × 768 with two bodies.
    [Fact]
    public void The_Fault_Takes_One_Line_And_Show_All_Stays_In_The_Panel()
    {
        var small = new Vector2(1024, 768);
        var fault = new BodyFault("W-SKETCH-REBUILD-FAILED", "The body '0fe4ab0b-8d7a-5381-a0e5-152a7ab115b6' keeps its last solid: the point 'a0000000-0000-0000-0000-000000000000' is the end of 1 line, and a closed profile has 2 lines at each point.");
        LiveBody[] bodies = [new(Guid.NewGuid(), "Extrude", "Plate", true, fault), new(Guid.NewGuid(), "Box", null, true)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, small, Vector2.One, bodies, bodies[0].Id);
        }

        var line = ui.ItemRectangle(DesktopUi.FaultLabel)!.Value;
        Assert.True(line.W - line.Y < 2 * ui.ControlHeight, $"The fault takes {line.W - line.Y} pixels.");
        var showAll = ui.ItemRectangle(DesktopUi.ShowAllLabel)!.Value;
        Assert.True(showAll.W <= ui.PanelRectangles.First().W, $"Show all ends at {showAll.W}, below the panel at {ui.PanelRectangles.First().W}.");
    }

    // V48: twenty changes of one body in one frame give one mesh.
    [VulkanImageFact]
    public async Task Twenty_Changes_Of_One_Body_In_One_Frame_Give_One_Mesh()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);
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
        using var renderer = new SceneRenderer(device, target.RenderPass, host);
        renderer.Update(scene, camera, 4f / 3);
        var before = renderer.Meshes;

        for (var i = 0; i < 20; i++)
            await host.Session.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 11 + i, Y = 6 });
        renderer.Update(scene, camera, 4f / 3);

        Assert.Equal(before + 1, renderer.Meshes);
        Assert.Equal(30.0, renderer.Bounds!.Value.Max.X);
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");
}
