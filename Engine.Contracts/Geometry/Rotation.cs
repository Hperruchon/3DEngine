namespace Engine.Contracts.Geometry;

// ADR-0112 item 3 (TASK-0335): a turn about the origin of the Document, as the images of the x, y and z axes. A point p
// goes to X * p.x + Y * p.y + Z * p.z. The command Rotate computes it from a quaternion with + - * / only, so each
// platform gives the same bits, and each reader (the backend, the owned kernel and the frame of a datum) takes the same
// nine numbers.
public readonly record struct Rotation(
    double Xx, double Xy, double Xz,
    double Yx, double Yy, double Yz,
    double Zx, double Zy, double Zz)
{
    // ADR-0112 item 2: the turn of the quaternion (w, x, y, z), which need not have the length 1. With s = w² + x² + y² +
    // z², each entry is a polynomial of degree 2 divided by s, so a quaternion of small integers, as (1, 1, 0, 0) for a
    // quarter turn about x, gives exact entries. Null when a value is not finite, when s is below SmallestSum or not finite,
    // or when an entry is not finite.
    public static Rotation? FromQuaternion(double w, double x, double y, double z)
    {
        if (!(double.IsFinite(w) && double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z)))
            return null;
        var (ww, xx, yy, zz) = (w * w, x * x, y * y, z * z);
        var s = ww + xx + yy + zz;
        if (!(s >= SmallestSum) || !double.IsFinite(s))
            return null;
        var (xy, xz, yz, wx, wy, wz) = (x * y, x * z, y * z, w * x, w * y, w * z);
        var rotation = new Rotation(
            (ww + xx - yy - zz) / s, 2 * (xy + wz) / s, 2 * (xz - wy) / s,
            2 * (xy - wz) / s, (ww - xx + yy - zz) / s, 2 * (yz + wx) / s,
            2 * (xz + wy) / s, 2 * (yz - wx) / s, (ww - xx - yy + zz) / s);
        double[] entries = [rotation.Xx, rotation.Xy, rotation.Xz, rotation.Yx, rotation.Yy, rotation.Yz, rotation.Zx, rotation.Zy, rotation.Zz];
        return entries.All(double.IsFinite) ? rotation : null;
    }

    // ADR-0114 item 1 (finding E201 of the codebase review of 2026-11-10, TASK-0339): the smallest s of a turn, 2^-969. A
    // double below 2^-1022 is subnormal and has fewer than 53 bits. When s is at least 2^-969, each product of two parts
    // that changes an entry by one unit in the last place or more is at least s · 2^-53, so it is a normal double with 53
    // bits. With parts near 1e-162 the squares were subnormal, and the entries gave a scale of 5, not a turn. ScaleB is exact.
    private static readonly double SmallestSum = Math.ScaleB(1.0, -969);

    // The image of the point (x, y, z), with + and * only, in a fixed order.
    public (double X, double Y, double Z) Apply(double x, double y, double z)
        => ((Xx * x) + (Yx * y) + (Zx * z), (Xy * x) + (Yy * y) + (Zy * z), (Xz * x) + (Yz * y) + (Zz * z));
}
