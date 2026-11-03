using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Features;

namespace Engine.Core.Sketch;

// ADR-0037 items 4 to 7: after an edit of a sketch, each extrude of it follows. The rebuild
// runs inside the handler of the edit, before the commit, so a replay of the log gives the
// same solids under the same keys. ADR-0039: a consumed extrude follows through its chain of
// Translate and Subtract to the live body at its end. ADR-0049: an edit of the distance of an
// extrude takes the same path, and each rebuild reads the distance in effect. ADR-0051 item 6:
// each datum in effect resolves again from the state after the edit, and each extrude of a
// sketch on a datum that moves follows, as an extrude of the edited sketch does. ADR-0052 item 8:
// each reference of a sketch resolves again too, and each extrude of a sketch whose reference moves
// follows its solved record. ADR-0055 item 1: the two edits give their inputs to one regeneration
// (Features/Regeneration.cs), which evaluates the features again.
public static class SketchRebuild
{
    // ADR-0037 item 4: the identifier of each command of the log whose step is in effect. The
    // walk does what StepHistory does: an Undo moves the last step in effect to the undone
    // list, a Redo moves it back, and each other entry is a new step that clears the undone
    // list. It depends only on the log. ADR-0054 item 4: the engine reads FeatureList.InEffect; this walk is the
    // reference that the tests compare with the fold.
    public static HashSet<Guid> InEffect(IReadOnlyList<LogEntry> log)
    {
        var done = new List<Guid>();
        var undone = new List<Guid>();
        foreach (var entry in log)
        {
            switch (entry.Command)
            {
                case UndoCommand when done.Count > 0:
                    undone.Add(done[^1]);
                    done.RemoveAt(done.Count - 1);
                    break;
                case RedoCommand when undone.Count > 0:
                    done.Add(undone[^1]);
                    undone.RemoveAt(undone.Count - 1);
                    break;
                case UndoCommand or RedoCommand:
                    break;
                default:
                    done.Add(entry.Command.CommandId);
                    undone.Clear();
                    break;
            }
        }

        return [.. done];
    }

    // ADR-0049 item 2: the Extrude in effect with the identifier, live or consumed, or null.
    public static ExtrudeCommand? ExtrudeInEffect(Document document, Guid extrudeId) => FeatureList.Of(document).Extrude(extrudeId);

    // ADR-0049 items 3 and 7: the distance in effect of the Extrude in effect with the identifier,
    // or null when no Extrude in effect has it.
    public static double? DistanceInEffect(Document document, Guid extrudeId) => FeatureList.Of(document).DistanceInEffect(extrudeId);

    // The new record of each live extrude of the sketch that the edit changes, and the
    // diagnostics of the rebuild. Before is the record of the sketch in the Document, and
    // after is the solved record that the edit gives. Each extrude takes its distance in effect
    // (ADR-0049 item 3).
    public static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<SketchRecord> Sketches, IReadOnlyList<Diagnostic> Diagnostics) Rebuild(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord before, SketchRecord after)
    {
        var distance = FeatureList.Of(document).Distance;
        return Run(document, backend, new Regeneration.Edit(
            edit,
            extrude => extrude.SketchId == after.Id,
            extrude => new(after.Id, before, distance(extrude)),
            extrude => new(after.Id, after, distance(extrude)),
            extrude => new(extrude.SketchId, document.Sketches.FirstOrDefault(sketch => sketch.Id == extrude.SketchId), distance(extrude)),
            id => id == after.Id ? after : SketchModel.Find(document, id),
            DiagnosticCodes.SketchRebuildFailed,
            DiagnosticCodes.SketchExtrudeConsumed));
    }

    // ADR-0049 items 4 and 5: the new record of the extrude, or of the end of its chain, after a
    // new distance. The sketch is its live record.
    public static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<SketchRecord> Sketches, IReadOnlyList<Diagnostic> Diagnostics) RebuildDistance(
        Document document, IGeometryBackend backend, Guid edit, ExtrudeCommand target, double next)
    {
        var distance = FeatureList.Of(document).Distance;
        Regeneration.ExtrudeInput Live(ExtrudeCommand extrude, double value)
            => new(extrude.SketchId, document.Sketches.FirstOrDefault(sketch => sketch.Id == extrude.SketchId), value);
        return Run(document, backend, new Regeneration.Edit(
            edit,
            extrude => extrude.CommandId == target.CommandId,
            extrude => Live(extrude, distance(extrude)),
            extrude => Live(extrude, next),
            extrude => Live(extrude, distance(extrude)),
            id => SketchModel.Find(document, id),
            DiagnosticCodes.FeatureRebuildFailed,
            DiagnosticCodes.FeatureRebuildFailed));
    }

    private static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<SketchRecord> Sketches, IReadOnlyList<Diagnostic> Diagnostics) Run(
        Document document, IGeometryBackend backend, Regeneration.Edit edit)
    {
        var outcome = Regeneration.Run(document, backend, edit);
        return (outcome.Bodies, outcome.Datums, outcome.Sketches, outcome.Diagnostics);
    }
}
