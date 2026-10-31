# Current state

What is built today. One short entry per shipped milestone. Reference ADRs by ID; do not restate decisions.

## v0.1 — Engine Runtime spine (P0, TASK-0001)

In-memory, in-process Engine Runtime. No transport, no clients, no persistence, no geometry backend.

**Built:**
- `Engine.Contracts` — `Command`, `CommandResult`, `Outputs`, `Diagnostic`, `ErrorDetail`, `Query`, `QueryResult<T>`, `EventRecord`, `Document`, `BodyHandle`, capability marker interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `IBRepOps`, `IFeatureIdMap`), handler abstractions.
- `Engine.Core` — `CommandBus` (serial, atomic commit-at-end), `QueryBus` (empty registry), `CommandRegistry`, `QueryRegistry`, `InMemoryEventSink` (bounded ring), `Replay`, `DiagnosticCodes`, `NoOpCommand` + handler.
- `Engine.Tests` — 9 tests covering command application, version-stale rejection, query rejection, monotonic Seq, ring eviction, replay determinism.

**Verified by:** `dotnet build` + `dotnet test` green.

**Decisions in force:** ADRs 0001–0008.

**Deferred:** HTTP/WS transport, CLI, concrete geometry backend, persistence, idempotency cache, schema endpoints, undo/redo. See V1 scope clamps in [CLAUDE.md](../CLAUDE.md).

## v0.2 — Engine.Cli (P1, TASK-0002)

In-process CLI per ADRs 0002, 0008. Verbs `apply` and `query`; JSON output to stdout. Only `NoOp` registered; query registry empty. Exit codes `0` Applied, `1` Rejected/Cancelled, `2` invalid usage. `dotnet build` + `dotnet test` green.

## v0.3 — Diagnostics registry CI gate (P2, TASK-0003)

Test-time gate enforcing CLAUDE.md's diagnostic-codes rule. Scans `Engine.Contracts/`, `Engine.Core/`, `Engine.Cli/` `.cs` sources for tokens matching `<E|W|I>-<SUBSYSTEM>-<tag>` and fails `dotnet test` if any are absent from `docs/diagnostics.md`. Implements the third gate in CLAUDE.md's gate list (after build and test). No production code changes; no new diagnostic codes. `dotnet build` + `dotnet test` green (31 tests).

## v0.4 — `3DEngine.Core` peer render kernel (P3, TASK-0004, ADR-0009)

`3DEngine.Core` is now part of the authority diagram as a peer render kernel to `Engine.Core`. The two kernels are mutually unreferenceable; render-capable hosts reference both and own the projection (events → render state). No code, csproj, or test changes — documentation only. CLAUDE.md, the ADR index, and the roadmap updated accordingly. `dotnet build` + `dotnet test` green (31 tests).

## v0.5 — Replay determinism CI gate (P4, TASK-0005)

Test-time gate completing CLAUDE.md's gate list (build, test, dependency direction, diagnostic codes registered, **replay determinism fixture**). A hand-authored fixture (three `NoOpCommand`s with stable `CommandId` GUIDs) is replayed via `Replay.ReplayLog`; the resulting `Document.Version`, `Document.Log`, and event sequence (`Seq`/`Kind`/`CauseCommandId`) are asserted against a hand-authored snapshot. A second test runs the replay twice and asserts the two runs match each other. Both modulo `Timestamp`/`DocumentId` per ADR-0005. No production code changes; no new diagnostic codes. `dotnet build` + `dotnet test` green (33 tests).

## v0.6 — Workflow gates (P5, TASK-0006)

`.github/` introduced: PR template, CODEOWNERS, and `workflows/ci.yml`. The workflow runs three jobs on push to `main` and on pull_request: **build-and-test** (`dotnet build` + `dotnet test`), **headless-smoke** (spawns the CLI binary and asserts NoOp's JSON output at process scope), and **contract-gate** (PR-only; fails when `Engine.Contracts/**` changes without a corresponding `docs/adr/**` change). No `Engine.*` code, test, or contract changes. V1 Pending is empty; the engine advances to V1.x.

## v0.7 — Engine.Api.Http scaffold (P6.1, TASK-0007)

First V1.x phase. New project `Engine.Api.Http` (ASP.NET Core minimal API) hosts `POST /commands` and `POST /queries` against an in-process engine, mirroring the CLI's dispatch and JSON shape. One command registered (`NoOp`); query registry empty. Transport errors (malformed body, missing required fields, wrong content-type, wrong method, unknown route) return `400`/`404`/`405`/`415` with an `E-API-BAD-REQUEST` envelope; engine verdicts always return `200` with the `CommandResult`/`QueryResult` JSON. New diagnostic code `E-API-BAD-REQUEST` registered in `docs/diagnostics.md`; new `API` subsystem token. References only `Engine.Core` and `Engine.Contracts` (authority diagram holds). No WebSocket, no idempotency cache, no schema endpoints — those are P6.2/P6.3/P6.4. `dotnet build` + `dotnet test` green (44 tests).

## v0.8 — Idempotency cache (P6.2, TASK-0008)

`Engine.Core/IdempotencyCache.cs` lands a FIFO-evicting `Dictionary<Guid, CommandResult>` (default capacity 1024 per ADR-0006 §7). `CommandBus.Apply` checks the cache inside its serial section: on hit, the cached `CommandResult` is returned with no new event, log entry, or Seq increment. On miss, the bus runs as before and stores the result on the way out. All three terminal states (Applied, Rejected, Cancelled) are cached — a retried `CommandId` always gets the same answer, which is the point of the cache. `CommandBus`'s constructor gains an optional `IdempotencyCache` parameter; existing callers (CLI, HTTP host, tests) compile unchanged. CLAUDE.md V1 clamps tightened: idempotency-cache clamp dropped; HTTP-transport clamp narrowed to WebSocket-only. No `Engine.Contracts/**` change. `dotnet build` + `dotnet test` green (52 tests).

## v0.9 — Schema endpoints (P6.4, TASK-0009)

Six `GET /schema/...` discovery endpoints on `Engine.Api.Http`, per ADR-0008 §9. Index endpoints (`/schema/commands`, `/schema/queries`) generate from `CommandRegistry`/`QueryRegistry` via a new `Registered` accessor. `/schema/events` lists the documented event kinds from ADR-0005 §7 (hand-encoded; will become registry-driven when an event registry lands). `/schema/diagnostics` mirrors `Engine.Core/DiagnosticCodes.cs`. Per-item command schemas are hand-known for `NoOp@1`; the full schema-declaration mechanism is deferred to P7's first concrete command. A new gate test (`SchemaEndpointGateTests`) enforces ADR-0008 §9's "every registered command has a schema entry" rule and the diagnostics-mirror invariant via reflection. No `Engine.Contracts/**` change; no new diagnostic code. `dotnet build` + `dotnet test` green (61 tests).

## v0.10 — WebSocket event stream + reconnect (P6.3, TASK-0010, ADRs 0010 & 0011)

`GET /events` on `Engine.Api.Http` upgrades to WebSocket and delivers Document events with the cursor-based reconnect protocol of ADR-0005 §§4–5. The client sends one `subscribe { documentId?, lastSeenSeq? }` frame; the engine replies with `subscription.resume { fromSeq }` (cursor inside ring) followed by buffered + live events, or `subscription.reset { documentId, snapshot }` (cursor missing or stale) with the V1.x snapshot shape from ADR-0010 — `Document` metadata only, no `Log`, no render state. Per-subscriber bounded outbound channel (default 1024 per ADR-0005 §6); on overflow the engine closes the WebSocket with status `1008` + reason `subscriber.lagged` and other subscribers are unaffected. 30 s idle heartbeat (`{ "kind": "heartbeat" }`). New `BroadcastingEventSink` decorator wraps the existing `InMemoryEventSink`; `Engine.Core` is untouched. New diagnostic codes `W-API-WS-LAGGED` and `E-API-WS-INVALID-SUBSCRIBE` registered in `docs/diagnostics.md`, `DiagnosticCodes.cs`, and `/schema/diagnostics`. ADR-0011 frames the WebSocket as part of the canonical server-default deployment topology; ADR-0010 fixes the snapshot wire shape. `dotnet build` + `dotnet test` green (70 tests). P6 is complete.

## v0.11 — First geometry slice: `CreateBox` end-to-end (P7a, TASK-0011, ADRs 0012 & 0013)

First concrete geometry command and the wiring posture for all future geometry. ADR-0012 settles the V1 capability surface (`IGeometryBackend.TryGet<T>()`, `IMeshOps.CreateBox`, `IGeometryQuery.GetBoundingBox`, `BackendCapabilities` flags), the handler-to-backend access mechanism (`Handle` gains a non-null `IGeometryBackend` parameter; bus and `Replay.ReplayLog` accept the active backend; cache-recovery via replay-against-fresh-backend), the Document `Bodies` projection (handles + minimum metadata; mutated only inside `CommandBus.Apply`'s commit section), and deterministic body identity (`BodyHandle = new BodyHandle(command.CommandId)` for single-body create commands). ADR-0013 makes `/schema/commands/{name}@{version}` and `/schema/queries/{name}@{version}` pure projections from handler-declared `Parameters` / `Outputs` / `Result` schemas; `FieldSchema` moves to `Engine.Contracts/Schema/`; `SchemaCommandsEndpoint` and `SchemaQueriesEndpoint` lose all per-command branching; the schema gate test now compares endpoint JSON against handler declarations structurally and asserts the endpoint sources contain no name literals. V1.x first backend is the in-process managed `InProcessMeshBackend` per ADR-0012 §7 — no native interop; Manifold is deferred to P7b. New event kind `body.created` (consecutive Seq after `command.applied`); `subscription.reset` snapshot grows a `bodies` array per ADR-0012 §6 (closes ADR-0010's "Geometry snapshot shape" open challenge). New diagnostic codes `E-GEOM-CAP-MISSING`, `E-GEOM-INVALID-PARAM`, `E-GEOM-BODY-NOT-FOUND` registered in the three required places. Replay-determinism fixture extends to cover `CreateBox` (one `body.created` event between two NoOps; `Document.Bodies` projection determinism asserted). `engine apply CreateBox … && engine query GetBoundingBox …` work in CLI; `POST /commands` + `POST /queries` work over HTTP with parity. CLAUDE.md drops the "No concrete geometry backend" V1 clamp.

## v0.12 — Charter (governance, docs-only)

`docs/CHARTER.md` adds the missing "why" layer above the ADRs: mission (anchored on "we do not build a 3D app…"), target consumers (AI agents first-class), V1 definition-of-success stated as realized properties (points here for the ledger, does not re-enumerate), V1.x/V2 direction (points to roadmap), the Non-goals (deferred) vs Anti-objectives (refused-forever) split, and an agent-first scope test (refuse → defer → proceed → stop-and-ask). Promoted to CLAUDE.md navigation entry #1 with the read-order rule "charter says whether to act · CLAUDE.md says where · ADR says how · CURRENT-STATE says what exists." No phase, no TASK, no code/test/contract change; no new diagnostic codes. Authored via a 4-phase multi-agent workflow (gather → draft → 3 adversarial critique lenses → reconcile); the critique caught and removed a false "founding manifesto" provenance claim. Build + test unaffected.

## v0.13 — Navigation infrastructure (governance, docs-only)

Finishes the navigation layer the P1 experiment deferred. Three new docs, each scoped to **point, not duplicate** — they hold navigation and naming only, deferring *why* to the ADRs, *what exists* to this file, and *boundaries* to CLAUDE.md:

- `docs/INDEX.md` — one-page repo map (paths + one-line purpose for every area); links to `architecture/engine-runtime-boundaries.md` as the canonical boundary diagram rather than redrawing it.
- `docs/glossary.md` — canonical vocabulary, one line per term + a pointer to its defining file/ADR. Terms verified against `Engine.Contracts/` source (Command, Query, EventRecord, CommandResult, Outputs, Diagnostic, ErrorDetail, Document, Version, Seq, BodyHandle, Body, IGeometryBackend, capability/`TryGet<T>`, Replay, projection, design truth, the two kernels).
- `docs/conventions.md` — file/naming/grammar conventions derived from what the code already does (file-per-command, `*Command`/`*Handler`/`*Query` suffixes, diagnostic-code grammar, event-`Kind` grammar with control-frame carve-outs, the contract-change trigger checklist, and the inline file-header→ADR citation), each annotated CI-enforced vs convention-only.

Promoted to CLAUDE.md navigation: `INDEX.md` as #2 (the *where* map), `glossary.md`/`conventions.md` adjacent to `diagnostics.md` as the reference cluster (#6–8). No phase, no TASK, no code/test/contract change; no new diagnostic codes. Applied the CHARTER scope test: additive governance work, touches no `Engine.Contracts` shape. Build + test unaffected (docs-only).

## v0.14 — Manifold native geometry backend (P7b, TASK-0012, ADR-0014)

Swaps a real, native, double-precision Manifold backend in behind the v0.11 capability surface — no `Engine.Contracts` change. A new project `Engine.Geometry.Manifold` (references only `Engine.Contracts`, keeping native deps out of `Engine.Core`) hosts `ManifoldGeometryBackend : IGeometryBackend, IMeshOps, IGeometryQuery, IDisposable`, bound to Manifold **v3.5.2**'s `manifoldc` C API via source-generated `[LibraryImport]` P/Invoke — the repo's first hand-authored native binding (ADR-0014 §1) — with a `SafeHandle` encoding the verified *delete-owns-the-buffer* ownership. The CLI and HTTP hosts select Manifold when its native library is loadable and fall back to the managed `InProcessMeshBackend` otherwise, so the engine runs on any platform; this pragmatically resolves ADR-0014/TASK-0012's deferred RID-aware-fallback open question. The canonical replay-determinism gate stays on the managed stub, so core determinism is untouched.

The native payload ships as a multi-RID NuGet (`Engine.Geometry.Manifold.Native`, `runtimes/<rid>/native`) built **from source, serial (`MANIFOLD_PAR=OFF`, no TBB)** by a manual CI matrix workflow (`.github/workflows/build-manifold-native.yml`, win-x64 + linux-x64 + osx-arm64) and consumed via a local-folder feed (interim bootstrap; GitHub Packages is the eventual home). New diagnostic codes `E-GEOM-NATIVE-OP` (raised by `CreateBoxCommandHandler` on a backend op failure) and `E-GEOM-BACKEND-INIT` (reserved — the fallback subsumes it) registered in the three required places. New tests: `ManifoldGeometryBackendTests` (behavioural parity with the stub) and `ManifoldReplayRoundTripTests` (native replay reconstructibility), both skipped when native manifoldc is unavailable so Linux CI stays green on the stub. Verified on win-x64: `engine apply CreateBox` applies via native Manifold and the full solution builds + tests green. Also fixed a latent CI bug — `ci.yml` now installs the pinned SDK via `global-json-file` (the `10.x` + `include-prerelease` combo never satisfied `global.json`). V1.x is complete.

## v0.15 — First real Manifold operations: Translate + Subtract (P7c, TASK-0013, ADR-0012 Amendment 1)

Adds the first geometry operations that require a real kernel — a boolean **Subtract** (box A minus box B → a carved solid) and a **Translate** (move a body off the origin) — behind two new capability interfaces `ITransformOps` and `IBooleanOps` (new `BackendCapabilities.Transform`/`.Booleans`). Both are implemented **only** by the native `ManifoldGeometryBackend` (via `manifold_translate` / `manifold_difference`, extending the P7b `manifoldc` binding); the managed `InProcessMeshBackend` is untouched and honestly reports `E-GEOM-CAP-MISSING`, since it stores box dimensions only and cannot represent a moved or carved solid. Translate exists to make the cut observable: with origin-centered boxes a non-empty `A − B` keeps A's bounding box, so moving B off-center first lets `GetBoundingBox` witness the trim — no new query.

Each op produces a **new** body (handle = `CommandId`, ADR-0012 §4), leaves its operands intact, and reuses the existing `body.created` event (no new event kind); the result's `Kind` is `"Solid"`. Handlers validate operand existence in `Document.Bodies` → capability → backend op, so a bad reference reports `E-GEOM-BODY-NOT-FOUND` on any backend and the one-shot CLI stays deterministic; a fully-consumed difference (subtrahend ⊇ minuend) rejects as a degenerate `E-GEOM-NATIVE-OP`. **No new diagnostic codes** — the existing `GEOM` codes cover every path. Wired through the CLI (`apply Translate` / `apply Subtract`) and HTTP (`POST /commands`), with `/schema/commands` auto-projecting both. The canonical replay-determinism gate stays stub-backed and unchanged; a separate native-gated round-trip replays create → create → translate → subtract twice and asserts identical state. `Engine.Contracts` change is confined to the two interfaces + two flags (gated by the ADR amendment). Verified on win-x64: the full solution builds + tests green with the native path exercised (a 10-cube minus an off-center 10-cube trims maxX from 5 to 0).

## v0.16 — A released SDK pin (P0.1, TASK-0014)

`global.json` pinned the SDK version `10.0.300-preview.0.26177.108` and set `rollForward` to
`disable`. No public feed supplies that preview SDK. Therefore each computer without that exact
build failed at SDK resolution, and the solution did not build. TASK-0006 recorded the risk at its
line 102 and predicted this outcome.

`global.json` now pins the released version `10.0.300`, sets `rollForward` to `latestFeature`, and
sets `allowPrerelease` to `false`. The policy accepts each SDK with the major version 10 and the
minor version 0, and it selects the highest one. A computer with 10.0.300 or with 10.0.400 builds
the solution.

Two documentation files gave the SDK version `10.0.200-preview.0.26103.119`, which no file pinned.
Each file now gives "10.0.300 or higher" and points to `global.json`. Rule 10 in `CLAUDE.md` permits
this change to `3DEngine/` and to `BlazorApp/`, because this task gave that scope.

`.github/workflows/ci.yml` needed no change. It installs the SDK from `global.json` already.

Verified on win-x64: the resolved SDK is 10.0.400. `dotnet build` passes with zero warnings and zero
errors. `dotnet test` passes 134 tests and skips none. No code changed, no contract changed, and no
diagnostic code was added.

Register entry R-0001 is closed.

## v0.17 — The charter and each operational rule (P0.2, TASK-0015)

A review of five steps examined the twenty objectives, the anti-objectives, the deferred non-goals
and the operational rules. The owner approved each result. This entry records the change to the
documents. No code changed.

**`docs/CHARTER.md` is replaced.** The mission is now "We do not build one application. We build the
parts that many applications use." The previous sentence, "We do not build a 3D app", had a correct
intent and incorrect words, and it blocked the renderer objective.

Two anti-objectives are amended, because each one blocked approved work. "A command lands completely
or it does not land" now states that it does not apply to a rebuild, therefore a feature tree can
hold a failed feature. "No needless future-proofing" now uses the cost test: add a property now if a
later change must rewrite the log or migrate each document.

Two anti-objectives gain a rule. Anti-objective 2 states that an interactive tool sends provisional
commands to the log and never uses a separate buffer. Anti-objective 10 states that a command records
the backend that performed the operation, therefore a replay never selects a backend.

Six anti-objectives are new. Four give the refused rungs of kernel ambition, by name and with a
measurement: no fillet on an edge chain, no general NURBS surface, no shape healing, and no general
surface intersector. One refuses a solver for fluid dynamics, a mesh generator and an engine for
molecular dynamics. One states that a replay gives the same result on each supported platform.

The charter gains a section for the kernel boundary. The owned kernel gives identity, and Manifold
gives geometry. The feature boundary is closed: five surfaces, three curves, seven topology types,
six operations, and one global tolerance.

The deferred list has one item on each line. Six items are promoted and are absent from the list:
persistence, undo and redo, B-Rep operations, fillet as a later rung, feature identifiers, and
tessellated meshes for a client. One item was obsolete and is removed, because v0.14 shipped the
native Manifold backend.

**`CLAUDE.md`** loses the persistence clamp. It gains nine operational rules: six for determinism and
three for the kernel. The rules forbid a transcendental function in the command path, a rotation
stored as an angle, `Math.FusedMultiplyAdd`, four APIs whose guarantee covers one process, a 32-bit
x86 runtime identifier, a tessellation in the Document, a tolerance on an entity, a general surface
intersector, and a solver result in the log. One rule is marked inactive until milestone P0.3,
because a person cannot obey it today. One reevaluation condition is added: report to the owner when
evidence shows that an objective needs a change.

**`docs/INDEX.md`** now agrees with `CLAUDE.md` about the projects outside the engine spine. The form
is conditional: do not change them unless a task gives that scope. The reference to a section named
"Do not touch" is repaired, because that section was renamed. The three new documents are in the map.
The entry for `engine-runtime-boundaries.md` records that the file is not current.

**`docs/roadmap.md`** is replaced. It now holds four tracks: foundations, the platform, the renderer
and the kernel, plus machine vision. Each approved objective has a track. This change repairs the
scope test in the charter, which treats the absence of a track as a stop signal.

`docs/register.md`, `docs/working-agreement.md` and `docs/templates.md` change from `Proposed` to
`Accepted`.

Verified: `dotnet build` passes with zero warnings and zero errors. `dotnet test` passes 134 tests
and skips none. No contract changed, and no diagnostic code was added.

## v0.18 — Handler-declared construction (P0.3, TASK-0016, ADR-0016)

Each host held a switch statement on the command name, and each case held parameter parsing written
by hand. A new command changed about ten files, and two of them were central. Therefore two agents
that added two commands collided each time. Registration was duplicated across seven positions, and
register entry R-0003 recorded the consequence: the canonical replay determinism gate registered two
handlers while each host registered five.

ADR-0013 already made each handler the source of truth for its schema. Nothing consumed that
declaration to build a command. ADR-0016 adds the step that reads it.

**`Engine.Contracts` change, gated by ADR-0016.** `ICommandHandler` gains `Command Create(CommandInput)`
and `IQueryHandler` gains `Query Create(QueryInput)`. Each input type is a record that carries the
bound parameters, the identifier and the optional expected version. A record avoids a second contract
change when a field arrives later.

**`Engine.Core/Hosting/ParameterBinder.cs`** converts raw values into typed values against the
declared `FieldSchema`. It reports a missing required field, an incorrect type and an unknown field,
each with the field name. Each parse uses the invariant culture, therefore a machine with a comma
decimal separator produces the same number. The types `object` and `array` stay deferred and fail
with a message that names the declared type.

**`Engine.Core/Hosting/HandlerCatalog.cs`** holds one explicit list of each handler. The list is not
a scan of the assembly, because replay determinism needs a fixed set and a fixed order. Each host and
each replay gate call `HandlerCatalog.RegisterAll`. Register entry R-0003 is closed at its source.

Both switch statements are removed. `Engine.Cli/Cli.cs`, `Engine.Api.Http/Endpoints/CommandsEndpoint.cs`
and `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` now follow the same three steps: find the handler,
bind, then call `Create`. `Engine.Cli/Usage.cs` generates the command list and each example from the
handler declarations, therefore no file in `Engine.Cli` names a command. The HTTP surface converts a
`JsonElement` to a neutral value and keeps a number as a double, so no number passes through a
string; `Engine.Core` therefore needs no reference to `System.Text.Json`.

**The rule becomes active.** `CLAUDE.md` marked one anti-pattern inactive in v0.17, because a person
could not obey it. `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` now enforces it: a quoted
command name in a host file fails the gate and the failure names the file and the line. A deliberate
violation was injected and the gate failed as designed, then the violation was removed.

New tests: 17. `ParameterBinderTests` covers each declared type, an unknown field, a value that is
already typed, and the invariant culture in both directions under the `fr-FR` culture.
`DispatchSurfaceGateTests` covers the host surface, the catalog, the stable order, and a round trip
from each declared schema through `Create`.

Three CLI tests changed their assertion. Each one asserted the exact wording of a message that a
hand-written parser produced. The binder produces a generic message, therefore each test now asserts
that the message names the field and the rejected value. The behaviour contract did not change: the
exit code is 2 and the usage text appears.

Verified: `dotnet build` on the solution gives zero errors. Two warnings remain in the vendored
sample framework and predate this task. `dotnet test` gives 151 passed, zero failed, zero skipped, up
from 134. The command-line host applies `NoOp` and `CreateBox` through the new path.

New diagnostic codes: none. The existing codes cover each path.

## v0.19 — Each governance rule becomes mechanical (P0.4, TASK-0017)

Four rules that existed only as text now fail a build. `docs/working-agreement.md` section 6.2 gives
the reason: a rule in text only is a rule that an agent will break.

**The register gate.** `Engine.Tests/Governance/RegisterGateTests.cs` reads `docs/register.md`. A
passed `due` date fails the build. A second extension fails the build. More than 20 open entries fails
the build. Each entry must declare a valid class and two dates. A `due` date must not be later than
the lifetime of its class; an earlier date is a deliberate tightening and stays permitted.

**The marker gate.** `Engine.Tests/Governance/MarkerGateTests.cs` reads live code and live
configuration. A word such as `TODO`, `interim` or `DRAFT` must cite an `R-nnnn` on its line or just
above it. The gate does not read an ADR, a closed task or the ledger, because each one is immutable or
append-only and the working agreement forbids a change to it. A gate that demanded such a change would
set two rules against each other.

**The ADR gate.** `Engine.Tests/Governance/AdrGateTests.cs` reads the front matter of each record.
Each ADR from 0001 to 0014 received front matter with `id`, `title`, `status`, `topic`, `date`,
`supersedes`, `superseded-by`, `amends`, `amended-by`, `affects` and `enforced-by`. The decision text
of each record is unchanged. A supersession field and an amendment field must be reciprocal. A status
must agree with those fields. The count of records with no enforcement must not grow past two.
`docs/adr/README.md` is regenerated from the front matter and a test holds the two in agreement.

**The dependency direction gate.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` reads
each project file in the working tree. It verifies each rule in `CLAUDE.md`, section "Dependency
rules": no reference from `Engine.Contracts`, only `Engine.Contracts` from `Engine.Core`, no reference
from `3DEngine.Core`, no reference to the render kernel from an `Engine.*` project, the permitted set
for a client, no reference between two clients, and no cycle. It needs no build, therefore it also
catches a reference that compiles. Register entry R-0004 recorded this gap since 2026-08-25.

**The amendment graph is now reciprocal.** ADR-0011 amends ADR-0004. ADR-0008 amends ADR-0006.
ADR-0016 amends ADR-0013. Each of the three amended records carries the status `Amended`. Register
entry R-0006 recorded the drift.

**Each gate was verified by injection.** A test that cannot fail is worth nothing, therefore each of
the four gates ran against a deliberate violation and each one failed as designed. The register gate
reported an overdue entry. The marker gate reported a `TODO` with no identifier in `CommandBus.cs`.
The ADR gate reported a one-way amendment. The dependency gate reported a reference from
`Engine.Core` to `3DEngine.Core`, named by two tests. Each violation was then removed.

**The first injection found a defect in the gate itself.** The dependency gate passed while the
violation existed. The cause: the repository holds three git worktrees under `.claude/worktrees/`,
and each one is a full copy of every project file pinned to an older commit. The graph is keyed by
project name, therefore a stale copy silently shadowed the real project and the gate reported a broken
rule as satisfied. The gate now excludes that directory, and a further test fails when one project
name appears more than once. This evidence is recorded on register entry R-0014, which asks whether
`.gitignore` must contain the directory.

New tests: 22. `dotnet test` gives 173 passed, zero failed, zero skipped, up from 151. `dotnet build`
on the solution gives zero errors and the same two warnings in the vendored sample framework.

Not done, and why: no tool generates the ADR index. A test that compares the index against the front
matter gives the same protection at a much lower cost. No check verifies the `writes` block of a task
file; that work needs a step in continuous integration and a decision about a local hook, therefore it
became register entry R-0017 with a limit of 2027-03-19.

Closed: R-0004, R-0006. Opened: R-0017. Open entries: 13 of 20.

New diagnostic codes: none. A gate is a test and a test does not emit a code.

## v0.20 — The unmerged branch is salvaged (P0.5, TASK-0018)

Branch `claude/happy-booth-1cef3f` held four identifiers that the main branch uses for different
work, and it held a working engine hosting factory that nobody merged. Register entry R-0012 recorded
that condition since 2026-08-25. Each identifier now names one thing.

**The engine hosting factory.** `Engine.Core/Hosting/EngineHosting.cs` gives `EngineKit` and
`EngineHosting.CreateDefault(backend)`. `Engine.Cli/Cli.BuildEngine` and
`Engine.Api.Http/EngineHost` both use it.

The factory is not the branch version. Three rules arrived after the branch and each one changed the
design. First, the factory takes `IGeometryBackend` as a parameter and does not construct one. ADR-0014
section 4 permits a reference to `Engine.Geometry.Manifold` only at the composition root of a host, and
`CLAUDE.md` permits `Engine.Core` to reference only `Engine.Contracts`; a factory that selected the
native backend would break both rules and the dependency direction gate of v0.19 would fail. Each host
therefore keeps its own three lines of backend selection, and that duplication is correct. Second, the
branch methods `RegisterDefaultCommands` and `RegisterDefaultQueries` are not salvaged, because
`HandlerCatalog` is the one list of handlers per ADR-0016 and a second registration surface is the
defect that register entry R-0003 recorded. Third, a verbatim merge would have removed `Translate` and
`Subtract` from each host, because the branch registered two handlers and the catalog holds four.

The kit gives `CreateCommandBus(sink)`, `CreateCommandBus()` and `CreateQueryBus()`. The reason that
the branch gave for not returning a bus still holds: the HTTP host wraps the sink in
`BroadcastingEventSink` before the bus sees it, and the command-line host uses the sink directly.

**ADR-0015 — command-log persistence.** The branch record is re-filed with the identifier 0015 and the
status `Proposed`. The design text is unchanged. The status is not `Accepted`, because no code
implements the design, no task is ready, and the clamp in `CLAUDE.md` still forbids persistence. An
accepted record with no implementation is the drift that the gates of v0.19 exist to prevent.
`docs/roadmap.md` phase P8a already said "ADR-0015" before this task ran.

**TASK-0019 — command-log persistence.** The branch task is re-filed with the number 0019 and the
status `Deferred`. Two conditions unblock it: the owner accepts ADR-0015, and `CLAUDE.md` drops the
clamp.

**The duplicate version label.** The branch also used the label v0.12 for the hosting factory. The
main line gives v0.12 to the charter. The branch label is void. This entry, v0.20, is the first ledger
entry for the hosting factory. The branch note that said "from this point forward, TASK numbers no
longer track CURRENT-STATE version numbers" is correct and stays true on the main line.

**The renderer proposal.** `docs/proposals/render-host-direction.md` existed only as an untracked file
inside `stash@{0}`, and no commit held it. Register entry R-0013 recorded the risk that the analysis
would disappear. A commit now holds it, with a header note that marks two statements as out of date:
the founding line that the v0.17 charter replaced, and the open decisions that roadmap track R settles
in part.

**One gate refinement.** The unenforced budget in `Engine.Tests/Governance/AdrGateTests.cs` now counts
a record in force only. A record with the status `Proposed`, `Withdrawn` or `Rejected` describes work
that nobody built or nobody will build, therefore enforcement cannot exist. The budget still bounds
each accepted decision at two.

New tests: 8. `dotnet test` gives 181 passed, zero failed, zero skipped, up from 173. `dotnet build`
gives zero errors.

Not done, and why: the branch task for the hosting factory is not re-filed as its own file. Its work
lands under TASK-0018, and a second file would give two records of one change. The branch and the
stash both stay, because a commit now holds each useful part and the owner decides when to remove
either one.

Closed: R-0012, R-0013. Open entries: 11 of 20.

New diagnostic codes: none.

## v0.21 — Three governance corrections (P0.8, TASK-0021)

The owner decided three items on 2026-09-21. Each one came out of the review of v0.19 and v0.20.

**One status vocabulary.** Three documents gave a different set of values for the ADR `status` field.
`CLAUDE.md`, section "Stop and ask", forbids an agent from correcting either side of a disagreement,
so the v0.20 report gave each side and stopped. The set is now `Proposed`, `Accepted`, `Amended`,
`Superseded`, `Withdrawn` and `Rejected`. `docs/templates.md` gained `Amended` and `Superseded`, and
it lost `Superseded-by-NNNN`, because the number belongs in the field `superseded-by`.

The gate also contradicted itself. `ValidStatuses` did not contain `Superseded`, and one test required
that value. The first superseded record would have failed one test whichever document was right. The
gate now accepts the value. This part was a defect and not a preference.

**The register limit is 15.** Register entry R-0016 asked whether a limit of 20 open entries is too
high to have an effect. The register held 11 open entries. A limit of 12, which the entry proposed,
gives one free slot, therefore the next new problem would force an exit on an old one immediately. A
limit of 15 gives four free slots. Rule 4 in `docs/register.md` and `RegisterGateTests.OpenLimit` both
give 15. R-0016 is closed with the exit `decided`.

**A correction to the v0.20 entry.** Working agreement rule 3.2 forbids a change to a past entry,
therefore this entry gives the correction.

The v0.20 entry, ADR-0015, TASK-0018 and TASK-0019 each stated that the clamp in `CLAUDE.md` forbids
persistence, and each used that statement to support the status `Proposed` on ADR-0015. The statement
was false. Milestone v0.17 removed the persistence clamp, in TASK-0015, four days earlier.
`CLAUDE.md`, section "Scope clamps", says "No clamp is active" and "Persistence arrives with ADR-0015
and its task".

The cause: the agent read the copy of `CLAUDE.md` that arrived in its context at the start of the
session, and not the file. The agent then changed that file itself and kept citing the old copy.

The conclusion does not change. Two true reasons remain for the status `Proposed`: no code implements
the design, and roadmap phase P8a is pending. The owner examined the question on 2026-09-21 and kept
the status. ADR-0015 and TASK-0019 each carry a correction note. TASK-0018 is closed, therefore its
text stays and a correction block follows it.

No gate can catch this class of error, because the wrong statement was about a file and it appeared in
prose. The defence is the habit of reading a file before citing it.

Tests: 181. No new test. `dotnet build` gives zero errors.

Closed: R-0016. Open entries: 10 of 15.

New diagnostic codes: none.

## v0.22 — The write set of a task becomes mechanical (P0.7, TASK-0022)

Register entry R-0017 recorded the last rule in `docs/templates.md` that had no check. Each task file
declares a `writes` block with a create list, a modify list and a forbid list. Nothing read that
block. An agent could change a file that the task forbids, and no check found the error.

**One implementation, two modes.** `Engine.Tests/Governance/WriteSetGateTests.cs` is a test and not a
script. Each static check always runs, therefore `dotnet test` covers it on a local computer and on
each of the three runners. The dynamic check runs when the environment variable `WRITE_SET_FILES`
gives a list of changed paths, one per line. The new job `write-set-gate` sets that variable from
`git diff --name-only`. The logic that continuous integration uses is the logic that the test suite
covers.

**Seven checks.** A changed path must not match a forbid pattern. A changed path must appear in a
create list or a modify list. A change must touch a task file, because `CLAUDE.md`, section
"Anti-patterns", forbids work outside the scope of the active task. A task must not both write and
forbid the same path. A declared path must use the forward slash and must not start with one. A task
with the status `Done` must have created each exact path in its create list. The count of task files
with no front matter must not grow past 13.

**A forbid beats a permit.** A change can touch more than one task file. The gate takes the union of
each create list and each modify list, and it refuses a path that matches any forbid pattern of any of
those tasks. A forbid records a boundary and a permit records an intention, therefore the strict
reading is the safe one. Only a task that the same change touches can govern that change, so a task
from an older change cannot authorise a file today.

**Each check was verified by injection.** The dynamic check ran four times. A legitimate change
passed. A change to `Engine.Contracts/Handlers/ICommandHandler.cs` reported the forbid pattern
`Engine.Contracts/**` and named TASK-0021 as its owner. A change to `docs/CHARTER.md` reported that no
list names it. A change with no task file reported that no write set governs it. The three static
checks each failed on a deliberate violation, and each message named the task and the path.

**The hook is refused, not deferred.** `docs/templates.md` described a hook before each commit and
said that the write set is only a recommendation without it. A hook needs an installation step on each
computer, and a person passes it with one flag. The gate runs where nobody can pass it. That row now
describes the gate.

**Task files 0001 to 0013 are exempt.** Each one predates `docs/templates.md` and carries no front
matter. The budget of 13 fails when the count grows, in the same way as the budget for an unenforced
ADR. A new task must declare its write set.

New tests: 6. `dotnet test` gives 187 passed, zero failed, zero skipped, up from 181. `dotnet build`
gives zero errors.

Not done, and why: the gate reads the tasks that a change touches and not a status. A change that
touches a task with the status `Deferred` would use that write set. No such change exists today, and a
stricter rule needs evidence before it earns its cost.

Closed: R-0017. Open entries: 9 of 15. Track 0 now has one pending phase, P0.6, which needs a pull
request before continuous integration can report.

New diagnostic codes: none.

**The gate found a defect on its first live use.** The first run reported
`Engine.Api.Http/Properties/` as a change with no declaration. The file is `launchSettings.json`. The
SDK generates it when a person runs the web host, and it carries random port numbers. It was untracked
and absent from `.gitignore`, therefore it would appear in `git status` after each run and it would
give a false difference on each computer. The stash from TASK-0018 holds an older copy with different
ports, which confirms the behaviour. `.gitignore` now names it.

## v0.23 — The repository is clean and the gate needs no person (P0.9, TASK-0023)

The owner gave three instructions: direct the work and do not do it by hand, clean the branches, and
close each entry that a decision can close.

**The gate no longer needs a person.** `.github/workflows/ci.yml` ran on a push to the main branch
only. The gate therefore gave no report on a branch, and a person had to open a pull request by hand
before register entry R-0002 could close. The trigger is now `push` with no branch filter, plus
`pull_request` for the two jobs that need a base reference.

**Twelve local branches became two.** Ten were fully merged, and `git branch -d` deleted each one,
because that command refuses an unmerged branch and therefore gives the check. Two held unique commits
and each one received a tag first: `archive/happy-booth-1cef3f`, which TASK-0018 salvaged into v0.20,
and `archive/p7b-integrate-package`, whose one commit reached the main branch as `831ebbc`.

**Three worktrees became zero.** Each one lived under `.claude/worktrees/` and held a full copy of
every project file, pinned between 31 and 51 commits behind the main branch. A stale copy of this kind
made the dependency direction gate report a broken rule as satisfied in v0.19. The filter in that gate
stays as a second defence.

**Two stashes became zero.** Neither held a change to a tracked file. Each held untracked files only,
and each of those files is accounted for: the renderer proposal entered a commit in v0.20,
`launchSettings.json` entered `.gitignore` in v0.22, and `.claude/settings.local.json` enters
`.gitignore` here. The tags `archive/stash-0` and `archive/stash-1` preserve both.

**The cleanup found an uncommitted draft ADR.** The worktree `happy-booth-1cef3f` refused removal,
because it held `docs/adr/0015-handler-owned-command-construction.md` with the status `Proposed`. Its
decision is the decision that ADR-0016 carries, which shipped in v0.18 and which a gate enforces, so
no new record is needed. A commit in that worktree captured the draft and
`archive/happy-booth-1cef3f` points at it.

The draft gave one thing that the repository did not hold. It names
`Engine.Core/Persistence/CommandCodec.cs` as a third position that would dispatch on a command name,
and it states that the persistence work must use handler-owned construction from the first day.
TASK-0019 now carries that constraint.

**R-0014 is decided.** `.gitignore` contains `.claude/`. The directory holds settings, a transcript
and a worktree; each one is host-specific and regenerable, and nothing under it was ever tracked.

**R-0009 was closed in fact and open in the register.** TASK-0017 corrected the false `DRAFT` header
and its Outcome says that the entry is resolved. Nobody moved the entry. The register and the
repository now agree. This was a bookkeeping defect of the agent, and the register is the instrument
that must not carry one.

Tests: 187, unchanged. No code changed. `dotnet build` gives zero errors.

Closed: R-0009, R-0014. Open entries: 7 of 15.

New diagnostic codes: none.

## v0.24 — The gate passes on three operating systems (P0.6, TASK-0020). Track 0 is complete

Run `35786216373` reports a pass on `ubuntu-latest`, on `windows-latest` and on `macos-latest`. Each
job runs `dotnet build`, `dotnet test`, the smoke test for `NoOp` and the smoke test for `CreateBox`.
Register entry R-0002 recorded this gap since 2026-08-25 and it is closed.

The report is automatic. The trigger change of v0.23 means that a push gives it, and a person opens
nothing. This milestone waited four days for one action by a person, which was the condition that
v0.23 removed.

**A correction.** TASK-0020 stated that the native Manifold payload carries the runtime identifier
`win-x64` only, and that the native tests therefore run on the Windows runner and skip on the other
two. The package holds three sets of binaries: `linux-x64`, `osx-arm64` and `win-x64`. Each runner has
a payload, therefore the native backend is expected to run on each runner. The comment in
`.github/workflows/ci.yml` is corrected and the task carries the correction.

The result does not change, because each job passed. The split between a test that ran and a test that
skipped is not observable from outside, because the log endpoint needs a token.

This is the third statement in three days that described the repository without reading it. The other
two were the dependency gate that trusted a project name, and a note that cited a scope clamp which a
previous milestone had removed. No gate catches this class, because each wrong statement sat in prose.

Tests: 187 on this computer. No code changed. Open entries: 6 of 15.

Track 0 is complete: P0.1 to P0.9 are shipped.

New diagnostic codes: none.

## v0.25 — A licence notice and a verified checksum for the native payload (P0.10, TASK-0024)

Register entry R-0005 recorded that the repository holds a binary of 4.4 MB with no licence element
and no checksum, against ADR-0014 section 5. Its limit was 2026-09-24.

**Two components, not one.** `THIRD-PARTY-NOTICES.md` names Manifold under the Apache License 2.0,
and Clipper2 under the Boost Software License 1.0. The first reading found Manifold only. The package
ships no separate Clipper2 binary, therefore the code is compiled into the Manifold library. The
evidence is a search of the raw bytes: the name Clipper appears in `libmanifold.so.3.5.2`, in
`manifold.dll` and in `libmanifold.3.5.2.dylib`. The build sets `MANIFOLD_CROSS_SECTION=ON` and
`cmake/manifoldDeps.cmake` fetches Clipper2 at commit `46f6391`, which agrees.

**The checksums are a rule and not a decoration.**
`Engine.Tests/Governance/NativePackageGateTests.cs` reads each SHA-256 value from the notices and
compares it against the file, in both directions. A change to a binary that does not change the
notices fails the build, and a binary inside the package that no value records fails too. Both
directions were verified by injection.

**The package names the wrong repository.** Its nuspec holds
`<repository type="git" commit="718eab0684178e4fdf7ef419cc4ff26484008705" />`. That commit is not a
Manifold commit. It is a commit in this repository, dated 2026-07-04, with the subject "Merge pull
request #8 from Hperruchon/p7b-finish", while the description in the same nuspec says "Version tracks
the pinned Manifold commit". The package therefore gives incorrect information about the origin of its
own binary. Register entry R-0018 holds the defect, because a correction needs a new build. The
notices give the commit that the tag `v3.5.2` names and state that a reader must not use the nuspec.

**A note on method.** A first attempt to look inside the binaries used `strings`, which this computer
does not have. The command returned nothing, and the absence read as "no Clipper2 inside". A check of
the total count caught the error, and `grep` on the raw bytes gave the true answer. A tool that is
absent returns an empty result, and an empty result is not evidence.

New tests: 4. `dotnet test` gives 191 passed, up from 187. `dotnet build` gives zero errors.

Closed: R-0005. Opened: R-0018. Open entries: 6 of 15.

New diagnostic codes: none.

## v0.26 — Each open question is decided (P0.11, TASK-0025)

Four entries held a question and not a piece of work. The register now holds two open entries, from
sixteen at the start of this branch.

**R-0008 — the command-line parser is accepted permanently.** The convention `--param k=v` is normal.
ADR-0016 gave type coercion and validation to `ParameterBinder`, therefore `ArgParser` splits text and
makes no decision about a value. JSON input on the command line would copy the HTTP surface. The
comment that promised a replacement is replaced by the decision. The entry is the first in the section
"Accepted compromises".

**R-0010 — the boundary document is archived.** It is at
`docs/archive/engine-runtime-boundaries.md` with a header that forbids its use, and `docs/INDEX.md`
points at the section "Authority diagram" in `CLAUDE.md`. A rewrite was refused: each part that is
still correct lives in that section and in the ADRs, therefore a rewrite would give a second place for
one decision, which is the condition that made the document wrong.

**R-0011 — a limit, and not only an answer.** The entry names quantity as the risk, so an answer about
two codes would return as the same question at the third. `docs/diagnostics.md` gains a section that
names each reserved code with its reason, as an addition, because `CLAUDE.md` permits an addition to
that file and nothing else. `Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` bounds the
quantity at two and was verified by injection.

**R-0015 — the command line uses the relaxed encoder.** `Engine.Cli/JsonRenderer.cs` uses
`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, therefore the output shows `it's here <&>` and not a
line of Unicode escapes. `Engine.Api.Http` keeps the default encoder, because a browser client can put
a response into a page. No test asserted the escaped form.

New tests: 3. `dotnet test` gives 194 passed, up from 191. `dotnet build` gives zero errors.

Closed: R-0010, R-0011, R-0015. Accepted: R-0008. Open entries: 2 of 15, which are R-0007 and R-0018.

New diagnostic codes: none. The two reserved codes are unchanged and now carry a permanent reason.

## v0.27 — The write-set gate runs in the pipeline and its rule is corrected (P0.12, TASK-0026)

Two jobs reported `skipped` on each run, because each one had the condition
`github.event_name == 'pull_request'` and v0.23 made a push the normal trigger. The write-set gate of
v0.22 therefore never ran in the pipeline. It ran on a local computer only, by hand, one change at a
time.

A measurement of what a pull request would report found a defect in the gate itself.

**The forbid rule was wrong.** TASK-0022 wrote that a forbid beats a permit, and beats a permit from
another task, and it called the strict reading the safe one. A forbid binds the task that declares it.
`Engine.Cli/**` in the forbid list of the persistence task says that the persistence work must stay
out of the command line. It does not say that nobody may touch the command line. The rule only looked
correct because each change that tested it carried one task.

The measurement: the whole-branch difference reported 19 files and each report was false, because the
forbid list of TASK-0014, which pinned the SDK, blocked each file that TASK-0016 created. One commit
failed for the same reason: TASK-0018 modified `Engine.Cli/Cli.cs` and declared it, and the forbid
list of the deferred TASK-0019 blocked that declaration.

A permit now beats a forbid. A forbid blocks a file only when no task in the change permits it, and it
then gives the better message, because it names the boundary that an author wrote. On this branch 12
of 13 commits pass, from 11 of 13 before.

**The gate reads one commit at a time.** A task governs the commit that carries it. A difference
across many tasks cannot say which task made which change, therefore it can only compare against the
union of each write set, and the union is not the rule.

**The job runs on a push.** It needs no base reference now. The contract gate keeps its condition,
because it compares two trees and a pull request is the correct condition for that.

**History is grandfathered by one named commit.** `eng/write-set-cutoff.txt` holds the tip at the
moment the gate was turned on. The gate skips each commit behind it. One commit needs this:
`ecb1f9a`, "gitignore: ignore graphify-out", which opened the branch and touches no task file. The
exemption names one commit and covers nothing after it.

Each case was verified by injection: a file that one task forbids and another permits passes; a file
that a task forbids and no task permits fails and names the task; a file in no list fails; a change
with no task file fails.

Tests: 194, unchanged. `dotnet build` gives zero errors. Open entries: 2 of 15.

New diagnostic codes: none.

## v0.28 — The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3 (R1, TASK-0027, ADR-0017)

Roadmap phase R1 is the first of the six phases that end at the first objective of the owner: create
a box, subtract a second box, and observe the cut.

**Two facts came first.** Three project files pinned `Vortice.Vulkan` 1.9.8, and nuget.org lists
3.2.3 as the highest version. The desktop host built and ran on this computer before any change: a
green window, the layer `VK_LAYER_KHRONOS_validation` active, and no validation message.

**One first-party project.** `3DEngine.Vulkan` holds the device, the swapchain and the SDL window
that owns the surface. The host referenced the vendored sample framework before, which `CLAUDE.md`
does not permit, and the dependency direction gate did not read the host. ADR-0017 gives the rules.
`Vortice.Vulkan.Sample` and `Vortice.Vulkan.SampleFramework` are removed. Each type and each method
that moved has a caller in the host. The two build warnings of the sample code are gone with it.

**The binding is 3.2.3, with one pin.** `3DEngine.Vulkan/3DEngine.Vulkan.csproj` holds the only pin of
`Vortice.Vulkan` and of `Alimer.Bindings.SDL`. Version 3.x removes each global Vulkan function: an
instance function lives on `VkInstanceApi` and a device function on `VkDeviceApi`. Commit `885ba75`
holds the port and nothing else.

**The host releases each Vulkan object at exit.** It released nothing before. The window closes with
the exit code 0 and no validation message.

**The validation layer was made to speak.** Its silence is evidence only if it can report. A render
pass that does not end gave `VUID-vkEndCommandBuffer-commandBuffer-00060`. A device that is not
destroyed gave `VUID-vkDestroyInstance-instance-00629` at the close. Each defect was removed.

**The gates.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` reads the host now, and it
adds three checks: the Vulkan layer references at most the render kernel, only a host that draws
references it, and each binding has one pin. A headless client can no longer reference the render
kernel, which ADR-0009 section 2 already forbade. `MarkerGateTests.cs` reads the render side. Seven
violations were injected, and each one failed with a message that names the project and the rule.

**The licence.** Each sample file said "See LICENSE in the repository root", which is the licence of
this repository. `THIRD-PARTY-NOTICES.md`, section 4, now gives the MIT licence of the sample author.

**Three statements of this milestone were false when first written, and each one is corrected.**

- The notice said that the first commit of the repository added the sample code. The root commit is
  `f758d1e`. The commit that added the files is `939e23f`, and both carry the subject "Initial
  commit".
- ADR-0017 said that the project owns about 600 lines. The count is 1,080. The record was corrected
  before the first push, when no person had read it. The decision did not change.
- Commit `885ba75` ticked "zero warnings". A clean build of it gives CS9191. The build that compiled
  the port ran behind a filter that failed, and the next build compiled nothing. Commit `f602022`
  removes the warning. A clean build is now the method for a warning count.

**Found in passing.** Six statements in five documents describe a repository state that no longer
exists. Register entry R-0019 holds them, with a limit of 30 days.

Not done, and why: no runner has a GPU, so the runners build the Vulkan code and never run it. macOS
needs MoltenVK, and nothing tests it. The swapchain is not created again after
`ErrorOutOfDateKHR`; the window is not resizable, and a minimized window was not tested.

New tests: 4. The test list holds 198, up from 194. `dotnet build` gives zero errors and zero
warnings on a clean build.

Opened: R-0019. Open entries: 3 of 15.

New diagnostic codes: none.

## v0.29 — The twenty objectives are in the charter (governance, TASK-0029)

The owner asked for a check of each rule against the objectives. The repository held no list of
objectives. `CLAUDE.md` cited "Objective 11" and `docs/roadmap.md` cited "Objective 15", and no file
defined either number. The owner confirmed the twenty objectives on 2026-09-20, but the list lived
only in the transcript of that session.

**The list.** `docs/CHARTER.md` now holds the section "The twenty objectives", in five groups, with
the two refinements of the owner for objectives 18 and 19. Objective 1 says "separate from work for
an employer" and does not name a product of the employer, because the repository is public.

**The gate.** `Engine.Tests/Governance/ObjectiveReferenceGateTests.cs` fails when the list is not
numbered from 1 to 20, and when a document cites an objective or an anti-objective that does not
exist. Four violations were injected, and each one failed with the file and the line.

**Two terms.** The glossary defines objective and anti-objective.

New tests: 3. The test list holds 201, up from 198. `dotnet build` gives zero errors and zero
warnings on a clean build.

Open entries: 3 of 15. New diagnostic codes: none.

## v0.30 — The codebase review of 2026-09-23 is in the repository (governance, TASK-0030)

The owner asked for a review of the code and then asked where to store it.
`docs/reviews/2026-09-23-codebase-review.md` holds it: the architecture with four diagrams, the
patterns, the defects in order of importance, the next task, the research with its sources, and a
recommended approach. `docs/INDEX.md` names the folder and states the rule: a review is a dated
record, and `CURRENT-STATE.md` stays the authority for what exists.

The review found two defects that change design truth. A replay can rebuild a different Document
after one rejected command, because `Document.Version` counts rejected events and the log does not
hold them. And queries run with no lock in the HTTP host. The review records each finding. It does
not act on one.

No code changed. Tests: 201, unchanged. Open entries: 3 of 15. New diagnostic codes: none.

## v0.31 — The codebase review is periodic (governance, TASK-0031)

The owner asked for a dated review that repeats, so that the evolution of the code stays visible.

Each review in `docs/reviews/` now has four fields: `date`, `commit`, `ledger` and `previous`. It
also has twelve measures with a method for each, and findings with stable identifiers. Each later
review must give the state of each earlier finding. `docs/templates.md`, section 7, gives the form.

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` fails the build when a field or a measure is
absent, when a finding of the previous review has no state, or when more than six milestones follow
the last review. The period counts milestones and not days, because objective 12 says that time is
irregular. The next review must come before ledger entry v0.35. Four violations were injected, and
each one failed. A complete second review passed as the positive control.

New tests: 5. The test list holds 206, up from 201. Open entries: 3 of 15. New diagnostic codes: none.

## v0.32 — The decisions of 2026-09-25 (governance, TASK-0032)

The owner answered the nine questions of the architecture challenge. This milestone records each
answer, and it changes no code.

- **Three new ADRs, accepted on the decision of the owner.** ADR-0019: the desktop host owns the
  session and serves the HTTP and WebSocket surface as a library, and `engine-api-http` becomes a
  small program around the same library. ADR-0020: the version counts applied commands, and the reset
  snapshot gains `seq` for the cursor. ADR-0021: an operation consumes its operands, and the Document
  holds the live bodies only.
- **ADR-0018 is accepted** after two corrections: both hosts call `Query<object>`, and
  `FieldSchema.Items` waits. It amends ADR-0012 only.
- **Five ADRs are now `Amended`:** ADR-0006, ADR-0008 and ADR-0010 by ADR-0020, ADR-0011 by ADR-0019,
  and ADR-0012 by ADR-0018 and ADR-0021. ADR-0005 does not change, because it does not define the
  version.
- **Anti-objective 2** keeps the drag rule of ADR-0007: the preview stays in the client, and one
  command goes to the log on release.
- **Six ready tasks.** TASK-0034 (one serial boundary), TASK-0035 (the version), TASK-0036 (a host
  refuses to start without the native backend), TASK-0028 (R2, now `Ready`), TASK-0037 (operand
  consumption) and TASK-0038 (the hybrid topology). The roadmap gives their order.
- **A gap in the write-set gate.** A commit that touches a task file gets all its permits. Register
  entry R-0026 records the risk, which an injection found.
- **The architecture challenge** is in `docs/reviews/2026-09-23-architecture-challenge.md`, with the
  answers of the owner in its last section.

Nothing new exists in the code. `CLAUDE.md` gives the hybrid topology as the target, not as the
state.

New tests: 0. The test list holds 206. Open entries: 10 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.35.

## v0.33 — The web shell is removed (governance, TASK-0033)

The owner said on 2026-09-25 that a browser client is not the main idea. `BlazorApp` and
`BlazorApp.Client` were a template that made no call to the engine, so this milestone deletes them.

- The solution holds no Blazor project. The two entries in `3DEngine.sln` are gone.
- ADR-0003 has the status `Withdrawn`, with the reason in its field `notes`. The legend and
  `docs/templates.md` now permit the owner to withdraw an accepted record that no later record
  replaces.
- The budget of unenforced ADRs falls from 2 to 1, because ADR-0003 was one of the two.
- `CLAUDE.md`, `docs/INDEX.md`, the pull request template and the notes of `3DEngine.Core` name no
  Blazor project.

Register entry R-0021 holds the open question of how to show the application.

New tests: 0. The test list holds 206. Open entries: 10 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.35.

## v0.34 — The rules are fewer, each one has one home, and ten more have a gate (governance, TASK-0039)

The rules review of 2026-09-25, `docs/reviews/2026-09-25-rules-review.md`, examined each rule and
the organization of the repository. It found 124 rules in thirteen files, seventy of them in text
only, fourteen statements about a repository that no longer existed, five rules that the practice
contradicted, and eighteen files that a session read to find its position. The owner agreed with
the proposal on 2026-09-26 and gave four directions: show the organization, change the rules that
contradict the practice, clean each statement that does not need to exist, and keep each ADR in
agreement with the code. This milestone does the first three. TASK-0040 holds the fourth.

**Three files are gone.** `docs/working-agreement.md`, `docs/conventions.md` and
`docs/open-questions.md`. Each live rule moved to `CLAUDE.md` or to `docs/templates.md`, and each
rule that repeated a gate or another file went. The rationale of the working agreement, with its
reasons from the Blender, FreeCAD and Ondsel projects, is in `docs/archive/`. `CLAUDE.md` is a
position file of 150 lines. The six determinism rules are unchanged. Section 4.0 of the review
gives one home for each topic that lived in two or more places.

**Five rules changed, and each one keeps its purpose.** A session runs the tests before each commit,
and the gate on three operating systems is the proof. A false statement about the repository is an
error that the task corrects; a decision that the code contradicts is a question for the owner. A
diagnostic code is never removed and never changes meaning, and a row may move. An accepted ADR
agrees with the code, with a section "History" and the acceptance of the owner for each change to a
Decision (`docs/templates.md`, section 1). Each push is green, and the owner merges when convenient.

**Four new gates.** `AdrEnforcementExistsGateTests` fails when an ADR in force names an `enforced-by`
file that does not exist, unless a Ready task creates it. `TaskGovernanceGateTests` fails when
`governed-by` of a Ready task differs from the ADRs whose `affects` intersect its write set, or when
`depends-on` names no task. `DeterminismCallGateTests` fails when a source in the log path calls a
transcendental function, `FusedMultiplyAdd` or a function whose guarantee covers one process, or when
a project file names a 32-bit runtime identifier. `DocumentPathGateTests` fails when a path in
`docs/INDEX.md` does not exist or a gate class has no row in the gate table.

**Two gates changed.** The write-set gate reads the task that the commit trailer names, a line of the
form `TASK-nnnn`, and only that task governs the commit; a commit with no trailer that touches several
task files fails. This closes register entry R-0026. The contract gate runs on each push, against the
previous tip, and not on a pull request only; it had never checked a merge into `main`.

**Five gates read a scan instead of a fixed list.** The dispatch gate reads each host source. The
diagnostics scanner reads each `Engine.*` project, and `Engine.Api.Http` raises three codes that it
did not read before. The schema gate reads `HandlerCatalog`. The dependency gate fails on a project
that is in no class. The marker gate reads each project that has a project file.

**Sixteen injections, and each one failed.** An ADR with a missing file and an ADR with a closed task;
a Ready task with an extra ADR, a missing ADR and an absent dependency; `Math.Pow` in `Engine.Core`
and `win-x86` in a project file; a host file with a quoted command name, a code in `Engine.Api.Http`,
a project in no class, a `TODO` in that project, and a quoted query name in the schema endpoint; the
change list of v0.32 plus `Engine.Core/CommandBus.cs`, with TASK-0032 named and with no task named;
a path that does not exist in the map, and the gate table without one class. The old fixed lists
passed four of the five scan injections. Each commit after the cut-off passes the trailer rule; a
prose line of the v0.27 message that starts with an identifier made the first form of the rule fail,
and the trailer form corrected it.

**Fourteen stale statements are corrected or removed.** Register entry R-0019 is closed. Two pointer
lines of `docs/CHARTER.md` named the deleted working agreement and now point at `CLAUDE.md`; no
objective, anti-objective or non-goal changed. The task moved the charter from `forbid` to `modify`
for those two lines after the write-set gate refused the commit.

**One status vocabulary.** A task is `Ready`, `Active`, `Done` or `Deferred`. The roadmap loses its
Status column and keeps the Shipped list. The glossary defines `V1` and `V1.x`, which fifteen ADRs
and the charter use.

**The measures**, with the method of the review, section 1, so that the next review can repeat them:

| Measure | Before | After | Method |
|---|---|---|---|
| Rule files | 13 | 10 | The files with rules, without the ledger and the three CI files. |
| Lines in the rule files and the CI files | 2,971 | 2,688 | `wc -l` on the same sixteen files, before this entry. |
| Sentences with "must" | 52 | 40 | `grep -o -w -i must` on the rule files. |
| Sentences with "do not" | 80 | 46 | `grep -o -i -E '\bdo not\b'` on the rule files. |
| Rules in the inventory | 124 | 93 | Section 2 of the review, less the rows marked deleted. |
| Rules that a gate enforces in full | 30 | 30 | Ten deleted rows had a gate; ten rows gained one. |
| Rules in text only | 70 | 48 | The rows with `none` that stay. Twelve are the charter's. |
| Files that a session reads to find its position | 18 | 17 | The method of the review, section 1, for TASK-0034. |
| Gate classes | 13 | 17 | Test classes whose name ends in `GateTests`. |
| Open register entries | 10 of 15 | 8 of 15 | Section "Open" of `docs/register.md`. |

**Not done, and why.** The folds of the seven amended ADRs are TASK-0040, because each one changes a
Decision that the owner accepts. `CLAUDE.md` has 150 lines and not the 120 of the estimate, because
the diagram and the six determinism rules take forty lines that stay. A gate for a contraction in a
document was offered and not asked for.

New tests: 8. The test list holds 214, up from 206. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Closed: R-0019, R-0026. Open entries: 8 of 15. New diagnostic
codes: none.

**The next ledger entry, v0.35, must be a codebase review.** Six milestones follow v0.28, which the
last review examined, and the limit of `CodebaseReviewGateTests` is six.

**Added on 2026-09-26, after the push and before the merge, in the same milestone (TASK-0041).** The
rules that earlier sessions broke, and that the owner then put in the prompt of each session, were
not in `CLAUDE.md`: read a file before you cite it, prove a gate by injection, `set -o pipefail`, an
absent tool is not evidence, Node.js and not Python, a clean build for a warning count, and the
commit trailer. One section "Method" now holds them, with the write-set command, and it absorbs the
sections "Tests" and "Rules with no other home". `CLAUDE.md` has 165 lines. This paragraph is in the
entry v0.34 and not in a new entry, because the branch is unmerged and the next entry must be the
codebase review.

## v0.35 — The codebase review of 2026-09-30 (governance, TASK-0042)

`docs/reviews/2026-09-30-codebase-review.md` is the second codebase review. It examines `main` at
`14e2c73`, the merge of v0.32 to v0.34. The review changes no code.

**No production source changed after the first review.** The difference between the two reviewed
commits in the eight production projects is one documentation file. Each engine finding and each
Vulkan finding of the first review is open. One earlier finding is fixed: the contract gate ran on a
push to `main`, in run 36443245386, where each of the five jobs gave a success.

**Thirty-three findings are open: two critical, twelve high, fourteen medium and five low.** Nineteen
are new. Four review agents read the code in parallel, and a run or a reading on disk checked each
finding that the review gives as observed.

- The query defect is a measured failure. With commands on one task and reads on four tasks, about
  4,500 queries and about 4,000 snapshot copies threw an exception in each of three runs.
- An idle subscriber receives 1,025 heartbeat frames at second 30, and not one frame.
- A replay that rejects a command returns an empty Document and reports nothing.
- A subscriber that connects during a commit can lose an event or receive it two times.
- The host accepts a WebSocket upgrade with a foreign Origin, and no source checks the bind address.
- Eight injected violations pass their gate. One of them is a reference from `Engine.Core` to
  `3DEngine.Core`. TASK-0039 wrote or changed nine of the gates two days before.

**Five register entries are new.** R-0027 holds six findings of the first review that had no owner.
R-0028 holds the new engine and host findings. R-0029 holds the heartbeat loop, with a limit of 30
days. R-0030 holds the Vulkan findings. R-0031 holds the holes in the gates.

**The roadmap is corrected.** It said that the path to the first objective takes 25 to 35 evenings.
The sum of its column "Evenings" for the ten phases that remain is 32 to 48. The sentence predates
the five phases that v0.32 added.

**The next task is TASK-0034.** The review gives the research and an approach in seven steps. It
recommends one change beyond the task text: the commit changes the Document first and publishes the
events second, so that a sink that fails cannot leave a partial commit. The owner decides that in
question Q1 of the review.

Not done, and why: no finding is corrected, because a review records and a task corrects. Two host
findings and six small findings are given as reported by a review agent and not checked. The
heartbeat and the handshake findings were not run on a second computer.

New tests: 0. The test list holds 214. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Opened: R-0027, R-0028, R-0029, R-0030, R-0031. Open entries: 13 of 15. New
diagnostic codes: none. The next codebase review must come before ledger entry v0.41.

## v0.36 — Each injected violation of the second review fails its gate (governance, TASK-0043)

The codebase review of 2026-09-30 injected eight violations that passed their gate, findings T3 to
T6. The owner answered yes to its five questions. This milestone closes the gate holes and records
the three decisions that need a file. No engine code changed.

**The gates.** The dependency gate reads each project file as XML and takes each path of each
`Include`; a reference from `Engine.Core` to `3DEngine.Core` inside an `Include` with two paths now
fails. The write-set gate checks a merge commit on the changes that the merge made itself, requires
that a commit changes the file of the task that it names, requires that a gate file is named exactly
and not by a pattern, and holds the cut-off commit in a test. The determinism gate reads each source
with its comments removed, so a call with the parenthesis on the next line is seen and a comment
that names a function is not; it also refuses the same functions on `double`, `float` and `Half`,
and a static import of `System.Math`. The marker gate refuses an identifier that is not in the
register. The task gate refuses a status outside the set and an identifier that differs from the
file name. The register gate compares the entries that it parsed with the headings of the section,
so an open code fence cannot hide an entry. The two small jobs of the workflow use `bash` with
`pipefail`, and each git command is tested.

**Each injection was repeated.** Two paths in one `Include`; `double.Pow`, a static import and a
call with the parenthesis on the next line; a marker with an identifier that no register holds; a
status `ready`; a register with an open fence; a gate file changed under a task that permits
`Engine.Tests/**`; a commit that names a task and does not change it; a moved cut-off. Each one
passed before the change and fails after it. A comment that names a function passes, as the
control. A merge with a change of its own, made in a throwaway clone, lists that change under
`git diff-tree --cc` and nothing under the plain form. Each commit after the cut-off still passes
the write-set rule.

**The decisions.** TASK-0034 gains the commit order, change the Document first and publish second,
and three tests for the findings E9 and E10 (question Q1). `docs/diagnostics.md` gives the close
status 1007 for an invalid subscribe frame; the code sent it from the start, and TASK-0010 said 1003
(question Q3). TASK-0044 opens for the heartbeat loop and the three host checks, before TASK-0038
mounts the surface in the desktop host (question Q5). Register entry R-0027 stays as it is (question
Q2), and the gate work comes before TASK-0034 (question Q4).

Not done, and why: the parts of finding T5 that need a new parser, an `affects` value with quotes,
the reserve table heading and the codebase review gate, and the low findings of T7. Register entry
R-0031 stays open for them, with its limit of 2026-12-24.

New tests: 3. The test list holds 217, up from 214. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.41.

## v0.37 — The host binds to a loopback address only, checks the Origin and the Host, and sends one heartbeat in each interval (TASK-0044)

Findings E8 and E15 of the codebase review of 2026-09-30, and question Q5, which the owner answered
yes. This is the first change to host code since v0.28.

**One heartbeat in each interval.** The loop wrote a frame and started again with no wait, so one
idle subscriber received 1,025 frames at second 30. The loop now moves the time of the last send
when it writes a frame (`Engine.Api.Http/WebSockets/Subscriber.cs`). The run of the review on the
real host, one idle subscriber for 36 seconds, gives 1 heartbeat frame at second 30.
Register entry R-0029 is closed.

**A loopback address only.** `engine-api-http` refuses to start on an address whose host is not a
loopback address, with exit code 1 and a message, before it builds the application. The framework
default, with no address given, is localhost and passes. `docs/CHARTER.md` said this for months,
and no source checked it.

**A foreign Origin gets 403.** A WebSocket upgrade with an Origin header that names a host outside
the list in `SubscriberOptions` is refused before the upgrade. The list holds the three loopback
names. A request with no Origin header passes, because a script and an agent send none. The value
"null" is refused.

**A foreign Host gets 400.** The host filtering middleware of the framework accepts the same three
names. A page that resolves its own name to the loopback address cannot post a command.

**Each test failed first.** `Engine.Tests/Http/HeartbeatTests.cs` counted a burst.
`Engine.Tests/Http/HostGuardTests.cs` saw 101 for the foreign Origin, 200 for the foreign Host, and a
process that ran on 0.0.0.0 until the test stopped it. After the change each one passes, and a run
on the real host gives 403, 400, and a refusal with exit code 1.

Not done, and why: the close frame after a cancelled token (finding E17) stays in R-0028, and the
Host filter is not tested with the IPv6 literal.

New tests: 16. The test list holds 233, up from 217. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Closed: R-0029. Open entries: 12 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.41.

## v0.38 — The heartbeat test measures the gap between frames (TASK-0044, correction)

The merge of v0.37 into `main` gave a red run: the Ubuntu job failed in its test step, while the
same content passed on the three runners of the branch minutes before, and the suite passed six
times in a row on this computer. The log of the job needs a sign-in, so the failed test has no name
here. The test step ran its normal eight seconds, so one test failed and the suite did not stop.

The first form of the heartbeat test counted the frames of one second and expected three to eleven.
A loaded runner can make that count low. The test now takes the moment of three consecutive
heartbeat frames and requires that each gap is at least the interval: a slow runner makes each gap
longer and never shorter, and the defect of finding E8 makes each gap close to zero. With the wait
line removed for one run, the test fails with the gaps that it saw. The two tests that spawn the real
host share one collection with the heartbeat test, so that a process start does not run beside a
measurement of time.

Not done, and why: the name of the failed test on Ubuntu stays unknown, because the log needs a
sign-in that this computer does not have. If the next run on Ubuntu fails again, the log names it.

New tests: 0. The test list holds 233. `dotnet build 3DEngine.sln --no-incremental` gives zero
errors and zero warnings. Open entries: 12 of 15. New diagnostic codes: none. The next codebase
review must come before ledger entry v0.41.
## v0.39 — Commands, queries and reads of the Document pass through one serial boundary (TASK-0034)

Finding E2 of the codebase review of 2026-09-23, findings E4, E9 and E10 of the review of 2026-09-30,
and question Q1, which the owner answered yes. This is the first code task since 2026-09-23.

**One document session.** `Engine.Core/DocumentSession.cs` owns the Document, its backend, its
command bus, its query bus and its event sink. A command, a query and a read of the Document enter one
serial section. The probe of the review is the first test, with 5,000 commands and four readers. On
the code before the session, three runs gave 1,021, 1,152 and 950 queries that threw
`InvalidOperationException`, and 839, 920 and 849 snapshot copies that threw `ArgumentException`.
With the session, the test passes 100 runs of 100.

**Both hosts use it.** The command line and the HTTP host send each command, each query and each
read of the Document through the session. The registries do not change after start, and a host reads
them with no lock.
The WebSocket handshake enters the session first and the broadcaster second, the same order as a
commit. On the code before the change, three runs gave 190, 201 and 209 answers with HTTP 500, and
in one run of three the first live `seq` after a reset was the snapshot version plus two. With the
opposite lock order injected into the handshake, the deadlock test stops at its limit of 11 s.

**Change, then publish.** The commit computes each sequence number, appends the command, adds each
body, advances the version and stores the result in the cache. Then it appends the events with
`CancellationToken.None`. A rejection and a cancellation use the same order. A cancellation after
the log append and a sink that throws now leave the log, the bodies and the version in agreement. The
sink loses an event, which a subscriber recovers with a reset, and a retry gets the cached result.

**One bus for each Document.** The bus refuses a second bus on the same Document, and it refuses to
move the version back.

Not done, and why: scope item 8 asks that `Document.AdvanceVersion` refuses a lower value. That
method is in `Engine.Contracts/Document.cs`, which the task forbids, and TASK-0035 modifies that file.
The bus, the one caller, refuses in its place. `docs/INDEX.md` lists the parts of `Engine.Core` and
does not name the session yet; the file was outside the write set. `docs/register.md` joined the
write set of the task, because the owner asked for the progress line in R-0028.

The first run of the pipeline on the code commits, 4545f75, failed in the test step on Windows, and
passed on Ubuntu and macOS. The log needs a sign-in, so the failed test has no name here. Here the
suite passed 30 runs, 10 runs at full load on each core, 12 runs with a smaller thread pool and 10
runs on two cores by affinity, with no failure. Register entry R-0032 asks for the name of a failed
test in a public annotation.

New tests: 9. The test list holds 242, up from 233. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 13 of 15, with the new entry R-0032. New diagnostic
codes: none. The next codebase review must come before ledger entry v0.41.
## v0.40 — A failed test in the pipeline has a public name, and two tests of v0.39 tolerate a slow runner (TASK-0045, TASK-0034 correction)

Register entry R-0032. The log of a job needs admin rights, and the annotations of a check run are
public. Two red runs, on Ubuntu for v0.38 and on Windows for v0.39, had no test name that a reader
without a sign-in could see.

**The name of each failed test.** The step "Test" of the pipeline writes a `.trx` file. When
`dotnet test` fails, `eng/report-failed-tests.js` writes the name, the message and six lines of the
stack of each failed test as an annotation, with a maximum of ten. On the branch
`r0032-injection-2026-10-03`, which is never merged, an injected failure gave its name on each of
the three runners through the check-run API with no sign-in. The first form of the script found no
failed test, because its pattern read past the empty element of a passed test; the injection on this
computer found that.

**The failure of v0.39 has a name.** The same run on Windows named a real failure:
`HttpConcurrencyTests.Queries_And_Commands_In_Parallel_Get_No_Http_500`, with
`TaskCanceledException`. It did not repeat here. Its four readers turned in a loop with no await
until the first body existed, which can starve the server on a runner with few cores. The readers now
start after the first command; this cause is inferred and not observed. On one core here, the E9 test
of v0.39 failed 3 times: the client stopped reading after its measurement, its queue filled, and the
server disconnected it as a slow subscriber, as ADR-0005 §6 requires. Its close then found a closed
socket. The close after a measurement now accepts that. The full suite on one core failed 1 time in
25 runs before the change and 0 times in 40 runs after it. The HTTP tests alone on one core
passed 80 runs of 80 after it.

Not done, and why: the cause of the Ubuntu failure of v0.38 stays unknown, because its log needs a
sign-in. The branch `r0032-injection-2026-10-03` stays on the server until the owner deletes it.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Closed: R-0032. Open entries: 12 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.41.
## v0.41 — The codebase review of 2026-10-04 (governance, TASK-0046)

The third codebase review, `docs/reviews/2026-10-04-codebase-review.md`. It examines `main` at
`ae5c521`, the merge of v0.39 and v0.40. The gate permitted six milestones after v0.34, and v0.40
was the sixth, so this entry had to be the review. On 2026-10-04 the owner chose the review and not a
change of the rule.

**What it found.** Forty findings are open: one critical, eleven high, twenty-one medium and seven
low. Six earlier findings are fixed: E2, E4, E8, E9, E15 and T6. Thirteen findings are new. The most
important new ones:

- E23: the bind guard reads the key `urls` only. An address given through `Kestrel:Endpoints`
  passes with no refusal. A run on this computer shows it.
- T8: the pipeline has no time limit, so a test that hangs holds a runner for six hours and gives no
  name.
- T9: the project file of the tests is not a gate file, so a task that permits `Engine.Tests/**` can
  remove each gate, and a `Directory.Build.props` file gets past the dependency gate and the x86
  gate. Runs on this computer show both.
- E18 to E21: the session of TASK-0034 has three latent defects, a reference that can leave it, a
  call back that blocks it, and a sink that leaves a gap, and a handler that fails after it changed
  the backend leaves an orphan solid.

**Method.** No file changed in `3DEngine/`, `3DEngine.Vulkan/`, `3DEngine.Core/` or
`Engine.Contracts/` after the last review, so three review agents read the code, and not four. I read
each cited line on disk, and I repeated the runs of E23 and T9. The first draft cited three lines
wrongly and said that two red runs had the shape of T12; the review corrects both before this entry.

**Owners.** R-0033 holds E18 to E24, and R-0034 holds T8, T12 and two parts of T13. R-0031 now also
holds T9, T10, T11 and the other parts of T13. R-0027 and R-0028 have a progress line. The roadmap
lists P0.13 as shipped, and the estimate is 30 to 45 evenings for the nine phases that remain.

**Questions for the owner.** Four, each with a recommendation: add the replay findings E7 and E17 to
TASK-0035; correct T8 and T9 before TASK-0035; refuse a commit that names a task that is `Done`; and
the rule for a sink that throws.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. New register entries: R-0033, R-0034. Open entries: 14 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.47.
## v0.42 — A hang stops with a name, no file outside the exact write set can turn a gate off, and a closed task governs no commit (governance, TASK-0047)

The owner answered "yes to all" to the four questions of the codebase review of 2026-10-04. This
task does the governance part: findings T8 and T9 and question Q3. TASK-0035 records Q1, register
entry R-0033 records Q4, and TASK-0035 now depends on this task, as Q2 asked.

**A hang stops with a name (T8).** Each job has `timeout-minutes`, and the step "Test" stops a test
after two minutes with `--blame-hang-timeout`. A test that hangs gives no result in the `.trx`
file, so `eng/report-failed-tests.js` now reads the sequence file of the blame collector, where the
test has `Completed="False"`. On a branch that is never merged, an injected hang stopped after about
130 seconds on each runner, and a public annotation named it.

**No file outside the exact write set can turn a gate off (T9).** The project file of the tests, each
`Directory.Build` and `Directory.Packages` file, the cut-off and each workflow are gate files. The
dependency gate reads each `Directory.Build` file above a project and each file that a project
imports, and the x86 gate reads each `.csproj`, `.props` and `.targets` file. Three injections
passed before the change and fail after it.

**A closed task governs no commit (Q3).** The gate reads the governing task at the commit, which the
workflow now gives in `WRITE_SET_COMMIT`, and refuses a task that is `Done` before the commit and
after it. A correction reopens the task with the status `Active`. Each commit up to `67c564f` keeps
the earlier rule, because three earlier commits changed a task that was `Done`; a replay of the 51
commits after the cut-off passes. `docs/templates.md`, section 3, gives the rule.

Not done, and why: the other gate findings of the review stay in R-0031 and R-0034. Two injection
scripts of this task were wrong first: one wrote a control character into a props file, and one lost
a backslash in a heredoc. Each was found before its result entered a record.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.47.
## v0.43 — The document version counts applied commands, and a replay rebuilds it (TASK-0035)

ADR-0020, finding E1 of the codebase review of 2026-09-23, and findings E7, E10 and the replay part of
E17 of the review of 2026-10-04, which the owner added to the task (question Q1).

**The version is the count of the log.** `Document.Version` equals the count of entries in
`Document.Log`, by construction. `Document.AdvanceVersion` is gone, so no code can set the version,
higher or lower, which closes finding E10. A rejection and a cancellation do not change it, and their
event still takes the next `Seq`. The bus keeps `Seq` alone, and no code computes one counter from
the other.

**The reset snapshot gives the cursor.** The snapshot of a `subscription.reset` gains the field
`seq`, the highest `Seq` at the reset, and a client continues from `seq + 1`. The E9 test of v0.39
reads it, and the reset test holds a rejection, so its snapshot gives version 5 and seq 6.

**A replay reports a divergence.** `Replay.ReplayLog` stops at the first result that is not
`Applied` with a `ReplayDivergenceException`, which names the entry, the command and the error. It
applies each entry with no idempotency cache.

**Each test failed first.** On the code before the change: the replay fixture, where `Beta` now
states the version 2, gave version 5 where 4 is expected; a live session with a rejection gave
version 4, and its replay gave 2 with the box lost; a rejected command reported version 2 where 1 is
expected; a replay of a rejection threw nothing; and a log with one `CommandId` two times replayed to
one entry.

Not done, and why: the backend argument of `ReplayLog` stays optional; finding E7 recommended to make
it necessary, and question Q1 did not include that part. The other findings of R-0028 stay.

New tests: 4. The test list holds 246, up from 242. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.47.
## v0.44 — A host refuses to start without the native backend, and a native test cannot skip in the pipeline (TASK-0036)

Findings E3 and T1 of the codebase review of 2026-09-23, and anti-objective 9, which refuses a silent
fallback when a capability is absent.

**Each host requires the native backend.** When the library `manifoldc` does not load, `Engine.Cli`
writes `E-GEOM-BACKEND-INIT` with a sentence that names the library, Manifold 3.5.2 and the platform,
and stops with exit code 3 before the command. `engine-api-http` builds its engine at the start, writes
the same, and stops with exit code 3 before it listens. Until now each host took the managed backend
with no message, and that backend holds boxes only. A test selects the managed backend with an option
record, `BackendOptions` for the command line and `HostBackendOptions` in the service container of
the HTTP host. No argument and no setting does.

**The active backend in /schema.** `GET /schema/backend` gives the name and the version of the
backend that the host composed, `manifold` and `3.5.2`. A test holds the version equal to the
package that the project file pins.

**A native test cannot skip in the pipeline.** GitHub Actions sets `CI` to `true` on each runner,
and with that value the attribute of the native tests no longer skips. A smoke test applies two
boxes, `Translate` and `Subtract` on the native backend through a session.

**Each result was seen before and after.** With the native library hidden from the output folders,
before the change: the command line applied a box with exit code 0, the HTTP host listened, and with
`CI=true` the 13 native tests skipped and the run passed. After it: exit code 3 with the message for
each host, and with `CI=true` 7 native tests failed and none skipped.

**The native backend had never loaded on Linux and macOS.** The first pipeline run of this task was red
on both: the hosts stopped with `E-GEOM-BACKEND-INIT`. The package stores the build folder of a
runner, such as `/home/runner/work/3DEngine/3DEngine/build/src`, as the search path of `libmanifold`
in `libmanifoldc`, and not `$ORIGIN` or `@loader_path`, so the system loader did not find the
dependency. Until now the native tests skipped there, the hosts took the managed backend, and each run
was green. The backend now loads the dependency first by its full path, and the run after it was green
on the three runners with no native test skipped. Register entry R-0035 holds the rebuild of the
package, which is the correct end state.

**The registry.** `E-GEOM-BACKEND-INIT` moved from the reserved codes to the raised codes of
`docs/diagnostics.md`, with the same name and meaning. The reserve holds one code, and the budget of
`DiagnosticsReserveGateTests` is one; with the reserved row put back, the gate failed. The three places
that read ADR-0014 as a rule for a fallback agree with the code, and the comment of `EngineHost.cs`
that cited the clamp removed in v0.17 is corrected (a part of finding C1).

Not done, and why: a platform with no payload, such as `win-arm64`, `linux-arm64` or `osx-x64`, now
cannot start a host. That is the honest result until register entry R-0020 decides the platforms.

New tests: 12. The test list holds 258, up from 246. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. New register entry: R-0035. Open entries: 15 of 15, the limit.
New diagnostic codes: none; one code moved from reserved to raised. The next codebase review must come before ledger entry v0.47.
## v0.45 — The native package finds its own dependency on each platform, names its source, and a commit rebuilds it (governance, TASK-0048)

Register entries R-0035 and R-0018, and the decision of the owner of 2026-10-06 that the pack job may
push the package to a branch (question Q1 of TASK-0048).

**The package 3.5.2.1.** It replaces 3.5.2, from the same Manifold source, `v3.5.2`. On Linux and
macOS each library now finds its dependency in its own folder: the build gives `$ORIGIN` and
`@loader_path`, and on macOS a step removes the absolute search path and signs the library again.
The nuspec records the Manifold address and commit `11235e6b…`, and no commit or branch of this
repository. The workaround of TASK-0036 is removed, and the pipeline passed on the three runners with
no native test skipped.

**A rebuild needs no manual step.** A push that changes `eng/manifold-native/manifold-ref.txt` or the
workflow starts the build, and the pack job pushes the package to `native-package/<version>`, where git
fetches it with no sign-in. Only the pack job can write; the jobs that compile the third-party source
read only. A check in the build job fails on a path of a build machine, and it writes each failure as
a public annotation.

**Each step failed first, or was found.** The first form, with no flag, failed its check on Linux and
macOS as planned. The install flags then had no effect, because Manifold's own `CMakeLists.txt` sets
those variables; the build flags of the build tree worked on Linux, and macOS needed the step after
the staging. The first package also named a branch of this repository; a local pack of one minute found
the flag that stops it. Two gate tests failed on 3.5.2 and pass on 3.5.2.1.

**On the way.** The polls of this evening used up the API limit of 60 requests an hour once. The rule
is now: the first check at the normal end of a run, then every 3 minutes. The owner decided that a
local build of the native libraries is for development and test, and the official package comes from
GitHub; a later task makes the shared build script and the local build.

New tests: 2. The test list holds 260, up from 258. `dotnet build 3DEngine.sln --no-incremental` gives
zero errors and zero warnings. Closed: R-0018, R-0035. Open entries: 13 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.47.
## v0.46 — One build script makes the native libraries in the pipeline and on this computer (governance, TASK-0049)

The owner decided on 2026-10-06 that a local build of the native libraries is for development and test,
and that the official package comes from GitHub. On 2026-10-08 the owner permitted the merge of each
task of the night after a green run on the three runners.

**One recipe.** `eng/manifold-native/build.sh` builds, stages and checks Linux and macOS, and
`build.ps1` builds Windows. The workflow calls the two scripts and keeps no copy of their steps, and
`eng/manifold-native/README.md` gives the local commands. Run 37696665835 built and checked with them
on the three runners.

**A version is immutable, and only main publishes.** The pack job pushes nothing when the branch of a
version exists, and nothing from a branch other than `main`. A tag push starts no run. The token of the
workflow stays off the disk, and the pin values pass strict forms before they reach a script.

**Found on the way.** A local run on this computer stopped at the link: the Windows SDK is absent, so
`kernel32.lib` cannot be found. The review agents of the night found five defects in the new code
before its merge, and this task corrected them: a second build into one staging folder, a stop of
PowerShell 5.1 at a CMake warning, a branch that could claim a version, the token on disk, and unchecked
pin values.

Not done, and why: the local Windows build waits for the Windows SDK, and the local Linux build for an
Ubuntu distribution in WSL. Both are installs of the owner.

New tests: 0. The test list holds 260. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next codebase review must come
before ledger entry v0.47.

## v0.47 — The codebase review of 2026-10-08 (governance, TASK-0050)

The fourth codebase review, `docs/reviews/2026-10-08-codebase-review.md`. It examines `main` at
`9ad025b`, the merge of v0.46. The gate permitted six milestones after v0.40, and v0.46 was the sixth,
so this entry had to be the review. The owner put it second in the order of the night.

**What it found.** Fifty-five findings are open: no critical, seven high, twenty-seven medium and
twenty-one low. Seven earlier findings are fixed: E1, E3, E7, E10, T3, T8 and T9. Twenty-seven findings
are new, and TASK-0049 corrected five of them before its merge (T18 to T22). The most important new
ones:

- T14: a module initializer in any test file can clear the variable that the write-set gate reads, so
  a commit can judge itself. Question Q1 of the review asks the owner where the check must run.
- E25: a replayed Document can never get a command bus, so the load path of TASK-0019 cannot continue
  live after a replay.
- E27 and E28: after a divergence the backend keeps its bodies, and a handler exception escapes the
  replay raw.

**Corrections of the repository.** Three statements were false, and this task corrects them.
`THIRD-PARTY-NOTICES.md` named the package 3.5.2, which `nuget/` no longer holds. TASK-0036 said that
`WebApplicationFactory` stops `Program.cs` at its build; an injection after the start check failed 46
of 59 HTTP tests, so the factory runs that code. TASK-0038 lost the permit for
`Engine.Tests/Engine.Tests.csproj` when TASK-0047 made it a gate file (T15); its write set names the
file again.

**Owners.** R-0036 holds E25 to E39. R-0031 holds T14, T16, T17, T23, T25 and the comment part of T24.
R-0031 and R-0033 have a progress line. The next task corrects E20 and E23, before TASK-0037.

New tests: 0. The test list holds 260. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.53.

## v0.48 — A call back into the session fails at once, and the HTTP host refuses a foreign address from each source (governance, TASK-0051)

Two defects of the review of 2026-10-04 that TASK-0038 must not ship with. The owner put their
correction before TASK-0037 on 2026-10-08.

**E20.** A sink or a read function that called `DocumentSession` waited for the section that its own
flow held, and each other client waited behind it. Now the flow that holds the section carries a
token, and the session refuses a call with the token of the current holder:
`InvalidOperationException`, at once. The other callers wait as before.

**E23.** The HTTP host checked the key `urls` only. It now also checks each
`Kestrel:Endpoints:<name>:Url` and the port keys of `ASPNETCORE_HTTP_PORTS` and
`ASPNETCORE_HTTPS_PORTS` before the start, so it never binds a foreign address from a known source.
After the start it reads the addresses that the server bound, and it stops with exit code 1 when one
is not a loopback address.

**Proof.** Each new test that tests a defect failed first: 8 of the 12 that ran. Before the correction
the host with a `Kestrel:Endpoints` address died with an unhandled exception when it tried to bind. An
injection proved the check after the start, which no test reaches without a bind on each interface.

**Not explained.** One local run of the full suite ended with "test run aborted", and its output is
lost. Twenty later runs passed, and no run made a crash dump. R-0034 has a progress line.

TASK-0038 now depends on TASK-0051. R-0033 has a progress line.

New tests: 13. The test list holds 273. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.53.

## v0.49 — An operation consumes its operands, and the Document holds the live bodies only (P0.16, TASK-0037)

ADR-0021, which the owner decided on 2026-09-25. Before this entry each operation added a body and
kept its operands, so the first demonstration left four bodies in the Document and the person saw one
solid.

**The contract.** `CommandHandlerResult` has `ConsumedBodies`, empty by default. `Translate` consumes
its source body, and `Subtract` consumes both operands. The bus removes each consumed body in the
commit and emits `body.consumed` after each `body.created`, so a subscriber that draws between two
events never sees fewer bodies than the result has. `/schema/events` lists the new kind. The bus
refuses a consumed list with a body that is not live or a body two times, before the commit.

**What a client sees.** After `CreateBox` A, `CreateBox` B, `Translate` B and `Subtract`, the Document
and the reset snapshot hold one body: the cut solid. A command on a consumed body gets
`E-GEOM-BODY-NOT-FOUND`, and the backend gets no call. A subtract of a body from itself gets
`E-GEOM-INVALID-PARAM`, and the body stays. A replay gives the same live set.

**Proof.** The contract field came first and alone, and 9 of 9 new tests failed on the old behaviour.
Two native tests asserted the old state; each one now asserts ADR-0021, and no test was deleted.

**Open.** TASK-0037 asked for the payload field of `body.consumed` in `/schema/events`. No entry of the
endpoint has payload fields, and ADR-0021 asks for the kind only. R-0037 asks the owner.

The roadmap lists P0.16 as shipped: the six phases that remain give the first objective in 26 to 38
evenings. A glossary term: live body.

New tests: 10. The test list holds 283. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 15 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.53.

## v0.50 — The decisions of the owner of 2026-10-08 (governance, TASK-0052)

After v0.49 the owner followed three recommendations of the agent.

**The payload fields of the events.** `/schema/events` will give the payload fields of each kind.
ADR-0008 §9 already decides this, so R-0037 closed with the outcome "decided": the code disagrees with
the ADR, and it was never an open question. TASK-0055 corrects the code. The recommendation said that
an amendment of ADR-0008 was necessary, which was false; the closed entry says so.

**The judge of the write set.** The answer to Q1 of the review of 2026-10-08 is option A: the dynamic
write-set check moves out of the test assembly into a program in `eng/` that the pipeline builds from
`main` (finding T14). TASK-0053 holds it, and R-0031 records the answer.

**TASK-0038 in two.** TASK-0038 now makes the surface a library and `engine-api-http` its program, and
it moves the address checks of TASK-0051 into the library. TASK-0054 mounts the surface in the desktop
host and changes `CLAUDE.md` and the dependency gate. The first version of TASK-0038 named two sections
of `CLAUDE.md` that do not exist; the new versions name the section "Authority diagram".

**The order.** P0.17 is TASK-0053, P0.18 is TASK-0038, P0.19 is TASK-0054, and P0.20 is TASK-0055. The
field `depends-on` holds the same chain. R5 needs P0.17 to P0.19, and the eight phases that remain give
the first objective in 27 to 39 evenings.

New tests: 0. The test list holds 283. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Closed: R-0037. Open entries: 14 of 15. New diagnostic codes: none. The next codebase
review must come before ledger entry v0.53.

## v0.51 — The judge of the write set is a program that the pipeline builds from main (P0.17, TASK-0053)

Finding T14 of the review of 2026-10-08 (high): the pipeline ran the write-set check as a test from the
build of the commit that it judged, so a file under `Engine.Tests/` could clear the variable that the
test read and switch the check off for its own commit. The owner chose option A on 2026-10-08.

**The change.** The rules moved, with no change, into the program `eng/write-set-check`. The job "Write
set of the governing task" builds it from `main`, or from the tip before the push for a push to `main`,
and runs it on each commit of the push. The steps of the range, the cut-off and the merge rule moved
from the workflow into the program too. The program holds the reader of the task files, and the gates
of `Engine.Tests` use the same reader. Before a commit a person runs
`dotnet run --project eng/write-set-check -- --staged --task TASK-nnnn`.

**Proof.** The 103 commits after the cut-off pass the new judge, as they passed the old gate. Each of
eight rules failed on its own injection. A local run of the pipeline steps put two injected commits on
top of the first commit of the task: the attack of the review, and a commit that changed the judge to
pass all. The judge built from the base failed both, and the judge of the injected commit passed both.
The same run on a branch in the pipeline can only come after the merge, because `main` holds no judge
before it.

**Weakest.** GitHub runs the workflow of the pushed commit, so a task that names
`.github/workflows/ci.yml` exactly can still change the job. A branch protection on GitHub closes that.

New tests: 20, and the dynamic test of `WriteSetGateTests` left. The test list holds 302. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.53.

## v0.52 — The pipeline proves the judge of the write set from main (governance, TASK-0053)

The owner permitted the merge of TASK-0053 on 2026-10-09, and v0.51 is on `main` at `8ba98e3`. Run
37963223561 on `main` is green on the three runners.

**The proof in the pipeline.** Run 37963719832 on the branch `inject-t14-never-merge`, which was never
merged and is deleted. It held two commits on top of `main`: the attack of finding T14 under TASK-0038,
and a judge that reports a pass for each change under TASK-0053. Each commit also changed a path that
its task forbids. The job built the judge from `main` and failed both commits. The judge of the second
commit passed both in a local run. This was the open part of criteria 1 and 2 of TASK-0053.

**Found on the way.** The job "Contract-touched-needs-ADR" passed a commit that changed
`Engine.Contracts/Document.cs` with no ADR. On the first push of a branch it compares the range from the
cut-off commit, and some ADR changed in that range. A push to `main` is not affected. R-0031 records it.
The Windows gate of the same run failed in the heartbeat test on time, which is T12; R-0034 records it,
and its limit is 2026-11-03.

TASK-0053 was reopened in one commit and closed in the next, because a task that is Done before and
after a commit governs nothing.

New tests: 0. The test list holds 302. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next ledger entry must be a
codebase review: v0.52 is the sixth milestone after v0.46.

## v0.53 — The codebase review of 2026-10-09 (governance, TASK-0056)

The fifth codebase review, `docs/reviews/2026-10-09-codebase-review.md`. It examines `main` at `f9521de`,
the merge of v0.52. The gate permitted six milestones after v0.46, and v0.52 was the sixth, so this
entry had to be the review. The owner said "go with the review" on 2026-10-09.

**What it found.** Sixty-eight findings are open: no critical, nine high, twenty-nine medium and thirty
low. Four earlier findings are fixed: E20, E23, T11 and T14. Eighteen findings are new. The most
important:

- T29 (high): a program named `git` in the judged checkout replaces git for the judge of TASK-0053.
- T30 (high): a root commit, a merge of unrelated history, and a merge that takes the tree of an old
  commit each pass any change, because the judge reads a merge with `--cc`. The old workflow had the
  same steps.
- T31 (high): a test file can point the other gates of `Engine.Tests` at a clean copy, the attack class
  of T14.
- E40 and E41 (medium): an address with a user part, and a change to `appsettings.json` after the
  start, each make the HTTP host bind a foreign address.
- T26 (medium): the contract gate checks a range and not a commit. My progress line on R-0031 of
  2026-10-09 said that a later push was not affected; that was false, and a new line corrects it.

**Owners.** TASK-0057 holds the judge and the pipeline (T26, T28 to T35), and TASK-0058 holds the guards
of TASK-0051 and TASK-0037 (E40 to E45, E47, T27). TASK-0055 holds E46. The roadmap lists P0.17 as
shipped, which TASK-0053 did not record, and puts the two tasks before TASK-0038 as P0.18 and P0.19. The
nine phases that remain give the first objective in 28 to 41 evenings.

**Questions for the owner.** Q1: a branch protection on GitHub that refuses a force push to `main`.
Q2: how the judge closes T31. The review gives a recommendation for each.

New tests: 0. The test list holds 302. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.59.

## v0.54 — The judge of the write set and the contract rule close the holes of the fifth review (P0.18, TASK-0057)

The codebase review of 2026-10-09 found three high holes in the judge of TASK-0053 and five medium or
low defects in it and in the pipeline. On 2026-10-10 the owner answered its questions: yes to a branch
protection of `main` against a force push, which the owner sets on GitHub, and option A for T31.

**The judge.** It runs git by a full path from a folder of `PATH` outside the judged checkout (T29). A
commit with no parent fails, and a merge is judged on each file that differs from its first parent and
that no commit of its second parent changed; a merge of unrelated history fails (T30). A changed test
file that holds one of four calls that can redirect a gate fails (T31). The task files come from the
commit (T33). It reads the paths of git with `-z`, and a failed git command is a failure (T35).

**The pipeline.** The judge comes from `main`, or the job fails; on `main` an unknown tip before the push
fails (T28). A branch is judged from its merge base with `main` (T32). The contract rule is now a rule of
the judge for each commit, and the job "Contract-touched-needs-ADR", which checked a range, is gone
(T26). The documented check before a commit joins its two commands with `&&` (T34).

**Proof.** `WriteSetRangeTests` runs the real judge on scratch repositories with git, so each run of the
pipeline repeats the attacks of the review on three operating systems. Eight of its ten tests failed on
the old behaviour. The replay of the 112 commits after the cut-off passes.

**T12.** Two timing tests failed in the run of the answers commit, which changed documents only: three
failures in four runs. R-0034 records it, and its limit is 2026-11-03.

New tests: 10, and one test of `WriteSetJudgeTests` changed its subject from the arrow of a rename to
the paths of `-z`. The test list holds 312. `dotnet build 3DEngine.sln --no-incremental` gives zero
errors and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review
must come before ledger entry v0.59.

## v0.55 — The three tests of finding T12 no longer depend on the speed of the runner (governance, TASK-0059)

Finding T12 of the codebase review of 2026-10-04, register entry R-0034. The tests failed in four of the
last six runs, the last one on `main` after the merge of v0.54 (run 37998763776, the heartbeat test on
Windows). The owner chose this task on 2026-10-10.

**The changes.** The heartbeat test counts the frames that arrive in at least one second against the
time since before the connection; slowness can only lower the count, and the defect of finding E8 gave
6,368 frames in 1,026 ms against a most of 12. The deadlock test starts its clock after the host is up,
and each loop runs at least one time. The collection "real host" has a definition with parallel runs
off, so its tests run alone.

**Proof.** Injected waits that copy a slow runner gave the two messages of the pipeline with the old
tests, and the new tests passed with the same waits. The inverted order of the locks still fails the
deadlock test. Five full local runs passed.

**Closed.** R-0034: each part of its exit is met. The abort of one local run of 2026-10-08 did not come
back; a second one gets a new entry.

New tests: 0; two tests changed their method. The test list holds 312. `dotnet build 3DEngine.sln
--no-incremental` gives zero errors and zero warnings. Open entries: 13 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.59.

## v0.56 — The address guard, the session guard and the consumed list close the gaps of the fifth review (P0.19, TASK-0058)

The codebase review of 2026-10-09 found gaps in the guards of TASK-0051 and TASK-0037. TASK-0038 moves
the address checks into a library, so this task came first.

**The HTTP host.** The start check reads each address with `BindingAddress.Parse`, the parser of
Kestrel, so the check and the bind read the same host. `http://evil@localhost:5000` passed the check
with `System.Uri`, and Kestrel then bound each interface; it is now refused (E40). The endpoint
configuration no longer reloads after the start, so a change of `appsettings.json` starts no endpoint
that no check reads (E41).

**The bus.** It refuses a created handle that it created before, live or consumed, and a created handle
that the same command consumes (E44). A command that created and consumed its own handle was applied
with no live body. With the rule, a consumed body never becomes live again, as ADR-0021 item 1 says.

**Texts.** The comment of the session gives the two limits of the check of a call back and the rule
for a sink (E42, E43). The message of a port key names each source, and the glossary says that the
order of `Document.Bodies` is not history (E45, E47).

**Proof.** Six new tests of a defect failed on the old code. The assertion of T27 failed on a probe
that bound 127.0.0.1 before an injected refusal; the probe was never committed.

New tests: 6, and one assertion more in the process test of the addresses. The test list holds 318.
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 13 of
15. New diagnostic codes: none. The next codebase review must come before ledger entry v0.59.

## v0.57 — The HTTP and WebSocket surface is a library, and engine-api-http is its program (P0.20, TASK-0038)

ADR-0019, the hybrid topology, which the owner decided on 2026-09-25. Before this entry only the
program `engine-api-http` could serve the surface, and no other host could mount it on its own session.

**The library.** `Engine.Api.Http` is a class library. A host composes an `EngineHost` with the backend
of its choice, and calls `EngineSurface`: the address check before the build, `AddEngineSurface` with
its session, `MapEngineSurface`, and the check of the bound addresses after the start. The address
checks of TASK-0044, TASK-0051 and TASK-0058 live in the library, so each host uses the same checks.
The library references `Engine.Core` and `Engine.Contracts` only.

**The program.** `Engine.Api.Http.Server` is the program `engine-api-http`. It chooses the native
backend, composes the session, mounts the library, and keeps exit codes 1 and 3. Each existing HTTP
test passes with no change to its meaning.

**Proof.** `DesktopSurfaceTests` is the host that the desktop will be: it mounts the surface on its own
session on a real Kestrel at 127.0.0.1. A command over HTTP changes the session that the test reads
with a typed query, and a WebSocket subscriber receives `body.created` for a typed call of the test.
Two injections failed the tests: a session with the plain sink, and a second session behind the
surface. The dependency gate has a class "surface library" with the rule of ADR-0019 item 3, and the
dispatch gate reads the new program; an injection proved each one.

**Corrected.** ADR-0019 item 3 said that the project referenced only `Engine.Core` and
`Engine.Contracts` "as the project does today", which was false: it also referenced the native
backend as the composition root.

New tests: 3, and two tests changed the name of a call. The test list holds 321. `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Projects in the solution: 10. Open
entries: 13 of 15. New diagnostic codes: none. The next codebase review must come before ledger entry
v0.59.

## v0.58 — The desktop host owns a document session and serves the HTTP and WebSocket surface (P0.21, TASK-0054)

ADR-0019, the hybrid topology. With TASK-0038 the surface became a library; now the desktop host mounts
it on its own session, so an agent sends a command over HTTP to the document that the person has open.

**The desktop.** It composes a session with the native backend, mounts the surface on 127.0.0.1 at the
port of the setting `port` (5180 by default), and writes the address before the window opens. The
surface stops with the window. The desktop stops before the window with exit code 1 for an address that
is not loopback, 2 for an invalid port, and 3 when the native library does not load.

**The run.** On Windows on 2026-10-11 the desktop wrote `3DEngine serves the document at
http://127.0.0.1:5180`, created its Vulkan instance, loaded its scene, and answered
`GET /schema/commands` with status 200 and the four commands. The log shows the window steps; no
screenshot was taken.

**The rules.** `CLAUDE.md`, section "Authority diagram", names the two hosts of the surface library.
`DependencyDirectionGateTests` lets each client reference the surface library; an injected reference
from the library to `3DEngine` failed three of its rules.

New tests: 0. The test list holds 321. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next ledger entry must be a
codebase review: v0.58 is the sixth milestone after v0.52.

## v0.59 — The codebase review of 2026-10-11 (governance, TASK-0060)

The sixth codebase review, `docs/reviews/2026-10-11-codebase-review.md`. It examines `main` at `de35f51`,
the merge of v0.58. The gate permitted six milestones after v0.52, and v0.58 was the sixth, so this
entry had to be the review. The owner said "merge, then go with the review" on 2026-10-11.

**What it found.** Sixty-seven findings are open: no critical, eight high, twenty-nine medium and thirty
low. Sixteen earlier findings are fixed, and four more in part. Fifteen findings are new. The most
important:

- T36 (high): the judge of TASK-0057 lost one rule of the old judge. A merge of `main` into a branch can
  rewrite any file that `main` changed; the old judge failed that merge, and the new one passes it.
- T31 (high) is open again: the four texts of option A have six bypasses, and two ran in a test process.
  Question Q1 asks the owner for option B.
- E48 (medium): seven forms with brackets pass the address check before the start of each host, and
  Kestrel binds each interface for them.
- E49, E50 (medium): a subscriber holds the stop of a host for 30 s, and Ctrl+C stops the surface of the
  desktop and leaves the window.

**My own errors.** The last review gave T31 the state "Fixed", and a line on R-0031 said that a branch
protection closes the hole of the workflow file. Both were false; this review and a new progress line
correct them.

**Owners and order.** TASK-0061 holds the judge and the pipeline, and TASK-0062 holds the hosts. The
roadmap gives them P0.23 and P0.24 before R2, and TASK-0055 now depends on TASK-0062. The roadmap also
lists P0.18 to P0.21 as shipped, which their tasks did not record. The seven phases that remain give the
first objective in 25 to 37 evenings.

**Questions for the owner.** Q1: move the repository gates out of the test assembly (T31). Q2: accept the
hole of the workflow file, or require a review of `.github/workflows/**` on GitHub (T28, T38).

New tests: 0. The test list holds 321. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.65.

## v0.60 — The decisions of the owner of 2026-10-11 (governance, TASK-0063)

On 2026-10-11 the owner answered the two questions of the codebase review of 2026-10-11.

**Q1, yes: the gates leave the test assembly.** The text rule of option A for T31 has six bypasses, so
the gates that read only files of the repository move into a program that the pipeline builds from
`main`, as the judge of the write set already does. TASK-0064 holds it as P0.25, after TASK-0061 and
TASK-0062. The gates that run engine code stay tests.

**Q2, option A: the workflow file.** A commit that names `.github/workflows/ci.yml` exactly can change
the job that judges it, because GitHub runs the workflow of the pushed commit. The project accepts the
condition: the exact permit and the diff show each such change, and a required review would need pull
requests. Register entry R-0038 holds it under "Accepted compromises".

**The order.** P0.23 (TASK-0061), P0.24 (TASK-0062) and P0.25 (TASK-0064) come before R2 and before
TASK-0055, which now depends on TASK-0064. The eight phases that remain give the first objective in 28
to 41 evenings.

New tests: 0. The test list holds 321. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15, and one more accepted compromise. New diagnostic codes: none.
The next codebase review must come before ledger entry v0.65.

## v0.61 — The judge of the write set judges each change of a merge, and its tests hold each rule (P0.23, TASK-0061)

Finding T36 of the codebase review of 2026-10-11 (high): the judge of TASK-0057 read a merge on its own
change minus the files that the side of its second parent changed, so a merge of `main` into a branch
could rewrite any file that `main` changed. The old judge read a merge with `--cc`, and it failed that
merge; TASK-0057 lost the rule.

**The change.** A merge is judged on the union of each file that differs from each parent (`--cc`) and
each file that differs from its first parent and that no commit of its second parent changed. Each set
alone misses the case of the other one. `FindGit` resolves links and junctions before it compares the
place of git with the checkout, and a failed read of a file that a rule needs fails the change (T40).
The fallback for exit code 2 left the workflow (T39).

**Proof.** The two forms of T36 failed on the old rule and fail on the new one. A new test of a merge of
unrelated history fails when its refusal is removed, which the older test did not (T37). A test of a
junction fails when the links are not resolved. The 128 commits after the cut-off pass.

New tests: 6. The test list holds 327. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.65.

## v0.62 — The hosts of the surface refuse each bracket form, stop well, and own their session (P0.24, TASK-0062)

The codebase review of 2026-10-11 found ten gaps in the hosts of the surface, E48 to E57. Seven forms
with brackets passed the address check, and Kestrel bound each interface for them (E48). A subscriber
held the stop of a host for 31.0 s (E49).

**The change.**

- The address check accepts brackets only as one pair around an IPv6 loopback address (E48).
- The `/events` endpoint ends its subscriber when the host stops (E49). The window loop of the desktop
  ends when the surface stops (E50).
- A port in use gives the new exit code 4 in `engine-api-http` and in the desktop (E51).
- The container of the desktop makes the session, so the disposal of the app disposes the backend
  (E52).
- The bus lost a clause that never decided, and a test gives one handle two times (E53).
- The port of the desktop is the setting `Desktop:Port`, from `--port`; the variable `PORT` has no
  effect (E54).
- The refusal messages name the surface (E55). `EngineHost.Backend` is internal (E56).
- ADR-0019, `EngineHost` and `DesktopSurfaceTests` no longer describe the desktop surface as work to
  come (E57). This corrects false statements about the repository, not a decision.

**Proof.** The seven forms of E48, the port in use and the stop with a subscriber failed on v0.61, and
pass now. The new case of E53 fails when its clause is removed. A run of the desktop executable on a
held port, with `PORT=abc`, stopped before the window with exit code 4. No test calls the desktop
surface, and E50 is not observed; register entry R-0039 holds the gap.

New tests: 10. The test list holds 337. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. New exit code: 4, in
`3DEngine/Documentation/Running.md` and in the header of `Engine.Api.Http.Server/Program.cs`. The next
codebase review must come before ledger entry v0.65.

## v0.63 — The gates that read the repository run as a program that the pipeline builds from main (P0.25, TASK-0064)

Finding T31 of the codebase review of 2026-10-11 (high): a file of the judged commit could change what
a gate of `Engine.Tests` read before the gate ran. Two forms of the review moved
`AppContext.BaseDirectory` from a test file and passed the four-text refusal of TASK-0057. On
2026-10-11 the owner chose option B: the gates leave the test process.

**The change.**

- The new program `eng/repo-gates` compiles the fourteen gate classes that read only files of the
  repository, with their helpers. It runs each check on the checkout that `--root` gives.
- The job "Write set of the governing task" builds it from the same checkout of `main` as the judge,
  and runs it on the judged checkout. When the judged commits change a source of the program, the job
  builds it from the judged commit instead, with the condition of R-0038.
- The gate files keep their paths, because three ADRs name one of them in `enforced-by`. `Engine.Tests`
  still runs them as tests, but the run of the program decides.
- The judge counts `eng/repo-gates/` as gate files. `RepoGates` is in the class of judges of
  `DependencyDirectionGateTests`. The solution holds both programs.
- `CLAUDE.md`, section "Method", gives the new local command. So does `docs/templates.md`, section 5.
  The task named section 6, which holds the rule for rules.

**Proof.** One injection for each of the fourteen gate classes, in the data of the checkout, failed its
own gate from the program. The attack of the review, with the escaped attribute and
`AppDomain.SetData`, passed the marker gate as a test and failed it from the program. The three gates
that run engine code stay tests, and the attack class still reaches them; R-0031 records it.

New tests: 2. The test list holds 339. The program runs 65 checks in 70 runs. `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New
diagnostic codes: none. The next codebase review must come before ledger entry v0.65.

## v0.64 — A mesh leaves the geometry backend through the query GetTessellation (R2, TASK-0028)

Roadmap phase R2, the first task of the batch R2 to R6 that the owner ordered on 2026-10-12. Until
this task no capability gave a triangle, and R3 to R6 had nothing to draw.

**The change.**

- `Engine.Contracts` has the interface `ITessellationOps`, the value type `Tessellation` and the flag
  `BackendCapabilities.Tessellation` (ADR-0018 items 1 and 2).
- `Engine.Core` has the query `GetTessellation`, version 1, in the handler catalog. Its errors are
  `E-GEOM-CAP-MISSING` and `E-GEOM-BODY-NOT-FOUND`, which exist.
- `ManifoldGeometryBackend` gives the triangles through `manifold_get_meshgl64`, in double precision,
  wound counter-clockwise when seen from outside. The managed backend does not implement it.
- `Engine.Cli` and `Engine.Api.Http` hold each query result as an object (ADR-0018 item 7).
- ADR-0018 names its test with no task marker. The glossary has the term "tessellation".
- The roadmap records P0.23 to P0.25 and R2 as shipped, and the first objective now needs R3 to R6:
  19 to 27 evenings. Two statements were false: the glossary said that the desktop host is a later
  host of the surface, and the roadmap missed P0.23 to P0.25.

**Proof.** The 2 × 3 × 4 box gives 12 triangles with a signed volume of +24, equal to
`manifold_volume`; a reversed winding injected into the backend failed with −24. The cut of two
2 × 2 × 2 boxes gives a closed mesh of +4. The HTTP scenario failed on v0.63 and passes. A body of
90,012 triangles took 26.1 ms to tessellate and 4,378,820 bytes of JSON. The first demonstration gives
8 vertices and 12 triangles for the cut body. The command line cannot show a record result end to
end, because each invocation has an empty Document; the task records it in "Weakest".

New tests: 14. The test list holds 353. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.65, so it comes next, before R3.

## v0.65 — The codebase review of 2026-10-12 (governance, TASK-0065)

The seventh codebase review, `docs/reviews/2026-10-12-codebase-review.md`, examines `main` at `4a4450d`,
the merge of v0.64. The review of 2026-10-11 examined v0.58, and v0.64 was the sixth entry after it, so
this entry had to be the review.

**The result.**

- Thirteen findings of the last review are fixed (E48, E52 to E56, T36, T38 to T40 in full, and E49 to
  E51 for the case that they named), and four in part (E13, E57, T31, T37).
- Ten findings are new. Three are high: a `global.json` of the judged checkout chooses the SDK that
  builds the judge and the gates of `main` (T41); a merge can undo a change of `main` with no judgement
  (T42); files that are not gate files switch the gates to the judged commit and control its packages
  (T44). T41 is a defect of my own step of TASK-0064.
- The tessellation of TASK-0028 holds under 200,000 calls, extreme sizes and 16 parallel tasks.
- Sixty-four findings are open: no critical, ten high, twenty-eight medium and twenty-six low.

**The tasks.** TASK-0066 (P0.26) holds T41 to T47, and TASK-0067 (P0.27) holds E58 to E60. The register
stays at 14 of 15.

**Questions for the owner.** Q1: must TASK-0066 come before R3 (recommended: yes)? Q2: four decisions
for R3: how the SPIR-V is made, render pass or dynamic rendering, where the tests of the render side
live (ADR-0009 line 68 and the dependency gate disagree), and which platform R3 shows.

New tests: none. The test list holds 353. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.71.

## v0.66 — The decisions of the owner of 2026-10-12 (governance, TASK-0068)

On 2026-10-12 the owner answered the two questions of the codebase review of 2026-10-12 with "A to all,
then continue the batch".

**Q1, option A: TASK-0066 first.** P0.26 must be done before R3, because each task of the batch relies
on the checks that T41, T42 and T44 can defeat. The first objective now needs P0.26 and R3 to R6: 21 to
30 evenings.

**Q2, option A for each decision of R3.** The GLSL source and the SPIR-V are both in the repository,
with a test of a hash of the source; the pipeline uses the render pass that exists; a new test project
`3DEngine.Tests`, with no GPU, may reference the render side, and ADR-0009 will name it; R3 shows its
result on the computer of the owner. TASK-0069 holds R3 and these decisions, and its first commit writes
them as ADR-0022.

New tests: none. The test list holds 353. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.71.

## v0.67 — The checks from main take no input of the build from the judged checkout, and the judge reads each change of a merge (P0.26, TASK-0066)

The codebase review of 2026-10-12 found three high holes in the checks that run from `main`, and the
owner put their task before R3 (question Q1, option A).

**The change.**

- T41: each build of the judge and of the gates of `main` runs in `$RUNNER_TEMP`, so a `global.json` of
  the judged checkout cannot choose their SDK. This was a defect of my own step of TASK-0064.
- T44: `global.json`, each NuGet configuration and `nuget/` are gate files. The judge of `main` answers
  the switch of the gates with `--gate-sources`, from the list that the gate files share; the regular
  expression of the workflow is gone.
- T42, T43: a merge is judged on each file where it differs from the automatic merge of its parents
  (`git merge-tree`), and on each file with a conflict.
- T45: the determinism gate binds each name of its list with Roslyn, with no build.
- T46: the dependency gate reads the `.user` file of a project, and refuses an import that it cannot
  resolve to one file. This also closes the part of T16 that skipped an import with a property.
- T47: the runner of the gates fails on each form that it cannot run and on a skip.

**Proof.** The 15 new test cases of the judge failed on the judge of `main` and pass now. Each form of
the review failed its check from the program: the planted `global.json` (a local run), nine calls, a
wildcard import, a `.csproj.user` file, and seven probe forms of the runner. Run 38209275659 showed the
bootstrap of the switch in the pipeline: the judge of `29ed267` gave no answer, and the step took the
gates of the judged commit, as the comment of the step says.

New tests: 16. The test list holds 369. The program runs 66 checks in 71 runs. `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New
diagnostic codes: none. The next codebase review must come before ledger entry v0.71.

## v0.68 — The desktop draws an indexed mesh through the first pipeline (R3, TASK-0069)

Roadmap phase R3, in the batch R2 to R6 that the owner ordered on 2026-10-12. Until this task the
desktop cleared its window and drew nothing.

**The change.**

- ADR-0022 records the four answers of the owner to question Q2 of the codebase review of 2026-10-12,
  and amends ADR-0009 and ADR-0017: the GLSL source and the SPIR-V of each shader with a lock of hashes,
  the render pass that exists, the test project `3DEngine.Tests`, and the computer of the owner as the
  platform of R3.
- `3DEngine.Vulkan` has the first pipeline: `ShaderLibrary` (the embedded SPIR-V), `RenderPipeline`
  (y up, counter-clockwise front faces, culled back faces, one push constant), `MeshVertex` (one position
  of three floats, narrowed after the subtraction of the origin in double precision), `GpuBuffer` and
  `MemoryTypes`. `Shaders/compile.js` compiles each shader and writes `shaders.lock`.
- The desktop draws the cut body of the first demonstration, as `GetTessellation` gave it in TASK-0028.
- The new project `3DEngine.Tests` (15 tests, no GPU) has its own class in the dependency gate: it may
  reference the render side only, and `Engine.Tests` references none of it.

**Proof.** The owner accepted two pictures of the window, which I took on the order of the owner: the
triangle, and then the mesh. Each run of the desktop gave no validation message. A shader change with no
new compile, a reference from the render tests to `Engine.Core` and a reversed triangle each failed a
test.

New tests: 16 (15 in `3DEngine.Tests`, 1 in the dependency gate). The test list holds 385. `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New diagnostic
codes: none. The first objective now needs R4 to R6: 15 to 21 evenings. The next codebase review must
come before ledger entry v0.71.

## v0.69 — An orbit camera, reversed depth, and a draw list over the scene graph (R4, TASK-0070)

Roadmap phase R4, in the batch R2 to R6. The owner answered four questions on R4 on 2026-10-12, and
ADR-0023 records the answers: reversed depth, an orbit camera that the mouse moves now, the scene graph
`Scene` and `Entity`, and a draw list with direct draws.

**The change.**

- The render pass has a depth attachment of `D32_SFLOAT`, cleared to 0, and the pipeline keeps the
  greater depth.
- The `Camera` of `3DEngine.Core` is an orbit camera: a target in double precision, a distance, a yaw
  and a pitch, a view with the eye at the origin, and a reversed perspective projection.
- `IDrawList`, `DrawList` and `SceneWalker` in `3DEngine.Core`: at each frame the walk of the graph gives
  one record for each entity with a mesh, with its model-view matrix relative to the eye.
- `DrawListExecutor`, `GpuMesh` and `DepthBuffer` in `3DEngine.Vulkan`, which now references
  `3DEngine.Core`, as ADR-0017 item 2 permits.
- The desktop draws two overlapping copies of the cut body; a left drag orbits and the wheel zooms.
- The roadmap line of R4 and the glossary say what the owner decided.

**Proof.** The owner accepted two pictures: the first view, and the view after a drag that a script sent.
Neither run gave a validation message. An injected standard projection failed two of the eleven new
tests.

New tests: 11. The test list holds 396 (370 in `Engine.Tests`, 26 in `3DEngine.Tests`). `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New diagnostic
codes: none. The first objective now needs R5 and R6: 10 to 14 evenings. The next codebase review must
come before ledger entry v0.71, so it comes after R5.

## v0.70 — The desktop draws each live body of its session, from the events and GetTessellation (R5, TASK-0071)

Roadmap phase R5, in the batch R2 to R6. The owner answered two questions on R5 on 2026-10-12, and
ADR-0024 records the answers: a public subscription on `EngineHost`, and an origin in double precision on
`Entity`. ADR-0024 amends ADR-0019 and ADR-0023.

**The change.**

- `EngineHost.Subscribe` gives a host the events of its session in its process, after each commit and
  in the order of the commits; an action that throws is removed and the commit continues.
- `Entity.Origin` holds three doubles, and `SceneWalker` adds the origins and the positions in double
  precision before it narrows the position relative to the eye.
- `SessionBodies` of the desktop reads the live bodies and subscribes in one read of the session, fetches
  the mesh of each new body with `GetTessellation`, narrows it around the centre of its box, and removes
  each consumed body. The fixed mesh of the first demonstration is gone, and the window starts empty.

**Proof.** The owner accepted three pictures while a script sent the first demonstration over HTTP to
the running desktop: the empty window, the two boxes after the translate, and the cut body after the
subtraction. No run gave a validation message. An injected rethrow in the broadcaster and an injected
float origin each failed their new test.

New tests: 6. The test list holds 402 (374 in `Engine.Tests`, 28 in `3DEngine.Tests`). `dotnet build
3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New diagnostic
codes: none. The first objective now needs R6: 4 to 6 evenings. The next ledger entry must be the codebase
review.

## v0.71 — The codebase review of 2026-10-13 (governance, TASK-0072)

The eighth codebase review, `docs/reviews/2026-10-13-codebase-review.md`, examines `main` at `2cc08b6`, the
merge of v0.70. The review of 2026-10-12 examined v0.64, and v0.70 was the sixth entry after it, so this
entry had to be the review. I wrote it on 2026-10-12; it carries the date 2026-10-13, because the review gate
permits one review for each date (T53).

**The result.**

- Five findings of the last review are fixed in full (T16, T28, T41, T42, T43), and four for the forms that
  they named (T44 to T47).
- Twenty findings are new. One is high: four kinds of file that are not gate files can take `Engine.Tests`
  out of the step "Test" with a green run (T48); I ran the probe again.
- The render side of R3 to R5 is sound in its core: the depth image with three frames in flight ran clean
  under synchronization validation. The new render findings are two medium (V8: no swapchain is made again;
  V9: no check of the version of the device) and six low.
- The subscription of R5 delivered each event in order under 8 threads; it has five small gaps (E61 to E65).
- Seventy-four findings are open: no critical, eight high, twenty-eight medium and thirty-eight low.
- Two corrections of earlier texts: T13 (the reviews of 2026-10-11 and 2026-10-12 misread
  `eng/report-failed-tests.js`) and the progress line of R-0031 of 2026-10-12 (T24 was fixed).

**The tasks.** TASK-0073 (P0.28) holds T48 to T53, and TASK-0074 (P0.29) holds E61 to E65 and V8 to V15. The
register stays at 14 of 15.

**Questions for the owner.** Q1: must TASK-0073 come before R6 (recommended: yes)? Q2: six decisions for R6:
the demonstration, where the image test lives, which runners run it, how the images compare, the frame of
the camera, and how the demonstration runs again. Q3: the estimate of R6, 6 to 8 evenings in place of 4 to 6.

New tests: none. The test list holds 402. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must come before
ledger entry v0.77.

## v0.72 — The decisions of the owner of 2026-10-14 (governance, TASK-0075)

On 2026-10-14 the owner answered the three questions of the codebase review of 2026-10-13 with "A to all,
then continue the batch".

**Q1, option A: TASK-0073 first.** P0.28 comes before R6, because T48 lets a commit stop each functional
test with a green run.

**Q2, option A for each decision of R6.** The demonstration moves the second box by (5, 5, 5); a new project
`3DEngine.ImageTests` holds an image test with no window; the pipeline installs lavapipe on Ubuntu and
requires the test there, and it skips on Windows and macOS with a reason in the register; the image is
compared by structure; the camera frames the live bodies; a script in the repository runs the
demonstration. TASK-0076 holds R6 and writes these decisions as ADR-0025 in its first commit.

**Q3, option A.** R6 needs 6 to 8 evenings. The first objective now needs P0.28 and R6: 8 to 11 evenings.

New tests: none. The test list holds 402. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must come before
ledger entry v0.77.

## v0.73 — No file outside the gate files can empty the step "Test", and the gates see what MSBuild evaluates (P0.28, TASK-0073)

The codebase review of 2026-10-13 found six gaps in the checks of the pipeline, and the owner put their
task before R6 (question Q1, option A).

**The change.**

- T48: the solution file, `Directory.Solution.*`, `*.sln.targets` and each `.user` file are gate files,
  and the names of the Directory files are compared with no regard to case. The new step "Count the tests"
  runs `eng/count-tests.js` and fails when fewer tests ran than `eng/test-floor.txt` (437).
- T51: the judge runs `git merge-tree` with `-X no-renames`, so a rename cannot carry a change into a gate
  file through a clean merge. The step "Report the runner" shows the version of git.
- T49: the determinism gate refuses code that a preprocessor condition hides, reads a folder `obj/` below
  the root of a project, refuses a `Compile` or `Analyzer` item in each file that MSBuild reads for the log
  path, and forbids the creators of `System.Numerics` that call a transcendental function, `Complex.Phase`
  and reflection.
- T50: the dependency gate refuses an `Include` that MSBuild computes, a `Reference` item, each property
  that imports a file, an SDK outside the two of the repository, and each extension file in `obj/` that
  NuGet did not write.
- T52, T53: the runner refuses an `async void` check, and the review gate permits a suffix after the date
  and orders the reviews by their ledger versions.

**Proof.** Each form of the review failed its new check, and passed the check of `main`. MSBuild listed
the three references of T50. The forms of T49, T50 and T53 are 21 test cases; with the logic of `main` put
back, 20 failed, and the 21st is a guard that `main` also keeps. T52 is a recorded probe and not a test.

New tests: 35. The test list holds 437 (409 in `Engine.Tests`, 28 in `3DEngine.Tests`). The program runs
75 checks in 95 runs. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings.
Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must come before ledger entry
v0.77.

## v0.74 — A person observes the cut of the first demonstration, and an image test with no window holds it (R6, TASK-0076)

Roadmap phase R6 completes the first objective of `docs/CHARTER.md`: create a box, subtract a second box,
and observe the cut. ADR-0025 records the six decisions of the owner of 2026-10-14 (question Q2 of the
codebase review of 2026-10-13, option A for each), and amends ADR-0022 items 2 and 3, ADR-0017 items 1, 3
and 7, and ADR-0009 section 2.

**The change.**

- The device: `GraphicsDevice` is the core and takes a window or none; `Presenter` holds the
  presentation to a window; `OffscreenTarget` draws into an image and copies it to host memory;
  `RenderPipeline` takes the render pass of its target. R-0030 V5 and V6 are corrected on that path.
- The desktop: `SceneRenderer` draws the scene of the session into one target, for the window and for the
  image test, and the camera frames the live bodies at each change (D5).
- `scripts/first-demonstration.js` sends the four commands with the offset (5, 5, 5) (D1, D6).
- `3DEngine.ImageTests` (D2) draws the demonstration with no window and compares the picture by structure
  (D4), with synchronization validation on. The pipeline installs lavapipe and the validation layers on
  Ubuntu and keeps the picture of a failed run; the test skips on Windows and macOS (D3, R-0040). The
  dependency gate has the class of the image verifier.

**Proof.** The owner accepted the picture of the window after the script on 2026-10-16. The image test
failed on an injected box with no cut, a wrong cull mode and a missing dependency of the render pass, and
run 38516827182 passed on the three runners with the test on lavapipe.

**Correction.** The done list of `docs/roadmap.md` had no line for P0.28: TASK-0073 could not change that
file. This task adds it.

New tests: 10 (2 cases of the camera, 7 in `3DEngine.ImageTests`, 1 rule of the dependency gate). The
test list holds 447 (410 in `Engine.Tests`, 30 in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`); the
Windows and macOS runners skip the image test. `dotnet build 3DEngine.sln --no-incremental` gives zero
errors and zero warnings. Open entries: 15 of 15. New diagnostic codes: none. The next codebase review
must come before ledger entry v0.77.

## v0.75 — The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera (P0.29, TASK-0074)

The codebase review of 2026-10-13 found five low gaps in the subscription of R5 (E61 to E65) and eight in
the render side (V8 to V15). TASK-0076 already did V12 and the synchronization part of V13. The owner put
this task first in the order of 2026-10-16.

**The change.**

- E61 to E65: `SessionBodies` puts the live bodies in the queue inside the read and isolates the failure
  of one body. The broadcaster keeps one entry for each subscription and reports a removed action with
  the new code `W-API-SUBSCRIPTION-REMOVED`. Each payload is read-only.
- V8, V9: the presenter skips a frame while the window has no area and makes the swapchain again when
  the window changes; it owns the surface and the render pass. A device must give Vulkan 1.3 and the
  depth format, and a refusal names each device and its reason.
- V10, V11, V13, V14: a transform refuses a negative scale; the camera checks its parameters; a test
  reads the push constant block from the SPIR-V; `compile.js` removes a byte order mark, which `glslc`
  refuses. V15 is measured, and nothing changes.

**Proof.** Each new test failed with its old rule put back. A forced remake of the swapchain ran 634 times
with no validation message. Run 38971865880 passed on the three runners. The owner accepted the picture of
the window after a minimise and a restore on 2026-10-17.

**Correction.** The task file named neither `Engine.Core/DiagnosticCodes.cs` nor the endpoint
`/schema/diagnostics`, which a new code needs, nor `docs/roadmap.md`; the task added each one.

New tests: 12 (3 in `Engine.Tests`, 9 in `3DEngine.Tests`). The test list holds 459 (413 in
`Engine.Tests`, 39 in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`). `dotnet build 3DEngine.sln
--no-incremental` gives zero errors and zero warnings. Open entries: 15 of 15. New diagnostic codes:
`W-API-SUBSCRIPTION-REMOVED`. The next codebase review must come before ledger entry v0.77.

## v0.76 — The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON (P0.27, TASK-0067)

The codebase review of 2026-10-12 found three low gaps in the hosts of the surface (E58 to E60). The owner
put this task second in the order of 2026-10-16.

**The change.**

- E58: the surface library sets a stop limit of 2 s, so an open request no longer holds the stop of a host
  for 30 s.
- E59: each failure of the start other than codes 1 to 4 gives exit code 5 and a message in
  `engine-api-http` and in the desktop, and the app is disposed. This also ends the open part of E36
  (R-0036). `3DEngine/Documentation/Running.md` gives the code.
- E60: the HTTP API writes compact JSON; the command line keeps its indented output.

**Proof.** With the old rules put back, the partial request held the stop for 30.0 s, the server crashed
with the exit code -532462766, and the tessellation held line breaks; each new check failed. The desktop
stopped with code 5 for the same endpoint.

**Correction.** The task file did not name `docs/roadmap.md`; the task added it for the line of P0.27.

New tests: 2. The test list holds 461 (415 in `Engine.Tests`, 39 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero
warnings. Open entries: 15 of 15. New diagnostic codes: none. The next codebase review must come before
ledger entry v0.77, so it is the next milestone.

## v0.77 — The codebase review of 2026-10-17 (governance, TASK-0077)

The review of 2026-10-13 examined v0.70, and v0.76 was the sixth entry after it, so this entry is the ninth
codebase review: `docs/reviews/2026-10-17-codebase-review.md`, at `main` `2b8e928`. The gate put it before
TASK-0055 and TASK-0040, which the order of the owner of 2026-10-16 had put first.

**The state.** The first objective is complete, and each fix of the period held for the forms that it named:
T48 to T53, E58 to E65, and V5, V6 and V8 to V14. The measures: 8,664 production lines in 127 files, 461 tests,
0 warnings, and open findings 0 critical, 7 high, 26 medium and 41 low.

**The new findings.** Twenty-four. Two are high: two files that are not gate files can leave no test in
`Engine.Tests` with a green run (T54; I ran its first half), and the gates read MSBuild names with case
(T55). One medium of the hosts: a stop can free the geometry backend under a running command (E66). E69 is
a broken comment line from my edit script of TASK-0074.

**The tasks.** TASK-0078 (P0.30) holds T54 to T61, and TASK-0079 (P0.31) holds E66 to E71 and V16 to V24.
V25, the preview SDL with no Linux arm64, goes with decision D7. The register stays at 15 of 15.

**Questions for the owner.** Q1: must TASK-0078 come before R7 (recommended: yes)? Q2: eight decisions for
R7: touch hardware, pen, location, gestures, parity, keyboard alone, the version of SDL, and the synthetic
mouse events of a touch. Q3: selection as a command (the roadmap row of R8) or as a state of the client
(ADR-0007); the two records disagree. Q4: the estimate of R7, 5 to 7 evenings.

New tests: none. The test list holds 461. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: none. The next codebase review must come before
ledger entry v0.83.

## v0.78 — The decisions of the owner of 2026-10-17 (governance, TASK-0080)

On 2026-10-17 the owner answered the four questions of the codebase review of 2026-10-17.

**Q1, option A: TASK-0078 first.** P0.30 comes before R7.

**Q2, the decisions of R7.** D1 A: synthetic scripts, and the touch path stays unverified until a check on a
touch screen. D2 B: a pen takes the path of one finger. D3 A: the intent layer in a folder of
`3DEngine.Core`, the translator of SDL events in `3DEngine.Vulkan`. D4 A with a change: the map of the
review is the default, and the action of each gesture is a setting of the client. D5 A: the parity of intents
and a coverage check. D6 B: the keyboard alone moves the camera. D7 B: a task before R7 moves to SDL 3.2 or
later (P0.32, TASK-0081). D8 C: the hint off, and the synthetic mouse events of a touch dropped. TASK-0082
holds R7 and writes these decisions as an ADR in its first commit.

**Q3, option A.** The selection is a state of the client, as ADR-0007 says; the row of R8 no longer makes it a
command.

**Q4, option A.** R7 needs 5 to 7 evenings, and the upgrade of SDL 1 to 2 more.

New tests: none. The test list holds 461. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: none. The next codebase review must come before
ledger entry v0.83.

## v0.79 — No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does (P0.30, TASK-0078)

The codebase review of 2026-10-17 found eight gaps in the checks of the pipeline, two of them high, and the owner
put their task before R7 (question Q1, option A).

**The change.**

- T54: `TestAttributeGateTests` refuses a type, a nested type, a delegate or an alias with a name that a test
  finds through `Xunit`, such as `FactAttribute` or `Assert`, in each test project.
- T54, T58: `eng/count-tests.js` requires the exact count of each test project of `eng/test-floor.txt`, with
  a value for one platform where needed, and one result file for each project and no other.
- T55, T56: the dependency gate and the determinism gate read MSBuild names with no regard to case, and a
  project reference with a wildcard is refused.
- T57: the determinism gate binds each name of the log path and refuses a member that ends in `Native`, an
  intrinsic of one platform, reflection other than a name, and a native import outside the Manifold binding.
- T59, T60, T61: the runner fails an async lambda that returns void; the workflow reads the repository only and
  logs the versions of lavapipe; a review must declare a ledger version that exists.

**Proof.** Each form of the review failed its new check and passed the check of `main`; the cases of the new
tests each failed with the old rule. Runs 39028617456 and 39029140388 passed on the three runners.

New tests: 34 in `Engine.Tests`. The test list holds 495 (449 in `Engine.Tests`, 39 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). The gate program runs 81 checks in 123 runs. `dotnet build 3DEngine.sln
--no-incremental` gives zero errors and zero warnings. Open entries: 15 of 15. New diagnostic codes: none. The
next codebase review must come before ledger entry v0.83.

## v0.80 — `/schema/events` gives the payload fields of each event kind (P0.22, TASK-0055)

ADR-0008 §9 asks for the payload schema of each event kind, and the endpoint gave the kind only (finding E46).

**The change.** Each entry of `/schema/events` gains `payload`: the fields that a message of its kind carries,
in the form of the fields of `/schema/commands`. `command.applied` and `command.cancelled` carry `name` and
`schemaVersion`; `command.rejected` also `errorCode`; `body.created` carries `bodyId` (guid) and `kind`;
`body.consumed` carries `bodyId`; `subscription.resume` carries `fromSeq`; `subscription.reset` carries
`documentId` and `snapshot`; the other kinds carry none. The field `kind` of each entry does not change.

**Proof.** A test compares each payload that the bus emits in a scenario of each outcome with its entry; with a
field removed from an entry, it named the field.

**Correction.** The task file named neither `eng/test-floor.txt` nor `docs/roadmap.md`; the task added both.
Since TASK-0078 each task that adds tests must name `eng/test-floor.txt` in its write set.

New tests: 3. The test list holds 498 (452 in `Engine.Tests`, 39 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. The next codebase review must come before ledger entry v0.83.

## v0.81 — A codebase review follows the product milestones (P0.33, TASK-0083)

On 2026-10-27 the owner decided how a review becomes due (decisions D1 to D5 of that date) and set the order of
the next tasks.

**The change.** A review is due when more than four product milestones, or more than twelve entries of any
kind, follow the ledger version of the last review. A product milestone is an entry whose task has a phase of
track R, K, P or V, read from its task file, and whose roadmap row names that task. Track 0 and governance
entries count toward the limit of twelve only. A critical or high finding goes into a task before the next
product phase, a medium or low finding after it, and the answers of the owner to a review go into the first
commit of the next task. `CLAUDE.md`, section "Workflow", `docs/templates.md`, section 7, and
`CodebaseReviewGateTests` give the rule.

**The order.** P0.32 (TASK-0081), R7 (TASK-0082), TASK-0040, then P0.31 (TASK-0079). On TASK-0040 the owner
chose its option A: the mechanical part, and one summary of the four folds for one acceptance.

**Proof.** Three probe ledgers failed the gate of `main`; with the new gate, 7 governance entries passed, 5
product entries failed and 13 entries failed. Eight test cases hold the rule.

New tests: 8. The test list holds 506 (460 in `Engine.Tests`, 39 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.76, 0 product milestones and 5 entries
followed it.

## v0.82 — The desktop uses a binding with SDL 3.2 or later (P0.32, TASK-0081)

The codebase review of 2026-10-17 found that the native SDL of the binding was the 3.1.3 preview (finding V25),
and the owner chose a move to SDL 3.2 or later before R7 (decision D7 of 2026-10-17).

**The change.** The one pin of `Alimer.Bindings.SDL` in `3DEngine.Vulkan/3DEngine.Vulkan.csproj` moves from 3.9.2
to 3.9.8, the newest version, whose native SDL is 3.2.10. No call changed.

**Proof.** The file version of `SDL3.dll` in the output of the desktop is 3.2.10. Run 40572683824 passed on the
three runners, and the owner accepted the picture of the window after the first demonstration, a minimise and a
restore.

**What stays.** The package has no `linux-arm64` runtime, so the desktop cannot run on Linux on arm64 (V25, open in
part). No runner opens a window (R-0022).

**Correction.** The line of P0.33 in the done list of `docs/roadmap.md` is new here: TASK-0083 did not add it.

New tests: none. The test list holds 506. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero
warnings. Open entries: 15 of 15. New diagnostic codes: none. Since the review of v0.76, 0 product milestones and
6 entries followed it.

## v0.83 — Touch, mouse and keyboard give the same intents, and a parity test holds it (R7, TASK-0082)

Roadmap phase R7 serves objective 9 of `docs/CHARTER.md`: each operation works by touch, and by mouse and
keyboard. ADR-0026 records the decisions D1 to D8 of the owner of 2026-10-17, and amends ADR-0023 item 2.

**The change.**

- `3DEngine.Core/Input/`: the samples (touch, pen, mouse, wheel, key, focus lost), the intents (orbit, pan, zoom,
  select, cancel), the settings with the map of gestures as a setting of the client, and `IntentMachine`. A pen
  takes the path of one finger, and a pair of fingers moves once for each frame.
- `Camera.Pan` moves the target in the plane of the view.
- `3DEngine.Vulkan/SdlInput.cs` translates SDL events and drops each mouse event of `SDL_TOUCH_MOUSEID`; the
  window turns `SDL_HINT_TOUCH_MOUSE_EVENTS` off.
- The desktop applies the intents to the camera and keeps the point of a selection, which draws nothing yet.

**Proof.** The parity test: a touch script and a mouse script give the same intents for each kind, and a
coverage check fails for a kind with no touch script. Four injections each failed a test. Run 40579390160
passed on the three runners, and the owner accepted the pictures of the window after the mouse and after the
keyboard on 2026-10-27.

**What stays.** The touch path is unverified on a real touch screen: no computer of the project has one. The owner
chose on 2026-10-27 that this ledger and TASK-0082 hold it, and that the next codebase review carries it.

New tests: 16. The test list holds 522 (460 in `Engine.Tests`, 55 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.76, 1 product milestone and 7 entries
followed it.

## v0.84 — Each ADR in force holds its current decision in one text, and the archive holds the rest (TASK-0040)

The owner decided on 2026-09-26 that an ADR is updated with the code, and that an ADR that nobody needs
is cleaned. Until this entry a reader needed two or three records for one decision of deployment.

**The change.**

- ADR-0003 (withdrawn) and ADR-0011 (superseded by ADR-0019) are in `docs/adr/archive/`. The index lists
  their numbers under "Archived".
- ADR-0004 item 4 gives the three deployment shapes of ADR-0019, and items 2 and 5 no longer name Blazor.
- ADR-0019 items 7 and 8 hold embedded mode and the rule that engine code knows nothing about topology,
  from ADR-0011. Its `affects` gains `Engine.Cli/**`.
- ADR-0012 holds its in-file Amendment 1 in its own sections. ADR-0013 item 7 holds the construction
  step of ADR-0016.
- ADR-0007 no longer lists `BlazorApp/**`. TASK-0079 no longer lists ADR-0011.

**Proof.** `AdrGateTests` refuses a withdrawn or superseded record in `docs/adr/`, an archived number
with a file in `docs/adr/`, and a reference to a number that is neither present nor archived. Three
injections each failed a test. Run 40585801792 passed on the three runners. The owner accepted the four
changes of "Decision" on 2026-10-27.

**What stays.** Comments in five test files and in `Engine.Api.Http/Endpoints/JsonParameters.cs` cite
the archived ADR-0011. The folds of ADR-0006, 0008, 0010 and of ADR-0012 by ADR-0018 and ADR-0021 waited
for tasks that are Done, and are not done. ADR-0008 still names Blazor. The register is at its limit, so
the next codebase review carries these three items.

New tests: 1. The test list holds 523 (461 in `Engine.Tests`, 55 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings.
Open entries: 15 of 15. New diagnostic codes: none. Since the review of v0.76, 1 product milestone and 8
entries followed it.

## v0.85 — A stop waits for a running command, and the present path is checked for hazards (P0.31, TASK-0079)

TASK-0079 corrects the findings E66 to E71 and V16 to V24 of the codebase review of 2026-10-17.

**The change.**

- E66: `DocumentSession.Close` refuses each later call and waits until the serial section is free, and
  `EngineHost.Dispose` calls it before it frees the backend.
- E67, E69: the report of a removed action runs on the thread pool, out of the commit. The broken comment
  line of the broadcaster goes.
- E68, E70, E71: `engine-api-http` gives code 5 for a configuration file that it cannot read. Each early
  return of the two hosts disposes the app. The comment of the stop limit gives the measured 3.01 s.
- V16: the setting `Desktop:SynchronizationValidation` turns on the checks of synchronization.
- V17, V21: a failed image test gives the text of each validation message. The messenger needs debug utils.
- V18, V19, V20: a remake makes the new swapchain first and keeps the old state on a failure. A failed
  submit throws, and each wait for a frame has a limit of 5 s. The composite alpha comes from the surface.
- V22: the camera refuses a distance of 0 and NaN, and `Frame` reports a box that the planes cannot hold.
- V23, V24: a minimised desktop waits for an event. The comment of the push constant test says what it holds.

**Proof.** 17 new tests, and each one failed with its old rule put back. The desktop ran the
demonstration, a minimise and a restore under synchronization validation with no message, and a forced
remake every 60 frames ran 228 times with no message. The owner accepted the pictures and the log on
2026-10-27.

**What stays.** E71, V19 and V21 have no test. Commit a0494b6 does not build, and ada1542 corrects it. A
minimise does not remake the swapchain on Windows here, so the check forced the remakes.

New tests: 17. The test list holds 540 (466 in `Engine.Tests`, 67 in `3DEngine.Tests`, 7 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings.
Open entries: 15 of 15. New diagnostic codes: none. Since the review of v0.76, 1 product milestone and 9
entries followed it.

## v0.86 — The desktop shows text and a panel, drawn by a first-party renderer (R7b, TASK-0084)

On 2026-10-27 the owner decided the path to the milestone "CAD on one part" (KD1 to KD6), and chose the binding
ImGui.NET, which has no question about a licence. ADR-0027 records KD1 and amends ADR-0026 item 1. Register entry
R-0021 is closed. The roadmap gets the rows R7b, R8a, R8b, K0a, K0b and K3a and rule 4 of its order, and the decision
sheet is `docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`.

**The change.**

- `3DEngine.Vulkan/Ui/`: neutral 2D draw data, the first sampled texture, descriptor set and 2D pipeline, and the
  shaders `ui.vert` and `ui.frag`.
- `3DEngine.Core/Input/` and `SdlInput`: the text sample, seven keys of text editing, and the Control key.
- `3DEngine/Ui/`: the router of the samples, the conversion of the draw data, and `DesktopUi` with the panel
  "Document": the address, the live bodies with their kind, a filter field and a Clear button, each control 44
  pixels high. ImGui.NET 1.91.6.1 has its only pin in `3DEngine/3DEngine.csproj`.
- `UiLibraryGateTests` refuses the namespace outside `3DEngine/Ui/` and the pin outside the host project.
- `THIRD-PARTY-NOTICES.md` section 5 gives the MIT texts of ImGui.NET, cimgui and Dear ImGui, and names its font.

**Proof.** 34 new tests, and each one failed under an injection. Run 40603870213 passed on the three runners, with
the native library on Windows, Linux and macOS arm64, and the image test of the panel on lavapipe. The desktop ran
under synchronization validation with no message. The owner accepted the pictures on 2026-10-28.

**What stays.** A failed check inside the native library stops the process; two such causes came up in the tests
and are corrected. The default font is coarse at 26 pixels. The touch path of the panel is proven by synthetic
samples only.

New tests: 34. The test list holds 574 (475 in `Engine.Tests`, 75 in `3DEngine.Tests`, 24 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.76, 2 product milestones and 10 entries
followed it.

## v0.87 — A body has a name and a visibility that commands change (R8a, TASK-0085)

The owner decided the defaults R8-Q1 to R8-Q8 on 2026-10-27 and accepted the contract of ADR-0028 on 2026-10-28,
because it changes the public shape of `Engine.Contracts`. ADR-0028 amends ADR-0012 §3 and §6 and ADR-0021 item 5.

**The change.**

- `BodyRecord` gains `Name` and `Visible`; `CommandHandlerResult` gains `ChangedBodies`. The bus refuses a
  wrong list and gives `body.changed { bodyId, name, visible }` after each `body.consumed` of a commit.
- The commands `RenameBody` and `SetBodyVisibility` and the query `GetBodyInfo`. A name has 1 to 128 UTF-16 units
  and no control character, and an empty name clears it; a refused name gives `E-VAL-BODY-NAME-INVALID`.
- `Translate` and `Subtract` give their result the name and the visibility of the source and of the minuend.
- `body.created`, the snapshot, `/schema/events` and `/schema/diagnostics` give the new fields and the new code.

**Proof.** 32 new tests, and six injections each failed their tests. The replay fixture renames and hides its box,
and two replays give the same names and visibilities. An injected host file that names "RenameBody" fails
`DispatchSurfaceGateTests`, so the exception of ADR-0028 item 9 does not reach the hosts.

**What stays.** The desktop does not show the name or the visibility yet; R8b does.

New tests: 32. The test list holds 606 (507 in `Engine.Tests`, 75 in `3DEngine.Tests`, 24 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: `E-VAL-BODY-NAME-INVALID`. Since the review of v0.76, 3 product milestones
and 11 entries followed it.

## v0.88 — The desktop selects, hides, shows and renames a body (R8b, TASK-0086)

The owner decided the defaults R8-Q6 to R8-Q8 and KD6 on 2026-10-27, and accepted the four pictures and the log on
2026-10-28. ADR-0029 amends ADR-0025 item 6, ADR-0026 items 2 and 3, and ADR-0027 item 7. No shape of
`Engine.Contracts` changes.

**The change.**

- The gestures long press (500 ms), right click, H, tap of two fingers, and Shift and H, with the intents
  `HideIntent` and `ShowAllIntent`.
- `3DEngine.Core/Picking/Picker.cs` gives the ray of a pixel and the nearest visible body that it hits.
- `BodyActions` keeps the selection, a state of the client, and sends `SetBodyVisibility` and `RenameBody` to the
  session. A hidden body leaves the scene, the pick and the framed box, and it cannot stay selected.
- A second pipeline (`highlight.frag`) draws the selected body again in one colour.
- The camera frames the bodies only when the visible set goes from empty to not empty (KD6).
- The panel rows select, and the selected body has a name field, Rename, and Hide or Show, beside Show all.

**Proof.** 30 new tests, and twelve injections each failed their tests. A touch script and a mouse script give the
same command log. The visual check ran under synchronization validation with no validation message and exit code 0.

**What stays.** No test with no GPU reads `SessionBodies`. The touch path has synthetic samples only, because no
computer of the project has a touch screen.

New tests: 30. The test list holds 636 (507 in `Engine.Tests`, 89 in `3DEngine.Tests`, 40 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.76, 4 product milestones and 12 entries
followed it.

## v0.89 — The codebase review of 2026-10-28 (governance, TASK-0087)

The review of 2026-10-17 examined v0.76, and 4 product milestones and 12 entries followed it, so this entry is the
tenth codebase review: `docs/reviews/2026-10-28-codebase-review.md`, at `main` `e0a2c98`.

**The state.** Each fix of the period held for the forms that it named: E66 to E71, V16 to V24, T55 to T61, and
E46. The measures: 11,494 production lines in 147 files, 636 tests, 0 warnings, and open findings 0 critical, 6
high, 26 medium and 32 low.

**The new findings.** Fourteen. One is high: a test framework of xUnit in a file that is not a gate file makes
each failed test pass, and the exact count stays (T62; I ran it). Four are medium: a click on the scene does not end
a text field, so Rename can rename the wrong body (V26); each frame costs time in the square of the count of bodies
(V27); a project under a `bin/` folder hides from two gates (T63); the gate of the UI library reads text (T64). V29
carries the touch path that no screen has checked, as the owner chose on 2026-10-27.

**The tasks.** TASK-0088 (P0.34) holds T62 and comes before P8a. TASK-0089 (P0.35) holds T63 to T67, and
TASK-0090 (P0.36) holds E72 to E74, V18, V26 to V28 and V30; both come after P8a. The register stays at 14 of 15.

**Questions for the owner.** Q1 to Q7 are the decisions of P8a: the identifier of a document at an open, the
identity of the backend (KD3), the length unit (KD4), a document with no name, the open in the desktop, save and
open by an agent, and the place of the code. Q2 and Q3 can each change the public shape of `Engine.Contracts`. Q8:
the research gives 5 to 7 evenings for P8a and the roadmap 3 to 5, so the review proposes a split into P8a1 and P8a2.

New tests: none. The test list holds 636. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero
warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review is due when more than four
product milestones or more than twelve entries follow v0.88.

## v0.90 — No file outside the gate files can replace the test framework of a test project (P0.34, TASK-0088)

The codebase review of 2026-10-28 found that a test framework of xUnit in a file that is not a gate file makes
each failed test pass, with the exact count of tests (T62, high). The owner answered the eight questions of the
review on 2026-10-28, A to each; the roadmap rows of P8a and of a new slice P8f hold the answers.

**The change.**

- `TestAttributeGateTests` refuses, in each test project, the attributes `TestFramework`, `TestCaseOrderer`,
  `TestCollectionOrderer` and the discoverers, each type that derives from the runner surface of xUnit, the
  namespace `Xunit.Sdk`, an alias that reaches them, and an extern alias.
- The same gate refuses a type with a name of xUnit in a product project, in the global namespace or in a namespace
  that a test namespace sees.
- The roadmap splits P8a: P8a holds the engine and the server, and P8f the desktop and the agent (Q8). The review
  gate reads a slice as a number and one letter, so the name is P8f and not P8a2.

**Proof.** 26 new tests. The file of T62 failed the gate program with six offences, and a type `Engine.Assert` in
`Engine.Core` failed it. Four injections each failed their cases.

**What stays.** The gate reads syntax and binds no name. The three gates that run engine code stay in the test
process (T31).

New tests: 26. The test list holds 662 (533 in `Engine.Tests`, 89 in `3DEngine.Tests`, 40 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.88, 0 product milestones and 2 entries
followed it.

## v0.91 — The document file in the engine and the server (P8a, TASK-0091)

The owner decided KD3 and KD4 on 2026-10-27, answered Q1 to Q8 of the codebase review of 2026-10-28 on 2026-10-28,
accepted the contract of ADR-0015 on 2026-10-28, and chose on 2026-10-28 that a server with no `--file` keeps the
document in memory. ADR-0015 is written again, and it amends ADR-0010 §2 and ADR-0012 §1.

**The change.**

- `Engine.Contracts` gains `BackendIdentity` and `LogEntry`. `Document.Log` holds entries, `Document.LengthUnit` is
  the millimetre, and `IGeometryBackend.Identity` has no default. The bus writes the identity of its backend into
  each entry, a replay of entries refuses another backend, and the snapshot gives `lengthUnit`.
- `Engine.Core/Persistence/`: an encoder for each handler, the codec of a line, the journal with one write and one
  flush to disk for each line, the open, and `DocumentSession.SaveAs`. The bus writes the entry after the handler
  and before the commit; a failed write rejects the command, and the session refuses each later command.
- `engine-api-http --file` opens or makes the file, and stops with exit code 6 when it does not open. With no
  `--file`, the server says that the document is in memory only.
- Seven codes of the namespace `IO`.

**Proof.** 31 new tests and thirteen injections. A process test kills the server after its results, and a second
server opens the file to the same renamed body. The full scene of each command opens again on the native backend
with the same version, log and bodies. A command with a file took 0.07 to 0.08 ms on this computer.

**What stays.** The desktop has no file until P8f. A failed write leaves the body of the handler in the backend. On
Linux and macOS the lock of the file is advisory.

New tests: 31. The test list holds 693 (564 in `Engine.Tests`, 89 in `3DEngine.Tests`, 40 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 13 of 15; R-0025 is closed. New diagnostic codes: `E-IO-BACKEND-MISMATCH`, `E-IO-NOT-ENCODABLE`,
`E-IO-WRITE-FAILED`, `E-IO-LOAD-FAILED`, `E-IO-SAVE-FAILED`, `W-IO-TRUNCATED`, `W-IO-BACKEND-VERSION`. Since the
review of v0.88, 1 product milestone and 3 entries followed it.

## v0.92 — The document file in the desktop and for an agent (P8f, TASK-0092)

The owner answered Q4 to Q6 of the codebase review of 2026-10-28, accepted ADR-0030 on 2026-10-28, and accepted the
two pictures and the log on 2026-10-28.

**The change.**

- `3DEngine --file` opens or makes the file before the window; with no `--file` the desktop writes into the folder
  of unsaved documents. Exit code 6 when the file does not open.
- One document folder, `<documents>/3DEngine`, for a save and an open by name; a name is a plain file name, and a
  save makes the folder.
- `GET /document`, `POST /document/save` and `POST /document/open`. Only the desktop serves the open;
  `engine-api-http` gives HTTP 501 with the new code `E-API-NOT-SUPPORTED`.
- An open checks the file in a scratch session with its own backend, then the desktop starts again with `--file` on
  the same port. A bad file leaves the window and gives its code.
- The panel shows the file, or "unsaved", with "File name", Save as and Open.

**Proof.** 26 new tests and nine injections. A process test starts the desktop program with a bad file and gets exit
code 6. The visual check, under synchronization validation, saved a box, refused a bad file, and opened the saved
file in a new process that rebuilt the same box.

**What stays.** No test runs the restart of the program. The folder of unsaved documents grows. An open applies each
command two times.

New tests: 26. The test list holds 719 (582 in `Engine.Tests`, 89 in `3DEngine.Tests`, 48 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 13 of 15. New diagnostic codes: `E-API-NOT-SUPPORTED`. Since the review of v0.88, 2 product milestones and
4 entries followed it.

## v0.93 — The hosts and the desktop of the review of 2026-10-28 (P0.36, TASK-0090)

After P8f the owner put P0.36 and then P0.35 before P8c on 2026-10-28, and accepted the new rule of a name in ADR-0028
item 3 on 2026-10-28.

**The change.**

- V26: a press on the scene ends the text field of the panel, so the next key goes to the scene and Rename sends the
  name of the selected body.
- V27: `BodyVisibility` gives the visibility by a lookup and keeps the count of visible bodies until a change.
- E72 and E73: a string field over HTTP takes only a well-formed JSON string; `BodyNames` refuses a lone surrogate
  with `E-VAL-BODY-NAME-INVALID`.
- E74: `engine-api-http` disposes its app at a normal stop and at each exception of the composition.
- V18: a retry of a remake gives no retired swapchain, and a failed swapchain frees its depth image.
- V28: an image test requires the highlight on 99 % of the pixels of the selected body; `mesh.vert` declares
  `invariant gl_Position`.
- V30: the setter of the pitch clamps to the range of `Orbit`.

**Proof.** 19 new tests. The two probes of V26 failed before the fix, and each other fix with a test failed under an
injection of its old rule. Run 40691527334 passed on the three runners.

**What stays.** E74 and the free of the depth image of V18 have no test.

New tests: 19. The test list holds 738 (591 in `Engine.Tests`, 94 in `3DEngine.Tests`, 53 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 13 of 15. New diagnostic codes: none. Since the review of v0.88, 2 product milestones and 5 entries
followed it.

## v0.94 — The gates of the review of 2026-10-28 (P0.35, TASK-0089)

The owner put P0.35 after P0.36 and before P8c on 2026-10-28.

**The change.**

- T63: each project reference must resolve to a project that the dependency gate reads, so a reference into `bin/`,
  `obj/` or outside the repository is refused.
- T64: the gate of the UI library reads the tokens of the compiler, each project file as XML and each file that
  MSBuild reads for a project; a string that names `cimgui` outside `3DEngine/Ui/` is an offence.
- T65: an archived ADR needs its file in `docs/adr/archive/` with the status Withdrawn or Superseded.
- T66: a product task after the last review whose roadmap row does not name it is a problem; the rule D3 stays.
- T67: the gate runner reads each gate file and refuses a discard of a call and a `Task.Run` as a statement.

**Proof.** 16 new tests. The form of each finding failed the gate program after its correction and had passed it
before.

**What stays.** The runner has no test of its own. T31 stays open in part: the three gates that run engine code stay
in the test process.

New tests: 16. The test list holds 754 (607 in `Engine.Tests`, 94 in `3DEngine.Tests`, 53 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 13 of 15. New diagnostic codes: none. Since the review of v0.88, 2 product milestones and 6 entries
followed it.

## v0.95 — Undo and redo as commands of the log (P8c, TASK-0093)

The owner answered Q1 B, Q2 A, Q3 A and Q4 A and accepted ADR-0031 on 2026-10-28.

**The change.**

- `Undo` and `Redo` are commands with no parameter. The log and the document file hold them, each adds one to the
  version, and the output `commandId` names the step. An open rebuilds the history from the file.
- The command bus records the body records that each step created, consumed and changed, and makes the lists of
  an undo and a redo itself. No backend call: no backend changes or frees a body. A new step clears the redo list.
- The contract gains `HistoryMove` and `CommandHandlerResult.History`, and the event kind `body.removed`. ADR-0031
  amends ADR-0006 (the open challenge of undo closes) and ADR-0021 items 1 and 5.
- New codes: `E-CMD-NOTHING-TO-UNDO` and `E-CMD-NOTHING-TO-REDO`.
- The desktop panel has the buttons Undo and Redo, and its projection removes a body at `body.removed`.

**Proof.** 13 injections: 12 were caught at once, and a schema with no `body.removed` failed after the payload test
applied undos. The visual check passed: a cut, an undo, and a redo whose picture is the picture of the cut. CI run
40720390838 passed on the three runners.

**What stays.** The keys Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z are register entry R-0041. The history has no limit, and
the backend keeps each body that an undo removes.

New tests: 16. The test list holds 770 (620 in `Engine.Tests`, 94 in `3DEngine.Tests`, 56 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: 2. Since the review of v0.88, 3 product milestones and 7 entries
followed it.

## v0.96 — The numeric pad, the evaluator and the keys of undo and redo (R10, TASK-0094)

The owner accepted ADR-0032 with Q1 A, Q2 A and Q3 A on 2026-10-28, and put the keys of R-0041 into this phase.

**The change.**

- `3DEngine.Core/Numeric/Expression.cs` evaluates numbers, `+ - * /`, a unary minus, parentheses, `pi` and
  `sqrt`, with the invariant culture. A command records the result number only, so a replay never evaluates.
- `NumericPad` holds three fields; a button and a typed character append the same text. The panel opens a pad
  for a new box and for a move of the selected body, and the moved body becomes the selection.
- While the pad is open it takes each key. Tab, Enter, Escape and Backspace edit it.
- `InputKey` gains Z and Y from the key code. `UndoIntent` and `RedoIntent` come from Control and Z, Control and
  Y, Control, Shift and Z, and the taps of three and four fingers. R-0041 closes.
- ADR-0032 amends ADR-0026 items 2 and 3 and ADR-0031 item 11.

**Proof.** 16 injections: 15 were caught at once, and the 16th after its test selected another field first. The
visual check passed: a box of 40, 30 and 2π mm, a move of 20 and -5 mm, then Ctrl+Z and Ctrl+Y. CI run
40741567326 passed on the three runners.

**What stays.** The drags of the camera have no numeric equivalent; the gizmo of R9 brings the first drag that
changes the document. No touch screen exists, so the taps of three and four fingers have synthetic proof only.

New tests: 59. The test list holds 829 (620 in `Engine.Tests`, 144 in `3DEngine.Tests`, 65 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 13 of 15. New diagnostic codes: none. Since the review of v0.88, 4 product milestones and 8 entries
followed it.

## v0.97 — The second codebase review of 2026-10-28 (governance, TASK-0095)

The review of 2026-10-28 examined v0.88, and 4 product milestones and 8 entries followed it, so K2 would make the
gate fail. This entry is the eleventh codebase review: `docs/reviews/2026-10-28-b-codebase-review.md`, at `main`
`c0fbb64`.

**The state.** The fixes of the period held for the forms that they named: E72 to E74, V18, V26 to V28, V30, and
T62 to T67. The measures: 13,749 production lines in 165 files, 829 tests, 0 warnings, and open findings 0
critical, 8 high, 29 medium and 44 low.

**The new findings.** Thirty-one. Three are high. An open of the document file skips an entry whose command
identifier the cache holds, so the Document is wrong with no error (E75; I ran it). A test framework in a `bin/`
folder inside a test project (T68; I ran it) and an alias in the project file of `3DEngine.Tests` (T69) each make
each failed test pass with the exact count. Seven are medium: E76, E77, V31, V32, T70, T71 and T72.

**The run of v0.95 on `main` (T76).** Run 40725490160 on `307ab67` failed only because no macOS runner took the job.
The tree of `307ab67` equals the tip of its branch, and run 40745912217 on `c0fbb64` passed on the three runners.

**The tasks.** TASK-0096 (P0.37) holds E75 and TASK-0097 (P0.38) holds T68 and T69; both come before K2.
TASK-0098 (P0.39) holds T70 to T75 and T77, and TASK-0099 (P0.40) holds E76 to E85 and V31 to V39; both come after
K2. R-0042 holds the ten questions of the review: nine for K2 and K0a, and one for the rules review (T78).

New tests: none. The test list holds 829. `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero
warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review is due when more than four
product milestones or more than twelve entries follow v0.96.

## v0.98 — An open applies each entry of the document file one time (P0.37, TASK-0096)

The owner answered the ten questions of the second codebase review of 2026-10-28 on 2026-10-28, A to each. The first
commit of this task records the answers: K2 splits into K2a (the engine model) and K2b (the tools of the desktop) on
the path to the milestone, the rest of K2 comes after it, the extrude of K0a records the identifier of its sketch,
and TASK-0098 adds the rule measures of Q10 to the review gate. R-0042 closes.

**The change.**

- E75: an open applies each entry with no look-up in the idempotency cache, as a replay does, and each result still
  goes into the cache. The version of each result must equal the version of its entry.
- An open refuses a session that has a command, with `E-IO-LOAD-FAILED`.
- ADR-0015 item 8 says so, and the comment of `StepHistory` is true again.

**Proof.** Two new tests. The probe of the review is a test: a command identifier that comes again after 1,024 other
results, then an open, a command and a second open. Three injections each failed a test: the look-up in the session
(through the check of the version: "The entry of version 4 gave the version 2"), the look-up in the bus, and no check
of a new session. CI run 40758728721 passed on the three runners.

**What stays.** The live bus still applies a command identifier again after the cache forgot it (E17, R-0028); the
file now holds such a command as the live session applied it.

New tests: 2. The test list holds 831 (622 in `Engine.Tests`, 144 in `3DEngine.Tests`, 65 in `3DEngine.ImageTests`).
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 13 of 15. New
diagnostic codes: none. Since the review of v0.96, 0 product milestones and 1 entry followed it.

## v0.99 — The test framework gate reads what MSBuild compiles into a test project (P0.38, TASK-0097)

**The change.**

- T68: `TestAttributeGateTests` skips `bin/` and `obj/` at the root of a test project only, as MSBuild does, so a
  source in a `bin/` folder below the root is read.
- T69: the gate reads the project file of each test project as XML. It refuses each import of a name of xUnit other
  than the namespace `Xunit`, an alias with a name of xUnit, a `Compile` item and an `Import`, with no regard to case
  and with `%XX` unescaped.
- The judge names the project files of `3DEngine.Tests` and `3DEngine.ImageTests` as gate files, and
  `docs/templates.md` says so.

**Proof.** 20 new tests. The file of T68 in `Engine.Tests/Http/bin/` and the alias of T69 in
`3DEngine.Tests/3DEngine.Tests.csproj` each failed the gate program ("200 runs of 104 checks, 2 failed"); the review
had seen both pass. Three injections each failed their tests: a skip of `bin/` at each depth, no check of a `Using`
item, and a judge with no project file of `3DEngine.Tests`. CI run 40759954421 passed on the three runners.

**What stays.** The three gates that run engine code stay in the test process (T31). Each other user of the same skip
of `bin/` waits for TASK-0098.

New tests: 20. The test list holds 851 (642 in `Engine.Tests`, 144 in `3DEngine.Tests`, 65 in `3DEngine.ImageTests`).
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 13 of 15. New
diagnostic codes: none. Since the review of v0.96, 0 product milestones and 2 entries followed it.

## v0.100 — The engine holds a sketch, and the desktop draws it (K2a, TASK-0100)

The owner accepted ADR-0033 on 2026-10-28 with no change, and accepted the picture of the visual check on 2026-10-28.

**The change.**

- `Engine.Contracts/Sketch/`: `SketchRecord` with its points, lines, circles, arcs and constraints, and the three
  origin planes with fixed identifiers (`Datums`). `Document.Sketches`, and `CreatedSketches` and `ChangedSketches`
  on `CommandHandlerResult`. ADR-0033 amends ADR-0012, ADR-0021 and ADR-0031.
- Seven commands: `CreateSketch`, `AddSketchPoint`, `AddSketchLine`, `AddSketchConstraint`,
  `SetSketchConstraintValue`, `MoveSketchPoint` and `DeleteSketchItem`, for the constraint kinds coincident,
  horizontal, vertical, distance and fixed. The query `GetSketch` gives the residual of each constraint, computed
  with `+ - * / sqrt` only.
- The events `sketch.created`, `sketch.changed` and `sketch.removed` after the body events of a commit, and
  `sketches` in the snapshot. Each step of the history records the sketch before and after, so undo and redo hold
  sketches, also after an open of the file.
- Six codes of the new namespace `SKETCH`.
- The desktop draws each live sketch in yellow over the scene, read only (`SessionSketches`, `SketchOverlay`).

**Proof.** 19 new tests and 11 injections, each caught. The replay gate holds a sketch with the bits of each
position. The visual check ran the desktop on 127.0.0.1 with a box, a rectangle on XY and a line on XZ made over
HTTP. CI run 40764792495 passed on the three runners.

**What stays.** The tools of the desktop are K2b. The circle, the arc and nine kinds of constraint wait for phase K2.
One of four captures of the desktop showed no sketch, and the cause is not known.

**A correction.** v0.98 and v0.99 gave 1 and 2 entries since the review of v0.96; the review entry v0.97 counts too,
so the counts were 2 and 3.

New tests: 19. The test list holds 870 (653 in `Engine.Tests`, 144 in `3DEngine.Tests`, 73 in `3DEngine.ImageTests`).
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 13 of 15. New
diagnostic codes: `E-SKETCH-NOT-FOUND`, `E-SKETCH-DATUM-NOT-FOUND`, `E-SKETCH-ITEM-NOT-FOUND`,
`E-SKETCH-LINE-INVALID`, `E-SKETCH-CONSTRAINT-INVALID`, `E-SKETCH-NAME-INVALID`. Since the review of v0.96, 1 product
milestone and 4 entries followed it.

## v0.101 — The desktop draws a sketch of lines and adds its constraints, by touch and by mouse (K2b, TASK-0101)

The owner accepted ADR-0034 on 2026-10-28 with Q1 to Q6 A, and accepted the picture of the visual check on 2026-10-28.

**The change.**

- The intent layer has the mode `Sketch` (`IntentMode`), with a second map of the settings: a tap or a click gives
  `SketchPointIntent`, and the gestures of a hide and of a show all give nothing. ADR-0034 amends ADR-0026, ADR-0029
  and ADR-0032.
- `Picker.OnPlane` gives the position of a ray on the plane of a datum with `+ - * /` only. `SketchGrid` rounds a tap
  to a step of 1, 2 or 5 × 10ᵏ mm of at least 8 pixels. `SketchCamera` turns the camera to the plane of a new active
  sketch.
- `SketchTool` in `3DEngine.Core/Sketching/`: the tool Line makes a chain that closes on its first point, and the
  tool Select picks a point or a line, two items at most. `SketchActions` sends the commands of K2a.
- The panel gets the buttons of a new sketch on XY, XZ and YZ. The window "Sketch" lists each point, line and
  constraint with its residual, and gives the buttons of the five kinds that fit the selection, Move point, Value and
  Delete. The pad holds one to three fields and gives the next point of a chain.
- The overlay draws the active sketch in yellow with labels, a selected item in white, the first point of an open
  chain with a ring, and each other sketch in grey.

**Proof.** 58 new test cases and 17 injections, each caught. A touch script and a mouse script that draw one closed
rectangle give the same log. The visual check ran the desktop on 127.0.0.1 with real clicks: a rectangle of five
clicks on XY, and the five kinds from the window. It found two faults of display, which the last commit of the task
corrects. CI run 40775886063 passed on the three runners.

**What stays.** The solver is K3a: a constraint shows its residual, and no point moves. A tap that adds a point and a
line gives two steps of undo. A drag of a point and a snap to a grid wait for R11. The first click into the window can
reach the panel at a wrong place (R-0043).

**A correction.** None.

New tests: 58. The test list holds 928 (653 in `Engine.Tests`, 185 in `3DEngine.Tests`, 90 in `3DEngine.ImageTests`).
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New
diagnostic codes: none. Since the review of v0.96, 2 product milestones and 5 entries followed it.

## v0.102 — The document file keeps each complete line and each held file, and the pad applies to the current selection (P0.40, TASK-0099)

The owner put P0.40 before K3a and before P0.39 on 2026-10-28, and answered the questions Q1 to Q7 of TASK-0099 on
2026-10-29 with A to each.

**The change.** Findings E76 to E85 and V31 to V39 of the second codebase review of 2026-10-28.

- The open of a document file: only a tail with no line feed, or a last line that is not JSON, is a torn line (E76).
  The reader refuses a number that is not finite (E78). The open holds the file with the access and the share of the
  journal before it applies an entry (E83). A file of `--file` that cannot be made, and an exception of a handler
  during an open, give `E-IO-LOAD-FAILED` (E82). A failed first open and a failed open after a move break the
  journal, and a failed cut says that the file can keep the line (E84).
- The hosts: a start that makes the file of `--file` says so on the error output (E77). A document folder with a
  final separator works (E79). A name that is not well-formed text gives HTTP 400 (E80). A name follows the rules of
  a file name of Windows on each operating system (E81). A save on a host with no file gives HTTP 501 (E85).
- The open in the desktop: the check holds the file as a journal does, except the file that the desktop holds (V31).
  After an accepted open, each command, save and open gets HTTP 503 and `E-API-OPEN-PENDING` (V37).
- The pad: it closes when the selection that it names changes (V32), the text fields are disabled while it is open
  (V33), a second apply waits for the end of the pad (V34), and it stays inside a tablet display (V35).
- A tap of two fingers counts the most contacts down at one time (V36). The yaw and the target of the camera refuse a
  value that is not finite (V38). One `try` covers the constructor of the swapchain (V39).
- ADR-0015 items 8 and 10, ADR-0030 items 2, 4 and 5, and ADR-0032 items 4 and 6 record the answers of the owner.

**Proof.** 51 new test cases and 3 changed. 35 injections, each caught by a test; one more injection showed a
redundant check, which went out. The visual check ran the desktop on 127.0.0.1: the New box pad opened inside the window, the
file name and the filter were disabled, and the log held no warning with validation on. CI run 40786895807 passed on the three runners.

**What stays.** The lock of a file is real on Windows only: on Linux and macOS .NET takes no lock for a write with the
share of a read, so the tests of E83 and V31 assert on Windows only. ADR-0015 item 13 calls that lock "advisory"; no run
verified the behaviour of .NET on Linux and macOS. A file that changes in the few milliseconds between
the check and the start of the new program can still stop the desktop (ADR-0030, Consequences). V39 is confirmed by
reading only. The branch `p040-file-and-pad-gaps` on origin holds a commit that the write-set check refuses;
`main` takes the branch `p040-file-and-pad-gaps-2`, which holds the same work with no such commit. The synthetic clicks of the visual check hit a wrong place two times before they worked (R-0043).

**A correction.** None.

New tests: 51. The test list holds 979 (689 in `Engine.Tests`, 190 in `3DEngine.Tests`, 100 in `3DEngine.ImageTests`).
`dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open entries: 14 of 15. New
diagnostic code: `E-API-OPEN-PENDING`. Since the review of v0.96, 2 product milestones and 6 entries followed it.

## v0.103 — The engine solves the five constraints of a sketch, and the desktop shows if a sketch is solved (K3a, TASK-0102)

The owner put K3a before P0.39 on 2026-10-29, accepted ADR-0035 with Q1 to Q4 A, and accepted the pictures of the
visual check on 2026-10-29.

**The change.**

- `Engine.Core/Sketch/SketchSolver.cs` is a Newton iteration with the step of minimum norm: it solves (J·Jᵀ)·λ = r
  by Gaussian elimination with partial pivoting, and drops a row with a pivot near zero. It uses `+ - * / sqrt`
  only, in one fixed order. The tolerance is 1e-9 and the limit is 50 steps.
- Each sketch command gives its record through the solver in `SketchResults.Changed`, so the Document holds the
  solved positions and the log holds the inputs only. A set with no solution keeps the positions of the command.
- `MoveSketchPoint` holds its point when a solution exists. No field and no schema version of a command changes.
- `GetSketch` gives `solved` and `freedom`. The window "Sketch" shows them in one line, for example "Solved, 2 free".
- ADR-0035 amends ADR-0033 items 4 and 7.

**Proof.** 15 new test cases. A test compares the bits of a fixed solve with constants: CI run 40836376649 gave the
same bits on Windows x64, Linux x64 and macOS arm64. A file that the server of v0.101 wrote, with a constraint that
its positions do not satisfy, opens and solves. 14 injections: 11 were caught at once, and 3 after two more tests.
The visual check ran the desktop on 127.0.0.1 with validation on: a rough rectangle became exact, a conflict showed
"Not solved", and Ctrl+Z gave "Solved, 0 free". The log held no warning.

**What stays.** The engine does not name the constraint that causes a conflict. The dense solve costs the cube of
the number of equations. The kinds 6 to 14 wait for phase K3. Four tests of K2a and K2b expected an unsolved
residual; they now expect a solved record or a conflict. On this computer `UiLibraryGateTests` also reads the
worktrees of other sessions under `.claude/worktrees` and fails there; the pipeline has no such folder (P0.39).

**A correction.** The list "Shipped" of `docs/roadmap.md` stopped at R10. This task adds P0.37, P0.38, K2a, K2b
and P0.40.

New tests: 15. The test list holds 994 (703 in `Engine.Tests`, 190 in `3DEngine.Tests`, 101 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.96, 3 product milestones and 7 entries
followed it.

## v0.104 — The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading (P0.39, TASK-0098)

The owner put P0.39 after K3a on 2026-10-29. The task holds the findings T70 to T75, T77 and T78 of the second
codebase review of 2026-10-28.

**The change.**

- T70: `UiLibraryGateTests` refuses a global using of `ImGuiNET` in each file, the UI folder too, and reads the
  value of a join, an interpolation and a constant of the file in a native import.
- T71: the UI gate unescapes `%XX` before it compares a package, and `DependencyDirectionGateTests` refuses an
  Include with an MSBuild escape.
- T72: each assembly name is unique and is not the name of another project, and only a project file sets it. The
  four programs `engine`, `engine-api-http`, `repo-gates` and `write-set-check` keep their names.
- T73: the review counter counts each heading, takes the product phase from the row of the roadmap that names a
  task, and refuses a version that does not rise, a heading after the last review with two tasks, and a task file
  whose phase is not the phase of its row.
- T74: the gate runner binds each gate file with the compiler and refuses each statement whose value is a task, and
  each local of a task type that the check never reads. It checks its own forms at each run.
- T75: an archived ADR keeps its identifier, its title and its sections "## Context" and "## Decision".
- T77: the NUL byte of `3DEngine.Tests/SdlInputTests.cs` is the escape `\0`, and a gate refuses a control byte in a
  `.cs` file.
- T78: a review with the ledger v0.104 or later gives the three measures of the rules review. `docs/templates.md`,
  sections 6 and 7, say so.
- `DependencyDirectionGateTests` and `DispatchSurfaceGateTests` leave out bin/ and obj/ at the root of a project
  only. `UiLibraryGateTests` skips `.claude/`, so the full test list passes on the computer of the owner.

**Proof.** 42 new test cases. Eight new forms of T70 and T71 failed their tests on the gate before the change. A
probe checkout held each form of T70 to T77: the gate program of main gave 0 failures, and the program of this
branch gave 8 failed checks, one or more for each form. With a type check that sees no task, the runner fails on
the five forms of T74.

**What stays.** A constant of another file in a native import still passes the UI gate. The control byte gate
reads `.cs` files only; the task file of this task held a NUL byte for one commit (ab22ade), and no gate saw it.
The inventory of the rules stays a separate review when the owner asks for it.

**A correction.** None.

New tests: 42. The test list holds 1036 (745 in `Engine.Tests`, 190 in `3DEngine.Tests`, 101 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.96, 3 product milestones and 8 entries
followed it.

## v0.105 — The engine extrudes the closed profile of a solved sketch over Manifold, and the desktop extrudes the active sketch (K0a, TASK-0103)

The owner put K0a after P0.39 on 2026-10-29, accepted ADR-0036 with Q1 to Q4 A, and accepted the pictures of the
visual check on 2026-10-29.

**The change.**

- `Engine.Contracts/Geometry` gains the capability `IExtrudeOps`, the value type `ExtrudeParameters` and the flag
  `BackendCapabilities.Extrude`. The managed stub does not implement them, so it gives `E-GEOM-CAP-MISSING`.
- `ManifoldGeometryBackend` makes one simple polygon of the loop, extrudes it along +Z with no slice and no twist,
  and moves it into the frame of the datum with `manifold_transform`. The frame holds 0 and 1 only.
- `Engine.Core/Sketch/SketchProfile.cs` finds the one closed loop of the lines with `+ - * /` and comparisons
  only, and orders it counter-clockwise.
- The command `Extrude` records `sketchId` and a signed `distance`, and gives `bodyId`. A profile that is not one
  closed loop, and a sketch that is not solved, give the new code `E-SKETCH-PROFILE-INVALID` with the reason.
  The sketch stays live and does not change.
- The window "Sketch" has the button "Extrude" with a pad of one field, `distance`. A success ends the sketch mode
  and selects the new body.

**Proof.** 22 new test cases. A test loads the 8 native functions of the extrude: CI runs 40865119918 and
40866036929 passed it on Windows x64, Linux x64 and macOS arm64, so KD5 is closed. A rectangle of 10 by 6 on each
origin plane, extruded by 5 and by -5, gives the volume 300 and the box of its frame. An open file replays the
solid, and an undo removes it. 22 injections: each one failed a test. The visual check ran the desktop on
127.0.0.1: a constrained rectangle extruded by 5 gave the box (2.25, 1.1, 0) to (12.25, 7.1, 5), and an undo
removed it. The log held no warning.

**What stays.** One loop only: a hole, an arc and a profile of selected lines wait. An edit of the sketch after the
extrude leaves the solid as it was until K0b. A point very near a line, and not on it, passes the check of a
touch, because the check compares with zero.

**A correction.** The comment of `Engine.Geometry.Manifold/Native/ManifoldNative.cs` gave "twelve symbols" for the
imports; it held more since TASK-0028. It now names the operations and no count.

New tests: 22. The test list holds 1058 (764 in `Engine.Tests`, 190 in `3DEngine.Tests`, 104 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: `E-SKETCH-PROFILE-INVALID`. Since the review of v0.96, 4 product
milestones and 9 entries followed it.

## v0.106 — The codebase review of 2026-10-29 (governance, TASK-0104)

The owner chose on 2026-10-29 to do the review before K0b. The review examined `main` at `730e508`, the merge
of v0.105.

**The change.** `docs/reviews/2026-10-29-codebase-review.md`, the twelfth codebase review. It gives the state of
each of the 204 findings of the last review, 23 new findings, the research for K0b and six questions for the
owner. Three tasks hold the new findings: TASK-0105 (P0.41) holds T79 before K0b, and TASK-0106 (P0.42) and
TASK-0107 (P0.43) hold the rest after K0b. Register entry R-0044 holds the questions. No code, test or gate
changes.

**Proof.** Four review agents worked in their own worktrees at `730e508`. I read each cited line on disk, and a
script printed each cited line of the code for that reading. I ran E86 again with a probe test ("again: Applied v6;
lines 2, with the id 2"), and T79 with a build, the gate program and a control. The review gate passes with twelve
reviews.

**What stays.** The new findings: 1 high (T79), 6 medium (E86, E87, V40, V41, T80, T81) and 16 low. Open findings
of each review: 0 critical, 6 high, 28 medium and 40 low. The review did not repeat the runs of the agents except
E86 and T79.

**A correction.** The run 40839708477 of the merge of v0.104 on `main` (`8fe8d0b`) is red: the job "Gate on
windows-latest" failed in `HostGuardTests.A_Host_With_A_File_Keeps_Each_Applied_Command_After_A_Kill`, with "The
process cannot access the file ... because it is being used by another process". The branch run 40839163079 on
`f8fae97` was green, and the ledger of v0.104 cites no run. I started TASK-0103 without reading the run on
`main` (finding T87). The run 40869048102 on `730e508` passed. TASK-0107 makes the test wait for its file.

New tests: none. The test list holds 1058 (764 in `Engine.Tests`, 190 in `3DEngine.Tests`, 104 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.105, 0 product milestones and 1 entry followed
it.

## v0.107 — The test framework gate reads the output path and each MSBuild file of a test project (P0.41, TASK-0105)

The codebase review of 2026-10-29 put P0.41 before K0b, because it holds the high finding T79.

**The change.**

- `TestAttributeGateTests` reads each MSBuild file of a test project with
  `DependencyDirectionGateTests.FilesThatMsBuildReads`: the project file, its `.user` file and each
  `Directory.Build` file above it. Until now it read the project file only.
- The gate refuses nine properties that move the output folders or change the default items, such as
  `BaseOutputPath`, `OutputPath` and `DefaultItemExcludes`. So the folders that `IsCompiled` leaves out are the
  folders that MSBuild leaves out.
- `docs/templates.md`, section 5, gives the new files and offences of the gate.

**Proof.** 7 new test cases. In the real checkout, `<BaseOutputPath>` with a framework attribute in
`3DEngine.Tests/bin/`, and an alias of `Xunit.Sdk` in `3DEngine.Tests/3DEngine.Tests.csproj.user`, each failed the
gate program. Each old rule, put back, failed a test: the project file only failed
`The_User_File_Of_A_Test_Project_Is_Read`, and no check of the properties failed the rows of T79.

**What stays.** A property that the list does not name is a new form. The review recommends to read the evaluation
of MSBuild. T80 and T81 wait for TASK-0107.

**A correction.** None.

New tests: 7. The test list holds 1065 (771 in `Engine.Tests`, 190 in `3DEngine.Tests`, 104 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.105, 0 product milestones and 2 entries
followed it.

## v0.108 — An edit of a sketch rebuilds each live extrude of it, and a broken profile gives a fault on the body (K0b, TASK-0108)

The owner answered Q1 to Q6 of the codebase review of 2026-10-29 with A on 2026-10-29, and accepted ADR-0037 with no
change. K0b is the engine and HTTP part; K0c is the desktop.

**The change.**

- `BodyRecord` gains `Geometry`, the key of its solid in the backend, and `Fault`, a `BodyFault(Code, Message)`.
  Both are optional, so no saved file changes. `GeometryKey` gives `Geometry ?? Handle.Id`.
- `Translate`, `Subtract`, `GetBoundingBox` and `GetTessellation` read a solid by `GeometryKey`. `Extrude` stores its
  solid under `GeometryKeys.Derive`, a version 5 guid of RFC 9562, and never under its handle.
- Each sketch command rebuilds each extrude of its sketch that is in effect in the log and live, in the order of the
  log, under a new key (`Engine.Core/Sketch/SketchRebuild.cs`). An edit that does not change the solid changes no
  body. `ExtrudeProfile` gives the parameters to `Extrude` and to the rebuild.
- A broken profile, a sketch that is not solved, or a failure of the backend keeps the last solid, and the body gets
  the fault `W-SKETCH-REBUILD-FAILED`. A consumed extrude gives `W-SKETCH-EXTRUDE-CONSUMED`. Both are new codes.
- `body.created`, `body.changed` and the snapshot of a subscription give `geometry` and `fault`.
- ADR-0037 amends ADR-0012, ADR-0021, ADR-0028, ADR-0031 and ADR-0036.

**Proof.** 12 new test cases: 11 in `Engine.Tests/Sketch/RebuildTests.cs` and one over HTTP and the WebSocket. The
test of the replay in `ExtrudeTests` turns round: the move of a corner now changes the box, the open gives the
rebuilt solid with the volume 735, and the first undo gives back the first box. The key test holds a constant that a
computation in Node with no shared code also gives. 16 injections: each one failed a test.

**What stays.** The desktop draws the first mesh after a rebuild until K0c. A consumed extrude does not follow until
K0. Each sketch command walks the whole log. The backend keeps one more solid for each rebuild (ADR-0031).

**A correction.** None.

New tests: 12. The test list holds 1077 (783 in `Engine.Tests`, 190 in `3DEngine.Tests`, 104 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: `W-SKETCH-REBUILD-FAILED`, `W-SKETCH-EXTRUDE-CONSUMED`. Since the review of
v0.105, 1 product milestone and 3 entries followed it.

## v0.109 — The desktop draws the rebuilt solid of an extrude and shows the fault of a body (K0c, TASK-0109)

K0c is the desktop part of the last step of the milestone "CAD on one part" (Q6 A of the codebase review of
2026-10-29). The owner accepted the pictures of the visual check on 2026-10-29. With K0c the milestone is complete:
a sketch with constraints, an extrude, and a solid that follows an edit of the sketch.

**The change.**

- `3DEngine/SessionBodies.cs` keeps the geometry key of the mesh of each live body. A `body.changed` with another
  key gives another solid, after a rebuild or an undo, so the desktop fetches the mesh again and replaces the
  entity. `BodyVisibility.RemoveMesh` keeps the visibility of the body while its mesh changes.
- `LiveBody` gains the fault of the body. The row of a body with a fault ends with "(fault)", and the selected body
  gives the reason.

**Proof.** 2 new test cases: an image test on the GPU with the validation layer follows a move, a broken profile and
two undos, with one entity in the scene at each step; a panel test checks the row and the reason. 6 injections: each
one failed a test. The visual check ran the desktop on 127.0.0.1: a rectangle extruded by 5, Edit and Move point of
P3, Delete of L3 with the fault in the panel, and two undos. The log held no warning.

**What stays.** The reason of a fault names the body and the point by their full identifiers, so it is long in the
panel. The pads of the sketch keep V40 and V41 until TASK-0106. A consumed extrude does not follow until K0.

**A correction.** None.

New tests: 2. The test list holds 1079 (783 in `Engine.Tests`, 190 in `3DEngine.Tests`, 106 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.105, 2 product milestones and 4 entries
followed it.

## v0.110 — The sketch refuses an identifier that it holds, and the pads of the sketch follow the state that they act on (P0.42, TASK-0106)

The codebase review of 2026-10-29 put P0.42 after K0b. The owner said yes to it on 2026-10-29, after K0c.

**The change.**

- E86: `CreateSketch`, `AddSketchPoint`, `AddSketchLine` and `AddSketchConstraint` refuse a command whose identifier
  the log holds, with the new code `E-SKETCH-ID-IN-USE`. A resend after the cache forgets a command gave a second
  item with one identifier, or an exception of the solver.
- E87: `SketchSolver.SolveWithHold` says when the solver dropped the hold, and a move gives the new warning
  `W-SKETCH-HOLD-DROPPED`. The comment of `SketchResults` agrees with the code.
- E88: a residual that is not finite is null in `GetSketch`, so the HTTP surface gives no status 500.
- E91: the lists of a record that the solver or a delete builds are read-only.
- E92: a line of length zero gives its own reason. E93: COM0, LPT0, the superscript ports, CONIN$ and CONOUT$ are
  reserved names.
- E89 and E90: the measured limits are History lines of ADR-0035: in a Release build a move takes 3 ms for 25 points,
  99 ms for 100 and 752 ms for 200, and a sketch more than about 10⁶ units from the origin does not solve. ADR-0036
  records that no smallest distance exists.
- V40 and V41: the Next point pad has the scope of the active sketch, the tool and the chain, and the Extrude pad the
  scope of the active sketch; each closes when its scope changes.
- V42: a pad of the sketch opens below the row of End and Close on a narrow display. V43: a selection with the origin
  gives no Fixed, Move point or Delete. V44: the desktop reads each changed sketch one time. V45: the overlay drops a
  pixel that is not finite.

**Proof.** 22 new test cases: 6 in `SketchGapTests`, 6 rows of reserved names, and 10 in `SketchGapUiTests`. 15
injections: each one failed a test. Four tests change with the new rules: three of the pads and the buttons, and the
residual of five tests is nullable.

**What stays.** The solve of a long chain stays in the serial section (E89). E87 warns and does not damp the step.
The reason of a fault names items by their full identifiers. The cache that forgets an identifier (E17) stays.

**A correction.** None.

New tests: 22. The test list holds 1101 (795 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: `E-SKETCH-ID-IN-USE`, `W-SKETCH-HOLD-DROPPED`. Since the review of v0.105, 2
product milestones and 5 entries followed it.

## v0.111 — The gates read the target name, the root folders and each text file, and a host test waits for its file (P0.43, TASK-0107)

The codebase review of 2026-10-29 put P0.43 after K0b. The owner said yes to it on 2026-10-30.

**The red run of v0.110.** The run 40910981448 on `main` at `13b4a01` failed. The jobs on Windows and Ubuntu passed,
and GitHub cancelled the job on macOS because no runner took it ("not acquired by Runner of type hosted even after
multiple attempts"). The tree of `13b4a01` is the tree of the branch tip `10eb878`, whose run 40910430159 passed on
the three runners. The cause is the infrastructure and not the code, as in T76.

**The change.**

- T80: no MSBuild file sets `TargetName`, which gives the built file and the assembly the name `Engine.Tests`.
- T81: the diagnostics scanner and the check of the runtime identifier leave out `bin/` and `obj/` at the root of a
  project only. The scanner also reads the value of each constant expression, so `"E-SKETCH-" + "HIDDEN"` is a code.
- T82: the UI gate takes the value of each constant string from the compiler. A raw string, `nameof`, an escape and
  a constant of another file name `cimgui` too. The new helper `Engine.Tests/Governance/ConstantStrings.cs` serves
  both gates.
- T83: the gate runner waits after each check until the thread pool and the timers of the check are idle, collects
  each task that nobody holds, and fails the check when a task of it failed with no reader. It runs on the main
  thread, and it checks its eight forms at each run.
- T84: the review counter counts an entry whose heading names a product phase, and the heading must give the phase
  of the task file. `docs/templates.md`, section 7, says so.
- T85: an archived ADR holds a line of text under "## Context" and under "## Decision".
- T86: the control byte gate reads each file that git tracks, except four binary types, and refuses U+202A to
  U+202E and U+2066 to U+2069.
- T87: `HostGuardTests` reads the file of the killed host for up to 10 s, until the process releases it.

**Proof.** 19 new test cases. 14 injections: each one failed a test or the gate program. Ten put back an old rule,
two removed a part of the wait of the runner, and two put the forms of T86 into the repository. One test changes
with the new rule of T84: five entries of a phase whose row names another task are now a due review.

**What stays.** T83 does not see a task that a static field holds. The two constant scans cost about 5 s. A
`Directory.Build.rsp` file can set `TargetName` for each project; a probe build gave `Engine.Tests.dll`, no such
file is in the repository, and R-0031 records it. T31 stays open in part.

**A correction.** The write set of TASK-0107 named its gate files with the patterns `Engine.Tests/Governance/**`,
`Engine.Tests/Diagnostics/**` and `eng/repo-gates/**`. The judge permits a gate file by its exact name only, so the
task now names each of the fifteen files.

New tests: 19. The test list holds 1120 (814 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.105, 2 product milestones and 6 entries followed
it.

## v0.112 — The engine solves pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle (K2c, TASK-0110)

The owner approved the plan of 2026-10-30 with the answers A to Q1 to Q4 and KQ1 to KQ4. K2c is its first item.

**The change.**

- `AddSketchConstraint` accepts seven more kinds of ADR-0033 item 3: `pointOn` (a point and a line), `midpoint`,
  `parallel`, `perpendicular`, `equal` (two lines), `symmetric` and `angle`. A point of `pointOn` or `midpoint`
  that is an end of its line, one line twice, one point twice in `symmetric`, and a pair of an angle that is not a
  unit pair each give `E-SKETCH-CONSTRAINT-INVALID`. So does a point of `symmetric` that is an end of its line. `tangent` and `radius` wait for K2d.
- `AddSketchConstraint` and `SetSketchConstraintValue` record the pair of an angle divided by its length.
- The solver gives each kind its equations, with derivatives by formula. The equations of `parallel`,
  `perpendicular` and `angle` use the two unit directions only, so no solve shrinks a line to one point. The
  equation of an angle is zero only in the direction of the angle, and it is not finite in the opposite direction.
- `GetSketch` gives the residual of each new kind. The History lines of ADR-0033 and ADR-0035 give the equations,
  under the answer Q2 A.

**Proof.** 25 new test cases in `Engine.Tests/Sketch/LineConstraintTests.cs`: each kind solves a rough sketch with
its free degrees, each derivative agrees with a central difference, a fixed solve of the seven kinds gives constant
bits, an angle keeps its branch, no solve from an exact start shrinks a line, each wrong form is refused, and the
pair of an angle is recorded as a unit pair. Each injection failed a test. One test of `SketchModelTests` now sends `tangent` in place of `parallel` for a
kind that waits.

**What stays.** The desktop panel of constraints gives the five kinds of K2a. The circle and the arc wait for K2d.

**A correction.** The first push of the branch (`5d1492e`) passed its run on the three runners, and it was not
merged. The engine probe of the codebase review of 2026-10-30 then found that `parallel`, `perpendicular` and
`angle` took a line to one point from an exact start and reported the sketch as solved, two high findings, and that
`symmetric` accepted an end of its line. The branch corrects them before the merge, and the review records them as
fixed. The first `governed-by` of TASK-0110 named 0033 and 0035 only, and the gate added seven ADRs whose
`affects` holds a file of the write set.

New tests: 25. The test list holds 1145 (839 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.105, 3 product milestones and 7 entries followed
it.

## v0.113 — The codebase review of 2026-10-30 (governance, TASK-0111)

The owner approved the plan of 2026-10-30 (answer Q1 A), which puts the thirteenth codebase review after K2c. The
review was not due: 3 product milestones and 7 entries followed the review of v0.105.

**The record.** `docs/reviews/2026-10-30-codebase-review.md` examines `main` at `a839bb7`, the merge of v0.112.
Four agents probed the code in parallel, each one in its own worktree: the engine, the desktop and the gates, and a
fourth agent researched K0d. The engine agent worked at the first push of K2c and found two high defects and one
low defect there; TASK-0110 corrected them before its merge.

**The findings.** 16 new findings: one high (T88, an item `IntermediateAssembly` gives a client the assembly name
`Engine.Tests`), seven medium (V46, T89, T90, T91, T93, T94, T95) and eight low (E94, E95, E96, V47 to V50, T92).
E86 to E93, V40 to V45 and T79 to T87 are fixed or fixed for their forms. Open findings: 0 critical, 6 high, 29
medium and 32 low.

**The order.** TASK-0112 (P0.44) holds T88 before R9. TASK-0114 (P0.45) holds the medium and low findings after R9.
TASK-0115 (P0.46) holds E11, E14, E16 and the test of R-0043, as the plan says. TASK-0116 (K0d) is Deferred until
the owner answers Q1 to Q3 (register entry R-0045).

**A correction.** The review counts 37 ADRs, and the ledger of v0.112 gives no count of ADRs; no earlier statement
changes.

New tests: none. The test list holds 1145 (839 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.112, 0 product milestones and 1 entry followed
it.

## v0.114 — No MSBuild element gives a client the assembly name of another project (P0.44, TASK-0112)

The codebase review of 2026-10-30 found T88 (high) and put it before R9 (`docs/templates.md`, section 7).

**The change.** `DependencyDirectionGateTests.AssemblyNameOffences` refuses the item `IntermediateAssembly` and
each `Output` of a task whose `ItemName` or `PropertyName` is `IntermediateAssembly`, `TargetName`,
`AssemblyName`, `TargetPath` or `TargetFileName`, in each file that MSBuild reads for a project, with no regard to
case. A target that replaced the item gave the desktop the file `Engine.Tests.dll` and the internals of the kernel.

**Proof.** 4 new rows of `Each_Form_Of_Finding_T72_Is_An_Offence`: the form of the review, its case form, and the
outputs of `CreateItem` and `CreateProperty`. 3 injections: each one failed a test or the gate program; the third
put the form of the review in `3DEngine/3DEngine.csproj`.

**What stays.** A target that calls the task `Csc` with its own output, and a `Directory.Build.rsp` file (R-0031).

**A correction.** None.

New tests: 4. The test list holds 1149 (843 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.112, 0 product milestones and 2 entries followed
it.

## v0.115 — The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says (P0.46, TASK-0115)

The plan of 2026-10-30 (answer Q4 A) gives this task E11, E14 and E16. R9 waits for the visual check of the owner on
its branch, and P0.45 changes the desktop, so under the answer Q3 A this task came before them: it changes no file of
the desktop.

**The change.**

- E14: each solid of Manifold in a buffer of `Marshal.AllocHGlobal` goes with `manifold_destruct_manifold` and then
  `Marshal.FreeHGlobal`, and each box with `manifold_destruct_box` and `Marshal.FreeHGlobal`. The C++ delete of
  `manifold_delete_*` gave one buffer to two allocators. The binding holds no delete function.
- E11: the binder refuses a number that is not finite, as text and as a typed value. `CreateBox` with `1e999`
  gave `Applied`, and a query of the body gave HTTP 500; it now gives HTTP 400 with `E-API-BAD-REQUEST`.
- E16: the envelopes of `POST /commands` and `POST /queries` refuse a member that they do not name, with HTTP 400.
  A typing error in `expectedDocumentVersion` removed the check of the version with no error.

**Proof.** 13 new test cases: 7 rows of `ParameterBinderTests`, 4 cases of `CommandsEndpointTests` with a control
for each envelope, and 2 in `ManifoldOwnershipTests`. 5 injections: each one failed a test.

**What stays.** R-0043 moves to TASK-0114: its probe needs a run of the desktop with a log of the samples, and its fix
can change a file that the parked branch of R9 changes. A file of an earlier version that holds a number that is not
finite cannot exist, because JSON cannot hold one.

**A correction.** The comment of `ManifoldNative.cs` said "VERIFIED" for a delete that frees the caller buffer; the
review of 2026-09-30 found that it is the C++ delete. The comment now gives the destruct and the free.

New tests: 13. The test list holds 1162 (856 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.112, 0 product milestones and 3 entries followed
it.

## v0.116 — An edit of a sketch rebuilds each translate and subtract that descends from its extrude (K0d, TASK-0116)

The owner chose the first slice of K0 on 2026-10-30 (KQ1 to KQ4 A) and answered A to Q1, Q2 and Q3 of the codebase
review of that date. ADR-0039 holds the decisions, and KQ4 A accepts it.

**The change.**

- `FeatureGraph.Of(log)` in `Engine.Core/Sketch/` gives the command in effect that creates each handle and the
  command that consumes it, as a projection of the log. No field of the Document and no file changes.
- After an edit of a sketch, each extrude of it whose body a `Translate` or a `Subtract` consumed is a seed. The
  rebuild walks from each seed to its live end, and runs each step upstream of the end again in log order under
  `GeometryKeys.Derive(edit, body)`: an extrude of the edited sketch from the solved record, an extrude of another
  sketch from its live record (Q1 A), and a `Translate` or a `Subtract` with a changed operand. A box keeps its key.
- The end gets the new key and one `body.changed`. A step that does not resolve or fails gives the end the fault
  `W-SKETCH-REBUILD-FAILED`, and the edit applies. The owner widened the meaning of that code to a body of the chain
  (Q2 A), and `docs/diagnostics.md` says so. `W-SKETCH-EXTRUDE-CONSUMED` stays for a consumer that the graph does
  not know (Q3 A); none exists today.
- An edit that gives the same profile changes no end. An undo of the edit swaps the records back and calls no backend.
- ADR-0039 amends ADR-0037 item 7. R-0045 closes.

**Proof.** 11 new test cases in `Engine.Tests/Sketch/ChainRebuildTests.cs`: a translate, a subtract with the extrude
as each operand, a chain of three steps, an undo and a redo, an empty cut with its fault and its repair, an operand of
another sketch, two extrudes in one subtract, two ends in the log order, an unchanged profile, the graph of a log, and
an open of the file. The test of a consumed extrude in `RebuildTests` now expects the rebuild. 10 injections: each
one failed a test.

**What stays.** Each edit runs each step upstream of each end on the backend again, and each old solid stays in the
backend (ADR-0037 item 9).

**A correction.** The `governed-by` of TASK-0114 gains ADR-0039, whose `affects` holds a file of its write set.

New tests: 11. The test list holds 1173 (867 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.112, 1 product milestone and 4 entries followed
it.

## v0.117 — The engine holds the circle and the arc of a sketch, and solves tangent, radius, pointOn and equal on them (K2d, TASK-0117)

The plan of 2026-10-30 puts K2d after K0d. `SketchRecord` already held `Circles` and `Arcs`, so the public shape of
`Engine.Contracts` does not change; the History lines of ADR-0033 and ADR-0035 give the decisions, under the answer Q2 A.

**The change.**

- `AddSketchCircle` (`sketchId`, `centerPointId`, `radius`) and `AddSketchArc` (`sketchId`, `centerPointId`,
  `startPointId`, `endPointId`) add a circle and an arc. A radius that is not a positive finite number, and an arc
  whose three points are not three different points, give the new code `E-SKETCH-CURVE-INVALID`.
- `DeleteSketchItem` takes a circle and an arc, and a delete of a point removes each curve that references it.
- The solver gives the radius of each circle a column after the points, and the solved record holds the new radii.
  Each arc adds |s − c| − |e − c| = 0, so its two radii are equal.
- `AddSketchConstraint` accepts `tangent` (a line and a curve, or two curves with 1 or −1), `radius`, and `pointOn`
  and `equal` on a circle or an arc. The radius of an arc is the distance from its centre to its start.
- `Extrude` still takes a profile of lines only, and refuses a sketch with a curve with `E-SKETCH-PROFILE-INVALID`.

**Proof.** 25 new test cases in `Engine.Tests/Sketch/CircleArcTests.cs`: each of ten forms on a curve solves a rough
sketch with its free degrees, the radii of an arc become equal, each derivative agrees with a central difference also
by a radius, a fixed solve with curves gives constant bits, the commands and each refusal, a delete, and an extrude.
`DocumentFileTests` holds a sample of each new command. 11 injections: each one failed a test.

**What stays.** The desktop draws no circle and no arc (K2e). The side of a tangent comes from the positions at the
start of a solve.

**A correction.** None.

New tests: 25. The test list holds 1198 (892 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: `E-SKETCH-CURVE-INVALID`. Since the review of v0.112, 2 product milestones and
5 entries followed it.

## v0.118 — The sketch keeps read-only lists and refuses each identifier that the log holds, and the gates read the forms of the thirteenth review (P0.45, TASK-0114)

The codebase review of 2026-10-30 put its medium and low findings after R9. R9 waits for the visual check of the owner
on its branch, so under the answer Q3 A of the plan this task took the items of the engine and the gates, and
TASK-0118 (P0.47) took the items of the desktop and R-0043.

**The change.**

- E94: each list of a sketch record that a command gives is read-only, also after a move and a change of a value.
- E95: `MoveSketchPoint`, `SetSketchConstraintValue` and `DeleteSketchItem` refuse an identifier that the log holds
  with `E-SKETCH-ID-IN-USE`. E96: the comment of `SubtractCommand` says that it consumes both operands.
- T89: the UI gate reads a source with the extension `.CS`. T90: `ConstantStrings` binds each file in the compilation
  of its own project and the projects that it references. T91: no engine source holds a constant that is a part of a
  diagnostic code. T92: the control byte gate refuses U+2028, U+2029, U+200E, U+200F, U+061C and U+0085. T93: each
  ledger heading after the last review ends with `(<phase>, TASK-nnnn)`. T94 and T95: no gate file makes a thread or a
  `TaskCompletionSource`, or starts a task with `TaskCreationOptions.LongRunning`.

**Proof.** 18 new test cases, and three new forms of the self check of the runner. 11 injections: each one failed a
test or the gate program.

**What stays.** A task of a gate that waits for another kind of signal that nobody gives (R-0031). The desktop items in
TASK-0118.

**A correction.** The first write set of TASK-0114 did not name the three sketch handlers of E95 and
`eng/repo-gates/DiscardedTasks.cs`; the task names them now.

New tests: 18. The test list holds 1216 (910 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.112, 2 product milestones and 6 entries followed
it.

## v0.119 — The desktop moves the selected body with a translate gizmo (R9, TASK-0113)

The owner chose the defaults R9-Q1 to Q8 on 2026-10-27, and the plan of 2026-10-30 put R9 after P0.44. ADR-0038 holds
the decisions, under the answer Q2 A, and amends ADR-0026 item 2. The owner said yes to the pictures of the visual
check on 2026-10-30.

**The change.**

- The intent layer has four new intents, `GizmoStartIntent`, `GizmoMoveIntent`, `GizmoEndIntent` and
  `GizmoCancelIntent`, and the host gives `IntentMachine` a hit test. A press of the left button or of the first
  contact on a handle in the mode `Scene` drags the gizmo after the slop. A second contact, Escape, a lost pointer and
  a change of the mode cancel it. A still press on a handle selects as before.
- `3DEngine.Core/Gizmo/TranslateGizmo.cs` gives three arrows of 100 window points at the depth of the pivot and three
  squares, the hit test, the offset along an axis or in a plane, and the triangles of each handle.
- `3DEngine/GizmoActions.cs` moves the drawing of the body during the drag, and the release sends one `Translate`
  through `BodyActions.Move`, which selects the new body. A cancel and an offset of zero send nothing.
- `3DEngine.Vulkan/OverlayRenderer.cs` with `overlay.vert` and `overlay.frag` draws the gizmo after the scene and the
  highlight and before the panels, with no depth test and no culling, and the colour in the push block.

**Proof.** 24 new test cases: 16 in `3DEngine.Tests/GizmoTests.cs`, 4 rows of `IntentParityTests`, and 4 in
`3DEngine.ImageTests/GizmoActionsTests.cs`; and one image test on Ubuntu, `GizmoImageTests`. 10 injections: each one
failed a test. The visual check on the computer of the owner: the gizmo on a selected box in front of it, and a drag
of the arrow X that gave one `Translate` with dx 11.89 and dy = dz = 0, with no message of the validation layer.

**What stays.** No touch screen ran the gizmo. No rotation and no snap (ADR-0038, "Next").

**A correction.** The branch r9-translate-gizmo started before P0.46, K0d, K2d and P0.45 merged. Its commit moves onto
`main` by a cherry-pick on the branch r9-gizmo-on-main; the index of the ADRs and the test floor merged by hand.

New tests: 24, and one image test. The test list holds 1241 (910 in `Engine.Tests`, 210 in `3DEngine.Tests`, 121 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.112, 3 product milestones and 7 entries followed
it.

## v0.120 — The pads of the sketch follow their values, and the desktop meshes each body one time for each frame (P0.47, TASK-0118)

TASK-0114 gave this task the desktop findings of the codebase review of 2026-10-30, and it waited for R9, which changed
the same files.

**The change.**

- V46: the key of a pad of the sketch also holds the positions of the selected points, of the ends of a selected line,
  and the values of a selected constraint, so the pad closes when a value that it shows changes. An agent edit or an
  undo while the pad is open no longer comes back at its apply.
- V47: when the scope check of a frame closes a pad, the action that an Enter of the same frame gave is dropped.
- V48: `SessionBodies.Apply` skips a `body.changed` that a later change of the same body follows in the same frame,
  so twenty changes give one mesh.
- V49: the overlay of the sketch drops a pixel farther than 10⁷ from the window.
- V50: the fault of a selected body takes one line with its code, and the reason shows on a hover or a press.

**Proof.** 6 new test cases in `3DEngine.ImageTests/DesktopFormsTests.cs`, one of them on a GPU. 5 injections: each one
failed a test.

**What stays.** R-0043 stays open. A probe logged each SDL event, each routed sample and each intent of a focus click,
and a focus to another window gave one `FocusLostSample` to both. Its click reached the window above the desktop, and
the log held motions of a mouse that the probe did not move, so a person used the computer and the run stopped.

**A correction.** None.

New tests: 6. The test list holds 1247 (910 in `Engine.Tests`, 210 in `3DEngine.Tests`, 127 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.112, 3 product milestones and 8 entries followed
it.

## v0.121 — The numeric pad shows the length unit of the document (P8e, TASK-0119)

P8a (TASK-0091) put the length unit of the document in the header of its file (KD4). The pad of the desktop still
showed the constant `mm`.

**The change.**

- `DesktopUi` takes the length unit in its constructor, with no default value. Each pad shows that unit beside
  each field.
- The desktop host gives `_host.Session.LengthUnit`, the unit that the header of the file gave to the session.
- ADR-0032 item 7 now says that the pad shows the length unit of the session, `mm` for now.

**Proof.** 1 new test case in `3DEngine.ImageTests/NumericPadUiTests.cs`: a panel with the unit `in` opens New box
and Move, and each pad shows `in`. An injection that puts back the constant `mm` failed it.

**What stays.** The header permits the millimetre only (ADR-0015 item 7), so no test can show that the host gives the
unit of its session. A second unit is a later decision of the owner.

**A correction.** None.

New tests: 1. The test list holds 1248 (910 in `Engine.Tests`, 210 in `3DEngine.Tests`, 128 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.112, 4 product milestones and 9 entries followed
it.

## v0.122 — The codebase review of 2026-10-30 (b) (governance, TASK-0120)

The review of 2026-10-30 examined v0.112. After it came 4 product milestones (K0d, K2d, R9, P8e) and 9 entries, so the
next product milestone makes a review due. K2e is the next product phase, so the fourteenth review comes before it.

**The record.** `docs/reviews/2026-10-30-b-codebase-review.md` examines `main` at `dab1355`, the merge of v0.121.
Three agents probed the code in parallel at `6e065d5`, the same tree, each one in its own worktree: the engine, the
desktop, and the gates with the HTTP surface. I ran each failing probe again, injected T96 with a control, and wrote
the probe of E100.

**The findings.** 19 new findings: one high (E97, a solve gives a circle a negative radius or a radius of zero and
the sketch shows as solved), ten medium (E100, E101, V51, V52, T96 to T101) and six low (E98, E99, V53 to V55, T102).
T88, E94 to E96, T89 to T95 and V46 to V50 of the last review are fixed, and so are E11, E14 and E16. Open findings:
0 critical, 6 high, 29 medium and 30 low.

**The order.** TASK-0121 (P0.48) holds E97 before K2e. TASK-0122 is K2e. TASK-0123 (P0.49) and TASK-0124 (P0.50) hold
the medium and low findings after K2e. E99 and E100 wait for Q1, and V53 waits for Q2 (register entry R-0046).

**A correction.** None.

New tests: none. The test list holds 1248 (910 in `Engine.Tests`, 210 in `3DEngine.Tests`, 128 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.121, 0 product milestones and 1 entry followed
it.

## v0.123 — A solve keeps each radius of a circle positive (P0.48, TASK-0121)

The codebase review of 2026-10-30 (b) found E97 (high) and put it before K2e (`docs/templates.md`, section 7).

**The change.**

- `SketchSolver` gives no solution when a circle has a radius that is not greater than 0, or an arc has its start or
  its end at its centre. The record then keeps the positions of the command, as for each other solve that fails, and
  `Status` gives not solved. Two circles with fixed centres 10 apart, a radius of 20 and an outer tangent gave the
  radius −10 and solved.
- `SketchConstraints.Refusal` refuses a `tangent` of a line with an end at the centre of its curve. That end is on
  the line, so only a radius of 0 satisfies it.

**Proof.** 6 new test cases in `Engine.Tests/Sketch/CircleArcTests.cs`: the four sketches of the review, the tangent
through a fixed centre by a command, and an arc of radius 0 in the status; and two refusals in the test of the
refusals. 4 injections: each one failed a test.

**What stays.** E98: `AddSketchArc` still accepts an arc of radius 0 (P0.49). A solve that fails here can miss a
positive solution from another start; the cases of the review have none.

**A correction.** None.

New tests: 6. The test list holds 1254 (916 in `Engine.Tests`, 210 in `3DEngine.Tests`, 128 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.121, 0 product milestones and 2 entries followed
it.

## v0.124 — The desktop draws and adds circles and arcs (K2e, TASK-0122)

K2d (TASK-0117) gave the engine the circle and the arc. The codebase review of 2026-10-30 (b), sections 5 and 6,
gave the research and the approach.

**The change.**

- `SketchOverlay` draws each circle and each arc as a polyline of 64 segments for a full turn, counter-clockwise
  from the start of an arc, and the picture of the tool holds the pixels of each curve. The polyline is presentation
  (ADR-0007), so it uses a sine and a cosine outside the log.
- `SketchTool` has the tools Circle and Arc. Circle takes the centre and a point on the circle; the radius is the
  distance in the plane, with `sqrt` only. Arc takes the centre, the start and the end, and a new end goes on the
  circle of the start. The tool Select also picks a curve.
- The window of the sketch has the buttons Circle and Arc, and On curve, Tangent, Inside tangent, Equal and Radius
  for a selection with a curve. A circle has the label R1, and an arc A1. ADR-0034 item 4 holds the tools.
- The row of Next point stays the second row of the window, so the pad stays inside a display of 1024 × 768.

**Proof.** 11 new test cases: 5 in `3DEngine.Tests/SketchToolTests.cs`, 2 in `SketchOverlayTests.cs`, 2 in
`SketchWindowUiTests.cs`, 1 in `SketchActionsTests.cs` through a session, and 1 on a GPU in `CurveImageTests.cs`.
9 injections: each one failed a test. The owner accepted the picture `curves.png` of the GPU test on 2026-10-30.

**What stays.** The kinds of K2c on lines have no button (pointOn and midpoint on a line, parallel, perpendicular,
equal of two lines, symmetric and angle). An extrude refuses a profile with a curve (TASK-0117). V51 to V55 in
TASK-0124. A person used the computer, so no run drove the real window with a mouse.

**A correction.** The Goal of TASK-0122 and the row of K2e named the kinds of K2c too; Scope (in) item 4 named the
kinds on a curve only, and the Goal and the row now agree with it.

New tests: 11. The test list holds 1265 (916 in `Engine.Tests`, 215 in `3DEngine.Tests`, 134 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.121, 1 product milestone and 3 entries followed
it.

## v0.125 — The engine, the surface and the gates close the forms of the fourteenth review (P0.49, TASK-0123)

The codebase review of 2026-10-30 (b) put its medium and low findings of the engine, the HTTP surface and the gates
after K2e. The owner answered Q1 A on 2026-10-30, after the first commit of this task, so its second commit records
the answer.

**The change.**

- E98: `AddSketchArc` refuses a start or an end at the position of the centre.
- E99 and E100 (Q1 A): the guard of a repeated identifier acts on a live command only. The bus sets
  `SketchResults.ApplyingEntry` while a handler applies an entry of an open or a replay, so a file of v0.117 in which
  a client sent a move again opens. `Extrude` takes the guard.
- E101: the HTTP surface refuses a repeated member of an envelope and of the parameters with HTTP 400.
- T96: each line that Markdown shows as a heading of a version has the exact form `## v`.
- T97 and T98: each project of the constants reader is its own compilation, with the compilations of the projects that
  it references, read from each file that MSBuild reads for it.
- T99: a part of a code in an interpolated string, a middle part such as `-SKETCH-`, a code in lower case, and a
  severity after the separator of `string.Join` are offences.
- T100: the check of the tasks reads the helper files, and refuses `TaskContinuationOptions.LongRunning`, a cast of
  the value 2, and a semaphore or an event that a gate makes.
- T101: each item `Compile` names `.cs` files only. T102: a lone CR, the C1 controls, U+200B, U+2060 and U+FEFF after
  the start are offences.

**Proof.** 33 new test cases and five new forms of the self check of the runner. 21 injections: each one failed a test
or the gate program.

**What stays.** T99 stays a rule in text for each other spelling of a call. V51 to V55 in TASK-0124. R-0046 stays
open until the first commit of TASK-0124 records Q2 A.

**A correction.** The row of `E-SKETCH-ID-IN-USE` in `docs/diagnostics.md` named four commands; TASK-0114 and
TASK-0117 gave the code to five more. The row now names each one. The first progress line of TASK-0123 had the date
2026-10-31; the work was on 2026-10-30.

New tests: 33. The test list holds 1298 (949 in `Engine.Tests`, 215 in `3DEngine.Tests`, 134 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 15 of 15. New diagnostic codes: none. Since the review of v0.121, 1 product milestone and 4 entries followed
it.

## v0.126 — The gizmo stays in front of the eye, and the overlay keeps each visible part of a line (P0.50, TASK-0124)

The codebase review of 2026-10-30 (b) put its desktop findings after K2e, which changes the same overlay. The owner
answered Q2 A on 2026-10-30, and the first commit of this task records it.

**The change.**

- V51: an arrow of the gizmo gives no offset when the point of the ray nearest to the axis is nearer than the near
  plane, so a drag past the vanishing point of an axis keeps its last offset. The body jumped behind the eye.
- V53 (Q2 A): while a drag of the gizmo runs, the wheel, the arrow keys, plus, minus, H, Control+Z and Control+Y give
  nothing; Escape still cancels. ADR-0038 items 5 and 8 hold V53 and V51.
- V52: the overlay clips each segment to the window with a margin of one window before the limit of a pixel of V49, so
  a line that passes beside and behind the eye keeps its visible part.
- V54: the release of a drag sends no `Translate` when the body left the document during the drag.
- V55: a body that is created and changed in one frame takes the state of its last change, so it gives one mesh.

**Proof.** 6 new test cases: 2 in `3DEngine.Tests/GizmoTests.cs`, 1 in `3DEngine.ImageTests/GizmoActionsTests.cs`,
and 3 in `DesktopFormsTests.cs`, one of them on a GPU. 6 injections: each one failed a test.

**What stays.** No person dragged the gizmo past a vanishing point in the real window. R-0043 stays open.

**A correction.** The test of V49 said that a point at 3·10³⁷ gives no stroke. A line through the window from such a
point now draws its part in the window, so the test checks that no vertex goes beyond the limit, which is the
property of V49. R-0046 is closed: the owner answered Q1 A (TASK-0123) and Q2 A (TASK-0124).

New tests: 6. The test list holds 1304 (949 in `Engine.Tests`, 217 in `3DEngine.Tests`, 138 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.121, 1 product milestone and 5 entries followed
it.

## v0.127 — The window of the sketch offers the constraints of K2c on lines (K2f, TASK-0125)

K2c gave the engine the kinds on points and lines, and K2e gave the window the kinds on a curve. The Scope (out) of
K2e left the kinds of K2c on lines to a later task.

**The change.**

- On line and Midpoint fit a point and a line when the point is not an end of the line, as the engine requires.
- Equal, Parallel, Perpendicular and Angle fit two lines. Angle opens a pad of degrees with the angle from the first
  line to the second, counter-clockwise, and the client gives its cosine and sine to the command (ADR-0033 item 3,
  determinism rule 2). A multiple of 90 degrees gives exact numbers.
- ADR-0034 item 4 names the buttons.

**Proof.** 2 new test cases in `3DEngine.ImageTests/SketchWindowUiTests.cs`, and the test of a line and O gains On
line and Midpoint. 5 injections: each one failed a test.

**What stays.** Symmetric has no button: it takes two points and a line, and the selection holds two items at most
(ADR-0034 item 4). No picture of the new buttons.

**A correction.** None.

New tests: 2. The test list holds 1306 (949 in `Engine.Tests`, 217 in `3DEngine.Tests`, 140 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.121, 2 product milestones and 6 entries followed
it.

## v0.128 — The selection holds three items, and the window of the sketch offers Symmetric (K2g, TASK-0127)

K2f left Symmetric with no button, because it takes two points and a line and the selection held two items. The owner
chose this item on 2026-10-31 ("1 3 4", item 3).

**The change.**

- `SketchTool.MaximumSelection` is 3, and a fourth item replaces the first (ADR-0034 item 4).
- Symmetric fits two points that are not ends of the line, and the line; its action takes the points first.

**Proof.** 1 new test case in `3DEngine.ImageTests/SketchWindowUiTests.cs`, and the test of the tool Select holds
three items. 2 injections: each one failed a test.

**What stays.** No picture of the window with three selected rows.

**A correction.** None.

New tests: 1. The test list holds 1307 (949 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.121, 3 product milestones and 7 entries followed
it.

## v0.129 — The roadmap row K3 closes, because K3a, K2c and K2d hold the fourteen kinds (governance, TASK-0128)

The row K3 said "The rest of the constraint solver, after K3a." `SketchConstraints.Given` holds the fourteen kinds of
ADR-0033 item 3: K3a (TASK-0102) gave five, K2c (TASK-0110) seven, and K2d (TASK-0117) tangent and radius. The owner
chose to close the row on 2026-10-31 ("1 3 4", item 4).

**The change.** The row K3 says that it is done and names the three tasks, and `docs/roadmap.md` has a done line.

**What stays.** The speed of the dense solve stays the finding P1 (register entry R-0027).

**A correction.** None.

New tests: none. The test list holds 1307 (949 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.121, 3 product milestones and 8 entries followed
it.

## v0.130 — An extrude takes a profile with arcs and circles (K0e, TASK-0126)

An extrude refused a sketch with a curve, because the polygon of an arc needs the points between its ends, and a sine
and a cosine are not permitted in code of the log (determinism rule 1). The owner chose this item on 2026-10-31
("1 3 4", item 1), and then told the session to use its recommended answers until the morning, so ADR-0040 is
Accepted with Q1 A, Q2 A and Q3 A.

**The change.**

- A profile is one closed loop of lines and arcs, or one circle alone (ADR-0040 item 1). `SketchProfile.Find` walks
  the edges and adds the polygon of each arc in the direction of the walk; the checks of ADR-0036 item 2 run on the
  polygon.
- `SketchProfile.Turn` gives the polygon of a curve with `+ - * / sqrt` only: the sweep comes from the dot and the
  cross of the two directions, each halving gives the cosine and the sine of half the angle, and each vertex is the one
  before it turned by the step. A full turn has 64 segments, and an arc the power of two that its sweep needs.
- The row of `E-SKETCH-PROFILE-INVALID` names the rules of a curve (Q3 A). No public shape of `Engine.Contracts`
  changes, and the desktop needs no change.

**Proof.** 11 new test cases in `Engine.Tests/Sketch/CurveProfileTests.cs`, among them a hash of the bits of an arc
and a circle, which the three runners check, and an extrude and a rebuild of a circle on Manifold. 7 injections: 6
failed a test. The seventh put back the constant cos(2π / 64) of the draft, and a full turn still gave 64 segments.

**What stays.** Holes (Q1 A) and a tolerance of the chord (Q2 A). A curved face is flat facets.

**A correction.** The draft of ADR-0040 said that the constant cos(2π / 64) gave a seventh halving of a full turn. No
run showed it: the injection gives 64 segments. The ADR, the comment of the code and the task now call 0.9951 a margin.
The test `An_Extrude_Refuses_A_Sketch_With_A_Circle` became `An_Extrude_Refuses_A_Circle_With_A_Line`.

New tests: 11. The test list holds 1318 (960 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.121, 4 product milestones and 9 entries followed
it.

## v0.131 — The codebase review of 2026-10-31 (governance, TASK-0129)

The review of 2026-10-30 (b) examined v0.121. After it came 4 product milestones (K2e, K2f, K2g, K0e) and 9 entries, so
the next product milestone makes a review due. The owner told the session on 2026-10-31 to work alone until the morning
and to use its recommendation for each question.

**The record.** `docs/reviews/2026-10-31-codebase-review.md` examines `main` at `661eb8a`, the merge of v0.130. Three
agents probed the code in parallel at `4f252bf`, the same tree: the engine with the HTTP surface, the desktop, and the
gates. I ran the failing engine probes again, injected T103 with a control, and read the cited desktop lines.

**The findings.** 18 new findings: two high (E102, a radius at the noise level shows as solved; T103, the check of the
tasks of a gate binds no task of a real gate file), six medium (E103, V56, T104 to T107) and ten low (E104, E105, V57
to V62, T108, T109). E97 to E101, V51 to V55 and T96 to T102 are fixed for their forms; T94, T95 and T100 hold for the
full names only. Open findings: 0 critical, 7 high, 25 medium and 34 low.

**The order.** TASK-0130 (P0.51) holds E102 and T103 before the next product phase. TASK-0131 is R11a, the first slice
of R11. TASK-0132 (P0.52) and TASK-0133 (P0.53) hold the medium and low findings after R11a. The standing answer of the
owner takes Q1 A and Q2 A, so no register entry waits.

**A correction.** The fourteenth review said that T94 and T95 were fixed, and TASK-0123 said that T100 was fixed. Each
fix holds for the full names only (T103).

New tests: none. The test list holds 1318 (960 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 0 product milestones and 1 entry followed
it.

## v0.132 — A radius at the noise level does not solve, and the check of the tasks binds the real gate files (P0.51, TASK-0130)

The codebase review of 2026-10-31 found E102 and T103 (high) and put them before R11a (`docs/templates.md`, section 7).

**The change.**

- E102: `SketchSolver` gives no solution, and `Status` gives not solved, when a radius, or the distance of the start or
  of the end of an arc from its centre, is not greater than `SketchSolver.Tolerance` (1e-9). A coincident centre and a
  tangent gave a radius of 5.6e-17 and a solved sketch.
- T103: `eng/repo-gates/DiscardedTasks.cs` compiles the gate files with the global usings of the SDK and of Xunit, as
  the two projects do; a gate file that does not compile there fails the run; the self check has no using of its own.
  `new Thread(...)`, a task in a local and a `SemaphoreSlim` with no full name now fail the gate program; they passed.

**Proof.** 2 new test cases in `Engine.Tests/Sketch/CircleArcTests.cs`, and four new forms of the self check of the
runner. 4 injections: each one failed a test or the gate program. The three forms of the review, injected into a gate
file, each failed the gate program.

**What stays.** The medium and low findings in TASK-0132 and TASK-0133.

**A correction.** None.

New tests: 2. The test list holds 1320 (962 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 0 product milestones and 2 entries followed
it.

## v0.133 — A tap of the sketch snaps to a midpoint, a line, a curve or the grid (R11a, TASK-0131)

The roadmap row R11 had no slice. The codebase review of 2026-10-31 proposed R11a, and the standing answer of the
owner of 2026-10-31 takes Q2 A, so ADR-0041 is Accepted.

**The change.**

- A tap of the tools Line, Circle and Arc that has no point within 22 pixels takes the midpoint of a line, else the
  nearest point on a line or a curve, else the position of the grid (ADR-0041 items 1 and 2). A snap adds no
  constraint (item 3).
- `SketchOverlay.Picture` gives each curve its centre and its radius in the plane. ADR-0041 amends ADR-0034.

**Proof.** 5 new test cases: 4 in `3DEngine.Tests/SketchToolTests.cs` and 1 in `3DEngine.ImageTests/SketchOverlayTests.cs`.
5 injections: each one failed a test.

**What stays.** The mark of the snap before a tap needs a hover intent (ADR-0026); the escalated selection of faces and
edges needs their identity (K4). Both stay in the row R11.

**A correction.** The review planned a mark of the snap and a picture of it; the intent machine has no hover intent, so
TASK-0131 moved the mark to Scope (out) and changed that criterion to a test of the picture.

New tests: 5. The test list holds 1325 (962 in `Engine.Tests`, 221 in `3DEngine.Tests`, 142 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 3 entries followed
it.

## v0.134 — The engine, the surface and the gates close the forms of the fifteenth review (P0.52, TASK-0132)

The codebase review of 2026-10-31 put its medium and low findings of the engine, the HTTP surface and the gates after
R11a. The standing answer of the owner of 2026-10-31 takes Q1 A for E103.

**The change.**

- E103 (Q1 A): the refusals of an arc at its centre and of a tangent through the centre do not act on an entry of an
  open or a replay, so the files of v0.117 to v0.122 open; the solve then gives no solution.
- E104: a command that creates an item is refused also for an entry whose identifier came before, with
  `E-SKETCH-ID-IN-USE`, so the open names the line. A second `AddSketchPoint` gave an `IndexOutOfRangeException`.
- E105: the routes `/document/save` and `/document/open` refuse a repeated member with HTTP 400.
- T104 and T105: each compilation of `ConstantStrings` has the implicit usings of the SDK, the items `Using` and only
  the grants `InternalsVisibleTo` of its project; a cycle of references ends.
- T106: each heading line of the ledger other than its title is `## v`. T107: an `Output` with the item name
  `Compile` is an offence. T108: a file that is not valid UTF-8, a character of the category Format after the start,
  a Hangul filler and a variation selector are offences. T109: a constant severity alone, a middle of a code with no
  dash after it, and a character severity before a string are parts of a code.

**Proof.** 32 new test cases. 13 injections: each one failed a test. The gate program gives 339 runs of 138 checks.

**What stays.** T109 reads the syntax for a call whose separator is not a literal. The desktop findings in TASK-0133.

**A correction.** The row of `E-SKETCH-ID-IN-USE` said that an open never raises the code; since E104 it raises it for
a command that creates an item, and the row says so. The first write set did not name the nine handlers that call
`SketchResults.InUse` with its new parameter; the check of the staged change found it before the push.

New tests: 32. The test list holds 1357 (994 in `Engine.Tests`, 221 in `3DEngine.Tests`, 142 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 4 entries followed
it.

## v0.135 — The pad of an angle takes degrees, and the tools and the window follow the review (P0.53, TASK-0133)

The codebase review of 2026-10-31 put its desktop findings after R11a, which changes the same tool.

**The change.**

- V56: the Value pad of an angle shows its degrees and gives the cosine and the sine; a tangent of two curves has no
  Value, because Tangent and Inside tangent give its value. V60: `CosineAndSine` reads the turn, so 1e20 gives the angle
  of its turn.
- V57: a pending circle or arc reads the current positions of its centre and its start. V61: a new end of an arc in
  the direction of its start ends the curve, with no second point. V62: the tool Select picks the nearer of a line and
  a curve.
- V58: from a press on a handle of the gizmo, the wheel, the camera keys, H, undo and redo give nothing. V59: On curve
  and Tangent take the item checks of `SketchConstraints.Refusal`.

**Proof.** 6 new test cases: 4 in `3DEngine.Tests` and 2 in `3DEngine.ImageTests/SketchWindowUiTests.cs`. 9 injections:
each one failed a test.

**What stays.** No person tried the new pad and the picks with the real window.

**A correction.** None.

New tests: 6. The test list holds 1363 (994 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 5 entries followed
it.

## v0.136 — The log of the Document is read-only, a cancelled replay is a cancellation, and a query of a lost body gives an error (P0.54, TASK-0135)

R11b (TASK-0134) waits for the visual check of the owner on its branch, so this task took three findings of register
entry R-0036 that change no file of the desktop.

**The change.**

- E26: `Document.Log` is a read-only view of the list. A cast to `List` and a `Clear` moved the version from 2 to 0.
  The public shape does not change; ADR-0020 has a section of history for it.
- E29: `Replay.ReplayLog` throws `OperationCanceledException` when a command gives Cancelled and the token of the
  caller is cancelled. It reported a divergence of the log.
- E32: `GetBoundingBox` and `GetTessellation` give `E-GEOM-NATIVE-OP` when the backend throws. A body that the
  backend did not hold threw `KeyNotFoundException` out of the query bus, and a host answered HTTP 500.

**Proof.** 3 new test cases in `Engine.Tests/Commands/EngineDebtTests.cs`. 4 injections: each one failed a test.

**What stays.** Twelve findings of R-0036.

**A correction.** The write set first did not name ADR-0020; the check of the staged change refuses a change of
`Engine.Contracts/` with no ADR file, and the write set names it now.

New tests: 3. The test list holds 1366 (997 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 6 entries followed
it.

## v0.137 — An exception of a handler in a replay is a divergence, and a replay needs a backend (P0.55, TASK-0136)

R11b (TASK-0134) still waits for the visual check of the owner, so this task took the findings of the replay in
register entry R-0036.

**The change.**

- E28: `Replay.ReplayLog` wraps an exception of an entry that is not a cancellation in `ReplayDivergenceException`.
  The exception names the entry and the command, `Result` is null, and `InnerException` holds the exception. Before
  the change an `InvalidCastException` left the replay with no entry and no command. The test of finding E7 asserts
  the exact type now.
- E30: the parameter `backend` of the two methods `Replay.ReplayLog` is required, and null gives
  `ArgumentNullException`. The default null backend made each log with geometry diverge at its first body. Six calls
  in the tests name `NullGeometryBackend.Instance`.

**Proof.** 2 new test cases in `Engine.Tests/Commands/EngineDebtTests.cs`. 2 injections: each one failed a test.

**What stays.** E27, the refusal of a backend that holds a body: `IGeometryBackend` has no member that tells it, so
the refusal needs a change of `Engine.Contracts` and an ADR, which is a stop condition. It stays in R-0036 for the
owner, with the other open findings.

**A correction.** The first write set had no `governed-by`; the gate of the task asked for ADR-0004, ADR-0015 and
ADR-0020, and the task names them now.

New tests: 2. The test list holds 1368 (999 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 7 entries followed
it.

## v0.138 — The command line checks a name before the backend, and the comments of the replay are true (P0.56, TASK-0137)

R11b (TASK-0134) still waits for the visual check of the owner, so this task took two more findings of register entry
R-0036.

**The change.**

- E37: `Cli.Apply` and `Cli.Query` find the handler in the catalog and bind the parameters before the backend
  loads. With no native library, `apply CreateBx` gave the exit code 3 of the backend. It gives `E-CMD-UNKNOWN` with
  the exit code 1 now, and a misspelled query gives `E-QRY-UNKNOWN`.
- E31: the comments of `InProcessMeshBackend` and `ManifoldGeometryBackend` said that the idempotency cache stops
  each repeated command. A replay and an open have no cache, and the second box is a divergence with
  `E-GEOM-NATIVE-OP`. The comments of `Replay.cs` and `CommandBus.cs` say the same, and that the cache keeps the
  results up to its capacity. The message of `ReplayDivergenceException` gave ".." after an error message that ends
  with a full stop.

**Proof.** 2 new test cases: one in `Engine.Tests/Hosting/BackendSelectionTests.cs` and one in
`Engine.Tests/Commands/EngineDebtTests.cs`, plus 2 new assertions for the "..". 3 injections: each one failed a test.
The test of the repeated box proves the new comments; it passes before the change too.

**What stays.** E25 (a session on a replayed Document needs a decision on the handover of the bus), E27 (a change of
`Engine.Contracts`) and the findings of the hosts in R-0036. The comment of the reset in `CommandBus.cs` is E18 of
R-0033.

**A correction.** The first task text named the code `E-QUERY-UNKNOWN`. The code is `E-QRY-UNKNOWN`.

New tests: 2. The test list holds 1370 (1001 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 8 entries followed
it.

## v0.139 — The reason of a native library that does not load separates a missing file from a platform with no payload (P0.57, TASK-0138)

R11b (TASK-0134) still waits for the visual check of the owner, so this task took finding E38 of register entry R-0036.

**The change.**

- E38: `ManifoldGeometryBackend.UnavailableReason` takes the platform. A platform of the folder `runtimes` of the
  package 3.5.2.1 (`win-x64`, `linux-x64`, `osx-arm64`) gives "the file is not in the folder of the program, or the
  system cannot load it". Another platform gives the list of the three platforms. On `win-x64` the sentence said that
  the package holds `win-x64`. The command line, the HTTP host and the desktop give the same sentence.

**Proof.** 1 new test case in `Engine.Tests/Hosting/BackendSelectionTests.cs`. 1 injection: it failed the test.

**What stays.** The list of the platforms is a copy of the package, and a new package must change it. E25, E27, E33,
E34, E35 and E39 stay in R-0036.

**A correction.** None.

New tests: 1. The test list holds 1371 (1002 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 9 entries followed
it.

## v0.140 — The schema of the backend is in the list of the surface, and it gives the identity of the backend (P0.58, TASK-0139)

R11b (TASK-0134) still waits for the visual check of the owner, so this task took finding E35 of register entry R-0036.

**The change.**

- ADR-0008 §9 lists `/schema/backend`, which TASK-0036 added with no line there, and a section of history records it.
  A new gate in `Engine.Tests/Http/SchemaEndpointGateTests.cs` holds the routes of `EngineSurface.cs` under
  `/schema` equal to the list of §9.
- `ServerEngine.Create` gives the host the name and the version of `backend.Identity` (ADR-0015 item 5). With the
  managed backend, `/schema/backend` gave the version "1.0.0.0" of the assembly `Engine.Core`; it gives "1" now,
  which each log entry records. The desktop host already gave the identity of the native backend.

**Proof.** 1 new test case and 1 new assertion. 2 injections: the list of ADR-0008 with no line for
`/schema/backend`, and the version of the assembly; each one failed a test.

**What stays.** E25, E27, E33, E34 and E39 stay in R-0036.

**A correction.** ADR-0008 §9 was incomplete; this task corrects it.

New tests: 1. The test list holds 1372 (1003 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 10 entries followed
it.

## v0.141 — A sink of events does not throw, and a test holds each sink to it (P0.59, TASK-0140)

R11b (TASK-0134) still waits for the visual check of the owner, so this task took finding E18 of register entry
R-0033. The owner answered its question Q4 on 2026-10-04: a sink must not throw, by contract, and a test holds each
sink to it. TASK-0047 recorded the answer, and no test held a sink to it until now.

**The change.**

- `IEventSink` states that `Append` must not throw for a record of the bus, and why: the bus appends after the
  Document changed, so a throw leaves a gap in the sequence that ADR-0005 forbids.
- `Engine.Tests/EventSinkContractTests.cs` finds each class of `Engine.Core` and `Engine.Api.Http` that implements
  `IEventSink` (two today) and requires it in a list with a test. It tests the ring when it is full and the
  broadcasting sink when an action of a subscription throws.
- The comment of `CommandBus.cs` said that a subscriber recovers a lost event with a reset; no code sends one. It
  states the rule now, and `InMemoryEventSink.cs` gives the reason that its range has no gap.

**Proof.** 3 new test cases. 3 injections: a new sink with no test, a ring that throws when it is full, and a
broadcaster that does not catch an action; each one failed a test.

**What stays.** E33 of R-0036 had only this cause, so the next review judges it again. E19, E21, E22 and seven parts of
E24 stay in R-0033.

**A correction.** None.

New tests: 3. The test list holds 1375 (1006 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.130, 1 product milestone and 11 entries followed
it.

## v0.142 — The codebase review of 2026-10-31 (b) (governance, TASK-0141)

The review of 2026-10-31 examined v0.130. After it came 1 product milestone (R11a) and 11 entries, and R11b (TASK-0134)
would give the twelfth, so this review comes before it. The owner told the session on 2026-10-31 to work alone until
the morning and to use its recommendation for each question.

**The record.** `docs/reviews/2026-10-31-b-codebase-review.md` examines `main` at `296ae13`, the merge of v0.141. Two
agents probed the code in parallel at `eb8c06f`, the same tree: the engine with the hosts, and the desktop with the
gates. I ran the twelve engine probes again, injected T110 with a control, and read the cited desktop lines.

**The findings.** 18 new findings: one medium (T110, a condition `#if` hides code from three gates) and seventeen low
(E106 to E112, V63, V64, T111 to T116). Each low finding is the next form of a fix of this period. E18, E26, E28 to
E33, E35, E37, E38, E102 to E105, V56 to V62 and T103 to T109 are fixed for their forms. Open findings: 0 critical, 5
high, 17 medium and 31 low.

**The order.** No new finding is high, so no task comes before R11b. TASK-0142 (P0.60) holds the engine findings,
TASK-0143 (P0.61) the gate findings and TASK-0144 (P0.62) the desktop findings, after R11b. The standing answer of the
owner takes Q1 A and Q2 A (the owner accepts E25 and E27; TASK-0142 records it in R-0036) and Q3 A (the owner chooses
the product phase after R11b).

**A correction.** The comment that TASK-0137 wrote in the two backends says that an open has no cache, which is false
(E110).

New tests: none. The test list holds 1375 (1006 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 0 product milestones and 1 entry followed
it.

## v0.143 — The overlay marks the point that a tap under the mouse or a finger would take (R11b, TASK-0134)

R11a (TASK-0131) gave a tap of the sketch a snap, and ADR-0041 item 5 left the mark of the snap to the rest of R11,
because the intent machine had no hover intent.

**The change.**

- In the mode Sketch, the intent machine gives `SketchHoverIntent` for a motion of the mouse with no button, for the
  press of the left button, and for the first contact of a touch or a pen (ADR-0041 item 6, which amends ADR-0026).
  A hover for the mouse alone failed the parity test of objective 9, so a finger gives the hover while it is down.
- `SketchTool.Target` gives the point that a tap there would take, `SketchActions.Mark` asks the tool for it, and
  `SketchOverlay.Pixel` gives its pixel. The panel draws a cyan square of 12 pixels there. The tool Select has none.

**Proof.** 3 new test cases: 1 in `3DEngine.Tests` and 2 in `3DEngine.ImageTests`. The GPU test of the curves also
draws the mark. 6 injections: each one failed a test. The owner answered "Use the recommendation" on 2026-10-31 for
the picture of the mark, which accepts it.

**What stays.** The escalated selection of faces and edges needs their identity (K4), and stays in the row R11.

**A correction.** None.

New tests: 3. The test list holds 1378 (1006 in `Engine.Tests`, 226 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 1 product milestone and 2 entries followed
it.

## v0.144 — The open follows the replay, the adds follow the solver, and the comments of the cache are true (P0.60, TASK-0142)

The codebase review of 2026-10-31 (b) found E106 to E112 (low), each the next form of a fix of P0.54 to P0.58. R11b
merged at v0.143, so this task came next.

**The change.**

- E106: the open of a document file throws `OperationCanceledException` for a result Cancelled with a cancelled token,
  as the replay does since E29.
- E107: `ErrorText.Clause` in `Engine.Core/Replay.cs` drops the full stop of an error message, and the replay and the
  open share it, so the message of an open has no "..".
- E108: a live add of a circle, a live add of a constraint `radius` and a live change of its value refuse a radius
  that is not greater than `SketchSolver.Tolerance`. An entry of a log keeps the rule of 0 (E103).
- E109: `EntryCodec` reads a header line and an entry line with `AllowDuplicateProperties = false`. `IsJson` keeps the
  default options, so a last line with a repeated member is refused and not cut.
- E110 and E112: the comments of the two backends say that an open does not look in the cache and a replay has none;
  the comment of `Replay` says that the rule of E103 does not act in a replay of plain commands.
- E111: `EngineHost` reads `backend.Identity`, and its two string parameters go. 21 files built a host with them: the
  two hosts and 19 test files, which entered the write set of the task for this reason.
- Register entry R-0036 records Q1 A and Q2 A of the review: the owner accepts E25 and E27, with their reasons.

**Proof.** 6 new test cases in `Engine.Tests/Commands/EngineDebtTests.cs`. 6 injections: each fix undone alone failed
its test. The removed parameters of E111 are a compile error at each old call.

**What stays.** E34 and E39 stay in R-0036. The gate findings T110 to T116 are P0.61 (TASK-0143), and V63 and V64 are
P0.62 (TASK-0144).

**A correction.** None.

New tests: 6. The test list holds 1384 (1012 in `Engine.Tests`, 226 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 1 product milestone and 3 entries followed
it.

## v0.145 — The gates read disabled text, the routes of the test host, each sink, and the next forms of the fifteenth review (P0.61, TASK-0143)

The codebase review of 2026-10-31 (b) found T110 (medium) and T111 to T116 (low) in the gates. P0.60 merged at v0.144,
so this task came next.

**The change.**

- T110: `ConstantStrings.Constants` throws for a file with code inside a condition `#if`, so each gate that reads
  constants fails (the scanner of codes, the scanner of parts and the UI gate). The check of the tasks refuses such a
  gate file, and its self check has the form.
- T111: the gate of the schema routes reads the endpoints of the test host, and keeps each route under `/schema` in
  any case. A route of `MapMethods`, of `MapGroup` or with a capital letter is now in the list that it compares.
- T112: the gate of the sinks keeps each type that is not abstract and not an interface, in each assembly of a
  project of the repository next to the tests. It reads the `AssemblyName` of each project, because `Engine.Cli` builds
  `engine.dll`; the first form missed a struct sink there.
- T113: the control byte gate refuses U+E0100 to U+E01EF and U+180B to U+180F.
- T114: the counter of the review refuses an ordered list marker before a heading, and a setext heading in a quote.
- T115: an `ItemName` that holds `$(` or `@(`, or `Compile` with spaces, is refused.
- T116: the scanner of parts takes the value of a constant char on the left of an add from the compiler.

**Proof.** 13 new test cases. 13 injections: the form of T110 in `Engine.Cli` and in a gate file, its control with no
condition, a route `/Schema/probe` of `MapMethods`, a struct sink in `Engine.Cli`, a character U+180B, and each fix
undone against its new test case. Each one failed a gate.

**What stays.** The gate of the sinks cannot see a sink in the desktop host, because `Engine.Tests` does not reference
it. No such sink exists.

**A correction.** None.

New tests: 13. The test list holds 1397 (1025 in `Engine.Tests`, 226 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 1 product milestone and 4 entries followed
it.

## v0.146 — An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop (P0.62, TASK-0144)

The codebase review of 2026-10-31 (b) found V63 and V64 (low) in the desktop. P0.61 merged at v0.145, so this task came
next, and it is the last task of that review.

**The change.**

- V63: the tool Arc ends the curve when the new end is within `HitPixels` of the start on the screen. The scale is the
  pixels for each unit of the plane between the tap and the start. The pad gives no pixel, so it keeps the limit of
  V61, 1e-9 of the radius.
- V64: Escape before the slop cancels the press on a handle, so a later motion starts no drag. No `GizmoCancelIntent`
  goes out, because no drag started.

**Proof.** 3 new test cases: 2 in `3DEngine.Tests/SketchToolTests.cs` and 1 in `3DEngine.Tests/GizmoTests.cs`. 3
injections: each fix undone, and the first form of the rule in pixels; each one failed a test. That first form took a
scale of 0 when the pad gave no pixel, so each arc of the pad ended with no arc. The GPU test of the curves showed it
before the commit, and the test of the pad holds it now.

**What stays.** Nothing of the review: its tasks P0.60 to P0.62 are done. The owner chooses the next product phase
(question Q3 A of the review).

**A correction.** None.

New tests: 3. The test list holds 1400 (1025 in `Engine.Tests`, 229 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and zero warnings. Open
entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 1 product milestone and 5 entries followed
it.

## v0.147 — The vision library loads and finds a contour on the three platforms of the pipeline (V0, TASK-0145)

The codebase review of 2026-10-31 (b), question Q3, left the product phase after R11b to the owner. On 2026-10-31 the
owner chose V0, the first phase of track V (objective 15). No earlier record chose a library.

**The decision.** ADR-0042 chooses OpenCvSharp4 4.13.0.20260627, over OpenCV 4.13, under the Apache-2.0 licence. Each
platform restores one native payload: `OpenCvSharp4.runtime.win`, `OpenCvSharp4.official.runtime.linux-x64.slim` and
`OpenCvSharp4.runtime.osx.arm64`. Emgu.CV has no macOS arm64 payload and is GPL v3; OpenCvSharp5 is two weeks old.
Vision is input, not design truth: a result enters the Document only through the commands that exist, so no project
of the log references the library and a replay never calls it.

**The change.**

- `3DEngine.VisionTests` is a new test project in the solution. It references no project and holds the only pins of
  OpenCvSharp4. Its three tests give the version 4.13, the four corners of the contour of a filled rectangle, and its
  edges on the border only, with exact pixels.
- `DependencyDirectionGateTests` gives the spike a class of its own, which references no project, and keeps each pin
  of OpenCvSharp4 in it.
- `docs/INDEX.md`, `docs/adr/README.md` and `eng/test-floor.txt` name the project and the record.

**Proof.** 5 new test cases: 3 in `3DEngine.VisionTests` and 2 in `Engine.Tests`. The pipeline ran the three tests of
the spike on Windows x64, Ubuntu x64 and macOS arm64 (run 41180854816), and the count of each runner holds them. 2
injections: a reference from the spike to `Engine.Core`, and a pin of OpenCvSharp4 in `Engine.Cli`; each one failed
the gate.

**What stays.** V1 adds a store of assets and a textured quad; the notices of the library come with the first program
that distributes it (ADR-0042 item 6).

**A correction.** None.

New tests: 5. The test list holds 1405 (1027 in `Engine.Tests`, 229 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: none. Since the review of v0.141, 2 product milestones
and 6 entries followed it.

## v0.148 — The engine keeps each asset under the SHA-256 of its bytes, in one store of the account (V1b, TASK-0147)

The owner told the session on 2026-10-31 to start V1: "A content-addressed asset store. A textured quad." V1a
(TASK-0146) draws an image on a quad and waits for the visual check of the owner; V1b needs no picture, so it merged
first. On 2026-10-31 the owner chose one store for each account for the bytes of an asset.

**The change.**

- `Engine.Core/Assets/AssetStore.cs` names each asset by the SHA-256 of its bytes, in 64 lower-case hexadecimal digits
  (ADR-0044 item 1). A put writes a scratch file, flushes it and moves it to the name; a read computes the name again
  (item 2). An asset holds at most 16 MiB (item 3). The folder is `<local application data>/3DEngine/assets`, or the
  setting `Engine:AssetFolder`.
- The surface gains `PUT /assets` and `GET /assets/{name}` (item 4), outside the triad of ADR-0008. The command line
  gains `engine asset put <file>` and `engine asset get <name> <file>`, each with `--folder` (item 5).
- Five codes in the new namespace `ASSET`: `E-ASSET-TOO-LARGE`, `E-ASSET-BAD-NAME`, `E-ASSET-NOT-FOUND`,
  `E-ASSET-CORRUPT` and `E-ASSET-WRITE-FAILED`, in `docs/diagnostics.md` and in `/schema/diagnostics`.

**Proof.** 6 new test cases in `Engine.Tests/Assets/AssetStoreTests.cs`. 5 injections: no hash check on a read, no
limit of size, an upper-case name, a missing asset with the status 500, and a path as a name; each one failed a test.

**What stays.** No command names an asset yet (V2), nothing removes an asset, and a copied document file does not carry
its assets (ADR-0044 item 6).

**A correction.** None.

New tests: 6. The test list holds 1411 (1033 in `Engine.Tests`, 229 in `3DEngine.Tests`, 146 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: 5. Since the review of v0.141, 3 product milestones and 7
entries followed it.

## v0.149 — The scene draws an image on a quad with the depth of the bodies (V1a, TASK-0146)

The owner told the session on 2026-10-31 to start V1: "A content-addressed asset store. A textured quad." V1b merged
first at v0.148. On 2026-10-31 the owner accepted the picture of V1a: "yes, close V1a". This entry closes V1.

**The change.**

- `3DEngine.Core/Rendering/ImageQuad.cs` holds an image name, a corner and two edges in double precision (ADR-0043
  item 1). Its model-view matrix subtracts the eye in double precision before the conversion to float, as the bodies
  do (item 2).
- `3DEngine.Vulkan/ImageRenderer.cs` and the shaders `image.vert` and `image.frag` draw an opaque image on each quad,
  on both faces, with the depth test and the depth write of the bodies (item 3). An image uploads once for each name (item 4);
  the renderer holds at most 16 images, and a quad of an absent image is not drawn.
- `3DEngine/SceneRenderer.cs` draws the quads after the bodies (item 3).

**Proof.** 9 new test cases: 8 in `3DEngine.Tests/ImageQuadTests.cs` and 1 in
`3DEngine.ImageTests/ImageQuadImageTests.cs`, which writes `image-quad.png`. 5 injections: no depth test, a second
upload that replaces the image, an absent image that throws, a matrix with no subtraction of the eye, and the
subtraction in float; each one failed a test. The first image test missed the injection with no depth test, because
its point behind the box fell in the blend of red and blue; it compares two points of the face of the box now.

**What stays.** The desktop window does not draw a quad, no command names an asset, and an image file is not decoded
(V2). A transparent image, a mip map and the release of an image are out of scope (ADR-0043).

**A correction.** None.

New tests: 9. The test list holds 1420 (1033 in `Engine.Tests`, 237 in `3DEngine.Tests`, 147 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: 0. Since the review of v0.141, 4 product milestones and 8
entries followed it.

## v0.150 — The codebase review of 2026-10-31 (c) (governance, TASK-0148)

The review of 2026-10-31 (b) examined v0.141. After it came 4 product milestones (R11b, V0, V1b and V1a) and 8
entries, so the next product milestone would make the review late. On 2026-10-31 the owner told the session: "do the
review, then start V2", and to work alone until 2026-11-01 10:00 with its recommendation for each question.

**The record.** `docs/reviews/2026-10-31-c-codebase-review.md` examines `main` at `e851093`, the merge of v0.149. Two
agents probed the code in parallel at `b410a59`, the same tree: the engine with the hosts, and the desktop with the
gates. I ran their 23 probes again, injected T117 with a control, and read each cited line.

**The findings.** 15 new findings: two medium (E113, the store takes a name that ends in a line feed; T117, an imported
file pins the vision library into the engine and the gate passes) and thirteen low (E114 to E118, V65 to V67, T118 to
T122). E106 to E112, V63, V64 and T110 to T116 are fixed for their forms, and the owner accepted E25 and E27. Open
findings: 0 critical, 5 high, 16 medium and 29 low.

**The order.** No new finding is high, so no task comes before V2. TASK-0149 (P0.63) holds the engine findings,
TASK-0150 (P0.64) the gate findings and TASK-0151 (P0.65) the desktop findings, after V2. The standing answer of the
owner takes Q1 A (the Document holds a record of each placed image), Q2 A (a calibration records two points and a
distance) and Q3 A (the client decodes the image); the task of V2 records them in its ADR.

**A correction.** None.

New tests: none. The test list holds 1420 (1033 in `Engine.Tests`, 237 in `3DEngine.Tests`, 147 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15: R-0047 records the lag test of the events, which failed on Windows in the first run
of this branch. New diagnostic codes: none. Since the review of v0.149, 0 product milestones and
1 entry followed it.

## v0.151 — The Document holds an image on a datum, and a command calibrates it to real units (V2a, TASK-0152)

The owner told the session on 2026-10-31: "do the review, then start V2". The review of v0.150 asked three questions,
and the standing answer of the owner takes Q1 A (the Document holds a record of each placed image), Q2 A (a calibration
records two points and a distance) and Q3 A (the client decodes the image). V2 changes the public shape of
`Engine.Contracts`, so ADR-0045 comes with it.

**The change.**

- `Engine.Contracts/Images/ImageRecord.cs` holds the asset, the datum, the origin, the width and the height in pixels,
  and the scale in millimetres for each pixel (ADR-0045 items 1 and 2). `Document.Images` holds each live image, and
  `CommandHandlerResult` gains `CreatedImages` and `ChangedImages` (items 3 and 7).
- `PlaceImage`@1 places an image of the store on an origin plane, and `CalibrateImage`@1 sets the scale from two points
  of the image and a real distance, with `+ - * / sqrt` only (items 4 and 5). The handlers do not read the store.
- The events `image.created`, `image.changed` and `image.removed` carry the record and come after the sketch events;
  a step records its images for undo and redo; the snapshot gives `images` (items 8 to 10). ADR-0045 amends ADR-0012,
  ADR-0031 and ADR-0033.
- Seven codes in the new namespace `IMAGE`, in `docs/diagnostics.md` and in `/schema/diagnostics`. An origin that is
  not finite gives `E-GEOM-INVALID-PARAM`.
- E113 of the review: the pattern of a name of an asset ends with `\z`. TASK-0149 keeps the tests of the route and of
  the command line.

**Proof.** 16 new test cases: 8 in `Engine.Tests/Images/ImageCommandTests.cs`, 1 in
`Engine.Tests/Http/HttpImageScenarioTests.cs`, 7 in `Engine.Tests/Hosting/ParameterBinderTests.cs`, and a line feed in
`AssetStoreTests`. 9 injections: a bus that lets a handler change the asset, an undo that keeps the placed image, a
product in place of the quotient, no check of the datum, a snapshot with no image, an event with no scale, `$` in place
of `\z`, and two faults of the integer field; each one failed a test.

**What stays.** The desktop does not show a placed image, and no client decodes a file (V2b). No command moves, turns or
removes an image.

**A correction.** `ParameterBinder` refused each JSON number for a field of the type `integer`, because the HTTP surface
gives each number as a double (`JsonParameters.AsNumber`). No command had an integer field before `PlaceImage`, so no
test saw it; the HTTP test of this task found it. A whole double up to 2^53 is an integer now.

New tests: 16. The test list holds 1436 (1049 in `Engine.Tests`, 237 in `3DEngine.Tests`, 147 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 7. Since the review of v0.149, 1 product milestone and 2
entries followed it.

## v0.152 — The desktop places a PNG file on a datum, draws it, and calibrates it with two taps (V2b, TASK-0153)

V2a (v0.151) put the record of an image on a datum in the Document. The standing answer Q3 A of the review of v0.150
says that the client decodes the image. The owner told the session on 2026-10-31 to work alone until 2026-11-01 10:00
and not to wait for the visual check, so the session judged the pictures and sends them. This entry closes V2.

**The change.**

- `3DEngine.Core/Imaging/PngReader.cs` reads a PNG file of 8 bits for each channel with no interlace, in the five colour
  types, with the five filters and the CRC of each chunk, on `System.IO.Compression` only, at most 4096 pixels on a side
  (ADR-0046 item 1). No new package.
- `3DEngine/SessionImages.cs` follows the live images and their events, uploads the bytes of each asset of the store one
  time, and gives the quad of each image; `3DEngine/ImagePlacement.cs` gives the quad and the pixel of ADR-0045 item 2
  (ADR-0046 item 2). The surface gives the desktop its store.
- The window "Images" places a PNG file on an origin plane, and calibrates an image with two taps of the scene on it and
  a real distance; during a calibration a tap does not select (`3DEngine/ImageActions.cs`, items 3 and 4).

**Proof.** 24 new test cases: 20 in `3DEngine.Tests/PngReaderTests.cs`, 4 in `3DEngine.ImageTests/ImageActionsTests.cs`,
and 1 image test, `3DEngine.ImageTests/PlacedImageImageTests.cs`, which writes `placed-image.png`. 10 injections; the
first mutant of the Paeth predictor gave the same result for each input, and the second one passed until a test with a
tie of up and the corner. The live window drew a PNG file of an independent encoder over HTTP, with no message of the
validation layer.

**What stays.** A mark of the taps, a move, a rotation and a removal of an image, and a format other than PNG. A picture
of a camera is often larger than 4096 pixels on a side.

**A correction.** The first place of the window of the images was under the panel, and in a window 720 pixels high it
hid Show all; it is to the right of the panel and closed at the start now.

New tests: 24. The test list holds 1461 (1049 in `Engine.Tests`, 257 in `3DEngine.Tests`, 152 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 0. Since the review of v0.149, 2 product milestones and 3
entries followed it.

## v0.153 — The store of the assets gives a code for each error and repairs a damaged file, and an arc follows the solver (P0.63, TASK-0149)

The codebase review of v0.150 found E113 (medium) and E114 to E118 (low) in the engine and the hosts, after V2. V2a
corrected the pattern of E113 at v0.151.

**The change.**

- E114: `AssetStore.Read` gives `E-ASSET-READ-FAILED` for a folder under a name, a locked file or a file that the
  account cannot read, and `AssetStore.Put` gives it for a stream that fails. The route gives a 500 with the envelope,
  and the command line the exit code 1.
- E115: a put of the right bytes writes a damaged file again, and the move replaces it. E116: each way out of a put
  deletes its scratch file, a cancellation too.
- E117: a live add of an arc refuses a start or an end within `SketchSolver.Tolerance` of the centre; an entry of a log
  keeps the rule of 0 (E103).
- E118: `PUT /assets` refuses a length over 16 MiB before the read, and for a body with no length it lifts the request
  limit of the server, so the store stops the body at its own limit with `E-ASSET-TOO-LARGE`.
- ADR-0044 item 2 names the new code.

**Proof.** 4 new test cases in `Engine.Tests/Assets/AssetStoreTests.cs` and `Engine.Tests/Commands/EngineDebtTests.cs`,
with the line feed of E113 on the route and the command line. 7 injections, one for each fix; each one failed a test.

**What stays.** No test sends a body over the limit through a socket: the server answers and closes the connection
while the client still sends, and the client then fails before it reads the reply.

**A correction.** The first fix of E118 caught `BadHttpRequestException` in the route. Its test showed that the new
catch of E114 in the store took the exception first, because it is an `IOException`, and gave a 500; the route now lifts
the limit of the server instead.

New tests: 4. The test list holds 1465 (1053 in `Engine.Tests`, 257 in `3DEngine.Tests`, 152 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 1. Since the review of v0.149, 2 product milestones and 4
entries followed it.

## v0.154 — The pin rules read each imported file, the sink gate reads the desktop, and the next forms of the sixteenth review fail (P0.64, TASK-0150)

The codebase review of v0.150 found T117 (medium) and T118 to T122 (low) in the gates, after V2.

**The change.**

- T117: `Only_The_Vision_Spike_Pins_The_Vision_Library` and `Each_Binding_Has_One_Pin_In_The_Vulkan_Layer` read the
  `PackageReference` items of each file that MSBuild reads for a project, as the other rules of the dependency gate do.
- T118: `EventSinkContractTests` fails when a project that reaches `Engine.Core`, the project of `IEventSink`, is not in
  its folder and no named test reads it; `3DEngine.ImageTests/DesktopSinkContractTests.cs` reads the sinks of the desktop.
- T119: the check of an item name decodes each escape of MSBuild. T120: each constant `char` E, W or I is a part of a
  code. T121: the control byte gate refuses the Unicode set `Default_Ignorable_Code_Point`, with U+034F as text and the
  byte order mark at the start of a file. T122: an underline of `=` or `-` at any indent after a line with a version
  fails the counter of the review.

**Proof.** 13 new test cases: 12 in `Engine.Tests` and 1 in `3DEngine.ImageTests`. The old gates passed each new
form. Four injections in real files fail a gate now: an imported `.props` file with `OpenCvSharp4` in `Engine.Core` and
with `Vortice.Vulkan` in `3DEngine.Core`, a throwing sink in `3DEngine`, and a list with no entry for `3DEngine`.

**What stays.** The set of T121 is the set of Unicode 16; a later version of Unicode can add a character.

**A correction.** The first set of T121 named U+FEFF, and the gate then refused the byte order mark at the start of a
file: two test cases and the check of the tracked files failed. The rule of the category Format keeps its exception.

New tests: 13. The test list holds 1478 (1065 in `Engine.Tests`, 257 in `3DEngine.Tests`, 153 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 0. Since the review of v0.149, 2 product milestones and 5
entries followed it.

## v0.155 — A cancelled press gives nothing, and the mark of the snap is where the point goes and only while a finger is down (P0.65, TASK-0151)

The codebase review of v0.150 found V65 to V67 (low) in the desktop, after V2. With this entry each finding of that
review has its task done.

**The change.**

- V65: `IntentMachine` gives no click and no tap for the release of a press on a handle that Escape cancelled; the
  rest of the press gives nothing, as the comment of `CancelGizmo` says.
- V66: `SketchTool.Target` is a method of the tool. For the end of an arc it gives the point on the circle of the start,
  which the tap adds, and null where the tap ends the curve; the tap and the target share `ArcEnd`.
- V67: `3DEngine.Core/Input/HoverMark.cs` clears the mark after a lift of a finger or a pen and after a cancel, and the
  host clears it on a change of mode. The comment of `SketchHoverIntent` says that a finger and a pen give it while
  they are down.
- ADR-0041 item 6 says the two rules of the mark.

**Proof.** 11 new test cases in `3DEngine.Tests`: `GizmoTests`, `SketchToolTests` and `HoverMarkTests`. 4 injections,
one for each rule; each one failed a test.

**What stays.** No test drives the host, so the clear on a change of mode has no test.

**A correction.** The comment of `SketchHoverIntent` said that a touch and a pen give no hover; R11b gave them one.

New tests: 11. The test list holds 1489 (1065 in `Engine.Tests`, 268 in `3DEngine.Tests`, 153 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 0. Since the review of v0.149, 2 product milestones and 6
entries followed it.

## v0.156 — The lag test of the events fills the queue of the subscriber with a count and no timing (P0.66, TASK-0154)

Register entry R-0047, which the review of v0.150 opened: a run of a change of documents only failed on Windows in
`EventsEndpointLagTests`, because the queue overflowed only when the posts came faster than a pump delay of 200 ms.

**The change.**

- `Engine.Api.Http/WebSockets/SubscriberOptions.cs`: `PumpGate`, a task that the pump of a subscriber awaits before each
  send, replaces `PumpDelay`. Production keeps it null, as it kept the delay at zero.
- `Engine.Tests/Http/EventsEndpointLagTests.cs` lets the pump send the reset and holds it while four events overflow a
  channel of two, then opens the gate and reads the close `subscriber.lagged`.
- R-0047 is closed.

**Proof.** With a pause of 500 ms between the posts, the old test failed in 16 s and the new one passed in 2 s. Five
runs of the lag test and of the heartbeat test passed.

**What stays.** The rule of ADR-0005 §6 does not change: a subscriber that lags is disconnected after its queued
messages. The heartbeat test still measures time.

**A correction.** The comment of `SubscriberOptions` said that the delay slowed the pump "deterministically"; it was a
delay, so the result depended on the speed of the runner.

New tests: 0. The test list holds 1489 (1065 in `Engine.Tests`, 268 in `3DEngine.Tests`, 153 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: 0. Since the review of v0.149, 2 product milestones and 7
entries followed it.

## v0.157 — A contour proposal of a placed image becomes an editable sketch (V3, TASK-0155)

On 2026-10-31 the owner gave the order: V3, then the cross-platform determinism hash in CI, then an STL export of the
live bodies, then the review when it is due, with the recommended answer to each question.

**The change.**

- `3DEngine.Core/Vision/ContourProposal.cs` proposes the outer contour of the largest region of an image: the grey in
  integers, the threshold of Otsu, the region of the class that touches the border less, the trace of Moore with the
  stop of Jacob, and the simplification of Douglas and Peucker to at most 200 vertices (ADR-0047 items 1 and 2).
- The window "Images" has "Propose contour" for each live image; the proposal shows over the scene, and Accept sends
  `CreateSketch`, `AddSketchPoint` and `AddSketchLine` on the datum of the image, and makes the sketch active (items
  3 and 4). Reject drops it.
- ADR-0047 amends ADR-0042 item 3: the vision library stays in the spike. Its Windows payload links about twelve
  libraries statically and has an FFmpeg file under LGPL v2.1, and the macOS payload links FFmpeg; register entry
  R-0048 asks the owner about these licences.

**Proof.** 11 new test cases: 7 in `3DEngine.Tests/ContourProposalTests.cs`, 3 in
`3DEngine.ImageTests/ContourAcceptTests.cs` and 1 image test, `ContourImageTests`, which writes `contour.png`: the six
points and lines of an L-shaped part lie on its edges. 7 injections, one for each rule; each one failed a test.

**What stays.** A hole, more than one region, an arc or a constraint from a contour, one step of undo for one accept,
and the vision library in the desktop (R-0048).

**A correction.** The first threshold injection passed, because each test image had two greys; a test with a ring of a
third grey holds the threshold now. The first full run of the image tests crashed the test host: the new image test
drove Dear ImGui out of the collection of the panel tests.

New tests: 11. The test list holds 1500 (1065 in `Engine.Tests`, 275 in `3DEngine.Tests`, 157 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 15 of 15. New diagnostic codes: 0. Since the review of v0.149, 3 product milestones and 8
entries followed it.

## v0.158 — Each runner hashes a native replay with geometry and solved sketches, and a last job compares the hashes (P0.67, TASK-0156)

On 2026-10-31 the owner gave the order: after V3, the cross-platform determinism hash in CI, and close the oldest
register entry that the work answers. Anti-objective 16 says that a replay gives the same result on each platform.

**The change.**

- `Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs` replays a log with a cut, two moves and an extrude of a
  sketch with six constraints on the native backend, and writes the SHA-256 of the bits of each mesh, each bounding box
  and each solved point, with a summary line for each body and each point, to the file of `DETERMINISM_HASH_FILE`.
- The job "Gate" keeps the file of each runner as an artifact, and the new job "Compare the determinism hashes" compares
  the three with `eng/compare-determinism.js`: a missing runner or two different hashes fail it, with the lines that
  differ as annotations.
- Register entry R-0023, opened 2026-09-25, is closed: each runner compared with its own constants only.

**Proof.** The script passed three equal files and failed a changed point, a changed hash and a missing runner. The
three runners of the pipeline give one hash, 61cfb8ff75aceb8466a27a3e608ea3bdd1a4aca399e1a1674b3a979ecfecad8f: the
native meshes, the boxes and the solved points are the same bits on Windows x64, Linux x64 and macOS arm64.

**What stays.** One log only, and no baseline in the repository, so a change on the three runners at once passes.

**A correction.** The first two runs failed with equal numbers: `StringBuilder.AppendLine` wrote a carriage return and a
line feed on Windows, so Windows had its own hash. Each line ends with a line feed now.

New tests: 1. The test list holds 1501 (1066 in `Engine.Tests`, 275 in `3DEngine.Tests`, 157 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: 0. Since the review of v0.149, 3 product milestones and 9
entries followed it.

## v0.159 — The live bodies leave the engine as one binary STL file, from the surface, the command line and the desktop (P8g, TASK-0157)

On 2026-10-31 the owner gave the order: after V3 and the determinism hash, "an STL export of the live bodies". P0.67
(v0.158) showed that the native triangles are the same bits on the three platforms.

**The change.**

- `Engine.Core/Export/StlExport.cs` writes each live body, in the order of its handle, as binary STL: a header of 80
  bytes, the count, and for each triangle the unit normal by the right-hand rule and the three vertices as floats, in
  millimetres (ADR-0048 items 1 and 2). A backend with no tessellation gives `E-GEOM-CAP-MISSING`, and a tessellation
  that fails gives `E-GEOM-NATIVE-OP`.
- `DocumentSession.ReadGeometry` gives the Document and its backend in the serial section (item 3).
- `GET /export/stl` gives the file as `model/stl`, or 501 and 500 with the envelope (item 4).
  `engine export stl <document.3de> <file.stl>` opens a document file and writes the file (item 5). The button
  "Export STL" of the desktop writes it in the document folder (item 6).

**Proof.** 4 new test cases in `Engine.Tests/Export/StlExportTests.cs`: a box gives twelve triangles whose normals are
unit vectors out of it and whose vertices are positions of the tessellation; two boxes, an empty document and the
managed backend; the route; the verb. 7 injections, one for each rule; each one failed a test.

**What stays.** ASCII STL, colours, the visible bodies only, and STEP (K6). No other program read a file of the export.

**A correction.** The test of the verb found that the export held the document file after it, because its session
stayed open; the verb closes its session now.

New tests: 4. The test list holds 1505 (1070 in `Engine.Tests`, 275 in `3DEngine.Tests`, 157 in
`3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). `dotnet build 3DEngine.sln --no-incremental` gives zero errors and
zero warnings. Open entries: 14 of 15. New diagnostic codes: 0. Since the review of v0.149, 4 product milestones and 10
entries followed it.
