using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Kernel;

namespace Engine.Core;

// ADR-0191 (phase P11.0, TASK-0474): the arithmetic of the placement of a body. Each function uses + - * / only, in a fixed
// order, so each platform gives the same bits (CLAUDE.md, determinism rules 1 to 4). A point p of the shape of a body is at
// R·p + t in the Document, where R is the turn of the quaternion and t the offset (item 1).
public static class Placements
{
    // The placement of the record: the identity when the record holds none.
    public static BodyPlacement Of(BodyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.Placement ?? BodyPlacement.Identity;
    }

    // The value that a record stores: null for the identity, so a body that goes back to the identity has the record of a
    // body that never moved (item 3).
    public static BodyPlacement? Stored(BodyPlacement placement) => placement.IsIdentity ? null : placement;

    // The turn of the placement, or null for a quaternion that gives no turn, as for the command Rotate (ADR-0191 item 1).
    public static Rotation? Turn(BodyPlacement placement)
        => Rotation.FromQuaternion(placement.Qw, placement.Qx, placement.Qy, placement.Qz);

    // True when the placement moves no point: a quaternion with no vector part and the offset zero.
    public static bool Moves(BodyPlacement placement)
        => !(placement.Qx == 0 && placement.Qy == 0 && placement.Qz == 0 && placement.Dx == 0 && placement.Dy == 0 && placement.Dz == 0);

    // True when the quaternion has a vector part, so the placement turns the shape.
    private static bool Turns(BodyPlacement placement) => !(placement.Qx == 0 && placement.Qy == 0 && placement.Qz == 0);

    // Item 6: the placement of the second body in the frame of the first, so that first · relative is the second. The turn is
    // the product of the conjugate of the first quaternion and the second; the offset is the turn of that conjugate applied to
    // the difference of the two offsets. When the first placement is the identity, the result is the second, bit for bit.
    public static BodyPlacement Relative(BodyPlacement first, BodyPlacement second)
    {
        var (w1, x1, y1, z1) = (first.Qw, -first.Qx, -first.Qy, -first.Qz);
        var (w2, x2, y2, z2) = (second.Qw, second.Qx, second.Qy, second.Qz);
        var w = (w1 * w2) - (x1 * x2) - (y1 * y2) - (z1 * z2);
        var x = (w1 * x2) + (x1 * w2) + (y1 * z2) - (z1 * y2);
        var y = (w1 * y2) - (x1 * z2) + (y1 * w2) + (z1 * x2);
        var z = (w1 * z2) + (x1 * y2) - (y1 * x2) + (z1 * w2);
        var (dx, dy, dz) = (second.Dx - first.Dx, second.Dy - first.Dy, second.Dz - first.Dz);
        if (Turns(first) && Rotation.FromQuaternion(first.Qw, -first.Qx, -first.Qy, -first.Qz) is { } back)
            (dx, dy, dz) = back.Apply(dx, dy, dz);
        return new BodyPlacement(w, x, y, z, dx, dy, dz);
    }

    // The placement that undoes the placement: the conjugate quaternion, and the turn of the conjugate applied to the negative
    // offset.
    public static BodyPlacement Inverse(BodyPlacement placement)
    {
        var (dx, dy, dz) = (-placement.Dx, -placement.Dy, -placement.Dz);
        if (Turns(placement) && Rotation.FromQuaternion(placement.Qw, -placement.Qx, -placement.Qy, -placement.Qz) is { } back)
            (dx, dy, dz) = back.Apply(dx, dy, dz);
        return new BodyPlacement(placement.Qw, -placement.Qx, -placement.Qy, -placement.Qz, dx, dy, dz);
    }

    // The placement outer after the placement inner: the product of the two quaternions, and the offset of inner with the
    // placement outer.
    public static BodyPlacement Compose(BodyPlacement outer, BodyPlacement inner)
    {
        var (w1, x1, y1, z1) = (outer.Qw, outer.Qx, outer.Qy, outer.Qz);
        var (w2, x2, y2, z2) = (inner.Qw, inner.Qx, inner.Qy, inner.Qz);
        var (dx, dy, dz) = Apply(outer, inner.Dx, inner.Dy, inner.Dz);
        return new BodyPlacement(
            (w1 * w2) - (x1 * x2) - (y1 * y2) - (z1 * z2),
            (w1 * x2) + (x1 * w2) + (y1 * z2) - (z1 * y2),
            (w1 * y2) - (x1 * z2) + (y1 * w2) + (z1 * x2),
            (w1 * z2) + (x1 * y2) - (y1 * x2) + (z1 * w2),
            dx, dy, dz);
    }

    // The point (x, y, z) of the shape in the Document, in doubles.
    public static (double X, double Y, double Z) Apply(BodyPlacement placement, double x, double y, double z)
    {
        if (Turns(placement) && Turn(placement) is { } turn)
            (x, y, z) = turn.Apply(x, y, z);
        return (x + placement.Dx, y + placement.Dy, z + placement.Dz);
    }

    // The direction (x, y, z) of the shape in the Document: the turn only.
    public static (double X, double Y, double Z) Direction(BodyPlacement placement, double x, double y, double z)
        => Turns(placement) && Turn(placement) is { } turn ? turn.Apply(x, y, z) : (x, y, z);

    // Item 7: the triangles of the shape in the Document. A tessellation is presentation (ADR-0007), so the doubles of the
    // backend take the placement in doubles.
    public static Tessellation Apply(BodyPlacement placement, Tessellation mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (!Moves(placement))
            return mesh;
        var positions = new double[mesh.Positions.Length];
        for (var i = 0; i + 2 < positions.Length; i += 3)
            (positions[i], positions[i + 1], positions[i + 2]) = Apply(placement, mesh.Positions[i], mesh.Positions[i + 1], mesh.Positions[i + 2]);
        return new Tessellation(positions, mesh.Indices);
    }

    // Item 7: the triangles of the body in the Document, from the triangles of its shape under its geometry key.
    public static Tessellation Tessellate(ITessellationOps ops, BodyRecord record)
    {
        ArgumentNullException.ThrowIfNull(ops);
        ArgumentNullException.ThrowIfNull(record);
        return Apply(Of(record), ops.Tessellate(new BodyHandle(record.GeometryKey)));
    }

    // Item 7: the box of the body in the Document, along the axes of the Document. A placement that turns the shape needs the
    // points of the shape, so the box comes from the triangles when the backend gives them, and else from the eight corners of
    // the box of the shape, which hold the shape.
    public static Aabb Box(IGeometryBackend backend, IGeometryQuery query, BodyRecord record)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(record);
        var placement = Of(record);
        if (!Moves(placement))
            return query.GetBoundingBox(new BodyHandle(record.GeometryKey));
        double[] points;
        if (backend.TryGet<ITessellationOps>() is { } ops)
        {
            points = Apply(placement, ops.Tessellate(new BodyHandle(record.GeometryKey))).Positions;
        }
        else
        {
            var b = query.GetBoundingBox(new BodyHandle(record.GeometryKey));
            points = new double[24];
            for (var i = 0; i < 8; i++)
            {
                (points[3 * i], points[(3 * i) + 1], points[(3 * i) + 2]) = Apply(
                    placement, (i & 1) == 0 ? b.MinX : b.MaxX, (i & 2) == 0 ? b.MinY : b.MaxY, (i & 4) == 0 ? b.MinZ : b.MaxZ);
            }
        }

        var (minX, minY, minZ, maxX, maxY, maxZ) = (double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
        for (var i = 0; i + 2 < points.Length; i += 3)
        {
            (minX, maxX) = (Math.Min(minX, points[i]), Math.Max(maxX, points[i]));
            (minY, maxY) = (Math.Min(minY, points[i + 1]), Math.Max(maxY, points[i + 1]));
            (minZ, maxZ) = (Math.Min(minZ, points[i + 2]), Math.Max(maxZ, points[i + 2]));
        }

        return new Aabb(minX, minY, minZ, maxX, maxY, maxZ);
    }

    // Item 5: the exact solid of the shape in the Document. The kernel reads the nine numbers of the turn and the offset
    // exactly, so the numbers grow by one product for each read, and not by one product for each move.
    public static KernelSolid Apply(BodyPlacement placement, KernelSolid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        if (Turns(placement) && Turn(placement) is { } r)
            solid = solid.Rotate(Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
        return placement.Dx == 0 && placement.Dy == 0 && placement.Dz == 0
            ? solid
            : solid.Translate(Vector3R.FromDoubles(placement.Dx, placement.Dy, placement.Dz));
    }

    // The exact map of a point and of a direction, as Apply maps a solid.
    public static Vector3R Point(BodyPlacement placement, Vector3R point) => Direction(placement, point) + Offset(placement);

    public static Vector3R Direction(BodyPlacement placement, Vector3R direction)
    {
        if (!Turns(placement) || Turn(placement) is not { } r)
            return direction;
        var (x, y, z) = (Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
        return (x * direction.X) + (y * direction.Y) + (z * direction.Z);
    }

    public static Vector3R Offset(BodyPlacement placement) => Vector3R.FromDoubles(placement.Dx, placement.Dy, placement.Dz);

    // Item 6: the solid of the backend under source with the placement, stored under the key into; source when the placement
    // moves no point. A turn and an offset take two steps of the backend; the turn goes under a key of its own.
    public static Guid Placed(ITransformOps ops, Guid source, BodyPlacement placement, Guid into)
    {
        ArgumentNullException.ThrowIfNull(ops);
        if (!Moves(placement))
            return source;
        var turned = source;
        if (Turns(placement))
        {
            var turn = Turn(placement) ?? throw new ArgumentException("The placement has a quaternion that gives no turn.", nameof(placement));
            turned = placement.Dx == 0 && placement.Dy == 0 && placement.Dz == 0 ? into : GeometryKeys.Derive(into, TurnStep);
            ops.Rotate(new BodyHandle(turned), new BodyHandle(source), turn);
        }

        if (turned != into)
            ops.Translate(new BodyHandle(into), new BodyHandle(turned), placement.Dx, placement.Dy, placement.Dz);
        return into;
    }

    // Item 6: the key of the solid of the second operand of a boolean in the frame of the first, for the step whose result has
    // the key result: the key of the second body when its placement in that frame moves no point. The backend must then
    // implement ITransformOps; else the call throws.
    public static Guid SecondOperand(IGeometryBackend backend, Guid result, BodyRecord first, BodyRecord second)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var relative = Relative(Of(first), Of(second));
        if (!Moves(relative))
            return second.GeometryKey;
        var ops = backend.TryGet<ITransformOps>() ?? throw new InvalidOperationException("The backend does not implement ITransformOps, so it cannot place the second body.");
        return Placed(ops, second.GeometryKey, relative, OperandKey(result, second.Handle.Id));
    }

    // Item 6: the key of the solid of the operand of a Translate or a Rotate in its place, for the step whose result has the
    // key result: the key of the operand when its placement moves no point.
    public static Guid Operand(ITransformOps ops, Guid result, BodyRecord operand)
    {
        ArgumentNullException.ThrowIfNull(operand);
        return Placed(ops, operand.GeometryKey, Of(operand), OperandKey(result, operand.Handle.Id));
    }

    // The key of the solid of an operand in its place, for the step whose result has the key result: a name-based key of the
    // result and the operand (ADR-0037 item 3), so a replay gives the same key.
    public static Guid OperandKey(Guid result, Guid operand) => GeometryKeys.Derive(result, GeometryKeys.Derive(operand, OperandStep));

    // Two fixed random guids that name the steps of a placement in the backend.
    private static readonly Guid TurnStep = new("3b1f6c2e-9a47-4d05-8e63-71c2a9d4f058");
    private static readonly Guid OperandStep = new("c84e0d19-5f2a-4b76-a3e1-0d9b6f7c2a41");
}
