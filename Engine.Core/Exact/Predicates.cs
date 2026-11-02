using System.Numerics;

namespace Engine.Core.Exact;

// ADR-0050 (phase K1, TASK-0164): the exact sign of a determinant of the coordinates of points. A
// decision that changes the result of a command of the log uses it, because a rounded determinant can
// have the wrong sign, or the sign zero, when the points are almost on one line.
//
// A predicate first computes the rounded value and a bound of its error. When the bound does not
// decide the sign, the predicate computes the value again with integers, which is exact. Both paths
// use + - * and comparisons only (CLAUDE.md, determinism rules), so the result is the same on each
// platform, and it is the sign of the true value of the coordinates.
public static class Predicates
{
    // ADR-0050 item 2: 4ε with ε = 2^-53. Shewchuk proves the bound (3 + 16ε)ε of the rounded orient2d,
    // with no underflow. The excess, at least 0.99ε times the permanent, covers the error of an underflow
    // when the permanent is at least SmallestPermanent.
    private const double Orient2DBound = 4.0 / 9007199254740992.0;

    // Above 2^-897, so 0.99ε times the permanent is above 2^-951, and the error of the underflow of two
    // products is at most 2^-1074.
    private const double SmallestPermanent = 1e-270;

    // The sign of (b − a) × (c − a): 1 when c is left of the line from a to b, −1 when c is right of
    // it, and 0 when the three points are on one line. Each coordinate must be finite.
    public static int Orient2D((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        var left = (b.X - a.X) * (c.Y - a.Y);
        var right = (b.Y - a.Y) * (c.X - a.X);
        var determinant = left - right;
        var permanent = Math.Abs(left) + Math.Abs(right);

        // A comparison with NaN is false, and an infinite permanent gives an infinite bound, so an
        // overflow goes to the exact path.
        if (permanent >= SmallestPermanent && Math.Abs(determinant) > Orient2DBound * permanent)
            return determinant > 0 ? 1 : -1;

        return Orient2DExact(a, b, c);
    }

    // The exact path of Orient2D alone. A test compares the two paths.
    internal static int Orient2DExact((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        var v = Integers([a.X, a.Y, b.X, b.Y, c.X, c.Y]);
        return (((v[2] - v[0]) * (v[5] - v[1])) - ((v[3] - v[1]) * (v[4] - v[0]))).Sign;
    }

    // The sign of twice the signed area of a loop, the sum of x(i) y(i + 1) − x(i + 1) y(i) in the order
    // of the loop: 1 when the loop turns counter-clockwise, −1 when it turns clockwise, and 0 when the
    // area is zero. The sum is computed with integers each time, because a profile calls it once.
    public static int AreaSign(IReadOnlyList<(double X, double Y)> loop)
    {
        var coordinates = new double[2 * loop.Count];
        for (var i = 0; i < loop.Count; i++)
            (coordinates[2 * i], coordinates[(2 * i) + 1]) = loop[i];

        var v = Integers(coordinates);
        var sum = BigInteger.Zero;
        for (var i = 0; i < loop.Count; i++)
        {
            var j = (i + 1) % loop.Count;
            sum += (v[2 * i] * v[(2 * j) + 1]) - (v[2 * j] * v[(2 * i) + 1]);
        }

        return sum.Sign;
    }

    // ADR-0050 item 3: the exact value of each coordinate as an integer times 2^shift, with one shift
    // for all the coordinates, so a sum and a product of the integers are exact.
    private static BigInteger[] Integers(double[] values)
    {
        var shift = int.MaxValue;
        foreach (var value in values)
        {
            if (!double.IsFinite(value))
                throw new ArgumentException($"The coordinate {value} is not finite, and an exact predicate takes finite coordinates only.", nameof(values));
            if (value != 0)
                shift = Math.Min(shift, Exponent(value));
        }

        var integers = new BigInteger[values.Length];
        for (var i = 0; i < values.Length; i++)
            integers[i] = values[i] == 0 ? BigInteger.Zero : new BigInteger(Mantissa(values[i])) << (Exponent(values[i]) - shift);
        return integers;
    }

    // A finite double is Mantissa(value) times 2^Exponent(value), with an integer mantissa of at most
    // 53 bits. A subnormal double has the exponent −1074 and no hidden bit.
    private static long Mantissa(double value)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var fraction = bits & 0xF_FFFF_FFFF_FFFFL;
        var mantissa = ((bits >> 52) & 0x7FF) == 0 ? fraction : fraction | (1L << 52);
        return bits < 0 ? -mantissa : mantissa;
    }

    private static int Exponent(double value)
    {
        var field = (int)((BitConverter.DoubleToInt64Bits(value) >> 52) & 0x7FF);
        return field == 0 ? -1074 : field - 1075;
    }
}
