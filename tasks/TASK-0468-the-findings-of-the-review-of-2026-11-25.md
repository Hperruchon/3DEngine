---
id: 0468
title: The medium and low findings of the codebase review of 2026-11-25
status: Done
phase: P0.189
opened: 2026-11-25
depends-on: [0472]
governed-by: [0025, 0092, 0100, 0111, 0124, 0127, 0158, 0168, 0173, 0188]
writes:
  create:
    - tasks/TASK-0468-the-findings-of-the-review-of-2026-11-25.md
    - tasks/TASK-0473-the-other-findings-of-the-review-of-2026-11-25.md
  modify:
    - Engine.Tests/Http/ProposalAcceptTests.cs
    - Engine.Tests/Pipeline/LostRunnerTests.cs
    - eng/lost-runner.js
    - eng/test-floor.txt
    - Engine.Tests/Http/EngineHostListenersTests.cs
    - Engine.Tests/Http/EventsHandshakeTests.cs
    - Engine.Tests/Http/EventsEndpointLagTests.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - Engine.Tests/Http/HeartbeatTests.cs
    - Engine.Tests/Http/EngineHostStopTests.cs
    - Engine.Tests/Http/EngineHostSubscriptionTests.cs
    - Engine.Tests/Http/DesktopSurfaceTests.cs
    - Engine.Tests/Http/HttpBodyNameScenarioTests.cs
    - Engine.Tests/Http/HttpDatumScenarioTests.cs
    - Engine.Tests/Http/HttpImageScenarioTests.cs
    - Engine.Tests/Http/HttpSheetScenarioTests.cs
    - Engine.Tests/Http/HttpSketchScenarioTests.cs
    - 3DEngine.ImageTests/LocalSessionHostTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - docs/guides/agent-loop.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
---

# TASK-0468 — The medium and low findings of the codebase review of 2026-11-25

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-25 (`docs/reviews/2026-11-25-codebase-review.md`, TASK-0467) found two medium and eleven low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

The owner answered the questions of the review on 2026-11-26 ("All A, merge it"). Q2 A decides E242: the reversal of a failed
group puts nothing on the redo list, and the log marks it. Q3 A decides T197: keep the rule of ADR-0188 "Bad", and the test
holds the fork. On 2026-11-26 the owner also gave a standing order until 09:00 on 2026-11-27: P0.190, then this task, then
P11.0, each merged after green continuous integration, with the recommended option of each question.

P0.190 (TASK-0472) did V238: on the order of the owner the history of git was rewritten, and no version of a file holds the
folder of the owner. This task does not change `WaitingWordTests` for V238.

## Goal

The four findings that this task does have a correction and a test, and each other finding has a task (TASK-0473) or a
register entry with its reason.

## Scope (in)

1. Medium: T197 (a race of the worker and the accept decides
   `A_Commit_That_Does_Not_Change_The_Result_Runs_One_Fork_And_Applies`), the exit of R-0078. The test holds the fork of the
   version of the `NoOp` with `BeforeFork` until the proposal is `Applying`, and then releases it, as
   `A_Commit_Between_The_Comparison_And_The_Apply_Makes_The_Accept_Compare_Again` does.
2. Low: T201. Each guard of the list of TASK-0435 that ends a wait for an event that must come gets the limit of 60 s of
   ADR-0173 item 4.
3. Low: T202. `eng/lost-runner.js` also knows "Failed to FinalizeArtifact" with "ECONNRESET", with a test on the text of the
   annotation of run 45707816188.
4. Low: E244. Section 12 of `docs/guides/agent-loop.md` says what a known `commandId` gives, and the row of
   `E-API-PROPOSAL-COMMAND-KNOWN` in `docs/diagnostics.md` gets a dated note that replaces its first claim.
5. The register: R-0078 closes, and R-0081 holds the question of the mark of E242.
6. The roadmap: row P0.191 (TASK-0473) gets the other findings.

## Scope (out)

- E242, E243, E245, V236, V237, T198, T199 and T200, and ADR-0188 items 5, 7 and 9 and "Bad": TASK-0473 (P0.191). The time of the
  standing order is short, and E242 needs the owner first (see below).
- E242 in this task. The smallest mark of the reversal is the group of the proposal on its `Undo`. That gives the field `GroupId`
  of `Engine.Contracts` a new meaning, and an older app then refuses a newer file at an open. This task said "stop and ask the
  owner first" for such a mark, and `CLAUDE.md` ("Stop and ask") says the same, so R-0081 asks the owner.
- V238: P0.190 did it.

## Acceptance criteria

- [x] Each finding of this task has a test that fails before its correction: the injection of the order of run 45574730547 makes
  `A_Commit_That_Does_Not_Change_The_Result_Runs_One_Fork_And_Applies` fail with "open", and the removal of the pair makes
  `A_Reset_Of_The_Artifact_Service_Gets_A_Re_Run` fail. T201 and E244 change a limit and a text, and no result.
- [x] Each other finding has a row in TASK-0473, and E242 has the register entry R-0081.
- [x] `docs/register.md` holds 13 open entries of 15, as before.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- `depends-on` names TASK-0472 (P0.190), the last task before this one on `main`. Until 2026-11-27 it named TASK-0467.
- `governed-by` names each ADR whose `affects` holds a file of the write set. ADR-0025, ADR-0092, ADR-0100, ADR-0111, ADR-0124,
  ADR-0127, ADR-0158 and ADR-0168 hold a test file of T201 only; a longer guard changes no decision of them.
- Make each branch from `origin/main` after the rewrite of 2026-11-26.

## Outcome

Status: Done · commit (the merge of the branch p0189-findings) · v0.456

## Method

Mechanical: the limit of 60 s in each guard; the pair of the reset in the list; the text of the guide and of the note.
Judgement:  the hold of the fork until the proposal is `Applying`, because the accept reads the kept preview in the same lock
            as the change of its state; the move of eight findings to TASK-0473 for the time of the standing order; E242 as a
            question for the owner and not a decision under the standing order.
Weakest:    The test of T197 shows the order that the rule of ADR-0188 "Bad" needs, and the injection shows the other order. The
            race in the product stays, as the owner decided (Q3 A).

## Progress

- 2026-11-26: V238 is done by P0.190 (TASK-0472).
- 2026-11-27: started under the standing order of 2026-11-26, branch `p0189-findings` from the close of P0.190 (`297d9cff`).
  The scope keeps T197, T201, T202 and E244; TASK-0473 gets the other findings, and R-0081 asks the owner about E242.
- 2026-11-27: run 45941449136 of `4b5e0121` passed on each job. Closed at v0.456.
