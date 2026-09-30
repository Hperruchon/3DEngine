---
date: 2026-09-30
commit: 14e2c73
ledger: v0.34
previous: 2026-09-23-codebase-review.md
---

# Codebase review — 2026-09-30

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the second codebase review. The next review must come before ledger entry v0.41: the build
fails when a seventh milestone follows v0.34. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds that rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `14e2c73`, the merge of v0.32 to v0.34 |
| Branch of this review | `codebase-review-2026-09-30` |
| Tests | 214 on this computer |
| Pipeline | Run 36443245386, a push to `main` at `14e2c73`: five jobs, each one a success |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** I read the source that the findings cite, the gates, the workflow and the task files. I
ran a clean build and each test. I ran two programs against `Engine.Core`, outside the
repository: a probe of concurrent reads, three times, and a program that verifies nine findings. I
ran the HTTP host on the loopback address and sent it requests and one idle WebSocket. I injected
eight violations into the working tree, ran the gates, and removed each one. I read the public run list of the pipeline. Four review agents read the
code in parallel: the engine kernel, the two hosts, the gates, and the Vulkan layer with the desktop
host. I checked on disk each of their findings that this review uses. A finding that I did not check
again says "reported by a review agent".

## Summary

- **No production source changed after the first review.** The difference between `01a42a1` and
  `14e2c73` in the eight production projects is one documentation file. Each engine finding and each
  Vulkan finding of the first review is therefore open. **[Observed]**
- **The decisions exist, and the corrections do not.** ADR-0019, ADR-0020 and ADR-0021 decide the
  three largest findings, and six tasks are `Ready`. Six milestones followed the first review, and
  none changed engine code or Vulkan code. **[Observed]**
- **Finding E2 is now a measured failure.** With commands on one task and reads on four tasks, about
  4,500 queries fail and about 4,000 snapshot copies fail, in each of three runs. **[Observed]**
- **The pipeline is green on `main`, and the contract gate ran on a push to `main` for the first
  time.** Finding G1 is fixed. **[Observed]**
- **Six earlier findings had no owner.** No task and no register entry named findings E5, E6, V4 and
  P1, or the open parts of T2 and C1. Register entry R-0027 now holds them. **[Observed]**
- **Thirty-three findings are open: two critical, twelve high, fourteen medium and five low.**
  Nineteen are new. A run confirms most of the new ones. **[Observed]**
- **Three new findings are about events.** An idle subscriber receives 1,025 heartbeat frames and
  not one. A subscriber that connects during a commit can lose an event or receive it two times. A
  replay that rejects a command reports nothing. **[Observed]**, the first and the third in a run.
- **The gates have holes, and TASK-0039 wrote several of them.** Seven injected violations pass
  their gate. One of them is a reference from `Engine.Core` to `3DEngine.Core`, which is the rule
  that the two kernels never reference each other. **[Observed]**
- **The test project is larger than the production code.** The production code holds 4,915 lines and
  the tests hold 5,900. Half of the test lines, 2,961, are gates. **[Observed]** The gates protect
  the rules. They do not yet protect the first objective, because no renderer code exists to test.
  **[Inferred]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. The
values are for `14e2c73`. A source line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 9 | `Project(` lines in `3DEngine.sln`. The first review gave 11. v0.33 removed the two projects of the web shell. |
| Production source lines | 4,915 | Each project except `Engine.Tests`, 97 files. The first review gave 5,292. |
| Test source lines | 5,900 | `Engine.Tests`, 52 files. The first review gave 5,153 in 46 files. |
| Tests | 214 | `dotnet test 3DEngine.sln --no-build`: 214 passed, 0 failed, 0 skipped. The first review gave 198. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. The first review gave 11. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 13 | Section "Open" of `docs/register.md`, with the entries that this review adds. The count before the review was 8. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19. |
| Findings: critical | 2 | Each finding that is open at this commit, from either review: E1, E2. The first review gave 2. |
| Findings: high | 12 | The same method: E3, E4, E7, E8, E9, V1, V2, V3, V5, T1, T3, T4. The first review gave 6. |
| Findings: medium | 14 | The same method: E5, E6, E10 to E16, V4, V6, T2, T5, T6. The first review gave 5. |
| Findings: low | 5 | The same method: E17, V7, T7, C1, P1. The first review gave 2. |

Two more measures, which the next review can repeat. The lines of each production project:
`Engine.Core` 1,343, `3DEngine.Vulkan` 1,080, `Engine.Api.Http` 1,060, `Engine.Contracts` 397,
`Engine.Cli` 397, `Engine.Geometry.Manifold` 273, `3DEngine.Core` 221, `3DEngine` 140. The tests of
each area: Governance 55, Http 39, Hosting 25, Geometry 24, Cli 20, Commands 16, and 35 in the other
areas.

## Findings of the previous review

Each line gives the state at `14e2c73` and the evidence. "Open" means that the code that the finding
cites did not change.

- **E1** — Open. `Engine.Core/CommandBus.cs:174` advances the version on a rejection, and
  `Engine.Contracts/Document.cs:11-14` gives that meaning. ADR-0020 decides the correction, and
  TASK-0035 is `Ready`.
- **E2** — Open, and now seen in a run. `Engine.Core/QueryBus.cs:23-43` takes no lock. Section 5 gives
  the probe. TASK-0034 is `Ready`.
- **E3** — Open. `Engine.Cli/Cli.cs:192-194` and `Engine.Api.Http/EngineHost.cs:39-41` select the
  managed backend with no message. TASK-0036 is `Ready`.
- **E4** — Open. `Engine.Core/CommandBus.cs:102-115` appends to the log and then gives the token of
  the caller to the sink. TASK-0034, scope item 4.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` is unchanged. It had no owner.
  Register entry R-0027.
- **E6** — Open, in each of its five parts. `Engine.Core/Hosting/ParameterBinder.cs:108` uses
  `RoundtripKind`. The command-line host rejects an unknown command before the bus
  (`Engine.Cli/Cli.cs:86-95`). `SchemaEventsEndpoint.cs:19-24` lists five kinds, and `Engine.Core`
  emits four other kinds. ADR-0006 §4, §5 and §7 have no code. The command-line host keeps no state.
  It had no owner. Register entry R-0027.
- **V1** — Open. `3DEngine.Vulkan/GraphicsDevice.cs:377-380`. Register entry R-0022.
- **V2** — Open. `3DEngine.Vulkan/Swapchain.cs:174` and `3DEngine.Vulkan/Window.cs:77`. Register
  entry R-0022.
- **V3** — Open. The word "portability" occurs in no file of `3DEngine.Vulkan`. Register entry
  R-0022.
- **V4** — Open. `3DEngine/DesktopEngineHost.cs:48-51` catches each exception, prints the message
  and returns, so the exit code is 0. It had no owner. Register entry R-0027.
- **T1** — Open, in each of its three parts, and each part has an owner. A native test that skips:
  TASK-0036. A difference between platforms: register entry R-0023. A rejection in a replay:
  TASK-0035. The fixture is unchanged (`ReplayDeterminismFixture.cs:25-28`).
- **T2** — Fixed in three parts of five. TASK-0039 made the dispatch gate read each host source, made
  the diagnostics scanner read each `Engine.*` project, and added `DeterminismCallGateTests`. Open:
  `Engine.Tests/QueryBusTests.cs:23,33` asserts on a sink that the bus never receives, which
  TASK-0034 has in its write set; and no test makes the backend throw through the command bus, which
  register entry R-0027 holds.
- **C1** — Fixed in one part of four. TASK-0033 deleted the web shell: `git ls-files BlazorApp` gives
  no file, and ADR-0003 is `Withdrawn`. Open: `IBRepOps` and `IFeatureIdMap` are seven lines each
  with no implementation; no source in `Engine.Core`, `Engine.Cli` or `Engine.Api.Http` reads a flag
  of `BackendCapabilities`; `Engine.Api.Http/Program.cs:7` and `Engine.Api.Http/EngineHost.cs:14-15`
  cite a clamp that v0.17 removed. Register entry R-0027.
- **P1** — Open. `Engine.Api.Http/WebSockets/EventBroadcaster.cs:70` builds the snapshot inside the
  lock of the broadcaster. TASK-0034 moves the handshake into the serial section, and the cost
  stays by decision. Register entry R-0027.
- **G1** — Fixed. The contract gate has no condition on the event (commit `ca87e6c`). Run
  36443245386, a push to `main` at `14e2c73`, shows the job "Contract-touched-needs-ADR" with the
  result success.

## 1 Current architecture

**[Observed]** The architecture is the architecture of the first review, less the web shell. Nine
projects are in the solution. The design-truth kernel is `Engine.Contracts`, `Engine.Core` and
`Engine.Geometry.Manifold`. The clients are `Engine.Cli` and `Engine.Api.Http`. The render side is
`3DEngine.Core`, `3DEngine.Vulkan` and the desktop host `3DEngine`. The desktop host has no reference
to the engine, and it clears the screen.

**[Observed]** Four decisions changed the target, and no code follows them yet:

| Decision | Record | Task | The code today |
|---|---|---|---|
| The desktop host owns the session and serves the HTTP and WebSocket surface | ADR-0019 | TASK-0038 | `engine-api-http` is the only host with the surface |
| The version counts applied commands | ADR-0020 | TASK-0035 | The version mirrors the last sequence number, rejections included |
| An operation consumes its operands | ADR-0021 | TASK-0037 | Translate and Subtract keep their operands |
| A mesh leaves the backend through a query | ADR-0018 | TASK-0028 | Both hosts fix the result type to `Aabb` |

**[Inferred]** The distance between the records and the code is the main risk of this period. An
accepted ADR now names a test that a task creates, and `AdrEnforcementExistsGateTests` holds each one
to its task. The gate fails when a task closes and its test is absent.

## 2 Patterns in use

- **Handler-declared construction (ADR-0016).** It holds. No host source names a command, and the
  gate now reads each file of each host. **[Observed]**
- **Capability negotiation.** It holds. A handler asks the backend for a typed capability and
  reports `E-GEOM-CAP-MISSING` when it is absent. **[Observed]**
- **A gate is a test.** Seventeen gate classes run with `dotnet test`. Each new gate of v0.34 failed
  on an injected violation before its commit. **[Observed]**
- **The commit trailer names the governing task.** The write-set gate reads it. Each commit after the
  cut-off passes. **[Observed]**
- **The ADR agrees with the code.** `docs/templates.md`, section 1, holds the rule since v0.34. No ADR
  follows it yet: seven records have the status `Amended`. TASK-0040 applies it. **[Observed]**

## 3 Review of the code

Each finding has an identifier, a severity, a title and a state. An identifier of the first review
stays with its finding. The first review gives the full evidence and the correction of each earlier
finding. This section gives the change and the owner.

### Correctness of the engine

#### E1 · Critical · A replay can rebuild a different Document · Open

ADR-0020 decides that the version counts applied commands. TASK-0035 changes the code and adds a
rejection to a replay test. No change in the code. **[Observed]**

#### E2 · Critical · Queries read shared state while a command writes it · Open, confirmed by a run

Section 5 gives the run. The first review called the effect a risk. It is a failure that a client
can cause with four parallel requests. TASK-0034. **[Observed]**

#### E3 · High · The backend changes with no message · Open

TASK-0036 makes a host refuse to start without the native backend. **[Observed]**

#### E4 · High · The commit uses a cancellable token after the log append · Open

TASK-0034, scope item 4. Section 5 shows that a sink that throws gives the same partial commit, with
or without a token. **[Observed]** in the code.

#### E5 · Medium · An integer from HTTP JSON can never bind · Open

No handler declares an integer field, so no run shows it. Register entry R-0027. **[Observed]**

#### E6 · Medium · Other engine findings · Open

Five parts, each one unchanged. Register entry R-0027. **[Observed]**

#### E7 · High · A replay discards each result, so a divergence is silent · Confirmed by a run

- **Evidence.** `Engine.Core/Replay.cs:26-31` applies each command and does not read the result.
  `ReplayResult` holds the Document and the sink only (`:35`). A run: a log with one `CreateBox`,
  replayed with no backend argument, returns with no exception, an empty log, no body and one event
  `command.rejected`. **[Observed]**
- **Impact.** Each command in a log was applied one time, so a rejection in a replay is always a
  divergence. The caller receives a Document that looks correct. This is the reason that finding E1
  is silent. The default backend of `ReplayLog` is the null backend (`:24`).
- **Correction.** **[Recommended]** `ReplayLog` stops at the first result that is not `Applied` and
  names the command and the error. The backend argument becomes necessary.
- **Owner.** None. TASK-0035 changes the replay and does not name this. Register entry R-0028.

#### E8 · High · An idle subscriber receives 1,025 heartbeat frames and not one · Confirmed by a run

- **Evidence.** The heartbeat loop writes a frame and starts again with no wait
  (`Engine.Api.Http/WebSockets/Subscriber.cs:117-132`). Only the pump changes the time of the last
  send, after the send completes (`:101`). A run on the real host: one subscriber, no event, 36
  seconds. The client received one `subscription.reset` frame, and 1,025 `heartbeat` frames at
  second 30. The channel holds 1,024. **[Observed]**
- **Impact.** ADR-0005 asks for one frame in each interval. While the channel is full of heartbeat
  frames, the next event does not fit, and the host disconnects a good subscriber as lagged.
  **[Inferred]** No test waits for a heartbeat, so no test sees this.
- **Correction.** **[Recommended]** The loop waits one interval after each frame that it writes.
- **Owner.** None. Register entry R-0029, with the class `risk`.

#### E9 · High · A subscriber that connects during a commit can lose an event or receive it two times · Confirmed in the code, not run

- **Evidence.** The handshake holds the lock of the broadcaster only (`EventBroadcaster.cs:33-74`). A
  commit appends an event, which goes to each subscriber at once (`BroadcastingEventSink.cs:26-30`),
  and it advances the version last (`Engine.Core/CommandBus.cs:138`). A subscriber that attaches
  between those two steps receives a snapshot with the version before the event (`WireMessage.cs:51`)
  and never receives the event. A subscriber that resumes between the append to the ring and the
  fan-out receives the event in the replay (`EventBroadcaster.cs:58-62`) and again from `OnEvent`. The
  comment at `EventBroadcaster.cs:14-16` says that the lock prevents this. **[Observed]** in the code.
  Reported by a review agent; I traced both paths.
- **Impact.** ADR-0005 forbids a gap and a repeat. A client that builds state from events holds a
  wrong state with no signal.
- **Correction.** TASK-0034 moves the handshake into the serial section, which closes both windows.
  **[Recommended]** Add one acceptance test to that task: the first live sequence number equals the
  snapshot version plus one.
- **Owner.** TASK-0034, with the added test. Register entry R-0028 holds the test.

#### E10 · Medium · A second bus on one Document starts the sequence at 1 again · Confirmed by a run, latent

- **Evidence.** The sequence counter belongs to the bus (`Engine.Core/CommandBus.cs:22`), and
  `AdvanceVersion` accepts a lower value (`Engine.Contracts/Document.cs:39-43`). A run: three commands
  on one bus give version 3. One command on a second bus gives sequence number 1 and version 1, and
  the sink holds the numbers 1, 2, 3, 1. **[Observed]**
- **Impact.** No host builds a second bus today. A host that continues after a replay must build a
  bus on the replayed Document, and ADR-0015 plans that path.
- **Correction.** **[Recommended]** The session of TASK-0034 owns the one bus. The bus takes its
  first number from the sink, and `AdvanceVersion` refuses a lower value.
- **Owner.** Register entry R-0028.

#### E11 · Medium · A value that is not finite enters the log, and the host answers with HTTP 500 · Confirmed by a run

- **Evidence.** The binder accepts the texts `Infinity`, `NaN` and `1e999` for a number
  (`Engine.Core/Hosting/ParameterBinder.cs:68-74`). `CreateBoxCommandHandler.cs:45` refuses `NaN` and
  accepts infinity. No source in the engine or in a host calls `double.IsFinite`. A run on the real
  host: `CreateBox` with `sizeX` equal to `1e999` gives `Applied`, and `GetBoundingBox` for that body
  gives HTTP 500 with an empty body. **[Observed]**
- **Impact.** The log holds a number that standard JSON cannot hold, which matters for phase P8a.
- **Correction.** **[Recommended]** The binder refuses a value that is not finite.
- **Owner.** Register entry R-0028.

#### E12 · Medium · The binder refuses a whole number for a number field · Confirmed by a run, latent

- **Evidence.** The case `number` accepts a `double` and a text only (`ParameterBinder.cs:67-76`). A
  run: the values `10L` and `10` fail with "expected a number", and the value `10.0` fails for an
  integer field. **[Observed]**
- **Impact.** Finding E5 hides this today, because `JsonParameters` gives a `double` for each JSON
  number. The correction that the first review gave for E5 makes each HTTP request with a whole value
  fail. Correct E5 and E12 in one change.
- **Owner.** Register entry R-0027, with E5.

#### E13 · Medium · A failed query returns a box of zeros and not null · Confirmed by a run

- **Evidence.** `QueryResult<T>` declares `T? Result` with no constraint
  (`Engine.Contracts/QueryResult.cs:6`), and both hosts ask for `Aabb` (`Engine.Cli/Cli.cs:157`,
  `Engine.Api.Http/Endpoints/QueriesEndpoint.cs:81`). A run on the real host: a query for an absent
  body gives the error `E-GEOM-BODY-NOT-FOUND` and a result with six zeros. **[Observed]**
- **Impact.** A client that tests the result for null reads a box of size zero as an answer.
- **Owner.** TASK-0028 changes both hosts to `Query<object>`, which gives null. Register entry R-0028
  holds the test.

#### E14 · Medium · The Manifold wrapper frees a caller buffer with the C++ `delete` · Confirmed in both sources, the effect is inferred

- **Evidence.** The wrapper allocates with `Marshal.AllocHGlobal` and gives the buffer to
  `manifold_cube` (`Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:70-72`). It releases with
  `manifold_delete_manifold` (`Native/ManifoldSolidHandle.cs:20`). The upstream source at the pinned
  tag v3.5.2, `bindings/c/manifoldc.cpp`: `manifold_cube` builds the solid in the buffer with a
  placement `new` (`:367-372`), `manifold_delete_manifold` is `delete from_c(m)` (`:1070`), and
  `manifold_destruct_manifold` runs the destructor only (`:1099`). The repository does not bind the
  third function. The comment at `Native/ManifoldNative.cs:46` says "VERIFIED". **[Observed]**
- **Impact.** Memory from one allocator goes to a different allocator. The thirteen native tests pass
  on this computer, so the two agree today. A different C runtime or a checked build gives heap
  corruption with no managed exception. **[Inferred]**
- **Correction.** **[Recommended]** Bind `manifold_destruct_manifold`, call it, and then call
  `Marshal.FreeHGlobal`. Or allocate with `manifold_alloc_manifold`.
- **Owner.** Register entry R-0028.

#### E15 · Medium · The host has no bind rule, no Origin check and no Host check · Confirmed by a run

- **Evidence.** `Engine.Api.Http/Program.cs:5` builds the host with the arguments of the process, and
  `:16` enables WebSockets with no option. `docs/CHARTER.md:106-107` says that the host binds to
  localhost only. No source checks the address. A run on the real host: a WebSocket upgrade with the
  header `Origin: https://evil.example` gives `101 Switching Protocols`, and a command with the header
  `Host: evil.example` gives `Applied`. **[Observed]**
- **Impact.** A web page in a browser on the same computer can open the event stream and read the
  snapshot and each event. **[Inferred]** The exposure grows with TASK-0038, because the desktop host
  then serves this surface while a person has a document open.
- **Correction.** **[Recommended]** The host refuses to start on an address that is not a loopback
  address. The WebSocket options list the permitted origins. The host filters the Host header.
- **Owner.** Register entry R-0028. TASK-0038 must not ship before this correction.

#### E16 · Medium · The request envelope ignores an unknown member · Confirmed by a run

- **Evidence.** The serializer options set no rule for an unmapped member
  (`Engine.Api.Http/Json/ApiJson.cs:14-24`). A run on the real host: a command with the member
  `expectedDocumentVersoin` gives `Applied`. **[Observed]**
- **Impact.** A typing error removes the version check, and a wrong name for `commandId` removes the
  protection against a repeat. The binder refuses an unknown parameter for this reason
  (`ParameterBinder.cs:38-44`). The envelope does not.
- **Owner.** Register entry R-0028.

#### E17 · Low · Other engine and host findings · Confirmed, except where the line says so

- A replay passes through the idempotency cache. A log that holds one command two times replays to a
  shorter log. A run gave a log of 2 and a replayed log of 1. **[Observed]**
- An exception from a handler, other than a cancellation, leaves the bus
  (`Engine.Core/CommandBus.cs:83-90`). A run: a command with the name `CreateBox` and a different type
  gives `InvalidCastException`, with no event and no result. **[Observed]**
- `QueryBus` reports a wrong result type with the code `E-QRY-UNKNOWN`
  (`Engine.Core/QueryBus.cs:60-63`). `docs/diagnostics.md:18` gives that code a different meaning.
  **[Observed]**
- A JSON object for a field of the type `string` is accepted as its raw text, and the text enters the
  log (`JsonParameters.cs:37-40`). A run gave `Applied`. **[Observed]**
- A response holds the text of an exception with the name of an internal type
  (`CommandsEndpoint.cs:39-42`), and a request with an unknown `charset` gives HTTP 500. **[Observed]**
  in a run.
- The close status for an invalid subscribe frame is 1007 in the code
  (`EventsEndpoint.cs:102-105`), and `docs/diagnostics.md:20` says 1003. **[Observed]** Question Q3.
- The pump cancels its token before it sends the close frame, so a lagged client can get a broken
  connection and no status 1008 (reported by a review agent, not checked).
- The command-line host writes text that is not ASCII in the code page of the console (reported by a
  review agent, not checked).
- Seven small defects in the command line and the endpoints (reported by a review agent, not
  checked): a flag read as a command name, an empty body for status 415, a subscribe read with no
  time limit, a subscriber that no code disposes when the handshake throws, a negative cursor, no
  link to the host shutdown, and a repeated JSON key.

### The Vulkan layer and the desktop host

#### V1 · High · `VK_SUBOPTIMAL_KHR` is handled as a failure · Open

Register entry R-0022. **[Observed]** in the code, not run.

#### V2 · High · The special extent value passes the size check · Open

Register entry R-0022. **[Observed]** in the code.

#### V3 · High · No portability flag for macOS · Open

Register entry R-0022. **[Observed]** absence.

#### V4 · Medium · Other desktop findings · Open

Register entry R-0027. The exit code part is **[Observed]** at `DesktopEngineHost.cs:48-51`. The
other parts stay as the first review gave them, reported by a review agent.

#### V5 · High · The device selection continues with a null device when no device is suitable · Confirmed in the code, not run

- **Evidence.** The loop sets `PhysicalDevice` for a suitable device only
  (`3DEngine.Vulkan/GraphicsDevice.cs:149-168`). No check follows the loop. Line 170 gives the value
  to `FindQueueFamilies`, which calls a Vulkan function with it. `IsDeviceSuitable` (`:497-508`) does
  not check that the device has the swapchain extension. **[Observed]**
- **Impact.** On a computer where no device can present to the surface, the host calls Vulkan with a
  null handle. The process stops in native code, so the `catch` of the host does not run and no
  message appears. **[Inferred]** Examples are a remote session and a virtual machine.
- **Correction.** **[Recommended]** Throw with a message when the loop finds no device.
- **Owner.** Register entry R-0030.

#### V6 · Medium · A pointer into managed memory goes to Vulkan with no pin · Confirmed in the code, the binding is reported

- **Evidence.** `GraphicsDevice.cs:47-49` and `:74` make a byte array with `Encoding.UTF8.GetBytes` and
  convert it to a string type of the binding. `:636-639` and `:665-668` take a pointer inside a
  `fixed` block and keep it after the block ends. **[Observed]** The string type of the binding stores
  the pointer and does not copy the bytes (reported by a review agent, who read the binding source).
- **Impact.** The garbage collector can move or free each array before Vulkan reads it. The result is
  a layer or an extension that is not found, with no error. The interval is short today, and it grows
  when the next phases add work before the device creation. **[Inferred]**
- **Correction.** **[Recommended]** Copy each name into memory that the device owns, or use the `u8`
  literals that the same file already uses.
- **Owner.** Register entry R-0030.

#### V7 · Low · Other findings in the Vulkan layer and the render kernel · Confirmed in the code

- The swapchain uses exclusive sharing (`Swapchain.cs:55`), and `FindQueueFamilies` can return two
  different families (`GraphicsDevice.cs:555-580`). **[Observed]**
- The host enables the colour space extension (`GraphicsDevice.cs:68-71`), and the format selection
  does not read the colour space (`Swapchain.cs:202-208`). **[Observed]**
- An exception in the constructor of the device skips each release, and no source calls
  `SDL_DestroyWindow` or `SDL_Quit`. **[Observed]** for the absent calls.
- Five more results are ignored (`GraphicsDevice.cs:308`, `:386`, `:391`, `:565`, `:660`).
  **[Observed]**
- The frame loop takes its time step from `DateTime.UtcNow` (`3DEngine/DesktopEngineHost.cs:23`,
  `:42-45`), which is not monotonic. **[Observed]**
- `3DEngine.Core` gives its lists as `IList<T>` (`Models/Scene.cs:10-14`, `Models/Entity.cs:18`), so
  a change during a read throws. No caller exists today. **[Observed]**

### Tests and continuous integration

#### T1 · High · The pipeline cannot see three kinds of loss · Open

Each part has an owner: TASK-0036, register entry R-0023, TASK-0035. **[Observed]**

#### T2 · Medium · Tests that cannot fail, and gates with a short list · Open in two parts of five

**[Observed]** `QueryBusTests.cs:23,33` and the absent test for a backend that throws.

The five findings below are about the gates. TASK-0039 wrote or changed nine of the seventeen gate
classes two days before this review, and its injections tested that each gate can fail. They did not
test the forms that a gate cannot see. A review agent searched for those forms, and I injected each
one that a line below marks as a run.

#### T3 · High · The write-set gate has three holes · Confirmed by a run

- **A merge commit is never examined.** `git diff-tree` prints no path for a commit with two parents
  (`.github/workflows/ci.yml:205`). The gate then receives an empty list and returns
  (`Engine.Tests/Governance/WriteSetGateTests.cs:124-126`). The replay of the branch showed zero files
  for each of the five merge commits. A change made inside a merge passes. **[Observed]**
- **A commit can name a closed task.** No code reads the status of the named task. With TASK-0026
  named, a change to `eng/write-set-cutoff.txt` passes, and the workflow reads the cut-off from that
  file. A commit can therefore move the cut-off past an earlier commit. **[Observed]** in a run.
- **A wide permit beats a narrow forbid.** TASK-0035, TASK-0036, TASK-0037 and TASK-0038 each permit
  `Engine.Tests/**`. With TASK-0038 named, a change to `RegisterGateTests.cs` passes. **[Observed]** in
  a run.
- **Correction.** **[Recommended]** Give `-m` to `git diff-tree`. Refuse a named task with the status
  `Done`, except in the commit that closes it. Hold the cut-off value in a test.
- **Owner.** Register entry R-0031.

#### T4 · High · Four gates read text with one expression and miss the other forms · Confirmed by a run

- **The dependency gate.** With `Include="..\3DEngine.Core\3DEngine.Core.csproj;..\Engine.Contracts\
  Engine.Contracts.csproj"` in `Engine.Core.csproj`, the gate passes. It keeps the last name of the
  value (`DependencyDirectionGateTests.cs:63-64`). This is the gate of the rule that the two kernels
  never reference each other. **[Observed]** in a run.
- **The determinism gate.** `double.Pow(2, 3)`, `Sin(1)` after `using static System.Math`, and
  `Math.Pow` with the parenthesis on the next line each pass. **[Observed]** in a run.
- **The marker gate.** `// TODO R-9999` passes. The gate does not read the register, so the
  identifier need not exist. **[Observed]** in a run. `nuget/.gitignore:2` holds the words "Interim
  bootstrap" with no identifier, and the gate does not read that file. **[Observed]**
- **The dispatch gate.** It reads two projects by name (`DispatchSurfaceGateTests.cs:18`). TASK-0038
  creates a third host project and changes `3DEngine/`, and the gate reads neither one.
  **[Observed]** in the code.
- **Correction.** **[Recommended]** Read each project file as XML and take each item of each
  `Include`. Build the determinism gate on the compiled assembly, where each call has one form. Make
  the marker gate compare each identifier with the open entries. Take the host list from the project
  classes of the dependency gate.
- **Owner.** Register entry R-0031.

#### T5 · Medium · A parser that returns nothing makes its gate pass · Confirmed by a run

- **The task status.** With `status: ready` and `governed-by: []` in TASK-0034, each gate passes. No
  test compares a status with the permitted set. **[Observed]** in a run.
- **The register.** With the closing code fence at `docs/register.md:72` removed and one entry made
  overdue, the gate passes. The heading "Open" is then inside the code block, and the parser returns
  no entry. With the fence in place, the same entry fails the gate. **[Observed]** in a run.
- **The field `affects`.** A value with quotes or an inline list keeps the quotes or the brackets in
  the pattern, and the pattern matches no path (`RepositoryFiles.cs:48`). **[Observed]** in the code.
- **Correction.** **[Recommended]** Each parser asserts what it found: a count of entries that is
  not zero, a status from the set, a pattern with no quote.
- **Owner.** Register entry R-0031.

#### T6 · Medium · Two jobs of the workflow hide a failed git command · Confirmed in the code, not run

- **Evidence.** Only the job `gate` sets the shell (`.github/workflows/ci.yml:36-40`). The jobs
  `contract-gate` and `write-set-gate` count the output of `git diff` with `wc -l` (`:133-134`), and
  they take the commits from `git rev-list` inside a `for` statement. **[Observed]** A failed command
  in either place gives a count of zero or a loop with no step, and the job passes (reported by a
  review agent, who ran both forms locally).
- **Impact.** I wrote the script of the contract gate in TASK-0039, against the rule in `CLAUDE.md`
  about `set -o pipefail`.
- **Correction.** **[Recommended]** Set `shell: bash` for each job, which adds `pipefail`. Put each
  list in a variable first, and test the exit code.
- **Owner.** Register entry R-0031.

#### T7 · Low · Other findings in the gates · Confirmed, except where the line says so

- The ADR gate compares the identifier and the status of an index row, and not the columns "Amends"
  and "Amended by". With the column emptied for ADR-0006, the gate passes. **[Observed]** in a run.
- TASK-0040 moves ADR-0011 to an archive and makes ADR-0019 supersede it. The reciprocity test reads
  `docs/adr/` with no recursion, so it then reports that ADR-0011 does not exist
  (`AdrGateTests.cs:122-124`). The task has the gate in its write set. **[Observed]** in the code.
- The register gate reads the clock. Four entries have the limit 2026-12-24, so each build fails
  from the next day until the owner decides them. This is the design, and it is a date to plan.
  **[Observed]**
- The codebase review gate accepts a measure row with an empty value, and `previous: none` skips the
  check of the earlier findings (reported by a review agent, not checked).
- A path check ignores case on Windows and on macOS, so a path with a wrong case passes here and
  fails on Linux (reported by a review agent, not checked).

### Complexity and dead code

#### C1 · Low · Code that serves no objective · Open in three parts of four

**[Observed]** The two empty markers, the flags that nothing reads, and the comments that cite a
removed clamp.

### Performance

#### P1 · Low now · Three costs that grow with the scene · Open

**[Inferred]** No measure exists yet. TASK-0028 measures the time of a tessellation.

## 4 The next planned task

**[Observed]** `docs/roadmap.md` gives phase **P0.13**: "One serial boundary for commands, queries and
snapshots." `tasks/TASK-0034-document-session.md` holds the work, with the status `Ready` and no
dependency. Each other code task depends on it: TASK-0035 and TASK-0036 directly, and TASK-0028,
TASK-0037 and TASK-0038 through them.

**[Observed]** The roadmap gave 25 to 35 evenings for the path to the first objective. The sum of its
column "Evenings" for the ten phases that remain is 32 to 48. TASK-0042 corrected the sentence.

### What P0.13 requires

- A test that fails on the code of today. The probe of section 5 is that test, with a time limit.
- A type `DocumentSession` in `Engine.Core` that owns the Document, the backend, both buses and the
  sink, with one serial section.
- Both hosts use the session. The WebSocket handshake reads its snapshot through the session.
- The commit cannot stop between the log append and the version advance.
- No change to `Engine.Contracts`.

## 5 Research for that task

### Three readers, three locks

**[Observed]** A command holds the semaphore of the bus for the whole call
(`Engine.Core/CommandBus.cs:21`, `:45-58`). A query holds nothing (`Engine.Core/QueryBus.cs:23-43`).
The handshake holds the lock of the broadcaster, reads the ring of events, and then reads
`Document.Bodies` (`Engine.Api.Http/WebSockets/EventBroadcaster.cs:33-74`). `Document` keeps its bodies
in a `Dictionary` (`Engine.Contracts/Document.cs:22`), and each backend keeps its solids in a
`Dictionary` (`Engine.Core/Geometry/InProcessMeshBackend.cs:12`,
`Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:28`).

### The probe

**[Observed]** A program outside the repository references `Engine.Core`. One task applies 20,000
`CreateBox` commands through the command bus. Four tasks run `GetBoundingBox` on the newest applied
body through the query bus, and copy `Document.Bodies` to an array, as the snapshot does. The backend
is the managed backend.

| Run | Commands applied | Queries that threw `InvalidOperationException` | Snapshot copies that threw `IndexOutOfRangeException` | Queries that reported an applied body as absent |
|---|---|---|---|---|
| 1 | 20,000 | 4,342 | 3,858 | 0 |
| 2 | 20,000 | 4,683 | 4,068 | 0 |
| 3 | 20,000 | 4,575 | 4,014 | 0 |

The Document is consistent after each run: version 40,000, log 20,000, bodies 20,000. The writer is
safe, and each reader is not. TASK-0034 records a probe of 2026-09-23 that also saw
`ArgumentException` and one absent body. The exception type depends on the moment of the read.
**[Inferred]** With the native backend, the same read reaches a native function while the dictionary
changes. No run of this review used the native backend for the probe.

### The order of the locks

**[Observed]** A commit takes the semaphore of the bus, and then the sink takes the lock of the
broadcaster for each event (`BroadcastingEventSink.cs:26-30`, `EventBroadcaster.cs:82-93`). The
handshake takes the lock of the broadcaster first (`EventsEndpoint.cs:61`, `EventBroadcaster.cs:33`).
The comment in `BroadcastingEventSink.cs:10-14` is correct for the two locks that it names.

**[Inferred]** When the handshake also enters the serial section, its order must be: the serial
section first, the broadcaster second. The opposite order gives two threads that each wait for the
other. TASK-0034 says this in its notes, and the acceptance test for it runs subscriptions and
commands in parallel.

### A commit that stops in the middle

**[Observed]** The commit has five steps in this order (`CommandBus.cs:101-138`): append the command
to the log, append the event `command.applied` to the sink, add each body and append its event, and
advance the version. Each append to the sink is an `await` on code that the host supplies.

**[Inferred]** Finding E4 names the token. The token is one of two causes. A sink that throws gives
the same result: the log holds the command, and the version, the bodies and the events do not agree
with it. The idempotency cache then has no result for that command (`CommandBus.cs:51-52`), so a
second request with the same identifier runs the handler again. The backend already holds the body,
and it throws (`InProcessMeshBackend.cs:25-27`).

**[Recommended]** Change the Document first, and publish second. Compute each sequence number, append
the command, add each body, advance the version, and store the result in the cache. Then append the
events with `CancellationToken.None`. A reader inside the serial section cannot see the state between
the two parts. A failure of the sink then loses an event, which a subscriber recovers with a reset,
and it never loses a part of the Document.

### What the new findings add to the task

**[Inferred]** Three new findings meet in the session. Finding E9: the handshake inside the serial
section cannot see a commit in the middle, so it loses no event and repeats none. Finding E10: the
session builds the one bus of a Document, so no second bus can start the sequence again. Finding
E7: a host that loads a log through the session must see a replay that fails. TASK-0034 names none
of the three. Each one is a test that the task can add at a low cost.

### Two semaphores

**[Observed]** `CommandBus` owns its semaphore. `Replay.ReplayLog` (`Engine.Core/Replay.cs:24`) and
seven test files build a bus with no session. **[Recommended]** Keep the semaphore of the bus. The session takes its own serial section
first and then calls the bus. The inner semaphore is then always free, and the order is fixed: the
session, the bus, the sink, the broadcaster.

## 6 Recommended approach

1. Add `Engine.Tests/DocumentSessionConcurrencyTests.cs` with the probe of section 5. Give it a limit
   of 5,000 commands, so that a run stays below two seconds. Record in the Outcome block that it
   fails on the code of today, with the exception types.
2. Add `Engine.Core/DocumentSession.cs`. It owns the parts of `EngineKit` and one `SemaphoreSlim`. It
   gives three methods: apply a command, run a query, and read with a function that receives the
   Document and the sink.
3. Change the commit to "change, then publish", as section 5 recommends, and use
   `CancellationToken.None` for each append after the log append. Add the test of TASK-0034 for a
   cancelled token, and one test with a sink that throws.
4. Make both hosts use the session. The handshake calls `AttachAndPrime` inside the read method of
   the session.
5. Correct `Engine.Tests/QueryBusTests.cs:23,33`: the assertion on a sink that the bus never receives
   proves nothing.
6. Correct the comment in `ManifoldGeometryBackend.cs:13-14`.
7. Add three tests for the new findings: the first live sequence number after a reset equals the
   snapshot version plus one (E9); a second bus on the Document of a session is refused or continues
   the sequence (E10); `AdvanceVersion` refuses a lower value.
8. Do not add a reader-writer lock and do not add snapshot isolation. TASK-0034 excludes both, and
   the architecture challenge gives the condition for a change: a read that takes longer than one
   frame while a person edits.

## Questions for the owner

Each question has a recommendation. An answer of "yes" accepts it.

**Q1. Accept "change, then publish" for the commit, inside TASK-0034?** Recommendation: yes. It is a
change to the order of steps in `CommandBus`, and ADR-0006 §1 says that a command applies atomically. If you see it
as a change to a Decision, the ADR gains a line in "History" with your acceptance.

**Q2. Keep the findings with no task in one register entry, R-0027?** Recommendation: yes. The entry
has the class `debt` and a limit of 180 days. Two of its parts are one line each, E5 and the datetime
part of E6, and one evening corrects both after TASK-0034.

**Q3. Which close status is correct for an invalid subscribe frame: 1007, as the code sends, or 1003,
as `docs/diagnostics.md` and TASK-0010 say?** Recommendation: 1007. It means that the payload is not
valid, the test asserts it, and no client exists yet. The registry text then follows the code.

**Q4. Correct the three high findings of the gates, T3 and T4, before TASK-0034 starts?**
Recommendation: yes, in one task of one evening. Each code task relies on the gates, and four Ready
tasks permit a change to each gate file.

**Q5. Correct the heartbeat loop, E8, and add the three checks of E15, before TASK-0038 mounts the
surface in the desktop host?** Recommendation: yes. Register entry R-0029 gives the heartbeat a limit
of 30 days.
