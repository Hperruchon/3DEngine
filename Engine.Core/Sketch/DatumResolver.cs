using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0051 items 1 to 3, 6 and 8: the frame of the face that a reference names. A reference
// resolves on the steps in effect at the place of the command that holds it: the feature gives
// its carrier from its current inputs, and each Translate before that place on the chain of the
// body of the feature moves it. The code uses + - * / sqrt and comparisons only (item 10), and it
// takes the datums in the order of the log, never in the order of a set.
public static class DatumResolver
{
    // ADR-0051 item 1: the names of the faces. The names of the edges and the vertices wait for K0h.
    public static readonly IReadOnlyList<string> ExtrudeFaces = ["start", "end", "side"];

    public static readonly IReadOnlyList<string> BoxFaces = ["+x", "-x", "+y", "-y", "+z", "-z"];

    // The sketch and the distance of an extrude in one state of the Document: now, or after an edit.
    public readonly record struct ExtrudeState(SketchRecord? Sketch, double Distance);

    // The frame of an origin plane of ADR-0033 item 1, or null.
    public static DatumFrame? OriginPlane(Guid id)
        => Datums.Find(id) is { } datum ? new DatumFrame(new DatumPoint(0, 0, 0), datum.U, datum.V, datum.Normal) : null;

    // The frame of a datum in the Document now: an origin plane, or a datum of Document.Datums with
    // its last frame. Null when no datum has the identifier.
    public static DatumFrame? Frame(Document document, Guid id)
        => OriginPlane(id) ?? document.Datums.FirstOrDefault(datum => datum.Id == id)?.Frame;

    // ADR-0051 item 7: the reason that a reference has the wrong form for its feature, or null. A
    // form that is right can still fail to resolve; Resolve gives that reason.
    public static string? Form(DatumReference reference, Command? feature) => feature switch
    {
        ExtrudeCommand when !ExtrudeFaces.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not a face of an extrude; the faces are {string.Join(", ", ExtrudeFaces)}. The edges and the vertices arrive with K0h.",
        ExtrudeCommand when reference.Element == "side" && reference.ItemId is null =>
            "The element 'side' needs the itemId of a line of the sketch of the extrude.",
        ExtrudeCommand when reference.Element != "side" && reference.ItemId is not null =>
            $"The element '{reference.Element}' takes no itemId.",
        CreateBoxCommand when !BoxFaces.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not a face of a box; the faces are {string.Join(", ", BoxFaces)}. The edges and the vertices arrive with K0h.",
        CreateBoxCommand when reference.ItemId is not null =>
            "A face of a box takes no itemId.",
        ExtrudeCommand or CreateBoxCommand => null,
        _ => $"No CreateBox or Extrude in effect has the id '{reference.FeatureId}'.",
    };

    // ADR-0051 item 2: the frame of the reference, or the reason that it does not resolve. Place is
    // the order of the command that holds the reference among the steps in effect, or
    // int.MaxValue for a command that is not in the log yet. State gives the sketch and the
    // distance of an extrude, and frameOf the frame of the datum of a sketch, in one state.
    public static (DatumFrame? Frame, string? Reason) Resolve(
        DatumReference reference, FeatureGraph graph, int place, Func<ExtrudeCommand, ExtrudeState> state, Func<Guid, DatumFrame?> frameOf)
    {
        var feature = graph.Creator(reference.FeatureId);
        if (feature is null || graph.Order(feature.CommandId) >= place)
            return (null, $"No CreateBox or Extrude in effect before the datum has the id '{reference.FeatureId}'.");
        if (Form(reference, feature) is { } wrong)
            return (null, wrong);

        var (frame, reason) = feature switch
        {
            ExtrudeCommand extrude => Extrude(extrude, reference, state(extrude), frameOf),
            CreateBoxCommand box => (Box(box, reference.Element), null),
            _ => (null, $"The command '{feature.Name}' has no faces."),
        };
        return frame is { } carrier ? (carrier with { Origin = Moved(carrier.Origin, graph, feature.CommandId, place) }, null) : (null, reason);
    }

    // ADR-0051 item 6: each datum in effect resolved again in the order of the log, from the state
    // after an edit. The result gives the new record of each datum whose frame or fault changes,
    // the frame of each datum after the edit, and a warning for each datum that does not resolve.
    // A datum that does not resolve keeps its last frame.
    public static (IReadOnlyList<DatumRecord> Changed, Func<Guid, DatumFrame?> FrameOf, IReadOnlyList<Diagnostic> Diagnostics) Rebuild(
        Document document, FeatureGraph graph, Func<ExtrudeCommand, ExtrudeState> state)
    {
        var frames = new Dictionary<Guid, DatumFrame>();
        var changed = new List<DatumRecord>();
        var diagnostics = new List<Diagnostic>();
        DatumFrame? FrameOf(Guid id) => OriginPlane(id) ?? (frames.TryGetValue(id, out var frame) ? frame : null);

        foreach (var create in document.Log.Select(entry => entry.Command).OfType<CreateDatumCommand>())
        {
            if (document.Datums.FirstOrDefault(datum => datum.Id == create.CommandId) is not { } datum)
                continue;
            var (frame, reason) = Resolve(datum.Reference, graph, graph.Order(create.CommandId), state, FrameOf);
            DatumRecord next;
            if (frame is { } resolved)
                next = datum with { Frame = resolved, Fault = null };
            else
            {
                var fault = new DatumFault(DiagnosticCodes.DatumReferenceFailed, $"The datum '{datum.Id}' keeps its last plane: {reason}");
                next = datum with { Fault = fault };
                diagnostics.Add(new Diagnostic(
                    Severity.Warning, fault.Code, fault.Message,
                    Data: new Dictionary<string, object?> { ["datumId"] = datum.Id }));
            }

            frames[datum.Id] = next.Frame;
            if (next != datum)
                changed.Add(next);
        }

        return (changed, FrameOf, diagnostics);
    }

    // ADR-0051 item 3: the faces of an extrude of a sketch on the frame (O, U, V, N) by the distance d.
    private static (DatumFrame? Frame, string? Reason) Extrude(
        ExtrudeCommand extrude, DatumReference reference, ExtrudeState state, Func<Guid, DatumFrame?> frameOf)
    {
        if (state.Sketch is not { } sketch)
            return (null, $"The sketch '{extrude.SketchId}' of the extrude '{extrude.CommandId}' is not live.");
        if (frameOf(sketch.DatumId) is not { } frame)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' does not exist.");
        var (parameters, reason) = ExtrudeProfile.Parameters(sketch, state.Distance, frame);
        if (parameters is not { } profile)
            return (null, $"The profile of the extrude '{extrude.CommandId}' does not resolve: {reason}");

        var (o, u, v, n, d) = (frame.Origin, frame.U, frame.V, frame.Normal, state.Distance);
        switch (reference.Element)
        {
            case "start":
                return (d > 0 ? new DatumFrame(o, u, Negative(v), Negative(n)) : frame, null);
            case "end":
            {
                var top = new DatumPoint(o.X + (d * n.X), o.Y + (d * n.Y), o.Z + (d * n.Z));
                return (d > 0 ? new DatumFrame(top, u, v, n) : new DatumFrame(top, u, Negative(v), Negative(n)), null);
            }
        }

        // The side of a line: its two ends in the order of the counter-clockwise loop. Each line of
        // a profile is one edge of the loop, and no two vertices of the loop share a position.
        if (sketch.Lines.FirstOrDefault(line => line.Id == reference.ItemId) is not { } side)
            return (null, $"The item '{reference.ItemId}' is not a line of the sketch '{sketch.Id}'.");
        var (a, b) = (sketch.Points.First(point => point.Id == side.Start), sketch.Points.First(point => point.Id == side.End));
        var loop = profile.Loop;
        (double X, double Y)? from = null, to = null;
        for (var i = 0; i < loop.Count; i++)
        {
            var (here, next) = (loop[i], loop[(i + 1) % loop.Count]);
            if (here == (a.X, a.Y) && next == (b.X, b.Y))
                (from, to) = (here, next);
            else if (here == (b.X, b.Y) && next == (a.X, a.Y))
                (from, to) = (here, next);
        }
        if (from is not { } p || to is not { } q)
            return (null, $"The line '{side.Id}' is not an edge of the profile of the extrude '{extrude.CommandId}'.");

        var (tx, ty) = (q.X - p.X, q.Y - p.Y);
        var length = Math.Sqrt((tx * tx) + (ty * ty));
        var along = new DatumAxis(((tx * u.X) + (ty * v.X)) / length, ((tx * u.Y) + (ty * v.Y)) / length, ((tx * u.Z) + (ty * v.Z)) / length);
        var origin = new DatumPoint(o.X + (p.X * u.X) + (p.Y * v.X), o.Y + (p.X * u.Y) + (p.Y * v.Y), o.Z + (p.X * u.Z) + (p.Y * v.Z));
        return (new DatumFrame(origin, along, n, Cross(along, n)), null);
    }

    // ADR-0051 item 3: a face of a box with its centre at the origin. Each axis holds 0 and ±1.
    private static DatumFrame Box(CreateBoxCommand box, string element)
    {
        var (x, y, z) = (new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1));
        var (hx, hy, hz) = (box.SizeX / 2, box.SizeY / 2, box.SizeZ / 2);
        return element switch
        {
            "+x" => new DatumFrame(new DatumPoint(hx, 0, 0), y, z, x),
            "-x" => new DatumFrame(new DatumPoint(-hx, 0, 0), z, y, Negative(x)),
            "+y" => new DatumFrame(new DatumPoint(0, hy, 0), z, x, y),
            "-y" => new DatumFrame(new DatumPoint(0, -hy, 0), x, z, Negative(y)),
            "+z" => new DatumFrame(new DatumPoint(0, 0, hz), x, y, z),
            _ => new DatumFrame(new DatumPoint(0, 0, -hz), y, x, Negative(z)),
        };
    }

    // ADR-0051 item 2: each Translate in effect before the place, on the chain of consumers of the
    // body of the feature, moves the origin, in the order of the chain, as the backend moves the
    // body. A Subtract passes it with no change (item 8).
    private static DatumPoint Moved(DatumPoint origin, FeatureGraph graph, Guid handle, int place)
    {
        var (x, y, z) = (origin.X, origin.Y, origin.Z);
        while (graph.Consumer(handle) is { } consumer && graph.Order(consumer.CommandId) < place)
        {
            if (consumer is TranslateCommand translate)
                (x, y, z) = (x + translate.Dx, y + translate.Dy, z + translate.Dz);
            else if (consumer is not SubtractCommand)
                break;
            handle = consumer.CommandId;
        }

        return new DatumPoint(x, y, z);
    }

    private static DatumAxis Negative(DatumAxis a) => new(-a.X, -a.Y, -a.Z);

    private static DatumAxis Cross(DatumAxis a, DatumAxis b)
        => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));
}
