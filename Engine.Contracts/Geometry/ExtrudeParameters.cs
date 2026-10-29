namespace Engine.Contracts.Geometry;

// Command-input value type for an extrude, per ADR-0036 item 5. Not a geometry POCO
// (ADR-0001 §Non-goals): it is the parameter shape of one backend call.
//
// Loop holds the points of one closed loop as (x, y) in the plane, counter-clockwise, and
// the last point does not repeat the first one. Height is greater than zero. The frame
// maps a point (x, y, h) of the plane to Origin + x·U + y·V + h·N.
public readonly record struct ExtrudeParameters(
    IReadOnlyList<(double X, double Y)> Loop,
    double Height,
    (double X, double Y, double Z) Origin,
    (double X, double Y, double Z) U,
    (double X, double Y, double Z) V,
    (double X, double Y, double Z) N);
