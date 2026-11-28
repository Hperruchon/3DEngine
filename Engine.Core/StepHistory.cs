using Engine.Contracts;

namespace Engine.Core;

// ADR-0031 item 3: the effect of one step on the live body set. Created holds each
// record that the step made, Consumed each record that it removed, as it was before
// the step, and Changed each record that it replaced, before and after. ADR-0033 item 10:
// CreatedSketches and ChangedSketches do the same for the sketches of the step. ADR-0045
// item 9: CreatedImages and ChangedImages do the same for the images of the step. ADR-0051
// item 4: CreatedDatums and ChangedDatums do the same for the datums of the step. ADR-0071 item 5: CreatedReferences
// does the same for the references of an assembly; a reference never changes. ADR-0188 item 3 (phase P16, TASK-0463):
// GroupId is the group of the command of the step, or null.
internal sealed record Step(
    Guid CommandId,
    Guid? GroupId,
    IReadOnlyList<BodyRecord> Created,
    IReadOnlyList<BodyRecord> Consumed,
    IReadOnlyList<(BodyRecord Before, BodyRecord After)> Changed,
    IReadOnlyList<Engine.Contracts.Sketch.SketchRecord> CreatedSketches,
    IReadOnlyList<(Engine.Contracts.Sketch.SketchRecord Before, Engine.Contracts.Sketch.SketchRecord After)> ChangedSketches,
    IReadOnlyList<Engine.Contracts.Images.ImageRecord> CreatedImages,
    IReadOnlyList<(Engine.Contracts.Images.ImageRecord Before, Engine.Contracts.Images.ImageRecord After)> ChangedImages,
    IReadOnlyList<Engine.Contracts.Sketch.DatumRecord> CreatedDatums,
    IReadOnlyList<(Engine.Contracts.Sketch.DatumRecord Before, Engine.Contracts.Sketch.DatumRecord After)> ChangedDatums,
    IReadOnlyList<Engine.Contracts.Assembly.PartReference> CreatedReferences,
    IReadOnlyList<Engine.Contracts.Drawing.SheetRecord> CreatedSheets,
    IReadOnlyList<(Engine.Contracts.Drawing.SheetRecord Before, Engine.Contracts.Drawing.SheetRecord After)> ChangedSheets,
    IReadOnlyList<Engine.Contracts.Drawing.SheetRecord> RemovedSheets,
    IReadOnlyList<Engine.Contracts.Joints.JointRecord> CreatedJoints,
    IReadOnlyList<(Engine.Contracts.Joints.JointRecord Before, Engine.Contracts.Joints.JointRecord After)> ChangedJoints,
    IReadOnlyList<Engine.Contracts.Joints.JointRecord> RemovedJoints);

// ADR-0031 item 2: one history for the Document. The command bus owns it and changes
// it only in its commit, so a rejected Undo or Redo leaves it as it was. The history
// depends only on the log, so an open and a replay rebuild the same history: each
// applies each entry one time, with no look-up in the idempotency cache (finding E75
// of the second codebase review of 2026-10-28).
//
// ADR-0188 item 3 (phase P16, TASK-0463): a unit of undo is the last run of consecutive steps that have one group, or the last
// step when it has no group. An Undo reverses the whole unit, and a Redo applies the whole last undone unit again. The
// undone list keeps each unit in the order of its steps, so its last run is the unit that the next Redo applies. The log
// holds the group of each command, so an open rebuilds the same units. FeatureList and SketchRebuild walk the log with the
// same rule (UnitStart).
internal sealed class StepHistory
{
    private readonly List<Step> _done = [];
    private readonly List<Step> _undone = [];

    // The steps that an Undo reverses, in the order of the log, or null.
    public IReadOnlyList<Step>? NextUndo => _done.Count > 0 ? _done[UnitStart(_done, step => step.GroupId)..] : null;

    // The steps that a Redo applies again, in the order of the log, or null.
    public IReadOnlyList<Step>? NextRedo => _undone.Count > 0 ? _undone[UnitStart(_undone, step => step.GroupId)..] : null;

    // ADR-0031 item 2 (Q3 A): a new step clears the redo list.
    public void Record(Step step)
    {
        _done.Add(step);
        _undone.Clear();
    }

    public void Undone() => Move(_done, _undone);

    public void Redone() => Move(_undone, _done);

    // ADR-0190 item 3 (TASK-0473, finding E242): the reversal of a failed group removes its unit and puts nothing on the redo
    // list, so no Redo applies a part of a proposal that no preview showed.
    public void Reversed()
    {
        var start = UnitStart(_done, step => step.GroupId);
        _done.RemoveRange(start, _done.Count - start);
    }

    // The last unit of one list goes to the end of the other list, in the order of its steps.
    private static void Move(List<Step> from, List<Step> to)
    {
        var start = UnitStart(from, step => step.GroupId);
        to.AddRange(from[start..]);
        from.RemoveRange(start, from.Count - start);
    }

    // ADR-0188 item 3: the index of the first step of the last unit of a list that is not empty: the last step when it has no
    // group, else the first step of the run of consecutive steps with its group at the end of the list.
    internal static int UnitStart<T>(IReadOnlyList<T> steps, Func<T, Guid?> group)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(group);
        var start = steps.Count - 1;
        if (group(steps[start]) is not { } last)
            return start;
        while (start > 0 && group(steps[start - 1]) == last)
            start--;
        return start;
    }
}
