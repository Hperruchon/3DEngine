using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0168 item 3 (phase R30c, TASK-0408): moves the centre of a view of a live sheet to a new place on the paper, in one step
// of undo. With Aligned, each other view on a line of projection through the view moves by the same distance: a view whose
// frame has the same right and whose centre has the same X (over or under it), or the same up and the same Y (at its left or
// right). So a drag of the front view takes its projected views with it.
public sealed record MoveSheetViewsCommand : Command
{
    public override string Name => "MoveSheetViews";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public required Guid ViewId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public bool Aligned { get; init; }
}
