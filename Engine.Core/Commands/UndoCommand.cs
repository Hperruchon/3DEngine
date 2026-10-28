using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0031 item 1: reverses the last step of the Document that is not undone.
public sealed record UndoCommand : Command
{
    public override string Name => "Undo";
    public override int SchemaVersion => 1;
}
