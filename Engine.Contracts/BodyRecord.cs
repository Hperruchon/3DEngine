namespace Engine.Contracts;

// Per-body Document-side projection per ADR-0012 §3. The Document holds the
// handle list and minimum metadata; the backend owns the geometry data
// (ADR-0001 §3). Kind discriminates primitive type for V1 ("Box");
// extends additively as new primitives land. Name and Visible are the name and
// the visibility of the body (ADR-0028 item 1): no name and visible until a
// command changes them.
//
// ADR-0037 items 1 and 6: Geometry is the key of the solid of the body in the
// backend, or null when the key is the handle. A rebuild gives a body a new key and
// keeps its handle. Fault is the reason that the last rebuild failed, or null.
public sealed record BodyRecord(
    BodyHandle Handle,
    string Kind,
    string? Name = null,
    bool Visible = true,
    Guid? Geometry = null,
    BodyFault? Fault = null)
{
    // The key of the solid in the backend (ADR-0037 item 2): each read of a solid uses it.
    public Guid GeometryKey => Geometry ?? Handle.Id;
}
