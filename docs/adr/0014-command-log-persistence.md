# ADR 0014 — Command-log persistence for `engine-api-http`

## Status

Accepted — 2026-05-27

## Context

ADR-0010 §4 settled the subscription-reset snapshot as a transport projection and explicitly NOT a persistence format. ADR-0011 §Open challenges flagged: "A separate persistence ADR is still open. Its design assumes server-default (the engine process owns the durable state); embedded mode either gets stripped-down persistence or stays ephemeral." `docs/roadmap.md` lists Persistence under V2 P8; the persistence question has been deferred since v0.5 (replay-determinism gate) made the underlying invariants concrete.

As of v0.11 every state-bearing component in the engine is in-memory:

- `Document` (metadata, `Log`, `Bodies`) — `Engine.Core/Document.cs`, lives in `CommandBus`.
- `Log` — the applied-command history, mutated only inside `CommandBus.Apply`.
- `Bodies` — handle/kind projection, mutated only inside the commit section per ADR-0012 §3.
- `InMemoryEventSink` — bounded ring, ADR-0005 §6.
- `IdempotencyCache` — FIFO of 1024, ADR-0006 §7.
- `InProcessMeshBackend` — `Dictionary<BodyHandle, BoxRecord>`, ADR-0012 §7.

Restart `engine-api-http` and every Document, every body, every cached idempotent reply is gone. For the CLI this is a non-issue — each `engine apply` invocation is its own ephemeral session by design (ADR-0002, ADR-0011 §5). For `engine-api-http`, server-default per ADR-0011 §1, it is the gap between "a process you launch" and "a service operators can run." Restart-survivable Document state is the precondition for treating `engine-api-http` as an actual service.

The clamps from the CLAUDE.md V1 scope section say "No persistence — in-memory only until persistence ADR + TASK." This ADR is that ADR. Its job is to pin the persistence model for V1.x: what is durable, what is recoverable, what is intentionally not.

The kernel already has the property that makes persistence simple: ADR-0001 §4 ("backends are caches; losing the cache is recoverable by replay") + ADR-0005 §5 (replay is deterministic modulo `Timestamp`/`DocumentId`) + the v0.5 replay-determinism gate (CI proves the determinism holds, extended in v0.11 to cover `CreateBox`). If the command log is durable, every other piece of state is re-derivable from it on startup. This ADR rides that property.

## Decision

### 1. The command log is the source of durable truth

The on-disk artifact is a single append-only file: the **command log**. It mirrors `Document.Log` — every `Command` that the bus has announced as `Applied`, in `Seq` order, durably written before the bus returns the `CommandResult` to the client.

Nothing else is persisted in V1.x:

- **Not events.** They are derivations (`command.applied`, `body.created`). Replay regenerates them.
- **Not the idempotency cache.** It is a session-local optimization (ADR-0006 §7). After a restart, retrying clients with old `CommandId`s see a fresh cache — they apply normally or hit replayed entries naturally, since the bus re-warms the cache during startup replay.
- **Not the event ring.** It is bounded by design (ADR-0005 §6). After a restart, the ring starts fresh; replay re-emits events into it; older events are gone, same as before. The WebSocket `subscription.reset` protocol (ADR-0010 §2) already handles cursors that fall outside the ring.
- **Not backend state.** Per ADR-0001 §4 and ADR-0012 §2, backends are caches. A fresh backend + replay reproduces every body.
- **Not snapshots.** No periodic `Document` snapshot, no compaction — V1.x. Reasoning under §6.

Rejected commands and Cancelled commands are NOT written to the log. Only `Applied` commands are durable. Rejection is a transport-level event (ADR-0005 §3); it leaves no state to persist.

### 2. Durability semantics: fsync inside the commit section

The bus's serial section (ADR-0006 §1) gains one new step:

```
validate → handler → events drafted → write-to-log + fsync → commit → return
                                       │
                                       └─ persistence happens here
```

Specifically: after the handler succeeds and the bus has decided the command is `Applied`, but before the bus mutates `Document.Log` / `Document.Bodies` / `Document.Version` and returns the `CommandResult`, the bus appends the command record to the on-disk log and calls `fsync` (or the platform equivalent). Only then does the bus commit in-memory state and announce the result.

Consequences:

- A client that has received `Status = Applied` has a durable guarantee. The command survives a crash.
- A crash before fsync leaves the command absent from both the in-memory state (rolled back inside the bus) and from disk. The client sees no `CommandResult` — its retry with the same `CommandId` (ADR-0006 §7) gets a fresh attempt.
- Write latency is added to every applied command. V1.x accepts this — operator-grade durability beats throughput for the first persistence slice. Performance-oriented batching is a future ADR.

A write failure (disk full, IO error) inside this step turns the command into `Rejected` with `Error.Code = E-IO-WRITE-FAILED`. The in-memory Document is untouched. The serial section preserves its atomic-commit-at-end property (ADR-0006 §1) — partial state never escapes the bus.

### 3. File format: JSON Lines with a header

The on-disk file is JSON Lines (one JSON object per line, UTF-8, LF newlines). Two record types in V1.x:

```
{"schemaVersion":1,"type":"log-header","documentId":"<guid>","createdAt":"<iso8601>"}
{"schemaVersion":1,"type":"command-applied","appliedAtSeq":1,"command":{...}}
{"schemaVersion":1,"type":"command-applied","appliedAtSeq":2,"command":{...}}
```

- **First record is always `log-header`.** It carries `documentId` (so the in-memory `Document` reconstitutes with the same Guid on startup; satisfies operator expectations) and `createdAt` (advisory, ADR-0005 §1).
- **Subsequent records are `command-applied`.** The `command` field is the *same JSON shape `POST /commands` accepts* — `{ name, version, commandId, parameters }`. Reusing the wire shape means the registry can deserialize each entry through the same path the HTTP endpoint uses; `cat | jq` is enough to inspect.
- `appliedAtSeq` is redundant with replay-derived order but cheap; it gives operators a quick way to spot gaps and lets startup detect corruption (next record must increment by 1).
- `schemaVersion` is on every record. A future format change can be detected on load and refused with `E-IO-LOAD-FAILED`.

Field names are camelCase JSON, matching the HTTP-API and the snapshot conventions (ADR-0010 §2).

Binary formats (BSON, MessagePack, protobuf) are rejected for V1.x. They would add a dependency and obfuscate the log for negligible payoff at V1.x scale. Revisit when log size or write throughput becomes a real problem.

### 4. Startup: replay rebuilds everything

On `engine-api-http` startup, given a configured log path:

1. If the file does not exist, create it, write the `log-header`, the engine is a fresh Document.
2. If the file exists, read it line-by-line:
   - Header line → restore `documentId`.
   - Each `command-applied` line → deserialize via the registry → submit through `Replay.ReplayLog` against a fresh backend and a fresh `InMemoryEventSink`.
3. If a trailing record is malformed (partial write from a crash mid-fsync), truncate it. Log `W-IO-TRUNCATED` with the byte offset and the rejected partial record. The bus had not announced `Applied` for that command, so dropping it is safe (per §2).
4. If a non-trailing record is malformed, abort startup with `E-IO-LOAD-FAILED`. Operator action required. The engine does not run with a partially-readable log.
5. Once replay completes, the engine is in pre-shutdown state: same `Document.Version`, same `Document.Log` contents, same `Document.Bodies`. The HTTP and WebSocket surfaces come up; clients connect and proceed.

Replay-derived `EventRecord.Timestamp` values reflect the restart time, not the original wall-clock. ADR-0005 §1 already declares timestamps advisory. Clients that surface timestamps for humans get post-restart values; clients that rely on `Seq` for ordering are unaffected.

The idempotency cache is re-warmed naturally by the replay path — each replayed command lands a `CommandResult` in the cache via the same code path as live application. After replay completes, the cache holds the most recent ≤1024 results, exactly like before shutdown (minus the FIFO order being reset to insertion order, which is a non-observable difference).

### 5. Scope: `engine-api-http` only in V1.x

Persistence is wired only into `engine-api-http`. `Engine.Cli` stays ephemeral — no log file, no startup replay. Reasoning:

- The CLI is canonical for embedded / single-invocation use (ADR-0011 §5). Adding a persistence flag invites file-locking conflicts with a running server and complicates the canonical-test surface for marginal value.
- Embedded mode persistence is plausible but currently uncalled-for. If a use case appears (a long-running embedded host that wants survivability), it gets its own ADR.

ADR-0011 §2 third bullet — "no persistence between client invocations is required (V1 clamp; revisits when persistence lands)" — is narrowed here: the clamp lifts for `engine-api-http`; it stays in force for `Engine.Cli`.

### 6. Snapshots and compaction are deferred

A standard event-sourcing pattern is "periodic snapshot + log tail since snapshot" to bound recovery time. V1.x does not do this. Reasoning:

- V1.x command counts are small (single-digit to low-hundreds per session). Replay-from-genesis is sub-second.
- Snapshotting adds: when-to-snapshot policy, snapshot format (potentially divergent from ADR-0010's wire snapshot), atomicity guarantees during snapshot writes, compaction policy, retention of old log segments for rollback. Each is its own design call.
- ADR-0010 §4 already says the wire snapshot is not a persistence format. A persistence-side snapshot would be a *third* serialization of Document state; deferring it avoids prematurely picking one.

When log size, startup latency, or compaction becomes a measured problem, a follow-on ADR adds the snapshot mechanism. Until then: append-only command log, replay-from-genesis on startup.

### 7. Configuration

The log path is operator-supplied at server startup. V1.x:

- Command-line flag and environment variable (e.g. `--log-path` and `ENGINE_LOG_PATH`); precise spelling is a TASK detail.
- If neither is set, the server runs in **ephemeral mode** — no file is opened, no replay, no persistence. Used by tests and dev runs. Logged at startup with `I-IO-EPHEMERAL` so operators are not surprised.
- One log file per process. Multi-Document, multi-tenant, and multi-process variants are V2 (ADR-0011 §Non-goals).

The file is opened with an exclusive lock on creation/open. A second `engine-api-http` process pointed at the same path fails fast with `E-IO-WRITE-FAILED` at startup. This prevents two engines from interleaving writes to the same log.

### 8. Schema version handling

A command's `Name@Version` is part of its serialized form (the same shape the HTTP API accepts). On replay, the registry must contain a handler for every `(Name, Version)` tuple present in the log. If a handler has been removed (a `CreateBox@1` whose handler was replaced by `CreateBox@2`-only), startup aborts with `E-IO-LOAD-FAILED` citing the missing handler.

Command-version deprecation, migration of old log records to newer command shapes, and "frozen" old handlers preserved only for replay are all out of scope for this ADR. Operators are responsible for keeping handlers around for any command versions that appear in their log. A future ADR addresses migrations when the question first becomes real.

## Consequences

- **`engine-api-http` becomes restart-survivable.** Clients that disconnect across a server restart reconnect to the same `documentId`, the same `Document.Version`, and the same set of bodies. WebSocket reconnect (ADR-0010 §2) handles cursor placement against the fresh in-memory ring.
- **`CommandBus` evolves.** Its constructor gains a `ICommandLog` (or equivalent) dependency. The default in tests and the CLI is `NullCommandLog` (no-op writes). `engine-api-http` wires the real file-backed implementation. The serial section adds the write+fsync step under §2. No public-API change to `Engine.Contracts`; the `ICommandLog` abstraction lives in `Engine.Core`.
- **Startup path changes.** `engine-api-http` reads its log and replays before the HTTP listener accepts connections. Startup latency now scales with log size; V1.x sub-second at expected scales.
- **`Engine.Cli` is unaffected.** No flag, no file, no replay — stays embedded-ephemeral per ADR-0011 §5.
- **Test posture.** A new persistence-roundtrip test fixture: apply N commands → drop the engine → restart against the same log → assert `Document.Version`, `Document.Bodies`, `Document.Log` match (modulo `Timestamp`/`DocumentId` per ADR-0005). This composes with the existing replay-determinism gate.
- **Three new diagnostic codes** (registered in `docs/diagnostics.md`, `DiagnosticCodes.cs`, and `/schema/diagnostics` in the same change):
  - `E-IO-WRITE-FAILED` — disk write failure inside commit; command Rejected, in-memory state untouched.
  - `E-IO-LOAD-FAILED` — startup load failure (missing handler, malformed non-trailing record, schema-version mismatch, lock contention). Server does not start.
  - `W-IO-TRUNCATED` — startup detected and truncated a partial trailing record.
  - `I-IO-EPHEMERAL` — informational, emitted at startup when no log path is configured.
- **V1 clamp updates.** CLAUDE.md's "No persistence" clamp narrows: persistence lands for `engine-api-http`; it stays clamped for `Engine.Cli`. The roadmap moves P8 partially — the persistence portion of P8 is now V1.x territory; multi-Document and undo/redo remain V2.

## Non-goals

- **Snapshots and compaction.** Deferred per §6.
- **Multi-Document persistence.** One log file = one Document. Multi-Document is V2 (ADR-0011 §Non-goals).
- **Multi-process / multi-writer safety.** Exclusive-lock-on-open is sufficient — concurrent writers are detected, not coordinated.
- **Persistence for `Engine.Cli`.** Out of V1.x per §5.
- **Encryption-at-rest.** Operator's filesystem-layer concern.
- **Cross-version log migration.** Operators keep the handlers; we abort if they don't.
- **WAL-style "torn write" recovery beyond truncate-trailing.** The fsync-per-applied-command discipline plus append-only writes makes torn writes only possible at the trailing edge.
- **Performance-tuned batched fsync.** Per-command fsync in V1.x; batching is future work with its own latency/durability trade-off ADR.
- **Persisting Rejected commands** for diagnostics. They surface as events at runtime (ADR-0005 §3); the log is for replay, not audit.
- **Replacing `Document.Log` with disk reads.** The log stays in memory; disk is its durable mirror.

## Validation rules

1. Every command for which the bus returns `Status = Applied` MUST be present in the on-disk log before the result is returned to the client. CI: a fixture that intercepts the write call asserts it happens inside the commit section before `CommandResult` is yielded.
2. A restart from the on-disk log + an empty backend + an empty event sink MUST reproduce the pre-shutdown `Document.Version`, `Document.Log` contents (modulo `Timestamp`), and `Document.Bodies`. CI: persistence-roundtrip fixture (described in §Consequences).
3. The first record in the file is always `log-header`; subsequent records are `command-applied` with monotonic `appliedAtSeq` starting from 1. CI: format-gate test parses a freshly-written log and asserts the invariant.
4. A second `engine-api-http` process pointed at the same log path MUST fail fast on startup. Manual test acceptable for V1.x; CI variant is a TASK detail.
5. `Engine.Cli` invocations MUST NOT open, read, or write any persistence file. CI: a test runs the CLI binary with no env vars and asserts no file IO outside the working directory's stdout/stderr.
6. The four new diagnostic codes (`E-IO-WRITE-FAILED`, `E-IO-LOAD-FAILED`, `W-IO-TRUNCATED`, `I-IO-EPHEMERAL`) MUST appear in `docs/diagnostics.md`, `Engine.Core/DiagnosticCodes.cs`, and `/schema/diagnostics` — enforced by the existing P2 + `SchemaEndpointGateTests` gates.

## Open challenges

- **Snapshot/compaction trigger.** When the log gets large enough that startup latency or disk usage matters, the snapshot ADR lands. The trigger (line count, byte size, time interval) is a future call.
- **Embedded persistence for the CLI.** Some users will want a CLI that persists across invocations. The use case has not arrived; defer until it does.
- **Multi-Document persistence.** When the engine grows beyond one Document per process, the log layout (per-Document file? single file with `documentId` per record?) needs a call. V2.
- **Command-version migration.** When a `CreateBox@1` in the log needs to become a `CreateBox@2`, the registry needs a migration step. V2 at earliest; V1.x relies on operator discipline.
- **Disk-format evolution.** `schemaVersion` is in every record so the load path can detect a mismatch and refuse. The actual upgrade mechanism is unspecified — when the second format lands, that ADR also specifies the upgrade tool.
- **Backup / restore tooling.** The log is a single file; `cp` works. A real ops surface (point-in-time recovery, hot backup) is later.
