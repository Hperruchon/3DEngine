namespace Engine.Contracts.Sketch;

// ADR-0051 item 1: what a reference names. FeatureId is a CreateBox or an Extrude in effect,
// Element is a name of the table of item 1, and ItemId is the line or the point of the
// sketch of an extrude, or null for a name that needs no item.
public sealed record DatumReference(Guid FeatureId, string Element, Guid? ItemId);

// A position in the space of the Document, in its length unit.
public sealed record DatumPoint(double X, double Y, double Z);

// ADR-0051 item 3: a right-handed frame, Normal = U × V, with the origin of the plane.
public sealed record DatumFrame(DatumPoint Origin, DatumAxis U, DatumAxis V, DatumAxis Normal);

// ADR-0051 item 6: the reason that the reference of a datum did not resolve at the last
// rebuild. The datum keeps its last frame.
public sealed record DatumFault(string Code, string Message);

// ADR-0051 item 4: the record of one datum in effect. A record never changes: a change
// gives a new record with the same identifier. The frame is a projection of the log, and
// Fault is null when the last resolution succeeded. Name is null until a name is given.
public sealed record DatumRecord(Guid Id, string? Name, DatumReference Reference, DatumFrame Frame, DatumFault? Fault);
