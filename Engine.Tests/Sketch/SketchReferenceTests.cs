using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// An edge or a vertex of a feature as a reference of a sketch (ADR-0052, TASK-0167). ADR-0052 names
// this file in enforced-by. The projection of the vertices and the edges of an extrude and of a box,
// the hold of the solver, the move and the delete, the profile, the rebuild after an edit of the
// base, a fault, a cycle, the refusals, an undo and a redo, and an open.
public sealed class SketchReferenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-reference-" + Guid.NewGuid().ToString("N"));

    public SketchReferenceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private sealed record Sketch(Guid Id, Guid[] Points, Guid[] Lines);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static async Task<(string Code, string Message)> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return (result.Error!.Code, result.Error.Message);
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(IGeometryBackend backend)
    {
        var kit = EngineHosting.CreateDefault(backend);
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<Guid> EmptySketch(DocumentSession session, Guid datum)
        => Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");

    // A closed sketch of the corners on a datum.
    private static async Task<Sketch> NewSketch(DocumentSession session, Guid datum, (double X, double Y)[] corners)
    {
        var sketch = await EmptySketch(session, datum);
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[corners.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        return new Sketch(sketch, points, lines);
    }

    private static async Task<Guid> Extrude(DocumentSession session, Guid sketch, double distance)
        => Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = distance }), "bodyId");

    private static async Task<Guid> Reference(DocumentSession session, Guid sketch, Guid feature, string element, Guid? item = null)
        => Output(await Applied(session, new AddSketchReferenceCommand { SketchId = sketch, FeatureId = feature, Element = element, ItemId = item }), "referenceId");

    private static Task<SketchRecord> Record(DocumentSession session, Guid sketch)
        => session.Read((document, _) => document.Sketches.Single(record => record.Id == sketch));

    // The positions of the points of a reference, in the order of the reference.
    private static async Task<IReadOnlyList<(double X, double Y)>> Points(DocumentSession session, Guid sketch, Guid reference)
    {
        var record = await Record(session, sketch);
        var entry = record.References.Single(item => item.Id == reference);
        return [.. entry.Points.Select(id => record.Points.Single(point => point.Id == id)).Select(point => (point.X, point.Y))];
    }

    private static async Task<(double, double, double, double, double, double)> Bounds(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        var box = result.Result;
        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9), Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9));
    }

    // Items 1 to 3: a vertex adds one point with the identifier of the command, and an edge adds a line
    // with the identifier of the command and two points, at the projection onto the plane XZ, whose u is
    // X and whose v is Z.
    [NativeManifoldFact]
    public async Task The_Vertices_And_The_Edges_Of_An_Extrude_Project_Onto_The_Sketch()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var base_ = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, base_.Id, 5);
        var sketch = await EmptySketch(session, Datums.XZ.Id);

        var vertex = await Reference(session, sketch, body, "endVertex", base_.Points[2]);
        var start = await Reference(session, sketch, body, "startVertex", base_.Points[2]);
        var top = await Reference(session, sketch, body, "endEdge", base_.Lines[0]);
        var side = await Reference(session, sketch, body, "sideEdge", base_.Points[1]);

        Assert.Equal([(11.0, 5.0)], await Points(session, sketch, vertex));
        Assert.Equal([(11.0, 0.0)], await Points(session, sketch, start));
        Assert.Equal([(1.0, 5.0), (11.0, 5.0)], await Points(session, sketch, top));
        Assert.Equal([(11.0, 0.0), (11.0, 5.0)], await Points(session, sketch, side));
        var record = await Record(session, sketch);
        Assert.Equal(vertex, Assert.Single(record.References, item => item.Id == vertex).Points.Single());
        var edge = record.References.Single(item => item.Id == top);
        Assert.Equal(top, edge.Line);
        Assert.Equal(new SketchLine(top, edge.Points[0], edge.Points[1]), record.Lines.Single(line => line.Id == top));
        Assert.Equal([GeometryKeys.Derive(top, DatumResolver.FirstEnd), GeometryKeys.Derive(top, DatumResolver.SecondEnd)], edge.Points);
        Assert.Empty(record.Constraints);
    }

    // Item 1: the edges and the vertices of a box of 40 by 30 by 10 with its centre at the origin. The
    // edge +x-y runs along z, so it projects to one point of XY and is refused there.
    [Fact]
    public async Task The_Edges_And_The_Vertices_Of_A_Box_Project_Onto_The_Sketch()
    {
        var (session, _) = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");
        var front = await EmptySketch(session, Datums.XZ.Id);
        var floor = await EmptySketch(session, Datums.XY.Id);

        Assert.Equal([(20.0, -5.0), (20.0, 5.0)], await Points(session, front, await Reference(session, front, box, "+x-y")));
        Assert.Equal([(-20.0, 5.0), (20.0, 5.0)], await Points(session, front, await Reference(session, front, box, "-y+z")));
        Assert.Equal([(20.0, -15.0)], await Points(session, floor, await Reference(session, floor, box, "+x-y+z")));
        var (code, message) = await Refused(session, new AddSketchReferenceCommand { SketchId = floor, FeatureId = box, Element = "+x-y" });
        Assert.Equal(DiagnosticCodes.SketchReferenceInvalid, code);
        Assert.Contains("one point", message, StringComparison.Ordinal);
    }

    // Item 5: a reference point has no variable. A constraint moves the point of the person, not the
    // reference point; no command moves the reference point, and a delete of one end of an edge alone
    // is refused. A delete of the reference removes its points, its line and each constraint on them.
    [Fact]
    public async Task A_Reference_Point_Holds_And_Only_The_Whole_Reference_Is_Deleted()
    {
        var (session, _) = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");
        var sketch = await EmptySketch(session, Datums.XY.Id);
        var corner = await Reference(session, sketch, box, "+x+y+z");
        var edge = await Reference(session, sketch, box, "-x+z");
        var free = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 3, Y = 4 }), "pointId");

        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "coincident", First = free, Second = corner });
        var record = await Record(session, sketch);
        Assert.Equal((20.0, 15.0), record.Points.Single(point => point.Id == free) is var p ? (Math.Round(p.X, 9), Math.Round(p.Y, 9)) : default);
        Assert.Equal((20.0, 15.0), record.Points.Single(point => point.Id == corner) is var c ? (c.X, c.Y) : default);
        Assert.True(SketchSolver.Status(record).Solved);

        Assert.Equal(DiagnosticCodes.SketchItemNotFound, (await Refused(session, new MoveSketchPointCommand { SketchId = sketch, PointId = corner, X = 0, Y = 0 })).Code);
        var end = record.References.Single(item => item.Id == edge).Points[0];
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, (await Refused(session, new DeleteSketchItemCommand { SketchId = sketch, ItemId = end })).Code);

        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch, ItemId = corner });
        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch, ItemId = edge });
        record = await Record(session, sketch);
        Assert.Empty(record.References);
        Assert.Empty(record.Constraints);
        Assert.Empty(record.Lines);
        Assert.Equal([sketch, free], record.Points.Select(point => point.Id));
    }

    // Items 6 and 8: a line of a reference is not an edge of the profile, so a sketch with a rectangle
    // and a reference edge extrudes. A corner of the rectangle coincides with the reference point of the
    // end vertex of the base, so a new distance of the base moves the reference, the solver moves the
    // rectangle, and its extrude follows, with sketch.changed in the commit.
    [NativeManifoldFact]
    public async Task An_Edit_Of_The_Base_Moves_The_References_And_The_Solid_That_Follows_Them()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var base_ = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, base_.Id, 5);
        var wall = await NewSketch(session, Datums.XZ.Id, [(11, 5), (15, 5), (15, 7), (11, 7)]);
        var vertex = await Reference(session, wall.Id, body, "endVertex", base_.Points[2]);
        await Reference(session, wall.Id, body, "endEdge", base_.Lines[0]);
        await Applied(session, new AddSketchConstraintCommand { SketchId = wall.Id, Kind = "coincident", First = wall.Points[0], Second = vertex });
        foreach (var (line, kind) in new[] { (0, "horizontal"), (1, "vertical"), (2, "horizontal"), (3, "vertical") })
            await Applied(session, new AddSketchConstraintCommand { SketchId = wall.Id, Kind = kind, First = wall.Lines[line] });
        await Applied(session, new AddSketchConstraintCommand { SketchId = wall.Id, Kind = "distance", First = wall.Lines[0], Value = 4 });
        await Applied(session, new AddSketchConstraintCommand { SketchId = wall.Id, Kind = "distance", First = wall.Lines[1], Value = 2 });
        var boss = await Extrude(session, wall.Id, -3);
        Assert.Equal((11.0, 0.0, 5.0, 15.0, 3.0, 7.0), await Bounds(session, boss));

        var edit = await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 9 });

        Assert.Empty(edit.Diagnostics);
        Assert.Equal([(11.0, 9.0)], await Points(session, wall.Id, vertex));
        Assert.Equal((11.0, 0.0, 9.0, 15.0, 3.0, 11.0), await Bounds(session, boss));
        var kinds = events.Snapshot().Where(e => e.CauseCommandId == edit.CommandId).Select(e => e.Kind).ToList();
        Assert.Equal(["command.applied", "body.changed", "body.changed", "sketch.changed"], kinds);
    }

    // Item 8 (a failure): a delete of the line of the base that an edge names breaks the reference. The
    // points keep their positions, the reference gets W-SKETCH-REFERENCE-FAILED, and the edit applies.
    // An undo gives back the record before the edit.
    [NativeManifoldFact]
    public async Task A_Reference_That_Does_Not_Resolve_Keeps_Its_Points_With_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var base_ = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, base_.Id, 5);
        var sketch = await EmptySketch(session, Datums.XZ.Id);
        var edge = await Reference(session, sketch, body, "endEdge", base_.Lines[0]);
        var before = await Points(session, sketch, edge);

        var delete = await Applied(session, new DeleteSketchItemCommand { SketchId = base_.Id, ItemId = base_.Lines[0] });

        Assert.Equal(before, await Points(session, sketch, edge));
        var entry = (await Record(session, sketch)).References.Single();
        Assert.Equal(DiagnosticCodes.SketchReferenceFailed, entry.Fault?.Code);
        Assert.Contains(delete.Diagnostics, d => d.Code == DiagnosticCodes.SketchReferenceFailed && Equals(d.Data?["referenceId"], edge));

        await Applied(session, new UndoCommand());
        Assert.Null((await Record(session, sketch)).References.Single().Fault);
    }

    // Item 7: a reference to an extrude of the sketch, or to an extrude that depends on the sketch through
    // a datum, is a cycle. A reference of the second sketch to the extrude of the first one is not.
    [NativeManifoldFact]
    public async Task A_Feature_That_Depends_On_The_Sketch_Is_A_Cycle()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var base_ = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, base_.Id, 5);
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "end" }), "datumId");
        var top = await NewSketch(session, datum, [(2, 3), (4, 3), (4, 5), (2, 5)]);
        var boss = await Extrude(session, top.Id, 2);

        Assert.Equal(DiagnosticCodes.SketchReferenceCycle, (await Refused(session, new AddSketchReferenceCommand { SketchId = base_.Id, FeatureId = body, Element = "endVertex", ItemId = base_.Points[0] })).Code);
        Assert.Equal(DiagnosticCodes.SketchReferenceCycle, (await Refused(session, new AddSketchReferenceCommand { SketchId = base_.Id, FeatureId = boss, Element = "endVertex", ItemId = top.Points[0] })).Code);
        await Reference(session, top.Id, body, "endVertex", base_.Points[0]);
        Assert.Equal(DiagnosticCodes.SketchReferenceCycle, (await Refused(session, new AddSketchReferenceCommand { SketchId = top.Id, FeatureId = boss, Element = "startVertex", ItemId = top.Points[1] })).Code);
    }

    // Item 7: each other refusal gives E-SKETCH-REFERENCE-INVALID with its reason.
    [NativeManifoldFact]
    public async Task Each_Refusal_Gives_Its_Code_And_Its_Reason()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var base_ = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, base_.Id, 5);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }), "bodyId");
        var sketch = await EmptySketch(session, Datums.XZ.Id);

        foreach (var (feature, element, item, reason) in new (Guid, string, Guid?, string)[]
        {
            (Guid.NewGuid(), "endVertex", base_.Points[0], "No CreateBox or Extrude"),
            (body, "end", null, "CreateDatum"),
            (body, "endEdge", null, "needs the itemId"),
            (body, "endEdge", base_.Points[0], "not a line of the profile"),
            (body, "sideEdge", base_.Lines[0], "not a vertex of the profile"),
            (body, "endVertex", sketch, "not a vertex of the profile"),
            (box, "+x", null, "not an edge or a vertex of a box"),
            (box, "+y+x", null, "not an edge or a vertex of a box"),
            (box, "+x+x+z", null, "not an edge or a vertex of a box"),
            (box, "+x-y", base_.Lines[0], "takes no itemId"),
        })
        {
            var (code, message) = await Refused(session, new AddSketchReferenceCommand { SketchId = sketch, FeatureId = feature, Element = element, ItemId = item });
            Assert.Equal(DiagnosticCodes.SketchReferenceInvalid, code);
            Assert.Contains(reason, message, StringComparison.Ordinal);
        }

        Assert.Equal(DiagnosticCodes.SketchNotFound, (await Refused(session, new AddSketchReferenceCommand { SketchId = Guid.NewGuid(), FeatureId = body, Element = "endVertex", ItemId = base_.Points[0] })).Code);
    }

    // Item 1: an arc of the profile is a curved edge, which waits for K5; its ends are vertices.
    [NativeManifoldFact]
    public async Task A_Curved_Edge_Is_Refused_And_Its_Ends_Are_Vertices()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await EmptySketch(session, Datums.XY.Id);
        async Task<Guid> Point(double x, double y) => Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId");
        var (centre, a, b) = (await Point(0, 0), await Point(10, 0), await Point(-10, 0));
        var arc = Output(await Applied(session, new AddSketchArcCommand { SketchId = sketch, CenterPointId = centre, StartPointId = a, EndPointId = b }), "arcId");
        await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = b, EndPointId = a });
        var body = await Extrude(session, sketch, 4);
        var other = await EmptySketch(session, Datums.XZ.Id);

        var (code, message) = await Refused(session, new AddSketchReferenceCommand { SketchId = other, FeatureId = body, Element = "endEdge", ItemId = arc });
        Assert.Equal((DiagnosticCodes.SketchReferenceInvalid, true), (code, message.Contains("K5", StringComparison.Ordinal)));
        Assert.Equal([(-10.0, 4.0)], await Points(session, other, await Reference(session, other, body, "endVertex", b)));
    }

    // Item 4: an undo removes the reference with its points, and a redo gives it back.
    [Fact]
    public async Task An_Undo_And_A_Redo_Remove_And_Give_Back_A_Reference()
    {
        var (session, _) = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 4, SizeZ = 6 }), "bodyId");
        var sketch = await EmptySketch(session, Datums.XY.Id);
        var reference = await Reference(session, sketch, box, "+x+z");
        var record = await Record(session, sketch);

        await Applied(session, new UndoCommand());
        Assert.Empty((await Record(session, sketch)).References);
        Assert.Single((await Record(session, sketch)).Points);
        await Applied(session, new RedoCommand());

        var again = await Record(session, sketch);
        Assert.Equal(record.Points, again.Points);
        Assert.Equal(record.References.Single().Points, again.References.Single(item => item.Id == reference).Points);
        var info = await session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch });
        Assert.Equal(reference, Assert.Single(info.Result!.References).Id);
    }

    // Items 2 and 8: the open of a file applies each entry, so it gives the same references and solids.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_References_And_Solids()
    {
        var path = Path.Combine(_folder, "reference" + DocumentFile.Extension);
        IReadOnlyList<SketchPoint> points;
        IReadOnlyList<BodyRecord> bodies;
        Guid wall;
        using (var backend = new ManifoldGeometryBackend())
        {
            var (live, _) = NewSession(backend);
            DocumentFile.Create(live, path);
            var base_ = await NewSketch(live, Datums.XY.Id, Rectangle);
            var body = await Extrude(live, base_.Id, 5);
            wall = await EmptySketch(live, Datums.XZ.Id);
            await Reference(live, wall, body, "endEdge", base_.Lines[0]);
            await Reference(live, wall, body, "sideEdge", base_.Points[3]);
            await Applied(live, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 7.5 });
            points = (await Record(live, wall)).Points;
            bodies = await live.Read((document, _) => document.Bodies.ToList());
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var (opened, _) = NewSession(backend);
            await DocumentFile.Open(opened, path);

            Assert.Equal(points, (await Record(opened, wall)).Points);
            Assert.Equal(bodies, await opened.Read((document, _) => document.Bodies.ToList()));
            opened.Close();
        }
    }
}
