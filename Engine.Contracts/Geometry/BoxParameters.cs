namespace Engine.Contracts.Geometry;

// Command-input value type for axis-aligned box creation. Per ADR-0012 §1.
// Not a geometry POCO (rejected by ADR-0001 §Non-goals) — it is the
// parameter shape of a command, not generic geometry data.
public readonly record struct BoxParameters(double SizeX, double SizeY, double SizeZ);
