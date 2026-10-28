using Engine.Contracts;

namespace Engine.Core;

// ADR-0031 item 3: the effect of one step on the live body set. Created holds each
// record that the step made, Consumed each record that it removed, as it was before
// the step, and Changed each record that it replaced, before and after. ADR-0033 item 10:
// CreatedSketches and ChangedSketches do the same for the sketches of the step.
internal sealed record Step(
    Guid CommandId,
    IReadOnlyList<BodyRecord> Created,
    IReadOnlyList<BodyRecord> Consumed,
    IReadOnlyList<(BodyRecord Before, BodyRecord After)> Changed,
    IReadOnlyList<Engine.Contracts.Sketch.SketchRecord> CreatedSketches,
    IReadOnlyList<(Engine.Contracts.Sketch.SketchRecord Before, Engine.Contracts.Sketch.SketchRecord After)> ChangedSketches);

// ADR-0031 item 2: one history for the Document. The command bus owns it and changes
// it only in its commit, so a rejected Undo or Redo leaves it as it was. The history
// depends only on the log, so an open and a replay rebuild the same history: each
// applies each entry one time, with no look-up in the idempotency cache (finding E75
// of the second codebase review of 2026-10-28).
internal sealed class StepHistory
{
    private readonly List<Step> _done = [];
    private readonly List<Step> _undone = [];

    // The step that an Undo reverses, or null.
    public Step? NextUndo => _done.Count > 0 ? _done[^1] : null;

    // The step that a Redo applies again, or null.
    public Step? NextRedo => _undone.Count > 0 ? _undone[^1] : null;

    // ADR-0031 item 2 (Q3 A): a new step clears the redo list.
    public void Record(Step step)
    {
        _done.Add(step);
        _undone.Clear();
    }

    public void Undone()
    {
        _undone.Add(_done[^1]);
        _done.RemoveAt(_done.Count - 1);
    }

    public void Redone()
    {
        _done.Add(_undone[^1]);
        _undone.RemoveAt(_undone.Count - 1);
    }
}
