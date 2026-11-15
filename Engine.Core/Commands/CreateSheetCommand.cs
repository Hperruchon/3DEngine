using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): makes a drawing sheet with no view. The identifier of the sheet is the identifier of
// the command. The paper is one of A4 to A0, landscape by default; the projection is ISO first angle by default.
public sealed record CreateSheetCommand : Command
{
    public override string Name => "CreateSheet";
    public override int SchemaVersion => 1;

    public required string SheetName { get; init; }
    public string Paper { get; init; } = "A4";
    public bool Landscape { get; init; } = true;
    public bool ThirdAngle { get; init; }
}
