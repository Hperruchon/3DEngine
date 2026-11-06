using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Sketch;

// ADR-0059 item 3 (phase K4c, TASK-0185): a datum on a face that a cut removed. The datum stays on the plane of its
// reference (ADR-0051 item 8), and it gets the fault W-DATUM-REFERENCE-FAILED with the reason that the face is gone. Only
// a Subtract removes a whole face, so a datum is checked only when its chain holds a Subtract.
internal static class FaceGone
{
    // The fault of a datum whose face is not a face of the body at the end of its chain.
    public static DatumFault Fault(DatumRecord datum, Guid end)
        => new(DiagnosticCodes.DatumReferenceFailed, Gone(datum) + $"{end}' has its name.");

    // The text of a face-gone fault of the datum up to the body of the end.
    private static string Gone(DatumRecord datum)
        => $"The datum '{datum.Id}' stays on the plane of its reference, and the face '{datum.Reference.Element}' of '{datum.Reference.FeatureId}' is gone: no face of the body '";

    // True when the fault is a face-gone fault of the datum, whatever end it names.
    private static bool IsGone(DatumRecord datum, DatumFault fault)
        => fault.Code == DiagnosticCodes.DatumReferenceFailed && fault.Message.StartsWith(Gone(datum), StringComparison.Ordinal);

    // The warning of a new fault, with the datum in its data, as ADR-0051 item 6 gives it.
    public static Diagnostic Warning(DatumRecord datum, DatumFault fault)
        => new(Severity.Warning, fault.Code, fault.Message, Data: new Dictionary<string, object?> { ["datumId"] = datum.Id });

    // True when the names hold the face of the reference.
    public static bool Holds(IReadOnlySet<FaceTag> tags, DatumReference reference)
        => tags.Contains(new FaceTag(reference.FeatureId, reference.Element, reference.ItemId));

    // The datums after a check: a datum with no fault whose chain holds a Subtract, and whose face is not a face of the
    // body at the end of the chain, gets the fault. TagsOf gives the names of the faces of a live end, or null when the
    // kernel cannot build it; then a datum keeps its face-gone fault from the state before (E139, E148), and another
    // datum has no check. Each datum keeps the order of its list.
    public static (IReadOnlyList<DatumRecord> Datums, IReadOnlyList<Diagnostic> Warnings) Check(
        IReadOnlyList<DatumRecord> datums, Func<Guid, DatumRecord?> before, FeatureGraph graph, Func<Guid, IReadOnlySet<FaceTag>?> tagsOf)
    {
        var result = new List<DatumRecord>();
        var warnings = new List<Diagnostic>();
        foreach (var datum in datums)
        {
            var next = datum;
            var (end, cut) = KernelProjection.End(graph, datum.Reference.FeatureId);
            if (datum.Fault is null && cut && end is { } handle && tagsOf(handle) is { } tags && !Holds(tags, datum.Reference))
            {
                var fault = Fault(datum, handle);
                next = datum with { Fault = fault };
                if (before(datum.Id)?.Fault != fault)
                    warnings.Add(Warning(datum, fault));
            }
            // Finding E139 of the codebase review of 2026-11-05 (b) (TASK-0192): while the kernel cannot build the end, the
            // end keeps the solid of an earlier state, which still has no such face. So the datum keeps the face-gone fault
            // of that end from the state before, with no new warning.
            // Finding E148 of the codebase review of 2026-11-06 (TASK-0197), Q2 A: the fault of a later cut names an earlier
            // end, because a fault that stays keeps its text. The datum keeps its earlier face-gone fault whatever end it names.
            else if (datum.Fault is null && cut && end is { } kept && tagsOf(kept) is null && before(datum.Id)?.Fault is { } earlier
                && IsGone(datum, earlier))
                next = datum with { Fault = earlier };

            result.Add(next);
        }

        return (result, warnings);
    }
}
