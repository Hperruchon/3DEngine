using System.Text.Json;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;

namespace Engine.Tests.Sketch;

// The findings of the sketch in the codebase review of 2026-10-29 (TASK-0106): E86 an identifier
// in use, E87 a dropped hold, E88 a residual that is not finite, E91 the read-only lists of a
// record, and E92 the reason of two points at one position.
public sealed class SketchGapTests
{
    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static async Task<SketchInfo> Sketch(DocumentSession session, Guid id)
        => (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!;

    // The cache of the bus forgets a command after 1,024 other results (E17), so a client that
    // sends one command again after them reaches the handler a second time.
    private static async Task Forget(DocumentSession session)
    {
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand());
    }

    // E86: each command that adds an item is refused when the log holds its identifier, and
    // the sketch keeps one item with that identifier.
    [Fact]
    public async Task A_Command_Sent_Again_After_The_Cache_Forgot_It_Is_Refused()
    {
        var session = NewSession();
        var create = new CreateSketchCommand { DatumId = Datums.XY.Id };
        var sketch = Output(await Applied(session, create), "sketchId");
        var a = new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 };
        var b = new AddSketchPointCommand { SketchId = sketch, X = 4, Y = 0 };
        await Applied(session, a);
        await Applied(session, b);
        var line = new AddSketchLineCommand { SketchId = sketch, StartPointId = a.CommandId, EndPointId = b.CommandId };
        await Applied(session, line);
        var horizontal = new AddSketchConstraintCommand { SketchId = sketch, Kind = "horizontal", First = line.CommandId };
        await Applied(session, horizontal);
        var before = await session.Read((document, _) => document.Version);

        await Forget(session);

        foreach (Command again in new Command[] { create, a, line, horizontal })
        {
            var result = await session.Apply(again);
            Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.SketchIdInUse), (result.Status, result.Error?.Code));
        }

        var info = await Sketch(session, sketch);
        Assert.Equal((3, 1, 1), (info.Points.Count, info.Lines.Count, info.Constraints.Count));
        Assert.Equal(1, await session.Read((document, _) => document.Sketches.Count));
        Assert.Equal(before, await session.Read((document, _) => document.Version));
    }

    // E87: a move of a fixed point cannot hold the point, so the solver moves it back and the
    // result says so; a move that the solver can hold gives no warning.
    [Fact]
    public async Task A_Move_That_The_Solver_Cannot_Hold_Gives_A_Warning()
    {
        var session = NewSession();
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var point = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 5, Y = 5 }), "pointId");
        var free = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 1 }), "pointId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "fixed", First = point, Value = 5, SecondValue = 5 });

        var moved = await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = point, X = 9, Y = 9 });

        var warning = Assert.Single(moved.Diagnostics);
        Assert.Equal((DiagnosticCodes.SketchHoldDropped, Severity.Warning, (object?)point), (warning.Code, warning.Severity, warning.Data!["pointId"]));
        var at = (await Sketch(session, sketch)).Points.Single(p => p.Id == point);
        Assert.Equal((5.0, 5.0), (at.X, at.Y));

        var held = await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = free, X = 2, Y = 3 });
        Assert.Empty(held.Diagnostics);
    }

    // E87: when no solve converges, the record keeps the position of the command, so the
    // point is where the person put it and no warning comes.
    [Fact]
    public void A_Hold_Is_Dropped_Only_When_The_Free_Solve_Converges()
    {
        var sketch = SketchModel.New(Guid.NewGuid(), Datums.XY.Id, null);
        var p = new SketchPoint(Guid.NewGuid(), 9, 9);
        var fixedAt = new SketchConstraint(Guid.NewGuid(), "fixed", [p.Id], [5, 5]);
        var alone = sketch with { Points = [.. sketch.Points, p], Constraints = [fixedAt] };

        Assert.True(SketchSolver.SolveWithHold(alone, p.Id).HoldDropped);

        var conflict = alone with { Constraints = [fixedAt, new SketchConstraint(Guid.NewGuid(), "fixed", [p.Id], [7, 7])] };
        var (record, dropped) = SketchSolver.SolveWithHold(conflict, p.Id);
        Assert.False(dropped);
        Assert.Same(conflict, record);
    }

    // E88: a residual that is not finite is null in GetSketch, and the JSON of the HTTP surface
    // holds it.
    [Fact]
    public async Task A_Residual_That_Is_Not_Finite_Is_Null()
    {
        var session = NewSession();
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var far = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 1.5e308, Y = 0.5 }), "pointId");
        var other = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = -1.5e308, Y = 0.5 }), "pointId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "coincident", First = far, Second = other });

        var info = await Sketch(session, sketch);

        Assert.Null(Assert.Single(info.Constraints).Residual);
        var json = JsonSerializer.Serialize(info, Engine.Api.Http.Json.ApiJson.Options);
        Assert.Contains("\"residual\":null", json, StringComparison.Ordinal);
    }

    // E91: the lists of a record that the solver or a delete gives are read-only, so a client
    // in the process cannot change the Document through a cast.
    [Fact]
    public async Task The_Lists_Of_A_Record_Are_Read_Only()
    {
        var session = NewSession();
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var point = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 1 }), "pointId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "fixed", First = point, Value = 2, SecondValue = 2 });
        var solved = await Sketch(session, sketch);

        Assert.True(((ICollection<SketchPoint>)solved.Points).IsReadOnly);
        Assert.IsNotType<List<SketchPoint>>(solved.Points);

        // The constraints of GetSketch are a new list for each query; the record of the
        // Document holds the lists that a cast could change.
        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch, ItemId = point });
        var deleted = await session.Read((document, _) => document.Sketches.Single());
        Assert.True(((ICollection<SketchPoint>)deleted.Points).IsReadOnly);
        Assert.True(((ICollection<SketchLine>)deleted.Lines).IsReadOnly);
        Assert.True(((ICollection<SketchConstraint>)deleted.Constraints).IsReadOnly);
    }

    // Finding E94 of the codebase review of 2026-10-30 (TASK-0114): a move in a sketch with no
    // constraint and a change of a value gave the Document a list of the handler.
    [Fact]
    public async Task A_Move_And_A_Change_Of_A_Value_Give_Read_Only_Lists()
    {
        var session = NewSession();
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var point = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 1 }), "pointId");
        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = point, X = 2, Y = 2 });
        var moved = await session.Read((document, _) => document.Sketches.Single());
        Assert.True(((ICollection<SketchPoint>)moved.Points).IsReadOnly);

        var other = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 5, Y = 1 }), "pointId");
        var distance = Output(await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = point, Second = other, Value = 3 }), "constraintId");
        await Applied(session, new SetSketchConstraintValueCommand { SketchId = sketch, ConstraintId = distance, Value = 4 });
        var changed = await session.Read((document, _) => document.Sketches.Single());
        Assert.True(((ICollection<SketchConstraint>)changed.Constraints).IsReadOnly);
        Assert.True(((ICollection<SketchLine>)changed.Lines).IsReadOnly);
    }

    // Finding E95 of the codebase review of 2026-10-30 (TASK-0114): a move, a change of a value
    // and a delete sent again after the cache forgot them are refused, as E86 refuses an add.
    [Fact]
    public async Task Each_Sketch_Command_Sent_Again_After_The_Cache_Forgot_It_Is_Refused()
    {
        var session = NewSession();
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var a = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var b = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 4, Y = 0 }), "pointId");
        var c = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 4, Y = 4 }), "pointId");
        var distance = Output(await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = a, Second = b, Value = 4 }), "constraintId");
        var move = new MoveSketchPointCommand { SketchId = sketch, PointId = b, X = 5, Y = 0 };
        var set = new SetSketchConstraintValueCommand { SketchId = sketch, ConstraintId = distance, Value = 6 };
        var delete = new DeleteSketchItemCommand { SketchId = sketch, ItemId = c };
        foreach (Command command in new Command[] { move, set, delete })
            await Applied(session, command);
        var before = await session.Read((document, _) => document.Version);

        await Forget(session);

        foreach (Command again in new Command[] { move, set, delete })
        {
            var result = await session.Apply(again);
            Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.SketchIdInUse), (result.Status, result.Error?.Code));
        }
        Assert.Equal(before, await session.Read((document, _) => document.Version));
    }

    // E92: two points of a line at one position give their own reason, and not "cross or touch".
    [Fact]
    public void A_Line_Of_Length_Zero_Gives_Its_Own_Reason()
    {
        var sketch = SketchModel.New(Guid.NewGuid(), Datums.XY.Id, null);
        SketchPoint[] points = [new(Guid.NewGuid(), 0, 0), new(Guid.NewGuid(), 10, 0), new(Guid.NewGuid(), 10, 0), new(Guid.NewGuid(), 10, 10), new(Guid.NewGuid(), 0, 10)];
        var square = sketch with
        {
            Points = [.. sketch.Points, .. points],
            Lines = [.. points.Select((point, i) => new SketchLine(Guid.NewGuid(), point.Id, points[(i + 1) % points.Length].Id))],
        };

        var (loop, reason) = SketchProfile.Find(square);

        Assert.Null(loop);
        Assert.Contains("has the length zero", reason);
        Assert.Contains(square.Lines[1].Id.ToString(), reason);
    }
}
