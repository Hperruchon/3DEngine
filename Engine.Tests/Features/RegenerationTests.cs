using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Features;

// The regeneration (ADR-0055, TASK-0174). ADR-0055 names this file in enforced-by. After each command of a session the
// solid of each live body is the solid that a build from nothing of the features in effect gives, and an edit calls the
// backend only for the steps downstream of it, with the ends in the order of the log.
public sealed class RegenerationTests
{
    // A rectangle of 10 by 6 with its first corner at the origin, counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(0, 0), (10, 0), (10, 6), (0, 6)];

    private sealed record Sketch(Guid Id, Guid[] Points);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static async Task<Sketch> NewSketch(DocumentSession session, Guid datum, (double X, double Y)[] corners)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < points.Length; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] });
        return new Sketch(sketch, points);
    }

    // Items 1, 2 and 5: after each command of eight sessions of 250 commands with sketches on the origin planes and on
    // the end of an extrude, moves of a point that break and mend a profile, new distances, moves, cuts, undos and redos,
    // each live body with no fault has the solid that a build from nothing of its chain gives from the records of the
    // Document, and each body with a fault has a chain that a build from nothing cannot give.
    [NativeManifoldFact]
    public async Task Each_Live_Body_Has_The_Solid_That_A_Build_From_Nothing_Gives_After_Each_Command()
    {
        var kinds = new HashSet<string>();
        var (checkedBodies, faulted) = (0, 0);
        for (var seed = 1; seed <= 8; seed++)
            await Session(seed);

        Assert.Superset(new HashSet<string> { "Extrude", "CreateBox", "Translate", "Subtract", "SetExtrudeDistance", "MoveSketchPoint", "CreateDatum", "Undo", "Redo" }, kinds);
        Assert.True(checkedBodies > 10000, $"Only {checkedBodies} bodies were compared.");
        Assert.True(faulted > 100, $"Only {faulted} bodies had a fault.");

        async Task Session(int seed)
        {
            using var backend = new ManifoldGeometryBackend();
            var session = new DocumentSession(EngineHosting.CreateDefault(backend));
            Sketch[] sketches = [await NewSketch(session, Datums.XY.Id, Rectangle), await NewSketch(session, Datums.XZ.Id, Rectangle)];
            var random = new Random(seed);
            for (var step = 0; step < 250; step++)
            {
                var (bodies, extrudes) = await session.Read((document, _) => (
                    document.Bodies.Where(body => body.Fault is null).Select(body => body.Handle.Id).OrderBy(id => FeatureList.Of(document).Order(id)).ToList(),
                    FeatureList.Of(document).Steps.OfType<ExtrudeCommand>().Select(extrude => extrude.CommandId).ToList()));
                Guid Any(List<Guid> ids) => ids[random.Next(ids.Count)];
                var sketch = sketches[random.Next(sketches.Length)];
                switch (random.Next(11))
                {
                    case 0 or 1:
                        await Apply(new ExtrudeCommand { SketchId = sketch.Id, Distance = (random.Next(2) == 0 ? 1 : -1) * (1 + random.Next(8)) });
                        break;
                    case 2:
                        await Apply(new CreateBoxCommand { SizeX = 2 + random.Next(20), SizeY = 2 + random.Next(20), SizeZ = 1 + random.Next(4) });
                        break;
                    case 3 when bodies.Count > 0:
                        await Apply(new TranslateCommand { BodyId = Any(bodies), Dx = random.Next(-3, 4), Dy = random.Next(-3, 4), Dz = random.Next(-3, 4) });
                        break;
                    case 4 when bodies.Count > 1:
                        var minuend = Any(bodies);
                        await Apply(new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = Any([.. bodies.Where(id => id != minuend)]) });
                        break;
                    case 5 when extrudes.Count > 0:
                        await Apply(new SetExtrudeDistanceCommand { ExtrudeId = Any(extrudes), Distance = 1 + random.Next(9) });
                        break;
                    case 6 or 7:
                        var (x, y) = random.Next(4) == 0 ? (-5, 3) : (8 + random.Next(6), 4 + random.Next(6));
                        await Apply(new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = x, Y = y });
                        break;
                    case 8 when extrudes.Count > 0:
                        // A sketch on the end of an extrude, and an extrude of it: a chain through a datum (ADR-0051).
                        var datum = await Apply(new CreateDatumCommand { FeatureId = Any(extrudes), Element = "end" });
                        if (datum.Status != CommandStatus.Applied)
                            break;
                        var top = await NewSketch(session, Output(datum, "datumId"), [(1, 1), (3, 1), (3, 3), (1, 3)]);
                        await Apply(new ExtrudeCommand { SketchId = top.Id, Distance = 2 });
                        break;
                    case 9:
                        await Apply(new UndoCommand());
                        break;
                    default:
                        await Apply(new RedoCommand());
                        break;
                }

                await session.Read((document, _) =>
                {
                    var list = FeatureList.Of(document);
                    var built = new Dictionary<Guid, Guid?>();
                    foreach (var body in document.Bodies)
                    {
                        var expected = Build(document, list, backend, body.Handle.Id, built);
                        if (body.Fault is not null)
                        {
                            Assert.True(expected is null, $"The body '{body.Handle.Id}' has a fault, and a build from nothing gives it.");
                            faulted++;
                            continue;
                        }
                        Assert.True(expected is not null, $"The body '{body.Handle.Id}' has no fault, and a build from nothing fails.");
                        Assert.Equal(Measure(backend, expected.Value), Measure(backend, body.GeometryKey));
                        checkedBodies++;
                    }
                    return 0;
                });
            }

            async Task<CommandResult> Apply(Command command)
            {
                var result = await session.Apply(command);
                if (result.Status == CommandStatus.Applied)
                    kinds.Add(command.Name);
                return result;
            }
        }
    }

    // A build from nothing: the solid of the handle from the records of the Document, under new keys, or null when a step
    // does not resolve or the backend refuses it.
    private static Guid? Build(Document document, FeatureList list, IGeometryBackend backend, Guid handle, Dictionary<Guid, Guid?> built)
    {
        if (built.TryGetValue(handle, out var known))
            return known;
        var key = Guid.NewGuid();
        Guid? result;
        try
        {
            switch (list.Creator(handle))
            {
                case CreateBoxCommand:
                    result = handle;
                    break;
                case ExtrudeCommand extrude:
                    var sketch = SketchModel.Find(document, extrude.SketchId);
                    var frame = sketch is null ? null : DatumResolver.Frame(document, sketch.DatumId);
                    var parameters = sketch is null || frame is null ? null : ExtrudeProfile.Parameters(sketch, list.DistanceInEffect(extrude.CommandId)!.Value, frame).Parameters;
                    if (parameters is not { } profile)
                    {
                        result = null;
                        break;
                    }
                    backend.TryGet<IExtrudeOps>()!.Extrude(new BodyHandle(key), profile);
                    result = key;
                    break;
                case TranslateCommand translate when Build(document, list, backend, translate.BodyId, built) is { } source:
                    backend.TryGet<ITransformOps>()!.Translate(new BodyHandle(key), new BodyHandle(source), translate.Dx, translate.Dy, translate.Dz);
                    result = key;
                    break;
                case SubtractCommand subtract
                    when Build(document, list, backend, subtract.MinuendBodyId, built) is { } minuend
                        && Build(document, list, backend, subtract.SubtrahendBodyId, built) is { } subtrahend:
                    backend.TryGet<IBooleanOps>()!.Subtract(new BodyHandle(key), new BodyHandle(minuend), new BodyHandle(subtrahend));
                    result = key;
                    break;
                default:
                    result = null;
                    break;
            }
        }
        catch (Exception)
        {
            result = null;
        }

        built[handle] = result;
        return result;
    }

    // The bounds and the volume of a solid, rounded.
    private static (double, double, double, double, double, double, double) Measure(IGeometryBackend backend, Guid key)
    {
        var box = backend.TryGet<IGeometryQuery>()!.GetBoundingBox(new BodyHandle(key));
        var mesh = backend.TryGet<ITessellationOps>()!.Tessellate(new BodyHandle(key));
        var volume = 0.0;
        for (var i = 0; i < mesh.Indices.Length; i += 3)
        {
            double P(int corner, int axis) => mesh.Positions[(mesh.Indices[i + corner] * 3) + axis];
            volume += ((P(0, 0) * ((P(1, 1) * P(2, 2)) - (P(1, 2) * P(2, 1))))
                - (P(0, 1) * ((P(1, 0) * P(2, 2)) - (P(1, 2) * P(2, 0))))
                + (P(0, 2) * ((P(1, 0) * P(2, 1)) - (P(1, 1) * P(2, 0))))) / 6;
        }

        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9),
            Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9), Math.Round(volume, 6));
    }

    // Items 2 and 4: an edit calls the backend only for the steps downstream of it, each other body keeps its key, the
    // ends change in the log order of the command that creates each one, and an edit that changes no input calls
    // nothing.
    [NativeManifoldFact]
    public async Task An_Edit_Calls_The_Backend_Only_For_The_Steps_Downstream_Of_It_In_The_Order_Of_The_Log()
    {
        using var inner = new ManifoldGeometryBackend();
        var backend = new CountingBackend(inner);
        var kit = EngineHosting.CreateDefault(backend);
        var session = new DocumentSession(kit);
        var a = await NewSketch(session, Datums.XY.Id, Rectangle);
        var b = await NewSketch(session, Datums.XY.Id, [(20, 0), (26, 0), (26, 4), (20, 4)]);
        var first = Output(await Applied(session, new ExtrudeCommand { SketchId = a.Id, Distance = 5 }), "bodyId");
        var second = Output(await Applied(session, new ExtrudeCommand { SketchId = a.Id, Distance = 3 }), "bodyId");
        var up = Output(await Applied(session, new TranslateCommand { BodyId = second, Dx = 0, Dy = 0, Dz = 10 }), "bodyId");
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = first, Dx = 0, Dy = 0, Dz = 20 }), "bodyId");
        var live = Output(await Applied(session, new ExtrudeCommand { SketchId = a.Id, Distance = 2 }), "bodyId");
        var other = Output(await Applied(session, new ExtrudeCommand { SketchId = b.Id, Distance = 4 }), "bodyId");
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 60, SizeY = 60, SizeZ = 2 }), "bodyId");
        var lifted = Output(await Applied(session, new TranslateCommand { BodyId = plate, Dx = 0, Dy = 0, Dz = 1 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = other, SubtrahendBodyId = lifted }), "bodyId");

        IReadOnlyList<Guid> Changed(CommandResult edit) => [.. kit.Events.Snapshot()
            .Where(e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId)
            .Select(e => (Guid)((IReadOnlyDictionary<string, object?>)e.Payload!)["bodyId"]!)];
        Task<Guid> Key(Guid body) => session.Read((document, _) => document.Bodies.Single(record => record.Handle.Id == body).GeometryKey);
        var cutKey = await Key(cut);

        // An edit of the sketch a: the three ends in the log order of the command that creates each one, which is not the
        // order of their extrudes and does not put the live extrude first. The extrude of b and the cut keep their solids.
        backend.Counts.Clear();
        var edit = await Applied(session, new MoveSketchPointCommand { SketchId = a.Id, PointId = a.Points[2], X = 12, Y = 7 });
        Assert.Equal([up, moved, live], Changed(edit));
        Assert.Equal(new Dictionary<string, int> { ["Extrude"] = 3, ["Translate"] = 2 }, backend.Counts);
        Assert.Equal(cutKey, await Key(cut));
        var keys = new[] { await Key(up), await Key(moved), await Key(live) };
        Assert.Equal(new[] { up, moved, live }.Select(body => GeometryKeys.Derive(edit.CommandId, body)), keys);

        // A new distance of the extrude of b: the extrude and the cut. The moved plate keeps its solid, and nothing of the
        // sketch a runs.
        backend.Counts.Clear();
        var distance = await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = other, Distance = 6 });
        Assert.Equal([cut], Changed(distance));
        Assert.Equal(new Dictionary<string, int> { ["Extrude"] = 1, ["Subtract"] = 1 }, backend.Counts);

        // A move of the point to its place changes no input: no call and no change.
        backend.Counts.Clear();
        var same = await Applied(session, new MoveSketchPointCommand { SketchId = a.Id, PointId = a.Points[2], X = 12, Y = 7 });
        Assert.Empty(Changed(same));
        Assert.Empty(backend.Counts);
    }

    // Items 3 and 4: an end with a fault is evaluated again at the next edit that targets it, also when its inputs do not
    // change, so the end of a failure of the backend that does not come again loses its fault.
    [NativeManifoldFact]
    public async Task An_End_With_A_Fault_Is_Evaluated_Again_With_The_Same_Inputs()
    {
        using var inner = new ManifoldGeometryBackend();
        var backend = new CountingBackend(inner);
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var a = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = a.Id, Distance = 5 }), "bodyId");
        Task<BodyRecord> Record() => session.Read((document, _) => document.Bodies.Single(record => record.Handle.Id == body));

        var first = (await Record()).GeometryKey;
        backend.FailExtrude = true;
        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 6 });
        var failed = await Record();
        Assert.Equal((DiagnosticCodes.FeatureRebuildFailed, first), (failed.Fault?.Code, failed.GeometryKey));

        backend.FailExtrude = false;
        backend.Counts.Clear();
        var repair = await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 6 });
        var mended = await Record();
        Assert.Equal((GeometryKeys.Derive(repair.CommandId, body), (BodyFault?)null), (mended.Geometry, mended.Fault));
        Assert.Equal(new Dictionary<string, int> { ["Extrude"] = 1 }, backend.Counts);
    }

    // The backend of Manifold, with a count of each call of an operation that makes a solid, and an extrude that throws
    // while FailExtrude is true.
    private sealed class CountingBackend(ManifoldGeometryBackend inner) : IGeometryBackend, IExtrudeOps, ITransformOps, IBooleanOps
    {
        public Dictionary<string, int> Counts { get; } = [];

        public bool FailExtrude { get; set; }

        public BackendCapabilities Capabilities => inner.Capabilities;

        public BackendIdentity Identity => inner.Identity;

        public T? TryGet<T>() where T : class
            => typeof(T) == typeof(IExtrudeOps) || typeof(T) == typeof(ITransformOps) || typeof(T) == typeof(IBooleanOps) ? this as T : inner.TryGet<T>();

        public void Extrude(BodyHandle handle, ExtrudeParameters parameters)
        {
            Count("Extrude");
            if (FailExtrude)
                throw new InvalidOperationException("The test fails this extrude.");
            inner.TryGet<IExtrudeOps>()!.Extrude(handle, parameters);
        }

        public void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz)
        {
            Count("Translate");
            inner.TryGet<ITransformOps>()!.Translate(result, source, dx, dy, dz);
        }

        public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend)
        {
            Count("Subtract");
            inner.TryGet<IBooleanOps>()!.Subtract(result, minuend, subtrahend);
        }

        private void Count(string operation) => Counts[operation] = Counts.GetValueOrDefault(operation) + 1;
    }
}
