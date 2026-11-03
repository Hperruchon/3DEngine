using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Features;

// The ordered feature list (ADR-0054, TASK-0173). ADR-0054 names this file in enforced-by. The fold of the log against
// a walk of the whole log after each command, and the rows, the inputs and the states of GetFeatures.
public sealed class FeatureListTests
{
    // A rectangle of 10 by 6 with its first corner at the origin, counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(0, 0), (10, 0), (10, 6), (0, 6)];

    private sealed record Sketch(Guid Id, Guid[] Points, Guid[] Lines);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static async Task<Sketch> NewSketch(DocumentSession session, Guid datum)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        var points = new Guid[Rectangle.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = Rectangle[i].X, Y = Rectangle[i].Y }), "pointId");
        var lines = new Guid[points.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        return new Sketch(sketch, points, lines);
    }

    // Items 1 and 4: after each command of a log of 400 commands with each kind of the graph, undos and redos, the fold of
    // the session gives what a walk of the whole log gives: the steps in effect in the order of the log, the identifiers of
    // the log, each creator, consumer and place, and the distance in effect of each extrude.
    [NativeManifoldFact]
    public async Task The_Fold_Gives_What_A_Walk_Of_The_Whole_Log_Gives_After_Each_Command()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = await NewSketch(session, Datums.XY.Id);
        var random = new Random(173);
        var kinds = new HashSet<string>();
        for (var step = 0; step < 400; step++)
        {
            var (bodies, extrudes) = await session.Read((document, _) => (
                document.Bodies.Select(body => body.Handle.Id).Order().ToList(),
                FeatureList.Of(document).Steps.OfType<ExtrudeCommand>().Select(extrude => extrude.CommandId).ToList()));
            Guid Any(List<Guid> ids) => ids[random.Next(ids.Count)];
            Command command = random.Next(10) switch
            {
                0 => new CreateBoxCommand { SizeX = 1 + random.Next(5), SizeY = 2, SizeZ = 3 },
                1 => new ExtrudeCommand { SketchId = sketch.Id, Distance = 1 + random.Next(9) },
                2 when bodies.Count > 0 => new TranslateCommand { BodyId = Any(bodies), Dx = random.Next(-3, 4), Dy = 0, Dz = 1 },
                3 when bodies.Count > 1 => new SubtractCommand { MinuendBodyId = bodies[0], SubtrahendBodyId = bodies[^1] },
                4 when extrudes.Count > 0 => new SetExtrudeDistanceCommand { ExtrudeId = Any(extrudes), Distance = 1 + random.Next(9) },
                5 => new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 10 + random.Next(3), Y = 6 },
                6 when extrudes.Count > 0 => new CreateDatumCommand { FeatureId = Any(extrudes), Element = "end" },
                7 or 8 => new UndoCommand(),
                _ => new RedoCommand(),
            };
            var result = await session.Apply(command);
            if (result.Status == CommandStatus.Applied)
                kinds.Add(command.Name);

            await session.Read((document, _) =>
            {
                AssertSameAsWalk(document.Log, FeatureList.Of(document));
                AssertSameAsWalk(document.Log, FeatureList.Build(document.Log));
                return 0;
            });
        }

        Assert.Superset(new HashSet<string> { "CreateBox", "Extrude", "Translate", "Subtract", "SetExtrudeDistance", "MoveSketchPoint", "CreateDatum", "Undo", "Redo" }, kinds);
    }

    // Item 4: a log that the bus cannot give, with a Redo after a new step and an Undo or a Redo with no step, folds as
    // the walk does. The commands are records only; no backend applies them.
    [Fact]
    public void The_Fold_Of_A_Written_Log_Gives_What_The_Walk_Gives()
    {
        var random = new Random(1173);
        var extrudes = new List<Guid>();
        var log = new List<LogEntry>();
        for (var step = 0; step < 600; step++)
        {
            Command command = random.Next(7) switch
            {
                0 => new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 },
                1 => new ExtrudeCommand { SketchId = Guid.NewGuid(), Distance = 1 + random.Next(9) },
                2 => new TranslateCommand { BodyId = Guid.NewGuid(), Dx = 1, Dy = 0, Dz = 0 },
                3 when extrudes.Count > 0 => new SetExtrudeDistanceCommand { ExtrudeId = extrudes[random.Next(extrudes.Count)], Distance = 1 + random.Next(9) },
                4 or 5 => new UndoCommand(),
                _ => new RedoCommand(),
            };
            if (command is ExtrudeCommand extrude)
                extrudes.Add(extrude.CommandId);
            log.Add(new LogEntry(command, new BackendIdentity("managed", "test")));
            AssertSameAsWalk(log, FeatureList.Build(log));
        }
    }

    private static void AssertSameAsWalk(IReadOnlyList<LogEntry> log, FeatureList list)
    {
        var inEffect = SketchRebuild.InEffect(log);
        var steps = log.Select(entry => entry.Command).Where(command => inEffect.Contains(command.CommandId)).ToList();
        Assert.Equal(steps.Select(command => command.CommandId), list.Steps.Select(command => command.CommandId));
        Assert.True(inEffect.SetEquals(list.InEffect));
        Assert.All(log, entry => Assert.True(list.Holds(entry.Command.CommandId)));
        Assert.False(list.Holds(Guid.NewGuid()));

        for (var i = 0; i < steps.Count; i++)
        {
            var command = steps[i];
            Assert.Equal(i, list.Order(command.CommandId));
            Assert.Same(FeatureList.Creates(command) ? command : null, list.Creator(command.CommandId));
            foreach (var operand in FeatureList.Operands(command))
                Assert.Same(command, list.Consumer(operand));
            if (command is ExtrudeCommand extrude)
            {
                var last = steps.OfType<SetExtrudeDistanceCommand>().LastOrDefault(set => set.ExtrudeId == extrude.CommandId);
                Assert.Equal(last?.Distance ?? extrude.Distance, list.DistanceInEffect(extrude.CommandId));
            }
        }

        var consumed = steps.SelectMany(FeatureList.Operands).ToHashSet();
        Assert.All(log.Select(entry => entry.Command).Where(command => !consumed.Contains(command.CommandId)), command => Assert.Null(list.Consumer(command.CommandId)));
        Assert.All(log.Select(entry => entry.Command).Where(command => !inEffect.Contains(command.CommandId)), command =>
        {
            Assert.Equal(int.MaxValue, list.Order(command.CommandId));
            Assert.Null(list.Creator(command.CommandId));
        });
    }

    // Items 2, 3 and 5: the rows are the creators in effect in the order of the log, with their inputs. A consumed body,
    // a body with a fault, and a datum and a sketch with a fault each give their state; an Undo removes the faults and the
    // last feature.
    [NativeManifoldFact]
    public async Task GetFeatures_Gives_Each_Creator_In_Order_With_Its_Inputs_And_Its_State()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 6, SizeZ = 4 }), "bodyId");
        var sketch = await NewSketch(session, Datums.XY.Id);
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch.Id, Distance = 5 }), "bodyId");
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 10 }), "bodyId");
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z" }), "datumId");
        var side = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "side", ItemId = sketch.Lines[1] }), "datumId");
        var top = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        await Applied(session, new AddSketchReferenceCommand { SketchId = top, FeatureId = body, Element = "endVertex", ItemId = sketch.Points[2] });
        var image = Output(await Applied(session, new PlaceImageCommand { Asset = new string('a', 64), DatumId = Datums.XY.Id, Width = 4, Height = 2 }), "imageId");

        async Task<IReadOnlyList<FeatureInfo>> Features() => (await session.Query<FeaturesInfo>(new GetFeaturesQuery())).Result!.Features;
        var rows = await Features();

        Assert.Equal(
            [
                (box, "CreateBox"), (sketch.Id, "CreateSketch"), (body, "Extrude"), (moved, "Translate"), (datum, "CreateDatum"),
                (side, "CreateDatum"), (top, "CreateSketch"), (image, "PlaceImage"),
            ],
            rows.Select(row => (row.FeatureId, row.Kind)));
        Assert.Equal(
            [[], [Datums.XY.Id], [sketch.Id], [body], [box], [body], [datum, body], [Datums.XY.Id]],
            rows.Select(row => row.Inputs.ToArray()));
        Assert.Equal([null, null, moved, null, null, null, null, null], rows.Select(row => row.ConsumedBy));
        Assert.Equal(["correct", "correct", "consumed", "correct", "correct", "correct", "correct", "correct"], rows.Select(row => row.State));

        // The line of the side datum breaks the loop: the end of the chain fails, and the side datum and the reference of
        // the top sketch keep their last values with a fault.
        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch.Id, ItemId = sketch.Lines[1] });
        var broken = (await Features()).ToDictionary(row => row.FeatureId);

        Assert.Equal(("failed", DiagnosticCodes.SketchRebuildFailed), (broken[moved].State, broken[moved].Fault?.Code));
        Assert.Equal(("unresolved", DiagnosticCodes.DatumReferenceFailed), (broken[side].State, broken[side].Fault?.Code));
        Assert.Equal(("unresolved", DiagnosticCodes.SketchReferenceFailed), (broken[top].State, broken[top].Fault?.Code));
        Assert.Equal(("consumed", (FeatureFault?)null), (broken[body].State, broken[body].Fault));

        await Applied(session, new UndoCommand());
        Assert.All(await Features(), row => Assert.Null(row.Fault));
        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());
        Assert.Equal([box, sketch.Id, body, moved, datum, side, top], (await Features()).Select(row => row.FeatureId));
    }
}
