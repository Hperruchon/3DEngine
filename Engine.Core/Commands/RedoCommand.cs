using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0031 item 1: applies again the last step that an Undo reversed.
public sealed record RedoCommand : Command
{
    public override string Name => "Redo";
    public override int SchemaVersion => 1;
}
