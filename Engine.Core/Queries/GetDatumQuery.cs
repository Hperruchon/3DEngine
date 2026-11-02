using Engine.Contracts;

namespace Engine.Core.Queries;

// The record of a datum in effect: its reference, its last frame and its fault (ADR-0051 item 9).
public sealed record GetDatumQuery : Query
{
    public override string Name => "GetDatum";
    public override int SchemaVersion => 1;

    public required Guid DatumId { get; init; }
}
