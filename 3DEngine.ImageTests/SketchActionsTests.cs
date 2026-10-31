using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// The sketch tools of the desktop with a session and no GPU (ADR-0034 items 2 to 7 and 10,
// TASK-0101). The central test is the comparison of logs of anti-objective 7: a touch script and
// a mouse script that draw one closed rectangle give the same commands.
public class SketchActionsTests
{
    private const double Width = 1280;
    private const double Height = 720;
    private const ulong Ms = 1_000_000;

    // The camera looks along -z at the plane XY, as the sketch mode turns it (ADR-0034 item 6).
    private static Camera Facing() => new() { Target = (0, 0, 0), Distance = 100, YawDegrees = 0, PitchDegrees = 0 };

    // The corners of a rectangle in pixels, and the first corner again to close it.
    private static readonly (double X, double Y)[] Corners = [(440, 460), (840, 460), (840, 260), (440, 260), (444, 457)];

    private static PointerSample Touch(double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Touch, 1, 5, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

    private static PointerSample Mouse(double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * Ms);

    private static InputSample[][] Script(Func<double, double, PointerPhase, ulong, PointerSample> sample)
        => [.. Corners.SelectMany((corner, index) => new InputSample[][]
        {
            [sample(corner.X, corner.Y, PointerPhase.Down, (ulong)(1000 * index))],
            [sample(corner.X, corner.Y, PointerPhase.Up, (ulong)(1000 * index + 100))],
        })];

    // Runs a script through the machine in the mode Sketch, the tool and the actions, and gives the
    // commands after CreateSketch. A point is named by the order of its command, so two runs with
    // two sets of identifiers compare.
    private static async Task<List<string>> Log(InputSample[][] script)
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        Assert.NotNull(actions.Create(Datums.XY.Id));
        var machine = new IntentMachine { Mode = IntentMode.Sketch };
        foreach (var frame in script)
        {
            foreach (var intent in machine.Step(frame))
            {
                if (intent is SketchPointIntent point)
                    actions.Tap(Facing(), point.X, point.Y, Width, Height);
            }
        }

        return await host.Session.Read((document, _) =>
        {
            var names = new Dictionary<Guid, string>();
            var log = new List<string>();
            foreach (var command in document.Log.Skip(1).Select(entry => entry.Command))
            {
                switch (command)
                {
                    case AddSketchPointCommand point:
                        names[point.CommandId] = $"p{names.Count + 1}";
                        log.Add($"point {point.X} {point.Y}");
                        break;
                    case AddSketchLineCommand line:
                        log.Add($"line {names[line.StartPointId]} {names[line.EndPointId]}");
                        break;
                    default:
                        log.Add(command.Name);
                        break;
                }
            }

            return log;
        });
    }

    [Fact]
    public async Task A_Touch_Script_And_A_Mouse_Script_Draw_The_Same_Closed_Rectangle()
    {
        var fromTouch = await Log(Script(Touch));
        var fromMouse = await Log(Script(Mouse));

        Assert.Equal(fromMouse, fromTouch);
        // Four points, four lines, and the last line ends on the first point. One millimetre is
        // about 6.2 pixels at this zoom, so the step is 2 mm and each coordinate is a multiple of
        // it (ADR-0034 item 3): the pixel 440 is at -32.07 mm, and it rounds to -32.
        Assert.Equal(
            ["point -32 -16", "point 32 -16", "line p1 p2", "point 32 16", "line p2 p3", "point -32 16", "line p3 p4", "line p4 p1"],
            fromTouch);
    }

    // ADR-0034 item 7: the pad draws a rectangle of exact sizes with four points and Close.
    [Fact]
    public async Task The_Pad_Draws_An_Exact_Rectangle()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XZ.Id);

        foreach (var (a, b) in new[] { (5.0, 5.0), (40, 0), (0, 20), (-40, 0) })
            Assert.Null(actions.Next(a, b));
        actions.Close();

        var sketch = actions.Current()!;
        Assert.Equal([(0.0, 0.0), (5, 5), (45, 5), (45, 25), (5, 25)], sketch.Points.Select(point => (point.X, point.Y)));
        Assert.Equal(4, sketch.Lines.Count);
        Assert.Equal(sketch.Points[1].Id, sketch.Lines[^1].End);
    }

    // ADR-0034 item 8: the five kinds of K2a from the actions, each with its residual. The solver
    // satisfies the first four (ADR-0035); a vertical line of length 30 then conflicts with them.
    [Fact]
    public async Task Each_Kind_Of_Constraint_Is_Added_With_Its_Residual()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.YZ.Id);
        actions.Next(5, 0);
        actions.Next(30, 1);
        var sketch = actions.Current()!;
        var (origin, a, b, line) = (sketch.Points[0].Id, sketch.Points[1].Id, sketch.Points[2].Id, sketch.Lines[0].Id);

        Assert.Null(actions.AddConstraint("horizontal", [line], []));
        Assert.Null(actions.AddConstraint("distance", [line], [30]));
        Assert.Null(actions.AddConstraint("fixed", [b], [30, 0]));
        Assert.Null(actions.AddConstraint("coincident", [origin, a], []));
        Assert.True(actions.Current()!.Solved);
        Assert.Null(actions.AddConstraint("vertical", [line], []));
        Assert.StartsWith("E-SKETCH-CONSTRAINT-INVALID", actions.AddConstraint("fixed", [line], [0, 0]));

        var residuals = actions.Current()!.Constraints.Select(constraint => (constraint.Kind, constraint.Residual)).ToArray();
        Assert.Equal(["horizontal", "distance", "fixed", "coincident", "vertical"], residuals.Select(residual => residual.Kind));
        Assert.All(residuals[..4], residual => Assert.Equal(0, residual.Residual!.Value, 9));
        Assert.Equal(30, residuals[4].Residual!.Value, 9);
        Assert.False(actions.Current()!.Solved);
    }

    // ADR-0034 items 5 and 6: an undo of the last point ends the chain, and an undo of CreateSketch
    // ends the mode.
    [Fact]
    public async Task An_Undo_Ends_The_Chain_Then_The_Mode()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var live = new SessionSketches(host);
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);
        actions.Next(1, 1);
        Assert.NotNull(actions.Tool.Last);

        await host.Session.Apply(new UndoCommand());
        live.Apply();
        actions.Follow(live.Live);
        Assert.Null(actions.Tool.Last);
        Assert.NotNull(actions.Active);

        await host.Session.Apply(new UndoCommand());
        live.Apply();
        actions.Follow(live.Live);
        Assert.Null(actions.Active);
    }

    // A delete of a point and of its line gives no message: the line left with its point.
    [Fact]
    public async Task A_Delete_Of_A_Point_And_Its_Line_Gives_No_Message()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);
        actions.Next(5, 0);
        actions.Next(10, 0);
        var sketch = actions.Current()!;

        actions.Delete([sketch.Points[2].Id, sketch.Lines[0].Id]);

        Assert.Null(actions.Message);
        Assert.Equal((2, 0), (actions.Current()!.Points.Count, actions.Current()!.Lines.Count));
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new UndoCommand())).Status);
    }
    // ADR-0036 item 9: an extrude of an open chain keeps the mode and gives the code for the pad, and
    // an extrude of the closed rectangle ends the mode and gives the new body.
    [Fact]
    public async Task An_Extrude_Ends_The_Mode_And_Gives_The_New_Body()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);
        foreach (var (a, b) in new[] { (0.0, 0.0), (10, 0), (0, 6), (-10, 0) })
            Assert.Null(actions.Next(a, b));

        var (rejection, none) = actions.Extrude(5);
        Assert.StartsWith("E-SKETCH-PROFILE-INVALID", rejection);
        Assert.Null(none);
        Assert.NotNull(actions.Active);

        actions.Close();
        var (closed, body) = actions.Extrude(5);
        Assert.Null(closed);
        Assert.Null(actions.Active);
        var created = await host.Session.Read((document, _) => document.Bodies.Single());
        Assert.Equal((body, "Extrude"), (created.Handle.Id, created.Kind));
    }

    // ADR-0041 item 6 (phase R11b, TASK-0134): the mark under the mouse is the pixel of the point that a
    // tap there would take: the midpoint of a line near its middle, and no mark for the tool Select.
    [Fact]
    public void The_Mark_Is_The_Pixel_Of_The_Point_That_A_Tap_Would_Take()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);
        var (a, b) = (actions.AddPoint(-20, 0)!.Value, actions.AddPoint(20, 0)!.Value);
        actions.AddLine(a, b);
        var camera = Facing();
        var sketch = actions.Current()!;
        var middle = SketchOverlay.Pixel(sketch, camera, Width, Height, (0, 0))!.Value;

        var mark = actions.Mark(camera, middle.X + 9, middle.Y + 7, Width, Height);

        Assert.Equal(middle, mark);
        var start = SketchOverlay.Pixel(sketch, camera, Width, Height, (-20, 0))!.Value;
        Assert.Equal(start, actions.Mark(camera, start.X + 5, start.Y - 4, Width, Height));
        actions.Tool.Kind = SketchToolKind.Select;
        Assert.Null(actions.Mark(camera, middle.X + 9, middle.Y + 7, Width, Height));
    }

    // Phase K2e (TASK-0122): a touch script of the tools Circle and Arc through the session adds a
    // circle and an arc, the sketch is solved, and a tap of the tool Select on each curve picks it.
    [Fact]
    public async Task The_Tools_Circle_And_Arc_Add_Curves_That_Select_Picks()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);
        var camera = Facing();
        void Taps(params (double X, double Y)[] pixels)
        {
            foreach (var (x, y) in pixels)
                actions.Tap(camera, x, y, Width, Height);
        }

        actions.Tool.Kind = SketchToolKind.Circle;
        Taps((440, 360), (540, 360));
        actions.Tool.Kind = SketchToolKind.Arc;
        Taps((840, 360), (940, 360), (840, 260));

        var sketch = actions.Current()!;
        var circle = Assert.Single(sketch.Circles);
        var arc = Assert.Single(sketch.Arcs);
        Assert.True(circle.Radius > 0);
        Assert.True(sketch.Solved);
        Assert.Equal(4, await host.Session.Read((document, _) => document.Sketches.Single().Points.Count - 1));

        // A vertex a quarter turn along each curve, away from each point.
        actions.Tool.Kind = SketchToolKind.Select;
        var picture = SketchOverlay.Picture(sketch, camera, Width, Height);
        Taps(picture.Curves.Single(curve => curve.Id == circle.Id).Pixels[SketchOverlay.TurnSegments / 4].A);
        Assert.Equal([circle.Id], actions.Tool.Selection);
        Taps(picture.Curves.Single(curve => curve.Id == arc.Id).Pixels[SketchOverlay.TurnSegments / 8].A);
        Assert.Equal([circle.Id, arc.Id], actions.Tool.Selection);
    }
}
