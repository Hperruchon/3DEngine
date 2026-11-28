using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Joints;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// ADR-0193 (phase P11a, TASK-0479): the joints of a part. ADR-0193 names this file in enforced-by. Each test holds one rule of
// the ADR: the place of the child for each kind, a connector on a face or on a datum, the tree and its refusals, the limits,
// the values, Undo and Redo, an open, the events and the query. The scene is a parent box of 40 x 30 x 10 and a child box of
// 20 x 10 x 6, each with its centre at the origin. The face "+x" of the parent has its centre at (20, 0, 0); a fixed joint
// to the face "-z" of the child puts the child from x = 20 to x = 26, with its x axis along y and its y axis along z.
public sealed class JointTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-joint-" + Guid.NewGuid().ToString("N"));

    public JointTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private const double Close = 1e-9;

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(IGeometryBackend? backend = null)
    {
        var kit = EngineHosting.CreateDefault(backend ?? new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

    // Each command of a test gets a new identifier, because a copy with "with" keeps the identifier, and the session gives the
    // cached result of an identifier again (ADR-0006 §7).
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command with { CommandId = Guid.NewGuid() });
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Task<string> Refused(DocumentSession session, Command command)
        => RefusedAsGiven(session, command with { CommandId = Guid.NewGuid() });

    private static async Task<string> RefusedAsGiven(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Guid Output(CommandResult result, string key)
        => result.Outputs.TryGet<Guid>(key, out var id) ? id : throw new InvalidOperationException($"No output {key}.");

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }), "bodyId");

    private static async Task<(Guid Parent, Guid Child)> Scene(DocumentSession session)
        => (await Box(session, 40, 30, 10), await Box(session, 20, 10, 6));

    // A joint from a face of the parent to a face of the child. The face of a box has the box as its feature.
    private static AddJointCommand Joint(string kind, Guid parent, string parentFace, Guid child, string childFace) => new()
    {
        Kind = kind,
        ParentBodyId = parent,
        ParentFeatureId = parent,
        ParentElement = parentFace,
        ChildBodyId = child,
        ChildFeatureId = child,
        ChildElement = childFace,
    };

    private static async Task<Guid> Joined(DocumentSession session, AddJointCommand command)
        => Output(await Applied(session, command), "jointId");

    private static async Task<FaceConnector> Face(DocumentSession session, Guid body, string element)
    {
        var result = await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return Assert.Single(result.Result!.Faces, face => face.Reference == new DatumReference(body, element, null));
    }

    private static void Near((double X, double Y, double Z) expected, DatumPoint? point)
    {
        Assert.NotNull(point);
        Assert.True(
            Math.Abs(point!.X - expected.X) < Close && Math.Abs(point.Y - expected.Y) < Close && Math.Abs(point.Z - expected.Z) < Close,
            $"Expected {expected}, got ({point.X}, {point.Y}, {point.Z}).");
    }

    private static void Near((double X, double Y, double Z) expected, DatumAxis? axis)
        => Near(expected, axis is null ? null : new DatumPoint(axis.X, axis.Y, axis.Z));

    // The placement of each live body in the order of the log of its creator, for two sessions with other identifiers.
    private static Task<IReadOnlyList<BodyPlacement?>> Placed(DocumentSession session)
        => session.Read<IReadOnlyList<BodyPlacement?>>((document, _) =>
            [.. document.Log.Select(entry => document.Bodies.FirstOrDefault(body => body.Handle.Id == entry.Command.CommandId)).OfType<BodyRecord>().Select(body => body.Placement)]);

    private static Task<IReadOnlyList<BodyRecord>> Bodies(DocumentSession session)
        => session.Read<IReadOnlyList<BodyRecord>>((document, _) => document.Bodies.OrderBy(body => body.Handle.Id).ToList());

    private static async Task<IReadOnlyList<JointRecord>> Joints(DocumentSession session)
    {
        var result = await session.Query<JointsInfo>(new GetJointsQuery());
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!.Joints;
    }

    // Items 2 and 3 (the answers Q1 A and Q2 A): a fixed joint puts the face of the child on the face of the parent, with the
    // two normals opposite.
    [Fact]
    public async Task A_Fixed_Joint_Puts_The_Child_Face_On_The_Parent_Face_With_Opposite_Normals()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        await Joined(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z"));

        var meeting = await Face(session, child, "-z");
        Near((20, 0, 0), meeting.Point);
        Near((-1, 0, 0), meeting.Direction);
        Near((23, 10, 0), (await Face(session, child, "+x")).Point);
        Near((26, 0, 0), (await Face(session, child, "+z")).Point);
    }

    // Items 3 and 4 (the answer Q3 A): a pivot turns the child counterclockwise about the normal of the parent connector by the
    // turn of its cosine and its sine. The length of the pair does not change the turn.
    [Fact]
    public async Task A_Pivot_Turns_The_Child_Counterclockwise_About_The_Parent_Normal()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var joint = await Joined(session, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = 0, Sin = 1 });
        Near((23, 0, 10), (await Face(session, child, "+x")).Point);
        Near((20, 0, 0), (await Face(session, child, "-z")).Point);

        await Applied(session, new SetJointValueCommand { JointId = joint, Cos = -1, Sin = 0 });
        Near((23, -10, 0), (await Face(session, child, "+x")).Point);

        await Applied(session, new SetJointValueCommand { JointId = joint, Cos = 0, Sin = -3 });
        Near((23, 0, -10), (await Face(session, child, "+x")).Point);

        await Applied(session, new SetJointValueCommand { JointId = joint, Cos = 3, Sin = 4 });
        Near((23, 6, 8), (await Face(session, child, "+x")).Point);
    }

    // Items 3 and 4: a slider moves the child along the normal of the parent connector by its distance.
    [Fact]
    public async Task A_Slider_Moves_The_Child_Along_The_Parent_Normal()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var joint = await Joined(session, Joint(JointRecord.Slider, parent, "+x", child, "-z") with { Distance = 4 });
        Near((24, 0, 0), (await Face(session, child, "-z")).Point);

        await Applied(session, new SetJointValueCommand { JointId = joint, Distance = -2.5 });
        Near((17.5, 0, 0), (await Face(session, child, "-z")).Point);
    }

    // Item 2: a named datum is a connector too. The child face meets the datum at its origin, with the normals opposite.
    [Fact]
    public async Task A_Datum_Connector_Puts_The_Child_Face_On_The_Datum()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = parent, Element = "+z", DatumName = "top" }), "datumId");
        var frame = await session.Read((document, _) => document.Datums.Single().Frame);
        await Joined(session, new AddJointCommand
        {
            Kind = JointRecord.Fixed, ParentBodyId = parent, ParentDatumId = datum,
            ChildBodyId = child, ChildFeatureId = child, ChildElement = "-z",
        });

        var meeting = await Face(session, child, "-z");
        Near((frame.Origin.X, frame.Origin.Y, frame.Origin.Z), meeting.Point);
        Near((-frame.Normal.X, -frame.Normal.Y, -frame.Normal.Z), meeting.Direction);
        var stored = Assert.Single(await Joints(session));
        Assert.Equal(datum, stored.Parent.DatumId);
        Assert.Null(stored.Parent.Face);
    }

    // Item 2: a connector names one face or one datum of its body. Finding T206 of the second codebase review of 2026-11-28
    // (TASK-0488): these refusals need no native backend, so they run on each computer; until TASK-0488 they were in the test of
    // the sphere below.
    [Fact]
    public async Task A_Connector_Is_One_Face_Or_One_Datum_Of_Its_Body()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = parent, Element = "+z", DatumName = "top" }), "datumId");
        var fixedJoint = Joint(JointRecord.Fixed, parent, "+x", child, "-z");

        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, fixedJoint with { ParentDatumId = datum }));
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, fixedJoint with { ParentFeatureId = null, ParentElement = null }));
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, fixedJoint with { ParentElement = "+w" }));
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, fixedJoint with { ChildFeatureId = null, ChildElement = null, ChildDatumId = datum }));
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, fixedJoint with { ChildFeatureId = null, ChildElement = null, ChildDatumId = Guid.NewGuid() }));
        Assert.Empty(await Joints(session));
    }

    // Item 2: a face is a plane or the side of a cylinder, so the face of a sphere gives no frame. A sphere needs the backend
    // Manifold.
    [NativeManifoldFact]
    public async Task A_Curved_Face_That_Is_Not_The_Side_Of_A_Cylinder_Is_No_Connector()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var parent = (await Scene(session)).Parent;
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 5 }), "bodyId");
        var curved = (await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = sphere })).Result!.Faces.Single().Reference;
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, new AddJointCommand
        {
            Kind = JointRecord.Fixed, ParentBodyId = parent, ParentFeatureId = parent, ParentElement = "+x",
            ChildBodyId = sphere, ChildFeatureId = curved.FeatureId, ChildElement = curved.Element, ChildItemId = curved.ItemId,
        }));
        Assert.Empty(await Joints(session));
    }

    // Items 1 and 4: the kind and the value of a joint. A fixed joint takes no value, a pivot takes a cosine and a sine that are
    // not both zero, and a slider takes one finite distance.
    [Fact]
    public async Task Each_Kind_Takes_Its_Own_Value()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        AddJointCommand Of(string kind) => Joint(kind, parent, "+x", child, "-z");

        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of("hinge")));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Fixed) with { Cos = 1, Sin = 0 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot)));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot) with { Cos = 1 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot) with { Cos = 0, Sin = 0 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot) with { Cos = double.NaN, Sin = 1 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot) with { Cos = 1e200, Sin = 1e200 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Pivot) with { Cos = 1, Sin = 0, Distance = 2 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Slider)));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Slider) with { Distance = double.PositiveInfinity }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Slider) with { Distance = 1, Sin = 0 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Of(JointRecord.Fixed) with { ChildBodyId = parent, ChildFeatureId = parent }));
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, await Refused(session, Of(JointRecord.Fixed) with { ChildBodyId = Guid.NewGuid() }));

        var joint = await Joined(session, Of(JointRecord.Fixed));
        Assert.Equal(JointValue.None, Assert.Single(await Joints(session)).Value);
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, new SetJointValueCommand { JointId = joint, Cos = 0, Sin = 1 }));
        Assert.Equal(DiagnosticCodes.JointNotFound, await Refused(session, new SetJointValueCommand { JointId = Guid.NewGuid(), Distance = 1 }));
        Assert.Equal(DiagnosticCodes.JointNotFound, await Refused(session, new RemoveJointCommand { JointId = Guid.NewGuid() }));
    }

    // Item 5: the limits of a pivot go counterclockwise from the lower to the upper turn, and a slider takes each distance
    // from the lower to the upper distance. A value outside the limits is refused, and the child does not move.
    [Fact]
    public async Task A_Value_Outside_The_Limits_Is_Refused()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var pivot = Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { LowerCos = 0, LowerSin = -1, UpperCos = 0, UpperSin = 1 };
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, pivot with { Cos = -1, Sin = 0 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, pivot with { Cos = 1, Sin = 0, UpperSin = null }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, pivot with { Cos = 1, Sin = 0, LowerDistance = 0, UpperDistance = 1 }));
        var joint = await Joined(session, pivot with { Cos = 1, Sin = 0 });

        foreach (var (c, s) in new[] { (0.0, -1.0), (0.0, 1.0), (1.0, 1.0), (1.0, -1.0), (2.0, 0.0) })
            await Applied(session, new SetJointValueCommand { JointId = joint, Cos = c, Sin = s });
        var before = await Bodies(session);
        foreach (var (c, s) in new[] { (-1.0, 0.0), (-1.0, 1.0), (-1.0, -1.0), (-1e-9, 1.0) })
            Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, new SetJointValueCommand { JointId = joint, Cos = c, Sin = s }));
        Assert.Equal(before, await Bodies(session));

        var (other, slid) = await Scene(session);
        var slider = Joint(JointRecord.Slider, other, "+z", slid, "-z") with { LowerDistance = 0, UpperDistance = 10 };
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, slider with { Distance = 1, LowerDistance = 11 }));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Joint(JointRecord.Fixed, other, "+z", slid, "-z") with { LowerDistance = 0, UpperDistance = 1 }));
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, slider with { Distance = -0.5 }));
        var rail = await Joined(session, slider with { Distance = 0 });
        await Applied(session, new SetJointValueCommand { JointId = rail, Distance = 10 });
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, new SetJointValueCommand { JointId = rail, Distance = 10.000001 }));
        Assert.Equal(new JointLimits(new JointValue(1, 0, 0), new JointValue(1, 0, 10)), (await Joints(session)).Last().Limits);
    }

    // Item 6: a body has one parent joint, and a joint that closes a loop is refused.
    [Fact]
    public async Task A_Second_Parent_And_A_Loop_Are_Refused()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        await Joined(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z"));
        await Joined(session, Joint(JointRecord.Fixed, child, "+z", third, "-z"));

        Assert.Equal(DiagnosticCodes.JointParentInUse, await Refused(session, Joint(JointRecord.Fixed, third, "+x", child, "-x")));
        Assert.Equal(DiagnosticCodes.JointLoop, await Refused(session, Joint(JointRecord.Fixed, third, "+x", parent, "-x")));
        Assert.Equal(DiagnosticCodes.JointLoop, await Refused(session, Joint(JointRecord.Fixed, child, "+y", parent, "-x")));
        Assert.Equal(2, (await Joints(session)).Count);
    }

    // Item 6 (the answer Q4 A): a PlaceBody of a root moves its whole tree in the same step, a PlaceBody of a child is refused,
    // and one Undo moves the tree back.
    [Fact]
    public async Task A_PlaceBody_Of_A_Root_Moves_Its_Tree_And_A_PlaceBody_Of_A_Child_Is_Refused()
    {
        var (session, events) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        await Joined(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z"));
        await Joined(session, Joint(JointRecord.Fixed, child, "+z", third, "-z"));
        Near((34, 0, 0), (await Face(session, third, "+z")).Point);
        var before = await Bodies(session);

        Assert.Equal(DiagnosticCodes.JointPlaced, await Refused(session, new PlaceBodyCommand { BodyId = child, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 1, Dy = 0, Dz = 0 }));
        Assert.Equal(DiagnosticCodes.JointPlaced, await Refused(session, new PlaceBodyCommand { BodyId = third, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 1, Dy = 0, Dz = 0 }));

        var mark = events.Snapshot().Count;
        await Applied(session, new PlaceBodyCommand { BodyId = parent, Qw = 1, Qx = 0, Qy = 0, Qz = 1, Dx = 100, Dy = 0, Dz = 0 });
        var moved = events.Snapshot().Skip(mark).ToList();
        Assert.Equal(["command.applied", "body.changed", "body.changed", "body.changed"], moved.Select(record => record.Kind));
        Assert.Equal([parent, child, third], moved.Skip(1).Select(record => (Guid)((IReadOnlyDictionary<string, object?>)record.Payload!)["bodyId"]!));
        Near((100, 20, 0), (await Face(session, child, "-z")).Point);
        Near((100, 34, 0), (await Face(session, third, "+z")).Point);

        await Applied(session, new UndoCommand());
        Assert.Equal(before, await Bodies(session));
    }

    // Items 4 and 6: SetJointValue places the child and each body under it, and gives the same placements as an AddJoint with
    // that value. One Undo gives the value and the placements back.
    [Fact]
    public async Task SetJointValue_Moves_The_Subtree_As_An_AddJoint_With_That_Value()
    {
        async Task<(DocumentSession Session, Guid Joint)> Build(double cos, double sin)
        {
            var (session, _) = NewSession();
            var (parent, child) = await Scene(session);
            var third = await Box(session, 8, 8, 8);
            var joint = await Joined(session, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = cos, Sin = sin });
            await Joined(session, Joint(JointRecord.Fixed, child, "+x", third, "-z"));
            return (session, joint);
        }

        var (changed, joint) = await Build(1, 0);
        var start = await Bodies(changed);
        await Applied(changed, new SetJointValueCommand { JointId = joint, Cos = 0.6, Sin = 0.8 });
        var (direct, _) = await Build(0.6, 0.8);
        Assert.Equal(await Placed(direct), await Placed(changed));

        await Applied(changed, new UndoCommand());
        Assert.Equal(start, await Bodies(changed));
        Assert.Equal(new JointValue(1, 0, 0), (await Joints(changed)).First().Value);
    }

    // Items 1 and 6: Undo of an AddJoint removes the joint and gives the child its placement back; Redo gives both again.
    // RemoveJoint keeps the placement of the child, which a PlaceBody can then move.
    [Fact]
    public async Task Undo_Redo_And_RemoveJoint_Keep_The_Joint_And_The_Child_Together()
    {
        var (session, events) = NewSession();
        var (parent, child) = await Scene(session);
        var before = await Bodies(session);
        var mark = events.Snapshot().Count;
        var joint = await Joined(session, Joint(JointRecord.Slider, parent, "+x", child, "-z") with { Distance = 3 });
        Assert.Equal(["command.applied", "body.changed", "joint.created"], events.Snapshot().Skip(mark).Select(record => record.Kind));
        var after = await Bodies(session);
        var record = Assert.Single(await Joints(session));
        Assert.Equal(joint, record.Id);
        Assert.Equal(new JointValue(1, 0, 3), record.Value);

        await Applied(session, new UndoCommand());
        Assert.Empty(await Joints(session));
        Assert.Equal(before, await Bodies(session));
        await Applied(session, new RedoCommand());
        Assert.Equal(record, Assert.Single(await Joints(session)));
        Assert.Equal(after, await Bodies(session));

        mark = events.Snapshot().Count;
        await Applied(session, new SetJointValueCommand { JointId = joint, Distance = 5 });
        Assert.Equal(["command.applied", "body.changed", "joint.changed"], events.Snapshot().Skip(mark).Select(record => record.Kind));

        mark = events.Snapshot().Count;
        await Applied(session, new RemoveJointCommand { JointId = joint });
        Assert.Equal(["command.applied", "joint.removed"], events.Snapshot().Skip(mark).Select(record => record.Kind));
        Assert.Empty(await Joints(session));
        Near((25, 0, 0), (await Face(session, child, "-z")).Point);
        await Applied(session, new PlaceBodyCommand { BodyId = child, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 0, Dy = 0, Dz = 0 });
        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());
        Assert.Equal(joint, Assert.Single(await Joints(session)).Id);
    }

    // Item 7: GetJoints gives each joint in the order of the log of its AddJoint, and an event of a joint carries its record.
    [Fact]
    public async Task GetJoints_Gives_The_Joints_In_The_Order_Of_The_Log()
    {
        var (session, events) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        var second = await Joined(session, Joint(JointRecord.Fixed, child, "+z", third, "-z"));
        var first = await Joined(session, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = 1, Sin = 0 });
        Assert.Equal([second, first], (await Joints(session)).Select(joint => joint.Id));

        var created = (IReadOnlyDictionary<string, object?>)events.Snapshot().Last(record => record.Kind == "joint.created").Payload!;
        Assert.Equal(first, created["jointId"]);
        Assert.Equal(JointRecord.Pivot, created["kind"]);
        Assert.Equal(parent, created["parentBodyId"]);
        Assert.Equal(child, created["childBodyId"]);
        Assert.Equal(new JointValue(1, 0, 0), created["value"]);
        Assert.Null(created["limits"]);
    }

    // Items 1 and 8: a joint takes the identifier of its command, so the identifier of a command of the log is refused.
    [Fact]
    public async Task A_Joint_With_The_Identifier_Of_A_Command_Of_The_Log_Is_Refused()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand()); // as for a sheet: refused Redo commands fill the cache of the results
        Assert.Equal(DiagnosticCodes.JointIdInUse, await RefusedAsGiven(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z") with { CommandId = parent }));
    }

    // Item 3: the log holds the numbers of the commands, so an open of the file gives the same joints and the same placements,
    // bit for bit.
    [Fact]
    public async Task An_Open_Gives_The_Same_Joints_And_Placements()
    {
        var path = Path.Combine(_folder, "joints" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var (parent, child) = await Scene(live);
        var third = await Box(live, 8, 8, 8);
        var joint = await Joined(live, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = 0.28, Sin = 0.96, LowerCos = 1, LowerSin = 0, UpperCos = -1, UpperSin = 0 });
        await Joined(live, Joint(JointRecord.Slider, child, "+y", third, "-z") with { Distance = 1.5 });
        await Applied(live, new SetJointValueCommand { JointId = joint, Cos = 0.6, Sin = 0.8 });
        await Applied(live, new PlaceBodyCommand { BodyId = parent, Qw = 0.9, Qx = 0.3, Qy = -0.2, Qz = 0.25, Dx = 12.5, Dy = -3.25, Dz = 7.1 });
        var bodies = await Bodies(live);
        var joints = await Joints(live);
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        Assert.Equal(bodies, await Bodies(opened));
        Assert.Equal(joints, await Joints(opened));
        opened.Close();
    }
}
