using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// The selection and the commands of the desktop (ADR-0029 items 1, 5, 6 and 8, TASK-0086), with
// no GPU. A pick function stands in for the pick of the picture, which PickingTests holds. The
// central test is the comparison of logs of anti-objective 7: a touch script and a mouse script
// give the same commands.
public class BodyActionsTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private const ulong Ms = 1_000_000;

    // Body A is under (100, 100) and body B under (300, 100). Each box has the size 1, so the hit at (100, 100) is on the
    // face +z of A, and the hit at (120, 100) on its face +x (ADR-0075 item 2).
    private static PickHit? Pick(double x, double y) => (x, y) switch
    {
        (100, 100) => new PickHit(A, 1) { Point = (0, 0, 0.5), Normal = (0, 0, 1) },
        (120, 100) => new PickHit(A, 1) { Point = (0.5, 0, 0), Normal = (1, 0, 0) },
        (300, 100) => new PickHit(B, 1) { Point = (0, 0, 0.5), Normal = (0, 0, 1) },
        _ => null,
    };

    private static async Task<EngineHost> TwoBoxes()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        foreach (var id in new[] { A, B })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = id, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        return host;
    }

    private static async Task<IReadOnlyList<LiveBody>> Live(EngineHost host)
        => await host.Session.Read((document, _) => document.Bodies.Select(body => new LiveBody(body.Handle.Id, body.Kind, body.Name, body.Visible)).ToArray());

    // Runs a script through the state machine and the actions, and gives the commands after the
    // two boxes, with their body and their value.
    private static async Task<List<string>> Log(InputSample[][] script)
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var machine = new IntentMachine();
        foreach (var frame in script)
        {
            foreach (var intent in machine.Step(frame))
                actions.Apply(intent, Pick, await Live(host));
        }

        return await host.Session.Read((document, _) => document.Log.Skip(2).Select(entry => entry.Command).Select(command => command switch
        {
            SetBodyVisibilityCommand visibility => $"SetBodyVisibility {visibility.BodyId} {visibility.Visible}",
            RenameBodyCommand rename => $"RenameBody {rename.BodyId} {rename.NewName}",
            _ => command.Name,
        }).ToList());
    }

    private static PointerSample Touch(ulong finger, double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Touch, 1, finger, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

    private static PointerSample Mouse(double x, double y, PointerButtons buttons, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, buttons, KeyModifiers.None, phase, ms * Ms);

    // Anti-objective 7 and objective 9 of docs/CHARTER.md: select A, hide B at its point, hide the
    // selection, then show all; by touch and by mouse and keyboard.
    [Fact]
    public async Task A_Touch_Script_And_A_Mouse_Script_Give_The_Same_Command_Log()
    {
        InputSample[][] touch =
        [
            [Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 100)],
            [Touch(1, 300, 100, PointerPhase.Down, 1000)], [Touch(1, 300, 100, PointerPhase.Up, 1700)],
            [new KeySample(InputKey.H, true, KeyModifiers.None, 2000 * Ms)],
            [Touch(1, 500, 500, PointerPhase.Down, 3000), Touch(2, 600, 500, PointerPhase.Down, 3000)],
            [Touch(1, 500, 500, PointerPhase.Up, 3100), Touch(2, 600, 500, PointerPhase.Up, 3100)],
        ];
        InputSample[][] mouse =
        [
            [Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(100, 100, PointerButtons.Left, PointerPhase.Up, 100)],
            [Mouse(300, 100, PointerButtons.Right, PointerPhase.Down, 1000)], [Mouse(300, 100, PointerButtons.Right, PointerPhase.Up, 1100)],
            [new KeySample(InputKey.H, true, KeyModifiers.None, 2000 * Ms)],
            [new KeySample(InputKey.H, true, KeyModifiers.Shift, 3000 * Ms)],
        ];

        var fromTouch = await Log(touch);
        var fromMouse = await Log(mouse);

        Assert.Equal(
            [$"SetBodyVisibility {B} False", $"SetBodyVisibility {A} False", $"SetBodyVisibility {A} True", $"SetBodyVisibility {B} True"],
            fromTouch);
        Assert.Equal(fromTouch, fromMouse);
    }

    [Fact]
    public async Task A_Select_Picks_A_Body_And_A_Select_Of_Nothing_Clears_The_Selection()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        Assert.True(actions.Apply(new SelectIntent(300, 100), Pick, await Live(host)));
        Assert.Equal(B, actions.Selection);
        actions.Apply(new SelectIntent(5, 5), Pick, await Live(host));
        Assert.Null(actions.Selection);
        Assert.False(actions.Apply(new OrbitIntent(1, 0), Pick, await Live(host)));
    }

    // ADR-0075 item 2 (phase R12a, TASK-0236): a second tap on the selected body selects the face under the point, a tap
    // on another face of the body moves the face, Escape and the row of the body give the body, and a tap on another
    // body selects that body with no face.
    [Fact]
    public async Task A_Second_Tap_On_The_Selected_Body_Selects_The_Face_Under_The_Point()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var live = await Live(host);

        actions.Apply(new SelectIntent(100, 100), Pick, live);
        Assert.Equal((A, (DatumReference?)null), (actions.Selection, actions.Face));
        actions.Apply(new SelectIntent(100, 100), Pick, live);
        Assert.Equal(new DatumReference(A, "+z", null), actions.Face);
        actions.Apply(new SelectIntent(120, 100), Pick, live);
        Assert.Equal(new DatumReference(A, "+x", null), actions.Face);

        // Finding 1 of the review of Codex of v0.221 (TASK-0237): the intent comes from the key, through the intent layer.
        var escape = Assert.Single(new IntentMachine().Step([new KeySample(InputKey.Escape, true, KeyModifiers.None, 0)]));
        Assert.True(actions.Apply(escape, Pick, live));
        Assert.Equal((A, (DatumReference?)null), (actions.Selection, actions.Face));
        // ADR-0100 item 3 (phase R17a, TASK-0314): Escape on the body goes up to the document; with nothing selected it gives
        // nothing. Until TASK-0314 Escape stopped at the body.
        Assert.True(actions.Apply(escape, Pick, live));
        Assert.Null(actions.Selection);
        Assert.False(actions.Apply(escape, Pick, live));

        actions.Apply(new SelectIntent(100, 100), Pick, live);
        actions.Apply(new SelectIntent(300, 100), Pick, live);
        Assert.Equal((B, (DatumReference?)null), (actions.Selection, actions.Face));
        actions.Apply(new SelectIntent(300, 100), Pick, live);
        actions.Select(B);
        Assert.Null(actions.Face);
    }

    // Objective 9 and anti-objective 7: two taps of a finger and two clicks of the mouse give the same face.
    [Fact]
    public async Task Two_Taps_Of_A_Finger_And_Two_Clicks_Give_The_Same_Face()
    {
        async Task<DatumReference?> FaceOf(InputSample[][] script)
        {
            using var host = await TwoBoxes();
            var actions = new BodyActions(host.Session);
            var machine = new IntentMachine();
            foreach (var frame in script)
            {
                foreach (var intent in machine.Step(frame))
                    actions.Apply(intent, Pick, await Live(host));
            }

            return actions.Face;
        }

        var touch = await FaceOf(
        [
            [Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 100)],
            [Touch(1, 100, 100, PointerPhase.Down, 1000)], [Touch(1, 100, 100, PointerPhase.Up, 1100)],
        ]);
        var mouse = await FaceOf(
        [
            [Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(100, 100, PointerButtons.Left, PointerPhase.Up, 100)],
            [Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 1000)], [Mouse(100, 100, PointerButtons.Left, PointerPhase.Up, 1100)],
        ]);

        Assert.Equal(new DatumReference(A, "+z", null), touch);
        Assert.Equal(touch, mouse);
    }

    // A hidden body cannot stay selected, and a selected body that leaves the session is forgotten.
    [Fact]
    public async Task A_Hidden_Or_Consumed_Body_Is_No_Longer_Selected()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Select(A);
        actions.Apply(new HideIntent(null, null), Pick, await Live(host));
        Assert.Null(actions.Selection);

        actions.Select(B);
        actions.Forget((await Live(host)).Where(body => body.Id != B).ToArray());
        Assert.Null(actions.Selection);
    }

    [Fact]
    public async Task A_Rename_Is_A_Command_Of_The_Session()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Rename(A, "Plate");

        Assert.Equal("Plate", (await Live(host)).Single(body => body.Id == A).Name);
    }

    // ADR-0032 item 4 (TASK-0094): the numbers of the pad become a CreateBox and a Translate, and the
    // new body of a move is the selection (Q3 A). The native backend runs Translate.
    [Fact]
    public async Task The_Pad_Creates_A_Box_And_Moves_The_Selected_Body()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new BodyActions(host.Session);

        Assert.Null(actions.CreateBox(40, 30, 2 * Math.PI));
        var box = Assert.Single(await Live(host)).Id;
        actions.Select(box);
        Assert.Null(actions.Move(box, 5, 0, -2));

        var moved = Assert.Single(await Live(host)).Id;
        Assert.NotEqual(box, moved);
        Assert.Equal(moved, actions.Selection);
        var (create, translate) = await host.Session.Read((document, _) => ((CreateBoxCommand)document.Log[0].Command, (TranslateCommand)document.Log[1].Command));
        Assert.Equal((40.0, 30.0, 2 * Math.PI), (create.SizeX, create.SizeY, create.SizeZ));
        Assert.Equal((box, 5.0, 0.0, -2.0), (translate.BodyId, translate.Dx, translate.Dy, translate.Dz));
    }

    // Phase K5b (ADR-0076, TASK-0244): the numbers of the pad become one CreateCylinder, and a size that is not greater
    // than zero gives its code for the pad.
    [Fact]
    public async Task The_Pad_Creates_A_Cylinder_And_A_Rejection_Gives_Its_Code()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new BodyActions(host.Session);

        Assert.Null(actions.CreateCylinder(5, 2 * Math.PI));
        Assert.StartsWith("E-GEOM-INVALID-PARAM: ", actions.CreateCylinder(0, 1), StringComparison.Ordinal);

        var body = Assert.Single(await Live(host));
        Assert.Equal("Cylinder", body.Kind);
        var create = await host.Session.Read((document, _) => (CreateCylinderCommand)Assert.Single(document.Log).Command);
        Assert.Equal((5.0, 2 * Math.PI), (create.Radius, create.Height));
    }

    // Phase K5e (ADR-0089 item 5, TASK-0298): the numbers of each pad become one CreateCone, CreateSphere or CreateTorus, and
    // a size that is not valid gives its code for the pad.
    [Fact]
    public async Task The_Pads_Create_A_Cone_A_Sphere_And_A_Torus()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new BodyActions(host.Session);

        Assert.Null(actions.CreateCone(5, 10));
        Assert.Null(actions.CreateSphere(4));
        Assert.Null(actions.CreateTorus(10, 3));
        Assert.StartsWith("E-GEOM-INVALID-PARAM: ", actions.CreateCone(0, 1), StringComparison.Ordinal);
        Assert.StartsWith("E-GEOM-INVALID-PARAM: ", actions.CreateSphere(-1), StringComparison.Ordinal);
        Assert.StartsWith("E-GEOM-INVALID-PARAM: ", actions.CreateTorus(3, 3), StringComparison.Ordinal);

        Assert.Equal(["Cone", "Sphere", "Torus"], (await Live(host)).Select(body => body.Kind).Order(StringComparer.Ordinal));
        var log = await host.Session.Read((document, _) => document.Log.Select(entry => entry.Command).ToList());
        Assert.Equal((5.0, 10.0), (Assert.Single(log.OfType<CreateConeCommand>()).Radius, Assert.Single(log.OfType<CreateConeCommand>()).Height));
        Assert.Equal(4.0, Assert.Single(log.OfType<CreateSphereCommand>()).Radius);
        Assert.Equal((10.0, 3.0), (Assert.Single(log.OfType<CreateTorusCommand>()).Radius, Assert.Single(log.OfType<CreateTorusCommand>()).TubeRadius));
    }

    // Finding V108 of the codebase review of 2026-11-06 (d) (TASK-0243): a lost pointer gives CancelIntent with no key, and
    // the face stays; only the key Escape goes back to the body.
    [Fact]
    public async Task A_Lost_Pointer_Keeps_The_Selected_Face()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var live = await Live(host);
        actions.Apply(new SelectIntent(100, 100), Pick, live);
        actions.Apply(new SelectIntent(100, 100), Pick, live);
        var face = actions.Face;
        Assert.NotNull(face);

        var machine = new IntentMachine();
        machine.Step([new PointerSample(DeviceKind.Touch, 1, 1, 10, 10, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0)]);
        var lost = Assert.Single(machine.Step([new FocusLostSample(10)]));

        Assert.False(actions.Apply(lost, Pick, live));
        Assert.Equal(face, actions.Face);
    }

    // Finding V109 of the codebase review of 2026-11-06 (d) (TASK-0243): with the geometry key of the body, the entry of the
    // selected face comes from the first query, so a frame does not wait for the serial section that a long command holds.
    [Fact]
    public async Task The_Entry_Of_The_Face_Does_Not_Wait_For_A_Long_Command()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var live = await Live(host);
        actions.Apply(new SelectIntent(100, 100), Pick, live);
        actions.Apply(new SelectIntent(100, 100), Pick, live);
        var key = Guid.NewGuid();
        var first = actions.SelectedFace(key);
        Assert.NotNull(first);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        // The read runs its delegate on the thread that calls it, so the hold runs on a thread of the pool.
        var held = Task.Run(() => host.Session.Read((_, _) =>
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(10));
            return 0;
        }));
        // P0.104 (TASK-0256): run 42313242154 on the Windows runner did not start the read in 5 seconds; the start is a
        // condition of the test, not its claim, so it waits 30 seconds.
        Assert.True(entered.Wait(TimeSpan.FromSeconds(30)), "The read did not start.");
        try
        {
            var again = Task.Run(() => actions.SelectedFace(key));
            Assert.Same(again, await Task.WhenAny(again, Task.Delay(TimeSpan.FromSeconds(2))));
            Assert.Same(first, await again);
        }
        finally
        {
            release.Set();
            await held;
        }
    }

    // Finding V113 of the codebase review of 2026-11-06 (e) (TASK-0252): the probe of the review. A rebuild that fails keeps
    // the geometry key and gives the body a fault; the next frame of the host gives the live bodies to Forget, and the kept
    // entry of the face is gone, so the face is no longer selected.
    [Fact]
    public async Task A_Fault_Of_The_Body_Clears_The_Kept_Face()
    {
        static Guid Out(CommandResult result, string name)
        {
            Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
            return value;
        }

        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        PickHit? Top(double x, double y) => new PickHit(body, 1) { Point = (5, 3, 3), Normal = (0, 0, 1) };
        var actions = new BodyActions(host.Session);
        var key = Guid.NewGuid();
        actions.Apply(new SelectIntent(0, 0), Top, []);
        actions.Apply(new SelectIntent(0, 0), Top, []);
        Assert.NotNull(actions.SelectedFace(key));

        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new DeleteSketchItemCommand { SketchId = sketch, ItemId = lines[0] })).Status);
        var fault = await host.Session.Read((document, _) => document.Bodies.Single(candidate => candidate.Handle.Id == body).Fault);
        Assert.NotNull(fault);
        actions.Forget([new LiveBody(body, "Extrude", null, true, fault)]);

        Assert.Null(actions.SelectedFace(key));
        Assert.Equal((body, (DatumReference?)null), (actions.Selection, actions.Face));
    }

    // Finding V107 of the codebase review of 2026-11-06 (d) (TASK-0243): the outline of a face with the offset of a drag is
    // the outline of the moved face, as the renderer moves the mesh; with no offset it does not move.
    [Fact]
    public void The_Outline_Of_The_Face_Follows_The_Offset_Of_A_Drag()
    {
        var camera = new Camera { YawDegrees = 30, PitchDegrees = 20, Distance = 50, Target = (0, 0, 0) };
        FaceSegment Segment((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => new(new DatumPoint(a.X, a.Y, a.Z), new DatumPoint(b.X, b.Y, b.Z));
        IReadOnlyList<FaceSegment> outline = [Segment((-1, -1, 1), (1, -1, 1)), Segment((1, -1, 1), (1, 1, 1))];
        IReadOnlyList<FaceSegment> moved = [Segment((2, -1, 1.5), (4, -1, 1.5)), Segment((4, -1, 1.5), (4, 1, 1.5))];

        Assert.Equal(FaceOverlay.Of(moved, camera, 800, 600), FaceOverlay.Of(outline, camera, 800, 600, (3, 0, 0.5)));
        Assert.Equal(FaceOverlay.Of(outline, camera, 800, 600), FaceOverlay.Of(outline, camera, 800, 600, default));
        Assert.NotEqual(FaceOverlay.Of(outline, camera, 800, 600), FaceOverlay.Of(moved, camera, 800, 600));
    }

    // ADR-0032 item 8: a rejection gives its code for the pad, and the selection stays.
    [Fact]
    public async Task A_Rejected_Pad_Command_Gives_Its_Code()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.Select(A);

        Assert.StartsWith("E-GEOM-INVALID-PARAM: ", actions.CreateBox(0, 1, 1), StringComparison.Ordinal);
        Assert.StartsWith("E-GEOM-CAP-MISSING: ", actions.Move(A, 1, 0, 0), StringComparison.Ordinal);
        Assert.Equal(A, actions.Selection);
    }

    // ADR-0031 item 11 (TASK-0093): an undo and a redo are commands of the session; an applied
    // move gives no line, and a rejection gives its code for the line of the panel.
    [Fact]
    public async Task An_Undo_And_A_Redo_Are_Commands_Of_The_Session_And_A_Rejection_Gives_Its_Code()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.Select(B);

        Assert.Null(actions.Undo());
        Assert.Equal([A], (await Live(host)).Select(body => body.Id));
        actions.Forget(await Live(host));
        Assert.Null(actions.Selection);

        Assert.Null(actions.Redo());
        Assert.Equal([A, B], (await Live(host)).Select(body => body.Id).Order());
        Assert.StartsWith("E-CMD-NOTHING-TO-REDO: ", actions.Redo(), StringComparison.Ordinal);
        Assert.Equal(["Undo", "Redo"], await host.Session.Read((document, _) => document.Log.Skip(2).Select(entry => entry.Command.Name).ToArray()));
    }

    // A hide with no selection and no body under the point sends nothing.
    [Fact]
    public async Task A_Hide_Of_Nothing_Sends_No_Command()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Apply(new HideIntent(null, null), Pick, await Live(host));
        actions.Apply(new HideIntent(5, 5), Pick, await Live(host));

        Assert.Equal(2, await host.Session.Read((document, _) => document.Version));
    }

    // ADR-0029 item 8 (decision KD6): the camera frames only when the set of visible bodies goes
    // from empty to not empty.
    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 3, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 2, false)]
    [InlineData(2, 1, false)]
    [InlineData(1, 0, false)]
    public void The_Camera_Frames_Only_When_The_First_Visible_Body_Comes(int before, int after, bool frames)
        => Assert.Equal(frames, SceneRenderer.ShouldFrame(before, after));
}
