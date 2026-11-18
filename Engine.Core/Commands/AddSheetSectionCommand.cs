using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0169 item 2 (phase R30h, TASK-0411): adds a section of a view to a live sheet. ViewId is the parent, which shows the cut
// line. Look is the direction of sight on the parent: "left", "right", "up" or "down". PointX, PointY and PointZ are a point of
// the cut plane in the design, in millimetres. X and Y are the place of the centre of the section on the paper, in
// millimetres from its lower left corner. With no Letter the section takes the first free letter. The identifier of the
// section is the identifier of the command.
public sealed record AddSheetSectionCommand : Command
{
    public override string Name => "AddSheetSection";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
    public required string Look { get; init; }
    public required double PointX { get; init; }
    public required double PointY { get; init; }
    public required double PointZ { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public string? Letter { get; init; }
}
