using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0155 item 2 (phase P10.2, TASK-0393): moves the centre of a view of a live sheet to a new place on the paper, in
// millimetres from its lower left corner.
public sealed record MoveSheetViewCommand : Command
{
    public override string Name => "MoveSheetView";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
}
