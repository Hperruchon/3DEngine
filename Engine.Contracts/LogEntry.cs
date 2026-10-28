using Engine.Contracts.Geometry;

namespace Engine.Contracts;

// ADR-0015 item 5 (KD3): one applied command and the backend that applied it. The
// bus writes the entry in the commit; no command payload holds the backend.
public sealed record LogEntry(Command Command, BackendIdentity Backend);
