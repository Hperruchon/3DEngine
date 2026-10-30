using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The rebuild of an extrude after an edit of its sketch (ADR-0037, TASK-0108). ADR-0037 names
// this file in enforced-by. The key of a solid, the steps in effect, the rebuild and its undo,
// a profile that breaks and mends, a consumed extrude, an edit that changes no solid, the
// events, the file and a replay, and a read by the body identifier.
public sealed class RebuildTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-rebuild-" + Guid.NewGuid().ToString("N"));

    public RebuildTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private sealed record Part(
        DocumentSession Session, InMemoryEventSink Events, ManifoldGeometryBackend Backend,
        Guid Sketch, Guid[] Points, Guid[] Lines, Guid Body);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    // A session on Manifold with the rectangle on XY and an extrude of it by 5.
    private static async Task<Part> NewPart(ManifoldGeometryBackend backend)
    {
        var kit = EngineHosting.CreateDefault(backend);
        var session = new DocumentSession(kit);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var points = new Guid[Rectangle.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = Rectangle[i].X, Y = Rectangle[i].Y }), "pointId");
        var lines = new Guid[points.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }), "bodyId");
        return new Part(session, kit.Events, backend, sketch, points, lines, body);
    }

    private static async Task<Aabb> Box(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result;
    }

    private static Task<BodyRecord> Record(DocumentSession session, Guid body)
        => session.Read((document, _) => document.Bodies.Single(record => record.Handle.Id == body));

    private static (double, double, double, double, double, double) Six(Aabb box)
        => (box.MinX, box.MinY, box.MinZ, box.MaxX, box.MaxY, box.MaxZ);

    // ADR-0037 item 3: the key is a version 5 guid of RFC 9562 with fixed bits, and it is
    // never one of its two identifiers. CI runs this test on Windows x64, Linux x64 and
    // macOS arm64. The constant agrees with a computation in Node with crypto SHA-1 of the
    // same 48 bytes (TASK-0108), which shares no code with GeometryKeys.
    [Fact]
    public void A_Key_Is_A_Name_Based_Guid_With_Fixed_Bits()
    {
        var command = new Guid("11111111-2222-3333-4444-555555555555");
        var body = new Guid("66666666-7777-8888-9999-aaaaaaaaaaaa");

        var key = GeometryKeys.Derive(command, body);

        Assert.Equal(new Guid("0fe4ab0b-8d7a-5381-a0e5-152a7ab115b6"), key);
        Assert.Equal(key, GeometryKeys.Derive(command, body));
        Assert.NotEqual(key, GeometryKeys.Derive(body, command));
        Assert.Equal(5, key.Version);
        Assert.Equal(0x80, key.ToByteArray(bigEndian: true)[8] & 0xC0);
    }

    // ADR-0037 item 4: the steps in effect, as StepHistory gives them.
    [Fact]
    public void The_Steps_In_Effect_Follow_Each_Undo_And_Redo_Of_The_Log()
    {
        Command A = new NoOpCommand { Echo = "A" }, B = new NoOpCommand { Echo = "B" }, C = new NoOpCommand { Echo = "C" };
        LogEntry Entry(Command command) => new(command, new BackendIdentity("managed", "test"));

        Assert.Equal([A.CommandId, B.CommandId], SketchRebuild.InEffect([Entry(A), Entry(B)]));
        Assert.Equal([A.CommandId], SketchRebuild.InEffect([Entry(A), Entry(B), Entry(new UndoCommand())]));
        Assert.Equal([A.CommandId, B.CommandId], SketchRebuild.InEffect([Entry(A), Entry(B), Entry(new UndoCommand()), Entry(new RedoCommand())]));
        // A new step clears the undone list, so the Redo after it has no step to apply.
        Assert.Equal([A.CommandId, C.CommandId], SketchRebuild.InEffect([Entry(A), Entry(B), Entry(new UndoCommand()), Entry(C), Entry(new RedoCommand())]));
    }

    // ADR-0037 items 2 and 3: the solid of an extrude is under its derived key, so a read by
    // the body identifier fails, and a read through the record succeeds.
    [NativeManifoldFact]
    public async Task A_Read_By_The_Body_Identifier_Fails_Loudly()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);

        var record = await Record(part.Session, part.Body);

        Assert.Equal(GeometryKeys.Derive(part.Body, part.Body), record.Geometry);
        Assert.Throws<KeyNotFoundException>(() => backend.GetBoundingBox(new BodyHandle(part.Body)));
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 5.0), Six(backend.GetBoundingBox(new BodyHandle(record.GeometryKey))));
    }

    // ADR-0037 items 5 and 8: a move of a corner rebuilds the solid under the same body
    // identifier and a new key, body.changed gives the key, and an undo gives back the old
    // box and the old key.
    [NativeManifoldFact]
    public async Task An_Edit_Rebuilds_The_Solid_And_An_Undo_Gives_It_Back()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        var first = await Record(part.Session, part.Body);

        var move = new MoveSketchPointCommand { SketchId = part.Sketch, PointId = part.Points[2], X = 15, Y = 12 };
        var result = await Applied(part.Session, move);

        var rebuilt = await Record(part.Session, part.Body);
        Assert.Equal(GeometryKeys.Derive(move.CommandId, part.Body), rebuilt.Geometry);
        Assert.Null(rebuilt.Fault);
        Assert.Empty(result.Diagnostics);
        Assert.Equal((1.0, 2.0, 0.0, 15.0, 12.0, 5.0), Six(await Box(part.Session, part.Body)));
        var changed = part.Events.Snapshot().Last(record => record.Kind == "body.changed");
        var payload = (IReadOnlyDictionary<string, object?>)changed.Payload!;
        Assert.Equal((part.Body, rebuilt.GeometryKey, (object?)null), ((Guid)payload["bodyId"]!, (Guid)payload["geometry"]!, payload["fault"]));

        await Applied(part.Session, new UndoCommand());

        Assert.Equal(first, await Record(part.Session, part.Body));
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 5.0), Six(await Box(part.Session, part.Body)));

        await Applied(part.Session, new RedoCommand());

        Assert.Equal(rebuilt, await Record(part.Session, part.Body));
    }

    // ADR-0037 item 2: after a rebuild each read of the solid uses the new key: the
    // tessellation, the box, a Subtract and a Translate. A read that kept the body identifier
    // fails, and a read that kept the first key gives the first solid.
    [NativeManifoldFact]
    public async Task Each_Read_Of_A_Rebuilt_Solid_Uses_Its_New_Key()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        await Applied(part.Session, new MoveSketchPointCommand { SketchId = part.Sketch, PointId = part.Points[2], X = 15, Y = 12 });

        var tessellation = (await part.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = part.Body })).Result!;
        var xs = Enumerable.Range(0, tessellation.Positions.Length / 3).Select(i => tessellation.Positions[3 * i]).ToList();
        Assert.Equal((1.0, 15.0), (xs.Min(), xs.Max()));
        Assert.Equal(15.0, (await Box(part.Session, part.Body)).MaxX);

        var tool = Output(await Applied(part.Session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 }), "bodyId");
        var cut = Output(await Applied(part.Session, new SubtractCommand { MinuendBodyId = part.Body, SubtrahendBodyId = tool }), "bodyId");
        Assert.Equal((1.0, 2.0, 0.0, 15.0, 12.0, 5.0), Six(await Box(part.Session, cut)));

        var moved = Output(await Applied(part.Session, new TranslateCommand { BodyId = cut, Dx = 1, Dy = 0, Dz = 0 }), "bodyId");
        Assert.Equal(16.0, (await Box(part.Session, moved)).MaxX);
    }

    // ADR-0037 item 8: the snapshot of a subscription gives the key and the fault of a body.
    [NativeManifoldFact]
    public async Task The_Snapshot_Gives_The_Key_And_The_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        await Applied(part.Session, new DeleteSketchItemCommand { SketchId = part.Sketch, ItemId = part.Lines[3] });

        var record = await Record(part.Session, part.Body);
        var snapshot = await part.Session.Read((document, _) => Engine.Api.Http.WebSockets.SnapshotProjector.Project(document, seq: 0));

        var entry = Assert.Single(snapshot.Bodies);
        Assert.Equal((record.GeometryKey, record.Fault), (entry.Geometry, entry.Fault));
        Assert.NotEqual(part.Body, entry.Geometry);
        Assert.Equal(DiagnosticCodes.SketchRebuildFailed, entry.Fault?.Code);
    }

    // ADR-0037 item 5: two extrudes of one sketch each follow the edit, in the order of the log.
    [NativeManifoldFact]
    public async Task Each_Live_Extrude_Of_The_Sketch_Follows()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        var second = Output(await Applied(part.Session, new ExtrudeCommand { SketchId = part.Sketch, Distance = -2 }), "bodyId");

        var result = await Applied(part.Session, new MoveSketchPointCommand { SketchId = part.Sketch, PointId = part.Points[1], X = 21, Y = 2 });

        Assert.Equal((1.0, 2.0, 0.0, 21.0, 8.0, 5.0), Six(await Box(part.Session, part.Body)));
        Assert.Equal((1.0, 2.0, -2.0, 21.0, 8.0, 0.0), Six(await Box(part.Session, second)));
        var changed = part.Events.Snapshot().Where(record => record.Kind == "body.changed" && record.CauseCommandId == result.CommandId)
            .Select(record => (Guid)((IReadOnlyDictionary<string, object?>)record.Payload!)["bodyId"]!);
        Assert.Equal([part.Body, second], changed);
    }

    // ADR-0037 item 5: an edit that does not change the profile gives no body event: a point
    // with no line, and a constraint that the positions already satisfy.
    [NativeManifoldFact]
    public async Task An_Edit_That_Does_Not_Change_The_Solid_Changes_No_Body()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        var first = await Record(part.Session, part.Body);

        var point = await Applied(part.Session, new AddSketchPointCommand { SketchId = part.Sketch, X = 30, Y = 30 });
        var horizontal = await Applied(part.Session, new AddSketchConstraintCommand { SketchId = part.Sketch, Kind = "horizontal", First = part.Lines[0] });

        Assert.Equal(first, await Record(part.Session, part.Body));
        Assert.DoesNotContain(part.Events.Snapshot(), record => record.Kind == "body.changed"
            && (record.CauseCommandId == point.CommandId || record.CauseCommandId == horizontal.CommandId));
    }

    // ADR-0037 item 6 (Q2 A): an edit that opens the loop applies; the body keeps its box and
    // gets the fault, with the same code in the result; the line again mends the profile and
    // clears the fault. A sketch that is not solved gives the same fault.
    [NativeManifoldFact]
    public async Task A_Profile_That_Breaks_Keeps_The_Solid_With_A_Fault_Until_It_Mends()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        var first = await Record(part.Session, part.Body);

        var delete = await Applied(part.Session, new DeleteSketchItemCommand { SketchId = part.Sketch, ItemId = part.Lines[3] });

        var broken = await Record(part.Session, part.Body);
        Assert.Equal(first.Geometry, broken.Geometry);
        Assert.Equal(DiagnosticCodes.SketchRebuildFailed, broken.Fault?.Code);
        Assert.Contains("is the end of 1 line", broken.Fault!.Message);
        Assert.Equal(DiagnosticCodes.SketchRebuildFailed, Assert.Single(delete.Diagnostics).Code);
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 5.0), Six(await Box(part.Session, part.Body)));
        var payload = (IReadOnlyDictionary<string, object?>)part.Events.Snapshot().Last(record => record.Kind == "body.changed").Payload!;
        Assert.Equal(DiagnosticCodes.SketchRebuildFailed, ((IReadOnlyDictionary<string, object?>)payload["fault"]!)["code"]);

        var line = new AddSketchLineCommand { SketchId = part.Sketch, StartPointId = part.Points[3], EndPointId = part.Points[0] };
        await Applied(part.Session, line);

        var mended = await Record(part.Session, part.Body);
        Assert.Null(mended.Fault);
        Assert.Equal(GeometryKeys.Derive(line.CommandId, part.Body), mended.Geometry);

        await Applied(part.Session, new AddSketchConstraintCommand { SketchId = part.Sketch, Kind = "distance", First = part.Lines[0], Value = 10 });
        var conflict = await Applied(part.Session, new AddSketchConstraintCommand { SketchId = part.Sketch, Kind = "distance", First = part.Lines[0], Value = 7 });

        Assert.Contains("not solved", (await Record(part.Session, part.Body)).Fault!.Message);
        Assert.Equal(DiagnosticCodes.SketchRebuildFailed, Assert.Single(conflict.Diagnostics).Code);
    }

    // ADR-0037 item 7, which ADR-0039 amends (TASK-0116): after a Translate consumes the extrude,
    // an edit rebuilds the translated body with no warning; an extrude that an undo removed is
    // not in effect and gives no warning. Until TASK-0116 the edit gave W-SKETCH-EXTRUDE-CONSUMED
    // and changed no body.
    [NativeManifoldFact]
    public async Task A_Consumed_Extrude_Follows_And_An_Undone_One_Gives_No_Warning()
    {
        using var backend = new ManifoldGeometryBackend();
        var part = await NewPart(backend);
        var translate = new TranslateCommand { BodyId = part.Body, Dx = 0, Dy = 0, Dz = 10 };
        await Applied(part.Session, translate);

        var edit = await Applied(part.Session, new MoveSketchPointCommand { SketchId = part.Sketch, PointId = part.Points[2], X = 15, Y = 12 });

        Assert.Empty(edit.Diagnostics);
        Assert.Equal((1.0, 2.0, 10.0, 15.0, 12.0, 15.0), Six(await Box(part.Session, translate.CommandId)));
        Assert.Contains(part.Events.Snapshot(), record => record.Kind == "body.changed" && record.CauseCommandId == edit.CommandId);

        using var other = new ManifoldGeometryBackend();
        var undone = await NewPart(other);
        await Applied(undone.Session, new UndoCommand());
        var quiet = await Applied(undone.Session, new MoveSketchPointCommand { SketchId = undone.Sketch, PointId = undone.Points[2], X = 15, Y = 12 });
        Assert.Empty(quiet.Diagnostics);
    }

    // ADR-0037, Determinism, and ADR-0015: an open of the file applies each entry again, and
    // gives the same records, the same keys and the same boxes as the live session.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Keys_And_The_Same_Boxes()
    {
        var path = Path.Combine(_folder, "rebuild" + DocumentFile.Extension);
        IReadOnlyList<BodyRecord> expected;
        Aabb box;
        Guid body;
        using (var backend = new ManifoldGeometryBackend())
        {
            var kit = EngineHosting.CreateDefault(backend);
            var live = new DocumentSession(kit);
            DocumentFile.Create(live, path);
            var sketch = Output(await Applied(live, new CreateSketchCommand { DatumId = Datums.YZ.Id }), "sketchId");
            var points = new Guid[Rectangle.Length];
            for (var i = 0; i < points.Length; i++)
                points[i] = Output(await Applied(live, new AddSketchPointCommand { SketchId = sketch, X = Rectangle[i].X, Y = Rectangle[i].Y }), "pointId");
            var lines = new Guid[points.Length];
            for (var i = 0; i < lines.Length; i++)
                lines[i] = Output(await Applied(live, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
            body = Output(await Applied(live, new ExtrudeCommand { SketchId = sketch, Distance = -3 }), "bodyId");
            await Applied(live, new MoveSketchPointCommand { SketchId = sketch, PointId = points[1], X = 14, Y = 2 });
            await Applied(live, new DeleteSketchItemCommand { SketchId = sketch, ItemId = lines[2] });
            await Applied(live, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[2], EndPointId = points[3] });
            expected = await live.Read((document, _) => document.Bodies.ToList());
            box = await Box(live, body);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = new DocumentSession(EngineHosting.CreateDefault(backend));
            await DocumentFile.Open(opened, path);

            Assert.Equal(expected, await opened.Read((document, _) => document.Bodies.ToList()));
            Assert.Equal(box, await Box(opened, body));
            opened.Close();
        }
    }
}
