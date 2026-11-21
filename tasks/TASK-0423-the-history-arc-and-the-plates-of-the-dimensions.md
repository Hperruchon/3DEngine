---
id: 0423
title: No plate of a dimension lies on the stroke of the history arc, and each text of the arc has the ground of the scene
status: Active
phase: R20d
opened: 2026-11-20
depends-on: [0425]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0077, 0078, 0104, 0116, 0119, 0120, 0123, 0125, 0129, 0130, 0172]
writes:
  create:
    - tasks/TASK-0423-the-history-arc-and-the-plates-of-the-dimensions.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
    - docs/adr/0125-each-dimension-says-what-it-measures.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0423 — No plate of a dimension lies on the stroke of the history arc, and each text of the arc has the ground of the scene

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-20 the scenario of R20c ran in the window (TASK-0422): at 1280 by 720 with "Cote gauche" selected and Dimensions
on, the history arc went across the plate "Overall · Y 400 mm", and a dimension line went across the text "Box" of the arc.
ADR-0125 item 4 says that no plate goes under the history arc. The placement knew the nodes and the texts of the arc, but not
its stroke. The frame draws the dimensions under the arc, and a text of the arc had no ground, so a line showed through it.
Register entry R-0065 is due on 2026-11-26. Section 4 of the codebase review of 2026-11-20 (TASK-0425) found two more texts of
the arc with no ground: the caption "HISTORY" and the amber text of a pending step, which was also no cover.

## Goal

The scenario of R20c passes: no element covers a plate, and no line goes across a text of the arc.

## Scope (in)

1. The stroke of the history arc as covers of the placement: the boxes of twelve chords, wider by half the widest stroke.
2. The ground of the scene behind each text of the arc, its caption and the text of a pending step; that text as a cover.
3. Two tests in `DimensionLabelsTests`, and ADR-0125 item 4.

## Scope (out)

- The long extension lines of a far plate (R-0065): each label can be read, and the person tells the values apart.
- A new run of the scenario in the window: the tests measure the stroke and read the drawn vertices.

## Acceptance criteria

- [x] No plate lies on the stroke of the arc in 96 views, and in some views a plate starts on it.
- [x] Each text of the arc, its caption and a pending text have a rectangle of the ground of the scene behind them, after
      each dimension line under them, and the pending text is a cover.
- [x] Each of six injections makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch r20d-history-arc-b) · v0.409

## Method

- Read `DesktopUi.History.cs` and `DimensionCovers` in `DesktopUi.Overlay.cs`: the covers held the items "History" and
  "HistoryText", which are the nodes and the texts, and no rectangle of the stroke.
- The test of the stroke measures each plate from 361 points of the arc (`DesktopUi.HistoryArc`), not from the covers of the
  program, so a cover that is too small fails it. The test of the ground reads the vertices that `Frame` returns, because a
  call of `ImGui.GetBackgroundDrawList` outside a frame stops the test host.
- Six injections with `inject0423.js` in the scratchpad: no cover of the stroke, no ground of a text of a step, covers with no
  width, no ground of the caption, no ground of the pending text, and a pending text that is no cover. Each made a named test
  fail, and each file came back byte for byte (sha256).

## Progress

- 2026-11-20: opened after P0.170, branch r20d-history-arc from its first work commit (then TASK-0422).
- 2026-11-20: the covers, the ground, two tests and ADR-0125 item 4, as v0.407 on the first branch (`b3b9b6e`).
- 2026-11-20: a parallel session merged R30f, which made the codebase review due before R20d. The review (TASK-0425,
  v0.408) named the caption and the pending text (finding V220 of its first form). This task added their ground and the
  cover, made the test of the ground a theory with and without a pending step, and moved to the branch r20d-history-arc-b
  from the review. Ledger entry v0.409; the first branch is not merged.
- 2026-11-20: run 44826283924 of `0b8b0ab` passed on each job. Closed at v0.409.
- 2026-11-21: run 44831092608 of the close commit `9cadf07` failed on macos-latest in the step "Keep the determinism hash":
  "Failed to CreateArtifact ... Request timeout" of the artifact service of GitHub; each test passed, and the other jobs
  passed. It is no lost runner, so ADR-0173 gives no re-run. The task is Active again for a new run, and closes after it.
