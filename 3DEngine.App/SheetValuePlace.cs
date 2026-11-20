using Engine.Contracts.Sketch;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop;

// ADR-0176 item 4 (phase R30e, TASK-0421): the place on the paper of a value that "Put on sheet" puts on a view of a sheet.
// The code uses + - * / and sqrt only. The numbers go into PlaceSheetDimension, so a replay reads them from the log and does
// not compute them again.
internal static class SheetValuePlace
{
    // ADR-0176 item 4: the distance from the middle of the measured edge to the place of the value, in millimetres of paper.
    public const double Offset = 10;

    // ADR-0176 item 4: as GetSheet places the lines of a view, a point p of the design goes to (X + f (p · right - cu),
    // Y + f (p · up - cv)), where f is the scale divided by the length of the right.
    public static (double X, double Y) Paper(SheetViewInfo view, DatumPoint point)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(point);
        var d = view.Direction;
        var length = Math.Sqrt((d[0] * d[0]) + (d[1] * d[1]) + (d[2] * d[2]));
        var factor = (double)view.ScaleNumerator / view.ScaleDenominator / length;
        var u = (point.X * d[0]) + (point.Y * d[1]) + (point.Z * d[2]);
        var v = (point.X * d[3]) + (point.Y * d[4]) + (point.Z * d[5]);
        return (view.X + (factor * (u - view.CentreU)), view.Y + (factor * (v - view.CentreV)));
    }

    // ADR-0176 item 4: Offset outside the middle of the edge from a to b on the paper, along its normal on the side away from
    // the centre (X, Y) of the view. A radius has one point (a equals b): the direction goes from (X, Y) to it. A direction of
    // zero length is up.
    public static (double X, double Y) Place(SheetViewInfo view, DatumPoint a, DatumPoint b)
    {
        ArgumentNullException.ThrowIfNull(view);
        var (pa, pb) = (Paper(view, a), Paper(view, b));
        var (mx, my) = ((pa.X + pb.X) / 2, (pa.Y + pb.Y) / 2);
        var (ox, oy) = (mx - view.X, my - view.Y);
        var (nx, ny) = a == b ? (ox, oy) : (pa.Y - pb.Y, pb.X - pa.X);
        if ((nx * ox) + (ny * oy) < 0)
            (nx, ny) = (-nx, -ny);
        var length = Math.Sqrt((nx * nx) + (ny * ny));
        return length > 0
            ? (mx + (Offset * nx / length), my + (Offset * ny / length))
            : (mx, my + Offset);
    }
}
