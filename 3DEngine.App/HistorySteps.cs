using Engine.Contracts.Sketch;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop;

// ADR-0104 item 2 (phase R16q, TASK-0325): what a tap on a node of the history arc does. A node of a sketch opens the edit
// of that sketch; a node of a live body that is not consumed selects that body; another node does nothing.
internal enum HistoryTap
{
    None,
    EditSketch,
    SelectBody,
}

// ADR-0104 item 1: one step of the selected body. Id is the identifier of its feature, Kind the kind of the feature (the
// name of its command), Text the text beside its node, State the state of the feature (correct, failed, consumed or
// unresolved) and Code the code of a fault, or null.
internal sealed record HistoryStep(Guid Id, string Kind, string Text, string State, string? Code, HistoryTap Tap)
{
    // A failed or an unresolved step says so, as the rows of a body do.
    public bool Fault => State is GetFeaturesQueryHandler.Failed or GetFeaturesQueryHandler.Unresolved;
}

// ADR-0104 item 1 (phase R16q, TASK-0325): the steps that made the selected body, from the result of GetFeatures (ADR-0054),
// with no GPU and no Dear ImGui. The steps are the feature of the body and each feature that it reads through its inputs,
// again and again, in the order of the log, the newest first, and at most six. An origin plane is not a feature, so it
// gives no step; it gives only the plane in the text of a sketch.
internal static class HistorySteps
{
    // The most steps that the arc shows.
    public const int Most = 6;

    public static IReadOnlyList<HistoryStep> Of(FeaturesInfo? features, IReadOnlyList<LiveBody> bodies, IEnumerable<SketchInfo> sketches, Guid? selection)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(sketches);
        if (features is null || selection is not { } selected)
            return [];

        var order = new Dictionary<Guid, int>();
        for (var i = 0; i < features.Features.Count; i++)
            order[features.Features[i].FeatureId] = i;
        if (!order.ContainsKey(selected))
            return [];

        // The chain: the feature of the body, and each feature that a feature of the chain reads.
        var chain = new HashSet<Guid> { selected };
        var open = new Stack<Guid>([selected]);
        while (open.TryPop(out var id))
        {
            foreach (var input in features.Features[order[id]].Inputs)
            {
                if (order.ContainsKey(input) && chain.Add(input))
                    open.Push(input);
            }
        }

        var names = sketches.ToDictionary(sketch => sketch.SketchId, sketch => sketch.Name);
        return [.. chain.OrderByDescending(id => order[id]).Take(Most).Select(id => Step(features.Features[order[id]], bodies, names))];
    }

    private static HistoryStep Step(FeatureInfo feature, IReadOnlyList<LiveBody> bodies, IReadOnlyDictionary<Guid, string?> names)
    {
        var code = feature.Fault?.Code;
        if (feature.Kind == "CreateSketch")
        {
            // The name of the sketch, or "Sketch", and its plane: an origin plane, or "a face" for a datum of the Document.
            var plane = feature.Inputs.Count > 0 && Datums.Find(feature.Inputs[0]) is { } datum ? datum.Name : "a face";
            var name = names.GetValueOrDefault(feature.FeatureId) ?? "Sketch";
            return new HistoryStep(feature.FeatureId, feature.Kind, $"{name} on {plane}", feature.State, code, HistoryTap.EditSketch);
        }

        // A body step: the name of the live body, or the word of its kind. Only a live body that is not consumed takes a tap.
        var live = feature.ConsumedBy is null ? bodies.FirstOrDefault(body => body.Id == feature.FeatureId) : null;
        var tap = live is not null ? HistoryTap.SelectBody : HistoryTap.None;
        return new HistoryStep(feature.FeatureId, feature.Kind, live?.Name ?? KindText(feature.Kind), feature.State, code, tap);
    }

    // The word of the kind of a feature, as the person knows it from the controls of the desktop.
    internal static string KindText(string kind) => kind switch
    {
        "CreateBox" => "Box",
        "CreateCylinder" => "Cylinder",
        "CreateCone" => "Cone",
        "CreateSphere" => "Sphere",
        "CreateTorus" => "Torus",
        "ImportStl" => "Mesh",
        "Translate" => "Move",
        "Subtract" => "Cut",
        "CreateDatum" => "Datum",
        "CreateSketch" => "Sketch",
        "PlaceImage" => "Image",
        _ => kind,
    };
}

// ADR-0104 item 1: the host reads GetFeatures again only when the version of the Document or the selection changes. The
// host reads the version before it applies the events of the frame to its live bodies and sketches, so a commit between
// the two reads gives a new version in the next frame, and the steps come again.
//
// Finding V161 of the codebase review of 2026-11-09 (b) (TASK-0331): a query waits for the serial lock of the session, so
// the read runs off the thread of the frame, and the frame takes its steps when the read is complete. Until then the frame
// keeps the steps of the last read of the same selection, or none for a new selection. With no selection there is no read.
// Until TASK-0331 the frame ran the query and waited for it, also with no selection, so a long command stopped the window.
internal sealed class HistoryCache
{
    private (long Version, Guid? Selection)? _key;
    private Task<IReadOnlyList<HistoryStep>>? _reading;
    private IReadOnlyList<HistoryStep> _steps = [];

    // The number of reads that started, for a test.
    public int Computed { get; private set; }

    // The read gives a task that the cache does not wait for; the host starts the query on the thread pool.
    public IReadOnlyList<HistoryStep> Steps(long version, Guid? selection, Func<Task<IReadOnlyList<HistoryStep>>> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (selection is null)
        {
            (_key, _reading, _steps) = ((version, null), null, []);
            return _steps;
        }

        if (_key != (version, selection))
        {
            if (_key?.Selection != selection)
                _steps = [];
            _key = (version, selection);
            _reading = read();
            Computed++;
        }

        if (_reading is { IsCompleted: true } done)
        {
            // A failed read, for example of a session that closes, gives no steps.
            _steps = done.IsCompletedSuccessfully ? done.Result : [];
            _ = done.Exception;
            _reading = null;
        }

        return _steps;
    }
}
