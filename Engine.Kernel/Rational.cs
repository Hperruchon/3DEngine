using System.Numerics;

namespace Engine.Kernel;

// ADR-0057 item 2: an exact rational number, the quotient of two BigInteger values. The denominator is positive and the
// fraction is in lowest terms, so two equal values have equal parts. + - * / and a comparison are exact on each
// platform. A double enters exactly from its bits and leaves with one rounding to the nearest.
public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
{
    public BigInteger Numerator { get; }

    public BigInteger Denominator { get; }

    public static readonly Rational Zero = new(BigInteger.Zero, BigInteger.One, normal: true);

    public static readonly Rational One = new(BigInteger.One, BigInteger.One, normal: true);

    private Rational(BigInteger numerator, BigInteger denominator, bool normal)
    {
        Numerator = numerator;
        Denominator = denominator;
        _ = normal;
    }

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException("A rational has a denominator that is not zero.");
        if (denominator.Sign < 0)
            (numerator, denominator) = (-numerator, -denominator);
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (!divisor.IsOne && !divisor.IsZero)
            (numerator, denominator) = (numerator / divisor, denominator / divisor);
        Numerator = numerator;
        Denominator = numerator.IsZero ? BigInteger.One : denominator;
    }

    // The default value of the struct has a denominator of zero; each member reads it as zero.
    private BigInteger Den => Denominator.IsZero ? BigInteger.One : Denominator;

    public int Sign => Numerator.Sign;

    public bool IsZero => Numerator.IsZero;

    public static implicit operator Rational(long value) => new(value, BigInteger.One, normal: true);

    // The exact value of a finite double: its significand times a power of two.
    public static Rational FromDouble(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "A rational has a finite value.");
        if (value == 0)
            return Zero;
        var bits = BitConverter.DoubleToInt64Bits(value);
        var negative = bits < 0;
        var exponent = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xFFFFFFFFFFFFFL;
        if (exponent == 0)
            exponent = 1;
        else
            mantissa |= 1L << 52;
        exponent -= 1075;
        var numerator = new BigInteger(negative ? -mantissa : mantissa);
        return exponent >= 0
            ? new Rational(numerator << exponent, BigInteger.One)
            : new Rational(numerator, BigInteger.One << -exponent);
    }

    // The nearest double, with a tie to the even significand. A value beyond the range of a double gives an infinity, and
    // a value below the smallest normal double loses bits, as a division of doubles does.
    public double ToDouble()
    {
        if (Numerator.IsZero)
            return 0;
        var a = BigInteger.Abs(Numerator);
        var b = Den;
        var shift = (int)(a.GetBitLength() - b.GetBitLength()) - 53;
        BigInteger Quotient(int s, out BigInteger rest, out BigInteger divisor)
        {
            divisor = s >= 0 ? b << s : b;
            var dividend = s >= 0 ? a : a << -s;
            return BigInteger.DivRem(dividend, divisor, out rest);
        }

        var q = Quotient(shift, out var r, out var d);
        if (q >= BigInteger.One << 54)
            q = Quotient(++shift, out r, out d);
        else if (q < BigInteger.One << 53)
            q = Quotient(--shift, out r, out d);

        // q has 54 bits: 53 for the double and one to round; r and d decide a tie.
        var half = !q.IsEven;
        q >>= 1;
        shift++;
        if (half && (!r.IsZero || !q.IsEven))
            q += 1;
        var result = Math.ScaleB((double)q, shift);
        return Numerator.Sign < 0 ? -result : result;
    }

    public static Rational operator +(Rational x, Rational y)
        => x.Den == y.Den ? new Rational(x.Numerator + y.Numerator, x.Den) : new Rational((x.Numerator * y.Den) + (y.Numerator * x.Den), x.Den * y.Den);

    public static Rational operator -(Rational x, Rational y)
        => x.Den == y.Den ? new Rational(x.Numerator - y.Numerator, x.Den) : new Rational((x.Numerator * y.Den) - (y.Numerator * x.Den), x.Den * y.Den);

    public static Rational operator -(Rational x) => new(-x.Numerator, x.Den, normal: true);

    public static Rational operator *(Rational x, Rational y) => new(x.Numerator * y.Numerator, x.Den * y.Den);

    public static Rational operator /(Rational x, Rational y) => new(x.Numerator * y.Den, x.Den * y.Numerator);

    public static bool operator ==(Rational x, Rational y) => x.Equals(y);

    public static bool operator !=(Rational x, Rational y) => !x.Equals(y);

    public static bool operator <(Rational x, Rational y) => x.CompareTo(y) < 0;

    public static bool operator >(Rational x, Rational y) => x.CompareTo(y) > 0;

    public static bool operator <=(Rational x, Rational y) => x.CompareTo(y) <= 0;

    public static bool operator >=(Rational x, Rational y) => x.CompareTo(y) >= 0;

    public int CompareTo(Rational other) => (Numerator * other.Den).CompareTo(other.Numerator * Den);

    public bool Equals(Rational other) => Numerator == other.Numerator && Den == other.Den;

    public override bool Equals(object? obj) => obj is Rational other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Numerator, Den);

    public override string ToString() => Den.IsOne ? Numerator.ToString() : $"{Numerator}/{Den}";
}
