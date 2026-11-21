---
id: 0173
title: A run of CI whose only failures are jobs that GitHub gave no runner gets one automatic re-run, and no time limit decides a test that passes
status: Accepted
topic: Pipeline
date: 2026-11-19
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - .github/workflows/rerun-lost-runner.yml
  - eng/lost-runner.js
  - Engine.Tests/Pipeline/**
  - Engine.Tests/Http/EngineHostSubscriptionTests.cs
  - Engine.Tests/Http/HttpConcurrencyTests.cs
enforced-by: Engine.Tests/Pipeline/LostRunnerTests.cs
---

# ADR-0173 — A run of CI whose only failures are jobs that GitHub gave no runner gets one automatic re-run, and no time limit decides a test that passes

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
2. **The token and the code that runs.** A run with the trigger `workflow_run` runs the file of the default branch with a
   token that can write. The workflow therefore checks out the default branch only, with no credentials, and runs only
   `eng/lost-runner.js` of that branch, never a file of the branch of the run. The token has `actions: write` (the re-run),
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

## Consequences

- A lost macOS runner costs a second wait and no person. A second loss on the same run stays red, and the owner sees it.
- The first real run of the workflow comes after the merge, because a `workflow_run` workflow runs only from the default
  branch. The tests of the choice use three saved runs of 2026-11-18.
- A failed install of the Android emulator (run 44416434810 on 2026-11-18) is a failed step, so it gets no re-run. A later
  task can decide on it with its own evidence.
- A crash of a test host is a failed step, so it gets no re-run. On Windows the step "Test" collects a mini dump of the crash,
  and a failed job keeps it as an artifact (finding T184, TASK-0426); `Engine.Tests/Pipeline/TestStepTests.cs` reads the step.

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
