namespace Engine.Contracts.Drawing;

// ADR-0153 items 1 to 5 (phase P10.1, TASK-0389): one drawing sheet of the Document. A record never changes: an edit gives a
// new record with the same identifier, as a sketch and an image do. Paper is one of Papers (ISO 216); Landscape is the
// orientation of the paper; ThirdAngle is false for ISO first angle (item 3). The views and the dimensions are in
// the order of their commands.
public sealed record SheetRecord(
    Guid Id,
    string Name,
    string Paper,
    bool Landscape,
    bool ThirdAngle,
    IReadOnlyList<SheetView> Views,
    IReadOnlyList<SheetDimension> Dimensions)
{
    // ADR-0153 item 2 (Q2 A): the papers of a sheet.
    public static readonly IReadOnlyList<string> Papers = ["A4", "A3", "A2", "A1", "A0"];
}

// ADR-0153 item 4 (Q3 A, Q4 A, Q8 A): one view of a sheet. Direction holds nine numbers: the right, the up and the direction
// toward the eye, each as x, y and z (determinism rule 2). The scale is ScaleNumerator : ScaleDenominator, two whole numbers
// greater than zero. X and Y are the place of the centre of the view on the paper, in millimetres from its lower left corner.
// A view shows each live body of the part. ADR-0165 items 1 and 2 (phase R30g, TASK-0405): HiddenLines and CentreLines say if
// the view shows its hidden lines and its centre lines; each is true by default, so a log of a view before R30g gives the
// same record, and SetSheetViewLines changes them.
public sealed record SheetView(
    Guid Id,
    IReadOnlyList<double> Direction,
    long ScaleNumerator,
    long ScaleDenominator,
    double X,
    double Y,
    bool HiddenLines = true,
    bool CentreLines = true,
    SheetSection? Section = null);

// ADR-0169 item 1 (phase R30h, TASK-0411): the cut of a section view. The cut plane goes through Point, the x, y and z of the
// design in millimetres, and its normal is the direction toward the eye of the view; the cut removes the material on the side
// of the eye. Parent is the view that shows the cut line, and Letter is one capital letter from A to Z.
public sealed record SheetSection(Guid Parent, IReadOnlyList<double> Point, string Letter);

// ADR-0153 item 5 (Q5 A, Q6 A): one dimension of a sheet, in a view. Source is the identifier of a constraint of a sketch
// (SourceKind "constraint") or of an extrude for its distance (SourceKind "distance"). X and Y are the place of its line on
// the paper, in millimetres. The number comes from the design at each read; with no source in the Document it is lost.
public sealed record SheetDimension(Guid Id, Guid ViewId, Guid Source, string SourceKind, double X, double Y);
