using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0158 item 1 (phase P10.3, TASK-0395): places a dimension of a value of the design in a view of a live sheet. Source is
// the identifier of a constraint of a sketch (SourceKind "constraint") or of an extrude for its distance (SourceKind
// "distance"). X and Y are the place of its line on the paper, in millimetres from its lower left corner. With no
// DimensionId the dimension is new and takes the identifier of the command; with a DimensionId the command places that
// dimension again, with the same identifier at the same index (ADR-0158 item 2).
public sealed record PlaceSheetDimensionCommand : Command
{
    public override string Name => "PlaceSheetDimension";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
    public required Guid Source { get; init; }
    public required string SourceKind { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public Guid? DimensionId { get; init; }
}
