---
id: 0435
title: No time decides a test, a crash of a test host names its cause on each system, and a known fault of the GitHub service gets one re-run
status: Active
phase: P0.173
opened: 2026-11-21
depends-on: [0434]
governed-by: [0004, 0025, 0033, 0035, 0037, 0039, 0100, 0124, 0149, 0154, 0156, 0164, 0168, 0173]
writes:
  create:
    - tasks/TASK-0435-a-red-run-names-a-fault-of-the-code.md
    - Engine.Tests/Pipeline/CrashReportTests.cs
    - Engine.Tests/Pipeline/LostRunner/artifact-service-timeout.json
    - Engine.Tests/Pipeline/LostRunner/crashed-test-host.json
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - Engine.Tests/Http/DesktopSurfaceTests.cs
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - Engine.Tests/DocumentSessionReentryTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - 3DEngine.ImageTests/SessionTurnsTests.cs
    - Engine.Tests/Pipeline/LostRunnerTests.cs
    - Engine.Tests/Pipeline/TestStepTests.cs
    - .github/workflows/ci.yml
    - .github/workflows/rerun-lost-runner.yml
    - eng/lost-runner.js
    - eng/report-failed-tests.js
    - eng/test-floor.txt
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
    - docs/adr/0164-the-findings-of-the-review-of-2026-11-17.md
    - docs/adr/README.md
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0435 — No time decides a test, a crash of a test host names its cause on each system, and a known fault of the GitHub service gets one re-run

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered "Start P0.173" on 2026-11-21, after four red runs in one night. No run was red for a fault of the code:

1. Attempt 1 of run 44758589835 (windows-latest, step "Test"): the test host of `3DEngine.ImageTests` crashed. The annotation
   named `ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free` as the test that did not
   complete, and then 2000 characters of the output of other tests, with no cause.
2. Run 44831092608 (macos-latest, step "Keep the determinism hash"): "Failed to CreateArtifact: Failed to make request after 5
   attempts: Request timeout: /twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact". Each test passed.
3. Run 44866049139 (macos-latest, step "Test"):
   `FixedLengthsTests.The_Fixed_Lengths_Of_A_Stair_Of_600_Lines_Take_A_Time_Near_The_Status` failed with "The fixed lengths
   took 00:00:00.9622113, and the status 00:00:00.1246249". The commit changed one task file.
4. Attempt 1 of run 44869058349 (ubuntu-latest, step "Test"): the test host of `Engine.Tests` crashed. The annotation named
   `TaskGovernanceGateTests.Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set` as the test that did
   not complete, and then 2000 characters of "engine-api-http has no document file", with no cause. Linux kept no dump. The
   next run of the same code passed.

The ledger entry v0.418 said "with no test named" for item 4. The public annotations (read on 2026-11-21) show a name; they
show no cause. The rule of the owner since ADR-0173 (TASK-0417): a run is red only for a fault of the code; a time limit
does not decide a test that passes (item 4); a lost runner gets one automatic re-run, and each failed attempt stays named in
the ledger.

## Goal

A run is red only for a fault of the code, and a red run names that fault.

## Scope (in)

1. No time decides a test: each assertion of `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and
   `3DEngine.VisionTests` whose result depends on the clock gets a count of the work, an order with the rule of 60 s of
   ADR-0173 item 4, or no limit when the test has another real assertion.
2. A crash of a test host gives a dump on Windows, Linux and macOS, a failed job keeps the dumps and the sequence files, and
   `eng/report-failed-tests.js` gives the cause from the error output of the test host. A crash stays red.
3. A failed upload or download of an artifact with a known fault of the service gets the one re-run of ADR-0173, with the
   saved run 44831092608 and the mixed cases.
4. ADR-0173 items 1, 2 and 4 and its title; ADR-0173 item 4 amends the tests of the time of ADR-0154 item 4 and ADR-0164
   item 5, which said that a time decides the test of the fixed lengths. The ledger entry v0.419, the roadmap and the counts
   of the tests.

## Scope (out)

- A count of the work of `Engine.Kernel`: the write set forbids the kernel (R-0073).
- A guard of 10 s to 30 s that ends a wait for an event that must come (the list under "Decisions"). Such a guard stops a
  hang; no run failed on one.
- The causes of the two crashes. The dumps of the next crash give them.

## Decisions

Each timing assertion that the search found (`Stopwatch`, `Elapsed`, `TimeSpan` in an assertion, a delay whose end is the
result), and the choice:

| Test | The time | The choice |
|---|---|---|
| `FixedLengthsTests.The_Fixed_Lengths_Of_A_Stair_Of_100_Lines_…` (was `…_Take_Less_Than_One_Second`) | status and analysis under 1 s | A count. `SketchSolver.StatusWork` and `AnalysisWork` count the products of two numbers of J·Jᵀ, of the elimination (a row operation and a sum of the back-substitution), of J·g and of the residual. The analysis computes at most 6 times the products of the status: 5.0 times today. One elimination for each open line (the form before TASK-0391) computes about 67 times. |
| `FixedLengthsTests.The_Fixed_Lengths_Of_A_Stair_Of_600_Lines_…` (was `…_Take_A_Time_Near_The_Status`) | analysis under 6 × status + 100 ms | The same count: 5.1 times today; the back-substitution of each column before TASK-0400 computes about 180 times. |
| `DesktopSurfaceTests.A_Host_With_A_Subscriber_Ends_It_When_It_Stops` (was `…_Stops_At_Once`) | stop under 5 s | No limit of the host. The host of the test has no stop limit, so only the end of the subscriber lets the stop end; 60 s stops a wait for ever. The old test did not protect E49: with the stop limit of 2 s of the surface (TASK-0067), a host that keeps its subscriber stopped in 2 s, and the old test passed with that fault (a run on 2026-11-21). |
| `DesktopSurfaceTests.A_Host_With_A_Partial_Request_Stops_With_The_Stop_Limit_Of_The_Surface` (was `…_Within_5_Seconds`) | stop under 5 s | The protection of E58 is the limit of the surface, so the test reads `HostOptions.ShutdownTimeout` of the host and finds `EngineSurface.ShutdownTimeout`; the stop with the partial request ends, with 60 s against a wait for ever. |
| `CreateSphereAndTorusCommandTests.A_Sphere_Cuts_A_Hole_Of_One_Name` | cut and faces under 300 s | No limit: the test asserts the holes and the name of the face. A count of the exact signs needs `Engine.Kernel` (R-0073). The hang limit of 2 minutes of the step "Test" still stops the form before TASK-0289 (about 3 minutes on the computer of the owner). |
| `HttpConcurrencyTests…Do_Not_Deadlock`, `clock.Elapsed >= 1 s` | a lower bound | Kept. Each loop ends only when the clock is at 1 s or more, so the condition is true on each runner; the stall rule of TASK-0426 already holds. |
| `DocumentSessionReentryTests`, `Limit` of 2 s | a call must end in 2 s | 60 s. The result is the type of the exception and the version; the limit only ends a call that waits for ever. |
| `BodyActionsTests.The_Entry_Of_The_Face_Does_Not_Wait_For_A_Long_Command` | the entry must end in 2 s; the hold ended after 10 s | An order: the hold ends only when the test releases it, so the entry ends while the read holds the section, or not at all; 60 s against a wait for ever. |
| `SheetPageTests.The_Commands_Run_Off_The_Frame_Thread_In_Their_Order` | Poll must return in 5 s | An order: the work waits until the test opens the gate after the Poll; 60 s against a wait for ever. |
| `SessionTurnsTests.The_Session_Of_A_Destroyed_Activity_Ends_Before_Its_Window` | the first session waited 5 s | 60 s. A wait of 5 s that ended before the second session began let the first session take the window. |

Kept with no change, because the time cannot make a correct test fail:
`HeartbeatTests` (the upper bound grows with the measured time), `HostGuardTests` (a delay of 5 s before a check that nothing
happened: a slow runner can only pass it), `SessionTurnsTests` (an expected end of a hold of 50 ms), `BookcaseCaseTests` and
`SheetViewTests` (times in the output only), `HttpConcurrencyTests` (the limits compared with the hang limit are constants).

Guards of 10 s to 30 s that end a wait for an event that must come, with no change: `EngineHostListenersTests` (10 s),
`EventsHandshakeTests` (10 s), `EventsEndpointLagTests` (10 s), `ProposalsEndpointTests` (10 s), `LocalSessionHostTests`
(10 s), `HeartbeatTests` (15 s), `BodyActionsTests` (30 s for the start of a read), `SheetPageTests` (30 s for the end of a
command), `EngineHostStopTests`,
`EngineHostSubscriptionTests`, `DesktopSurfaceTests`, `HostGuardTests` and the four `Http*ScenarioTests` (30 s each).

The crash (part 2): `--blame-crash --blame-crash-dump-type mini` on each system. Windows takes the dump with the dump tool of
the test platform, as since TASK-0426. On Linux and macOS the test platform sets the environment of the test host so that
createdump of the .NET runtime writes the dump; the runtime of the runners holds createdump, so no tool is installed. A failed
job keeps `*.dmp` and `Sequence_*.xml` as the artifact `test-host-crash-<system>`, one name for each job of the matrix. The
report script already named the test that did not complete from the sequence file; it gave the first 2000 characters of the
reason, and the runtime writes its report at the end. Now the cause is the text from the last report of the runtime
("Unhandled exception.", "Stack overflow.", "Process terminated.", "Fatal error."), or the last 1000 characters, and the
names of the dumps come before it.

The re-run (part 3): a failed job is a fault of the service when each of its failed steps is a step of `ci.yml` whose `uses:`
is `actions/upload-artifact` or `actions/download-artifact`, and each of its annotations of the level "failure" holds both
texts of a known fault: today "Failed to CreateArtifact" and "Request timeout". The script reads the names from `ci.yml` of
the default branch, as text; the workflow checks out that file too. A failed set-up of an action ("Set up job") has no known
text yet, so it gets no re-run; a later case adds the step and its text with their evidence. The two saved runs are from the
public API: the first attempts of runs 44831092608 and 44869058349.

## Acceptance criteria

- [x] Each assertion of the table has its choice, and an injection of the fault that it protects makes it fail.
- [x] The step "Test" gives the crash options on each system, and a failed job keeps the dumps and the sequence files.
- [x] The report gives the cause and the dumps of a crash (`CrashReportTests`).
- [x] `eng/lost-runner.js` re-runs run 44831092608 and keeps each mixed case red (`LostRunnerTests`).
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0173-red-runs) · v0.419

## Method

Mechanical: the counts of `SketchSolver`, the rule of 60 s in five tests, the two saved runs from the public API, the text of
ADR-0173, of `docs/templates.md` section 5 and of the register, and the counts of `eng/test-floor.txt`.

Judgement: the choice of each row of the table; the products of two numbers as the work of the analysis (the time of a
runner changed by a factor of eight for the same code); the cause of a crash at the end of the error output; the names of the
steps of the service from `ci.yml` of the default branch.

Weakest: the dump on Linux and macOS, the report of the runtime in the reason of the test platform, and the re-run of a fault
of the service run only in the pipeline. A local run on Windows cannot prove them; the next crash and the next fault of the
service in a run on `main` prove them, and the ledger names that run.

Injections (`inject.js` in `%TEMP%\p0173`), each restored byte for byte (sha256), each with the named test failing:

| Injection | File | Failing test |
|---|---|---|
| I1 the back-substitution of each column (before TASK-0400) | `SketchSolver.cs` | `…Stair_Of_600_Lines_Compute_At_Most_Six_Times…` (and the 100 lines) |
| I2 one elimination for each open line (before TASK-0391) | `SketchSolver.cs` | `…Stair_Of_100_Lines_Compute_At_Most_Six_Times…` (and the 600 lines) |
| I3 the stop does not end the subscriber (E49) | `EventsEndpoint.cs` | `A_Host_With_A_Subscriber_Ends_It_When_It_Stops` |
| I4 no stop limit of the surface (E58) | `EngineSurface.cs` | `A_Host_With_A_Partial_Request_Stops_With_The_Stop_Limit_Of_The_Surface` |
| I5 the entry of the face does not use the kept result (V109) | `BodyActions.cs` | `The_Entry_Of_The_Face_Does_Not_Wait_For_A_Long_Command` |
| I6 Poll waits for the work | `SheetPage.cs` | `The_Commands_Run_Off_The_Frame_Thread_In_Their_Order` |
| I7 the crash options on Windows only | `ci.yml` | `The_Test_Step_Collects_A_Crash_Dump_On_Each_System` |
| I8 the dump kept on Windows only | `ci.yml` | `A_Failed_Job_On_Each_System_Keeps_The_Dump_And_The_Sequence` |
| I9 one artifact name for the three systems | `ci.yml` | the same |
| I10 no sequence file kept | `ci.yml` | the same |
| I11 the cause is the whole reason | `report-failed-tests.js` | `A_Crash_Names_The_Running_Test_The_Dump_And_The_Report_Of_The_Runtime` |
| I12 no end of the reason | `report-failed-tests.js` | `A_Crash_With_No_Report_Of_The_Runtime_Gives_The_End_Of_The_Error_Output` |
| I13 no names of the dumps | `report-failed-tests.js` | `A_Crash_Names_The_Running_Test_The_Dump_And_The_Report_Of_The_Runtime` |
| I14 no known fault | `lost-runner.js` | `A_Fault_Of_The_Artifact_Service_Gets_A_Re_Run` |
| I15 each failed step counts | `lost-runner.js` | `A_Fault_Of_The_Service_With_A_Fault_Of_The_Code_Stays_Red` |
| I16 one text of a fault is sufficient | `lost-runner.js` | the same |
| I17 another annotation of a failure is accepted | `lost-runner.js` | the same |
| I18 each action is a step of the service | `lost-runner.js` | `The_Steps_Of_The_Service_Are_The_Uploads_And_Downloads_Of_The_Artifacts_Of_Ci` |
| I19 no checkout of `ci.yml` | `rerun-lost-runner.yml` | `The_Workflow_Runs_The_Script_Of_The_Default_Branch_With_A_Small_Token` |
| I20 a lost runner is not counted | `lost-runner.js` | `A_Lost_Runner_And_A_Fault_Of_The_Artifact_Service_Get_A_Re_Run` |

The test of E49 of the base commit, with I3, passed in 5 s: the old limit of 5 s did not see the fault.

## Progress

- 2026-11-21: opened under the order of the owner "Start P0.173", branch `p0173-red-runs` from `1b5827fe` (TASK-0434, K15b,
  v0.418, not merged yet). The search, the table of choices, the dumps on each system, the cause of a crash, the re-run of a
  fault of the artifact service, R-0073, and ledger entry v0.419.
