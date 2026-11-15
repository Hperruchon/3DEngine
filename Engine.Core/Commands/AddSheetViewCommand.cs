using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0155 item 1 (phase P10.2, TASK-0393): adds a view to a live sheet. The identifier of the view is the identifier of the
// command. The nine numbers are the right, the up and the direction toward the eye, each as x, y and z, an exact frame
// (ADR-0155 item 1); they are nine properties, so two commands with the same numbers are equal. The scale is
// ScaleNumerator : ScaleDenominator. X and Y are the place of the centre of the view on the paper, in millimetres from its
// lower left corner.
public sealed record AddSheetViewCommand : Command
{
    public override string Name => "AddSheetView";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required double RightX { get; init; }
    public required double RightY { get; init; }
    public required double RightZ { get; init; }
    public required double UpX { get; init; }
    public required double UpY { get; init; }
    public required double UpZ { get; init; }
    public required double EyeX { get; init; }
    public required double EyeY { get; init; }
    public required double EyeZ { get; init; }
    public required long ScaleNumerator { get; init; }
    public required long ScaleDenominator { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }

    // The nine numbers in the order of SheetView.Direction.
    public IReadOnlyList<double> Direction() => [RightX, RightY, RightZ, UpX, UpY, UpZ, EyeX, EyeY, EyeZ];
}
