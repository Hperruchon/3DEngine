---
id: 0173
title: A run of CI whose only failures are faults of the GitHub service gets one automatic re-run, and no time limit decides a test that passes
status: Accepted
topic: Pipeline
date: 2026-11-19
supersedes: []
superseded-by: []
amends: ['0154', '0164']
amended-by: []
affects:
  - .github/workflows/rerun-lost-runner.yml
  - .github/workflows/ci.yml
  - eng/lost-runner.js
  - eng/report-failed-tests.js
  - Engine.Tests/Pipeline/**
  - Engine.Tests/Http/EngineHostSubscriptionTests.cs
  - Engine.Tests/Http/HttpConcurrencyTests.cs
enforced-by: Engine.Tests/Pipeline/LostRunnerTests.cs
---

# ADR-0173 — A run of CI whose only failures are faults of the GitHub service gets one automatic re-run, and no time limit decides a test that passes

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked why many runs failed. Of the 44 runs that started on that day, 40 passed and 4 failed, and none
failed for a fault of the code (TASK-0417):

- In three runs the job on macOS ended after 15 minutes with "The job was not acquired by Runner of type hosted even after
  multiple attempts". GitHub had no free macOS arm64 runner, and no step ran.
- In one run the test `A_Report_That_Blocks_Does_Not_Hold_The_Session` gave the first Apply of a new host 5 seconds, and a
  slow runner on Windows passed that limit. The same test failed in the same way on 2026-11-11 (the ledger entry v0.330).

A session merges only after a green run. The gh tool of the sessions has no sign-in, so only the owner can start a re-run, and
a red run with no fault of the code stops the work until the owner sees it.

## Decision

1. **One automatic re-run of a lost runner.** The workflow `.github/workflows/rerun-lost-runner.yml` runs after each run of
   "CI" (trigger `workflow_run`). When the run failed on its first attempt, `eng/lost-runner.js` reads its jobs and the
   annotations of each job that did not pass. When each such job has an annotation of the level "failure" with the text
   "was not acquired by Runner" and no failed step, the script asks GitHub for one re-run of the failed jobs. A failed test, a
   failed step, a run of another workflow and a second attempt get no re-run, so each fault of the code stays red.
   A job also counts as a fault of the service (TASK-0435) when each of its failed steps is an upload or a download of an
   artifact, and each of its annotations of the level "failure" is a known fault of the service. The script reads the names
   of those steps from `.github/workflows/ci.yml` of the default branch, as text: each step whose `uses:` is
   `actions/upload-artifact` or `actions/download-artifact`. The one known fault today holds both texts "Failed to
   CreateArtifact" and "Request timeout" (run 44831092608). A failed set-up of an action has no known text yet, so it stays
   red. A failed test, a crash of the test host, each other failed step and each other annotation of a failure in the same
   job or in another job keep the whole run red.
2. **The token and the code that runs.** A run with the trigger `workflow_run` runs the file of the default branch with a
   token that can write. The workflow therefore checks out the default branch only, with no credentials, and runs only
   `eng/lost-runner.js` of that branch, never a file of the branch of the run. It also checks out `.github/workflows/ci.yml`
   of the default branch, which the script reads as text and does not run (TASK-0435). The token has `actions: write` (the re-run),
   `checks: read` (the annotations) and `contents: read` (the checkout), and nothing else.
3. **The record of a failed attempt.** The API gives a run the result of its last attempt, so a re-run that passes hides the
   failed first attempt. The log of the workflow names each run, its attempt and the choice, so a session can list a failed
   first attempt in the ledger.
4. **No time limit decides a test that passes.** In `A_Report_That_Blocks_Does_Not_Hold_The_Session` the report blocks until
   the test releases it, after the Apply and the Read ended. A report inside the commit can then never let the Apply end, and
   a slow runner only makes the test slower. A limit of 60 seconds stops a test that waits for ever. The same rule holds in
   `HttpConcurrencyTests` (finding T183, Q8 A of the owner, 2026-11-20): the test of a deadlock fails only when no operation
   ends for 60 seconds, so the time of the work is not a result. A request of the class stops only after 90 seconds, after
   that limit, so a deadlock fails with its own text and not as a cancel, and before the hang limit of 2 minutes of the step
   "Test" (finding T185 of the codebase review of 2026-11-21).
   The rule holds for each test (TASK-0435). A test that protects a cost compares a count of the work, not a time: the
   analysis of `FixedLengthsTests` computes at most six times the products of two numbers of the status
   (`SketchSolver.StatusWork` and `AnalysisWork`). A test that protects an order holds the other side until the result comes,
   as the report of item 4 does, with 60 seconds against a wait for ever: the entry of a face in `BodyActionsTests`, the Poll
   of `SheetPageTests`, the turn of `SessionTurnsTests` and the calls of `DocumentSessionReentryTests`. A test that protects
   a stop removes the limit that hid the fault: in `DesktopSurfaceTests` the host of the subscriber has no stop limit, and the
   host of the partial request keeps the limit of the surface. A test with another real assertion drops its limit: the cut of
   a plate by a sphere in `CreateSphereAndTorusCommandTests`; a count of the work of the kernel waits for R-0073.

## Consequences

- A lost macOS runner costs a second wait and no person. A second loss on the same run stays red, and the owner sees it.
- The first real run of the workflow comes after the merge, because a `workflow_run` workflow runs only from the default
  branch. The tests of the choice use three saved runs of 2026-11-18.
- A failed install of the Android emulator (run 44416434810 on 2026-11-18) is a failed step, so it gets no re-run. A later
  task can decide on it with its own evidence.
- A crash of a test host is a failed step, so it gets no re-run. On Windows the step "Test" collects a mini dump of the crash,
  and a failed job keeps it as an artifact (finding T184, TASK-0426); `Engine.Tests/Pipeline/TestStepTests.cs` reads the step.
  Since TASK-0435 the step collects the dump on Linux and macOS too, with createdump of the .NET runtime, and a failed job on
  each system keeps each dump and each sequence file as the artifact `test-host-crash-<system>`. `eng/report-failed-tests.js`
  names each test that was running, the dumps, and the report of the runtime at the end of the error output of the test host
  (`CrashReportTests`).
- The choice of a fault of the service depends on the names of the steps of `ci.yml` on the default branch. A branch that
  renames a step of an artifact gets no re-run for it until the merge, so its run stays red.

## Alternatives considered

- **Leave the macOS job out of the branch runs.** Rejected: CLAUDE.md makes three systems a condition for each run.
- **A larger time limit in the test.** Rejected: a slower runner can pass any limit, and the limit would still decide the
  result.
- **A re-run of each failed run.** Rejected: a failed test would turn green by chance, and the owner would not see it.

## History

- 2026-11-19 · TASK-0417 · The workflow, the choice, the token and the test with no deciding limit (phase P0.168).
- 2026-11-21 · TASK-0426 · Item 4 holds for `HttpConcurrencyTests`: a deadlock is no progress for 60 seconds (finding T183, Q8 A
  of the owner); a crash of a test host on Windows keeps a mini dump (finding T184).
- 2026-11-21 · TASK-0431 · Item 4: a request of `HttpConcurrencyTests` stops after 90 seconds, after the stall limit of 60
  seconds (finding T185).
- 2026-11-21 · TASK-0435 · Items 1, 2 and 4 and the title: a known fault of the artifact service in an upload or a download
  gets the one re-run (run 44831092608); the script reads the steps of the service from `ci.yml`; no time decides a test, by
  a count of the work, an order with 60 seconds against a wait for ever, or no limit (phase P0.173). A crash of the test host
  gives a dump and its cause on each system. Item 4 amends ADR-0154 item 4 and ADR-0164 item 5, the tests of the time of the
  fixed lengths.
