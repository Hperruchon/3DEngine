using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Picking;

// A ray in the space of the scene: an origin and a direction of length 1.
public readonly record struct Ray((double X, double Y, double Z) Origin, (double X, double Y, double Z) Direction);

// The triangles of one body, as the desktop holds them for a pick: the positions of the
// tessellation in double precision, three values for each vertex, and three indices for each
// triangle. A hidden body is not picked (ADR-0029 item 3).
public sealed record PickBody(Guid Id, double[] Positions, int[] Indices, bool Visible);

// The body that a ray hits first, and the distance from the origin of the ray to the hit.
public readonly record struct PickHit(Guid Id, double Distance);

// The pick of the desktop (ADR-0029 item 1, TASK-0086): the ray of a pixel, and the nearest
// visible body that it hits. It needs no GPU, so a test reads it. A pick is presentation: it
// does not enter the log, only the identifier of the body that a later command names does.
public static class Picker
{
    // The ray from the eye of the camera through a point of a picture of this size in pixels,
    // with x to the right and y down, as the samples of the window give it. It agrees with
    // Camera.View and Camera.Projection: the centre of the picture gives the direction of the
    // target.
    public static Ray RayThrough(Camera camera, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (!(width > 0 && height > 0))
            throw new ArgumentOutOfRangeException(nameof(width), "A picture has a positive size.");

        var eye = camera.Eye();
        var forward = Normal((camera.Target.X - eye.X, camera.Target.Y - eye.Y, camera.Target.Z - eye.Z));
        var right = Normal(Cross(forward, (0, 1, 0)));
        var up = Cross(right, forward);

        var tanHalf = Math.Tan(camera.FieldOfViewDegrees * Math.PI / 360.0);
        var aspect = width / height;
        var nx = (2 * x / width - 1) * tanHalf * aspect;
        var ny = (1 - 2 * y / height) * tanHalf;

        var direction = Normal((
            forward.X + right.X * nx + up.X * ny,
            forward.Y + right.Y * nx + up.Y * ny,
            forward.Z + right.Z * nx + up.Z * ny));
        return new Ray(eye, direction);
    }

    // The nearest visible body that the ray hits in front of its origin, or null. Each face
    // counts, so the inside of an open mesh is a hit too.
    public static PickHit? Nearest(Ray ray, IEnumerable<PickBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        PickHit? nearest = null;

        foreach (var body in bodies)
        {
            if (!body.Visible)
                continue;

            for (var i = 0; i + 2 < body.Indices.Length; i += 3)
            {
                if (Hit(ray, Vertex(body, body.Indices[i]), Vertex(body, body.Indices[i + 1]), Vertex(body, body.Indices[i + 2])) is { } distance
                    && (nearest is null || distance < nearest.Value.Distance))
                {
                    nearest = new PickHit(body.Id, distance);
                }
            }
        }

        return nearest;
    }

    // The distance along the ray to a triangle, or null: the method of Möller and Trumbore.
    private static double? Hit(Ray ray, (double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c)
    {
        const double Epsilon = 1e-12;
        var edge1 = Minus(b, a);
        var edge2 = Minus(c, a);
        var p = Cross(ray.Direction, edge2);
        var determinant = Dot(edge1, p);
        if (Math.Abs(determinant) < Epsilon)
            return null;

        var inverse = 1 / determinant;
        var s = Minus(ray.Origin, a);
        var u = Dot(s, p) * inverse;
        if (u < 0 || u > 1)
            return null;

        var q = Cross(s, edge1);
        var v = Dot(ray.Direction, q) * inverse;
        if (v < 0 || u + v > 1)
            return null;

        var t = Dot(edge2, q) * inverse;
        return t > Epsilon ? t : null;
    }

    private static (double X, double Y, double Z) Vertex(PickBody body, int index)
        => (body.Positions[3 * index], body.Positions[3 * index + 1], body.Positions[3 * index + 2]);

    private static (double X, double Y, double Z) Minus((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => (a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) v)
    {
        var length = Math.Sqrt(Dot(v, v));
        return (v.X / length, v.Y / length, v.Z / length);
    }
}
