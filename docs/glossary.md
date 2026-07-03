# Glossary

Canonical vocabulary. One line per term + a pointer to where it is **defined** (file) and **decided** (ADR). Definitions here are nominal only — the *why* and the *rules* live in the linked ADR; the *current shape* lives in the code. Names listed are locked to what exists in `Engine.Contracts/` and `Engine.Core/` today; do not rename without an ADR (see [conventions.md](conventions.md) "Contract-change triggers").

## Triad

| Term | Meaning | Defined / decided |
|---|---|---|
| **Command** | The only mutator. Serializable, versioned (`SchemaVersion`), replayable; carries `CommandId` and optional `ExpectedDocumentVersion`. | `Engine.Contracts/Command.cs` · ADR-0006, ADR-0008 |
| **Query** | A read. Never logged, replayed, or streamed. Carries `QueryId` + `SchemaVersion`. | `Engine.Contracts/Query.cs` · ADR-0008 |
| **Event** | An observation of what happened, derived from commands; surfaces only via the event stream. The record type is `EventRecord`. | concept · ADR-0005 |

## Result envelopes

| Term | Meaning | Defined / decided |
|---|---|---|
| **CommandResult** | Structured outcome of an `Apply` — `Status` (`CommandStatus`: Applied/Rejected/Cancelled), `AppliedAtSeq`, `DocumentVersion`, `Outputs`, diagnostics, optional `Error`. | `Engine.Contracts/CommandResult.cs` · ADR-0008 (extends ADR-0006) |
| **QueryResult\<T\>** | Structured outcome of a query — `AsOfDocumentVersion`, typed `Result`, diagnostics, optional `Error`. | `Engine.Contracts/QueryResult.cs` · ADR-0008 |
| **Outputs** | Typed-bag of named result values (`IReadOnlyDictionary<string, object?>`) with `TryGet<T>`. Returned by command handlers. | `Engine.Contracts/Outputs.cs` · ADR-0008 |
| **Diagnostic** | A `Severity` + stable `Code` + message attached to a result (non-fatal channel). Codes are registered in [diagnostics.md](diagnostics.md). | `Engine.Contracts/Diagnostic.cs` · ADR-0008 |
| **ErrorDetail** | The fatal-error channel on a result — `Code`, message, optional `Cause`, `Retriable`. Present iff the command/query failed. | `Engine.Contracts/ErrorDetail.cs` · ADR-0008 |
| **EventRecord** | The event wire type — `Seq`, `Timestamp`, `DocumentId`, `CauseCommandId`, `Kind`, `Payload`. | `Engine.Contracts/EventRecord.cs` · ADR-0005 |

## State

| Term | Meaning | Defined / decided |
|---|---|---|
| **Document** | The design-truth aggregate: ordered command `Log` + materialized projections (`Bodies`) + metadata. Mutated only inside `CommandBus`'s commit section. | `Engine.Contracts/Document.cs` · ADR-0004 |
| **Version** | `Document.Version` — runtime observation counter mirroring the **last emitted `Seq`** across all events (applied, rejected, cancelled), not a successful-mutation count. | `Engine.Contracts/Document.cs` · ADR-0005 |
| **Seq** | Monotonic per-Document event sequence number on `EventRecord`; the cursor for replay/reconnect. | `Engine.Contracts/EventRecord.cs` · ADR-0005 |

## Geometry

| Term | Meaning | Defined / decided |
|---|---|---|
| **BodyHandle** | Opaque identity for a geometry body (`Guid Id`). Deterministic from `CommandId` for single-body creates, so replay is byte-stable. | `Engine.Contracts/BodyHandle.cs` · ADR-0001, ADR-0012 |
| **Body** | Document-side body projection (`BodyRecord` = `Handle` + `Kind`): handles + minimum metadata only; the backend owns the geometry data. | `Engine.Contracts/BodyRecord.cs` · ADR-0012 §3 |
| **IGeometryBackend** | The only kernel type a handler sees. Exposes `Capabilities` flags + `TryGet<T>()`; backends are caches, swapped by replay. | `Engine.Contracts/Geometry/IGeometryBackend.cs` · ADR-0001, ADR-0012 |
| **capability (`TryGet<T>`)** | A typed geometry interface negotiated at runtime via `IGeometryBackend.TryGet<T>()` (e.g. `IMeshOps`, `IGeometryQuery`; reserved `IBRepOps`/`IFeatureIdMap`); declared by `BackendCapabilities`. Returns null, never silently falls back. | `Engine.Contracts/Geometry/` · ADR-0001 §1, ADR-0012 §1 |

## Concepts

| Term | Meaning | Defined / decided |
|---|---|---|
| **Replay** | Reconstructing an equivalent `Document` (+ backend state) by re-applying a log on a fresh backend. Equivalence holds modulo `Timestamp`/`DocumentId`. | `Engine.Core/Replay.cs` · ADR-0005, ADR-0012 §2 |
| **projection** | Any state regenerable from the log — `Document.Bodies`, the event stream, render scene, snapshots. Losing a projection is recovery, not data loss. | concept · [CHARTER.md](CHARTER.md) · ADR-0012 §3 |
| **design truth** | The authoritative, ordered, replayable command log (the `Document`) that every other piece of state is downstream of. Owned by `Engine.*`. | concept · [CHARTER.md](CHARTER.md) · ADR-0004 |
| **the two kernels** | `Engine.*` (design truth) and `3DEngine.Core` (render state) — peers that never reference each other; render hosts own the projection from events. | [CLAUDE.md](../CLAUDE.md) "Authority diagram" · ADR-0009 |
