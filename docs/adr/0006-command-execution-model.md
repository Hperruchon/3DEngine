# ADR 0006 — Command Execution Model

## Status

Accepted — 2026-04-28

## Context

The CommandBus is the only mutator (ADR 0004). To remain that *and* support real-world operations, the execution model must answer:

- Are commands synchronous or asynchronous?
- How do long-running operations report progress?
- How are they cancelled?
- What happens when commands arrive faster than the engine can apply them (back-pressure)?
- What happens if two clients submit conflicting commands at once?

Choosing the *simplest correct answer* for V1 matters more than choosing the most flexible one. Concurrency, in particular, is the wrong place to be clever early.

## Decision

### 1. Commands apply atomically; results are exactly one of three states

```
CommandResult.Status ∈ { Applied, Rejected, Cancelled }
```

- **Applied** — the Document moved forward by exactly one log entry; events were emitted.
- **Rejected** — the Document is unchanged; a structured error is returned; an event `command.rejected` is emitted.
- **Cancelled** — the Document is unchanged; the command was abandoned mid-execution; `command.cancelled` is emitted.

There is no partial application. A command either lands or does not. This is the strongest invariant in the system and is non-negotiable.

### 2. The bus is async on the surface, serial in execution

```
Task<CommandResult> Apply(Command, CancellationToken)
```

- **Async surface.** Submitters get a `Task` and may await. CLI, HTTP, AI agents, desktop UI all use the same shape.
- **Serial execution per Document.** The engine applies one command at a time. A second submission queues behind the first. This is V1's concurrency model — period.
- Serial execution removes an entire class of races (interleaved kernel calls, partial event interleaving, conflicting transforms) at the cost of throughput. V1 is not throughput-bound; it is correctness-bound.

### 3. Long-running commands report progress on the event stream

- Long-running commands (boolean of large meshes, future imports) emit `command.progress` events while executing, with the same `CauseCommandId` as the eventual `command.applied`.
- Progress events carry: `fraction ∈ [0,1]?`, `phase: string?`, `message: string?`. All optional; absence means "still working."
- Progress events flow through the same stream and the same back-pressure rules (ADR 0005). A slow subscriber may miss intermediate progress events but will still receive the final `command.applied` (subject to ring retention).
- Progress is advisory. It is not a guarantee that the command will succeed.

### 4. Cancellation

- Every `Apply` accepts a `CancellationToken`.
- A command handler **may** observe the token. Handlers that don't observe it are non-cancellable; that's a property of the command, not a bug.
- Each `Command` declares its `Cancellable` boolean in metadata. Clients use this to decide whether to show a cancel UI.
- On cancel: the engine rolls back any in-progress kernel work that hasn't already mutated the Document. Because Document mutation is the *last* step of a successful apply (commit-at-end), cancellation before commit is clean. Cancellation after commit is impossible — once committed, the only path back is a compensating command.

### 5. Back-pressure on submission

- The bus has a **bounded inbound queue** (V1 default: 64 pending commands).
- Submitters that overflow receive `CommandResult.Rejected` with reason `bus.busy`. The command is not queued; the client decides whether to retry.
- This applies symmetrically to all clients. There is no priority lane for the desktop UI in V1.

### 6. Conflicting submissions

- Because execution is serial, "conflicting" reduces to "out of order." Commands that depended on prior state observed by the client may arrive after that state has changed.
- Each command may carry an optional `expectedDocumentVersion` (the `Seq` of the last event the client observed before submitting).
- If `expectedDocumentVersion` is set and does not match the engine's current version at apply time, the command is `Rejected` with reason `version.stale`.
- If unset, the engine applies optimistically. This is fine for fire-and-forget commands (e.g., `CreateBox` at given coords). It is dangerous for stateful edits (e.g., "modify entity X"). Clients are responsible for choosing.

### 7. Idempotency

- Commands carry a `CommandId : Guid`.
- The engine deduplicates within a short window (V1 default: most recent 1024 applied command IDs). A duplicate `CommandId` returns the cached `CommandResult` of the prior application without re-executing.
- This protects against transport retry storms (HTTP/WS reconnects), not against intentional repeats — a client that wants to apply the same logical command twice generates two `CommandId`s.

## Consequences

- The desktop UI's drag/gizmo flow commits a single command on release, not many during the gesture (ADR 0007).
- Importing a large model is a single command that may take seconds; the UI shows progress via the event stream; cancellation works only if the import handler observes the token.
- AI agents can submit a batch by awaiting each command in sequence. Concurrent multi-agent edits to one Document are not supported in V1; they must serialize externally.
- The HTTP API exposes one command at a time per connection. WebSocket multiplexing of in-flight commands is not modeled.

## Non-goals

- Parallel command execution within a Document. Rejected for V1.
- Multi-Document parallelism. Out of scope for V1 (single Document per runtime).
- Distributed transactions across commands.
- Compensating-command auto-generation for cancellation-after-commit. Clients must explicitly issue an inverse command.
- Speculative execution / preview-as-command. Previews are client-side state (ADR 0007).
- Priority queues, fairness policies, deadlines.

## Validation rules

1. The Document changes only as the *last* step of a successful apply. Any kernel-side mutation visible before commit is a bug.
2. CI: a "cancel before commit" test confirms a long-running command observing the token leaves the Document untouched.
3. CI: a "queue overflow" test confirms the 65th simultaneous submission returns `bus.busy` and is not queued.
4. CI: a "stale version" test confirms a command with a mismatched `expectedDocumentVersion` is rejected.
5. CI: an "idempotency" test submits the same `CommandId` twice and asserts one application + cached result on the second call.
6. Every command type declares its `Cancellable` flag in the registry.

## Open challenges

- **Compensating commands as the only undo path post-commit** is theoretically clean but requires every command to have a defined inverse. V1 punts: undo/redo is implemented as log truncation + replay (cheap, correct, slow on long logs). Compensation arrives if/when log replay becomes too slow.
- **Long-running commands holding the serial slot** stall every other client. If this hurts UX in practice, the answer is *not* parallelism — it is breaking the long command into smaller steps. Architectural pressure points the right direction.
- **Per-client back-pressure** vs the current global bus queue: with multiple clients, one noisy client can fill the 64-slot queue. Mitigation if observed: per-client quotas; deferred until needed.
