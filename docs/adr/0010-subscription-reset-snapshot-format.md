# ADR 0010 — `subscription.reset` snapshot format

## Status

Accepted — 2026-05-12

## Context

ADR-0005 §5 specifies the WebSocket reconnect protocol. When a client's cursor is older than the in-memory ring's tail, the engine cannot stream the missing events; it responds with `subscription.reset { documentId, snapshot }`, then resumes live events from `current+1`. The client discards its prior in-memory event-derived state and rebuilds from the snapshot.

The shape of `snapshot` was left as an Open Challenge in ADR-0005:

> **Snapshot format for `subscription.reset`.** Provisionally: a serialized Document header + materialized scene summary. Final shape blocked on the persistence ADR (TASK-0002 territory).

This blocked P6.3 (WebSocket events) on a persistence ADR that is significantly larger than the WebSocket task warrants. The two concerns share the question *"what does a serialized `Document` look like?"* but they are separable:

- **Snapshot** is an in-memory, in-protocol transport projection. Bounded by what a connected client needs.
- **Persistence** is on-disk, durable, recoverable across processes. Bounded by what every future restart must reload.

This ADR settles the snapshot scope for V1.x so P6.3 can proceed. Persistence remains pending its own ADR; whichever lands second adapts to the other.

## Decision

### 1. The snapshot is a transport projection of `Document`'s public surface

A `subscription.reset` carries a `snapshot` object that is the JSON projection of `Engine.Contracts/Document` at a specific `Seq`. It is authoritative for rebuilding: a client that takes the snapshot as ground truth and resumes from `current+1` reaches an equivalent state, without needing the events between its prior cursor and `current`.

### 2. V1.x snapshot shape

```
subscription.reset payload
  documentId : Guid       // engine's Document.DocumentId, same as the event header
  snapshot   : object
    documentId      : Guid
    projectId       : Guid?
    schemaVersion   : integer
    version         : long     // Document.Version at the moment of the reset
    createdAt       : DateTime // UTC, engine clock
    updatedAt       : DateTime // UTC, engine clock
```

Field names are camelCase JSON, matching the existing HTTP-API conventions. Nulls are preserved.

The snapshot does NOT include:

- **`Document.Log`** — the historical command list. The reset's purpose is the current state, not the history. Replay-from-disk is the persistence ADR's concern. Including Log on every reset scales linearly with command count and is wasteful for clients that only need the current state.
- **Render-side state** (`Scene`, `Camera`, `Light`, materials). Those live in `3DEngine.Core` per ADR-0009; the engine never projects them. A rendering host derives them from the snapshot + live events.

### 3. Extensibility

When a concrete geometry backend lands (P7), the snapshot extends with the authoritative geometry state — minimally, the list of `BodyHandle`s known to the Document. The addition is additive and forward-compatible:

- Existing clients that ignore unknown fields continue to work.
- New clients that depend on the new field require the new server.

If the geometry state grows too large to ship inline (mass-import scenes, many MB), a follow-up ADR introduces a "fetch-by-handle" pattern where the snapshot ships a list of handles and the client queries each. That decision waits until a real workload forces it.

### 4. The snapshot is NOT a persistence format

Same as ADR-0005's framing of events: this is a transport projection, not a durable record. The persistence ADR may reuse some of the same field shape or define its own; it is not bound by the snapshot shape, and changes to the persistence format must not silently change the snapshot wire shape (each surface owns its evolution).

### 5. Time fields are advisory

`createdAt` and `updatedAt` follow the same status as `EventRecord.Timestamp` in ADR-0005 §1: engine clock, UTC, advisory only. They are not used for ordering or replay correctness; clients render them for humans.

## Consequences

- **P6.3 unblocks.** The WebSocket TASK can size against this shape without waiting for persistence.
- **Snapshot is small in V1.x.** A few hundred bytes of JSON. Reset cost is negligible.
- **Snapshot grows with the Document.** Once P7 adds bodies, the snapshot size becomes proportional to the design size, which is acceptable for V1.x scale. The fetch-by-handle decision waits for evidence that inline is too big.
- **Persistence ADR is still future work.** Nothing here precludes its design.

## Non-goals

- Defining the persistence format. Separate ADR when scoped.
- Defining the snapshot of a Document that has never been mutated (V1.x: `Version=0`, no commands applied). The shape above already covers it — every field has a defined V1.x value, including `version: 0`.
- Compressing the snapshot. Premature; revisit if WS payload sizes become a real problem.
- Snapshots at arbitrary historical `Seq` (i.e., "give me the Document as it was at Seq=12"). The protocol only delivers a snapshot at the *current* Seq.
- A separate "header" vs "summary" partition (ADR-0005's "Provisionally" sketch). Single flat object is simpler and the data justifies no partition today.

## Validation rules

1. The `snapshot.documentId` MUST equal the outer `documentId` of the `subscription.reset` event.
2. The `snapshot.version` MUST equal the highest `Seq` the engine has emitted at the moment of the reset.
3. A client that consumes a `subscription.reset` and discards prior state, then applies live events from `current+1`, reaches a state equal to a client that received all events from `Seq=1`. *Equal* here is modulo `Timestamp`/`DocumentId` per ADR-0005.
4. The snapshot omits `Document.Log`. CI in P6.3 asserts the wire shape does not include a `log` field for V1.x.

## Open challenges

- **Geometry snapshot shape.** When P7 lands the first `IGeometryBackend` + a `CreateBox`-style command, `Document` gains entities/bodies. The snapshot extends; the gate test in P6.4 (`/schema/commands` invariant) doesn't catch a missing snapshot field today. Add a snapshot-coverage gate when state actually grows.
- **Persistence reuse.** If the persistence ADR chooses a Document serialization different from this snapshot, the engine maintains two projections. That is fine — different surfaces, different shapes — but worth re-reading this ADR when persistence lands to confirm intentional divergence.
- **Snapshot at request.** Clients may want to ask "give me the current snapshot without subscribing." That is a query, not a subscription event. If the use case appears, size it as a query (e.g. `DocumentSnapshot`); out of scope here.
