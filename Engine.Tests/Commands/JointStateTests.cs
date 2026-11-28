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

// ADR-0194 (phase P11b, TASK-0489): the loose state of a joint, the intended pair of each joint in CheckInterference, and
// PlaceByConnectors. ADR-0194 names this file in enforced-by. The scene is the scene of JointTests: a parent box of 40 x 30 x 10
// and a child box of 20 x 10 x 6, each with its centre at the origin; a fixed joint from the face "+x" of the parent to the face
// "-z" of the child puts the face "-z" of the child at (20, 0, 0).
public sealed class JointStateTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-joint-state-" + Guid.NewGuid().ToString("N"));

    public JointStateTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private const double Close = 1e-9;

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(IGeometryBackend? backend = null)
    {
        var kit = EngineHosting.CreateDefault(backend ?? new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

    // Each command gets a new identifier, because a copy with "with" keeps the identifier (ADR-0006 §7).
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command with { CommandId = Guid.NewGuid() });
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command with { CommandId = Guid.NewGuid() });
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Guid Output(CommandResult result, string key)
        => result.Outputs.TryGet<Guid>(key, out var id) ? id : throw new InvalidOperationException($"No output {key}.");

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }), "bodyId");

    private static async Task<(Guid Parent, Guid Child)> Scene(DocumentSession session)
        => (await Box(session, 40, 30, 10), await Box(session, 20, 10, 6));

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

    private static PlaceByConnectorsCommand Snap(Guid parent, string parentFace, Guid child, string childFace) => new()
    {
        ParentBodyId = parent,
        ParentFeatureId = parent,
        ParentElement = parentFace,
        ChildBodyId = child,
        ChildFeatureId = child,
        ChildElement = childFace,
    };

    private static PlaceBodyCommand Move(Guid body, double dx) => new() { BodyId = body, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = dx, Dy = 0, Dz = 0 };

    private static SetJointStateCommand State(Guid joint, string state) => new() { JointId = joint, State = state };

    private static async Task<DatumPoint> Centre(DocumentSession session, Guid body, string element)
    {
        var result = await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return Assert.Single(result.Result!.Faces, face => face.Reference == new DatumReference(body, element, null)).Point!;
    }

    private static async Task Near(DocumentSession session, Guid body, string element, (double X, double Y, double Z) expected)
    {
        var point = await Centre(session, body, element);
        Assert.True(
            Math.Abs(point.X - expected.X) < Close && Math.Abs(point.Y - expected.Y) < Close && Math.Abs(point.Z - expected.Z) < Close,
            $"Expected {expected}, got ({point.X}, {point.Y}, {point.Z}).");
    }

    private static async Task<IReadOnlyList<JointRecord>> Joints(DocumentSession session)
        => (await session.Query<JointsInfo>(new GetJointsQuery())).Result!.Joints;

    private static Task<IReadOnlyList<BodyRecord>> Bodies(DocumentSession session)
        => session.Read<IReadOnlyList<BodyRecord>>((document, _) => document.Bodies.OrderBy(body => body.Handle.Id).ToList());

    // Items 1 and 2 (the answer Q5 A): a loose joint keeps its record and gives no placement. The child takes a PlaceBody and does
    // not follow its parent; a new value changes the record only; the state "tight" places the child at the value again; one
    // Undo gives back the loose joint and the place of the child.
    [Fact]
    public async Task A_Loose_Joint_Keeps_Its_Record_And_Gives_No_Placement_Until_It_Is_Tight_Again()
    {
        var (session, events) = NewSession();
        var (parent, child) = await Scene(session);
        var joint = Output(await Applied(session, Joint(JointRecord.Slider, parent, "+x", child, "-z") with { Distance = 2 }), "jointId");
        Assert.Equal(JointRecord.Tight, Assert.Single(await Joints(session)).State);
        await Near(session, child, "-z", (22, 0, 0));

        var mark = events.Snapshot().Count;
        await Applied(session, State(joint, JointRecord.Loose));
        Assert.Equal(["command.applied", "joint.changed"], events.Snapshot().Skip(mark).Select(record => record.Kind));
        var changed = (IReadOnlyDictionary<string, object?>)events.Snapshot().Last().Payload!;
        Assert.Equal(JointRecord.Loose, changed["state"]);

        await Applied(session, Move(child, 100));
        await Near(session, child, "-z", (100, 0, -3));
        await Applied(session, Move(parent, -50));
        await Near(session, child, "-z", (100, 0, -3));
        await Applied(session, new SetJointValueCommand { JointId = joint, Distance = 5 });
        await Near(session, child, "-z", (100, 0, -3));
        Assert.Equal(new JointValue(1, 0, 5), Assert.Single(await Joints(session)).Value);
        var loose = await Bodies(session);

        await Applied(session, State(joint, JointRecord.Tight));
        await Near(session, child, "-z", (-25, 0, 0));
        await Applied(session, new UndoCommand());
        Assert.Equal(loose, await Bodies(session));
        Assert.Equal(JointRecord.Loose, Assert.Single(await Joints(session)).State);
    }

    // Items 1 and 2: a loose joint still counts for the one parent and for a loop, and a state that the joint has, a state that
    // is not a state, and a joint that is not in the Document are refused.
    [Fact]
    public async Task A_Loose_Joint_Still_Counts_For_The_Tree_And_Each_Wrong_State_Is_Refused()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        var joint = Output(await Applied(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z")), "jointId");
        await Applied(session, State(joint, JointRecord.Loose));

        Assert.Equal(DiagnosticCodes.JointParentInUse, await Refused(session, Joint(JointRecord.Fixed, third, "+x", child, "-x")));
        Assert.Equal(DiagnosticCodes.JointLoop, await Refused(session, Joint(JointRecord.Fixed, child, "+y", parent, "-x")));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, State(joint, JointRecord.Loose)));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, State(joint, "free")));
        Assert.Equal(DiagnosticCodes.JointNotFound, await Refused(session, State(Guid.NewGuid(), JointRecord.Tight)));
    }

    // Item 3 (ADR-0157 item 5): each joint of the Document, tight or loose, declares its two bodies as an intended pair of
    // CheckInterference, with no parameter; a pair with no joint stays not intended.
    [NativeManifoldFact]
    public async Task Each_Joint_Declares_Its_Two_Bodies_As_An_Intended_Pair_Of_The_Check()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        var joint = Output(await Applied(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z")), "jointId");

        async Task<IReadOnlyList<PairCheck>> Pairs()
        {
            var result = await session.Query<InterferenceInfo>(new CheckInterferenceQuery());
            Assert.True(result.Error is null, result.Error?.Message);
            return result.Result!.Pairs;
        }

        bool Of(PairCheck pair, Guid a, Guid b) => (pair.BodyIdA == a && pair.BodyIdB == b) || (pair.BodyIdA == b && pair.BodyIdB == a);
        var pairs = await Pairs();
        Assert.True(Assert.Single(pairs, pair => Of(pair, parent, child)).Intended);
        Assert.False(Assert.Single(pairs, pair => Of(pair, parent, third)).Intended);
        Assert.False(Assert.Single(pairs, pair => Of(pair, child, third)).Intended);

        await Applied(session, State(joint, JointRecord.Loose));
        Assert.True(Assert.Single(await Pairs(), pair => Of(pair, parent, child)).Intended);
    }

    // Item 4 (the answer Q6 A): PlaceByConnectors gives the child the placement of a fixed joint and records no joint; a body under
    // the child through a tight joint follows it in the same step; one Undo gives each place back.
    [Fact]
    public async Task PlaceByConnectors_Places_The_Child_As_A_Fixed_Joint_And_Records_No_Joint()
    {
        var (session, events) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        await Applied(session, Joint(JointRecord.Fixed, child, "+z", third, "-z"));
        await Applied(session, Move(parent, 100));
        var before = await Bodies(session);

        var mark = events.Snapshot().Count;
        Assert.Equal(child, Output(await Applied(session, Snap(parent, "+x", child, "-z")), "bodyId"));
        Assert.Equal(["command.applied", "body.changed", "body.changed"], events.Snapshot().Skip(mark).Select(record => record.Kind));
        await Near(session, child, "-z", (120, 0, 0));
        await Near(session, third, "+z", (134, 0, 0));
        Assert.Single(await Joints(session));

        await Applied(session, new UndoCommand());
        Assert.Equal(before, await Bodies(session));
    }

    // Item 4: PlaceByConnectors refuses the child of a tight joint, a parent under the child through tight joints, the same body,
    // and a wrong connector; the child of a loose joint takes it.
    [Fact]
    public async Task PlaceByConnectors_Refuses_A_Held_Child_A_Loop_And_A_Wrong_Connector()
    {
        var (session, _) = NewSession();
        var (parent, child) = await Scene(session);
        var third = await Box(session, 8, 8, 8);
        var joint = Output(await Applied(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z")), "jointId");

        Assert.Equal(DiagnosticCodes.JointPlaced, await Refused(session, Snap(third, "+x", child, "-x")));
        Assert.Equal(DiagnosticCodes.JointLoop, await Refused(session, Snap(child, "+y", parent, "-x")));
        Assert.Equal(DiagnosticCodes.JointInvalid, await Refused(session, Snap(third, "+x", third, "-x")));
        Assert.Equal(DiagnosticCodes.JointConnectorInvalid, await Refused(session, Snap(third, "+w", parent, "-x")));
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, await Refused(session, Snap(Guid.NewGuid(), "+x", third, "-x")));

        await Applied(session, State(joint, JointRecord.Loose));
        await Applied(session, Snap(third, "+x", child, "-z"));
        await Near(session, child, "-z", (4, 0, 0));
    }

    // Items 1, 2 and 4: the log holds the commands, so an open gives the same joints with their states and the same placements.
    [Fact]
    public async Task An_Open_Gives_The_Same_States_And_Placements()
    {
        var path = Path.Combine(_folder, "states" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var (parent, child) = await Scene(live);
        var third = await Box(live, 8, 8, 8);
        var joint = Output(await Applied(live, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = 0.6, Sin = 0.8 }), "jointId");
        await Applied(live, State(joint, JointRecord.Loose));
        await Applied(live, Snap(third, "+y", child, "-x"));
        await Applied(live, Move(parent, 7.5));
        var bodies = await Bodies(live);
        var joints = await Joints(live);
        var face = await Centre(live, child, "+x");
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        Assert.Equal(bodies, await Bodies(opened));
        Assert.Equal(joints, await Joints(opened));
        Assert.Equal(face, await Centre(opened, child, "+x"));
        opened.Close();
    }
}
