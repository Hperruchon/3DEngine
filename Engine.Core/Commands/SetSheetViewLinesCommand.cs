using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0165 item 2 (phase R30g, TASK-0405): sets whether a view of a live sheet shows its hidden lines and its centre lines.
public sealed record SetSheetViewLinesCommand : Command
{
    public override string Name => "SetSheetViewLines";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
    public required bool HiddenLines { get; init; }
    public required bool CentreLines { get; init; }
}
