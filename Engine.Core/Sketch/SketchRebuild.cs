using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0037 items 4 to 7: after an edit of a sketch, each extrude of it follows. The rebuild
// runs inside the handler of the edit, before the commit, so a replay of the log gives the
// same solids under the same keys. ADR-0039: a consumed extrude follows through its chain of
// Translate and Subtract to the live body at its end. ADR-0049: an edit of the distance of an
// extrude takes the same path, and each rebuild reads the distance in effect. ADR-0051 item 6:
// each datum in effect resolves again from the state after the edit, and each extrude of a
// sketch on a datum that moves follows, as an extrude of the edited sketch does.
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

    // ADR-0049 item 2: the Extrude in effect with the identifier, live or consumed, or null.
    public static ExtrudeCommand? ExtrudeInEffect(IReadOnlyList<LogEntry> log, Guid extrudeId)
        => InEffect(log).Contains(extrudeId)
            ? log.Select(entry => entry.Command).OfType<ExtrudeCommand>().FirstOrDefault(extrude => extrude.CommandId == extrudeId)
            : null;

    // ADR-0049 items 3 and 7: the distance in effect of the Extrude in effect with the identifier,
    // or null when no Extrude in effect has it.
    public static double? DistanceInEffect(IReadOnlyList<LogEntry> log, Guid extrudeId)
        => ExtrudeInEffect(log, extrudeId) is { } extrude ? Distances(log, InEffect(log))(extrude) : null;

    // ADR-0049 item 3: the distance of the last SetExtrudeDistance in effect for an extrude, in
    // the order of the log, or else the distance of the Extrude. The dictionary is a lookup only,
    // so no order comes from it.
    private static Func<ExtrudeCommand, double> Distances(IReadOnlyList<LogEntry> log, HashSet<Guid> inEffect)
    {
        var last = new Dictionary<Guid, double>();
        foreach (var entry in log)
        {
            if (entry.Command is SetExtrudeDistanceCommand set && inEffect.Contains(set.CommandId))
                last[set.ExtrudeId] = set.Distance;
        }

        return extrude => last.TryGetValue(extrude.CommandId, out var distance) ? distance : extrude.Distance;
    }

    // The sketch and the distance that give the parameters of an extrude. A null sketch is not live.
    private readonly record struct ExtrudeInput(Guid SketchId, SketchRecord? Sketch, double Distance);

    // What an edit changes: the extrudes that it targets, the input of each one before and after
    // the edit, and the codes of its warnings. An edit of a sketch targets each extrude of the
    // sketch (ADR-0037 item 5), and an edit of a distance targets one extrude (ADR-0049 item 4).
    private sealed record Edit(
        Guid Id,
        Func<ExtrudeCommand, bool> Targets,
        Func<ExtrudeCommand, ExtrudeInput> Before,
        Func<ExtrudeCommand, ExtrudeInput> After,
        Func<ExtrudeCommand, ExtrudeInput> Other,
        string FailedCode,
        string ConsumedCode);

    // The parameters of an extrude in one state, with the frame of the datum of its sketch in that
    // state (ADR-0051 item 5).
    private static (ExtrudeParameters? Parameters, string? Reason) Parameters(ExtrudeInput input, Func<Guid, DatumFrame?> frames)
        => input.Sketch is not { } sketch
            ? (null, $"The sketch '{input.SketchId}' is not live.")
            : frames(sketch.DatumId) is { } frame
                ? ExtrudeProfile.Parameters(sketch, input.Distance, frame)
                : (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' does not exist.");

    // The new record of each live extrude of the sketch that the edit changes, and the
    // diagnostics of the rebuild. Before is the record of the sketch in the Document, and
    // after is the solved record that the edit gives. Each extrude takes its distance in effect
    // (ADR-0049 item 3).
    public static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<Diagnostic> Diagnostics) Rebuild(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord before, SketchRecord after)
    {
        var inEffect = InEffect(document.Log);
        var distance = Distances(document.Log, inEffect);
        return Run(document, backend, inEffect, new Edit(
            edit,
            extrude => extrude.SketchId == after.Id,
            extrude => new(after.Id, before, distance(extrude)),
            extrude => new(after.Id, after, distance(extrude)),
            extrude => new(extrude.SketchId, document.Sketches.FirstOrDefault(sketch => sketch.Id == extrude.SketchId), distance(extrude)),
            DiagnosticCodes.SketchRebuildFailed,
            DiagnosticCodes.SketchExtrudeConsumed));
    }

    // ADR-0049 items 4 and 5: the new record of the extrude, or of the end of its chain, after a
    // new distance. The sketch is its live record.
    public static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<Diagnostic> Diagnostics) RebuildDistance(
        Document document, IGeometryBackend backend, Guid edit, ExtrudeCommand target, double next)
    {
        var inEffect = InEffect(document.Log);
        var distance = Distances(document.Log, inEffect);
        ExtrudeInput Live(ExtrudeCommand extrude, double value)
            => new(extrude.SketchId, document.Sketches.FirstOrDefault(sketch => sketch.Id == extrude.SketchId), value);
        return Run(document, backend, inEffect, new Edit(
            edit,
            extrude => extrude.CommandId == target.CommandId,
            extrude => Live(extrude, distance(extrude)),
            extrude => Live(extrude, next),
            extrude => Live(extrude, distance(extrude)),
            DiagnosticCodes.FeatureRebuildFailed,
            DiagnosticCodes.FeatureRebuildFailed));
    }

    private static (IReadOnlyList<BodyRecord> Changed, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<Diagnostic> Diagnostics) Run(
        Document document, IGeometryBackend backend, HashSet<Guid> inEffect, Edit first)
    {
        var changed = new List<BodyRecord>();
        var graph = FeatureGraph.Of(document.Log);

        // ADR-0051 item 6: the datums after the edit, in the order of the log. An extrude of a sketch
        // on a datum whose frame moves is a target of the edit, with the frame of each state.
        var (datums, after, datumWarnings) = DatumResolver.Rebuild(document, graph, extrude =>
        {
            var input = first.Targets(extrude) ? first.After(extrude) : first.Other(extrude);
            return new DatumResolver.ExtrudeState(input.Sketch, input.Distance);
        });
        Func<Guid, DatumFrame?> before = id => DatumResolver.Frame(document, id);
        var moved = datums.Where(datum => datum.Frame != before(datum.Id)).Select(datum => datum.Id).ToHashSet();
        bool OnMoved(ExtrudeCommand extrude) => first.Other(extrude).Sketch is { } sketch && moved.Contains(sketch.DatumId);
        var edit = first with
        {
            Targets = extrude => first.Targets(extrude) || OnMoved(extrude),
            Before = extrude => first.Targets(extrude) ? first.Before(extrude) : first.Other(extrude),
            After = extrude => first.Targets(extrude) ? first.After(extrude) : first.Other(extrude),
        };
        var diagnostics = new List<Diagnostic>(datumWarnings);
        var seeds = new List<ExtrudeCommand>();

        // The order of the log, never the order of Document.Bodies (ADR-0037 item 5).
        foreach (var extrude in document.Log.Select(entry => entry.Command).OfType<ExtrudeCommand>())
        {
            if (!edit.Targets(extrude) || !inEffect.Contains(extrude.CommandId))
                continue;

            // ADR-0039 item 2: an extrude in effect whose body is not live was consumed by a later
            // command in effect, so it is a seed of a chain.
            if (document.Bodies.FirstOrDefault(body => body.Handle.Id == extrude.CommandId) is not { } body)
            {
                seeds.Add(extrude);
                continue;
            }

            var (parameters, reason) = Parameters(edit.After(extrude), after);
            if (parameters is not { } next)
            {
                Fail(body, reason!);
                continue;
            }

            // ADR-0037 item 5: an edit that does not change the solid changes nothing.
            if (body.Fault is null && Parameters(edit.Before(extrude), before).Parameters is { } previous
                && ExtrudeProfile.Same(previous, next))
                continue;

            if (backend.TryGet<IExtrudeOps>() is not { } ops)
            {
                Fail(body, "The active backend does not implement IExtrudeOps, so the solid cannot follow the edit.");
                continue;
            }

            var key = GeometryKeys.Derive(edit.Id, body.Handle.Id);
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
        return (changed, datums, diagnostics);

        // ADR-0037 item 6 (Q2 A) and ADR-0049 item 5: the body keeps its last solid and gets the
        // fault, and the result gives the same code. A fault that does not change gives no new record.
        void Fail(BodyRecord body, string reason)
        {
            var fault = new BodyFault(edit.FailedCode, $"The extrude '{body.Handle.Id}' keeps its last solid: {reason}");
            diagnostics.Add(new Diagnostic(
                Severity.Warning, fault.Code, fault.Message,
                Data: new Dictionary<string, object?> { ["bodyId"] = body.Handle.Id }));
            if (body.Fault != fault)
                changed.Add(body with { Fault = fault });
        }
    }

    // ADR-0039 items 2 to 7: the chain of each consumed extrude that the edit targets, from the
    // seeds to the live ends.
    private sealed class Chain(
        Document document, IGeometryBackend backend, Edit edit, Func<Guid, DatumFrame?> before, Func<Guid, DatumFrame?> after,
        List<BodyRecord> changed, List<Diagnostic> diagnostics)
    {
        private readonly FeatureGraph _graph = FeatureGraph.Of(document.Log);

        public void Rebuild(IReadOnlyList<ExtrudeCommand> seeds)
        {
            var ends = new List<BodyRecord>();
            foreach (var seed in seeds)
            {
                if (End(seed) is not { } end)
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
        // consumer that the graph does not know gives W-SKETCH-EXTRUDE-CONSUMED (ADR-0039 item 7),
        // or W-FEATURE-REBUILD-FAILED for an edit of a distance (ADR-0049 item 5).
        private BodyRecord? End(ExtrudeCommand seed)
        {
            var handle = seed.CommandId;
            while (true)
            {
                if (document.Bodies.FirstOrDefault(body => body.Handle.Id == handle) is { } live)
                    return live;
                if (_graph.Consumer(handle) is not { } consumer || !FeatureGraph.Creates(consumer))
                {
                    diagnostics.Add(new Diagnostic(
                        Severity.Warning,
                        edit.ConsumedCode,
                        $"The extrude '{seed.CommandId}' of the sketch '{seed.SketchId}' was consumed by a command that the feature graph does not know, so its solid does not follow the edit.",
                        Data: new Dictionary<string, object?> { ["bodyId"] = seed.CommandId }));
                    return null;
                }
                handle = consumer.CommandId;
            }
        }

        // True when an extrude that the edit targets upstream of the handle gets other parameters.
        private bool ProfileChanges(Guid handle)
        {
            switch (_graph.Creator(handle))
            {
                case ExtrudeCommand extrude when edit.Targets(extrude):
                    var previous = Parameters(edit.Before(extrude), before).Parameters;
                    var next = Parameters(edit.After(extrude), after).Parameters;
                    return previous is not { } p || next is not { } n || !ExtrudeProfile.Same(p, n);
                // An extrude that the edit does not target does not change with this edit.
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
            var key = GeometryKeys.Derive(edit.Id, handle);
            switch (_graph.Creator(handle))
            {
                case CreateBoxCommand:
                    return (handle, false, null);

                case ExtrudeCommand extrude:
                {
                    var input = edit.Targets(extrude) ? edit.After(extrude) : edit.Other(extrude);
                    if (input.Sketch is null)
                        return (handle, false, $"the sketch '{extrude.SketchId}' of the extrude '{handle}' is not live");
                    var (parameters, reason) = Parameters(input, after);
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

        // ADR-0039 item 5 (Q2 A) and ADR-0049 item 5: the end keeps its last solid and gets the
        // fault, and the result gives the same code. A fault that does not change gives no new record.
        private void Fail(BodyRecord end, string reason)
        {
            var fault = new BodyFault(edit.FailedCode, $"The body '{end.Handle.Id}' keeps its last solid: {reason}");
            diagnostics.Add(new Diagnostic(
                Severity.Warning, fault.Code, fault.Message,
                Data: new Dictionary<string, object?> { ["bodyId"] = end.Handle.Id }));
            if (end.Fault != fault)
                changed.Add(end with { Fault = fault });
        }
    }
}
