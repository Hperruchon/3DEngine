---
id: 0362
title: The medium and low findings of the codebase review of 2026-11-14 (b)
status: Done
phase: P0.147
opened: 2026-11-14
depends-on: [0361]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0120, 0124, 0125, 0127, 0129, 0130, 0131]
writes:
  create:
    - tasks/TASK-0362-medium-and-low-findings-of-the-review-of-2026-11-14-b.md
  modify:
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.ImageTests/ProposalWindowTests.cs
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0362 — The medium and low findings of the codebase review of 2026-11-14 (b)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (b) (`docs/reviews/2026-11-14-b-codebase-review.md`) gives one medium finding (V178) and two
low findings (V179 and V180). Section 7 of `docs/templates.md` puts them after the next product phase, R18l.

## Goal

"Undo" of the line after an accept undoes the step of the proposal or does not show, no plate lies under the arc of a
proposal, and the mix test checks Reject and Accept at each size.

## Scope (in)

1. V178: the button "Undo" of the line shows only while the newest step is the last step of the proposal.
2. V179: the covers of the plates hold the targets, the caption and the plate of the lines of the arc of the last frame,
   and "AGENT IS WAITING".
3. V180: the checks of Reject and Accept at each size of the mix test.
4. A test for V178 and V179, which fails before the fix.

## Scope (out)

- An undo of the whole proposal in one tap.

## Acceptance criteria

- [x] A test proves that a step after an accept takes the button "Undo" away.
- [x] A test proves that no plate is under Accept or the lines of the arc of a proposal.
- [x] The mix test checks Reject and Accept at each size.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0147-review-findings) · v0.348

## Method

Mechanical: the count of the log at the accept; the covers; the two checks.
Judgement:  when the button goes: at the first change of the count of the log, for good.
Weakest:    a command of another client in the same frame as the end of the accept takes the button away; no person used
            the change.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (b) (TASK-0361).
- 2026-11-14: started after R18l, branch p0147-review-findings. V178: the host gives the count of the log at the end of the
  accept, and the line shows "Undo" only while the count of the status is that count; once the count changes, the button
  goes for good. V179: the arc keeps the boxes of the circles of Reject and Accept, their words, the caption and the plate
  of the lines, and the plates of the next frame take them as covers. The test of V179 found a third form: a plate under
  "AGENT IS WAITING", which is no window either; the word is a cover too. A first form of the test read the covers from
  the list that the fix fills, so it did not see a missing target; it reads the drawn items now. The ring stays inside the
  display in height, so the test moves it across under each plate to bring the lines onto a plate. V180: the mix test
  checks Reject and Accept at each size.
- 2026-11-14: six injections each failed a named test: no change of the button, no covers of the arc, no plate of the
  lines, no targets, no word "AGENT IS WAITING", and no Accept in the sheet at 390 by 844. Each file came back byte for
  byte.
- 2026-11-14: run 43550335599 passed on the work commit. Closed at v0.348.
