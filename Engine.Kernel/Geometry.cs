namespace Engine.Kernel;

// ADR-0057 items 2 and 3: an exact point or vector of the kernel.
public readonly record struct Vector3R(Rational X, Rational Y, Rational Z)
{
    public static Vector3R FromDoubles(double x, double y, double z) => new(Rational.FromDouble(x), Rational.FromDouble(y), Rational.FromDouble(z));

    public static Vector3R operator +(Vector3R a, Vector3R b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3R operator -(Vector3R a, Vector3R b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3R operator -(Vector3R a) => new(-a.X, -a.Y, -a.Z);

    public static Vector3R operator *(Vector3R a, Rational s) => new(a.X * s, a.Y * s, a.Z * s);

    public Rational Dot(Vector3R b) => (X * b.X) + (Y * b.Y) + (Z * b.Z);

    public Vector3R Cross(Vector3R b) => new((Y * b.Z) - (Z * b.Y), (Z * b.X) - (X * b.Z), (X * b.Y) - (Y * b.X));

    public bool IsZero => X.IsZero && Y.IsZero && Z.IsZero;

    public (double X, double Y, double Z) ToDoubles() => (X.ToDouble(), Y.ToDouble(), Z.ToDouble());
}

// ADR-0057 item 3: an exact plane n · p = d. Canonical scales n so that its first component that is not zero is +1 or
// -1, so two planes with the same points and the same side have equal values.
public readonly record struct PlaneR(Vector3R Normal, Rational Offset)
{
    // The plane through three points that are not on one line, with the normal (b - a) x (c - a), or null.
    public static PlaneR? Through(Vector3R a, Vector3R b, Vector3R c)
    {
        var n = (b - a).Cross(c - a);
        return n.IsZero ? null : new PlaneR(n, n.Dot(a)).Canonical();
    }

    public PlaneR Canonical()
    {
        var first = !Normal.X.IsZero ? Normal.X : !Normal.Y.IsZero ? Normal.Y : Normal.Z;
        var scale = first.Sign > 0 ? Rational.One / first : -(Rational.One / first);
        return new PlaneR(Normal * scale, Offset * scale);
    }

    public PlaneR Flipped() => new(-Normal, -Offset);

    // The sign of n · p - d: +1 in front of the plane, -1 behind it, 0 on it.
    public int Side(Vector3R p) => Rational.SignOfDotMinus(Normal.X, p.X, Normal.Y, p.Y, Normal.Z, p.Z, Offset);
}

public static class Predicates
{
    // ADR-0057 item 3 (ADR-0050, Next): the sign of the determinant of b - a, c - a and d - a. Positive when d is on the
    // side of the plane a, b, c to which (b - a) x (c - a) points.
    public static int Orient3D(Vector3R a, Vector3R b, Vector3R c, Vector3R d)
        => (b - a).Cross(c - a).Dot(d - a).Sign;
}
