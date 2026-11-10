using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0051 items 1 to 3, 6 and 8: the frame of the face that a reference names. A reference
// resolves on the steps in effect at the place of the command that holds it: the feature gives
// its carrier from its current inputs, and each Translate before that place on the chain of the
// body of the feature moves it. The code uses + - * / sqrt and comparisons only (item 10), and it
// takes the datums in the order of the log, never in the order of a set. ADR-0052: the points of an
// edge or a vertex for a reference of a sketch, their projection onto the plane of the sketch, and
// the check of a cycle.
public static class DatumResolver
{
    // ADR-0051 item 1: the names of the faces.
    public static readonly IReadOnlyList<string> ExtrudeFaces = ["start", "end", "side"];

    public static readonly IReadOnlyList<string> BoxFaces = ["+x", "-x", "+y", "-y", "+z", "-z"];

    // ADR-0076 item 3: the faces of a cylinder. The side is curved, so only +z and -z carry a datum (item 4).
    public static readonly IReadOnlyList<string> CylinderFaces = ["+z", "-z", "side"];

    // ADR-0089 item 3: the faces of a cone. The side is curved, so only -z carries a datum.
    public static readonly IReadOnlyList<string> ConeFaces = ["-z", "side"];

    // ADR-0052 item 1: the names of the edges and the vertices of an extrude. Each one takes an itemId:
    // a line for startEdge and endEdge, and a point for the others.
    public static readonly IReadOnlyList<string> ExtrudeEdges = ["startEdge", "endEdge", "sideEdge"];

    public static readonly IReadOnlyList<string> ExtrudeVertices = ["startVertex", "endVertex"];

    // The identifiers of the two ends of the edge of a reference (ADR-0052 item 2).
    public static readonly Guid FirstEnd = new("00000000-0000-0000-0000-000000000001");

    public static readonly Guid SecondEnd = new("00000000-0000-0000-0000-000000000002");

    // The smallest positive normal double, 2^-1022. A smaller positive value is subnormal and has fewer bits (E130).
    private const double SmallestNormal = 2.2250738585072014E-308;

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
            $"The element '{reference.Element}' is not a face of an extrude; the faces are {string.Join(", ", ExtrudeFaces)}. An edge or a vertex is a reference of a sketch (AddSketchReference, ADR-0052).",
        ExtrudeCommand when reference.Element == "side" && reference.ItemId is null =>
            "The element 'side' needs the itemId of a line of the sketch of the extrude.",
        ExtrudeCommand when reference.Element != "side" && reference.ItemId is not null =>
            $"The element '{reference.Element}' takes no itemId.",
        CreateBoxCommand when !BoxFaces.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not a face of a box; the faces are {string.Join(", ", BoxFaces)}. An edge or a vertex is a reference of a sketch (AddSketchReference, ADR-0052).",
        CreateBoxCommand when reference.ItemId is not null =>
            "A face of a box takes no itemId.",
        CreateCylinderCommand when !CylinderFaces.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not a face of a cylinder; the faces are {string.Join(", ", CylinderFaces)}.",
        CreateCylinderCommand when reference.Element == "side" =>
            "The face 'side' of a cylinder is curved, so it has no plane for a datum (ADR-0076 item 4).",
        CreateCylinderCommand when reference.ItemId is not null =>
            "A face of a cylinder takes no itemId.",
        CreateConeCommand when !ConeFaces.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not a face of a cone; the faces are {string.Join(", ", ConeFaces)}.",
        CreateConeCommand when reference.Element == "side" =>
            "The face 'side' of a cone is curved, so it has no plane for a datum (ADR-0089 item 3).",
        CreateConeCommand when reference.ItemId is not null =>
            "A face of a cone takes no itemId.",
        CreateSphereCommand or CreateTorusCommand when reference.Element != "surface" =>
            $"The element '{reference.Element}' is not a face of a sphere or a torus; the face is surface.",
        CreateSphereCommand or CreateTorusCommand =>
            "The face 'surface' is curved, so it has no plane for a datum (ADR-0089 item 5).",
        ExtrudeCommand or CreateBoxCommand or CreateCylinderCommand or CreateConeCommand => null,
        ImportStlCommand => $"The feature '{reference.FeatureId}' is a mesh body of an STL file, which has no named faces (ADR-0083 item 4).",
        _ => $"No CreateBox, CreateCylinder, CreateCone, CreateSphere, CreateTorus or Extrude in effect has the id '{reference.FeatureId}'.",
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
            return (null, $"No CreateBox, CreateCylinder, CreateCone, CreateSphere, CreateTorus or Extrude in effect before the datum has the id '{reference.FeatureId}'.");
        if (Form(reference, feature) is { } wrong)
            return (null, wrong);

        var (frame, reason) = feature switch
        {
            ExtrudeCommand extrude => Extrude(extrude, reference, state(extrude), frameOf),
            CreateBoxCommand box => (Box(box, reference.Element), null),
            CreateCylinderCommand cylinder => (Cylinder(cylinder, reference.Element), null),
            // ADR-0089 item 3: the base of a cone is the bottom of a cylinder of the same height.
            CreateConeCommand cone => (Cylinder(new CreateCylinderCommand { Radius = cone.Radius, Height = cone.Height }, reference.Element), null),
            _ => (null, $"The command '{feature.Name}' has no faces."),
        };
        if (frame is not { } carrier)
            return (null, reason);

        // Finding E125 of the codebase review of 2026-11-03 (TASK-0169): a sum of the origin can overflow, and a frame
        // with a value that is not finite or an axis of the length zero is not a plane.
        // ADR-0112 item 7 (TASK-0335): a Rotate on the chain also turns the axes.
        var resolved = Moved(carrier, graph, feature.CommandId, place);

        // ADR-0059 item 4 (phase K4c, TASK-0185): a face of a subtrahend is a wall of the result, with its normal into the
        // hole. Each Subtract before the place that takes the body as its subtrahend turns the frame, so that n stays out
        // of the solid: (u, v, n) becomes (u, -v, -n), which is right-handed again.
        if (Turned(graph, feature.CommandId, place))
            resolved = resolved with { V = Negative(resolved.V), Normal = Negative(resolved.Normal) };
        return Finite(resolved)
            ? (resolved, null)
            : (null, $"The element '{reference.Element}' of '{reference.FeatureId}' gives a frame with a value that is not finite or an axis of the length zero.");
    }

    // What a rebuild of the datums and the references gives: the new record of each datum and each
    // sketch that changes, in the order of the log, the frame of each datum and the record of each
    // sketch after the edit, and a warning for each one that does not resolve.
    public sealed record Outcome(
        IReadOnlyList<DatumRecord> Datums,
        IReadOnlyList<SketchRecord> Sketches,
        Func<Guid, DatumFrame?> FrameOf,
        Func<Guid, SketchRecord?> SketchOf,
        IReadOnlyList<Diagnostic> Diagnostics);

    // ADR-0051 item 6 and ADR-0052 item 8: each CreateDatum and each AddSketchReference in effect,
    // resolved again in the order of the log, from the state after an edit. SketchOf gives the record
    // of a sketch after the edit, and distanceOf the distance of an extrude after it. A reference
    // whose points move changes its sketch, and the solver solves it again, so a later reference or
    // datum sees the new record. A datum or a reference that does not resolve keeps its last frame or
    // its last points and gets a fault. ADR-0055 item 1: this is the first walk of the regeneration
    // (Features/Regeneration.cs).
    //
    // Finding E128 of the second codebase review of 2026-11-03 (TASK-0176): a reference later in the
    // log can change a sketch that an earlier datum or reference read, so the walk repeats while a
    // reference solves a sketch again. Each walk reads the frames and the sketches of the walk before
    // it, and the datums and the warnings come from the last walk. A datum reads only steps before it
    // in the log, except through a sketch, and the cycle check of ADR-0052 item 7 keeps the graph of
    // the references acyclic, so a walk that changes no sketch ends the loop; the count of the
    // references bounds it.
    public static Outcome Rebuild(
        Document document, FeatureGraph graph, Func<Guid, SketchRecord?> sketchOf, Func<ExtrudeCommand, double> distanceOf)
    {
        var frames = new Dictionary<Guid, DatumFrame>();
        var sketches = new Dictionary<Guid, SketchRecord>();
        var changedDatums = new List<DatumRecord>();
        var changedSketches = new List<Guid>();
        var diagnostics = new List<Diagnostic>();
        DatumFrame? FrameOf(Guid id) => OriginPlane(id) ?? (frames.TryGetValue(id, out var frame) ? frame : Frame(document, id));
        SketchRecord? SketchOf(Guid id) => sketches.TryGetValue(id, out var sketch) ? sketch : sketchOf(id);
        ExtrudeState State(ExtrudeCommand extrude) => new(SketchOf(extrude.SketchId), distanceOf(extrude));

        // ADR-0054 item 4: the steps in effect, in the order of the log. A datum or a reference of a step that is not in
        // effect has no record, so the walk of the whole log gave the same result.
        var steps = Features.FeatureList.Of(document).Steps;
        var limit = steps.OfType<AddSketchReferenceCommand>().Count() + 1;
        for (var walk = 1; Walk(); walk++)
        {
            if (walk > limit)
                throw new InvalidOperationException("The walk of the planes does not end, so the references of the sketches have a cycle.");
        }

        // Finding E133 of the codebase review of 2026-11-05 (TASK-0182): the sketches came in the order of their first solve
        // over all walks. They come in the log order of the command that creates each one, as the Outcome says.
        return new Outcome(changedDatums, [.. changedSketches.OrderBy(id => graph.Order(id)).Select(id => sketches[id])], FrameOf, SketchOf, diagnostics);

        // One walk of the steps in effect. True when a reference solved a sketch again.
        bool Walk()
        {
            var solved = false;
            changedDatums.Clear();
            diagnostics.Clear();
            foreach (var command in steps)
            {
                switch (command)
                {
                    case CreateDatumCommand create when document.Datums.FirstOrDefault(datum => datum.Id == create.CommandId) is { } datum:
                    {
                        var (frame, reason) = Resolve(datum.Reference, graph, graph.Order(create.CommandId), State, FrameOf);
                        DatumRecord next;
                        if (frame is { } resolved)
                            next = datum with { Frame = resolved, Fault = null };
                        else
                        {
                            // Finding E127 (Q2 A of the review of 2026-11-03): a fault that stays gives no new warning.
                            var fault = new DatumFault(DiagnosticCodes.DatumReferenceFailed, $"The datum '{datum.Id}' keeps its last plane: {reason}");
                            next = datum with { Fault = fault };
                            if (datum.Fault != fault)
                                diagnostics.Add(new Diagnostic(
                                    Severity.Warning, fault.Code, fault.Message,
                                    Data: new Dictionary<string, object?> { ["datumId"] = datum.Id }));
                        }

                        frames[datum.Id] = next.Frame;
                        if (next != datum)
                            changedDatums.Add(next);
                        break;
                    }

                    case AddSketchReferenceCommand add
                        when SketchOf(add.SketchId) is { } record && record.References.FirstOrDefault(reference => reference.Id == add.CommandId) is { } entry:
                    {
                        var (positions, reason) = Positions(entry.Target, graph, graph.Order(add.CommandId), State, FrameOf, record);
                        DatumFault? fault = null;
                        if (positions is null)
                        {
                            fault = new DatumFault(DiagnosticCodes.SketchReferenceFailed, $"The reference '{entry.Id}' of the sketch '{record.Id}' keeps its last points: {reason}");
                            // A fault that stays gives no new warning. The fault before the edit decides, so a later walk
                            // gives the warning of the first one.
                            var before = sketchOf(record.Id)?.References.FirstOrDefault(reference => reference.Id == entry.Id)?.Fault;
                            if (before != fault)
                                diagnostics.Add(new Diagnostic(
                                    Severity.Warning, fault.Code, fault.Message,
                                    Data: new Dictionary<string, object?> { ["sketchId"] = record.Id, ["referenceId"] = entry.Id }));
                        }

                        if (SketchModel.Placed(record, entry, positions, fault) is not { } placed)
                            break;
                        sketches[record.Id] = SketchSolver.Solve(placed);
                        solved = true;
                        if (!changedSketches.Contains(record.Id))
                            changedSketches.Add(record.Id);
                        break;
                    }
                }
            }

            return solved;
        }
    }

    // ADR-0052 item 7: the reason that a reference of a sketch has the wrong form for its feature, or null.
    public static string? PointForm(DatumReference reference, Command? feature) => feature switch
    {
        ExtrudeCommand when !ExtrudeEdges.Contains(reference.Element) && !ExtrudeVertices.Contains(reference.Element) =>
            $"The element '{reference.Element}' is not an edge or a vertex of an extrude; they are {string.Join(", ", ExtrudeEdges.Concat(ExtrudeVertices))}. A face is a datum (CreateDatum, ADR-0051).",
        ExtrudeCommand when reference.ItemId is null =>
            $"The element '{reference.Element}' needs the itemId of {(reference.Element is "startEdge" or "endEdge" ? "a line" : "a point")} of the profile of the extrude.",
        CreateBoxCommand when BoxAxes(reference.Element) is not { Count: 2 or 3 } =>
            $"The element '{reference.Element}' is not an edge or a vertex of a box: an edge has two signed axes and a vertex three, in the order x, y, z, for example '+x-z' or '+x-y+z'.",
        CreateBoxCommand when reference.ItemId is not null =>
            "An edge or a vertex of a box takes no itemId.",
        CreateCylinderCommand =>
            "A cylinder gives no edge and no vertex to a sketch in this slice (ADR-0076 item 4).",
        CreateConeCommand =>
            "A cone gives no edge and no vertex to a sketch in this slice (ADR-0089 item 3).",
        CreateSphereCommand or CreateTorusCommand =>
            "A sphere or a torus gives no edge and no vertex to a sketch (ADR-0089 item 5).",
        ExtrudeCommand or CreateBoxCommand => null,
        _ => $"No CreateBox, CreateCylinder, CreateCone, CreateSphere, CreateTorus or Extrude in effect has the id '{reference.FeatureId}'.",
    };

    // ADR-0052 items 1 and 3: the positions in the plane of the sketch of the point of a vertex, or of
    // the two ends of an edge, or the reason that the reference does not resolve.
    public static (IReadOnlyList<(double X, double Y)>? Positions, string? Reason) Positions(
        DatumReference reference, FeatureGraph graph, int place, Func<ExtrudeCommand, ExtrudeState> state,
        Func<Guid, DatumFrame?> frameOf, SketchRecord sketch)
    {
        var feature = graph.Creator(reference.FeatureId);
        if (feature is null || graph.Order(feature.CommandId) >= place)
            return (null, $"No CreateBox, CreateCylinder, CreateCone, CreateSphere, CreateTorus or Extrude in effect before the reference has the id '{reference.FeatureId}'.");
        if (PointForm(reference, feature) is { } wrong)
            return (null, wrong);
        if (frameOf(sketch.DatumId) is not { } plane)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' does not exist.");

        var (points, reason) = feature switch
        {
            ExtrudeCommand extrude => ExtrudePoints(extrude, reference, state(extrude), frameOf),
            CreateBoxCommand box => (BoxPoints(box, reference.Element), null),
            _ => (null, $"The command '{feature.Name}' has no edges."),
        };
        if (points is null)
            return (null, reason);

        // Item 3: the orthogonal projection onto the plane of the sketch, with + - * only.
        var positions = points
            .Select(point => Moved(point, graph, feature.CommandId, place))
            .Select(point =>
            {
                var (x, y, z) = (point.X - plane.Origin.X, point.Y - plane.Origin.Y, point.Z - plane.Origin.Z);
                return ((x * plane.U.X) + (y * plane.U.Y) + (z * plane.U.Z), (x * plane.V.X) + (y * plane.V.Y) + (z * plane.V.Z));
            })
            .ToList();
        // Finding E125: a sum can overflow, and an infinity times 0 gives NaN.
        if (positions.Any(at => !double.IsFinite(at.Item1) || !double.IsFinite(at.Item2)))
            return (null, $"The element '{reference.Element}' of '{reference.FeatureId}' gives a point that is not finite in the plane of the sketch.");
        if (positions.Count == 2 && positions[0] == positions[1])
            return (null, $"The edge '{reference.Element}' of '{reference.FeatureId}' projects to one point of the plane of the sketch; use its vertex.");
        return (positions, null);
    }

    // ADR-0052 item 7: true when the feature depends on the sketch: it is an extrude of the sketch, or
    // an extrude whose sketch lies on a datum or holds a reference whose feature depends on the sketch.
    public static bool DependsOn(Document document, FeatureGraph graph, Guid feature, Guid sketch)
    {
        var seen = new HashSet<Guid>();
        return Feature(feature);

        bool Feature(Guid id) => graph.Creator(id) is ExtrudeCommand extrude && Sketch(extrude.SketchId);

        bool Sketch(Guid id)
        {
            if (id == sketch)
                return true;
            if (!seen.Add(id) || SketchModel.Find(document, id) is not { } record)
                return false;
            if (document.Datums.FirstOrDefault(datum => datum.Id == record.DatumId) is { } datum && Feature(datum.Reference.FeatureId))
                return true;
            return record.References.Any(reference => Feature(reference.Target.FeatureId));
        }
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

        // Finding E125: the square of a very short or very long side underflows to 0 or overflows. Then the root
        // takes tx and ty over the larger of their magnitudes, as a calibration does (E123). Finding E130 of the second
        // codebase review of 2026-11-03 (TASK-0177): a sum of the squares that is subnormal but not 0 loses bits, and a
        // side of 3e-162 gave axes of the length 0.954; such a side scales too. Each side whose sum is a normal double
        // keeps the bits of v0.167.
        var along = new DatumAxis(((tx * u.X) + (ty * v.X)) / length, ((tx * u.Y) + (ty * v.Y)) / length, ((tx * u.Z) + (ty * v.Z)) / length);
        if (((tx * tx) + (ty * ty) < SmallestNormal || double.IsPositiveInfinity(length)) && double.IsFinite(tx) && double.IsFinite(ty) && (tx != 0 || ty != 0))
        {
            // Finding E131 of the codebase review of 2026-11-05 (TASK-0182): the scaled length is subnormal when both
            // components are, and tx and ty over it kept their error, so a diagonal side of 5e-324 gave an axis of the
            // length 1.414. The axis comes from the scaled components and their own root, which are normal.
            var larger = Math.Max(Math.Abs(tx), Math.Abs(ty));
            var (sx, sy) = (tx / larger, ty / larger);
            var root = Math.Sqrt((sx * sx) + (sy * sy));
            along = new DatumAxis(((sx * u.X) + (sy * v.X)) / root, ((sx * u.Y) + (sy * v.Y)) / root, ((sx * u.Z) + (sy * v.Z)) / root);
        }
        var origin = new DatumPoint(o.X + (p.X * u.X) + (p.Y * v.X), o.Y + (p.X * u.Y) + (p.Y * v.Y), o.Z + (p.X * u.Z) + (p.Y * v.Z));
        return (new DatumFrame(origin, along, n, Cross(along, n)), null);
    }

    // ADR-0052 item 1: the points of an edge or a vertex of an extrude, in the space of the Document,
    // before the moves of its chain. A line of a reference is not a line of the profile (item 6).
    private static (IReadOnlyList<DatumPoint>? Points, string? Reason) ExtrudePoints(
        ExtrudeCommand extrude, DatumReference reference, ExtrudeState state, Func<Guid, DatumFrame?> frameOf)
    {
        if (state.Sketch is not { } sketch)
            return (null, $"The sketch '{extrude.SketchId}' of the extrude '{extrude.CommandId}' is not live.");
        if (frameOf(sketch.DatumId) is not { } frame)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' does not exist.");
        var (parameters, reason) = ExtrudeProfile.Parameters(sketch, state.Distance, frame);
        if (parameters is null)
            return (null, $"The profile of the extrude '{extrude.CommandId}' does not resolve: {reason}");

        var (o, u, v, n, d) = (frame.Origin, frame.U, frame.V, frame.Normal, state.Distance);
        DatumPoint World(SketchPoint p, double h)
            => new(o.X + (p.X * u.X) + (p.Y * v.X) + (h * n.X), o.Y + (p.X * u.Y) + (p.Y * v.Y) + (h * n.Y), o.Z + (p.X * u.Z) + (p.Y * v.Z) + (h * n.Z));
        var construction = sketch.References.Select(item => item.Line).OfType<Guid>().ToHashSet();
        var item = reference.ItemId;

        if (reference.Element is "startEdge" or "endEdge")
        {
            if (sketch.Arcs.Any(arc => arc.Id == item))
                return (null, $"The arc '{item}' is a curved edge, which waits for K5.");
            if (sketch.Lines.FirstOrDefault(line => line.Id == item && !construction.Contains(line.Id)) is not { } line)
                return (null, $"The item '{item}' is not a line of the profile of the extrude '{extrude.CommandId}'.");
            var h = reference.Element == "endEdge" ? d : 0;
            return ([World(SketchModel.Point(sketch, line.Start), h), World(SketchModel.Point(sketch, line.End), h)], null);
        }

        // A vertex of the profile is an end of a line of the profile or of an arc.
        var vertex = sketch.Lines.Where(line => !construction.Contains(line.Id)).Any(line => line.Start == item || line.End == item)
            || sketch.Arcs.Any(arc => arc.Start == item || arc.End == item);
        if (!vertex)
            return (null, $"The item '{item}' is not a vertex of the profile of the extrude '{extrude.CommandId}'.");
        var point = SketchModel.Point(sketch, item!.Value);
        return reference.Element switch
        {
            "sideEdge" => ([World(point, 0), World(point, d)], null),
            "endVertex" => ([World(point, d)], null),
            _ => ([World(point, 0)], null),
        };
    }

    // ADR-0052 item 1: the signed axes of the name of an edge or a vertex of a box, in the order x, y,
    // z, or null. "+x-z" gives [(0, 1), (2, -1)].
    private static IReadOnlyList<(int Axis, int Sign)>? BoxAxes(string element)
    {
        if (element.Length is 0 || element.Length % 2 != 0)
            return null;
        var axes = new List<(int Axis, int Sign)>();
        for (var i = 0; i < element.Length; i += 2)
        {
            var axis = "xyz".IndexOf(element[i + 1]);
            if (element[i] is not ('+' or '-') || axis < 0 || (axes.Count > 0 && axes[^1].Axis >= axis))
                return null;
            axes.Add((axis, element[i] == '+' ? 1 : -1));
        }

        return axes;
    }

    // ADR-0052 item 1: the points of an edge or a vertex of a box with its centre at the origin. An
    // edge runs along its free axis from -h to +h.
    private static IReadOnlyList<DatumPoint> BoxPoints(CreateBoxCommand box, string element)
    {
        var half = new[] { box.SizeX / 2, box.SizeY / 2, box.SizeZ / 2 };
        var axes = BoxAxes(element)!;
        var ends = axes.Count == 3 ? new[] { 1 } : new[] { -1, 1 };
        // An edge runs along the one axis that its name does not give; a vertex has no free axis.
        var free = axes.Count == 2 ? Enumerable.Range(0, 3).First(axis => axes.All(item => item.Axis != axis)) : -1;
        return
        [
            .. ends.Select(end =>
            {
                var at = new double[3];
                foreach (var (axis, sign) in axes)
                    at[axis] = sign * half[axis];
                if (axes.Count == 2)
                    at[free] = end * half[free];
                return new DatumPoint(at[0], at[1], at[2]);
            }),
        ];
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

    // ADR-0076 item 4: the top or the bottom of a cylinder with its centre at the origin, as the face of a box of the
    // same height. Form refuses the side before this.
    private static DatumFrame Cylinder(CreateCylinderCommand cylinder, string element)
    {
        var (x, y, z) = (new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1));
        var half = cylinder.Height / 2;
        return element == "+z"
            ? new DatumFrame(new DatumPoint(0, 0, half), x, y, z)
            : new DatumFrame(new DatumPoint(0, 0, -half), y, x, Negative(z));
    }

    // ADR-0051 item 2: each Translate in effect before the place, on the chain of consumers of the
    // body of the feature, moves the origin, in the order of the chain, as the backend moves the
    // body. A Subtract passes it with no change (item 8). ADR-0112 item 7 (TASK-0335): each Rotate
    // turns the origin about the origin of the Document, with the nine numbers that the backend takes.
    private static DatumPoint Moved(DatumPoint origin, FeatureGraph graph, Guid handle, int place)
    {
        var (x, y, z) = (origin.X, origin.Y, origin.Z);
        foreach (var step in Chain(graph, handle, place))
        {
            if (step is TranslateCommand translate)
                (x, y, z) = (x + translate.Dx, y + translate.Dy, z + translate.Dz);
            else if (step is RotateCommand rotate && RotateCommandHandler.Turn(rotate) is { } turn)
                (x, y, z) = turn.Apply(x, y, z);
        }

        return new DatumPoint(x, y, z);
    }

    // ADR-0112 item 7: the frame moved as its origin, and each Rotate on the chain also turns its three axes.
    private static DatumFrame Moved(DatumFrame frame, FeatureGraph graph, Guid handle, int place)
    {
        var (u, v, n) = (frame.U, frame.V, frame.Normal);
        foreach (var step in Chain(graph, handle, place))
        {
            if (step is RotateCommand rotate && RotateCommandHandler.Turn(rotate) is { } turn)
                (u, v, n) = (Rotated(turn, u), Rotated(turn, v), Rotated(turn, n));
        }

        return new DatumFrame(Moved(frame.Origin, graph, handle, place), u, v, n);
    }

    private static DatumAxis Rotated(Rotation turn, DatumAxis a)
    {
        var (x, y, z) = turn.Apply(a.X, a.Y, a.Z);
        return new DatumAxis(x, y, z);
    }

    // The Translate and Rotate steps in effect before the place, on the chain of consumers of the body of the feature,
    // in the order of the chain. A Subtract passes with no step; another consumer ends the chain.
    private static IEnumerable<Command> Chain(FeatureGraph graph, Guid handle, int place)
    {
        while (graph.Consumer(handle) is { } consumer && graph.Order(consumer.CommandId) < place)
        {
            if (consumer is TranslateCommand or RotateCommand)
                yield return consumer;
            else if (consumer is not SubtractCommand)
                yield break;
            handle = consumer.CommandId;
        }
    }

    // ADR-0059 item 4: true when an odd count of the Subtract steps in effect before the place, on the chain of consumers
    // of the body of the feature, takes the body of the chain as the subtrahend.
    private static bool Turned(FeatureGraph graph, Guid handle, int place)
    {
        var turned = false;
        while (graph.Consumer(handle) is { } consumer && graph.Order(consumer.CommandId) < place)
        {
            if (consumer is SubtractCommand subtract && subtract.SubtrahendBodyId == handle)
                turned = !turned;
            else if (consumer is not (TranslateCommand or RotateCommand or SubtractCommand))
                break;
            handle = consumer.CommandId;
        }

        return turned;
    }

    // Finding E125: each value of the frame is finite, and no axis has the length zero.
    private static bool Finite(DatumFrame frame)
        => Finite(frame.Origin.X, frame.Origin.Y, frame.Origin.Z) && Axis(frame.U) && Axis(frame.V) && Axis(frame.Normal);

    private static bool Axis(DatumAxis a) => Finite(a.X, a.Y, a.Z) && (a.X != 0 || a.Y != 0 || a.Z != 0);

    private static bool Finite(double x, double y, double z) => double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z);

    private static DatumAxis Negative(DatumAxis a) => new(-a.X, -a.Y, -a.Z);

    private static DatumAxis Cross(DatumAxis a, DatumAxis b)
        => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));
}
