namespace Engine.Contracts.Geometry;

// ADR-0015 item 5 (KD3): the name and the version of the backend that applied a
// command. Each log entry records it, and a replay refuses an entry of another
// backend (anti-objective 10).
public sealed record BackendIdentity(string Name, string Version);
