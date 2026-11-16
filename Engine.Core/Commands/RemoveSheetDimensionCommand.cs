using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0158 item 3 (phase P10.3, TASK-0395): removes a dimension of a live sheet. A lost dimension can be removed, because the
// command does not read the source.
public sealed record RemoveSheetDimensionCommand : Command
{
    public override string Name => "RemoveSheetDimension";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid DimensionId { get; init; }
}
