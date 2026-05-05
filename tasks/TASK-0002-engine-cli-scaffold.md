# TASK-0002 — Engine.Cli scaffold (P1)

## Status

Ready

## Context

The Engine Runtime spine (TASK-0001) ships in-process types only: `CommandBus`, `QueryBus`, `Document`, `InMemoryEventSink`, `NoOpCommand`. There is no client. Per ADR 0002, the CLI is the canonical client for tests; until it exists, every claim about headless parity is unverifiable. Per ADR 0008 §6, the CLI surface is two verbs: `engine apply`, `engine query`.

This task introduces the CLI host process and the `apply`/`query` verbs. JSON is the wire format for output — both `CommandResult` and `QueryResult<T>` print as JSON to stdout. Inputs use a generic `--param key=value` flag; one command (`NoOp`) is registered.

JSON-input dispatch (`engine apply <command.json>`, per ADR 0002 §2) pairs with persistence/transport and is deferred. Generic `--param` keeps the CLI usable today for the only command that exists.

## Goal

Stand up `Engine.Cli` as a thin headless client that:

- Parses verb (`apply` | `query` | `help`) and dispatches to the in-process bus.
- For each invocation, builds a fresh in-process engine (`Document` + `CommandBus` + `QueryBus`). No persistence between invocations.
- Translates `<name>` + `--param k=v` pairs into a `Command` / `Query` instance and submits it.
- Serializes the resulting `CommandResult` / `QueryResult<T>` to JSON and writes it to stdout.
- Returns a stable exit code: `0` on `Status = Applied`; `1` on `Rejected`/`Cancelled`; `2` on invalid usage.

## Scope (in)

1. **New project: `Engine.Cli`**
   - .NET 10 console executable. `AssemblyName = engine`.
   - References only `Engine.Core` and `Engine.Contracts` (per the authority diagram).
   - `InternalsVisibleTo` Engine.Tests so scenario tests can call `Cli.Run` directly.
   - Standard library only (`System.Text.Json`). No third-party packages.

2. **Entry point and testable surface**
   - `Program.Main(args)` delegates to `Cli.Run(args, Console.Out, Console.Error)` and returns its exit code.
   - `public static int Cli.Run(string[] args, TextWriter stdout, TextWriter stderr)` — synchronous wrapper around the async dispatch. This is what tests call.

3. **Verbs**
   - `engine help` — prints usage to stdout, exits `0`.
   - `engine` (no args) — prints usage to stderr, exits `2`.
   - `engine apply <name> [--param k=v ...]` — constructs a `Command` (only `NoOp` recognized), submits it through `CommandBus.Apply`, writes the resulting `CommandResult` as JSON to stdout. Exit code derived from `Status`.
   - `engine query <name> [--param k=v ...]` — every name returns a `Rejected` `QueryResult<object>` with `E-QRY-UNKNOWN` (registry is empty). Exit `1`.
   - Unknown verb (e.g. `engine wibble ...`) — prints usage to stderr, exits `2`.

4. **Command dispatch (in-CLI)**
   - The CLI knows one command: `NoOp`. For `NoOp`, the CLI builds `NoOpCommand { Echo = params["echo"] }` and submits it to the bus. The bus is authoritative for `Status`, `AppliedAtSeq`, `DocumentVersion`, `Outputs`, `DurationMs`.
   - For any other command name, the CLI directly produces a `Rejected` `CommandResult` with `Error.Code = "E-CMD-UNKNOWN"`. This avoids a sentinel command type and a parallel command registry — both of which were considered and rejected. The bus cannot dispatch by name without a `Command` instance, and `Command` is abstract; constructing a fake `Command` to feed the bus would itself be a sentinel.
   - This is the only place the CLI raises `E-CMD-UNKNOWN` itself. The string lives once in `Engine.Core/DiagnosticCodes.cs`; the CLI references that constant.

5. **Query dispatch (in-CLI)**
   - The query registry is empty in P1. The CLI directly produces a `Rejected` `QueryResult<object>` with `Error.Code = "E-QRY-UNKNOWN"` for every query name. Same rationale as §4: `Query` is abstract.
   - When the first concrete query lands (post-P1), this branch is replaced by a typed-name dispatch parallel to `NoOp`'s.

6. **Argument parsing**
   - Format: zero or more `--param k=v` pairs after `<name>`. Each `--param` is followed by a single `key=value` argument.
   - Split on the first `=`. Empty key rejected. Empty value allowed.
   - Duplicate keys rejected with exit `2`.
   - Unknown flags (anything other than `--param`) rejected with exit `2`.

7. **JSON output**
   - One JSON document per invocation, indented, written to stdout.
   - `CommandResult` shape per ADR 0008 §2. `QueryResult<T>` shape per ADR 0008 §6.
   - Property names camelCase. Enums (`Status`, `Severity`) as strings. Nulls preserved (per ADR 0008: every `CommandResult` has the field).
   - `Outputs` serializes as the bare map `{"echo": "hi"}` per ADR 0008 §3, not as `{"values": {...}}`. Custom converter handles this.

8. **Exit codes**
   - `0` — `Status == Applied`.
   - `1` — `Status == Rejected` or `Status == Cancelled` (commands), or any query result with `Error != null`.
   - `2` — invalid CLI usage (no verb, unknown verb, missing required arg, malformed `--param`, duplicate key).

9. **Tests in `Engine.Tests/Cli/`**
   - Each test calls `Cli.Run(args, sw_out, sw_err)`, deserializes the captured stdout JSON, and asserts on exit code, `Status`, `Outputs`, and `Error.Code`.

## Scope (out)

- **JSON input** (`engine apply <command.json>`). Strict-reading of ADR 0002 §2 says this is required; deferred to the wire-format task that pairs with persistence/transport.
- Schema endpoints (`/schema/...`) — deferred per CLAUDE.md V1 clamps.
- Persistence between invocations — every CLI invocation creates a fresh `Document`.
- Cross-invocation state, daemon mode, REPL.
- HTTP/WS surface, transport, idempotency cache.
- Concrete query handlers (registry stays empty per TASK-0001).
- Multi-document workspaces.
- `--expected-document-version` flag (no surface for the only command, deferred).
- `--follow` event tail — deferred to transport task.
- Sentinel command/query types in `Engine.Contracts` or `Engine.Cli`.
- Argparse libraries, ANSI color, progress bars.
- Process-spawn integration tests; tests call `Cli.Run` in-process for speed.
- Any change to `Engine.Contracts/**` or `Engine.Core/**`.
- Adding diagnostic codes — none added; CLI surfaces `E-CMD-UNKNOWN` and `E-QRY-UNKNOWN` from the existing registry.

## Inputs

- ADR 0002 (headless-first; CLI as canonical client) — verb set, "if it only works in the UI, it is not implemented" rule.
- ADR 0004 (Engine Runtime is authority) — CLI is a thin client, no business logic.
- ADR 0007 (UI ephemeral state boundary) — CLI is a client; same boundary applies.
- ADR 0008 (triad) — `apply`/`query` verbs, structured `CommandResult` / `QueryResult<T>` shape, `Outputs` as map.
- TASK-0001 — types and the bus surface.
- `docs/architecture/engine-runtime-boundaries.md`.

## Outputs

- `Engine.Cli` project compiles, references only `Engine.Core` and `Engine.Contracts`.
- `dotnet run --project Engine.Cli -- apply NoOp --param echo=hello` returns exit `0` and prints a JSON `CommandResult` with `"status": "Applied"` and `"outputs": {"echo": "hello"}`.
- `dotnet run --project Engine.Cli -- apply Unknown` returns exit `1` and prints a JSON `CommandResult` with `"status": "Rejected"` and `"error": {"code": "E-CMD-UNKNOWN", ...}`.
- `dotnet run --project Engine.Cli -- query Anything` returns exit `1` and prints a JSON `QueryResult` with `"error": {"code": "E-QRY-UNKNOWN", ...}`.
- `dotnet run --project Engine.Cli` (no args) returns exit `2` and prints usage to stderr.
- All listed tests pass.
- Solution updated to include `Engine.Cli`. Existing projects unchanged.
- `docs/CURRENT-STATE.md` v0.2 entry referencing this task and ADRs 0002/0008.

## Files

**Created:**
- `Engine.Cli/Engine.Cli.csproj`
- `Engine.Cli/Program.cs`
- `Engine.Cli/Cli.cs`
- `Engine.Cli/ArgParser.cs`
- `Engine.Cli/JsonRenderer.cs`
- `Engine.Cli/Usage.cs`
- `Engine.Tests/Cli/CliApplyTests.cs`
- `Engine.Tests/Cli/CliQueryTests.cs`
- `Engine.Tests/Cli/CliUsageTests.cs`

**Modified:**
- `3DEngine.sln` — add `Engine.Cli` project entry.
- `Engine.Tests/Engine.Tests.csproj` — add `Engine.Cli` project reference (so tests can call `Cli.Run` and reference internals).
- `docs/CURRENT-STATE.md` — add a v0.2 entry referencing this task.

**Do not touch:**
- `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, `Vortice.Vulkan.*`.
- `Engine.Contracts/**`, `Engine.Core/**`.
- `docs/diagnostics.md` (no new codes in this task).
- ADRs.

## Tests

- `CliApplyTests.Apply_NoOp_With_Echo_Returns_Exit0_And_Json_Status_Applied_With_Echo_Output`
- `CliApplyTests.Apply_Unknown_Command_Returns_Exit1_And_Json_Error_E_CMD_UNKNOWN`
- `CliApplyTests.Apply_NoOp_Missing_Echo_Returns_Exit2_With_Usage_On_Stderr`
- `CliApplyTests.Apply_NoOp_Malformed_Param_Returns_Exit2_With_Usage_On_Stderr`
- `CliQueryTests.Query_Anything_Returns_Exit1_And_Json_Error_E_QRY_UNKNOWN`
- `CliUsageTests.NoArgs_Returns_Exit2_And_Prints_Usage_To_Stderr`
- `CliUsageTests.Help_Verb_Returns_Exit0_And_Prints_Usage_To_Stdout`
- `CliUsageTests.Unknown_Verb_Returns_Exit2_And_Prints_Usage_To_Stderr`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes all listed tests plus the existing TASK-0001 suite.
3. `Engine.Cli` references only `Engine.Core` and `Engine.Contracts`. Dependency check confirms.
4. No file under `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, `Vortice.Vulkan.*`, `Engine.Contracts/`, `Engine.Core/`, or `docs/adr/` is modified.
5. No new diagnostic code is introduced. The CLI surfaces only `E-CMD-UNKNOWN` and `E-QRY-UNKNOWN` from the existing registry.
6. Exit codes match the table above for every test scenario.
7. No sentinel `Command`/`Query`-derived types exist in `Engine.Cli` or anywhere else.
8. `docs/CURRENT-STATE.md` lists v0.2 with this task id and a one-line summary referencing ADRs 0002/0008.

## Notes for the implementer

- **Authority is the bus for known commands; the CLI for unknown names.** `CommandBus.Apply(Command)` requires a concrete `Command`. `Command` is abstract. Submitting unknowns would require a sentinel. Per task constraint, no sentinels — so the CLI directly produces the `Rejected` result with the registry's `E-CMD-UNKNOWN` code. The bus is still authoritative for every command we *can* construct (only `NoOp` in P1).
- **Outputs is a map at the wire.** ADR 0008 §3 says `Outputs : map<string, value>`. Default `JsonSerializer` would render the `Outputs` record as `{"values": {...}}`. Use a custom `JsonConverter<Outputs>` that writes only the `Values` map. The CLI does not deserialize `Outputs`; only the writer is needed.
- **No System.CommandLine.** `--param k=v` is small enough to hand-roll. The wire-format task can revisit when JSON input arrives.
- **Single-shot Document.** Every CLI invocation builds a fresh `Document`. Cross-invocation state is a persistence concern. Document this in `Usage.cs` text.
- **Tests are in-process.** Calling `Cli.Run` directly with `StringWriter` instances is faster and more deterministic than spawning `dotnet run`.
- **`TreatWarningsAsErrors`** consistent with the other Engine.* projects. Nullable on. ImplicitUsings on.
- **Why JSON now, not later (against TASK-0002's earlier deferral):** the wire format is the contract clients depend on. Plain text would be replaced and tests rewritten; JSON now, even with a hand-rolled converter, is cheaper than two passes. JSON-input dispatch is still deferred — that is where persistence and schema lookup intersect, and ADR-0008 §9 punts schemas until the HTTP API.
