---
id: 0473
title: The other findings of the codebase review of 2026-11-25
status: Done
phase: P0.191
opened: 2026-11-27
depends-on: [0468]
governed-by: [0004, 0006, 0007, 0008, 0013, 0015, 0016, 0017, 0019, 0020, 0021, 0023, 0025, 0028, 0029, 0031, 0033, 0035, 0037, 0038, 0039, 0045, 0049, 0054, 0055, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0109, 0110, 0111, 0112, 0114, 0116, 0120, 0124, 0127, 0129, 0130, 0137, 0138, 0140, 0153, 0155, 0158, 0163, 0166, 0167, 0168, 0170, 0171, 0174, 0176, 0177, 0179, 0181, 0184, 0185, 0187, 0188, 0190]
writes:
  create:
    - tasks/TASK-0473-the-other-findings-of-the-review-of-2026-11-25.md
    - docs/adr/0190-the-reversal-of-a-failed-group-is-marked-in-the-log.md
  modify:
    - Engine.Tests/Http/ProposalAcceptTests.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
    - Engine.Tests/Governance/PathPatternWorkTests.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
    - Engine.Tests/Assembly/LampSupportCaseTests.cs
    - eng/test-floor.txt
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/0188-the-accept-after-a-preview-and-the-group-in-the-log.md
    - 3DEngine.Vulkan/RenderPipeline.cs
    - 3DEngine.Vulkan/OverlayRenderer.cs
    - 3DEngine.ImageTests/PipelineStartTests.cs
    - docs/register.md
    - docs/diagnostics.md
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Tests/History/GroupHistoryTests.cs
    - Engine.Contracts/Command.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - 3DEngine.App/StepTexts.cs
    - docs/adr/README.md
    - docs/CURRENT-STATE.md
  forbid: []
---

# TASK-0473 — The other findings of the codebase review of 2026-11-25

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-25 (`docs/reviews/2026-11-25-codebase-review.md`, TASK-0467) found two medium and eleven low
findings. P0.190 (TASK-0472) did V238, and P0.189 (TASK-0468) did T197, T201, T202 and E244 under the standing order of
2026-11-26. This task holds each other finding.

## Goal

Each finding of this task has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E242 (after a failed accept, one `Redo` applies the part of the proposal that ran). The decided correction (Q2 A of the
   owner, 2026-11-26): the reversal of a failed group puts nothing on the redo list, and the log marks it, so that each walker
   of the log agrees. The mark waits for the answer of the owner to R-0081, because the smallest mark gives the field `GroupId`
   of `Engine.Contracts` a new meaning. An ADR comes in the same commit as the mark. Add the test of the probe of the review.
2. Low: E243, E245, V236, V237, T198, T199 and T200, each with the correction that the review recommends. On 2026-11-27 the
   owner moved E243 to register entry R-0082.
3. ADR-0188 items 5, 7 and 9 and "Bad" say what the code does after the corrections (E242, E245, V237).

## Scope (out)

- T197, T201, T202 and E244: TASK-0468 did them. V238: TASK-0472 did it.
- Until 2026-11-27 the forbid was `Engine.Contracts/**`, until the owner answered R-0081. The owner answered A ("All A").
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Notes for the implementer

- Read the section "3 Review of the code" of the review for the place, the probe and the correction of each finding.
- `governed-by` names each ADR whose `affects` holds a file of the write set. The 42 ADRs of `3DEngine.App/NativeThreeDEngine.cs`
  come from one comment of V237 in that file; this task changes no code of the window.
- E242 can start only after the answer of R-0081. E243, E245, V236, V237, T198, T199 and T200 need no answer.

## Outcome

Status: Done · commit (the merge of the branch p0191-findings) · v0.457

## Method

Mechanical: the corrections that the review names.
Judgement:  each choice between the two corrections that the review gives.
Weakest:    E243 has no correction; R-0082 holds it. For E242 the walkers `FeatureList`, `SketchRebuild` and `StepTexts` give the same result on a
            valid log with or without the change, because the bus refuses each `Redo` after a reversal; only the bus test fails. The read of the index in `Proposals` has no test of
            its own: a handler that throws only outside the fork needs a host with a second catalogue. `UiRenderer` still leaks at a failed start (R-0077).

## Progress

- 2026-11-27: opened by TASK-0468 with the findings that it did not do.
- 2026-11-27: under the standing order of 2026-11-26, branch `p0191-findings` from `ea2f4dac` (v0.456). T199: the gate of
  governed-by gives the texts that it can ask for, from the objects that it reads one time, and the test asks that each asked
  text is one of them; an injection that gives a distinct text for each path makes
  `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern` fail, and the comment says "the three newest tasks,
  of any status". T198: the test of a stale group releases the fork only when the accept waits for a preview, at the version 2,
  with no sleep; an `ApplyGroup` that ignores the version makes
  `A_Commit_Between_The_Comparison_And_The_Apply_Makes_The_Accept_Compare_Again` fail; the limits of 30 s of the new tests are
  60 s (ADR-0173 item 4). Not merged: the other findings are open.
- 2026-11-27: V237: the comment of the accept in `NativeThreeDEngine` and ADR-0188 item 9 say that the version is the version
  of the preview of the last frame, because the frame draws the shapes also when no panel shows; a version only when the panel
  shows would hold each accept on a portrait phone. The code does not change. T200: `Each_Pair_That_The_Bound_Refuses_Shares_No_Volume`
  intersects the two bodies of each of the 8 refused pairs and asks for no shared volume; a shaft of the knob of 7 mm gives
  31.76 mm³ for the bracket and the knob, and the test fails. Row K24 already names ADR-0188 item 8 (TASK-0469).
- 2026-11-27: V236: `RenderPipeline` and `OverlayRenderer` have the form of TASK-0461, and `PipelineStartTests` fails each of
  their starts at each object; `UiRenderer` makes a set layout, a pool and a sampler before its modules, as `ImageRenderer` does,
  so R-0077 now holds it.
- 2026-11-27: E245, first half: when the `Undo` of the group of an accept does not apply, the proposal is failed with the new
  code `E-API-PROPOSAL-NOT-REVERSED`, registered in `docs/diagnostics.md`; ADR-0188 item 7 and the comment of `Proposal` say
  so. `An_Undo_Of_The_Group_That_Does_Not_Apply_Gives_Its_Own_Code` breaks the file at the second command; without the read of
  the `Undo` the test gets `E-IO-WRITE-FAILED`. The second half, the index of a command whose handler throws, is open.
- 2026-11-27: E245, second half: `ApplyGroup` puts the index of a command whose handler throws on the exception
  (`DocumentSession.FailedIndexKey`), and the accept names that index; until TASK-0473 it named 0. The group test of a handler
  that throws asks for the index 1; without the line the test fails.
- 2026-11-27: the owner answered "All A" for R-0081 and the questions of P11.0. E242: ADR-0190 (amends ADR-0188 items 1, 3 and 4).
  The `Undo` that reverses a failed group has the group of the proposal; the bus takes it only from `CommandBus.Reverse` and
  from an entry of the log; `StepHistory.Reversed` and the three other walkers put nothing on the redo list.
  `A_Redo_After_A_Failed_Group_Applies_Nothing` (a live session, the walkers, a replay and an open) fails when the reversal goes
  to the redo list, and `An_Undo_With_A_Group_From_A_Client_Is_Still_Refused` fails when the bus takes a client `Undo` with a
  group. R-0081 closes.
- 2026-11-28: the owner answered "Move E243, merge" on 2026-11-27. E243 changes only a cost, so register entry R-0082 holds
  it with its probe and its correction as the exit. Ledger entry v0.457.
- 2026-11-28: run 46083547079 of `0dbb03d9` passed on each job. Closed at v0.457.
