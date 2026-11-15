using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): gives a live sheet a new name.
public sealed record RenameSheetCommand : Command
{
    public override string Name => "RenameSheet";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required string SheetName { get; init; }
}
