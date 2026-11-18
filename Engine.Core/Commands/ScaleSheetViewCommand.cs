using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0167 item 1 (phase R30b, TASK-0407): changes the scale of one view of a live sheet, or of each view when ViewId is null,
// in one step of undo. The scale is ScaleNumerator : ScaleDenominator. The place of each view and each dimension stay.
public sealed record ScaleSheetViewCommand : Command
{
    public override string Name => "ScaleSheetView";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public Guid? ViewId { get; init; }
    public required long ScaleNumerator { get; init; }
    public required long ScaleDenominator { get; init; }
}
