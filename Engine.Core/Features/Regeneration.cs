using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Sketch;

namespace Engine.Core.Features;

// ADR-0055 (phase P8d2, TASK-0174): the regeneration, the third of the three layers of P8d. After an edit, it evaluates
// the steps in effect of the ordered feature list (ADR-0054) again from the state after the edit, and each step whose
// inputs did not change keeps its solid. It runs inside the handler of the edit, before the commit, so a replay of the
// log gives the same records under the same keys (ADR-0037 item 4). It makes two walks of the steps in effect, in the
// order of the log:
//
// 1. The planes. Each datum and each reference of a sketch resolves again (DatumResolver.Rebuild, ADR-0051 item 6,
//    ADR-0052 item 8). They read the parameters of the features only, never a solid.
// 2. The solids. Each live body whose chain holds an extrude that the edit changes is evaluated again, and each step of
//    the chain whose inputs did not change keeps its solid (ADR-0037 item 5, ADR-0039).
//
// The planes come first because a reference later in the log can change a sketch that an earlier extrude reads. The
// records of the Document hold each result and each fault: they are a projection of the log, the cache of charter
// rule 4 (ADR-0055 item 3).
internal static class Regeneration
{
    // The sketch and the distance that give the parameters of an extrude. A null sketch is not live.
    public readonly record struct ExtrudeInput(Guid SketchId, SketchRecord? Sketch, double Distance);

    // What an edit changes: the extrudes that it targets, the input of each one before and after the edit, the input of
    // each other extrude, the record of each sketch after the edit, and the codes of its warnings. An edit of a sketch
    // targets each extrude of the sketch (ADR-0037 item 5), and an edit of a distance targets one extrude (ADR-0049
    // item 4).
    public sealed record Edit(
        Guid Id,
        Func<ExtrudeCommand, bool> Targets,
        Func<ExtrudeCommand, ExtrudeInput> Before,
        Func<ExtrudeCommand, ExtrudeInput> After,
        Func<ExtrudeCommand, ExtrudeInput> Other,
        Func<Guid, SketchRecord?> SketchAfter,
        string FailedCode,
        string ConsumedCode)
    {
        // Finding E126 (Q3 A of the codebase review of 2026-11-03): the datum whose frame moved, for an extrude that
        // the edit targets only because its sketch lies on that datum, or null.
        public Func<ExtrudeCommand, Guid?> Through { get; init; } = _ => null;
    }

    // The new record of each body, datum and sketch that the regeneration changes, and its warnings.
    public sealed record Outcome(
        IReadOnlyList<BodyRecord> Bodies, IReadOnlyList<DatumRecord> Datums, IReadOnlyList<SketchRecord> Sketches, IReadOnlyList<Diagnostic> Diagnostics);

    // ADR-0055 item 1: the two walks after the edit.
    public static Outcome Run(Document document, IGeometryBackend backend, Edit first)
    {
        var list = FeatureList.Of(document);
        var graph = FeatureGraph.Of(document);

        // ADR-0051 item 6 and ADR-0052 item 8: the datums and the references after the edit. An extrude of a sketch on a
        // datum whose frame moves, or of a sketch whose reference moves, is a target of the edit, with the frame and the
        // record of each state.
        ExtrudeInput Next(ExtrudeCommand extrude) => first.Targets(extrude) ? first.After(extrude) : first.Other(extrude);
        var planes = DatumResolver.Rebuild(document, graph, first.SketchAfter, extrude => Next(extrude).Distance);
        Func<Guid, DatumFrame?> before = id => DatumResolver.Frame(document, id);
        var moved = planes.Datums.Where(datum => datum.Frame != before(datum.Id)).Select(datum => datum.Id).ToHashSet();
        var resketched = planes.Sketches.Select(sketch => sketch.Id).ToHashSet();
        bool Follows(ExtrudeCommand extrude)
            => resketched.Contains(extrude.SketchId) || (first.Other(extrude).Sketch is { } sketch && moved.Contains(sketch.DatumId));
        var edit = first with
        {
            Targets = extrude => first.Targets(extrude) || Follows(extrude),
            Before = extrude => first.Targets(extrude) ? first.Before(extrude) : first.Other(extrude),
            After = extrude => resketched.Contains(extrude.SketchId) ? Next(extrude) with { Sketch = planes.SketchOf(extrude.SketchId) } : Next(extrude),
            Through = extrude => !first.Targets(extrude) && first.Other(extrude).Sketch is { } sketch && moved.Contains(sketch.DatumId) ? sketch.DatumId : null,
        };

        var solids = new Solids(document, backend, list, edit, before, planes.FrameOf);
        solids.Walk();

        // ADR-0059 item 3 (phase K4c, TASK-0185): after the solids, a datum on a face that a cut removed gets a fault. The
        // rebuild of the planes gives each datum that resolves no fault, so each datum in effect has the check again. The
        // names of an end that the edit changes come from the state after the edit; the names of an end that it does not
        // change come from the kept solid of its committed key.
        DatumResolver.ExtrudeState StateAfter(ExtrudeCommand extrude)
        {
            var input = edit.Targets(extrude) ? edit.After(extrude) : edit.Other(extrude);
            return new DatumResolver.ExtrudeState(input.Sketch, input.Distance);
        }
        var names = new Dictionary<Guid, IReadOnlySet<Kernel.FaceTag>?>();
        IReadOnlySet<Kernel.FaceTag>? TagsOf(Guid end)
        {
            if (names.TryGetValue(end, out var known))
                return known;
            Kernel.KernelSolid? solid = null;
            if (solids.Changed.FirstOrDefault(body => body.Handle.Id == end) is { } rebuilt)
                solid = rebuilt.Fault is null ? KernelProjection.Of(graph, end, StateAfter, planes.FrameOf).Solid : null;
            else if (document.Bodies.FirstOrDefault(body => body.Handle.Id == end) is { } live)
                solid = KernelProjection.OfRecord(document, live).Solid;
            return names[end] = solid is null ? null : KernelProjection.Tags(solid);
        }

        var rebuiltDatums = planes.Datums.ToDictionary(datum => datum.Id);
        // The order of the log, never the order of a collection of the Document.
        var inEffect = document.Datums.OrderBy(datum => graph.Order(datum.Id)).Select(datum => rebuiltDatums.GetValueOrDefault(datum.Id, datum)).ToList();
        var (checkedDatums, gone) = FaceGone.Check(inEffect, id => document.Datums.FirstOrDefault(datum => datum.Id == id), graph, TagsOf);
        var datums = checkedDatums.Where(datum => !document.Datums.Contains(datum)).ToList();
        return new Outcome(solids.Changed, datums, planes.Sketches, [.. planes.Diagnostics, .. solids.Diagnostics, .. gone]);
    }

    // The parameters of an extrude in one state, with the frame of the datum of its sketch in that state (ADR-0051
    // item 5).
    private static (ExtrudeParameters? Parameters, string? Reason) Parameters(ExtrudeInput input, Func<Guid, DatumFrame?> frames)
        => input.Sketch is not { } sketch
            ? (null, $"The sketch '{input.SketchId}' is not live.")
            : frames(sketch.DatumId) is { } frame
                ? ExtrudeProfile.Parameters(sketch, input.Distance, frame)
                : (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' does not exist.");

    // The data of a warning of the regeneration: the body, and the datum through which it follows the edit (finding
    // E126).
    private static Dictionary<string, object?> Data(Guid body, Guid? through)
        => through is { } datum
            ? new Dictionary<string, object?> { ["bodyId"] = body, ["datumId"] = datum }
            : new Dictionary<string, object?> { ["bodyId"] = body };

    // ADR-0055 item 1, walk 2: the solids. A live extrude is the end of its own chain; a consumed extrude follows its
    // chain of Translate and Subtract to the live body at its end (ADR-0039 items 2 to 7).
    private sealed class Solids(
        Document document, IGeometryBackend backend, FeatureList list, Edit edit, Func<Guid, DatumFrame?> before, Func<Guid, DatumFrame?> after)
    {
        public List<BodyRecord> Changed { get; } = [];

        public List<Diagnostic> Diagnostics { get; } = [];

        public void Walk()
        {
            // The order of the log, never the order of Document.Bodies (ADR-0037 item 5) or of a set.
            var ends = new List<BodyRecord>();
            foreach (var extrude in list.Steps.OfType<ExtrudeCommand>())
            {
                if (!edit.Targets(extrude) || End(extrude) is not { } end)
                    continue;
                if (!ends.Any(known => known.Handle.Id == end.Handle.Id))
                    ends.Add(end);
            }

            // ADR-0055 item 2: the ends in the log order of the command that creates each one.
            foreach (var end in ends.OrderBy(body => list.Order(body.Handle.Id)))
            {
                // ADR-0037 item 5 and ADR-0039 item 4: an edit that changes no extrude upstream of the end changes
                // nothing. An end with a fault is evaluated again, so a repair removes the fault.
                if (end.Fault is null && !Changes(end.Handle.Id))
                    continue;

                var (key, stepChanged, failure) = Evaluate(end.Handle.Id);
                if (failure is not null)
                    Fail(end, failure);
                else if (stepChanged)
                    Changed.Add(end with { Geometry = key, Fault = null });
            }
        }

        // The live body at the end of the walk from an extrude through each consumer, or null. A consumer that the list
        // does not know gives W-SKETCH-EXTRUDE-CONSUMED (ADR-0039 item 7), or W-FEATURE-REBUILD-FAILED for an edit of a
        // distance (ADR-0049 item 5).
        private BodyRecord? End(ExtrudeCommand seed)
        {
            var handle = seed.CommandId;
            while (true)
            {
                if (document.Bodies.FirstOrDefault(body => body.Handle.Id == handle) is { } live)
                    return live;
                if (list.Consumer(handle) is not { } consumer || !FeatureList.Creates(consumer))
                {
                    Diagnostics.Add(new Diagnostic(
                        Severity.Warning,
                        edit.ConsumedCode,
                        $"The extrude '{seed.CommandId}' of the sketch '{seed.SketchId}' was consumed by a command that the feature graph does not know, so its solid does not follow the edit.",
                        Data: new Dictionary<string, object?> { ["bodyId"] = seed.CommandId }));
                    return null;
                }
                handle = consumer.CommandId;
            }
        }

        // ADR-0055 item 2: true when the inputs of the step that creates the handle change with this edit. An extrude
        // that the edit targets changes when its parameters change; a Translate and a Subtract change when an operand
        // changes; no edit changes a box or a cylinder (ADR-0076).
        private bool Changes(Guid handle)
        {
            switch (list.Creator(handle))
            {
                case ExtrudeCommand extrude when edit.Targets(extrude):
                    var previous = Parameters(edit.Before(extrude), before).Parameters;
                    var next = Parameters(edit.After(extrude), after).Parameters;
                    return previous is not { } p || next is not { } n || !ExtrudeProfile.Same(p, n);
                case ExtrudeCommand or CreateBoxCommand or CreateCylinderCommand or ImportStlCommand:
                    return false;
                case { } command:
                    return FeatureList.Operands(command).Any(Changes);
                default:
                    return true;
            }
        }

        // Finding E126: the first datum, in the order of the operands, through which an extrude upstream of the handle is
        // a target of the edit, or null.
        private Guid? Through(Guid handle) => list.Creator(handle) switch
        {
            ExtrudeCommand extrude => edit.Through(extrude),
            CreateBoxCommand or CreateCylinderCommand or ImportStlCommand or null => null,
            { } command => FeatureList.Operands(command).Select(Through).FirstOrDefault(id => id is not null),
        };

        // ADR-0039 item 3: the key of the solid of a handle for this edit, with true when the step changed, or the reason
        // of a failure. The record of a consumed body is not in the Document, so each extrude upstream of an end that the
        // edit changes is extruded again. Each handle has one consumer in effect (ADR-0021 item 1), so the steps upstream
        // of one end are a tree and each runs one time; two extrudes that meet in one Subtract give one end, which Walk
        // takes one time.
        private (Guid Key, bool Changed, string? Failure) Evaluate(Guid handle)
        {
            var key = GeometryKeys.Derive(edit.Id, handle);
            switch (list.Creator(handle))
            {
                // ADR-0083 item 1 (TASK-0258): no edit changes a mesh body, as no edit changes a box.
                case CreateBoxCommand or CreateCylinderCommand or ImportStlCommand:
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
                    return Call(handle, key, () => ops.Extrude(new BodyHandle(key), next));
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
                    return Call(handle, key, () => ops.Translate(new BodyHandle(key), new BodyHandle(source), translate.Dx, translate.Dy, translate.Dz));
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
                    return Call(handle, key, () => ops.Subtract(new BodyHandle(key), new BodyHandle(minuend), new BodyHandle(subtrahend)));
                }

                case { } other:
                    return (handle, false, $"the command '{other.Name}' that creates the body '{handle}' is not a step that the feature graph knows");
                default:
                    return (handle, false, $"no command in effect creates the body '{handle}'");
            }
        }

        // One call of the backend for one step; an exception is the failure of the step.
        private static (Guid Key, bool Changed, string? Failure) Call(Guid handle, Guid key, Action call)
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

        // ADR-0037 item 6 (Q2 A), ADR-0039 item 5 (Q2 A) and ADR-0049 item 5: the end keeps its last solid and gets the
        // fault, and the result gives the same code. A fault that does not change gives no new record.
        private void Fail(BodyRecord end, string reason)
        {
            var fault = new BodyFault(edit.FailedCode, $"The body '{end.Handle.Id}' keeps its last solid: {reason}");
            Diagnostics.Add(new Diagnostic(
                Severity.Warning, fault.Code, fault.Message,
                Data: Data(end.Handle.Id, Through(end.Handle.Id))));
            if (end.Fault != fault)
                Changed.Add(end with { Fault = fault });
        }
    }
}
