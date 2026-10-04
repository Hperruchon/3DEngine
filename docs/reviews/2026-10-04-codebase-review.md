---
date: 2026-10-04
commit: ae5c521
ledger: v0.40
previous: 2026-09-30-codebase-review.md
---

# Codebase review — 2026-10-04

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the third codebase review. The next review must come before ledger entry v0.47: the build
fails when a seventh milestone follows v0.40. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds that rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `ae5c521`, the merge of v0.39 and v0.40 |
| Branch of this review | `codebase-review-2026-10-04` |
| Tests | 242 on this computer |
| Pipeline | Run 37165652108, a push to `main` at `ae5c521`: five jobs, each one a success |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** `git diff --stat 14e2c73 ae5c521` gives 26 changed files outside `docs/` and `tasks/`,
and none in `3DEngine/`, `3DEngine.Vulkan/`, `3DEngine.Core/` or `Engine.Contracts/`. Three review
agents therefore read the code in parallel, and not four: the engine kernel, the two hosts, and the
tests with the pipeline. They ran probes outside the repository, ran the real host on the loopback
address, and injected violations into the working tree and removed each one. I read each line that
this review cites, on disk, in this session. I repeated three runs myself: the bypass of the bind
guard (E23), and the two gate holes of T9. A line that says "a review agent ran it" means that I read
the cited code and did not repeat the run. I ran a clean build and each test, and I read the public
run list of the pipeline.

## Summary

- **TASK-0034 closed four findings and made one critical finding history.** Queries, commands and
  the WebSocket handshake now pass through one serial section. Findings E2, E4 and E9 are fixed, and
  E10 is fixed in part. The probe of the last review passes 100 runs of 100. **[Observed]**
- **TASK-0044 closed two findings.** The heartbeat sends one frame in each interval (E8), and the
  host refuses a foreign Origin, a foreign Host and an address that is not a loopback address
  (E15). **[Observed]**
- **The bind guard has a second door.** It reads the configuration key `urls` only. An address given
  through `Kestrel:Endpoints` passes with no refusal (E23). The surface has no authentication.
  **[Observed]** in a run.
- **The session closes the old race and opens three latent defects.** A function given to `Read`
  can keep the Document and write to the sink after the section ends (E19). A call back into the
  session from a sink blocks every client (E20). A sink that throws leaves a gap in the sequence,
  and no code sends the reset that the comment promises (E18). **[Observed]** in runs of a review
  agent.
- **A handler that fails after it changed the backend leaves an orphan solid** (E21). The backend
  and the Document then disagree, and a retry of the same command is refused. **[Observed]**
- **The pipeline has no time limit.** A test that hangs runs for six hours, and the annotation of
  TASK-0045 never comes (T8). **[Observed]**
- **The project file of the tests can turn the gates off.** It is not a gate file, so each task that
  permits `Engine.Tests/**` may change it. A `Directory.Build.props` file gets past the dependency
  gate and the x86 gate (T9). **[Observed]** in my runs.
- **Forty findings are open: one critical, eleven high, twenty-one medium and seven low.** Six
  earlier findings are fixed. Thirteen findings are new. **[Observed]**
- **I used one hole myself.** Commit `4715b88` changed a test under TASK-0034 after that task was
  `Done`, and the gate passed, because it does not read the status of the named task (T3).
  **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. The
values are for `ae5c521`. A source line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 9 | `Project(` lines in `3DEngine.sln`. The last review gave 9. |
| Production source lines | 5,179 | Each project except `Engine.Tests`, 98 files. The last review gave 4,915 in 97 files. |
| Test source lines | 6,804 | `Engine.Tests`, 56 files. The last review gave 5,900 in 52 files. |
| Tests | 242 | `dotnet test 3DEngine.sln --no-build`: 242 passed, 0 failed, 0 skipped. The last review gave 214. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`, with the entries that this review adds. The count before the review was 12. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19. |
| Findings: critical | 1 | Each finding that is open at this commit, from any review: E1. The last review gave 2. |
| Findings: high | 11 | The same method: E3, E7, V1, V2, V3, V5, T1, T3, T4, T8, T9. The last review gave 12. |
| Findings: medium | 21 | The same method: E5, E6, E10, E11, E12, E13, E14, E16, E18 to E23, V4, V6, T2, T5, T10, T11, T12. The last review gave 14. |
| Findings: low | 7 | The same method: E17, E24, V7, T7, T13, C1, P1. The last review gave 5. |

Two more measures. The lines of each production project: `Engine.Core` 1,480, `Engine.Api.Http`
1,194, `3DEngine.Vulkan` 1,080, `Engine.Contracts` 397, `Engine.Cli` 388, `Engine.Geometry.Manifold`
275, `3DEngine.Core` 221, `3DEngine` 140, `eng` 4. The tests of each area, from the `.trx` file of the
run: Governance 58, Http 58, Hosting 25, Geometry 24, Cli 20, Commands 16, and 41 in the other areas.

## Findings of the previous review

Each line gives the state at `ae5c521` and the evidence. "Open" means that the code that the finding
cites did not change, or that it changed and the defect stays.

- **E1** — Open. `Engine.Core/CommandBus.cs:161`, `:183` and `:208` advance the version on an apply,
  a rejection and a cancellation. `Engine.Contracts/Document.cs:11-13` gives that meaning. A review
  agent ran it: a rejected box and then a box with an expected version give version 3 live and
  version 1, with no body, after the replay. TASK-0035 is `Ready`.
- **E2** — Fixed. Each host reaches the Document through `Engine.Core/DocumentSession.cs`
  (`CommandsEndpoint.cs:90`, `QueriesEndpoint.cs:83`, `EventsEndpoint.cs:70-76`, `Engine.Cli/Cli.cs:84`,
  `:131`). The probe test passes 100 runs of 100. TASK-0034, v0.39. Two paths around the session stay:
  finding E19.
- **E3** — Open. `Engine.Api.Http/EngineHost.cs:44-46` and `Engine.Cli/Cli.cs:194-196` select the
  backend with no message. TASK-0036 is `Ready`.
- **E4** — Fixed. `Engine.Core/CommandBus.cs:76-77` appends each event with
  `CancellationToken.None` after the commit, and the commit (`:132-174`) does not await. A test
  cancels the token after the log append. TASK-0034, v0.39. A sink that throws gives a new defect:
  finding E18.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:43-44` gives a `double` for each JSON
  number. A review agent ran it: 9007199254740993 becomes 9007199254740992. Register entry R-0027.
- **E6** — Open, in each of its five parts. `Engine.Core/Hosting/ParameterBinder.cs:105-109` uses
  `RoundtripKind`. `Engine.Cli/Cli.cs:86-95` rejects an unknown command before the bus.
  `SchemaEventsEndpoint.cs:19-24` lists five kinds that `Engine.Core` does not emit. ADR-0006 §4, §5
  and §7 have no code. The command-line host keeps no state. Register entry R-0027.
- **E7** — Open. `Engine.Core/Replay.cs:24` defaults to the null backend, and `:26-29` discard each
  result. Register entry R-0028. Question Q1.
- **E8** — Fixed. `Engine.Api.Http/WebSockets/Subscriber.cs:136` moves the time of the last send when
  the loop writes a frame. A review agent ran the real host: one idle subscriber for 66 seconds
  received one heartbeat at second 30 and one at second 60. TASK-0044, v0.37.
- **E9** — Fixed. The handshake runs inside `Session.Read` (`EventsEndpoint.cs:70-76`), and a commit
  holds the session across its change and its publication. `HttpConcurrencyTests` asserts that the
  first live `seq` after a reset is the snapshot version plus one. TASK-0034, v0.39.
- **E10** — Fixed in part. `Engine.Core/CommandBus.cs:48-50` refuses a second bus on one Document.
  `Engine.Contracts/Document.cs:39-43` still accepts a lower version, because TASK-0034 forbade
  `Engine.Contracts`. TASK-0035 modifies that file. Register entry R-0028.
- **E11** — Open. `ParameterBinder.cs:67-76` has no check for a finite value. A review agent ran it:
  `sizeX` equal to `1e999` gives `Applied` over HTTP and in the command line, and a later query gives
  HTTP 500. Register entry R-0028.
- **E12** — Open. `ParameterBinder.cs:67-76`. A review agent ran it: `10L` and `10` are refused for a
  number field. Register entry R-0027.
- **E13** — Open. `QueriesEndpoint.cs:84` and `Engine.Cli/Cli.cs:157` still ask for `Aabb`. A review
  agent ran it: a query for an absent body gives six zeros. TASK-0028.
- **E14** — Open. `ManifoldGeometryBackend.cs:72` allocates with `AllocHGlobal`, and
  `Native/ManifoldSolidHandle.cs:20` releases with `manifold_delete_manifold`. Register entry R-0028.
- **E15** — Fixed. `Engine.Api.Http/Program.cs:15` refuses an address in `urls` that is not a
  loopback address, `:52-67` refuses a foreign Origin, and `:33-40` filter the Host. TASK-0044,
  v0.37. A second way to give an address passes: finding E23.
- **E16** — Open. `Engine.Api.Http/Json/ApiJson.cs:14-24` sets no rule for an unmapped member. A
  review agent ran it: `expectedDocumentVersoin` gives `Applied`. Register entry R-0028.
- **E17** — Open in eleven parts of thirteen. Fixed: the close status, because `docs/diagnostics.md:20`
  now says 1007 by the decision of the owner; and a subscriber that the handshake leaves, because
  `EventsEndpoint.cs:57-88` detaches and disposes it in a `finally`. Open, each one run by a review
  agent: a replay through the idempotency cache; a handler exception that leaves the bus (finding
  E21 now holds it); `E-QRY-UNKNOWN` for a wrong result type (`QueryBus.cs:61`); a JSON object as a
  string; an internal type name in a response and HTTP 500 for an unknown `charset`; a pump that
  cancels before the close frame, so that a lagged client on Kestrel gets no close frame; text that is
  not ASCII in the command line; a flag read as a name; an empty body for 415; a subscribe read with
  no time limit; a negative cursor; no link to the host shutdown, which held a stop for 31 seconds;
  and a repeated JSON key. Register entry R-0028.
- **V1** — Open. No file of `3DEngine.Vulkan/` changed after `14e2c73`. Register entry R-0022.
- **V2** — Open. The same evidence. Register entry R-0022.
- **V3** — Open. The same evidence. Register entry R-0022.
- **V4** — Open. No file of `3DEngine/` changed after `14e2c73`. Register entry R-0027.
- **V5** — Open. The same evidence as V1. Register entry R-0030.
- **V6** — Open. The same evidence as V1. Register entry R-0030.
- **V7** — Open. No file of `3DEngine.Vulkan/`, `3DEngine/` or `3DEngine.Core/` changed. Register
  entry R-0030.
- **T1** — Open, in each of its three parts. `Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs:13-16`
  still skips. Register entry R-0023 is open. `Engine.Core/Replay.cs:28` discards the result. Owners:
  TASK-0036, R-0023, TASK-0035.
- **T2** — Fixed in four parts of five. `Engine.Tests/QueryBusTests.cs:39-52` now reads the sink of a
  session (TASK-0034). Open: no type in `Engine.Tests` implements `IGeometryBackend`, so no test makes
  the backend throw through the command bus. Register entry R-0027.
- **T3** — Fixed in two parts of three. The workflow gives `--cc` for a merge commit
  (`.github/workflows/ci.yml:239-249`), and a gate file needs its exact name
  (`WriteSetGateTests.cs:176-181`). Open: the gate does not read the status of the named task
  (`:233-237`). My commit `4715b88` changed a test under TASK-0034, which was `Done`. Register entry
  R-0031. Question Q3.
- **T4** — Fixed in one part of four, and in part in two more. The dependency gate reads each project
  file as XML (`DependencyDirectionGateTests.cs:38-52`); a new form passes, finding T9. The determinism
  gate refuses `double.Pow`, the static import and a call split after the parenthesis
  (`DeterminismCallGateTests.cs:33-43`); a review agent injected four forms that still pass: an alias
  of `System.Math`, a line break before `.Pow`, a `//` inside a string on the same line, and a generic
  `T.Sin`. The marker gate refuses `R-9999` (`MarkerGateTests.cs:37-61`); a closed identifier and the
  template `R-0000` pass, and `nuget/.gitignore:2` is not read. Open: the dispatch gate reads two
  projects by name (`DispatchSurfaceGateTests.cs:18`). Register entry R-0031.
- **T5** — Fixed in two parts of three. A wrong status fails, and a register with no fence fails;
  a review agent injected each one. Open: `RepositoryFiles.cs:48` keeps the quotes of an `affects`
  item. Register entry R-0031.
- **T6** — Fixed. Each job sets `shell: bash` (`ci.yml:41-45`, `:120-122`, `:173-175`), and each git
  command is tested. One `|| true` remains at `ci.yml:251`; the gate then falls back to the touched
  task file, so it fails closed.
- **T7** — Open, in each of its five parts, and one part is larger. `AdrGateTests.cs:199` compares the
  status only. `AdrGateTests.cs:214` reads `docs/adr/` with no recursion. The register gate reads the
  clock, and five entries now have the limit 2026-12-24: R-0020, R-0021, R-0024, R-0025 and R-0031.
  `CodebaseReviewGateTests.cs:100` accepts an empty value, and `:120-122` skip the check for
  `previous: none`. A path with a wrong case passes on Windows. Register entry R-0031.
- **C1** — Fixed in two parts of four. TASK-0033 deleted the web shell, and `Engine.Api.Http/Program.cs`
  no longer cites the removed clamp. Open: `IBRepOps` and `IFeatureIdMap` have no implementation, no
  production code reads a flag of `BackendCapabilities`, and `Engine.Api.Http/EngineHost.cs:14-15`
  still cites the clamp. Register entry R-0027.
- **P1** — Open, and the cost is larger. `EventBroadcaster.cs:78` projects the snapshot inside the
  lock of the broadcaster, and since TASK-0034 also inside the session, so a large snapshot now
  delays queries too. Nothing is measured. Register entry R-0027.

## 1 Current architecture

**[Observed]** The projects and the references are those of the last review. One type is new:
`Engine.Core/DocumentSession.cs` owns the Document, the backend, the command bus, the query bus and
the sink, behind one `SemaphoreSlim`. Each host builds one session and reaches the engine through it.
The command bus keeps its own semaphore for `Replay` and the tests, and it refuses a second bus on one
Document with a static weak table (`CommandBus.cs:25`).

**[Observed]** The commit order changed: the bus changes the Document, stores the result in the
cache, and then appends the events with `CancellationToken.None` (`CommandBus.cs:60-77`). The owner
chose that order on 2026-09-30.

**[Observed]** The lock order is: the session, the semaphore of the bus, the lock of the sink, the
lock of the broadcaster. The handshake takes the session first and the broadcaster second. A test
with the opposite order injected stopped at its time limit (TASK-0034).

**[Observed]** Three records still wait for their code: ADR-0019 (TASK-0038), ADR-0020 (TASK-0035) and
ADR-0021 (TASK-0037). ADR-0018 waits for TASK-0028.

## 2 Patterns in use

- **One serial boundary.** It holds for each host path. It does not hold for a caller that keeps a
  reference that `Read` gives (E19). **[Observed]**
- **Change, then publish.** It holds on each path of the bus: apply, rejection and cancellation.
  **[Observed]**
- **Each test fails first.** TASK-0034, TASK-0044 and TASK-0045 record a failure before the change for
  each new behaviour test. **[Observed]**
- **A failed test has a public name.** TASK-0045. The first use named the failure that the session of
  2026-10-01 could not find. **[Observed]**
- **A rule in text only.** Three new rules have no gate: `EngineHost.Document` is "for the tests
  only" (`EngineHost.cs:35-38`), a function given to `Read` "must not keep a reference"
  (`DocumentSession.cs:84-86`), and a sink must not call the session (no text at all). **[Observed]**
  `CLAUDE.md` says that a rule in text only is a rule that an agent will break.

## 3 Review of the code

Each finding has an identifier, a severity, a title and a state. An identifier of an earlier review
stays with its finding. The earlier reviews give the full evidence of each earlier finding.

### Correctness of the engine

#### E1 · Critical · A replay can rebuild a different Document · Open

TASK-0035. A review agent ran it again at this commit with the same result. **[Observed]**

#### E2 · Critical · Queries read shared state while a command writes it · Fixed

TASK-0034, v0.39. Two paths around the session stay: finding E19. **[Observed]**

#### E3 · High · The backend changes with no message · Open

TASK-0036. **[Observed]**

#### E4 · High · The commit uses a cancellable token after the log append · Fixed

TASK-0034, v0.39. **[Observed]**

#### E5 · Medium · An integer from HTTP JSON can never bind · Open

Register entry R-0027. **[Observed]**

#### E6 · Medium · Other engine findings · Open

Five parts, each one unchanged. Register entry R-0027. **[Observed]**

#### E7 · High · A replay discards each result, so a divergence is silent · Open

TASK-0035 changes `Replay.cs` and does not name this finding. Question Q1. **[Observed]**

#### E8 · High · An idle subscriber receives 1,025 heartbeat frames and not one · Fixed

TASK-0044, v0.37. **[Observed]**

#### E9 · High · A subscriber that connects during a commit can lose an event or receive it two times · Fixed

TASK-0034, v0.39. **[Observed]**

#### E10 · Medium · A second bus on one Document starts the sequence at 1 again · Fixed in part

The bus is refused. `Document.AdvanceVersion` still accepts a lower value. TASK-0035. **[Observed]**

#### E11 · Medium · A value that is not finite enters the log, and the host answers with HTTP 500 · Open

Register entry R-0028. A review agent ran it at this commit. In the environment `Development`, the
body of the HTTP 500 holds the stack and the local paths. **[Observed]** in the code.

#### E12 · Medium · The binder refuses a whole number for a number field · Open

Register entry R-0027, with E5. **[Observed]**

#### E13 · Medium · A failed query returns a box of zeros and not null · Open

TASK-0028. **[Observed]**

#### E14 · Medium · The Manifold wrapper frees a caller buffer with the C++ `delete` · Open

Register entry R-0028. **[Observed]** in the code.

#### E15 · Medium · The host has no bind rule, no Origin check and no Host check · Fixed

TASK-0044, v0.37. Finding E23 holds the second way to give an address. **[Observed]**

#### E16 · Medium · The request envelope ignores an unknown member · Open

Register entry R-0028. **[Observed]**

#### E17 · Low · Other engine and host findings · Open in eleven parts of thirteen

The list is in "Findings of the previous review". Register entry R-0028. **[Observed]** in the code;
the runs are by a review agent.

#### E18 · Medium · A sink that throws leaves a gap in the sequence, and nothing sends a reset · Confirmed by a run

- **Evidence.** The bus appends the events of a commit in a loop, and the first throw ends the loop
  (`Engine.Core/CommandBus.cs:76-77`). The comment at `:70-71` says that a subscriber recovers the
  lost event with a reset. No code sends a reset for this case, and the check for a resume reads the
  range of the ring and not its gaps (`Engine.Api.Http/WebSockets/EventBroadcaster.cs:52-55`).
  `Engine.Core/InMemoryEventSink.cs:5-7` says that the engine never produces gaps. **[Observed]** in
  the code. A review agent ran it: a sink that throws on `body.created` gives an exception to the
  caller, the ring holds `1`, and after the next command it holds `1, 3`.
- **Impact.** The HTTP client gets HTTP 500 for a command that was applied. A subscriber that
  resumes or that is attached misses `body.created` with no signal, and a host that projects events
  (ADR-0009) misses a body. ADR-0005 forbids a gap. The owner accepted the loss of an event
  (question Q1 of the last review). The recovery that the decision assumed does not exist.
  **[Inferred]**
- **Correction.** **[Recommended]** Either a sink must not throw, by contract and by a test of each
  sink, or the bus continues after a failed append and marks the stream so that the broadcaster
  sends a reset to each subscriber. Question Q4.
- **Owner.** Register entry R-0033.

#### E19 · Medium · A reference to the Document or to the sink can leave the session · Confirmed by a run

- **Evidence.** `DocumentSession.Read` gives the Document and the writable sink to the function
  (`Engine.Core/DocumentSession.cs:87-94`). `Document.Bodies` is a live view of a dictionary, and
  `Document.Log` is the list itself (`Engine.Contracts/Document.cs:16-23`). `EngineHost.Document`
  stays public "for the tests only" (`Engine.Api.Http/EngineHost.cs:35-38`), and
  `EngineKit.CreateQueryBus` builds a bus with no lock (`Engine.Core/Hosting/EngineHosting.cs:67`).
  **[Observed]** A review agent ran it: a view of `Bodies` that left the section counted 0 and then 2
  after two later commands, and a function given to `Read` appended an event with `Seq` 999 to the
  ring.
- **Impact.** The defect of finding E2 can come back with one line of host code, and no test fails.
  **[Inferred]**
- **Correction.** **[Recommended]** `Read` gives a read-only view or takes a projection to copied
  data. It gives a sink interface with `Snapshot` only. The tests read `Session.DocumentId` and
  `Session.Read`, and `EngineHost.Document` goes. A gate refuses `.Document` of the host and
  `CreateQueryBus` outside the tests.
- **Owner.** Register entry R-0033.

#### E20 · Medium · A call back into the session blocks every client · Confirmed by a run, latent

- **Evidence.** The session has one `SemaphoreSlim(1, 1)` and no check for a second entry
  (`DocumentSession.cs:26`, `:60`, `:73`, `:91`). The sink runs inside the section
  (`CommandBus.cs:76-77`). The comment on the lock order (`DocumentSession.cs:16-19`) does not forbid
  a call back. **[Observed]** A review agent ran it: a sink that calls `session.Read` waited for its
  limit on each event.
- **Impact.** No caller does this today. TASK-0038 projects events in the desktop host and can ask a
  query when a body arrives, which stops each client with no error. **[Inferred]**
- **Correction.** **[Recommended]** Detect a second entry with an `AsyncLocal` flag and throw
  `InvalidOperationException`. A test holds the rule.
- **Owner.** Register entry R-0033. TASK-0038 must not ship before this correction.

#### E21 · Medium · A handler that fails after it changed the backend leaves an orphan solid · Confirmed by a run

- **Evidence.** Each handler changes the backend before the commit
  (`Engine.Core/Commands/CreateBoxCommandHandler.cs:67`). The bus catches a cancellation only
  (`CommandBus.cs:117`) and has no rollback, which ADR-0006 §4 requires. `TakeSeqs` (`:137`) can also
  throw after that change. **[Observed]** A review agent ran it: a handler that creates a box and then
  throws gives an exception, a Document with no body, a backend with one body, and no event. A
  `CreateBox` with the same `CommandId` is then rejected with `E-GEOM-NATIVE-OP`.
- **Impact.** The backend of the session differs from the backend of a replay (ADR-0012 §2), native
  memory stays until the end of the session, and a correct retry is refused. **[Inferred]**
- **Correction.** **[Recommended]** A backend result that the bus commits or discards, or a call
  that removes the handle on each path that does not commit. Each handler exception becomes a
  rejection with an event, which also closes a part of E17.
- **Owner.** Register entry R-0033.

#### E22 · Medium · A cancellation from a handler is cached, so a retry never runs · Confirmed by a run, latent

- **Evidence.** The bus turns each `OperationCanceledException` into `Cancelled`, with no check that
  the token of the caller was cancelled (`CommandBus.cs:117`). The cache stores each result, also a
  rejection and a cancellation (`:74`), and ADR-0006 §7 names "applied command IDs". **[Observed]** A
  review agent ran it: a handler that throws `TaskCanceledException` with a live token gives
  `Cancelled`, and a retry with the same identifier gives `Cancelled` with no call to the handler.
- **Impact.** No handler observes the token today. A client that leaves and retries, which is the
  purpose of ADR-0006 §7, gets `Cancelled` until 1,024 other results evict it. A rejection also takes
  a slot of the cache, so a duplicate box after 1,024 other commands gets the misleading code
  `E-GEOM-NATIVE-OP`. **[Inferred]**
- **Correction.** **[Recommended]** Add `when (ct.IsCancellationRequested)`, cache applied results
  only, and read the cache before the token check.
- **Owner.** Register entry R-0033.

#### E23 · Medium · The bind guard reads one source of addresses, and the surface has no authentication · Confirmed by a run

- **Evidence.** `Engine.Api.Http/Program.cs:15` reads the configuration key `urls` only. I ran it:
  `engine-api-http.exe --urls http://192.0.2.1:5880` gives the refusal and exit code 1, and
  `engine-api-http.exe --Kestrel:Endpoints:E1:Url=http://192.0.2.1:5880` gives no refusal, and Kestrel
  tries to bind the address. **[Observed]** The variable `ASPNETCORE_HTTP_PORTS` is a third source;
  no run tested it, so that no run binds each interface. **[Inferred]**
- **Impact.** One variable or one argument puts a surface with no authentication on the network,
  against ADR-0011 and ADR-0019 §5. The containers of the framework set `ASPNETCORE_HTTP_PORTS`.
  **[Inferred]**
- **Correction.** **[Recommended]** Check the addresses that the server bound, at the start, and
  stop with exit code 1. Or bind the loopback address in the code and refuse each other source.
- **Owner.** Register entry R-0033. TASK-0038 must not ship before this correction.

#### E24 · Low · Other engine and host findings of this review · Confirmed, except where the line says so

- `TakeSeqs` compares a `Seq` with the version (`CommandBus.cs:235-245`), which ADR-0020 §2 forbids
  after TASK-0035, and no production path can reach it. **[Observed]** TASK-0035 removes it.
- The static table gives one bus to a Document for its whole life, and `EngineKit.CreateCommandBus`
  (`EngineHosting.cs:59-65`) then throws when a session exists. **[Observed]**
- The comments of both backends say that the cache stops each duplicate handle
  (`ManifoldGeometryBackend.cs:63`, `InProcessMeshBackend.cs:20-21`). The cache holds 1,024 results.
  **[Observed]**
- The heartbeat task starts in the constructor of the subscriber (`Subscriber.cs:63-64`), before the
  handshake, so a frame can come before `subscription.reset` and take a slot that the replay check
  counts as free. A review agent ran it with an interval of 200 ms.
- A client that leaves before its subscribe frame gives an error with a stack in the log
  (`EventsEndpoint.cs:43-49` catches a cancellation only). A review agent saw it in a run.
- The bind guard accepts each address of 127/8, and the Host filter accepts three names
  (`Program.cs:35`, `:115-117`), so `127.0.0.2` gives a host that refuses each request. A review agent
  ran it.
- The Host filter reads `SubscriberOptions.Default`, and the Origin check reads the instance of the
  container (`Program.cs:35`, `:57`). **[Observed]**
- The Origin check accepts each port on a loopback host, so a page of another local web server can
  read the stream. TASK-0044 accepted this. **[Observed]** Record it for TASK-0038.
- Comments that are false: `EventsEndpoint.cs:16` says 1003; `DocumentSession.cs:16-19` gives the
  order "sink, then host", and the handshake takes the broadcaster and then the sink, with no
  deadlock, because a commit never holds both; `EngineHost.cs:14-15` cites the removed clamp.
  **[Observed]**

### The Vulkan layer and the desktop host

No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `14e2c73`. Each finding
keeps its state and its evidence. **[Observed]**

#### V1 · High · `VK_SUBOPTIMAL_KHR` is handled as a failure · Open

Register entry R-0022.

#### V2 · High · The special extent value passes the size check · Open

Register entry R-0022.

#### V3 · High · No portability flag for macOS · Open

Register entry R-0022.

#### V4 · Medium · Other desktop findings · Open

Register entry R-0027.

#### V5 · High · The device selection continues with a null device when no device is suitable · Open

Register entry R-0030.

#### V6 · Medium · A pointer into managed memory goes to Vulkan with no pin · Open

Register entry R-0030.

#### V7 · Low · Other findings in the Vulkan layer and the render kernel · Open

Register entry R-0030.

### Tests and continuous integration

#### T1 · High · The pipeline cannot see three kinds of loss · Open

TASK-0036, register entry R-0023, TASK-0035. **[Observed]**

#### T2 · Medium · Tests that cannot fail, and gates with a short list · Open in one part of five

No test makes the backend throw through the command bus. Register entry R-0027. **[Observed]**

#### T3 · High · The write-set gate has three holes · Open in one part of three

A commit can name a task that is `Done`. Register entry R-0031. Question Q3. **[Observed]**

#### T4 · High · Four gates read text with one expression and miss the other forms · Open in three parts of four

The determinism gate and the marker gate are fixed in part. The dispatch gate is open. Register entry
R-0031. **[Observed]** in the code; the injections are by a review agent.

#### T5 · Medium · A parser that returns nothing makes its gate pass · Open in one part of three

The field `affects`. Register entry R-0031. **[Observed]**

#### T6 · Medium · Two jobs of the workflow hide a failed git command · Fixed

TASK-0043, v0.36. **[Observed]**

#### T7 · Low · Other findings in the gates · Open

Register entry R-0031. Five entries reach their limit on 2026-12-24. **[Observed]**

#### T8 · High · A test that hangs holds the pipeline for six hours and gives no name · Confirmed in the workflow

- **Evidence.** `.github/workflows/ci.yml` has no `timeout-minutes` in a job and no
  `--blame-hang-timeout` (a search gives zero lines). Several waits in the tests have no limit, for
  example a WebSocket receive in the tests of `/events`. **[Observed]**
- **Impact.** A hang runs to the limit of GitHub, 360 minutes. The runner then cancels the step, so
  the branch after `||` never runs, and the annotation of TASK-0045 never comes. **[Inferred]**
- **Correction.** **[Recommended]** `timeout-minutes` on each job, `--blame-hang-timeout` on
  `dotnet test`, and a limit on each receive and each connect in the tests.
- **Owner.** Register entry R-0034. Question Q2.

#### T9 · High · A project file can turn the gates off, and a props file gets past two gates · Confirmed by a run

- **Evidence.** A gate file is a file in `Engine.Tests/Governance/`, in `Engine.Tests/Diagnostics/`,
  or a file whose name ends in `GateTests.cs` (`WriteSetGateTests.cs:206-209`). `Engine.Tests.csproj`
  is none of these. I ran the write-set gate with TASK-0035 and that file: it passes. TASK-0035,
  TASK-0036, TASK-0037 and TASK-0038 each permit `Engine.Tests/**`. I added
  `Engine.Core/Directory.Build.props` with a reference to `3DEngine.Core` and the runtime
  identifier `win-x86`: the dependency gate and the determinism gate pass, 16 tests of 16, because
  they read `*.csproj` only. I removed the file. **[Observed]**
- **Impact.** One line, `<Compile Remove="Governance/**" />`, in the project file removes each gate,
  the write-set gate included, under a task that is open now. A props file breaks the rule that the
  two kernels never reference each other, and determinism rule 5. **[Inferred]**
- **Correction.** **[Recommended]** Add `Engine.Tests/Engine.Tests.csproj`, each `Directory.Build.*`
  file, `eng/write-set-cutoff.txt` and `.github/workflows/**` to the gate files. Make the two gates
  read the items that MSBuild evaluates, or each `.props` and `.targets` file.
- **Owner.** Register entry R-0031. Question Q2.

#### T10 · Medium · A register heading with a wrong form hides an overdue entry · Confirmed by an injection

- **Evidence.** The cross-check of the register gate uses the same pattern as its parser
  (`RegisterGateTests.cs:43`, `:54`, `:199`), so it cannot see a heading that the parser misses.
  **[Observed]** in the code. A review agent injected an overdue R-0030 and a heading `#### R-0031`:
  each register test passed, because the fields of R-0031 replaced the date of R-0030.
- **Impact.** An entry can be overdue, or not counted, with a green build. **[Inferred]**
- **Correction.** **[Recommended]** Count each heading line that holds `R-` and a number, require the
  exact form, and refuse a second `due` line in one entry.
- **Owner.** Register entry R-0031.

#### T11 · Medium · The write-set job reads the rules at the tip and not at each commit · Confirmed in the workflow

- **Evidence.** The job runs the gate of the checked-out tip for each commit of the range
  (`ci.yml:254`). **[Observed]**
- **Impact.** A later commit of one push can widen the write set that judges an earlier commit. A new
  branch replays each commit after the cut-off under the rules of today, so a later change to a rule
  can fail an old commit. **[Inferred]**
- **Correction.** **[Recommended]** Read the task file at the commit (`git show <commit>:<path>`), or
  record the rule of the tip and limit the replay.
- **Owner.** Register entry R-0031.

#### T12 · Medium · Three tests depend on timing on a slow runner · Confirmed in the code, not reproduced

- **Evidence.** `Engine.Tests/Http/HeartbeatTests.cs:15-19` says that a slow runner cannot make a gap
  shorter. The client measures the gap, so a late client reads two frames that wait in the queue
  with a gap of about zero. The collection "real host" has no `CollectionDefinition` (a search gives
  zero lines), so it runs beside the concurrency tests, against the comment at
  `HostGuardTests.cs:20-21`. The deadlock test loops `while (clock.Elapsed < 1 s)`
  (`HttpConcurrencyTests.cs:158`, `:168`), so a task that starts after one second never runs, and the
  assertion that it ran fails. **[Observed]** in the code. No run reproduced a failure.
- **Impact.** A false red run on a slow runner. The Windows run of 2026-10-01 failed in a test of
  this kind; the cause of the Ubuntu run of v0.38 is unknown. **[Inferred]**
- **Correction.** **[Recommended]** Measure the send time of the server, add the
  `CollectionDefinition` with `DisableParallelization`, and use `do`/`while` in the deadlock test.
- **Owner.** Register entry R-0034.

#### T13 · Low · Other findings in the tests and the pipeline · Confirmed, except where the line says so

- `git diff-tree --cc` lists a file for a clean merge with changes on both sides, and the comment at
  `ci.yml:203-205` says that a clean merge lists nothing. A review agent ran it in a scratch
  repository. No merge of this repository has the form today.
- `eng/report-failed-tests.js:54-66` reads `UnitTestResult` only. The reason for a crash of the test
  host, in `RunInfo`, and the outcomes `Error`, `Timeout` and `Aborted` give a general annotation.
  **[Observed]** in the code.
- The marker gate accepts a closed identifier and the template `R-0000`, and it reads `.cs`,
  `nuget.config`, `global.json` and `*.yml` only (`MarkerGateTests.cs:73-120`). A review agent
  injected the two identifiers.
- The two concurrency tests do not assert that a reader ran
  (`DocumentSessionConcurrencyTests.cs:55-84`, `HttpConcurrencyTests.cs:67-87`). **[Observed]**
- **Owner.** Register entry R-0031 for the first and the third, R-0034 for the second and the fourth.

### Complexity and dead code

#### C1 · Low · Code that serves no objective · Open in two parts of four

**[Observed]** The two empty markers, the flags that nothing reads, and one comment that cites the
removed clamp.

### Performance

#### P1 · Low now · Three costs that grow with the scene · Open

**[Inferred]** The snapshot now holds the session too. No measure exists yet. TASK-0028 measures the
time of a tessellation.

## 4 The next planned task

**[Observed]** `docs/roadmap.md` gives phase **P0.14**: "The version counts applied commands."
`tasks/TASK-0035-version-counts-applied-commands.md` holds the work, with the status `Ready` and one
dependency, TASK-0034, which is `Done`. TASK-0037 depends on it.

**[Observed]** The roadmap gave 32 to 48 evenings for ten phases. P0.13 shipped, and the nine phases
that remain give 30 to 45. This task corrects the sentence and adds P0.13 to the list "Shipped".

## 5 Research for that task

### What TASK-0034 left for it

**[Observed]** Five places now hold the old meaning of the version:

- `Engine.Core/CommandBus.cs:161`, `:183` and `:208` advance the version to a `Seq` on an apply, a
  rejection and a cancellation.
- `Engine.Core/CommandBus.cs:235-245`, `TakeSeqs`, refuses a `Seq` at or below the version. After
  the change the version is below each `Seq`, so the check does nothing and couples the two counters,
  which ADR-0020 §2 forbids.
- `Engine.Contracts/Document.cs:11-13` and `Engine.Core/CommandBus.cs:13` give the old meaning in a
  comment. `Document.AdvanceVersion` (`:39-43`) accepts a lower value. TASK-0035 has that file in its
  write set, so finding E10 can close.
- `Engine.Tests/Http/HttpConcurrencyTests.cs:120-123` compares the first live `seq` with the snapshot
  `version` plus one. After the change it must read the new field `seq` of the snapshot.
- `Engine.Tests/CommandBusTests.cs` asserts the old value at `:73` (a rejection moves the version),
  `:167` and `:191` (the version equals the last `Seq`), and `:240` (`AdvanceVersion(10)`). Two
  assertions agree with the new meaning by chance: `:38` and `:141` use `NoOp`, which gives one
  event for each command.

### The replay

**[Observed]** `Engine.Core/Replay.cs:24-31` builds a bus with the null backend as default, applies
each command and discards each result (E7). It also passes through the idempotency cache, so a log
that holds one identifier two times replays to a shorter log (E17). TASK-0035 changes this file and
names neither finding. **[Inferred]** Its goal, "a replay gives the same version and the same
bodies", cannot be shown while a rejection in the replay is silent: the new fixture can pass with a
body missing on both sides. Question Q1.

### The gates around it

**[Observed]** TASK-0035 permits `Engine.Tests/**`. Finding T9 shows that this permit covers the
project file of the tests. **[Recommended]** Correct T8 and T9 first, in one governance task. Question
Q2.

## 6 Recommended approach

1. **[Recommended]** One governance task of one evening first: `timeout-minutes` and
   `--blame-hang-timeout` in the workflow (T8), and the project file, the props files, the cut-off
   file and the workflow as gate files (T9). Each one is injected first and fails.
2. TASK-0035 adds its failing fixture first, as its scope says, with a rejection and then a command
   with an expected version.
3. `Document.AdvanceVersion` becomes "add one", called on an apply only, and it cannot go back. This
   closes the rest of E10. `TakeSeqs` loses its check.
4. The reset snapshot gains `seq`, and the E9 test reads it.
5. Each test that asserts the old value changes to the meaning of ADR-0020, and none is deleted.
6. **[Recommended]** If the owner answers yes to Q1: `ReplayLog` stops at the first result that is
   not `Applied` and names the command, and it does not pass through the idempotency cache.
7. The comments at `Document.cs:11-13` and `CommandBus.cs:13` agree with ADR-0020.

## Questions for the owner

Each question has a recommendation. An answer of "yes" accepts it.

**Q1. Add findings E7 and the replay part of E17 to TASK-0035?** Recommendation: yes. The task
changes `Engine.Core/Replay.cs`, its goal is a replay that rebuilds the Document, and a replay that
hides a rejection cannot prove that goal. The cost is one check in a loop and one test.

**Q2. Correct T8 and T9 before TASK-0035, in one governance task?** Recommendation: yes. TASK-0035
is the next task that permits `Engine.Tests/**`, and T9 lets that permit remove each gate. A hang
without T8 costs six hours of a runner and gives no name.

**Q3. Should a commit be refused when it names a task that is `Done`?** Recommendation: yes, except
in a commit that also changes the status line of that task. A correction then reopens the task with
the status `Active` and a progress line, and closes it again. Today v0.38 and `4715b88` each changed
code under a `Done` task.

**Q4. When a sink throws after a commit, which rule is correct?** Option A: a sink must not throw,
by contract, and a test holds each sink to it. Option B: the bus continues with the other events and
marks the stream, and the broadcaster sends a reset to each subscriber. Recommendation: A. It is the
smaller change, the two sinks of today can meet it, and it keeps ADR-0005, "the engine never
produces gaps", true.
