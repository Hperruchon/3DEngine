using Engine.Contracts;
using Engine.Contracts.Joints;
using Engine.Contracts.Sketch;
using Engine.Kernel;

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

    // ADR-0194 item 2 (phase P11b, TASK-0489): the tight joint whose child is the body, or null. A tight joint gives the placement
    // of its child, so a PlaceBody and a PlaceByConnectors of that child are refused.
    public static JointRecord? TightParentOf(IEnumerable<JointRecord> joints, Guid body)
        => ParentOf(joints, body) is { State: JointRecord.Tight } joint ? joint : null;

    // Finding E254 of the second codebase review of 2026-11-28 (TASK-0488, the answer Q1 A): the tight joint that holds the body: a
    // tight parent joint whose parent is live. A joint whose parent a command consumed places nothing, so it does not hold its
    // child, and a PlaceBody of the child applies; the joint still counts for the one parent and for a loop.
    public static JointRecord? HolderOf(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TightParentOf(document.Joints, body) is { } joint && document.Bodies.Any(live => live.Handle.Id == joint.ParentBodyId) ? joint : null;
    }

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
        // Finding E253 of the second codebase review of 2026-11-28 (TASK-0488): the turn back by the lower limit and the order of
        // the two turns are exact. Until TASK-0488 they were doubles, so a value on a limit with another length, as (3, 4) for the
        // limit (0.6, 0.8), rounded to a small negative sine and to a whole turn, and a large pair could reach infinity.
        var (lc, ls) = (Rational.FromDouble(limits.Lower.Cos), Rational.FromDouble(limits.Lower.Sin));
        (Rational C, Rational S) Back(JointValue turn)
        {
            var (c, s) = (Rational.FromDouble(turn.Cos), Rational.FromDouble(turn.Sin));
            return ((c * lc) + (s * ls), (s * lc) - (c * ls));
        }

        return NotAfter(Back(value), Back(limits.Upper));
    }

    // Item 5: true when the angle of a counterclockwise from the axis x, in [0°, 360°), is not above the angle of b. Each direction
    // is in the half [0°, 180°) or [180°, 360°); in one half, the sign of the cross product a × b orders the two.
    private static bool NotAfter((Rational C, Rational S) a, (Rational C, Rational S) b)
    {
        static int Half((Rational C, Rational S) p) => p.S.Sign > 0 || (p.S.Sign == 0 && p.C.Sign > 0) ? 0 : 1;
        var (ha, hb) = (Half(a), Half(b));
        return ha != hb ? ha < hb : ((a.C * b.S) - (a.S * b.C)).Sign >= 0;
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
            // ADR-0194 item 2 (phase P11b, TASK-0489): a loose joint gives no placement, so its child and the bodies under it stay.
            foreach (var joint in joints.Where(joint => joint.ParentBodyId == parent && joint.State == JointRecord.Tight).OrderBy(joint => order.GetValueOrDefault(joint.Id, int.MaxValue)))
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
