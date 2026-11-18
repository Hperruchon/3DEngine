using Engine.Contracts;

namespace Engine.Core.Queries;

// ADR-0165 item 1 (phase R30a, TASK-0405): the visible and the hidden lines of the live bodies seen along one exact frame,
// with no sheet. The nine numbers are the right, the up and the direction toward the eye, each as x, y and z, as for
// AddSheetView.
public sealed record GetViewLinesQuery : Query
{
    public override string Name => "GetViewLines";
    public override int SchemaVersion => 1;

    public required double RightX { get; init; }
    public required double RightY { get; init; }
    public required double RightZ { get; init; }
    public required double UpX { get; init; }
    public required double UpY { get; init; }
    public required double UpZ { get; init; }
    public required double EyeX { get; init; }
    public required double EyeY { get; init; }
    public required double EyeZ { get; init; }

    // The nine numbers in the order of SheetView.Direction.
    public IReadOnlyList<double> Direction() => [RightX, RightY, RightZ, UpX, UpY, UpZ, EyeX, EyeY, EyeZ];
}

// ADR-0165 item 1: the result of GetViewLines. Each place is in units of the design on the plane of the view: a point p of
// the world goes to (p · Right / L, p · Up / L), where L is the length of Right. So a line keeps the length of the design,
// and the origin of the world is at (0, 0). The lines are in the order of ViewEdges, as GetSheet gives them.
public sealed record ViewLinesInfo(
    IReadOnlyList<double> Direction,
    IReadOnlyList<ViewLineInfo> Lines,
    IReadOnlyList<ViewSkippedBody> Skipped);

// One line of the view. Kind is "edge" or "outline", as in GetSheet. A line with a circle or an ellipse is its arc from the
// start to the end, counter-clockwise on the view; a start equal to the end is the whole curve.
public sealed record ViewLineInfo(Guid Body, double FromX, double FromY, double ToX, double ToY, bool Visible, string Kind)
{
    public ViewCircleInfo? Circle { get; init; }

    public ViewEllipseInfo? Ellipse { get; init; }
}

// A circle on the view: its centre and its radius in units of the design.
public sealed record ViewCircleInfo(double X, double Y, double Radius);

// An ellipse on the view: its centre and its two half axes at a right angle, in units of the design.
public sealed record ViewEllipseInfo(double X, double Y, double MajorX, double MajorY, double MinorX, double MinorY);

// A body that gives no lines, with the reason.
public sealed record ViewSkippedBody(Guid Body, string Reason);
