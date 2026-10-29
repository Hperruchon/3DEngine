using Engine.Contracts;

namespace Engine.Api.Http.WebSockets;

// Wire shapes for the /events WebSocket stream.
// Per TASK-0010 §5: every wire message is a JSON object with `kind` as the
// discriminator. Three families: Document events (full EventRecord shape),
// subscription protocol messages, and heartbeat.
//
// Incoming: SubscribeRequest is the single client→server message, expected
// as the first frame after the WebSocket upgrade.

internal sealed record SubscribeRequest(Guid? DocumentId, long? LastSeenSeq);

internal sealed record SubscriptionResumeMessage(string Kind, long FromSeq)
{
    public static SubscriptionResumeMessage Create(long fromSeq)
        => new("subscription.resume", fromSeq);
}

internal sealed record SubscriptionResetMessage(string Kind, Guid DocumentId, SnapshotMessage Snapshot)
{
    public static SubscriptionResetMessage Create(Guid documentId, SnapshotMessage snapshot)
        => new("subscription.reset", documentId, snapshot);
}

internal sealed record HeartbeatMessage(string Kind)
{
    public static HeartbeatMessage Instance { get; } = new("heartbeat");
}

// Snapshot DTO per ADR-0010 §2 + extension from ADR-0012 §6.
// V1.x scope: Document metadata + the body projection list.
//
// Version is the count of applied commands, and Seq is the highest event
// sequence number at the moment of the reset. A client continues the stream
// from Seq + 1 (ADR-0020 item 4, which replaces validation rule 2 of ADR-0010).
// Before TASK-0035 the two were one value, and a client used the version.
internal sealed record SnapshotMessage(
    Guid DocumentId,
    Guid? ProjectId,
    int SchemaVersion,
    long Version,
    long Seq,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string LengthUnit,
    IReadOnlyList<SnapshotBodyEntry> Bodies,
    IReadOnlyList<SnapshotSketchEntry> Sketches);

// ADR-0012 §6 and ADR-0028 item 7: each live body with its kind, its name and its visibility.
// ADR-0037 item 8: the snapshot gives the geometry key and the fault of each body.
internal sealed record SnapshotBodyEntry(Guid Handle, string Kind, string? Name, bool Visible, Guid Geometry, BodyFault? Fault);

// ADR-0033 item 9, which amends ADR-0012 §6: each live sketch with its datum and its name.
internal sealed record SnapshotSketchEntry(Guid SketchId, Guid DatumId, string? Name);

internal static class SnapshotProjector
{
    public static SnapshotMessage Project(Document doc, long seq) => new(
        DocumentId: doc.DocumentId,
        ProjectId: doc.ProjectId,
        SchemaVersion: doc.SchemaVersion,
        Version: doc.Version,
        Seq: seq,
        CreatedAt: doc.CreatedAt,
        UpdatedAt: doc.UpdatedAt,
        // ADR-0015 item 7, which amends ADR-0010 section 2.
        LengthUnit: doc.LengthUnit,
        Bodies: doc.Bodies
            .Select(b => new SnapshotBodyEntry(b.Handle.Id, b.Kind, b.Name, b.Visible, b.GeometryKey, b.Fault))
            .ToArray(),
        Sketches: doc.Sketches
            .Select(s => new SnapshotSketchEntry(s.Id, s.DatumId, s.Name))
            .ToArray());
}
