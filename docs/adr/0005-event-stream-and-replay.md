# ADR 0005 — Event Stream and Replay Protocol

## Status

Accepted — 2026-04-28

## Context

ADRs 0002, 0003, and 0004 all depend on an event stream as the engine's primary observability surface. Without a fixed protocol — sequence numbering, ordering, retention, reconnect — every client (Blazor, CLI `--follow`, AI agents, future plugins) reinvents recovery logic, and small protocol drift produces undebuggable divergence.

This ADR fixes the stream's shape and the recovery rules. It does **not** define the wire format details (HTTP framing, WebSocket binary vs text); those are an implementation choice that follows from the contract here.

## Decision

### 1. Event shape

A single `EventRecord` type, discriminated by `Kind`. No type hierarchy.

```
EventRecord
  Seq           : long          // 64-bit, monotonic per Document instance
  Timestamp     : DateTime UTC  // engine clock, advisory only
  DocumentId    : Guid          // identifies the Document instance
  CauseCommandId: Guid?         // null for engine-internal events
  Kind          : string        // e.g. "command.applied", "command.rejected",
                                //      "command.progress", "document.loaded",
                                //      "document.replayed", "validation.report"
  Payload       : object        // shape determined by Kind, schema-published
```

- `Seq` is **the** identity of an event. Timestamps are not unique and are not used for ordering.
- The engine never re-emits an event with the same `Seq`. If the Document is reloaded, a new `DocumentId` is issued and `Seq` restarts from 1.

### 2. Ordering guarantees

- **Total order per Document.** All events with the same `DocumentId` are delivered to all subscribers in `Seq` order, with no gaps.
- **No global order across Documents.** V1 runs one Document per Engine Runtime; multi-document is post-V2.
- A subscriber that observes `Seq=N` is guaranteed to have observed `1..N-1` already (or to receive them before any `Seq>N`).

### 3. Retention

- **In-memory ring buffer** of the most recent `N` events per Document (V1 default: `N = 10_000`, configurable).
- **Command log on disk** is the long-term recovery substrate. The command log is not the event stream, but it is sufficient to *reconstruct* an equivalent event sequence by replay.
- Events are **not persisted as events**. Only commands are persisted. This is deliberate: the command log is the source of truth (ADR 0004), events are a derived projection.

### 4. Replay-from-cursor

A client subscribes by passing a cursor: `(DocumentId, lastSeenSeq)`. The engine responds in one of three ways:

1. **Resume.** `lastSeenSeq` is within the in-memory ring. The engine streams `lastSeenSeq+1..current` then continues live.
2. **Reconstruct.** `lastSeenSeq` is older than the ring's tail. The engine responds with `subscription.reset` carrying the new `DocumentId` (if changed) and the current Document state snapshot. The client discards its in-memory event-derived state and rebuilds from the snapshot. Live events resume from `current+1`.
3. **Unknown.** `DocumentId` does not match the running Document. Same as Reconstruct, with a warning.

There is no "replay from arbitrary historical Seq beyond the ring." Older state is recovered by Document reload, not event replay.

### 5. Reconnect protocol

1. Client opens transport (WebSocket).
2. Client sends `subscribe { documentId?, lastSeenSeq? }`.
3. Engine responds with one of:
   - `subscription.resume { fromSeq }` followed by buffered + live events.
   - `subscription.reset { documentId, snapshot }` followed by live events.
4. Heartbeat: engine sends a `heartbeat` event at most every 30 s when the stream is otherwise idle. No application meaning; transport keepalive only.
5. If the client sees a gap in `Seq` (jumps), it MUST treat it as a transport bug and reconnect. The engine never produces gaps.

### 6. Back-pressure

Subscribers are not allowed to slow the engine.

- The engine maintains a **per-subscriber bounded outbound queue** (V1 default: 1024 events).
- If the queue overflows, the engine **disconnects that subscriber** with a structured reason `subscriber.lagged`. The engine does not block, drop arbitrary events, or coalesce.
- A lagged client reconnects and follows the cursor protocol. If its cursor is now behind the ring, it gets a Reconstruct.

This is the only acceptable back-pressure design for an authority that serves multiple unequal clients. Slow Blazor in the browser must not pause the desktop app.

### 7. What may emit events

- The `CommandBus` after applying a command (`command.applied` or `command.rejected`).
- A long-running command in flight (`command.progress`) — see ADR 0006.
- Document lifecycle (`document.loaded`, `document.replayed`, `document.saved`).
- Validation/debug subsystems (`validation.report`).
- The runtime itself for transport (`heartbeat`, `subscription.*`).

Anything else introducing a new `Kind` requires a contracts PR with an updated schema.

## Consequences

- Event volume is bounded and well-defined. Clients can size buffers.
- Event-stream consumers cannot accumulate stale state without bound: they either keep up, or get reset.
- The command log remains the only durable record. Loss of the in-memory ring is not data loss.
- Multi-document, federated runtimes, and live collaboration are *not* served by this protocol. They will need an ADR amendment when scoped.

## Non-goals

- Cross-Document ordering.
- Persistent event journal on disk (command log fills that role).
- At-most-once or at-least-once semantics across reconnects — the protocol is exactly-once *within a connection*, with explicit Reset on cursor miss.
- Filtering or topic-subscription per client. V1 streams everything; clients filter locally.
- Per-Kind retention policies.

## Validation rules

1. Every event the engine emits has a unique strictly-increasing `Seq` per `DocumentId`.
2. CI: a "subscriber lag" test starts a slow consumer, asserts it is disconnected, asserts the engine continues serving fast consumers without gaps.
3. CI: a "reconnect resume" test verifies a client with a recent cursor receives buffered events and no Reset.
4. CI: a "reconnect reset" test verifies a client with a stale cursor receives a Reset and a snapshot.
5. The schema for every `Kind` is published at `/schema/events` (when the HTTP API exists). New `Kind`s without schema entries fail CI.
6. The engine never reorders, drops, or duplicates events within a single subscriber connection.

## Open challenges

- **Snapshot format for `subscription.reset`.** Provisionally: a serialized Document header + materialized scene summary. Final shape blocked on the persistence ADR (TASK-0002 territory).
- **Maximum reasonable event rate.** If commands generate >1k events each (e.g. mass import), the ring fills quickly and slow clients reset frequently. Mitigation deferred until a real workload exhibits it.
- **Multi-Document story.** When V2 introduces multiple Documents per runtime, `DocumentId` filtering on the subscription becomes a feature. Out of scope here.
