using Engine.Contracts;
using Engine.Contracts.Drawing;

namespace Engine.Core.Queries;

// ADR-0155 item 5 (phase P10.2, TASK-0393): a live sheet with the lines of each view on the paper.
public sealed record GetSheetQuery : Query
{
    public override string Name => "GetSheet";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
}

// The result of GetSheet. Width and Height are the size of the paper in millimetres with its orientation. Each place is in
// millimetres from the lower left corner of the paper. The dimensions are the records of the sheet until P10.3.
public sealed record SheetInfo(
    Guid SheetId,
    string Name,
    string Paper,
    bool Landscape,
    bool ThirdAngle,
    double Width,
    double Height,
    IReadOnlyList<SheetViewInfo> Views,
    IReadOnlyList<SheetDimension> Dimensions);

// One view: its record, its lines on the paper in the order of ViewEdges, and each body that gives no lines with
// its reason (ADR-0155 item 5).
public sealed record SheetViewInfo(
    Guid ViewId,
    IReadOnlyList<double> Direction,
    long ScaleNumerator,
    long ScaleDenominator,
    double X,
    double Y,
    IReadOnlyList<SheetLine> Lines,
    IReadOnlyList<SheetSkippedBody> Skipped);

// One line of a view on the paper. Kind is "edge" or "outline". A line with a circle or an ellipse is its
// arc from the start to the end, counter-clockwise on the paper; a start equal to the end is the whole curve (ADR-0155 item 5).
public sealed record SheetLine(Guid Body, double FromX, double FromY, double ToX, double ToY, bool Visible, string Kind)
{
    public SheetCircle? Circle { get; init; }

    public SheetEllipse? Ellipse { get; init; }
}

// A circle on the paper: its centre and its radius in millimetres.
public sealed record SheetCircle(double X, double Y, double Radius);

// An ellipse on the paper: its centre and its two half axes at a right angle, in millimetres.
public sealed record SheetEllipse(double X, double Y, double MajorX, double MajorY, double MinorX, double MinorY);

// A body that gives no lines, with the reason.
public sealed record SheetSkippedBody(Guid Body, string Reason);
