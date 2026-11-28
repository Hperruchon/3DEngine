using Engine.Contracts;
using Engine.Contracts.Joints;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Queries;

// ADR-0196 (phase K12b, TASK-0491): the exact sweep of a slider in CheckTravel. ADR-0196 names this file in enforced-by. The
// scene: a parent box of 40 x 30 x 10 and a child box of 20 x 10 x 6, each with its centre at the origin, and a slider from the
// face "+x" of the parent to the face "-z" of the child. At the distance d the child is from x = 20 + d to x = 26 + d, from
// y = -10 to 10 and from z = -5 to 5, so a body whose face is at x = 35 meets it at d = 9.
public sealed class CheckSlideTests
{
    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command with { CommandId = Guid.NewGuid() });
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string key)
        => result.Outputs.TryGet<Guid>(key, out var id) ? id : throw new InvalidOperationException($"No output {key}.");

    // A box with its centre at (x, y, z), turned by the quaternion (qw, 0, 0, qz) about z.
    private static async Task<Guid> Box(DocumentSession session, double sx, double sy, double sz, double x, double y, double z, double qw = 1, double qz = 0)
    {
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = sx, SizeY = sy, SizeZ = sz }), "bodyId");
        await Applied(session, new PlaceBodyCommand { BodyId = box, Qw = qw, Qx = 0, Qy = 0, Qz = qz, Dx = x, Dy = y, Dz = z });
        return box;
    }

    private static async Task<(DocumentSession Session, Guid Child, Guid Joint)> Scene(double lower = 0, double upper = 20)
    {
        var session = NewSession();
        var parent = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");
        var child = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 10, SizeZ = 6 }), "bodyId");
        var joint = Output(await Applied(session, new AddJointCommand
        {
            Kind = JointRecord.Slider, ParentBodyId = parent, ParentFeatureId = parent, ParentElement = "+x",
            ChildBodyId = child, ChildFeatureId = child, ChildElement = "-z", Distance = lower, LowerDistance = lower, UpperDistance = upper,
        }), "jointId");
        return (session, child, joint);
    }

    private static async Task<TravelInfo> Travel(DocumentSession session, Guid joint)
    {
        var result = await session.Query<TravelInfo>(new CheckTravelQuery { JointId = joint });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    // Item 2: a plate smaller than the face of the child: its vertices meet the face of the child at d = 9, exactly.
    [Fact]
    public async Task The_Vertices_Of_A_Small_Plate_Meet_The_Face_Of_The_Child()
    {
        var (session, child, joint) = await Scene();
        var plate = await Box(session, 5, 10, 6, 37.5, 0, 0);

        var travel = await Travel(session, joint);

        Assert.False(travel.Free);
        Assert.Equal(new JointValue(1, 0, 9), travel.Contact);
        Assert.Equal(child, travel.MovingBodyId);
        Assert.Equal(plate, travel.FixedBodyId);
        Assert.Equal(0, travel.Gap);
        Assert.Equal(1, travel.Samples);
    }

    // Item 2: a wall larger than the face of the child: the vertices of the child meet the face of the wall at d = 9, also from a
    // lower limit of 2.
    [Fact]
    public async Task The_Vertices_Of_The_Child_Meet_The_Face_Of_A_Wall()
    {
        var (session, _, joint) = await Scene();
        await Box(session, 5, 60, 20, 37.5, 0, 0);
        Assert.Equal(9, (await Travel(session, joint)).Contact!.Distance);

        var (later, _, from2) = await Scene(lower: 2);
        await Box(later, 5, 60, 20, 37.5, 0, 0);
        Assert.Equal(9, (await Travel(later, from2)).Contact!.Distance);
    }

    // Item 2: a tall square bar turned by 45 degrees about z, with its centre at x = 40: its leading edge is at x = 40 - 2√2, and
    // it crosses the face of the child, so the first touch is an edge of the bar on the upper and the lower edges of the face.
    [Fact]
    public async Task An_Edge_Of_A_Turned_Bar_Meets_The_Edges_Of_The_Child()
    {
        var (session, _, joint) = await Scene();
        var half = Math.Sqrt(2) / 2;
        await Box(session, 4, 4, 40, 40, 0, 0, qw: 1 + half, qz: half);

        var travel = await Travel(session, joint);

        Assert.InRange(travel.Contact!.Distance, 14 - (2 * Math.Sqrt(2)) - 1e-9, 14 - (2 * Math.Sqrt(2)) + 1e-9);
    }

    // Items 2 and 3: a body beside the path, and a body beyond the upper limit, give a free travel; a body that overlaps the child
    // at the lower limit is a contact there.
    [Fact]
    public async Task A_Body_Off_The_Path_Is_Free_And_An_Overlap_Is_A_Contact_At_The_Lower_Limit()
    {
        var (session, _, joint) = await Scene();
        await Box(session, 5, 5, 5, 37.5, 20, 0);
        await Box(session, 5, 60, 20, 52.5, 0, 0);
        Assert.True((await Travel(session, joint)).Free);

        await Box(session, 2, 2, 2, 23, 0, 0);
        var travel = await Travel(session, joint);
        Assert.Equal(new JointValue(1, 0, 0), travel.Contact);
    }

    // Item 1: a slider needs limits.
    [Fact]
    public async Task A_Slider_With_No_Limits_Has_No_Travel()
    {
        var session = NewSession();
        var parent = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");
        var child = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 10, SizeZ = 6 }), "bodyId");
        var joint = Output(await Applied(session, new AddJointCommand
        {
            Kind = JointRecord.Slider, ParentBodyId = parent, ParentFeatureId = parent, ParentElement = "+x",
            ChildBodyId = child, ChildFeatureId = child, ChildElement = "-z", Distance = 0,
        }), "jointId");

        var result = await session.Query<TravelInfo>(new CheckTravelQuery { JointId = joint });

        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, result.Error!.Code);
    }
}
