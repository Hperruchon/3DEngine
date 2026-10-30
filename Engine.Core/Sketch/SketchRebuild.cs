using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0037 items 4 to 7: after an edit of a sketch, each extrude of it follows. The rebuild
// runs inside the handler of the edit, before the commit, so a replay of the log gives the
// same solids under the same keys. ADR-0039: a consumed extrude follows through its chain of
// Translate and Subtract to the live body at its end.
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
        var seeds = new List<ExtrudeCommand>();

        // The order of the log, never the order of Document.Bodies (ADR-0037 item 5).
        foreach (var extrude in document.Log.Select(entry => entry.Command).OfType<ExtrudeCommand>())
        {
            if (extrude.SketchId != after.Id || !inEffect.Contains(extrude.CommandId))
                continue;

            // ADR-0039 item 2: an extrude in effect whose body is not live was consumed by a later
            // command in effect, so it is a seed of a chain.
            if (document.Bodies.FirstOrDefault(body => body.Handle.Id == extrude.CommandId) is not { } body)
            {
                seeds.Add(extrude);
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

        if (seeds.Count > 0)
            new Chain(document, backend, edit, before, after, changed, diagnostics).Rebuild(seeds);
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

    // ADR-0039 items 2 to 7: the chain of each consumed extrude of the edited sketch, from the
    // seeds to the live ends.
    private sealed class Chain(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord before, SketchRecord after,
        List<BodyRecord> changed, List<Diagnostic> diagnostics)
    {
        private readonly FeatureGraph _graph = FeatureGraph.Of(document.Log);

        public void Rebuild(IReadOnlyList<ExtrudeCommand> seeds)
        {
            var ends = new List<BodyRecord>();
            foreach (var seed in seeds)
            {
                if (End(seed.CommandId) is not { } end)
                    continue;
                if (!ends.Any(known => known.Handle.Id == end.Handle.Id))
                    ends.Add(end);
            }

            // The log order of the command that creates each end, never the order of a set.
            foreach (var end in ends.OrderBy(body => _graph.Order(body.Handle.Id)))
            {
                // ADR-0039 item 4: an edit that changes no extrude upstream of the end changes nothing.
                if (end.Fault is null && !ProfileChanges(end.Handle.Id))
                    continue;

                var (key, stepChanged, failure) = Evaluate(end.Handle.Id);
                if (failure is not null)
                    Fail(end, failure);
                else if (stepChanged)
                    changed.Add(end with { Geometry = key, Fault = null });
            }
        }

        // The live body at the end of the walk from a seed through each consumer, or null. A
        // consumer that the graph does not know gives W-SKETCH-EXTRUDE-CONSUMED (ADR-0039 item 7).
        private BodyRecord? End(Guid seed)
        {
            var handle = seed;
            while (true)
            {
                if (document.Bodies.FirstOrDefault(body => body.Handle.Id == handle) is { } live)
                    return live;
                if (_graph.Consumer(handle) is not { } consumer || !FeatureGraph.Creates(consumer))
                {
                    diagnostics.Add(new Diagnostic(
                        Severity.Warning,
                        DiagnosticCodes.SketchExtrudeConsumed,
                        $"The extrude '{seed}' of the sketch '{after.Id}' was consumed by a command that the feature graph does not know, so its solid does not follow the edit.",
                        Data: new Dictionary<string, object?> { ["bodyId"] = seed }));
                    return null;
                }
                handle = consumer.CommandId;
            }
        }

        // True when an extrude of the edited sketch upstream of the handle gets other parameters.
        private bool ProfileChanges(Guid handle)
        {
            switch (_graph.Creator(handle))
            {
                case ExtrudeCommand extrude when extrude.SketchId == after.Id:
                    var previous = ExtrudeProfile.Parameters(before, extrude.Distance).Parameters;
                    var next = ExtrudeProfile.Parameters(after, extrude.Distance).Parameters;
                    return previous is not { } p || next is not { } n || !ExtrudeProfile.Same(p, n);
                // An extrude of another sketch does not change with this edit.
                case ExtrudeCommand:
                    return false;
                case { } command:
                    return FeatureGraph.Operands(command).Any(ProfileChanges);
                default:
                    return true;
            }
        }

        // ADR-0039 item 3: the key of the solid of a handle for this edit, with true when the step
        // changed, or the reason of a failure. Each handle has one consumer in effect (ADR-0021
        // item 1), so the steps upstream of one end are a tree and each runs one time; two seeds
        // that meet in one Subtract give one end, which Rebuild takes one time.
        private (Guid Key, bool Changed, string? Failure) Evaluate(Guid handle)
        {
            var key = GeometryKeys.Derive(edit, handle);
            switch (_graph.Creator(handle))
            {
                case CreateBoxCommand:
                    return (handle, false, null);

                case ExtrudeCommand extrude:
                {
                    var sketch = extrude.SketchId == after.Id ? after : document.Sketches.FirstOrDefault(s => s.Id == extrude.SketchId);
                    if (sketch is null)
                        return (handle, false, $"the sketch '{extrude.SketchId}' of the extrude '{handle}' is not live");
                    var (parameters, reason) = ExtrudeProfile.Parameters(sketch, extrude.Distance);
                    if (parameters is not { } next)
                        return (handle, false, $"the extrude '{handle}': {reason}");
                    if (backend.TryGet<IExtrudeOps>() is not { } ops)
                        return (handle, false, "the active backend does not implement IExtrudeOps");
                    return Run(handle, key, () => ops.Extrude(new BodyHandle(key), next));
                }

                case TranslateCommand translate:
                {
                    var (source, sourceChanged, failure) = Evaluate(translate.BodyId);
                    if (failure is not null)
                        return (handle, false, failure);
                    if (!sourceChanged)
                        return (handle, false, null);
                    if (backend.TryGet<ITransformOps>() is not { } ops)
                        return (handle, false, "the active backend does not implement ITransformOps");
                    return Run(handle, key, () => ops.Translate(new BodyHandle(key), new BodyHandle(source), translate.Dx, translate.Dy, translate.Dz));
                }

                case SubtractCommand subtract:
                {
                    var (minuend, minuendChanged, minuendFailure) = Evaluate(subtract.MinuendBodyId);
                    var (subtrahend, subtrahendChanged, subtrahendFailure) = Evaluate(subtract.SubtrahendBodyId);
                    if ((minuendFailure ?? subtrahendFailure) is { } failure)
                        return (handle, false, failure);
                    if (!minuendChanged && !subtrahendChanged)
                        return (handle, false, null);
                    if (backend.TryGet<IBooleanOps>() is not { } ops)
                        return (handle, false, "the active backend does not implement IBooleanOps");
                    return Run(handle, key, () => ops.Subtract(new BodyHandle(key), new BodyHandle(minuend), new BodyHandle(subtrahend)));
                }

                case { } other:
                    return (handle, false, $"the command '{other.Name}' that creates the body '{handle}' is not a step that the feature graph knows");
                default:
                    return (handle, false, $"no command in effect creates the body '{handle}'");
            }
        }

        // One call of the backend for one step; an exception is the failure of the step.
        private static (Guid Key, bool Changed, string? Failure) Run(Guid handle, Guid key, Action call)
        {
            try
            {
                call();
                return (key, true, null);
            }
            catch (Exception ex)
            {
                return (handle, false, $"the step '{handle}' failed in the geometry backend: {ex.Message}");
            }
        }

        // ADR-0039 item 5 (Q2 A): the end keeps its last solid and gets the fault, and the result
        // gives the same code. A fault that does not change gives no new record.
        private void Fail(BodyRecord end, string reason)
        {
            var fault = new BodyFault(DiagnosticCodes.SketchRebuildFailed, $"The body '{end.Handle.Id}' keeps its last solid: {reason}");
            diagnostics.Add(new Diagnostic(
                Severity.Warning, fault.Code, fault.Message,
                Data: new Dictionary<string, object?> { ["bodyId"] = end.Handle.Id }));
            if (end.Fault != fault)
                changed.Add(end with { Fault = fault });
        }
    }
}
