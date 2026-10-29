using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0037 items 4 to 7: after an edit of a sketch, each extrude of it follows. The rebuild
// runs inside the handler of the edit, before the commit, so a replay of the log gives the
// same solids under the same keys.
public static class SketchRebuild
{
    // ADR-0037 item 4: the identifier of each command of the log whose step is in effect. The
    // walk does what StepHistory does: an Undo moves the last step in effect to the undone
    // list, a Redo moves it back, and each other entry is a new step that clears the undone
    // list. It depends only on the log.
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

    // The new record of each live extrude of the sketch that the edit changes, and the
    // diagnostics of the rebuild. Before is the record of the sketch in the Document, and
    // after is the solved record that the edit gives.
    public static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<Diagnostic> Diagnostics) Rebuild(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord before, SketchRecord after)
    {
        var changed = new List<BodyRecord>();
        var diagnostics = new List<Diagnostic>();
        var inEffect = InEffect(document.Log);

        // The order of the log, never the order of Document.Bodies (ADR-0037 item 5).
        foreach (var extrude in document.Log.Select(entry => entry.Command).OfType<ExtrudeCommand>())
        {
            if (extrude.SketchId != after.Id || !inEffect.Contains(extrude.CommandId))
                continue;

            // ADR-0037 item 7 (Q3 A): an extrude in effect whose body is not live was consumed
            // by a later command in effect. It does not follow until phase K0.
            if (document.Bodies.FirstOrDefault(body => body.Handle.Id == extrude.CommandId) is not { } body)
            {
                diagnostics.Add(new Diagnostic(
                    Severity.Warning,
                    DiagnosticCodes.SketchExtrudeConsumed,
                    $"The extrude '{extrude.CommandId}' of the sketch '{after.Id}' was consumed by a later command, so its solid does not follow the edit.",
                    Data: new Dictionary<string, object?> { ["bodyId"] = extrude.CommandId }));
                continue;
            }

            var (parameters, reason) = ExtrudeProfile.Parameters(after, extrude.Distance);
            if (parameters is not { } next)
            {
                Fail(body, reason!);
                continue;
            }

            // ADR-0037 item 5: an edit that does not change the solid changes nothing.
            if (body.Fault is null && ExtrudeProfile.Parameters(before, extrude.Distance).Parameters is { } previous
                && ExtrudeProfile.Same(previous, next))
                continue;

            if (backend.TryGet<IExtrudeOps>() is not { } ops)
            {
                Fail(body, "The active backend does not implement IExtrudeOps, so the solid cannot follow the edit.");
                continue;
            }

            var key = GeometryKeys.Derive(edit, body.Handle.Id);
            try
            {
                ops.Extrude(new BodyHandle(key), next);
            }
            catch (Exception ex)
            {
                Fail(body, $"The geometry backend failed to extrude the new profile: {ex.Message}");
                continue;
            }

            changed.Add(body with { Geometry = key, Fault = null });
        }

        return (changed, diagnostics);

        // ADR-0037 item 6 (Q2 A): the body keeps its last solid and gets the fault, and the
        // result gives the same code. A fault that does not change gives no new record.
        void Fail(BodyRecord body, string reason)
        {
            var fault = new BodyFault(DiagnosticCodes.SketchRebuildFailed, $"The extrude '{body.Handle.Id}' keeps its last solid: {reason}");
            diagnostics.Add(new Diagnostic(
                Severity.Warning, fault.Code, fault.Message,
                Data: new Dictionary<string, object?> { ["bodyId"] = body.Handle.Id }));
            if (body.Fault != fault)
                changed.Add(body with { Fault = fault });
        }
    }
}
