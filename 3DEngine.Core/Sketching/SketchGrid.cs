namespace ThreeDEngine.Core.Sketching;

// One step of the series 1, 2 and 5 × 10^Power mm (ADR-0034 item 3). The step is a mantissa and
// a power of 10, so that a rounded position is the double nearest to a decimal number: 0.3 and not
// 0.30000000000000004.
public readonly record struct GridStep(int Mantissa, int Power)
{
    public double Millimetres => Power >= 0 ? Mantissa * Ten(Power) : Mantissa / Ten(-Power);

    // The multiple of the step nearest to the value; a half goes away from zero.
    public double Round(double value)
    {
        if (Power >= 0)
        {
            var unit = Mantissa * Ten(Power);
            return Math.Round(value / unit, MidpointRounding.AwayFromZero) * unit;
        }

        var scale = Ten(-Power);
        return Math.Round(value * scale / Mantissa, MidpointRounding.AwayFromZero) * Mantissa / scale;
    }

    // 10^power as an exact double, for a power from 0 to 6.
    private static double Ten(int power)
    {
        var result = 1.0;
        for (var i = 0; i < power; i++)
            result *= 10;
        return result;
    }
}

// The rounding of a tap on the plane of a sketch (ADR-0034 item 3, Q3 A). The step depends on the
// zoom: it is the smallest step of the series that is at least MinimumPixels on the plane, so a
// person sees round numbers, and two taps on one row give the same coordinate. It is presentation:
// it has no grid to show and no target to find, as the snap engine of R11 will have.
public static class SketchGrid
{
    public const double MinimumPixels = 8;

    // The steps from 0.001 mm to 5 000 000 mm, the smallest first.
    public static IReadOnlyList<GridStep> Series { get; } =
        [.. Enumerable.Range(-3, 10).SelectMany(power => new[] { 1, 2, 5 }.Select(mantissa => new GridStep(mantissa, power)))];

    // The smallest step of at least MinimumPixels, for a zoom of this many pixels for each
    // millimetre. A zoom that is not a positive number gives the largest step.
    public static GridStep Step(double pixelsPerMillimetre)
    {
        if (!(pixelsPerMillimetre > 0) || double.IsInfinity(pixelsPerMillimetre))
            return Series[^1];

        foreach (var step in Series)
        {
            if (step.Millimetres * pixelsPerMillimetre >= MinimumPixels)
                return step;
        }

        return Series[^1];
    }
}
