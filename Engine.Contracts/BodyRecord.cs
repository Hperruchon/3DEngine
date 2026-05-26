namespace Engine.Contracts;

// Per-body Document-side projection per ADR-0012 §3. The Document holds the
// handle list and minimum metadata; the backend owns the geometry data
// (ADR-0001 §3). Kind discriminates primitive type for V1 ("Box");
// extends additively as new primitives land.
public sealed record BodyRecord(BodyHandle Handle, string Kind);
