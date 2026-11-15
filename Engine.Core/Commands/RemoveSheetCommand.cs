using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): removes a live sheet with its views and its dimensions. An undo gives it back.
public sealed record RemoveSheetCommand : Command
{
    public override string Name => "RemoveSheet";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
}
