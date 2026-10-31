using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The findings of the desktop in the codebase review of 2026-10-29 (TASK-0106), with no GPU: V40
// and V41 the scope of the pads of the sketch, V42 the pad on a portrait display, V43 the
// buttons of the origin, V44 one read of each changed sketch, and V45 a pixel that is not finite.
[Collection(DearImGui.Collection)]
public sealed class SketchGapUiTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly Guid SketchId = Guid.Parse("50000000-0000-0000-0000-000000000000");
    private static readonly Guid OtherId = Guid.Parse("60000000-0000-0000-0000-000000000000");
    private static readonly Guid A = Guid.Parse("a0000000-0000-0000-0000-000000000000");

    private static SketchInfo Sketch(Guid id) => new(id, Datums.XY.Id, null, [new(id, 0, 0), new(A, 3, 4)], [], [], [], [], true, 2);

    private static SketchPanel Line(bool chainOpen, Guid? active = null)
    {
        var sketch = Sketch(active ?? SketchId);
        return new SketchPanel([Sketch(SketchId), Sketch(OtherId)], sketch, SketchToolKind.Line, chainOpen, [], null);
    }

    private static void Step(DesktopUi ui, SketchPanel? panel, Vector2? display = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display ?? Display, Vector2.One, [], null, null, null, panel);
    }

    private static DesktopUi Opened(SketchPanel? panel, Vector2? display = null)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui, panel, display);
        Step(ui, panel, display);
        return ui;
    }

    private static void Tap(DesktopUi ui, string item, SketchPanel? panel)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The user interface has no control {item}.");
        var (x, y) = ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
        Step(ui, panel, null, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, panel, null, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, panel);
        Step(ui, panel);
    }

    // V40: the Next point pad closes when the chain ends, when the tool changes and when another
    // sketch becomes active, because each one changes what its numbers mean.
    [Theory]
    [InlineData("end")]
    [InlineData("select")]
    [InlineData("other sketch")]
    public void The_Next_Point_Pad_Closes_When_Its_Chain_Changes(string change)
    {
        using var ui = Opened(Line(chainOpen: true));
        ui.OpenNextPointPad(chainOpen: true);
        Step(ui, Line(chainOpen: true));
        Assert.Equal(["dx", "dy"], ui.Pad!.Fields.Select(field => field.Name));

        Step(ui, change switch
        {
            "end" => Line(chainOpen: false),
            "select" => Line(chainOpen: true) with { Tool = SketchToolKind.Select },
            _ => Line(chainOpen: true, active: OtherId),
        });

        Assert.Null(ui.Pad);
    }

    // V40: the host opens the pad again in the frame of an apply, before the panel sees the
    // chain that the first point opened; the pad stays open with dx and dy.
    [Fact]
    public void The_Pad_That_The_Host_Opens_Again_Stays_Open()
    {
        using var ui = Opened(Line(chainOpen: false));
        ui.OpenNextPointPad(chainOpen: false);
        Step(ui, Line(chainOpen: false));

        ui.EndPad(null);
        ui.OpenNextPointPad(chainOpen: true);
        Step(ui, Line(chainOpen: true));
        Step(ui, Line(chainOpen: true));

        Assert.Equal(["dx", "dy"], ui.Pad!.Fields.Select(field => field.Name));
    }

    // V41: the Extrude pad closes at Finish and when another sketch becomes active, so its apply
    // never extrudes a sketch that the person did not see in the pad.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_Extrude_Pad_Closes_When_The_Active_Sketch_Changes(bool other)
    {
        var panel = Line(chainOpen: false);
        using var ui = Opened(panel);
        Tap(ui, DesktopUi.SketchItem(DesktopUi.ExtrudeLabel), panel);
        Assert.Equal(DesktopUi.ExtrudeLabel, ui.Pad!.Title);

        Step(ui, other ? Line(chainOpen: false, active: OtherId) : new SketchPanel([Sketch(SketchId)], null, SketchToolKind.Line, false, [], null));

        Assert.Null(ui.Pad);
    }

    // V42: on a portrait tablet display the pad of a sketch opens below the row of Next point, End
    // and Close, so it covers neither End nor Close.
    [Fact]
    public void On_A_Portrait_Display_The_Pad_Leaves_End_And_Close_Visible()
    {
        var portrait = new Vector2(768, 1024);
        var panel = Line(chainOpen: true);
        using var ui = Opened(panel, portrait);
        ui.OpenNextPointPad(chainOpen: true);
        Step(ui, panel, portrait);
        Step(ui, panel, portrait);

        var pad = ui.PanelRectangles.Last();
        foreach (var label in new[] { DesktopUi.EndLabel, DesktopUi.CloseLabel })
        {
            var control = ui.ItemRectangle(DesktopUi.SketchItem(label))!.Value;
            var covered = control.X < pad.Z && pad.X < control.Z && control.Y < pad.W && pad.Y < control.W;
            Assert.False(covered, $"The pad {pad} covers {label} {control}.");
        }

        Assert.True(pad.W <= portrait.Y, $"The pad ends at {pad.W}, below the display.");
    }

    // V43: a selection with the origin gives no Fixed, Move point or Delete, and a selection of
    // another point gives them.
    [Fact]
    public void The_Origin_Has_No_Fixed_No_Move_Point_And_No_Delete()
    {
        string[] labels = ["Fixed", DesktopUi.MovePointLabel, DesktopUi.DeleteLabel];
        var origin = Line(chainOpen: false) with { Tool = SketchToolKind.Select, Selection = [SketchId] };
        var point = Line(chainOpen: false) with { Tool = SketchToolKind.Select, Selection = [A] };

        using var onOrigin = Opened(origin);
        using var onPoint = Opened(point);

        Assert.All(labels, label => Assert.Null(onOrigin.ItemRectangle(DesktopUi.SketchItem(label))));
        Assert.All(labels, label => Assert.NotNull(onPoint.ItemRectangle(DesktopUi.SketchItem(label))));
    }

    // V44: twenty moves of one sketch in one frame give one read of it.
    [Fact]
    public async Task Twenty_Changes_Of_One_Sketch_Give_One_Read()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var sketch = (await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id })).Outputs;
        sketch.TryGet<Guid>("sketchId", out var id);
        (await host.Session.Apply(new AddSketchPointCommand { SketchId = id, X = 1, Y = 1 })).Outputs.TryGet<Guid>("pointId", out var point);
        using var live = new SessionSketches(host);
        live.Apply();
        var before = live.Reads;

        for (var i = 0; i < 20; i++)
            await host.Session.Apply(new MoveSketchPointCommand { SketchId = id, PointId = point, X = i, Y = 2 });
        live.Apply();

        Assert.Equal(before + 1, live.Reads);
        Assert.Equal((19.0, 2.0), live.Live.Single().Points.Where(p => p.Id == point).Select(p => (p.X, p.Y)).Single());
    }

    // V45: a point at a large finite position has no pixel that is not finite in the overlay, in
    // the strokes, the labels and the picture of the tool.
    [Fact]
    public void A_Far_Point_Gives_No_Pixel_That_Is_Not_Finite()
    {
        var far = Guid.NewGuid();
        var sketch = new SketchInfo(SketchId, Datums.XY.Id, null, [new(SketchId, 0, 0), new(far, 1e308, 1e308)],
            [new SketchLine(Guid.NewGuid(), SketchId, far)], [], [], [], true, 2);
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 100, Target = (0, 0, 0) };

        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y, new SketchOverlay.Focus(SketchId, [], null));

        static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
        Assert.All(strokes.Points, spot => Assert.True(Finite(spot.At)));
        Assert.All(strokes.Lines, line => Assert.True(Finite(line.A) && Finite(line.B)));
        Assert.All(strokes.Labels, label => Assert.True(Finite(label.At)));
        Assert.Single(strokes.Points);
        Assert.Null(SketchOverlay.Picture(sketch, camera, Display.X, Display.Y).Points.Single(p => p.Id == far).PixelX);
    }
}
