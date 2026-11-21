---
id: 0441
title: The test of the resize is one test for each state, and a stop at the hang limit names the limit and not a crash
status: Done
phase: P0.176
opened: 2026-11-21
depends-on: [0440]
governed-by: [0025, 0113, 0114, 0131, 0163, 0172, 0173]
writes:
  create:
    - tasks/TASK-0441-the-resize-test-stays-below-the-hang-limit.md
  modify:
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - eng/report-failed-tests.js
    - Engine.Tests/Pipeline/CrashReportTests.cs
    - .github/workflows/ci.yml
    - eng/test-floor.txt
    - docs/adr/0163-the-desktop-window-resizes-and-maximizes.md
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0441 — The test of the resize is one test for each state, and a stop at the hang limit names the limit and not a crash

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The facts, from the public API of the pipeline (2026-11-20 and 2026-11-21):

1. Attempt 1 of run 44758589835 (windows-latest, step "Test"): the test host of `3DEngine.ImageTests` stopped while
   `ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free` ran. The reason was "The active
   test run was aborted. Reason: Test host process crashed". The step took 179 seconds. Attempt 2 passed. Finding T184 of the
   codebase review of 2026-11-20 named it and found no cause by reading.
2. Run 44908442870 (windows-latest, step "Test", a commit of documents only): the same test, and the reason "Data
   collection : Test case event channel failed: ... An existing connection was forcibly closed by the remote host". The step
   took 169 seconds. The artifact `test-host-crash-windows-latest` held the sequence file and no dump, with
   `--blame-crash --blame-crash-dump-type mini` on the step.
3. Attempt 1 of run 44869058349 (ubuntu-latest): the test host of `Engine.Tests` stopped while
   `TaskGovernanceGateTests.Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set` ran (R-0074). The
   step took 247 seconds; the other runs of ubuntu-latest in this list took 115 to 123 seconds.

R-0062 (closed 2026-11-14, TASK-0382) held a different stop: an access violation in `vkCreateDevice` on the computer of the
owner, when classes created Vulkan devices at the same time. The pipeline does not create a device on Windows: each
`[VulkanImageFact]` skips there (ADR-0025 item 4, R-0040). `ResponsiveLayoutTests` is in the collection of Dear ImGui, and
the test of the resize creates no device; it creates a Dear ImGui context, a font atlas and three frames for each layout.

The step "Test" has `--blame-hang-timeout 2m --blame-hang-dump-type none`. The hang limit holds for each test.

## Goal

The test of the resize ends far below the hang limit of the pipeline on a slow runner, and a stop at the hang limit gives
an annotation that names the limit.

## Scope (in)

1. `ResponsiveLayoutTests`: the test of the resize is a theory with one case for each state. Each case checks the 496 sizes
   of ADR-0163 item 4 in its state. The sizes come from `ResizeSizes()`.
2. Each case counts the layouts that `Faults` checks, and fails above 1,000 layouts (`LayoutsPerTest`) or at 0.
3. `eng/report-failed-tests.js`: the message of the blame data collector "The specified inactivity time of <limit> has
   elapsed" in a .trx file gives the text "A test ran longer than the hang limit of <limit>, so the test platform stopped
   the test host: a hang, not a crash.", and no text "No dump of the crash.".
4. `CrashReportTests`: a test with the facts of run 44908442870 and the message of the collector.
5. The false comment of the step "Test" in `.github/workflows/ci.yml` ("the slowest test today has its own limit of 30
   seconds").
6. A progress line for R-0074, which can be a stop of the same kind.

## Scope (out)

- The time of `TaskGovernanceGateTests` (R-0074). The evidence is weaker there, and a change of the gate is a task of its
  own. The register is full (15 of 15), so this task adds no entry.
- A faster `DesktopUi`, for example one font atlas for each interface. The constructor is product code, and the theory
  gives the margin with no change of it.
- A change of the hang limit. A larger limit hides a slower test until the next state.

## Acceptance criteria

- [x] The test of the resize is 15 cases, one for each state, and each case checks 496 layouts.
- [x] A case that checks more than 1,000 layouts or no layout fails, with the count in its message.
- [x] A local run with a hang limit of 20 seconds gives the reason, the sequence file and no dump of the runs on Windows.
- [x] With the message of the collector, the annotation names the hang limit (`CrashReportTests`).
- [x] Each injection makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- The cause is the hang limit, not a crash of the code. The evidence:
  1. The test took 41 seconds alone on the computer of the owner, 57.5 seconds in the whole step on four logical
     processors, and 67.6 seconds on two (`start /affinity`). The runner of windows-latest has four slower processors.
  2. A local run of the test alone with `--blame-hang-timeout 20s --blame-hang-dump-type none --blame-crash
     --blame-crash-dump-type mini` gave "The active test run was aborted. Reason: Test host process crashed", the sequence
     file with the test not completed, and no dump: the facts of run 44908442870. The .trx file held a second message with
     the outcome Warning: "Data collector 'Blame' message: The specified inactivity time of 20 seconds has elapsed.".
     The script did not read it, because it reads the outcome Error only.
  3. A crash of the code on Windows writes a mini dump with `--blame-crash` (TASK-0426). Run 44908442870 had that option
     and no dump.
  4. ADR-0163 gave 486 sizes in four states in about 8 seconds. ADR-0172, ADR-0174, ADR-0175 and ADR-0178 added eleven
     states to the same test, so it checked 7,440 layouts.
- The other candidates of the order are out, each by reading: each layout makes, uses and disposes its own context on the
  thread of the test (`Faults`, `using var ui`); the collection of Dear ImGui runs its tests one after the other; the atlas
  is owned by its context, and the font data is freed after `DestroyContext`; the smallest display of the test is 360 by
  560 (`DesktopUi.MinimumWindowWidth` and `MinimumWindowHeight`), so no size of 0 or less reaches Dear ImGui; the layout
  has no recursion that grows with a smaller display.
- A count of the work, not a time (ADR-0173 item 4). The stopped runs took more than 120 seconds for 7,440 layouts, so at
  least 16 milliseconds for each layout on windows-latest. At that rate the 1,000 layouts of the limit take about 16
  seconds, and the 496 layouts of one case about 8 seconds.
- One case for each state, and not one case for each line of the resize: a new state adds a case of 496 layouts and
  keeps the margin. A smaller step of the sizes fails the count.
- The counter is a static field of the class. The tests of the collection of Dear ImGui run one after the other, and only
  this class calls `Faults`, so no other test adds to the count while a case runs.
- The words of the collector come from the SDK 10.0.400 (the resource string "The specified inactivity time of {0} {1}
  has elapsed. Collecting hang dumps from testhost and its child processes", read from its files) and from a local run with
  the English text (`DOTNET_CLI_UI_LANGUAGE=en`). No .trx file of the pipeline is kept, so the form on the runner is not
  seen.

## Outcome

Status: Done · commit (the merge of the branch p0176-image-test-crash) · v0.425

## Method

Mechanical: the theory of the states, the sizes as a method, the count of the layouts, the counts of
`eng/test-floor.txt`, the text of the comment of the step, the history of ADR-0163 and ADR-0173.

Judgement: the cause from the evidence above; the limit of 1,000 layouts; the text of a hang in the annotation.

Weakest: the time of the test on the runner of windows-latest is not seen. The pipeline keeps no .trx file of a passed
job, and the job log needs admin rights. The cause rests on the same reason, the same files and no dump in a local stop at
a smaller limit, and on the times of the steps. Only the next runs of windows-latest prove it: no stop of the test of the
resize.

The runs (results in `%TEMP%\p0176`). The order asked for loops of 300 runs of the test. One run took 41 seconds, so 300
runs take about 3.5 hours; the session ran the stop itself at a smaller limit and the step on fewer processors in place of
the loop. No run stopped at the limit of 2 minutes on this computer.

| Run | Form | Result | Time of the test of the resize |
|---|---|---|---|
| r0 | the test alone | passed | 41 s |
| hang20, hang20en | the test alone, `--blame-hang-timeout 20s --blame-hang-dump-type none --blame-crash --blame-crash-dump-type mini`, French and English text | "Test host process crashed", the sequence file, no dump, and the message of the collector with the outcome Warning | stopped at 20 s |
| ci4a | each test project with the options of the step, on 4 logical processors (`start /affinity F`) | 3111 passed | 57.5 s (the governance gate of R-0074: 56.6 s) |
| ci2a | the same on 2 logical processors | 3111 passed | 67.6 s (the governance gate: 89.8 s) |
| ci1a | the same on 1 logical processor | 3111 passed | 68.2 s (the governance gate: 38.1 s) |
| after1 | the class after the change | 42 passed | 1.8 to 2.9 s for each case |
| full | `dotnet test 3DEngine.sln` after the change, each processor | 3126 passed | at most 5.2 s for each case (the governance gate: 100.2 s) |

Other sessions ran tests on this computer at the same time, so the times vary between runs.

Injections (`inject.js` in `%TEMP%\p0176`), each restored byte for byte (sha256), each with the named test failing:

| Injection | Failing test |
|---|---|
| I1 the form before TASK-0441: one case checks each state | `Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free(state: Nothing)`: "The test checked 7936 layouts, and the limit is 1000" |
| I2 `Faults` does not count | the same: "The test checked 0 layouts" |
| I3 a step of 2 pixels | the same: "The test checked 1981 layouts" |
| I4 no text of a hang in the message of a running test | `A_Stop_At_The_Hang_Limit_Names_The_Limit_And_Not_A_Crash` |
| I5 a hang says "No dump of the crash." | the same |
| I6 the hang limit only from a message with the outcome Error | the same |
| I7 the script of `391c66a6` | the same |

The first forms of I1 and I2 did not compile (the analyzer xUnit1026 and the warning CS0649 as an error), so the injections
keep the parameter and the field in use.

## Progress

- 2026-11-21: opened, branch `p0176-image-test-crash` from `391c66a6` (P0.175, TASK-0440, v0.424, not merged yet). The
  cause, the theory of the states with a count of layouts, the text of a hang in the annotation, a test, the comment of the
  step, injections, ADR-0163 and ADR-0173, R-0074, the roadmap and ledger entry v0.425.
- 2026-11-21: run 44920237330 of `fe1809fd` passed on each job. Closed at v0.425.
