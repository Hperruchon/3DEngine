using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Core.Gizmo;

// The place of each handle of the gizmo in the window, in window points (ADR-0038 items 3 and 4):
// the pixel of the pivot, the end of each arrow, and the four corners of each square.
public sealed record GizmoLayout(
    Vector2 Origin,
    IReadOnlyDictionary<GizmoHandle, Vector2> Ends,
    IReadOnlyDictionary<GizmoHandle, Vector2[]> Squares);

// A triangle list of one colour, in window points, for the overlay pipeline (ADR-0038 item 9).
public sealed record GizmoShape(GizmoHandle Handle, Vector2[] Triangles, Vector4 Colour);

// The translate gizmo of the desktop (ADR-0038). Each function is pure: a camera, the size of the
// window in points, and the pivot in. The client computes with the camera, which uses a tangent;
// the command records three numbers only (ADR-0038 item 10).
public static class TranslateGizmo
{
    // ADR-0038 item 3: the length of each axis on the screen.
    public const double AxisPoints = 100;

    // ADR-0038 item 4: a press within this distance of an arrow takes it.
    public const double ArrowTolerancePoints = 8;

    // ADR-0038 item 4: each square lies from this part of the axes to the next.
    public const double SquareFrom = 0.25;
    public const double SquareTo = 0.45;

    private const double ShaftPoints = 3;
    private const double HeadLengthPoints = 14;
    private const double HeadWidthPoints = 10;

    public static IReadOnlyList<GizmoHandle> Arrows { get; } = [GizmoHandle.X, GizmoHandle.Y, GizmoHandle.Z];

    public static IReadOnlyList<GizmoHandle> Planes { get; } = [GizmoHandle.XY, GizmoHandle.YZ, GizmoHandle.ZX];

    // The direction of an arrow, or the two directions of a square.
    public static (double X, double Y, double Z) Axis(GizmoHandle handle) => handle switch
    {
        GizmoHandle.X => (1, 0, 0),
        GizmoHandle.Y => (0, 1, 0),
        GizmoHandle.Z => (0, 0, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(handle), handle, "A square has two directions."),
    };

    public static ((double X, double Y, double Z) U, (double X, double Y, double Z) V) Plane(GizmoHandle handle) => handle switch
    {
        GizmoHandle.XY => ((1, 0, 0), (0, 1, 0)),
        GizmoHandle.YZ => ((0, 1, 0), (0, 0, 1)),
        GizmoHandle.ZX => ((0, 0, 1), (1, 0, 0)),
        _ => throw new ArgumentOutOfRangeException(nameof(handle), handle, "An arrow has one direction."),
    };

    // The length in world units of one axis at the depth of the pivot, or null when the pivot is
    // not in front of the camera.
    public static double? WorldLength(Camera camera, double width, double height, (double X, double Y, double Z) pivot)
    {
        var view = View.Of(camera, width, height);
        var depth = view.Depth(pivot);
        if (!(depth > camera.NearPlane) || !(height > 0))
            return null;
        return AxisPoints * 2 * depth * view.TanHalf / height;
    }

    // The place of each handle, or null when the pivot is not in front of the camera or a point
    // is not finite.
    public static GizmoLayout? Layout(Camera camera, double width, double height, (double X, double Y, double Z) pivot)
    {
        if (!(width > 0 && height > 0) || WorldLength(camera, width, height, pivot) is not { } length)
            return null;

        var view = View.Of(camera, width, height);
        if (view.Pixel(pivot) is not { } origin)
            return null;

        var ends = new Dictionary<GizmoHandle, Vector2>();
        foreach (var arrow in Arrows)
        {
            if (view.Pixel(Along(pivot, Axis(arrow), length)) is not { } end)
                return null;
            ends[arrow] = end;
        }

        var squares = new Dictionary<GizmoHandle, Vector2[]>();
        foreach (var plane in Planes)
        {
            var (u, v) = Plane(plane);
            var corners = new Vector2[4];
            (double A, double B)[] steps = [(SquareFrom, SquareFrom), (SquareTo, SquareFrom), (SquareTo, SquareTo), (SquareFrom, SquareTo)];
            for (var i = 0; i < 4; i++)
            {
                var point = Along(Along(pivot, u, steps[i].A * length), v, steps[i].B * length);
                if (view.Pixel(point) is not { } corner)
                    return null;
                corners[i] = corner;
            }
            squares[plane] = corners;
        }

        return new GizmoLayout(origin, ends, squares);
    }

    // ADR-0038 item 4: the handle under a point of the window, or null. A square wins over an
    // arrow, and the nearest arrow within the tolerance wins.
    public static GizmoHandle? Hit(GizmoLayout layout, double x, double y)
    {
        var point = new Vector2((float)x, (float)y);
        foreach (var plane in Planes)
        {
            if (Inside(layout.Squares[plane], point))
                return plane;
        }

        GizmoHandle? best = null;
        var bestDistance = ArrowTolerancePoints;
        foreach (var arrow in Arrows)
        {
            var distance = ToSegment(point, layout.Origin, layout.Ends[arrow]);
            if (distance <= bestDistance)
                (best, bestDistance) = (arrow, distance);
        }
        return best;
    }

    // ADR-0038 item 8: the offset of the pivot for a drag of a handle from the point of the press
    // to the point of the pointer, or null when a ray is parallel to the axis or to the plane, or
    // meets the plane behind the eye.
    public static (double X, double Y, double Z)? Offset(
        Camera camera, double width, double height, (double X, double Y, double Z) pivot, GizmoHandle handle,
        (double X, double Y) from, (double X, double Y) to)
    {
        var start = Picker.RayThrough(camera, from.X, from.Y, width, height);
        var end = Picker.RayThrough(camera, to.X, to.Y, width, height);
        if (Arrows.Contains(handle))
        {
            var axis = Axis(handle);
            if (Nearest(start, pivot, axis) is not { } a || Nearest(end, pivot, axis) is not { } b)
                return null;
            return Along((0, 0, 0), axis, b - a);
        }

        var (u, v) = Plane(handle);
        if (Picker.OnPlane(start, pivot, u, v) is not { } p || Picker.OnPlane(end, pivot, u, v) is not { } q)
            return null;
        return Along(Along((0, 0, 0), u, q.X - p.X), v, q.Y - p.Y);
    }

    // The triangles of each handle in its colour; the active handle is yellow (ADR-0038 item 4).
    public static IReadOnlyList<GizmoShape> Shapes(GizmoLayout layout, GizmoHandle? active)
    {
        var shapes = new List<GizmoShape>();
        foreach (var plane in Planes)
        {
            var c = layout.Squares[plane];
            shapes.Add(new GizmoShape(plane, [c[0], c[1], c[2], c[0], c[2], c[3]], Colour(plane, active)));
        }

        foreach (var arrow in Arrows)
            shapes.Add(new GizmoShape(arrow, ArrowTriangles(layout.Origin, layout.Ends[arrow]), Colour(arrow, active)));
        return shapes;
    }

    public static Vector4 Colour(GizmoHandle handle, GizmoHandle? active)
    {
        if (handle == active)
            return new Vector4(1f, 0.85f, 0.1f, 1f);
        var alpha = Arrows.Contains(handle) ? 1f : 0.45f;
        return handle switch
        {
            GizmoHandle.X or GizmoHandle.YZ => new Vector4(0.9f, 0.15f, 0.15f, alpha),
            GizmoHandle.Y or GizmoHandle.ZX => new Vector4(0.15f, 0.75f, 0.2f, alpha),
            _ => new Vector4(0.2f, 0.35f, 0.95f, alpha),
        };
    }

    // A shaft of two triangles and a head of one triangle. An arrow that points to the eye has
    // no length on the screen and gives no triangle.
    private static Vector2[] ArrowTriangles(Vector2 origin, Vector2 end)
    {
        var along = end - origin;
        var length = along.Length();
        if (!(length > 1))
            return [];
        var unit = along / length;
        var side = new Vector2(-unit.Y, unit.X);
        var head = Math.Min((float)HeadLengthPoints, length / 2);
        var neck = end - (unit * head);
        var half = side * (float)(ShaftPoints / 2);
        var wing = side * (float)(HeadWidthPoints / 2);
        return
        [
            origin - half, neck - half, neck + half,
            origin - half, neck + half, origin + half,
            neck - wing, end, neck + wing,
        ];
    }

    // True when the point is inside the convex quadrilateral, in either order of its corners.
    private static bool Inside(Vector2[] corners, Vector2 point)
    {
        var sign = 0;
        for (var i = 0; i < corners.Length; i++)
        {
            var (a, b) = (corners[i], corners[(i + 1) % corners.Length]);
            var cross = ((b.X - a.X) * (point.Y - a.Y)) - ((b.Y - a.Y) * (point.X - a.X));
            var s = Math.Sign(cross);
            if (s == 0)
                continue;
            if (sign != 0 && s != sign)
                return false;
            sign = s;
        }
        return sign != 0;
    }

    private static double ToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var squared = ab.LengthSquared();
        var t = squared == 0 ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / squared, 0, 1);
        return (p - (a + (ab * t))).Length();
    }

    // The parameter of the point of the line pivot + t·axis that is nearest to the ray, or null
    // when the ray is parallel to the axis.
    private static double? Nearest(Ray ray, (double X, double Y, double Z) pivot, (double X, double Y, double Z) axis)
    {
        const double Parallel = 1e-9;
        var w = (pivot.X - ray.Origin.X, pivot.Y - ray.Origin.Y, pivot.Z - ray.Origin.Z);
        var b = Dot(axis, ray.Direction);
        var denominator = 1 - (b * b);
        if (denominator < Parallel)
            return null;
        // The two lines p(t) = pivot + t·axis and r(s) = origin + s·direction, with unit vectors.
        return ((b * Dot(ray.Direction, w)) - Dot(axis, w)) / denominator;
    }

    private static (double X, double Y, double Z) Along((double X, double Y, double Z) p, (double X, double Y, double Z) d, double t)
        => (p.X + (t * d.X), p.Y + (t * d.Y), p.Z + (t * d.Z));

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    // The frame of the camera, with the axes of Picker.RayThrough.
    private sealed record View(
        (double X, double Y, double Z) Eye,
        (double X, double Y, double Z) Right,
        (double X, double Y, double Z) Up,
        (double X, double Y, double Z) Forward,
        double TanHalf,
        double Width,
        double Height,
        double Near)
    {
        public static View Of(Camera camera, double width, double height)
        {
            var eye = camera.Eye();
            var forward = Unit((camera.Target.X - eye.X, camera.Target.Y - eye.Y, camera.Target.Z - eye.Z));
            var right = Unit(Cross(forward, (0, 1, 0)));
            var up = Cross(right, forward);
            return new View(eye, right, up, forward, Math.Tan(camera.FieldOfViewDegrees * Math.PI / 360.0), width, height, camera.NearPlane);
        }

        public double Depth((double X, double Y, double Z) p) => Dot(Minus(p, Eye), Forward);

        // The point of the window of a point in front of the near plane, or null.
        public Vector2? Pixel((double X, double Y, double Z) p)
        {
            var d = Minus(p, Eye);
            var forward = Dot(d, Forward);
            if (!(forward > Near))
                return null;
            var aspect = Width / Height;
            var nx = Dot(d, Right) / forward / (TanHalf * aspect);
            var ny = Dot(d, Up) / forward / TanHalf;
            var pixel = new Vector2((float)((nx + 1) / 2 * Width), (float)((1 - ny) / 2 * Height));
            return float.IsFinite(pixel.X) && float.IsFinite(pixel.Y) && Math.Abs(pixel.X) < 1e7 && Math.Abs(pixel.Y) < 1e7 ? pixel : null;
        }

        private static (double X, double Y, double Z) Minus((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        private static (double X, double Y, double Z) Unit((double X, double Y, double Z) v)
        {
            var length = Math.Sqrt(Dot(v, v));
            return (v.X / length, v.Y / length, v.Z / length);
        }
    }
}
