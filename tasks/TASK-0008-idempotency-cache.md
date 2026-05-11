# TASK-0008 — Idempotency cache (P6.2)

## Status

Ready

## Context

ADR-0006 §7 fixes the idempotency contract: *"Commands carry a `CommandId : Guid`. The engine deduplicates within a short window (V1 default: most recent 1024 applied command IDs). A duplicate `CommandId` returns the cached `CommandResult` of the prior application without re-executing."* Its purpose is explicit: *"protect against transport retry storms (HTTP/WS reconnects), not against intentional repeats."*

CLAUDE.md still carries the V1 clamp *"No idempotency cache, no schema endpoints — deferred to transport task."* P6.1 (TASK-0007) shipped the HTTP scaffold without the cache; this task lands it.

Because the cache benefits every client of the `CommandBus` (CLI, HTTP, future in-process embedders), it lives in `Engine.Core`, not in `Engine.Api.Http`. The HTTP scaffold's singleton `EngineHost` constructs one `CommandBus` for the process lifetime; it picks up the cache by construction.

## Goal

Add `Engine.Core/IdempotencyCache.cs`: a FIFO-evicting `Dictionary<Guid, CommandResult>` of bounded capacity (default 1024 per ADR-0006, configurable via constructor). Wire it into `CommandBus.Apply` so a duplicate `CommandId` returns the cached result with no log entry, no event, and no further handler invocation.

## Scope (in)

1. **`Engine.Core/IdempotencyCache.cs`**
   - Public class, mirroring the visibility/style of `InMemoryEventSink`.
   - `DefaultCapacity = 1024` constant; constructor accepts an optional capacity for tests.
   - FIFO eviction: a `Dictionary<Guid, CommandResult>` for O(1) lookup plus a `Queue<Guid>` for insertion order. When the queue reaches capacity, dequeue the oldest entry and remove it from the dictionary.
   - `bool TryGet(Guid commandId, out CommandResult result)` and `void Store(Guid commandId, CommandResult result)`. Thread-safety is not required — the cache is accessed only from inside `CommandBus.Apply`'s serial section (ADR-0006 §2).

2. **`Engine.Core/CommandBus.cs`**
   - Constructor accepts an optional `IdempotencyCache cache = null` parameter. If null, the bus constructs a default-capacity cache internally.
   - Inside `Apply`'s serial section, **after** acquiring the `SemaphoreSlim` and **before** any other work: `TryGet(command.CommandId)`. On hit: return the cached result. No `Seq` increment, no event, no log entry.
   - At the end of every `Apply` code path that produces a `CommandResult` — Applied, Rejected, Cancelled — `Store(command.CommandId, result)` before returning. The current control flow uses early returns from `Reject(...)` / `Cancel(...)`; refactor so all paths converge on a single point that stores then returns.

3. **Decision: all terminal states are cached.**
   - ADR-0006 §7 reads "applied command IDs," but the validation goal (transport-retry safety) applies equally to Rejected and Cancelled. A retried submission of any answered `CommandId` should produce the same answer.
   - The cache stores every `CommandResult` the bus produces. The bus, not the cache, decides what counts as a result.

4. **Tests in `Engine.Tests/`**
   - `IdempotencyCacheTests.TryGet_Returns_False_For_Unknown_CommandId`
   - `IdempotencyCacheTests.Store_Then_TryGet_Returns_Stored_Result`
   - `IdempotencyCacheTests.Capacity_Is_Bounded_And_Evicts_Oldest_When_Full` — instantiate with capacity 2, store three, assert the oldest is gone.
   - `IdempotencyCacheTests.Store_Is_Idempotent_For_Same_CommandId` — re-storing the same id does not double-count or evict.
   - `CommandBusIdempotencyTests.Applying_Same_CommandId_Twice_Returns_Cached_Result_With_No_New_Event_Or_Log_Entry` — canonical case from ADR-0006 validation rule §5.
   - `CommandBusIdempotencyTests.Applying_Same_CommandId_Twice_Returns_Identical_AppliedAtSeq_And_DurationMs` — confirms the cache returns the original, not a recomputed value.
   - `CommandBusIdempotencyTests.Rejected_Command_Is_Cached_And_Returns_Cached_Rejection_On_Duplicate` — design decision §3 verified.
   - `Engine.Tests/Http/CommandsEndpointIdempotencyTests.PostCommands_With_Same_CommandId_Returns_Cached_Result` — end-to-end through HTTP via `WebApplicationFactory<Program>`.

5. **Documentation**
   - `CLAUDE.md` V1 clamps: the line *"No idempotency cache, no schema endpoints — deferred to transport task."* becomes *"No schema endpoints — until P6.4."* The HTTP-transport line is also stale (P6.1 landed); rewrite it to scope to WebSocket only — *"No WebSocket transport — until P6.3."*
   - `docs/CURRENT-STATE.md` — v0.8 entry.
   - `docs/roadmap.md` — move P6.2 from Pending V1.x to Shipped.

## Scope (out)

- WebSocket events / reconnect (P6.3).
- Schema endpoints (P6.4).
- Pluggable storage backends (Redis, etc.). The 1024-entry in-memory cache is enough for V1.x; persistent idempotency arrives with persistence.
- Configurable cache capacity at the API layer / via env var. The constructor parameter is enough for tests.
- Per-Document cache scoping. V1 runs one Document per Engine Runtime (ADR-0005); a single cache is correct.
- Cache observability endpoints (`/metrics`, …). Not needed in V1.x.
- LRU (recently-*used*) eviction. ADR-0006 says "most recent" — FIFO on insertion is the literal reading and simpler. Switching to LRU later is non-breaking.
- Changing `Engine.Contracts/**`. `Command.CommandId` already exists.
- Changing the CLI. The CLI builds a fresh bus per invocation; idempotency is naturally per-process. No behavior change there.
- Adding any new diagnostic code.

## Inputs

- ADR-0006 §7 — idempotency contract.
- ADR-0006 §§1–2 — `CommandResult.Status` triad, serial execution.
- `Engine.Core/CommandBus.cs` — current implementation.
- `Engine.Tests/CommandBusTests.cs` — test style.
- `Engine.Tests/Http/CommandsEndpointTests.cs` — HTTP test style.

## Outputs

- `Engine.Core/IdempotencyCache.cs` exists; `CommandBus` uses it.
- Submitting the same `CommandId` twice through `CommandBus.Apply` returns the cached `CommandResult`. No second log entry, no second event.
- HTTP endpoint test confirms duplicate `CommandId` returns identical JSON with no bus side effects (Document.Version unchanged between the two HTTP calls).
- `CLAUDE.md` V1 clamps list no longer mentions idempotency cache. The HTTP-transport clamp is rewritten to WebSocket-only.
- `docs/CURRENT-STATE.md` v0.8 entry.
- `docs/roadmap.md` shows P6.2 Shipped V1.x.

## Files

**Created:**
- `Engine.Core/IdempotencyCache.cs`
- `Engine.Tests/IdempotencyCacheTests.cs`
- `Engine.Tests/CommandBusIdempotencyTests.cs`
- `Engine.Tests/Http/CommandsEndpointIdempotencyTests.cs`
- `tasks/TASK-0008-idempotency-cache.md` (this file)

**Modified:**
- `Engine.Core/CommandBus.cs` — cache lookup + store; constructor accepts an optional cache.
- `CLAUDE.md` — V1 clamps tightened (idempotency clamp dropped; HTTP clamp narrowed to WebSocket).
- `docs/CURRENT-STATE.md` — add v0.8 entry.
- `docs/roadmap.md` — move P6.2 to Shipped.
- `tasks/TASK-0008-idempotency-cache.md` — flip Status to Done in close commit.

**Do not touch:**
- `Engine.Contracts/**`. No contract change.
- `Engine.Cli/**`. Naturally per-process; no behavior change.
- `Engine.Api.Http/**` apart from inheriting the new behavior through `EngineHost`'s shared `CommandBus`.
- `docs/diagnostics.md`. No new codes.
- ADRs.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*`.

## Tests

- `IdempotencyCacheTests.TryGet_Returns_False_For_Unknown_CommandId`
- `IdempotencyCacheTests.Store_Then_TryGet_Returns_Stored_Result`
- `IdempotencyCacheTests.Capacity_Is_Bounded_And_Evicts_Oldest_When_Full`
- `IdempotencyCacheTests.Store_Is_Idempotent_For_Same_CommandId`
- `CommandBusIdempotencyTests.Applying_Same_CommandId_Twice_Returns_Cached_Result_With_No_New_Event_Or_Log_Entry`
- `CommandBusIdempotencyTests.Applying_Same_CommandId_Twice_Returns_Identical_AppliedAtSeq_And_DurationMs`
- `CommandBusIdempotencyTests.Rejected_Command_Is_Cached_And_Returns_Cached_Rejection_On_Duplicate`
- `CommandsEndpointIdempotencyTests.PostCommands_With_Same_CommandId_Returns_Cached_Result`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — existing 44 plus the 8 new tests (52 total).
3. The bus's serial-execution contract is preserved: cache lookup is inside the `SemaphoreSlim` section.
4. No new diagnostic code; no new `Engine.Contracts/**` type.
5. `CLAUDE.md` V1 clamps no longer mention idempotency cache; HTTP-transport clamp is rewritten to WebSocket-only.
6. `docs/CURRENT-STATE.md` v0.8 entry exists.
7. `docs/roadmap.md` lists P6.2 under Shipped V1.x, removed from V1.x Pending.

## Notes for the implementer

- **All terminal states are cached.** Documented above; revisit if a future test or use case demands otherwise.
- **FIFO, not LRU.** Reads do not refresh ordering. Insertions do. Cap is hit when 1024 distinct CommandIds have been answered since startup.
- **Cache check before semaphore-internal work.** Reuses the existing serial guarantee; no new locking. The check happens *after* `await _serial.WaitAsync(ct)` to keep the cache consistent across concurrent submissions of the same CommandId.
- **Constructor backward-compat.** The new optional parameter goes last so existing callers (CLI, HTTP host, tests) compile unchanged.
- **HTTP test pattern.** Reuses `WebApplicationFactory<Program>` from TASK-0007. The factory's `CreateClient()` yields one host with one engine; two POSTs with the same CommandId hit the same cache.
- **The HTTP endpoint test cannot directly assert "no second event."** It asserts `Document.Version` is unchanged across the two calls, which is the observable proxy. The direct event assertion lives in `CommandBusIdempotencyTests` against the in-process bus.
- **Refactor `CommandBus.Apply` carefully.** The existing flow has early returns from `Reject(...)` / `Cancel(...)` inside the `try`. Converging all paths to a single `result` variable and a single store-then-return at the bottom keeps the change minimal and avoids cache-store-misses in any path.
