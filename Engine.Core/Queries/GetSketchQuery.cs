using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// The record of a live sketch and the residual of each constraint (ADR-0033 item 7).
public sealed record GetSketchQuery : Query
{
    public override string Name => "GetSketch";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
}

// The result of GetSketch. Name is null when the sketch has no name. Each constraint gives
// its residual: zero when the positions satisfy it. Solved is true when each equation of
// the solver is within its tolerance, and Freedom is the number of free degrees (ADR-0035
// item 8).
public sealed record SketchInfo(
    Guid SketchId,
    Guid DatumId,
    string? Name,
    IReadOnlyList<SketchPoint> Points,
    IReadOnlyList<SketchLine> Lines,
    IReadOnlyList<SketchCircle> Circles,
    IReadOnlyList<SketchArc> Arcs,
    IReadOnlyList<SketchConstraintInfo> Constraints,
    bool Solved,
    int Freedom);

public sealed record SketchConstraintInfo(Guid Id, string Kind, IReadOnlyList<Guid> Items, IReadOnlyList<double> Values, double Residual);
