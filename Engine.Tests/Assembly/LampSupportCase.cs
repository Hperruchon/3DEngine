using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Tests.Assembly;

// Row K10b (TASK-0466): the tilting support of a lamp, the second case of K10 (ADR-0126, register entry R-0070). On 2026-11-25
// the owner chose stand-in sizes for the crossbar, its two fixing points and the volumes of the lamp, the housing and the cable,
// until the owner measures the real ones. The bracket, the arm and the knob are first sizes of the session.
// docs/proposals/2026-11-25-the-lamp-support-case-of-k10.md gives the table, and LampSupportCaseTests checks each body against it.
//
// The axes: z is up, and the lower face of the crossbar is at z = 0. The crossbar runs along y. The pivot of the arm is on an
// axis along y through the point (15, y, -20), so the lamp tilts down in the plane x-z; at 0 degrees the arm points along +x.
//
// Each command exists (the list of Engine.Core/Commands). CreateBox and CreateCylinder centre a body on the origin, and a
// cylinder has its axis along +z, so a part is made at the origin, turned, and then moved. A turn of 90 degrees is a Rotate with
// the quaternion (1, 1, 0, 0) about x or (1, 0, 1, 0) about y: Rotation.FromQuaternion divides by the sum of the squares, 2, so
// each entry of the turn is 1, 0 or -1, exactly. A hole is a Subtract of a cylinder. Each gap of the commands is named where the
// builder models around it, with the row that removes it.
internal static class LampSupportCase
{
    internal const string Crossbar = "Crossbar";
    internal const string Bracket = "Bracket";
    internal const string Arm = "Arm";
    internal const string Knob = "Knob";
    internal const string Lamp = "Lamp";
    internal const string Housing = "Housing";
    internal const string Cable = "Cable";

    // The parts of the mechanism, which the envelope of 60 mm under the crossbar holds (K13 checks it over the travel).
    internal static readonly IReadOnlyList<string> Mount = [Bracket, Arm, Knob];

    // The pairs whose contact is intended: the bracket on the crossbar, the arm on the lug of the bracket, the collar of the
    // knob on the arm, the lamp on the end of the arm, the housing on the crossbar, and the cable at the housing.
    internal static readonly IReadOnlyList<(string A, string B)> IntendedContacts =
    [
        (Bracket, Crossbar), (Arm, Bracket), (Knob, Arm), (Lamp, Arm), (Housing, Crossbar), (Cable, Housing),
    ];

    private enum Axis
    {
        X,
        Y,
        Z,
    }

    // Sends the commands of the seven bodies and gives the handle of each final body by its name.
    internal static async Task<IReadOnlyDictionary<string, Guid>> Build(DocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var bodies = new Dictionary<string, Guid>(StringComparer.Ordinal);

        // The crossbar (stand-in): a square tube of 30 by 30 mm with a wall of 2 mm, 600 mm long, and the two fixing points, holes
        // of 5.5 mm for M5 screws, 40 mm apart, through the tube. The void is 2 mm longer than the tube, so no end face is shared.
        var tube = await Subtract(session, await Box(session, 30, 600, 30, 0, 0, 15), await Box(session, 26, 602, 26, 0, 0, 15));
        foreach (var y in new[] { -20.0, 20.0 })
            tube = await Subtract(session, tube, await Cylinder(session, 2.75, 40, Axis.Z, 0, y, 15));
        bodies.Add(Crossbar, await Named(session, tube, Crossbar));

        // The bracket: a plate of 30 by 60 by 5 mm under the crossbar, with the holes of the two fixing points, and a lug of 20 by
        // 6 mm down to z = -30 with the hole of the pivot, 6.5 mm for a shaft of 6 mm. The lug enters the plate by 2.5 mm, so the
        // union shares a volume and not only a face. Gap: a hole command (row K23); a hole is a Subtract of a cylinder here.
        var bracket = await Union(session, await Box(session, 30, 60, 5, 0, 0, -2.5), await Box(session, 20, 6, 27.5, 15, -12, -16.25));
        foreach (var y in new[] { -20.0, 20.0 })
            bracket = await Subtract(session, bracket, await Cylinder(session, 2.75, 10, Axis.Z, 0, y, -2.5));
        bracket = await Subtract(session, bracket, await Cylinder(session, 3.25, 10, Axis.Y, 15, -12, -20));
        bodies.Add(Bracket, await Named(session, bracket, Bracket));

        // The arm: 80 by 6 by 20 mm beside the lug, from 10 mm behind the pivot to the lamp, with the hole of the pivot. Gap: a
        // turn about the axis of the pivot is a Translate, a Rotate and a Translate (ADR-0112 item 9); P11 gives the joint.
        var arm = await Subtract(session, await Box(session, 80, 6, 20, 45, -6, -20), await Cylinder(session, 3.25, 10, Axis.Y, 15, -6, -20));
        bodies.Add(Arm, await Named(session, arm, Arm));

        // The knob that locks the pivot: a shaft of 6 mm through the arm and the lug, a collar of 12 mm that presses on the arm,
        // and a head of 24 mm, as one body by two Union (K16). Each piece enters the next by 1 mm. Gap: a revolve (row K7) would
        // make the knob from one profile; the thread of the shaft waits for the screw of P19.
        var knob = await Union(session, await Cylinder(session, 3, 13, Axis.Y, 15, -8.5, -20), await Cylinder(session, 6, 5, Axis.Y, 15, -0.5, -20));
        knob = await Union(session, knob, await Cylinder(session, 12, 8, Axis.Y, 15, 5, -20));
        bodies.Add(Knob, await Named(session, knob, Knob));

        // The lamp head (stand-in): a cylinder of 70 mm, 100 mm long, on the axis along x at the end of the arm. Gap: its role,
        // a context volume and not a part to print, waits for P14.
        bodies.Add(Lamp, await Named(session, await Cylinder(session, 35, 100, Axis.X, 135, -6, -20), Lamp));

        // The housing of the driver (stand-in): 40 by 80 by 25 mm on the crossbar. The envelope excludes it (P14 and K13).
        bodies.Add(Housing, await Named(session, await Box(session, 40, 80, 25, 0, 0, 42.5), Housing));

        // The cable (stand-in): 5 mm, a straight piece of 90 mm from the housing to the front, above the crossbar. Its real path
        // is not known; the envelope excludes it (P14 and K13).
        bodies.Add(Cable, await Named(session, await Cylinder(session, 2.5, 90, Axis.X, 65, -6, 32.5), Cable));

        return bodies;
    }

    private static async Task<Guid> Box(DocumentSession session, double sizeX, double sizeY, double sizeZ, double x, double y, double z)
    {
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = sizeX, SizeY = sizeY, SizeZ = sizeZ }));
        return Output(await Applied(session, new TranslateCommand { BodyId = box, Dx = x, Dy = y, Dz = z }));
    }

    // A cylinder on an axis along x, y or z with its centre at (x, y, z). The turn comes before the move, so it is about the
    // centre of the cylinder.
    private static async Task<Guid> Cylinder(DocumentSession session, double radius, double height, Axis axis, double x, double y, double z)
    {
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = radius, Height = height }));
        if (axis == Axis.Y)
            cylinder = Output(await Applied(session, new RotateCommand { BodyId = cylinder, Qw = 1, Qx = 1, Qy = 0, Qz = 0 }));
        else if (axis == Axis.X)
            cylinder = Output(await Applied(session, new RotateCommand { BodyId = cylinder, Qw = 1, Qx = 0, Qy = 1, Qz = 0 }));
        return Output(await Applied(session, new TranslateCommand { BodyId = cylinder, Dx = x, Dy = y, Dz = z }));
    }

    private static async Task<Guid> Subtract(DocumentSession session, Guid minuend, Guid subtrahend)
        => Output(await Applied(session, new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend }));

    private static async Task<Guid> Union(DocumentSession session, Guid first, Guid second)
        => Output(await Applied(session, new UnionCommand { FirstBodyId = first, SecondBodyId = second }));

    private static async Task<Guid> Named(DocumentSession session, Guid body, string name)
    {
        await Applied(session, new RenameBodyCommand { BodyId = body, NewName = name });
        return body;
    }

    private static Task<CommandResult> Applied(DocumentSession session, Command command) => BookcaseCase.Applied(session, command);

    private static Guid Output(CommandResult result) => BookcaseCase.Output(result);
}
