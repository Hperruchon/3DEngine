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

// The findings E251, E253, E254 and T206 of the second codebase review of 2026-11-28 (phase P0.201, TASK-0488). The scene is the
// scene of JointTests: a parent box of 40 x 30 x 10 and a child box of 20 x 10 x 6, each with its centre at the origin; a fixed
// joint from the face "+x" of the parent to the face "-z" of the child puts the face "+x" of the child at (23, 10, 0).
public sealed class JointFindingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-joint-findings-" + Guid.NewGuid().ToString("N"));

    public JointFindingsTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private const double Close = 1e-9;

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static DocumentSession NewSession(IGeometryBackend? backend = null)
        => new(EngineHosting.CreateDefault(backend ?? new InProcessMeshBackend()));

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

    private static PlaceBodyCommand Move(Guid body, double dx) => new() { BodyId = body, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = dx, Dy = 0, Dz = 0 };

    private static async Task<IReadOnlyList<FaceConnector>> Faces(DocumentSession session, Guid body)
    {
        var result = await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!.Faces;
    }

    private static void Near((double X, double Y, double Z) expected, DatumPoint? point)
    {
        Assert.NotNull(point);
        Assert.True(
            Math.Abs(point!.X - expected.X) < Close && Math.Abs(point.Y - expected.Y) < Close && Math.Abs(point.Z - expected.Z) < Close,
            $"Expected {expected}, got ({point.X}, {point.Y}, {point.Z}).");
    }

    // E251: a log that holds one PlaceBody identifier two times. The second entry changes no placement. Until TASK-0488 the
    // feature list took the placements of the first entry for it, so the solid of the child was where the joint had put it before
    // the SetJointValue, at (123, 10, 0), while its record was right. A replay applies each entry, as an open does.
    [Fact]
    public async Task A_Command_Identifier_Two_Times_In_A_Log_Gives_Each_Step_Its_Own_Placements()
    {
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry());
        var place = new PlaceBodyCommand { CommandId = Id(4), BodyId = Id(1), Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = 100, Dy = 0, Dz = 0 };
        Command[] log =
        [
            new CreateBoxCommand { CommandId = Id(1), SizeX = 40, SizeY = 30, SizeZ = 10 },
            new CreateBoxCommand { CommandId = Id(2), SizeX = 20, SizeY = 10, SizeZ = 6 },
            Joint(JointRecord.Pivot, Id(1), "+x", Id(2), "-z") with { CommandId = Id(3), Cos = 1, Sin = 0 },
            place,
            new SetJointValueCommand { CommandId = Id(5), JointId = Id(3), Cos = 0, Sin = 1 },
            place,
        ];

        var backend = new InProcessMeshBackend();
        var replay = await Replay.ReplayLog(log, registry, backend);
        Assert.Equal(log.Length, replay.Document.Log.Count);
        var result = await new GetConnectorsQueryHandler().Handle(new GetConnectorsQuery { BodyId = Id(2) }, replay.Document, backend, CancellationToken.None);
        var faces = ((ConnectorsInfo)result.Result!).Faces;
        Near((123, 0, 10), Assert.Single(faces, face => face.Reference == new DatumReference(Id(2), "+x", null)).Point);
    }

    // E253: the check of the limits of a pivot is exact. Until TASK-0488 the turn back by the lower limit was in doubles, so the
    // answer near a limit depended on the rounding: the value below is a little after the upper limit in exact numbers, and the
    // check in doubles took it. A search of random limits found it. A value with the direction of a limit is in the limits at any
    // length, and a large pair is exact too.
    [Fact]
    public async Task The_Check_Of_The_Limits_Of_A_Pivot_Is_Exact()
    {
        var session = NewSession();
        var (parent, child) = (await Box(session, 40, 30, 10), await Box(session, 20, 10, 6));
        var pivot = Joint(JointRecord.Pivot, parent, "+x", child, "-z") with
        {
            LowerCos = 0.9957840635062241, LowerSin = 0.09172839727713766, UpperCos = 0.41756983652362484, UpperSin = 0.9086448324981732,
        };
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, pivot with { Cos = 2.505419019141749, Sin = 5.451868994989039 }));

        var joint = Output(await Applied(session, pivot with { Cos = 0.9957840635062241 * 4, Sin = 0.09172839727713766 * 4 }), "jointId");
        await Applied(session, new SetJointValueCommand { JointId = joint, Cos = 0.41756983652362484 * 0.5, Sin = 0.9086448324981732 * 0.5 });
        await Applied(session, new SetJointValueCommand { JointId = joint, Cos = 7e150, Sin = 7e150 });
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, new SetJointValueCommand { JointId = joint, Cos = 1, Sin = 0 }));
        Assert.Equal(DiagnosticCodes.JointLimit, await Refused(session, new SetJointValueCommand { JointId = joint, Cos = -1e150, Sin = 1e150 }));
    }

    // E254 (the answer Q1 A of the review): a tight joint whose parent a command consumed does not hold its child, so a PlaceBody of
    // the child applies; the joint still counts for the one parent.
    [NativeManifoldFact]
    public async Task The_Child_Of_A_Joint_Whose_Parent_Is_Consumed_Takes_A_PlaceBody()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (parent, child) = (await Box(session, 40, 30, 10), await Box(session, 20, 10, 6));
        var third = await Box(session, 8, 8, 8);
        await Applied(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z"));
        Assert.Equal(DiagnosticCodes.JointPlaced, await Refused(session, Move(child, 1)));

        await Applied(session, new TranslateCommand { BodyId = parent, Dx = 0, Dy = 50, Dz = 0 });
        await Applied(session, Move(child, 1));
        Assert.Equal(DiagnosticCodes.JointParentInUse, await Refused(session, Joint(JointRecord.Fixed, third, "+x", child, "-x")));
    }

    // T206: a boolean of the child of a joint takes the placement that the joint gave, as ADR-0193 item 9 says: the result of a
    // Subtract has the placement of its first body, and its face "+x" is where the face of the child was.
    [NativeManifoldFact]
    public async Task A_Boolean_Of_The_Child_Of_A_Joint_Keeps_The_Placement_Of_The_Joint()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (parent, child) = (await Box(session, 40, 30, 10), await Box(session, 20, 10, 6));
        var tool = await Box(session, 2, 2, 40);
        await Applied(session, Joint(JointRecord.Fixed, parent, "+x", child, "-z"));
        var before = Assert.Single(await Faces(session, child), face => face.Reference == new DatumReference(child, "+x", null)).Point;
        Near((23, 10, 0), before);

        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = child, SubtrahendBodyId = tool }), "bodyId");
        Near((23, 10, 0), Assert.Single(await Faces(session, cut), face => face.Reference == new DatumReference(child, "+x", null)).Point);
    }

    // T206: the side of a cylinder is a connector: a fixed joint from the face "+z" of a box puts the base of the axis on the face,
    // with the axis opposite to its normal.
    [NativeManifoldFact]
    public async Task The_Side_Of_A_Cylinder_Is_A_Connector()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var parent = await Box(session, 40, 30, 10);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 3, Height = 12 }), "bodyId");
        var side = Assert.Single(await Faces(session, cylinder), face => face.Kind == ConnectorKind.Cylinder).Reference;
        await Applied(session, new AddJointCommand
        {
            Kind = JointRecord.Fixed, ParentBodyId = parent, ParentFeatureId = parent, ParentElement = "+z",
            ChildBodyId = cylinder, ChildFeatureId = side.FeatureId, ChildElement = side.Element, ChildItemId = side.ItemId,
        });

        var placed = Assert.Single(await Faces(session, cylinder), face => face.Kind == ConnectorKind.Cylinder);
        Near((0, 0, 5), placed.Point);
        Near((0, 0, -1), placed.Direction is { } axis ? new DatumPoint(axis.X, axis.Y, axis.Z) : null);
    }

    // T206: an open, an Undo and a Redo give the same solids as the live session, not only the same records: the faces of each
    // body of a tree of two joints are the same.
    [Fact]
    public async Task An_Open_An_Undo_And_A_Redo_Give_The_Same_Faces()
    {
        var path = Path.Combine(_folder, "faces" + DocumentFile.Extension);
        var live = NewSession();
        DocumentFile.Create(live, path);
        var (parent, child) = (await Box(live, 40, 30, 10), await Box(live, 20, 10, 6));
        var third = await Box(live, 8, 8, 8);
        var joint = Output(await Applied(live, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with { Cos = 0.6, Sin = 0.8 }), "jointId");
        await Applied(live, Joint(JointRecord.Slider, child, "+y", third, "-z") with { Distance = 1.5 });
        await Applied(live, Move(parent, 12.5));

        async Task<List<FaceConnector>> All(DocumentSession session)
            => [.. (await Faces(session, parent)).Concat(await Faces(session, child)).Concat(await Faces(session, third))];

        var before = await All(live);
        await Applied(live, new SetJointValueCommand { JointId = joint, Cos = -0.28, Sin = 0.96 });
        var after = await All(live);
        await Applied(live, new UndoCommand());
        Assert.Equal(before, await All(live));
        await Applied(live, new RedoCommand());
        Assert.Equal(after, await All(live));
        live.Close();

        var opened = NewSession();
        await DocumentFile.Open(opened, path);
        Assert.Equal(after, await All(opened));
        opened.Close();
    }
}
