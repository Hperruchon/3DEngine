using System.Globalization;

namespace ThreeDEngine.Core.Measuring;

// ADR-0056 (phase R13, TASK-0178): the dimensions of a box in the space of the Document, for the overlay of the
// desktop. They are presentation (ADR-0007): nothing here reaches the log, and the code uses + - * / and comparisons
// only.
public static class Dimensions
{
    // An axis of the space of the Document.
    public enum Axis
    {
        X,
        Y,
        Z,
    }

    // One dimension: the dimension line from From to To, the two ends of the edge that it measures, and its value.
    public sealed record Dimension(
        Axis Along,
        (double X, double Y, double Z) From,
        (double X, double Y, double Z) To,
        (double X, double Y, double Z) EdgeFrom,
        (double X, double Y, double Z) EdgeTo,
        double Value);

    // The share of the largest extent by which a dimension line stands out from its edge (item 4).
    public const double Gap = 1.0 / 20.0;

    // Item 4: the three dimensions of the box, along X, Y and Z, seen from the eye. With the y axis up, the dimensions of
    // X and Z lie at the lowest Y, on the faces that face the eye, and the dimension of Y lies on the vertical edge of the
    // side of the eye in X and the far side in Z. A box with no extent gives no dimension.
    // Finding V147 of the codebase review of 2026-11-08 (c) (TASK-0311): with the z axis up (the camera of the session
    // since v0.292), the roles of Y and Z change: the dimensions of X and Y lie at the lowest Z, and the dimension of Z lies on
    // the vertical edge of the side of the eye in X and the far side in Y. Until TASK-0311 the lines took the y axis as up.
    public static IReadOnlyList<Dimension> Of(
        (double X, double Y, double Z) min, (double X, double Y, double Z) max, (double X, double Y, double Z) eye, bool zUp = false)
    {
        var (dx, dy, dz) = (max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var largest = Math.Max(dx, Math.Max(dy, dz));
        if (!(largest > 0) || !double.IsFinite(largest))
            return [];

        var gap = largest * Gap;
        if (zUp)
            return ZUp(min, max, eye, gap, (dx, dy, dz));

        var (cx, cz) = ((min.X + max.X) / 2, (min.Z + max.Z) / 2);
        var sx = eye.X >= cx ? 1.0 : -1.0;
        var sz = eye.Z >= cz ? 1.0 : -1.0;
        var nearX = sx > 0 ? max.X : min.X;
        var nearZ = sz > 0 ? max.Z : min.Z;
        var farZ = sz > 0 ? min.Z : max.Z;
        var low = min.Y;

        Dimension[] all =
        [
            Line(Axis.X, (min.X, low, nearZ), (max.X, low, nearZ), (0, 0, sz * gap), dx),
            Line(Axis.Y, (nearX, min.Y, farZ), (nearX, max.Y, farZ), (sx * gap, 0, 0), dy),
            Line(Axis.Z, (nearX, low, min.Z), (nearX, low, max.Z), (sx * gap, 0, 0), dz),
        ];

        // An extent of zero, for example of a flat body, has no line.
        return [.. all.Where(dimension => dimension.Value > 0)];
    }

    // V147 (TASK-0311): the lines of a box with the z axis up, as Of with the roles of Y and Z changed.
    private static IReadOnlyList<Dimension> ZUp(
        (double X, double Y, double Z) min, (double X, double Y, double Z) max, (double X, double Y, double Z) eye, double gap, (double X, double Y, double Z) extent)
    {
        var (cx, cy) = ((min.X + max.X) / 2, (min.Y + max.Y) / 2);
        var sx = eye.X >= cx ? 1.0 : -1.0;
        var sy = eye.Y >= cy ? 1.0 : -1.0;
        var nearX = sx > 0 ? max.X : min.X;
        var nearY = sy > 0 ? max.Y : min.Y;
        var farY = sy > 0 ? min.Y : max.Y;
        var low = min.Z;

        Dimension[] all =
        [
            Line(Axis.X, (min.X, nearY, low), (max.X, nearY, low), (0, sy * gap, 0), extent.X),
            Line(Axis.Y, (nearX, min.Y, low), (nearX, max.Y, low), (sx * gap, 0, 0), extent.Y),
            Line(Axis.Z, (nearX, farY, min.Z), (nearX, farY, max.Z), (sx * gap, 0, 0), extent.Z),
        ];

        return [.. all.Where(dimension => dimension.Value > 0)];
    }

    private static Dimension Line(
        Axis along, (double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) offset, double value)
        => new(along, Add(a, offset), Add(b, offset), a, b, value);

    private static (double X, double Y, double Z) Add((double X, double Y, double Z) p, (double X, double Y, double Z) d)
        => (p.X + d.X, p.Y + d.Y, p.Z + d.Z);

    // Item 5: a value with at most two decimals, in the invariant culture.
    public static string Format(double value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);

    // Item 5: the label of a dimension, for example "X 1200 mm".
    public static string Label(Dimension dimension, string unit) => $"{dimension.Along} {Format(dimension.Value)} {unit}";

    // Item 6 (Q1 A): the thickness of a body, the absolute value of the distance in effect of a live extrude, else the
    // smallest extent of its box.
    public static double Thickness((double X, double Y, double Z) min, (double X, double Y, double Z) max, double? distance)
        => distance is { } d ? Math.Abs(d) : Math.Min(max.X - min.X, Math.Min(max.Y - min.Y, max.Z - min.Z));

    // Item 3: the union of the boxes, or null when there is none.
    public static ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Union(
        IEnumerable<((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)> boxes)
    {
        ArgumentNullException.ThrowIfNull(boxes);
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? union = null;
        foreach (var (min, max) in boxes)
        {
            union = union is not { } u
                ? (min, max)
                : ((Math.Min(u.Min.X, min.X), Math.Min(u.Min.Y, min.Y), Math.Min(u.Min.Z, min.Z)),
                    (Math.Max(u.Max.X, max.X), Math.Max(u.Max.Y, max.Y), Math.Max(u.Max.Z, max.Z)));
        }

        return union;
    }
}
