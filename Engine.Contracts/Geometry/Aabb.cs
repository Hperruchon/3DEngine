namespace Engine.Contracts.Geometry;

// Query-output value type for axis-aligned bounding box. Per ADR-0012 §1.
public readonly record struct Aabb(
    double MinX, double MinY, double MinZ,
    double MaxX, double MaxY, double MaxZ);
