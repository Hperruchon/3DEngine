using Engine.Contracts;
using Engine.Contracts.Joints;
using Engine.Contracts.Sketch;

namespace Engine.Core.Commands;

// ADR-0193 items 3 to 7 (phase P11a, TASK-0479): the kinematic tree of the joints of a part. Each function uses + - * / and
// sqrt only, in a fixed order, so each platform gives the same bits (CLAUDE.md, determinism rules 1 to 4). The engine has no
// solver: the place of each child comes from the place of its parent and its joint.
internal static class JointTree
{
    // Item 3: the turn of a half turn about the axis U of a connector, so that the normal of the child connector is opposite to
    // the normal of the parent connector, as two faces that meet (the answer Q2 A).
    private static readonly BodyPlacement Flip = new(0, 1, 0, 0, 0, 0, 0);

    // Item 6: the joint whose child is the body, or null. A body has at most one parent joint.
    public static JointRecord? ParentOf(IEnumerable<JointRecord> joints, Guid body)
        => joints.FirstOrDefault(joint => joint.ChildBodyId == body);

    // Item 6: true when the body ancestor is on the chain of parents of the body, or is the body. The walk stops at a body that
    // it has seen, so a tree with a fault of the engine does not stop the session.
    public static bool IsAncestor(IReadOnlyCollection<JointRecord> joints, Guid ancestor, Guid body)
    {
        var seen = new HashSet<Guid>();
        for (Guid? at = body; at is { } current && seen.Add(current); at = ParentOf(joints, current)?.ParentBodyId)
        {
            if (current == ancestor)
                return true;
        }

        return false;
    }

    // Item 3: the placement of the child of the joint when its parent has the placement parent:
    // parent · frame(parent connector) · joint(value) · flip · frame(child connector)⁻¹.
    public static BodyPlacement Child(BodyPlacement parent, JointRecord joint)
    {
        ArgumentNullException.ThrowIfNull(joint);
        var inner = Placements.Compose(Motion(joint.Kind, joint.Value), Placements.Compose(Flip, Placements.Inverse(Placement(joint.Child.Frame))));
        return Placements.Compose(parent, Placements.Compose(Placement(joint.Parent.Frame), inner));
    }

    // Item 3: the motion of the joint in the frame of the parent connector. A fixed joint moves nothing, a pivot turns about the
    // normal, and a slider moves along the normal.
    public static BodyPlacement Motion(string kind, JointValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return kind switch
        {
            JointRecord.Pivot => Turn(value.Cos, value.Sin),
            JointRecord.Slider => new BodyPlacement(1, 0, 0, 0, 0, 0, value.Distance),
            _ => BodyPlacement.Identity,
        };
    }

    // Item 4 (the answer Q3 A): the quaternion of the turn about z whose cosine and sine are in the ratio c : s. With r the
    // length of (c, s), (r + c, 0, 0, s) and (s, 0, 0, r - c) are the quaternion of the half angle times a number, and
    // Rotation.FromQuaternion divides by the sum of the squares. The second form serves for c < 0, so no sum cancels.
    public static BodyPlacement Turn(double c, double s)
    {
        var r = Math.Sqrt((c * c) + (s * s));
        return c >= 0 ? new BodyPlacement(r + c, 0, 0, s, 0, 0, 0) : new BodyPlacement(s, 0, 0, r - c, 0, 0, 0);
    }

    // Item 3: the placement that takes the axes x, y and z to U, V and the normal of the frame, and the origin to its origin.
    // The quaternion comes from the matrix of the three columns, by the method of Shepperd with no root: each form is the
    // quaternion times four times one of its parts, and the largest part decides the form.
    public static BodyPlacement Placement(DatumFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var (m00, m10, m20) = (frame.U.X, frame.U.Y, frame.U.Z);
        var (m01, m11, m21) = (frame.V.X, frame.V.Y, frame.V.Z);
        var (m02, m12, m22) = (frame.Normal.X, frame.Normal.Y, frame.Normal.Z);
        var trace = m00 + m11 + m22;
        var (w, x, y, z) = trace > 0
            ? (1 + trace, m21 - m12, m02 - m20, m10 - m01)
            : m00 > m11 && m00 > m22
                ? (m21 - m12, 1 + m00 - m11 - m22, m01 + m10, m02 + m20)
                : m11 > m22
                    ? (m02 - m20, m01 + m10, 1 + m11 - m00 - m22, m12 + m21)
                    : (m10 - m01, m02 + m20, m12 + m21, 1 + m22 - m00 - m11);
        return new BodyPlacement(w, x, y, z, frame.Origin.X, frame.Origin.Y, frame.Origin.Z);
    }

    // Item 2: the frame of a face connector from its origin and its unit normal n. U is the axis of the Document with the
    // smallest part of n (x first, then y, then z), less its part along n, divided by its length; V = n × U.
    public static DatumFrame FaceFrame(DatumPoint origin, DatumAxis n)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(n);
        var (ax, ay, az) = (Math.Abs(n.X), Math.Abs(n.Y), Math.Abs(n.Z));
        var (ex, ey, ez) = ax <= ay && ax <= az ? (1.0, 0.0, 0.0) : ay <= az ? (0.0, 1.0, 0.0) : (0.0, 0.0, 1.0);
        var along = (ex * n.X) + (ey * n.Y) + (ez * n.Z);
        var (ux, uy, uz) = (ex - (along * n.X), ey - (along * n.Y), ez - (along * n.Z));
        var length = Math.Sqrt((ux * ux) + (uy * uy) + (uz * uz));
        var u = new DatumAxis(ux / length, uy / length, uz / length);
        var v = new DatumAxis((n.Y * u.Z) - (n.Z * u.Y), (n.Z * u.X) - (n.X * u.Z), (n.X * u.Y) - (n.Y * u.X));
        return new DatumFrame(origin, u, v, n);
    }

    // Item 2: the frame of the Document in the frame of a body with the placement: the inverse placement applied to the origin
    // and to each axis.
    public static DatumFrame InBody(BodyPlacement placement, DatumFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var back = Placements.Inverse(placement);
        var (ox, oy, oz) = Placements.Apply(back, frame.Origin.X, frame.Origin.Y, frame.Origin.Z);
        DatumAxis Axis(DatumAxis a)
        {
            var (x, y, z) = Placements.Direction(back, a.X, a.Y, a.Z);
            return new DatumAxis(x, y, z);
        }

        return new DatumFrame(new DatumPoint(ox, oy, oz), Axis(frame.U), Axis(frame.V), Axis(frame.Normal));
    }

    // Item 5: true when the value is in the limits of the joint, or the joint has no limits. The turn of a pivot goes
    // counterclockwise from the lower limit to the upper limit; a slider takes each distance from the lower to the upper limit.
    public static bool InLimits(string kind, JointValue value, JointLimits? limits)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (limits is null)
            return true;
        if (kind == JointRecord.Slider)
            return value.Distance >= limits.Lower.Distance && value.Distance <= limits.Upper.Distance;
        if (kind != JointRecord.Pivot)
            return true;
        var (lc, ls) = (limits.Lower.Cos, limits.Lower.Sin);
        return Order((value.Cos * lc) + (value.Sin * ls), (value.Sin * lc) - (value.Cos * ls))
            <= Order((limits.Upper.Cos * lc) + (limits.Upper.Sin * ls), (limits.Upper.Sin * lc) - (limits.Upper.Cos * ls));
    }

    // Item 5: a number from 0 up to 4 that grows with the angle of the direction (c, s) counterclockwise from the axis x, with
    // + - / only: 0 at 0°, 1 at 90°, 2 at 180° and 3 at 270°. The length of (c, s) does not change it.
    private static double Order(double c, double s)
    {
        var p = c / (Math.Abs(c) + Math.Abs(s));
        return s >= 0 ? 1 - p : 3 + p;
    }

    // Item 6 (the answer Q4 A): the new records of the bodies under the body root when root has the placement at, with the
    // joints of the list. A joint whose child is not live gives no record. The walk takes the children of each body in the order
    // of the log of their joints, so the events of a step have one order on each platform.
    public static List<BodyRecord> Below(Document document, IReadOnlyCollection<JointRecord> joints, Guid root, BodyPlacement at)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(joints);
        var order = new Dictionary<Guid, int>();
        for (var i = 0; i < document.Log.Count; i++)
            order.TryAdd(document.Log[i].Command.CommandId, i);
        var live = document.Bodies.ToDictionary(body => body.Handle.Id);
        var changed = new List<BodyRecord>();
        var seen = new HashSet<Guid> { root };
        var queue = new Queue<(Guid Body, BodyPlacement Placement)>();
        queue.Enqueue((root, at));
        while (queue.Count > 0)
        {
            var (parent, placement) = queue.Dequeue();
            foreach (var joint in joints.Where(joint => joint.ParentBodyId == parent).OrderBy(joint => order.GetValueOrDefault(joint.Id, int.MaxValue)))
            {
                if (!live.TryGetValue(joint.ChildBodyId, out var child) || !seen.Add(child.Handle.Id))
                    continue;
                var next = Child(placement, joint);
                var record = child with { Placement = Placements.Stored(next) };
                if (record != child)
                    changed.Add(record);
                queue.Enqueue((child.Handle.Id, next));
            }
        }

        return changed;
    }
}
