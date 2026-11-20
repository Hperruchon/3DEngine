---
id: 0417
title: A run of the pipeline is red only for a fault of the code, not for a lost macOS runner or a slow Windows runner
status: Done
phase: P0.168
opened: 2026-11-18
depends-on: []
governed-by: [0173]
writes:
  create:
    - tasks/TASK-0417-a-run-is-red-only-for-a-fault-of-the-code.md
    - .github/workflows/rerun-lost-runner.yml
    - eng/lost-runner.js
    - Engine.Tests/Pipeline/LostRunnerTests.cs
    - Engine.Tests/Pipeline/LostRunner/lost-macos-runner.json
    - Engine.Tests/Pipeline/LostRunner/failed-test.json
    - Engine.Tests/Pipeline/LostRunner/failed-emulator-install.json
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
  modify:
    - Engine.Tests/Http/EngineHostSubscriptionTests.cs
    - docs/templates.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
    - .github/workflows/ci.yml
---

# TASK-0417 — A run of the pipeline is red only for a fault of the code, not for a lost macOS runner or a slow Windows runner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked why many runs failed. The API of GitHub gave 44 runs that started on 2026-11-18: 40 passed and
4 failed, and none of the 4 failed for a fault of the code.

1. **A lost macOS runner (3 runs).** Runs 44329686249, 44337062610 and 44342738663: the job "Gate on macos-latest" ended
   after 15 minutes with "The job was not acquired by Runner of type hosted even after multiple attempts", and a notice
   "Due to capacity constraints, jobs targeting macOS arm64 runners may experience longer queue times." No step ran.
2. **A slow Windows runner (1 run).** Run 44409491212 on `main` at `fa2ff0d`: the job "Gate on windows-latest" failed in
   `EngineHostSubscriptionTests.A_Report_That_Blocks_Does_Not_Hold_The_Session` with `System.TimeoutException`. The test
   gives the first Apply of a new host 5 seconds. The entry v0.330 of the ledger records the same failure for run
   42927620137 on 2026-11-11.

A red run stops a merge (the rule of the session: merge only after a green run) until a person starts a re-run. The gh tool
of the session has no sign-in, so only the owner can start one. The owner answered "Yes, open a task" on 2026-11-18.

## Goal

A lost macOS runner gets one automatic re-run, and the test of a report that blocks has no time limit that decides its result.

## Scope (in)

1. A new workflow `.github/workflows/rerun-lost-runner.yml`. When a run of the workflow "CI" ends with the conclusion
   `failure` on its first attempt, it reads the annotations of each failed job. When each failed job has the text "was not
   acquired by Runner" and no other failure, it asks GitHub for one re-run of the failed jobs
   (`POST /repos/{owner}/{repo}/actions/runs/{run_id}/rerun-failed-jobs`). It does no re-run of a second attempt, and no
   re-run for a failed test or step.
2. The token of the new workflow has `actions: write`, `checks: read` and `contents: read` only, and the workflow checks out
   no code of the branch of the run. A new ADR (the next free
   number at the start) records the reason: a run with `workflow_run` runs the file of the default branch with a token that
   can write, so the workflow must run no file of the branch of the run.
3. The log of the new workflow names the run that it re-ran, so a session can list a failed first attempt for the ledger
   (the API gives a run the result of its last attempt).
4. `A_Report_That_Blocks_Does_Not_Hold_The_Session`: the report blocks until the test releases it after the Apply and the
   Read ended, and the test asserts that both ended while the report still blocked. A long limit (60 seconds or more) only
   stops a hang. A slow runner then makes the test slower, not red.
5. `docs/templates.md`, section 5: the text on the jobs of the pipeline names the new workflow.

## Scope (out)

- `.github/workflows/ci.yml`: no change of its jobs, its matrix or its runners. Three systems stay a condition (CLAUDE.md).
- An automatic re-run of a failed test, of a failed step, or of a job on Linux or Windows.
- Each other time limit of the tests.

## Acceptance criteria

- [x] A first injection gives the new workflow a run whose failed job has a failed step, and the workflow starts no re-run.
- [x] The test of a report that blocks fails when the report holds the serial section (an injection that calls the report
      inside the commit), and passes with a delay of 10 seconds before the first Apply.
- [x] A run of the new workflow on `main` re-runs a run with a lost macOS runner, or the ledger says that no such run came
      before the close, and names the evidence that replaces it.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

A workflow with the trigger `workflow_run` runs only from the default branch, so the first real proof comes after the
merge. On the branch, test the logic of the choice (which job is a lost runner) with the JSON of the jobs and the annotations
of run 44337062610 (lost runner) and run 44409491212 (failed test), saved from the public API. The workflow is a gate file
(`docs/templates.md`, section 2): the write set names it exactly. The first commit adds the path of the new ADR to the write
set, and the path of a script if the choice goes into one (for example `eng/lost-runner.js`, which the workflow reads from
the default branch).

## Outcome

Status: Done · commit (the merge of the branch p0168-red-runs-work) · v0.402

## Method

Mechanical: the choice with three saved runs and five changed copies; eleven injections (`inject0417.js` of the session).
Judgement:  a job that did not pass counts as lost only with the annotation of the level "failure" and no failed step; the
            emulator install is a failed step, so it stays red. The report of the test blocks with no limit, and the
            test releases it; 60 seconds only stops a hang.
Weakest:    the workflow and the calls of the API run first after the merge, from `main`; the tests check the choice and
            the text of the workflow, not a real re-run.

## Progress

- 2026-11-19: opened under the order of the owner "then start TASK-0417", branch p0168-red-runs-work from `main` at `2f9fc00`.
  The write set adds the script, the test, three saved runs, ADR-0173 and the test floor. Scope item 2 named `actions: write`
  only; the annotations need `checks: read` and the checkout needs `contents: read`, so the item now names the three. ADR-0172
  is on the branch responsive-small-windows (TASK-0418), so this task takes ADR-0173. Ledger entry v0.400.
- 2026-11-19: run 44425406463 passed on the branch on each job. No run of the new workflow came before the close, because a
  workflow with `workflow_run` runs only from `main`; the evidence that replaces it is `LostRunnerTests` with the saved run
  44337062610, as the entry v0.400 says. Closed at v0.400; the merge waits for the owner.
- 2026-11-20: the owner ordered "Merge it". TASK-0418 took v0.400 on `main` first (`e4d7b55`), so the branch merged
  `main`: the ledger, the roadmap and the ADR index keep both rows, and this task moves to v0.401. Active until the
  close.
- 2026-11-20: merge `5b5ba9a`: a clean build gives 0 warnings, `dotnet test` passes 2907 tests, and the strict write-set
  check and `eng/repo-gates` pass. Closed again at v0.401.
- 2026-11-20: run 44641637677 of `dfb49ad` passed on each job. Before the merge into `main`, TASK-0419 took v0.401
  (`5691ca7`), so the branch merged `main` a second time and this task moves to v0.402. Active until the close.
- 2026-11-20: merge `2b3cebb3`: a clean build gives 0 warnings, `dotnet test` passes 2921 tests, and the strict write-set
  check and `eng/repo-gates` pass. Closed again at v0.402.
