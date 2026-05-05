namespace Engine.Contracts;

public sealed record EventRecord(
    long Seq,
    DateTime Timestamp,
    Guid DocumentId,
    Guid? CauseCommandId,
    string Kind,
    object? Payload);
