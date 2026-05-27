# TASK-0012 — Command-log persistence for `engine-api-http` (P8a)

## Status

Ready

## Context

P8a is the first V1.x phase after the geometry slice, per `docs/roadmap.md`. It lifts the V1 "no persistence" clamp for `engine-api-http` only — `Engine.Cli` stays ephemeral.

ADR-0014 (Accepted 2026-05-27) pre-decides every load-bearing call:

- The on-disk artifact is a single append-only command log; nothing else is persisted (no events, no snapshots, no idempotency cache, no backend state).
- Format: JSON Lines, UTF-8, LF newlines. First record is `log-header` (carries `documentId` for restart-survival); subsequent records are `command-applied` reusing the `POST /commands` wire shape.
- Durability: fsync inside the bus's serial commit section, before the `CommandResult` returns to the client. Write failure → command Rejected with `E-IO-WRITE-FAILED`, in-memory state untouched.
- Startup: open + exclusive-lock the file, replay records into a fresh `CommandBus`/`Document` against a fresh backend. Trailing partial record → truncate + `W-IO-TRUNCATED`; non-trailing malformed record → abort with `E-IO-LOAD-FAILED`.
- Scope: `engine-api-http` only. The CLI does not open the log file. No log path configured → ephemeral mode + `I-IO-EPHEMERAL`.
- No `Engine.Contracts` changes. The `ICommandLog` abstraction lives in `Engine.Core`.

ADR-0001 §4 (backends are caches), ADR-0005 §5 (replay is deterministic modulo `Timestamp`/`DocumentId`), ADR-0006 §1 (serial commit-at-end), ADR-0010 §4 (snapshot ≠ persistence), ADR-0011 §1 + §5 (server-default, CLI stays embedded), and ADR-0012 §4 (deterministic body handles from `CommandId`) all stay in force unchanged.

## Goal

Ship durable, restart-survivable Document state for `engine-api-http` behind a configured log path. After this TASK, an operator can:

1. Start `engine-api-http --log-path ./engine.log`, apply N commands (NoOp, CreateBox), observe `Document.Version = N`, `Document.Bodies` populated.
2. Kill and restart the process against the same path.
3. Observe identical `documentId`, identical `Document.Version`, identical `Document.Bodies` (handles intact), all derived by replay.
4. Reconnect a WebSocket client; receive `subscription.reset` whose snapshot matches pre-shutdown state (modulo `Timestamp` per ADR-0005).

`Engine.Cli` behaviour is unchanged.

## Scope (in)

### 1. Engine.Core changes

- `Engine.Core/Persistence/ICommandLog.cs` — new abstraction:
  ```csharp
  public interface ICommandLog
  {
      Guid DocumentId { get; }                 // From header, or assigned at file creation
      void Append(Command command, long appliedAtSeq);   // Synchronous: writes + fsyncs before return
      IEnumerable<LoggedCommand> Read();        // Header-then-records iteration for startup
      void Dispose();                            // Releases exclusive lock; idempotent
  }

  public readonly record struct LoggedCommand(Command Command, long AppliedAtSeq);
  ```
  - `Append` is synchronous and blocking. Returns only after the fsync completes. Throws on IO failure; `CommandBus` catches and converts to `E-IO-WRITE-FAILED`.
  - `Read` is called once at startup before the bus accepts live commands; yields records in file order. Throws on non-trailing malformed records.
- `Engine.Core/Persistence/NullCommandLog.cs` — new. `DocumentId = Guid.NewGuid()` at construction; `Append` is no-op; `Read` returns empty. The default for tests, CLI, and ephemeral server. Implements singleton `Instance` like `NullGeometryBackend.Instance`.
- `Engine.Core/Persistence/FileCommandLog.cs` — new, the V1.x file-backed implementation:
  - Constructor: `FileCommandLog(string path)`.
    - Opens `FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)` — `FileShare.None` is the exclusive lock per ADR-0014 §7.
    - On `IOException` (lock conflict, permission denied), throws `PersistenceLoadException` with reason code `E-IO-LOAD-FAILED` and the OS error.
    - If file is empty: assign `DocumentId = Guid.NewGuid()`, write the header line, fsync.
    - If file has content: parse header line into `DocumentId`. Defer record parsing to `Read`.
  - `Append(command, appliedAtSeq)`: serialize the record to a single JSON line, write, then `FileStream.Flush(flushToDisk: true)` for fsync. Throw `PersistenceWriteException` on IO failure.
  - `Read()`: yields the parsed records in order. On malformed line: if it is the last line of the file, truncate the stream to before the malformed line, log `W-IO-TRUNCATED` with byte offset, and end iteration. If it is not the last line, throw `PersistenceLoadException` (`E-IO-LOAD-FAILED`).
  - `Dispose`: closes the stream, releases the lock.
- `Engine.Core/Persistence/CommandLogRecord.cs` — new. DTOs for the two record kinds; JSON shapes per ADR-0014 §3. Uses `System.Text.Json` with camelCase. Includes a `RecordType` discriminator (`"log-header"` / `"command-applied"`) so the line parser dispatches.
- `Engine.Core/Persistence/PersistenceExceptions.cs` — new. `PersistenceWriteException`, `PersistenceLoadException`, `PersistenceTruncatedWarning`. Each carries the diagnostic code and a message.
- `Engine.Core/CommandRegistry.cs` — expose a `SerializeCommand(Command) → JsonNode` helper that mirrors the existing deserialization path. The handler knows its own parameter projection; the registry can route through the handler. Used by `FileCommandLog` to serialize the `command` field of each `command-applied` record.
- `Engine.Core/CommandBus.cs`:
  - Constructor adds optional `ICommandLog log` parameter, defaulting to `NullCommandLog.Instance`.
  - Constructor optionally accepts the bus's `_document` from outside (so `EngineHost` can construct a `Document` with the persisted `documentId`). If today's bus owns Document construction, add a `CommandBus(IGeometryBackend backend, ICommandLog log)` overload that uses `log.DocumentId` to construct the Document.
  - `ApplyOnce` commit section: after the handler succeeds and events are drafted, BEFORE `_document.AppendCommand` / `AdvanceVersion` / sink emission, call `_log.Append(command, prospectiveSeq)`. Catch `PersistenceWriteException` → convert to `CommandResult { Status = Rejected, Error = new(E-IO-WRITE-FAILED, ...) }`. In-memory state untouched. No event emitted.
  - The commit ordering: write+fsync first, then mutate Document, then emit events to the sink. If the sink throws (in-memory only — should never happen for `InMemoryEventSink`), Document is already advanced; that is a pre-existing invariant of the bus.
- `Engine.Core/Replay.cs`: no signature change. Replay already accepts a `Document` and `IGeometryBackend`; the startup replay path constructs both fresh and iterates `ICommandLog.Read()`, submitting each via `Replay.ReplayLog`.
- `Engine.Core/DiagnosticCodes.cs` — add four constants: `E-IO-WRITE-FAILED`, `E-IO-LOAD-FAILED`, `W-IO-TRUNCATED`, `I-IO-EPHEMERAL`.

### 2. Engine.Api.Http changes

- `Engine.Api.Http/EngineHost.cs`:
  - Construction takes a new option (`EngineHostOptions { string? LogPath }` or a constructor parameter — implementer's choice).
  - If `LogPath` is null/empty: construct with `NullCommandLog.Instance`; log `I-IO-EPHEMERAL` at startup via the host's existing logging path (or write to stdout — minimal logging for V1.x).
  - If `LogPath` is set:
    1. Construct `FileCommandLog(logPath)`.
    2. Construct `Document` with `log.DocumentId`.
    3. Construct `CommandBus(backend, log, document)`.
    4. Iterate `log.Read()`. For each `LoggedCommand`, submit through `Replay.ReplayLog` against the fresh bus, document, and backend. (Replay re-applies commands without re-writing them — the bus's `Apply` is the live path; replay uses a separate code path that does NOT call `log.Append`.)
    5. Bring up HTTP + WebSocket listeners.
  - Disposal: `EngineHost.Dispose()` disposes the log (releases lock).
- `Engine.Api.Http/Program.cs` (or wherever args are parsed):
  - Add `--log-path <path>` command-line flag and `ENGINE_LOG_PATH` env var. CLI flag wins over env var. Both unset → ephemeral.
  - Document the flag in the help text / README (out of scope for this TASK if no README exists yet; a startup-banner one-liner is enough).
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs`: mirror the four new codes.

### 3. Engine.Cli changes

**None.** The CLI stays ephemeral per ADR-0014 §5. It does not read `ENGINE_LOG_PATH`, does not open the file, does not replay. CI guard test asserts this (§5 below).

### 4. Replay path detail

`Replay.ReplayLog` already takes `(log, document, registry, sink, backend)` per TASK-0011. For startup replay:

- `log` is the in-memory `Document.Log` after the bus rebuilds it. But we're replaying *from disk*, not from in-memory log. Two options:
  - (a) `EngineHost` reads disk records, builds a `List<Command>`, calls existing `Replay.ReplayLog(list, ...)`. This is a one-line iteration.
  - (b) Add a `Replay.ReplayFromCommandLog(ICommandLog log, ...)` helper that takes the persistence-side log directly.
- Pick (a) — it requires no new replay API surface and the iteration is trivial.

The bus's `Apply` writes to the live log on each command. The replay path does NOT write to the log (it would double-write). Two ways to ensure this:
  - (a) During startup replay, construct the bus with `NullCommandLog.Instance`, then swap to `FileCommandLog` before going live. Risky — two buses sharing state.
  - (b) Have `Replay.ReplayLog` use a `CommandBus.ReplayApply` overload that bypasses the log write. This already conceptually exists since replay must be side-effect-free on the log; ADR-0005 already says replay is deterministic, and writing to disk would be a side effect.

Pick (b). `CommandBus` gains an `internal void ReplayApply(Command command)` method that runs the handler + commits state + emits to sink, but does NOT call `_log.Append`. Replay drives this method directly via `Replay.ReplayLog`.

### 5. Diagnostic codes registered (three places)

- `docs/diagnostics.md` — append rows for `E-IO-WRITE-FAILED`, `E-IO-LOAD-FAILED`, `W-IO-TRUNCATED`, `I-IO-EPHEMERAL`. `IO` subsystem token is already in §Conventions.
- `Engine.Core/DiagnosticCodes.cs` — add constants.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror entries.

### 6. Tests (in `Engine.Tests/`)

**Persistence unit tests:**

- `Engine.Tests/Persistence/NullCommandLogTests.cs`:
  - `Append_Is_NoOp`.
  - `Read_Returns_Empty`.
  - `DocumentId_Is_Stable_Across_Reads`.
- `Engine.Tests/Persistence/FileCommandLogTests.cs`:
  - `New_File_Writes_Header_With_Fresh_DocumentId`.
  - `Append_Writes_Record_And_Fsyncs` — assert via wrapping FileStream or by reopening the file and reading the line.
  - `Append_Multiple_Records_Increments_AppliedAtSeq_Externally` — note: `appliedAtSeq` comes from the bus, log just records it.
  - `Read_Returns_All_Records_In_File_Order`.
  - `Read_With_Trailing_Malformed_Line_Truncates_And_Yields_Earlier_Records` — write a good record, then garbage; Read yields the good record and truncates the garbage; file is now valid.
  - `Read_With_NonTrailing_Malformed_Line_Throws_E_PERSIST_LOAD_FAILED`.
  - `Second_Open_With_Exclusive_Lock_Throws_E_PERSIST_LOAD_FAILED` — open the file twice from the same test process.
  - `Header_DocumentId_Preserved_Across_Reopens`.
- `Engine.Tests/Persistence/CommandLogRecordTests.cs`:
  - `Header_Round_Trips_Through_Json` — serialize, deserialize, assert equality.
  - `Command_Applied_Round_Trips_For_NoOp`.
  - `Command_Applied_Round_Trips_For_CreateBox`.
  - `Schema_Version_Is_Always_1_In_V1` — fixture asserts written records have `schemaVersion: 1`.

**Bus integration tests:**

- `Engine.Tests/CommandBus/CommandBus_FileLogTests.cs`:
  - `Applied_Command_Is_Written_To_Log_Before_Result_Returns` — use a wrapping `ICommandLog` that records the order of `Append` vs. `CommandResult` returned (or assert via the file content immediately after the bus returns).
  - `Rejected_Command_Is_Not_Written_To_Log` — apply an unknown command (returns `Rejected` with `E-CMD-UNKNOWN`); assert the log file contains zero `command-applied` records.
  - `Write_Failure_Returns_E_PERSIST_WRITE_FAILED_And_Does_Not_Mutate_Document` — wrap the log in a stub that throws on `Append`; assert `Document.Version` is unchanged, `Document.Log` is unchanged, no event emitted.
  - `Idempotency_Cache_Is_Hit_For_Replayed_Commands_During_Startup` — apply 3 commands to a file-backed bus, dispose, reopen → after startup replay, the cache should contain all 3 `CommandResult`s. A retry of the third `CommandId` post-replay returns the cached result without re-applying.

**Persistence roundtrip tests:**

- `Engine.Tests/Persistence/PersistenceRoundtripTests.cs`:
  - `NoOp_NoOp_NoOp_Roundtrip_Reproduces_Document_Version_And_Log`.
  - `CreateBox_Roundtrip_Reproduces_Document_Bodies_With_Same_Handle`.
  - `Mixed_NoOp_CreateBox_NoOp_Roundtrip_Reproduces_Full_State`.
  - `DocumentId_Is_Preserved_Across_Restart` — assert the `Document.DocumentId` after restart equals the pre-shutdown value.
  - `Subscription_Reset_Snapshot_After_Restart_Matches_Pre_Shutdown_Snapshot` — modulo `createdAt`/`updatedAt`/`Timestamp`.

**Startup behaviour tests** (in `Engine.Tests/Http/` since they target `EngineHost`):

- `Engine.Tests/Http/EngineHostStartupTests.cs`:
  - `No_LogPath_Configured_Runs_Ephemeral_And_Emits_I_PERSIST_EPHEMERAL`.
  - `Empty_File_Writes_Header_And_Starts_Fresh`.
  - `Existing_File_Replays_All_Commands_Before_Listener_Accepts`.
  - `Lock_Conflict_At_Startup_Aborts_With_E_PERSIST_LOAD_FAILED`.

**CLI scope guard:**

- `Engine.Tests/Cli/CliPersistenceScopeTests.cs`:
  - `Cli_Run_Ignores_ENGINE_LOG_PATH_Env_Var` — set the env var to a path that would fail if opened (e.g., a directory), run an `engine apply NoOp`, assert it succeeds and no file is created at that path.
  - `Cli_Does_Not_Read_Log_Path_Argument` — pass `--log-path /some/path` to CLI; should fail with usage error or be silently ignored (pick one, document it). Assert no file IO at the path.

**Existing tests stay green:**

- The v0.5 replay-determinism fixture runs against `NullCommandLog.Instance` (the default). No behavioural change.
- All existing CLI and HTTP tests construct buses with the default null log; no signature breakage.

### 7. Documentation

- `CLAUDE.md` — narrow the V1 clamp:
  - Replace `- **No persistence** — in-memory only until persistence ADR + TASK.` with `- **No CLI persistence** — \`Engine.Cli\` stays ephemeral until a follow-on ADR + TASK. \`engine-api-http\` persistence landed in v0.12 (ADR-0014, TASK-0012).`
- `docs/CURRENT-STATE.md` — append v0.12 entry: "Command-log persistence (P8a, TASK-0012, ADR-0014). `engine-api-http` gains a configured `--log-path` flag; applied commands are durably written + fsynced inside the bus's serial commit section before `CommandResult` returns. Startup replays from genesis against a fresh backend; `documentId` is preserved across restart. CLI stays ephemeral. Four new diagnostic codes registered. `dotnet build` + `dotnet test` green (≈84 + new tests)."
- `docs/roadmap.md`:
  - Move `P8a — Command-log persistence` from V1.x Pending to Shipped (one-line entry).
- `docs/diagnostics.md` — four appended rows (registered as part of the implementation commit).

## Scope (out)

- **Snapshots / compaction.** ADR-0014 §6 defers; revisit when log size warrants.
- **Embedded-CLI persistence.** ADR-0014 §5 + §Open challenges. Out of V1.x.
- **Multi-Document persistence.** V2 (ADR-0011 §Non-goals).
- **Multi-process / multi-writer coordination.** Exclusive lock detects conflict; coordination is V2.
- **Encryption at rest.** Operator's filesystem concern.
- **Command-version migration / cross-version log upgrade.** ADR-0014 §8 + §Open challenges. Operators preserve handlers.
- **Performance-tuned batched fsync.** Per-command fsync in V1.x; future ADR.
- **Persisting Rejected commands** for diagnostic audit. Out of scope.
- **Replacing `Document.Log` with disk reads at runtime.** The log stays in memory; disk mirrors.
- **Backup / restore tooling.** `cp` works at V1.x scale.
- **CLI flag deprecation for `--log-path` on CLI.** If implementer chooses "fail with usage error" path in §5, document; if "silently ignore," document. Don't surface as a feature.

## Inputs

- ADR-0001 — backends are caches; losing them is recoverable by replay.
- ADR-0005 — event stream; replay determinism modulo `Timestamp`/`DocumentId`.
- ADR-0006 — command execution; serial commit-at-end section (now extended with the log write step).
- ADR-0008 — `CommandResult` shape (unchanged).
- ADR-0010 — snapshot ≠ persistence; restart-rebuilt snapshot matches pre-shutdown modulo time.
- ADR-0011 — server-default deployment; CLI stays embedded.
- ADR-0012 — deterministic body handles from `CommandId` (replay reproduces handles).
- **ADR-0014 — Command-log persistence for `engine-api-http`** (Accepted this set).
- TASK-0005 — replay-determinism fixture (unaffected; uses `NullCommandLog`).
- TASK-0007 — `Engine.Api.Http` scaffold (`EngineHost`, `ApiJson`).
- TASK-0010 — WebSocket events and `subscription.reset` (reconnect post-restart relies on the fresh in-memory ring + replay-derived state).
- TASK-0011 — `Document.Bodies` projection, `Replay.ReplayLog(... backend)`, `CommandBus(IGeometryBackend backend)` signature.

## Outputs

- `engine-api-http --log-path ./engine.log` starts; applying commands writes to the file.
- Kill + restart → same `Document.DocumentId`, `Document.Version`, `Document.Bodies`.
- `engine-api-http` with no log path → ephemeral run + `I-IO-EPHEMERAL` at startup.
- Two concurrent `engine-api-http` processes on the same path → second fails with `E-IO-LOAD-FAILED`.
- Disk full mid-Apply → command returns `Rejected` with `E-IO-WRITE-FAILED`; Document unchanged.
- Crash mid-write → trailing partial record is truncated on next startup with `W-IO-TRUNCATED`; no command lost that was announced Applied.
- `Engine.Cli` invocations open no file, write no log, ignore `ENGINE_LOG_PATH`.
- `dotnet build` + `dotnet test` green.
- `/schema/diagnostics` mirrors the four new PERSIST codes.
- `docs/CURRENT-STATE.md` v0.12 entry.
- `docs/roadmap.md` shows P8a Shipped V1.x.
- CLAUDE.md "No persistence" clamp narrowed to CLI-only.

## Files

**Created:**
- `Engine.Core/Persistence/ICommandLog.cs`
- `Engine.Core/Persistence/NullCommandLog.cs`
- `Engine.Core/Persistence/FileCommandLog.cs`
- `Engine.Core/Persistence/CommandLogRecord.cs`
- `Engine.Core/Persistence/PersistenceExceptions.cs`
- `Engine.Tests/Persistence/NullCommandLogTests.cs`
- `Engine.Tests/Persistence/FileCommandLogTests.cs`
- `Engine.Tests/Persistence/CommandLogRecordTests.cs`
- `Engine.Tests/Persistence/PersistenceRoundtripTests.cs`
- `Engine.Tests/CommandBus/CommandBus_FileLogTests.cs`
- `Engine.Tests/Http/EngineHostStartupTests.cs`
- `Engine.Tests/Cli/CliPersistenceScopeTests.cs`
- `tasks/TASK-0012-command-log-persistence.md` (this file)

**Modified:**
- `Engine.Core/CommandBus.cs` — `ICommandLog log` ctor param (optional, defaults to `NullCommandLog.Instance`); commit-section write+fsync; `internal ReplayApply` that bypasses the log write.
- `Engine.Core/CommandRegistry.cs` — expose `SerializeCommand(Command)` helper mirroring the deserialization path.
- `Engine.Core/DiagnosticCodes.cs` — four new constants.
- `Engine.Api.Http/EngineHost.cs` — accept `LogPath` option; construct `FileCommandLog` or `NullCommandLog`; preserve `documentId`; run startup replay loop before listener accepts.
- `Engine.Api.Http/Program.cs` — parse `--log-path` flag and `ENGINE_LOG_PATH` env var.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror four new codes.
- `CLAUDE.md` — narrow the V1 persistence clamp.
- `docs/diagnostics.md` — four appended rows.
- `docs/CURRENT-STATE.md` — v0.12 entry.
- `docs/roadmap.md` — P8a Shipped under V1.x.
- `tasks/TASK-0012-command-log-persistence.md` — Status flip in close commit.

**Do not touch:**
- `Engine.Contracts/**` — no public-shape change. ADR-0014 §Consequences pins this.
- ADRs 0001–0013 (in force; not amended).
- `Engine.Cli/Program.cs` — stays ephemeral.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*` (CLAUDE.md "Do not touch").
- WebSocket transport (`EventsEndpoint`, `EventBroadcaster`, `SnapshotProjector`) — unchanged. Snapshot shape is unaffected by persistence; reconnect-after-restart works because the replayed `Document` matches pre-shutdown state.
- `Replay.ReplayLog` signature — unchanged from TASK-0011. Use it as-is.

## Tests

(Listed under §6. Existing ≈84 + ~20–25 new = ~105–110 total after this TASK. Final count is a check, not a target.)

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — all existing tests plus the new persistence tests.
3. A `command-applied` record appears in the on-disk log before the bus returns `Status = Applied` to the client (CI: bus-integration test asserts ordering).
4. A `Rejected` command produces no on-disk record.
5. A startup replay against an existing file reproduces `Document.DocumentId` (header-restored), `Document.Version` (replay-derived), `Document.Log` (replay-rebuilt), and `Document.Bodies` (replay-derived through the backend).
6. Two `engine-api-http` instances on the same log path → the second aborts with `E-IO-LOAD-FAILED` (manual or CI-test).
7. `engine-api-http` with no log configured emits `I-IO-EPHEMERAL` at startup and runs identically to pre-v0.12 behaviour.
8. `Engine.Cli` opens no file when `ENGINE_LOG_PATH` is set (CI: scope-guard test).
9. The replay-determinism fixture (from v0.5 / v0.11) continues to pass without modification.
10. Four new diagnostic codes registered in the three required places.
11. `CLAUDE.md` persistence clamp narrowed to CLI-only.
12. `docs/CURRENT-STATE.md` v0.12 entry exists.
13. `docs/roadmap.md` shows P8a Shipped under V1.x.

## Notes for the implementer

- **One implementation commit, then one close commit.** Same cadence as v0.1..v0.11. Close commit flips `Status` to `Done — shipped in <impl-hash>` and updates `CURRENT-STATE.md` reference.
- **Order of changes in the impl commit.** Probably: persistence abstractions (`ICommandLog`, `NullCommandLog`, `FileCommandLog`, exceptions, record types) first, then `CommandBus` wiring, then `EngineHost` integration, then tests, then docs + diagnostics + CLAUDE.md + roadmap last. The compile errors guide intermediate steps.
- **fsync mechanism.** .NET: `FileStream.Flush(flushToDisk: true)`. On Windows this calls `FlushFileBuffers`; on POSIX it calls `fsync(2)`. Equivalent semantics. No P/Invoke needed.
- **Exclusive lock.** `FileShare.None` on `FileStream` construction is the exclusive lock. A second process opening the same path gets `IOException` ("The process cannot access the file because it is being used by another process") — wrap that in `PersistenceLoadException` with `E-IO-LOAD-FAILED`.
- **Header serialization.** Write the header line as soon as the file is created (length-zero check). Use `JsonSerializer.Serialize(header, ApiJson.Options)` then append `'\n'` and write bytes. fsync. Subsequent records use the same approach.
- **Reading a JSON line.** `StreamReader.ReadLine()` is fine for V1.x. Each line is one JSON object; parse with `JsonSerializer.Deserialize`. Malformed line → `JsonException` → handle per §1 truncation rules.
- **Truncation on trailing partial record.** Compute the byte offset of the start of the malformed line (track it during the StreamReader walk). Set `FileStream.Position = offset`; `SetLength(offset)`; fsync. Then log `W-IO-TRUNCATED` and end iteration.
- **`Command` serialization for the log.** The `command` field of each `command-applied` record is the same JSON shape `POST /commands` accepts: `{ "name": "...", "version": N, "commandId": "<guid>", "parameters": { ... } }`. The bus has the typed `Command` object; serializing it back requires knowing the parameter projection. Add `CommandRegistry.SerializeCommand(Command) → JsonNode` that delegates to the handler's parameter-projection knowledge (the handler already declares `Parameters` schema via TASK-0011). For each declared parameter, read the value off the typed command via reflection or per-handler projection. Implementer's call: pure reflection over `command` properties matching the declared `Parameters` keys is the simplest.
- **`ReplayApply` vs `Apply`.** `Apply` writes to the log; `ReplayApply` does not. Both share the validate/handle/commit/emit-events code path. Refactor `Apply` to call `ReplayApply` internally after the log write succeeds. Keep the existing `Apply` public contract identical.
- **Document construction with persisted `documentId`.** Today's `Document` likely assigns `DocumentId = Guid.NewGuid()` in its constructor. Add an overload or factory: `Document.WithDocumentId(Guid id)` or `new Document(Guid documentId)`. The `EngineHost` startup path calls the persisted-id overload after reading the header.
- **Idempotency cache after restart.** Each replayed command stores its `CommandResult` in the cache via the standard commit path — no special handling. The cache is naturally re-warmed to the last ≤1024 entries.
- **WebSocket reconnect after restart.** Out of this TASK's concern beyond a smoke test. The protocol already handles cursor-outside-ring via `subscription.reset` (ADR-0010). After restart, the in-memory ring is fresh; reconnecting clients with old cursors get a reset to the replayed snapshot.
- **No new event-registry abstraction.** No new event kinds. Persistence emits no events.
- **No schema endpoint changes.** No new commands, no new queries. `/schema/commands`, `/schema/queries`, `/schema/events` unchanged. Only `/schema/diagnostics` updates (four new codes mirrored).
- **CLI guard test specifics.** Set `ENGINE_LOG_PATH = "/this/path/should/never/be/opened/by/cli"`. Run `Cli.Run` for a NoOp. Assert (a) exit code 0, (b) the path does not exist on disk afterward, (c) no `FileStream` was opened (test via a stub `IFileSystem` if you prefer, or via the absence of the file).
- **No `Engine.Contracts` changes.** If a need arises, stop and flag — those are gated by ADR. ADR-0014 deliberately keeps the abstraction in `Engine.Core`.
- **Diagnostic code naming.** `IO` is the subsystem token, already reserved in `docs/diagnostics.md` §Conventions ("persistence/transport"). No conventions-section change needed.
- **CURRENT-STATE.md tone.** Match the existing v0.11 entry's density: one paragraph, references the ADR, lists new diagnostic codes, names the test count delta, ends with `dotnet build` + `dotnet test` green.
