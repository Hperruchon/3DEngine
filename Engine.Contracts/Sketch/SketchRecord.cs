namespace Engine.Contracts.Sketch;

// ADR-0033 item 4: the record of one live sketch. A record never changes: an edit gives
// a new record with the same identifier. Points holds the origin of the frame first,
// with the identifier of the sketch and the position (0, 0) (item 2). The positions are
// a projection: in K2a each one comes from the command that adds or moves the point,
// and from K3a the solver writes them (item 4). Name is null until a rename.
public sealed record SketchRecord(
    Guid Id,
    Guid DatumId,
    string? Name,
    IReadOnlyList<SketchPoint> Points,
    IReadOnlyList<SketchLine> Lines,
    IReadOnlyList<SketchCircle> Circles,
    IReadOnlyList<SketchArc> Arcs,
    IReadOnlyList<SketchConstraint> Constraints);

// A point of a sketch, at (X, Y) in the plane of its datum, in the length unit of the
// Document (ADR-0033 item 2).
public sealed record SketchPoint(Guid Id, double X, double Y);

// A line segment from the point Start to the point End; the two are different points.
public sealed record SketchLine(Guid Id, Guid Start, Guid End);

// A circle with the point Center and a radius. Phase K2 gives the command that adds one.
public sealed record SketchCircle(Guid Id, Guid Center, double Radius);

// An arc from Start to End, counter-clockwise, about Center; its two radii are equal by
// an implicit equation. Phase K2 gives the command that adds one.
public sealed record SketchArc(Guid Id, Guid Center, Guid Start, Guid End);

// ADR-0033 item 3: a constraint of a kind, its items in order and its values. No value
// is an angle: the kind "angle" holds a cosine and a sine.
public sealed record SketchConstraint(Guid Id, string Kind, IReadOnlyList<Guid> Items, IReadOnlyList<double> Values);
