using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0155 item 3 (phase P10.2, TASK-0393): removes a view of a live sheet with the dimensions of that view. An undo gives
// both back.
public sealed record RemoveSheetViewCommand : Command
{
    public override string Name => "RemoveSheetView";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
}
