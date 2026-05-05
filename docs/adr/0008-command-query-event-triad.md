# ADR 0008 — Command, Query, Event Triad and Structured Results

## Status

Accepted — 2026-04-28
Amends ADR 0006 (`CommandResult` shape extended; serial execution and atomicity unchanged).

## Context

ADR 0006 modeled `CommandResult` as a status enum with optional fields. That is insufficient: real clients (CLI, Blazor, AI agents) need to consume structured outputs — generated entity ids, validation reports, warnings, error chains — without UI assumptions. Treating results as an afterthought forces every client to invent its own conventions, defeating the headless-first goal of ADR 0002.

Equally, the system has been talking about commands as if they covered *everything* a client might ask the engine to do. They don't. A request for "the bounding box of entity X" is not a state change; routing it through the command bus pollutes the log with reads, fights serial execution, and conflates two different operations.

Common command-based architectures (CQRS, gen_server's `call`/`cast`, Elm `Cmd`/`Msg`/`Model`, Redux actions + selectors) all converge on the same answer: separate intent (Command), inspection (Query), and observation (Event). This ADR commits to that triad and fixes the contracts.

## Decision

### 1. The triad

| Concept | Purpose | Mutates state? | Logged? | Replayable? | Surfaces in event stream? |
|---|---|---|---|---|---|
| **Command** | Intentional state-change request | Yes (atomically, or not at all) | Yes (durable log) | Yes | Yes (`command.applied` / `command.rejected` / `command.progress` / `command.cancelled`) |
| **Query** | Read state / inspect Document | No | No | No (re-runnable, not replayed) | No |
| **Event** | Observation of what happened | No (it *describes* mutation) | No (derivable from log) | No (regenerable by replay) | Yes — events *are* the stream |

Each has a separate registry, a separate transport surface, and a separate schema namespace.

### 2. `CommandResult` as a first-class contract

```
CommandResult
  CommandId        : Guid              // echoed from submission
  CommandName      : string            // echoed
  Status           : Applied | Rejected | Cancelled
  AppliedAtSeq     : long?             // event Seq of command.applied; null otherwise
  DocumentVersion  : long              // engine's Seq at result time
  Outputs          : Outputs           // schema-defined per command, see §3
  Diagnostics      : Diagnostic[]      // see §4 — may include warnings on Applied
  Error            : ErrorDetail?      // present iff Status = Rejected
  DurationMs       : long              // engine-measured, advisory
```

Rules:

- **Always present.** Every `Apply` returns a `CommandResult`. There is no void result.
- **Self-describing.** `CommandName` and a schema reference are sufficient to interpret `Outputs` without prior knowledge of the command.
- **Stream-correlatable.** `AppliedAtSeq` ties the result back to the event stream so clients can wait for, or fast-forward to, the corresponding event without race.
- **Diagnostics ≠ Error.** A successful command can emit warnings; only rejected commands have an `Error`. See §4.

### 3. `Outputs` — typed at the wire by schema, generic in code

```
Outputs : map<string, value>      // value = JSON-shaped (string/number/bool/array/object/null)
```

- Each command kind publishes its `Outputs` schema at `/schema/commands/{name}@{version}`.
- C# clients may codegen typed wrappers; the runtime representation is a map. AI agents and dynamic clients read the schema at runtime.
- Common conventions (recommendations, not enforced):
  - `createdEntityIds` for newly created Document entities.
  - `affectedEntityIds` for modified entities.
  - `removedEntityIds` for deleted entities.
  - Anything else is command-specific and must be schema-published.

This avoids both extremes: a giant typed `CommandResult<T>` hierarchy that every non-C# client has to mirror, and a free-form blob that nobody can rely on.

### 4. `Diagnostic` — structured, not strings

```
Diagnostic
  Severity : Info | Warning | Error
  Code     : string           // stable identifier, e.g. "E-GEOM-001"
  Message  : string           // human-readable, advisory only
  Path     : string?          // pointer into the input or document, e.g. "/parameters/radius"
  Data     : map<string,value>?  // structured details
```

Rules:

- **Codes are stable.** Once shipped, a `Code` does not change meaning. Codes are namespaced (`E-GEOM-`, `W-PARSE-`, `I-LOG-`).
- **Codes are documented.** A registry in `docs/diagnostics.md` maps code → meaning. PRs adding codes update the registry.
- **Severity allowed on success.** `Status = Applied` may carry `Warning` and `Info` diagnostics. Clients render them; they do not change application semantics.
- **No raw exceptions.** The engine never returns stack traces in diagnostics on a non-debug build. `Error.Cause` may chain structured causes; raw exception text is for logs, not the wire.

### 5. `ErrorDetail` — for rejection

```
ErrorDetail
  Code      : string         // namespaced, like Diagnostic.Code
  Message   : string
  Cause     : ErrorDetail?   // optional chain
  Retriable : bool           // hint for clients
```

`Status = Rejected ⇒ Error != null`. `Status = Applied ⇒ Error == null`. The engine never returns both.

### 6. Queries

Queries are first-class siblings of commands, not a special command kind.

```
Task<QueryResult<T>> Query<T>(Query, CancellationToken)

QueryResult<T>
  QueryName       : string
  AsOfDocumentVersion : long      // Seq at which the query was answered
  Result          : T
  Diagnostics     : Diagnostic[]
  Error           : ErrorDetail?
  DurationMs      : long
```

Rules:

- **Read-only.** A query MUST NOT mutate the Document, the backend cache, or any persistent state. Caching internal to the engine for performance is allowed; it must be invisible to clients.
- **No log entry, no event.** Queries do not appear in the command log or the event stream.
- **Snapshot-consistent.** A query observes the Document at a single `Seq`. It does not see partial commits.
- **Concurrent with each other (V1).** Queries may run in parallel with each other.
- **Serialized against commands (V1).** A query runs either before or after a command commit, never straddling. Concrete model: queries acquire a read on the current Document version; the next command commit waits for outstanding reads or proceeds against an immutable snapshot — implementation choice deferred, observable behavior fixed.
- **Schema-published.** `/schema/queries/{name}@{version}` defines `Result` shape.
- **Cancellable.** Long queries (large raycasts, mass tessellation) accept the token; non-observers are non-cancellable, declared in the registry.

Examples of queries (illustrative, not V1 scope):

- `GetEntity(id) → Entity?`
- `GetBoundingBox(id) → AABB?`
- `Tessellate(handle, tolerance) → Mesh`
- `ValidateDocument() → ValidationReport`
- `Raycast(ray) → Hit[]`

### 7. What may read what

| Caller | May read Document? | May read backend? | May read UI state? | May read other client's state? |
|---|---|---|---|---|
| Command handler | ✅ | ✅ via capability interface | ❌ | ❌ |
| Query handler | ✅ | ✅ via capability interface | ❌ | ❌ |
| Event subscriber (any client) | via API only | ❌ | n/a | ❌ |

A command handler reads only: its own parameters, the current Document, and the active backend's capabilities. *Nothing else.* In particular, no client UI state, no environment state, no I/O outside the engine's defined surfaces. Same rule for query handlers.

### 8. Sync vs async at the API surface

- Both commands and queries use `Task<...>` at the engine API surface.
- Submitters may await or fire-and-forget. Fire-and-forget is structurally identical to "submit, ignore the result"; the result still exists and can be retrieved by `CommandId` within the idempotency window (ADR 0006).
- Long-running operations report progress on the event stream (commands) or simply take longer (queries). There is no separate "async command" type.

### 9. Schema endpoints

When the HTTP API exists:

- `/schema/commands` — index of available commands, with versions.
- `/schema/commands/{name}@{version}` — full schema (parameters + outputs).
- `/schema/queries` — index.
- `/schema/queries/{name}@{version}` — full schema (parameters + result).
- `/schema/events` — index of event kinds + payload schemas.
- `/schema/diagnostics` — code registry.

Schemas are generated from the engine's registries, not hand-written. CI fails if a registered command/query/event lacks a schema entry.

## Consequences

- **`Engine.Contracts` grows.** New types: `CommandResult`, `Outputs`, `Diagnostic`, `ErrorDetail`, `Query`, `QueryResult<T>`, `IQueryHandler`, `QueryRegistry`. These are V1 contracts; they ship in TASK-0001 (see amendment note below).
- **TASK-0001 scope grows slightly.** The spine task must include the structured `CommandResult` (not the placeholder it had) and the `Query` half of the triad — at least the contracts and an empty registry. Implementation of any concrete query is out of scope for the spine.
- **Two registries, two surfaces.** Commands and queries are separate. CLI gets two verbs (`engine apply`, `engine query`). HTTP API has two route families (`POST /commands`, `POST /queries`).
- **Diagnostic code registry is real work.** Discipline early pays off; sloppy codes accumulated in V1 will hurt agents and tooling later.
- **Clients can be built schema-first.** The Blazor command submitter (ADR 0003) reads `/schema/commands` and renders forms. AI agents do the same. No hand-maintained DTOs for the dynamic surface.

## Non-goals

- A typed `CommandResult<T>` hierarchy in the engine. Outputs are schema-typed, not class-typed.
- Subscribing to query results (live queries / reactive queries). Queries are one-shot in V1.
- Mixing commands and queries in a single submission ("apply this and return the new state"). Clients chain the two; the result includes `AppliedAtSeq` for correlation.
- Per-client diagnostic localization. Messages are English; codes are the stable surface.
- Plugin-defined diagnostic codes in V1. Reserved namespace prefix (`X-`) for V2 plugin codes.

## Validation rules

1. Every command type registered has a schema entry under `/schema/commands`. CI fails missing entries.
2. Every query type registered has a schema entry under `/schema/queries`. Same.
3. `CommandResult.Status == Applied ⇒ Error == null`. Property-based test.
4. `CommandResult.Status == Rejected ⇒ Error != null`. Same.
5. Query handlers do not have write access to the Document or backend caches. CI dependency check: `IQueryHandler` implementations may not depend on `CommandBus` or any mutating API.
6. Diagnostic codes used in shipped code appear in `docs/diagnostics.md`. CI scan (string match on `E-` / `W-` / `I-` prefixes) reports unregistered codes.
7. Command handlers do not import any client/UI assembly. (Already enforced by ADR 0004.)
8. Queries do not appear in the event stream. Negative test asserts no event with `Kind = "query.*"` exists.

## Amendment to ADR 0006

ADR 0006 §1 (`CommandResult.Status ∈ { Applied, Rejected, Cancelled }`) is unchanged. The full structure of `CommandResult` is now defined in this ADR (§2). Where ADR 0006 referred to "structured error," that is now the `ErrorDetail` of §5. Where it implied progress events on the stream, the `command.progress` event Kind defined in ADR 0005 §7 carries the `CauseCommandId` that ties back to the eventual `CommandResult.CommandId`.

Serial execution, atomic commit, idempotency window, optimistic version check, and back-pressure rules of ADR 0006 are unchanged.

## Open challenges

- **Query concurrency model in implementation.** Snapshot reads with copy-on-write Document, or read-locks with command-side wait, or a simple "queue queries through the same bus and serialize" — three implementations satisfy the observable contract here. V1 picks the simplest (serialize) and revisits if profiling demands.
- **Bulk queries / pagination.** Listing all entities in a 100k-entity Document needs paging. Out of scope for V1 (no V1 query is that expensive); the schema for query results should leave room for a `cursor` field so paging adds without a contract break.
- **Streaming queries.** Some inspections (live raycast under the cursor) want a subscription, not a one-shot. V1 has no answer. Likely solved by treating them as ephemeral commands with progress events, or by introducing a third surface in V2; do not improvise it now.
- **Diagnostic code stability across versions.** A code that disappears in V2 leaves stranded clients. Promise: codes are append-only; deprecation marks them obsolete in the registry but keeps them recognized.
