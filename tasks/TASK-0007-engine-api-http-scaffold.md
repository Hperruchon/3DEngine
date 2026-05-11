# TASK-0007 — Engine.Api.Http scaffold (P6.1)

## Status

Done — shipped in commit `3093ed2`. See `docs/CURRENT-STATE.md` v0.7.

## Context

P6 introduces the HTTP/WebSocket transport surface. ADR-0008 §6 specifies two HTTP route families (`POST /commands`, `POST /queries`); ADR-0005 §5 specifies the WebSocket reconnect protocol; ADR-0006 §7 specifies the idempotency cache; ADR-0008 §9 specifies the schema endpoints. ADR-0005 §1 is explicit that the *wire format* (HTTP framing, body shape, status code mapping, WebSocket binary vs text) is implementation choice that follows from the contract.

P6 is too large for one TASK. It splits naturally along ADR axes:

- **P6.1 — TASK-0007 (this task).** `Engine.Api.Http` scaffold: project, `POST /commands`, `POST /queries`. One command registered (`NoOp`), query registry empty — mirroring TASK-0002's CLI. No WS, no idempotency cache, no schema endpoints.
- **P6.2 — TASK-0008 (future).** Idempotency cache per ADR-0006 §7.
- **P6.3 — TASK-0009 (future).** WebSocket event stream + reconnect cursor per ADR-0005 §§4–5. Pre-requisite: the snapshot format for `subscription.reset` (ADR-0005 §5, "blocked on the persistence ADR") needs to be resolved. May require a new ADR.
- **P6.4 — TASK-0010 (future).** Schema endpoints `/schema/{commands,queries,events,diagnostics}` per ADR-0008 §9.

This task ships P6.1. It is the parity layer that makes the engine accessible from anywhere with HTTP, matching the CLI's verb shape but at the wire.

## Goal

Stand up `Engine.Api.Http` as a thin HTTP host that:

- Builds a fresh in-process engine (`Document` + `CommandBus` + `QueryBus`) **per process**. The host keeps the engine alive across requests; restarting the host resets state. This matches the V1 clamp "No persistence — in-memory only."
- Exposes `POST /commands` accepting a JSON body, dispatches through `CommandBus.Apply`, and returns the `CommandResult` as JSON.
- Exposes `POST /queries` accepting a JSON body, dispatches through `QueryBus`, and returns the `QueryResult` as JSON.
- Registers exactly one command (`NoOp`); query registry stays empty.
- Returns clear HTTP status codes for transport problems (malformed body, wrong method, wrong content-type). HTTP status is always `200` when the engine answered — `CommandResult.Status` carries the engine's verdict, exactly as in the CLI's JSON. This is parity with ADR-0002 §3 (CLI as canonical client): clients consume the structured result, not a status code.

No WS, no idempotency, no schema endpoints, no auth, no TLS. Those are subsequent TASKs.

## Scope (in)

1. **New project: `Engine.Api.Http`**
   - .NET 10 ASP.NET Core minimal-API host. `AssemblyName = engine-api-http`.
   - References only `Engine.Core` and `Engine.Contracts` (authority diagram).
   - `InternalsVisibleTo Engine.Tests` so scenario tests can run the host in-process.
   - Standard ASP.NET Core only (`Microsoft.AspNetCore.App` framework reference). No third-party packages beyond what the SDK ships.
   - `TreatWarningsAsErrors`, nullable, implicit usings — consistent with the other `Engine.*` projects.

2. **Host bootstrap**
   - `Program.cs` builds a `WebApplication`. Registers a singleton engine (one `Document` + `CommandBus` + `QueryBus` + `InMemoryEventSink`). Registers exactly one command (`NoOp`) and an empty query registry.
   - `internal sealed class EngineHostFactory` (test seam) constructs the engine; tests substitute it to assert state.
   - The host listens on `http://localhost:5099` by default; the port is configurable via `ASPNETCORE_URLS` (standard ASP.NET Core convention).

3. **`POST /commands`**
   - Request `Content-Type: application/json`. Body shape:
     ```
     {
       "name": string,                    // required, e.g. "NoOp"
       "schemaVersion": integer,          // required, e.g. 1
       "parameters": object,              // required, may be {}
       "commandId": string?,              // optional UUID; server generates if omitted
       "expectedDocumentVersion": long?   // optional
     }
     ```
   - For `name == "NoOp"`: build `NoOpCommand { CommandId, Echo = parameters["echo"] }` and submit through `CommandBus.Apply`. Bus is authoritative for `Status`, `AppliedAtSeq`, `DocumentVersion`, `Outputs`, `DurationMs`.
   - For any other `name`: synthesise a `Rejected` `CommandResult` with `Error.Code = "E-CMD-UNKNOWN"`. Same rationale as TASK-0002 §4: `Command` is abstract; sentinels are forbidden; the API surfaces the existing registry diagnostic.
   - Response: HTTP `200`, `Content-Type: application/json`, body = the `CommandResult` JSON. Same shape and converter as the CLI (`Outputs` as bare map per ADR-0008 §3).

4. **`POST /queries`**
   - Request `Content-Type: application/json`. Body shape:
     ```
     {
       "name": string,
       "schemaVersion": integer,
       "parameters": object
     }
     ```
   - Every name returns a `Rejected` `QueryResult<object>` with `E-QRY-UNKNOWN` (registry empty, same as TASK-0002 §5).
   - Response: HTTP `200`, `Content-Type: application/json`, body = `QueryResult` JSON.

5. **Transport-level errors**
   - Body missing or not valid JSON → `400 Bad Request`, body: `{"error": {"code": "E-API-BAD-REQUEST", "message": "..." }}`.
   - Missing required field (`name`, `schemaVersion`) → `400`, same envelope.
   - `Content-Type` not `application/json` → `415 Unsupported Media Type`.
   - Wrong method on the route → `405 Method Not Allowed`.
   - Unknown route → `404 Not Found`.
   - These are API-layer errors, not engine errors. They never reach `CommandBus`.

6. **New diagnostic code**
   - `E-API-BAD-REQUEST` — surfaces transport-level malformed input. Registered in `docs/diagnostics.md` and in `Engine.Core/DiagnosticCodes.cs` (the existing registry consumer).
   - This is the only new code in this TASK. The bus still owns `E-CMD-UNKNOWN`, `E-CMD-VERSION-STALE`, `E-CMD-BUS-BUSY`, `E-QRY-UNKNOWN`.

7. **Tests in `Engine.Tests/Http/`**
   - Use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>` to run the host in-process. Test project adds the NuGet reference.
   - Cover: NoOp success, unknown command, query rejection, malformed body, missing fields, wrong content-type, wrong method, unknown route.
   - Each test deserialises the response body and asserts on `Status`, `Outputs`, `Error.Code` — exactly like `CliApplyTests`.

8. **Documentation**
   - `docs/CURRENT-STATE.md` — v0.7 entry referencing this task.
   - `docs/roadmap.md` — split P6 into P6.1–P6.4; mark P6.1 Shipped, P6.2–P6.4 Pending under V1.x.
   - `docs/diagnostics.md` — append `E-API-BAD-REQUEST`.

## Scope (out)

- **WebSocket / events.** Deferred to P6.3 (TASK-0009).
- **Idempotency cache.** Deferred to P6.2 (TASK-0008). The request body accepts `commandId` so the future cache wires in without a contract break.
- **Schema endpoints.** Deferred to P6.4 (TASK-0010).
- **Multi-Document / multi-tenant routes.** V1 clamp; one Document per host process.
- **Auth, HTTPS, TLS termination, CORS.** V1 host is `http://localhost`-only. Production hosting is its own task.
- **HTTP status code mapping for engine verdicts.** `CommandResult.Status` stays in the JSON body; HTTP status reflects only "did the API process the request." Revisiting this is a follow-up open question, not a scaffold decision.
- **Process-level integration tests** (spawn `engine-api-http` binary, fire `curl`). The in-process `WebApplicationFactory` pattern is faster and deterministic.
- **Cancellation token propagation from HTTP request abort to `CommandBus`.** Useful but adds complexity; deferred.
- **Persistence between restarts.** Per V1 clamp.

## Inputs

- ADR-0002 (headless-first; HTTP/CLI parity) — verb set, "if it only works in the UI, it is not implemented" rule.
- ADR-0004 (Engine Runtime is authority) — HTTP host is a thin client.
- ADR-0005 (event stream) — confirms HTTP-only scaffold does not need event-stream wiring yet; §1 confirms wire format is implementation choice.
- ADR-0006 (command execution model) — `CommandId`, `expectedDocumentVersion`, idempotency cache shape (cache itself deferred to P6.2).
- ADR-0008 (triad + schema endpoints) — `POST /commands`, `POST /queries` route families and result shapes.
- TASK-0001 — bus / registry surface.
- TASK-0002 — CLI's `NoOp` dispatch and `Outputs` JSON converter (the same converter is reused).

## Outputs

- `Engine.Api.Http` project compiles, references only `Engine.Core` and `Engine.Contracts`.
- `dotnet run --project Engine.Api.Http` starts the host on `http://localhost:5099`.
- `curl -s -X POST -H 'Content-Type: application/json' http://localhost:5099/commands -d '{"name":"NoOp","schemaVersion":1,"parameters":{"echo":"hello"}}'` returns a JSON `CommandResult` with `"status": "Applied"` and `"outputs": {"echo": "hello"}`. HTTP status 200.
- `curl -s -X POST -H 'Content-Type: application/json' http://localhost:5099/commands -d '{"name":"Unknown","schemaVersion":1,"parameters":{}}'` returns a JSON `CommandResult` with `"status": "Rejected"` and `"error": {"code": "E-CMD-UNKNOWN", ...}`. HTTP status 200.
- `curl -s -X POST -H 'Content-Type: application/json' http://localhost:5099/queries -d '{"name":"Anything","schemaVersion":1,"parameters":{}}'` returns a JSON `QueryResult` with `"error": {"code": "E-QRY-UNKNOWN", ...}`. HTTP status 200.
- `curl -s -X POST -H 'Content-Type: application/json' http://localhost:5099/commands -d 'not-json'` returns `400` with `{"error": {"code": "E-API-BAD-REQUEST", ...}}`.
- `Engine.Tests/Http/*` tests pass alongside the existing 33 tests.
- `docs/diagnostics.md` lists `E-API-BAD-REQUEST`.
- `docs/CURRENT-STATE.md` v0.7 entry references TASK-0007.
- `docs/roadmap.md` shows P6 split into P6.1–P6.4 with P6.1 Shipped under V1.x.

## Files

**Created:**
- `Engine.Api.Http/Engine.Api.Http.csproj`
- `Engine.Api.Http/Program.cs`
- `Engine.Api.Http/EngineHostFactory.cs`
- `Engine.Api.Http/Endpoints/CommandsEndpoint.cs`
- `Engine.Api.Http/Endpoints/QueriesEndpoint.cs`
- `Engine.Api.Http/Json/ApiJson.cs` — `JsonSerializerOptions` (camelCase, enum-as-string, custom `Outputs` converter — same one the CLI uses; lifted into a shared place is out of scope, so duplicate the converter here).
- `Engine.Api.Http/Errors/ApiErrorEnvelope.cs` — the `{"error":{"code","message"}}` shape for 4xx responses.
- `Engine.Tests/Http/CommandsEndpointTests.cs`
- `Engine.Tests/Http/QueriesEndpointTests.cs`
- `Engine.Tests/Http/TransportErrorTests.cs`
- `tasks/TASK-0007-engine-api-http-scaffold.md` (this file)

**Modified:**
- `3DEngine.sln` — add `Engine.Api.Http` project entry.
- `Engine.Tests/Engine.Tests.csproj` — add `Engine.Api.Http` project reference and `Microsoft.AspNetCore.Mvc.Testing` package reference.
- `Engine.Core/DiagnosticCodes.cs` — add `ApiBadRequest` constant.
- `docs/diagnostics.md` — append the `E-API-BAD-REQUEST` row and (if not yet present) the `API` subsystem token.
- `docs/CURRENT-STATE.md` — add v0.7 entry.
- `docs/roadmap.md` — split P6; move P6.1 to Shipped V1.x; P6.2/3/4 stay Pending under V1.x.

**Do not touch:**
- `Engine.Contracts/**`. No new contract types; reuse `Command`, `Query`, `CommandResult`, `QueryResult`, `Outputs`, `Diagnostic`, `ErrorDetail`, `NoOpCommand`.
- `Engine.Cli/**`. No CLI changes.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*`. Out of authority graph.
- ADRs.

## Tests

- `CommandsEndpointTests.PostCommands_NoOp_With_Echo_Returns_200_And_Json_Status_Applied`
- `CommandsEndpointTests.PostCommands_Unknown_Command_Returns_200_And_Json_Status_Rejected_E_CMD_UNKNOWN`
- `CommandsEndpointTests.PostCommands_With_ExpectedDocumentVersion_Mismatch_Returns_200_And_Rejected_E_CMD_VERSION_STALE`
- `CommandsEndpointTests.PostCommands_Echoes_Client_Supplied_CommandId`
- `CommandsEndpointTests.PostCommands_Generates_CommandId_When_Body_Omits_It`
- `QueriesEndpointTests.PostQueries_Anything_Returns_200_And_Json_QueryResult_E_QRY_UNKNOWN`
- `TransportErrorTests.PostCommands_With_Malformed_Json_Returns_400_With_E_API_BAD_REQUEST`
- `TransportErrorTests.PostCommands_With_Missing_Name_Returns_400_With_E_API_BAD_REQUEST`
- `TransportErrorTests.PostCommands_With_Wrong_Content_Type_Returns_415`
- `TransportErrorTests.GetCommands_Returns_405`
- `TransportErrorTests.Unknown_Route_Returns_404`

## Acceptance criteria

1. `dotnet build` succeeds; `Engine.Api.Http` references only `Engine.Core` and `Engine.Contracts`.
2. `dotnet test` passes — existing 33 tests plus all new tests listed above.
3. The diagnostics-registry gate (TASK-0003) still passes: every code literal in the new sources appears in `docs/diagnostics.md`. The new `E-API-BAD-REQUEST` is in the registry.
4. Manual `curl` against a running host matches the four examples in §Outputs.
5. No file under `Engine.Contracts/**`, `Engine.Cli/**`, `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, or `Vortice.Vulkan.*` is modified.
6. The PR opens against `main` and the CI workflow (`build-and-test`, `headless-smoke`, `contract-gate`) passes.
7. `docs/CURRENT-STATE.md` v0.7 entry exists.
8. `docs/roadmap.md` shows P6 split into P6.1–P6.4; P6.1 Shipped.

## Notes for the implementer

- **`Program.cs` must be reachable to `WebApplicationFactory<Program>`.** ASP.NET Core minimal APIs emit a `Program` class implicitly; mark it `public` via `public partial class Program {}` at the bottom of `Program.cs` so the test factory can reference it.
- **Single engine instance per host.** A singleton registered with the DI container: `services.AddSingleton<EngineHost>()` where `EngineHost` owns the `Document`, `CommandBus`, `QueryBus`, `InMemoryEventSink`. The two endpoints inject it.
- **`Outputs` JSON converter.** TASK-0002 introduced the bare-map converter. Duplicate it here for now (no shared types library yet); lifting it into a shared place is its own refactor.
- **Why the new diagnostic code lives in `Engine.Core`.** Per the diagnostics convention (P2), every code's constant lives in `Engine.Core/DiagnosticCodes.cs`. `Engine.Api.Http` references the constant by name. The `API` subsystem token may be new — if so, append it to `docs/diagnostics.md`'s subsystem list.
- **CommandId optional in the body.** The future idempotency TASK relies on the client controlling `CommandId`. Accept it now (even though no cache yet) so the wire shape doesn't break later.
- **Why HTTP 200 for engine rejections.** ADR-0002 / ADR-0008 frame the structured result as the contract; HTTP status would duplicate it weakly. Treating HTTP status as "did the API process the request" keeps the layers clean. Status code mapping is an explicit follow-up open question, not the scaffold's job.
- **`5099` is arbitrary.** Just needs to not collide with common dev ports. Configurable via `ASPNETCORE_URLS` like any ASP.NET Core app.
- **CI implication.** This TASK changes `Engine.Contracts/` indirectly? No — only `Engine.Core/DiagnosticCodes.cs` is modified (adds one constant). That is `Engine.Core`, not `Engine.Contracts`. The contract-gate CI job (TASK-0006) will NOT fire on this PR.
- **`Microsoft.AspNetCore.Mvc.Testing` is a Microsoft.* package**; same trust level as the SDK itself. Acceptable per the existing "standard library only" stance of TASK-0002 for the test seam.
