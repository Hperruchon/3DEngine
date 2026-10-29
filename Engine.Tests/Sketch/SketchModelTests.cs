using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;

namespace Engine.Tests.Sketch;

// The sketch model in the engine (ADR-0033, TASK-0100). ADR-0033 names this file in
// enforced-by. The datums, the frame, the seven commands, the residual of the kinds 1 to 5,
// each refusal, the events and their order, undo and redo, the document file and a replay.
public sealed class SketchModelTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sketch-" + Guid.NewGuid().ToString("N"));

    public SketchModelTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : Guid.Empty;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static async Task<SketchInfo> Sketch(DocumentSession session, Guid id)
    {
        var result = await session.Query<SketchInfo>(new GetSketchQuery { SketchId = id });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!;
    }

    // A sketch as text, in a fixed form, to compare two states.
    private static string Text(SketchInfo sketch)
        => $"{sketch.SketchId} {sketch.DatumId} {sketch.Name} | "
            + string.Join(" ", sketch.Points.Select(p => $"{p.Id}:{BitConverter.DoubleToInt64Bits(p.X)},{BitConverter.DoubleToInt64Bits(p.Y)}")) + " | "
            + string.Join(" ", sketch.Lines.Select(l => $"{l.Id}:{l.Start}-{l.End}")) + " | "
            + string.Join(" ", sketch.Constraints.Select(c => $"{c.Id}:{c.Kind}:{string.Join(",", c.Items)}:{string.Join(",", c.Values.Select(BitConverter.DoubleToInt64Bits))}"));

    // A triangle-free scene: a sketch on XZ, three points, two lines and one constraint of
    // each kind 1 to 5.
    private sealed record Scene(Guid Sketch, Guid A, Guid B, Guid C, Guid AB, Guid BC, Guid[] Constraints);

    private static async Task<Scene> BuildScene(DocumentSession session)
    {
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XZ.Id, SketchName = "Profile" }, "sketchId");
        var a = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }, "pointId");
        var b = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 30, Y = 1 }, "pointId");
        var c = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 30, Y = 20 }, "pointId");
        var ab = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = a, EndPointId = b }, "lineId");
        var bc = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = b, EndPointId = c }, "lineId");
        Guid[] constraints =
        [
            await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "coincident", First = a, Second = sketch }, "constraintId"),
            await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "horizontal", First = ab }, "constraintId"),
            await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "vertical", First = bc }, "constraintId"),
            await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = bc, Value = 20 }, "constraintId"),
            await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "fixed", First = c, Value = 30, SecondValue = 20 }, "constraintId"),
        ];
        return new Scene(sketch, a, b, c, ab, bc, constraints);
    }

    // ADR-0033 item 1: three origin planes with fixed identifiers and right-handed frames.
    [Fact]
    public void Each_Datum_Is_A_Right_Handed_Frame_With_A_Fixed_Identifier()
    {
        Assert.Equal(["00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000003"],
            Datums.All.Select(datum => datum.Id.ToString()));
        foreach (var datum in Datums.All)
        {
            var (u, v, n) = (datum.U, datum.V, datum.Normal);
            Assert.Equal((n.X, n.Y, n.Z), ((u.Y * v.Z) - (u.Z * v.Y), (u.Z * v.X) - (u.X * v.Z), (u.X * v.Y) - (u.Y * v.X)));
        }
    }

    // ADR-0033 items 2, 3, 5 and 7: the frame, the identifiers, and the residual of each
    // kind. The solver satisfies the five constraints (ADR-0035); a value that conflicts
    // with the others keeps the positions and shows its residual.
    [Fact]
    public async Task A_Sketch_Holds_Its_Frame_Its_Items_And_The_Residual_Of_Each_Constraint()
    {
        var (session, _) = NewSession();
        var scene = await BuildScene(session);

        var sketch = await Sketch(session, scene.Sketch);
        Assert.Equal((Datums.XZ.Id, "Profile"), (sketch.DatumId, sketch.Name));
        Assert.Equal([scene.Sketch, scene.A, scene.B, scene.C], sketch.Points.Select(point => point.Id));
        Assert.Equal((0.0, 0.0), (sketch.Points[0].X, sketch.Points[0].Y));
        Assert.Equal([scene.AB, scene.BC], sketch.Lines.Select(line => line.Id));
        Assert.Equal(scene.Constraints, sketch.Constraints.Select(constraint => constraint.Id));
        Assert.All(sketch.Constraints, constraint => Assert.Equal(0, constraint.Residual, 9));
        Assert.Equal((30.0, 0.0), (Math.Round(sketch.Points[2].X, 9), Math.Round(sketch.Points[2].Y, 9)));
        Assert.Equal((true, 0), (sketch.Solved, sketch.Freedom));

        // B is on the vertical line through the fixed C, and A is at the origin, so a length of 25
        // for BC conflicts with the horizontal AB.
        await Applied(session, new SetSketchConstraintValueCommand { SketchId = scene.Sketch, ConstraintId = scene.Constraints[3], Value = 25 }, "constraintId");
        var conflict = await Sketch(session, scene.Sketch);
        Assert.Equal(-5.0, conflict.Constraints[3].Residual, 9);
        Assert.False(conflict.Solved);
    }

    // The distance of a point to a line, and of two points.
    [Fact]
    public async Task A_Distance_Measures_A_Point_To_A_Line_And_Two_Points()
    {
        var (session, _) = NewSession();
        var scene = await BuildScene(session);
        var toLine = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "distance", First = scene.C, Second = scene.AB, Value = 0 }, "constraintId");
        var toPoint = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "distance", First = scene.A, Second = scene.C, Value = 0 }, "constraintId");

        // The two lengths of zero conflict with the fixed C, so the positions stay solved for
        // the five constraints of the scene: C (30, 20) and the line from (0, 0) to (30, 0).
        var constraints = (await Sketch(session, scene.Sketch)).Constraints;
        Assert.Equal(20, constraints.Single(c => c.Id == toLine).Residual, 9);
        Assert.Equal(Math.Sqrt(1300), constraints.Single(c => c.Id == toPoint).Residual, 9);
    }

    // ADR-0033 item 6: a point goes with each line that references it, and each constraint
    // that references a removed item goes too.
    [Fact]
    public async Task A_Delete_Of_A_Point_Removes_Its_Lines_And_Their_Constraints()
    {
        var (session, _) = NewSession();
        var scene = await BuildScene(session);

        await Applied(session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.B }, "itemId");
        var sketch = await Sketch(session, scene.Sketch);

        Assert.Equal([scene.Sketch, scene.A, scene.C], sketch.Points.Select(point => point.Id));
        Assert.Empty(sketch.Lines);
        Assert.Equal([scene.Constraints[0], scene.Constraints[4]], sketch.Constraints.Select(constraint => constraint.Id));
    }

    // ADR-0033 items 2 and 6: each refusal and its code. A refusal does not change the version.
    [Fact]
    public async Task Each_Refusal_Gives_Its_Code_And_Changes_Nothing()
    {
        var (session, _) = NewSession();
        var scene = await BuildScene(session);
        var before = await session.Read((document, _) => document.Version);
        var text = Text(await Sketch(session, scene.Sketch));
        var s = scene.Sketch;

        Assert.Equal(DiagnosticCodes.SketchDatumNotFound, await Refused(session, new CreateSketchCommand { DatumId = Guid.NewGuid() }));
        Assert.Equal(DiagnosticCodes.SketchNameInvalid, await Refused(session, new CreateSketchCommand { DatumId = Datums.XY.Id, SketchName = "a\u0001b" }));
        Assert.Equal(DiagnosticCodes.SketchNotFound, await Refused(session, new AddSketchPointCommand { SketchId = Guid.NewGuid(), X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new AddSketchPointCommand { SketchId = s, X = double.NaN, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new AddSketchLineCommand { SketchId = s, StartPointId = scene.A, EndPointId = scene.AB }));
        Assert.Equal(DiagnosticCodes.SketchLineInvalid, await Refused(session, new AddSketchLineCommand { SketchId = s, StartPointId = scene.A, EndPointId = scene.A }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "horizontal", First = Guid.NewGuid() }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "parallel", First = scene.AB, Second = scene.BC }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "glue", First = scene.A }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "horizontal", First = scene.A }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "coincident", First = scene.A, Second = scene.A }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "distance", First = scene.AB, Value = -1 }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "distance", First = scene.AB }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "fixed", First = scene.A, Value = 1 }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "coincident", First = scene.A, Third = scene.B }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new SetSketchConstraintValueCommand { SketchId = s, ConstraintId = scene.Constraints[1], Value = 3 }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new SetSketchConstraintValueCommand { SketchId = s, ConstraintId = scene.A, Value = 3 }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new MoveSketchPointCommand { SketchId = s, PointId = s, X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new DeleteSketchItemCommand { SketchId = s, ItemId = s }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new DeleteSketchItemCommand { SketchId = s, ItemId = Guid.NewGuid() }));

        Assert.Equal(before, await session.Read((document, _) => document.Version));
        Assert.Equal(text, Text(await Sketch(session, s)));
        Assert.Equal(DiagnosticCodes.SketchNotFound, (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = Guid.NewGuid() })).Error!.Code);
    }

    // ADR-0033 item 9: the kinds, the payloads and the order of the sketch events, in a
    // commit, an undo and a redo.
    [Fact]
    public async Task Each_Commit_Gives_Its_Sketch_Events_After_Its_Body_Events()
    {
        var (session, events) = NewSession();

        async Task<List<EventRecord>> Events(Command command)
        {
            var before = events.Snapshot().Count;
            var result = await session.Apply(command);
            Assert.Equal(CommandStatus.Applied, result.Status);
            return events.Snapshot().Skip(before).ToList();
        }

        var created = await Events(new CreateSketchCommand { DatumId = Datums.YZ.Id });
        var sketch = (Guid)((IReadOnlyDictionary<string, object?>)created[1].Payload!)["sketchId"]!;
        var changed = await Events(new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 2 });
        var undone = await Events(new UndoCommand());
        var undoneTwice = await Events(new UndoCommand());
        var redone = await Events(new RedoCommand());

        Assert.Equal(["command.applied", "sketch.created"], created.Select(record => record.Kind));
        var payload = (IReadOnlyDictionary<string, object?>)created[1].Payload!;
        Assert.Equal((Datums.YZ.Id, (string?)null), ((Guid)payload["datumId"]!, (string?)payload["name"]));
        Assert.Equal(["command.applied", "sketch.changed"], changed.Select(record => record.Kind));
        Assert.Equal(["command.applied", "sketch.changed"], undone.Select(record => record.Kind));
        Assert.Equal(["command.applied", "sketch.removed"], undoneTwice.Select(record => record.Kind));
        Assert.Equal(["command.applied", "sketch.created"], redone.Select(record => record.Kind));
        Assert.All(created.Concat(changed).Concat(undone).Concat(undoneTwice).Concat(redone).Where(record => record.Kind.StartsWith("sketch.", StringComparison.Ordinal)),
            record => Assert.Equal(sketch, ((IReadOnlyDictionary<string, object?>)record.Payload!)["sketchId"]));
    }

    // ADR-0033 item 10: each undo gives the record before its step, and each redo the record
    // after it; an undo of CreateSketch removes the sketch.
    [Fact]
    public async Task Each_Undo_Gives_The_Sketch_Before_Its_Step_And_Each_Redo_The_Sketch_After_It()
    {
        var (session, _) = NewSession();
        var scene = await BuildScene(session);

        async Task<string> State()
            => await session.Read((document, _) => document.Sketches.Count) == 0 ? "none" : Text(await Sketch(session, scene.Sketch));

        // Each command of the scene is one step: the sketch, three points, two lines and
        // five constraints. The undos give the state before each step, from the last.
        const int steps = 11;
        var states = new List<string> { await State() };
        for (var step = 0; step < steps; step++)
        {
            await Applied(session, new UndoCommand(), "commandId");
            states.Insert(0, await State());
        }

        Assert.Equal("none", states[0]);
        Assert.Equal(steps + 1, states.Distinct().Count());
        for (var step = 1; step <= steps; step++)
        {
            await Applied(session, new RedoCommand(), "commandId");
            Assert.Equal(states[step], await State());
        }
    }

    // ADR-0015 and ADR-0033 item 6: the document file holds each sketch command, and an
    // open gives the same sketch, with an undo after the open.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Sketch()
    {
        var path = Path.Combine(_folder, "sketch" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var scene = await BuildScene(live);
        await Applied(live, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Constraints[2] }, "itemId");
        await Applied(live, new UndoCommand(), "commandId");
        var expected = Text(await Sketch(live, scene.Sketch));
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = Text(await Sketch(opened, scene.Sketch));
        await Applied(opened, new RedoCommand(), "commandId");
        var redone = await Sketch(opened, scene.Sketch);
        opened.Close();

        Assert.Equal(expected, actual);
        Assert.DoesNotContain(redone.Constraints, constraint => constraint.Id == scene.Constraints[2]);
    }

    // ADR-0033 item 8: a handler keeps the datum of a sketch, so a handler that changes it
    // is a defect, and the bus throws before the commit.
    [Fact]
    public async Task The_Bus_Refuses_A_Handler_That_Changes_The_Datum_Of_A_Sketch()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        kit.CommandRegistry.Register(new DatumChangingHandler());
        var session = new DocumentSession(kit);
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.Apply(new DatumChange { SketchId = sketch }));
        Assert.Equal(Datums.XY.Id, (await Sketch(session, sketch)).DatumId);
    }

    private sealed record DatumChange : Command
    {
        public override string Name => "ProbeDatumChange";
        public override int SchemaVersion => 1;
        public required Guid SketchId { get; init; }
    }

    private sealed class DatumChangingHandler : Engine.Contracts.Handlers.ICommandHandler
    {
        public string CommandName => "ProbeDatumChange";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, Engine.Contracts.Schema.FieldSchema> Parameters { get; } = new Dictionary<string, Engine.Contracts.Schema.FieldSchema>();
        public IReadOnlyDictionary<string, Engine.Contracts.Schema.FieldSchema> Outputs { get; } = new Dictionary<string, Engine.Contracts.Schema.FieldSchema>();
        public Command Create(Engine.Contracts.Handlers.CommandInput input) => throw new NotSupportedException();

        public Task<Engine.Contracts.Handlers.CommandHandlerResult> Handle(Command command, Document document, Engine.Contracts.Geometry.IGeometryBackend backend, CancellationToken ct)
        {
            var sketch = document.Sketches.Single(candidate => candidate.Id == ((DatumChange)command).SketchId);
            return Task.FromResult(Engine.Contracts.Handlers.CommandHandlerResult.Success(Engine.Contracts.Outputs.Empty, changedSketches: [sketch with { DatumId = Datums.YZ.Id }]));
        }
    }
}
