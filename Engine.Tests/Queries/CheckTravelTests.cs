using Engine.Contracts;
using Engine.Contracts.Joints;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Queries;

// ADR-0195 (phase K12a, TASK-0490): the travel of a pivot. ADR-0195 names this file in enforced-by. The scene: a parent box of
// 40 x 30 x 10 and a child box of 20 x 10 x 6, each with its centre at the origin, and a pivot from the face "+x" of the parent to
// the face "-z" of the child, from (1, 0) to (0, 1). The child turns about the axis x through (20, 0, 0); at the turn (c, s) its
// highest corner is at z = 10 s + 5 c, so a plate whose lower face is at z = 8 meets it where 10 s + 5 c = 8. No angle is needed.
public sealed class CheckTravelTests
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

    // A box with its centre at (x, y, z).
    private static async Task<Guid> Box(DocumentSession session, double sx, double sy, double sz, double x = 0, double y = 0, double z = 0)
    {
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = sx, SizeY = sy, SizeZ = sz }), "bodyId");
        if (x != 0 || y != 0 || z != 0)
            await Applied(session, new PlaceBodyCommand { BodyId = box, Qw = 1, Qx = 0, Qy = 0, Qz = 0, Dx = x, Dy = y, Dz = z });
        return box;
    }

    private static AddJointCommand Joint(string kind, Guid parent, string parentFace, Guid child, string childFace) => new()
    {
        Kind = kind, ParentBodyId = parent, ParentFeatureId = parent, ParentElement = parentFace,
        ChildBodyId = child, ChildFeatureId = child, ChildElement = childFace,
    };

    private static async Task<(DocumentSession Session, Guid Parent, Guid Child, Guid Joint)> Scene()
    {
        var session = NewSession();
        var parent = await Box(session, 40, 30, 10);
        var child = await Box(session, 20, 10, 6);
        var joint = Output(await Applied(session, Joint(JointRecord.Pivot, parent, "+x", child, "-z") with
        {
            Cos = 1, Sin = 0, LowerCos = 1, LowerSin = 0, UpperCos = 0, UpperSin = 1,
        }), "jointId");
        return (session, parent, child, joint);
    }

    private static async Task<TravelInfo> Travel(DocumentSession session, Guid joint, double? tolerance = null)
    {
        var result = await session.Query<TravelInfo>(new CheckTravelQuery { JointId = joint, Tolerance = tolerance });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static async Task<string> Refused(DocumentSession session, Guid joint, double? tolerance = null)
    {
        var result = await session.Query<TravelInfo>(new CheckTravelQuery { JointId = joint, Tolerance = tolerance });
        Assert.NotNull(result.Error);
        return result.Error!.Code;
    }

    // Items 3 and 4: the travel stops at the first sample whose gap is at most the tolerance, and that sample is on the side of
    // the plate before the contact: 10 s + 5 c is at most 8, and within the tolerance of 8. The radius is the distance of the far
    // corner from the axis, the square root of 125.
    [Fact]
    public async Task A_Travel_Stops_At_The_First_Contact_Within_The_Tolerance()
    {
        var (session, _, child, joint) = await Scene();
        var plate = await Box(session, 6, 60, 4, 23, 0, 10);

        var travel = await Travel(session, joint);

        Assert.False(travel.Free);
        Assert.Equal(child, travel.MovingBodyId);
        Assert.Equal(plate, travel.FixedBodyId);
        var (c, s) = (travel.Contact!.Cos, travel.Contact.Sin);
        Assert.InRange((c * c) + (s * s), 1 - 1e-12, 1 + 1e-12);
        var top = (10 * s) + (5 * c);
        Assert.InRange(top, 8 - CheckTravelQueryHandler.DefaultTolerance - 1e-9, 8 + 1e-9);
        Assert.InRange(travel.Gap!.Value, 8 - top - 1e-9, 8 - top + 1e-9);
        Assert.InRange(travel.Radius, Math.Sqrt(125) - 1e-9, Math.Sqrt(125) + 1e-9);
        Assert.Equal(CheckTravelQueryHandler.DefaultTolerance, travel.Tolerance);
        Assert.InRange(travel.Samples, 2, 200);

        // A smaller tolerance gives a contact nearer to the plate.
        var fine = await Travel(session, joint, 1e-4);
        Assert.InRange((10 * fine.Contact!.Sin) + (5 * fine.Contact.Cos), 8 - 1e-4 - 1e-9, 8 + 1e-9);
    }

    // Item 2: the child touches its parent at each turn, and the pair of a joint is exempt, so a travel with no
    // other body near is free, from the lower to the upper limit.
    [Fact]
    public async Task The_Pair_Of_The_Joint_Is_Exempt_And_A_Travel_With_No_Obstacle_Is_Free()
    {
        var (session, _, _, joint) = await Scene();
        await Box(session, 6, 60, 4, 23, 0, 100);

        var travel = await Travel(session, joint);

        Assert.True(travel.Free);
        Assert.Null(travel.Contact);
        Assert.InRange(travel.Samples, 2, 50);
        Assert.Equal(new JointValue(1, 0, 0), travel.Lower);
        Assert.Equal(new JointValue(0, 1, 0), travel.Upper);
    }

    // Item 3: a body that overlaps a turning body at the lower limit is a contact at the lower limit, with the gap 0.
    [Fact]
    public async Task An_Overlap_At_The_Lower_Limit_Is_A_Contact_There()
    {
        var (session, _, _, joint) = await Scene();
        await Box(session, 2, 2, 2, 23, 0, 0);

        var travel = await Travel(session, joint);

        Assert.False(travel.Free);
        Assert.Equal(new JointValue(1, 0, 0), travel.Contact);
        Assert.Equal(0, travel.Gap);
        Assert.Equal(1, travel.Samples);
    }

    // Item 2: each body under the child through a tight joint turns too. A box on the face "+x" of the child reaches a radius of
    // about 14.1, and the bar at z = 12.5 is beyond the radius of the child, about 11.2: the travel meets the bar with the box only.
    [Fact]
    public async Task A_Body_Under_The_Child_Turns_With_It()
    {
        var (session, _, child, joint) = await Scene();
        await Box(session, 6, 2, 0.5, 23, 0, 12.75);
        Assert.True((await Travel(session, joint)).Free);

        var end = await Box(session, 4, 4, 4);
        await Applied(session, Joint(JointRecord.Fixed, child, "+x", end, "-z"));
        var travel = await Travel(session, joint);

        Assert.False(travel.Free);
        Assert.Equal(end, travel.MovingBodyId);
    }

    // Items 1, 3 and 5: a travel needs a pivot with limits and a finite tolerance above zero.
    [Fact]
    public async Task A_Travel_Needs_A_Pivot_With_Limits_And_A_Tolerance_Above_Zero()
    {
        var (session, parent, _, joint) = await Scene();
        var other = await Box(session, 8, 8, 8);
        var fixedJoint = Output(await Applied(session, Joint(JointRecord.Fixed, parent, "+y", other, "-z")), "jointId");
        var third = await Box(session, 8, 8, 8);
        var free = Output(await Applied(session, Joint(JointRecord.Pivot, parent, "-y", third, "-z") with { Cos = 1, Sin = 0 }), "jointId");

        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, await Refused(session, fixedJoint));
        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, await Refused(session, free));
        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, await Refused(session, joint, 0));
        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, await Refused(session, joint, -1));
        Assert.Equal(DiagnosticCodes.CheckTravelInvalid, await Refused(session, joint, double.NaN));
        Assert.Equal(DiagnosticCodes.JointNotFound, await Refused(session, Guid.NewGuid()));
    }

    // Item 1: the query changes nothing: the log and the bodies are the same after a travel.
    [Fact]
    public async Task A_Travel_Changes_Nothing()
    {
        var (session, _, _, joint) = await Scene();
        await Box(session, 6, 60, 4, 23, 0, 10);
        var before = await session.Read((document, _) => (document.Version, document.Bodies.OrderBy(body => body.Handle.Id).ToList()));

        await Travel(session, joint);

        var after = await session.Read((document, _) => (document.Version, document.Bodies.OrderBy(body => body.Handle.Id).ToList()));
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Item2, after.Item2);
    }
}
