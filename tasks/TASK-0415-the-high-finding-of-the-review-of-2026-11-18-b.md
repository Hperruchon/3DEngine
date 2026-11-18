---
id: 0415
title: The high finding of the codebase review of 2026-11-18 (b)
status: Done
phase: P0.166
opened: 2026-11-18
depends-on: [0414]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0168, 0170, 0171]
writes:
  create:
    - tasks/TASK-0415-the-high-finding-of-the-review-of-2026-11-18-b.md
  modify:
    - tasks/TASK-0416-the-findings-of-the-review-of-2026-11-18-b.md
    - 3DEngine.App/SheetPage.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - docs/adr/0168-the-sheet-page.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
---

# TASK-0415 — The high finding of the codebase review of 2026-11-18 (b)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-18 (`docs/reviews/2026-11-18-b-codebase-review.md`) gives the high finding V202: a view
that leaves the sheet during a drag of the sheet page stops the window with an exception on the frame thread. Section 7 of
`docs/templates.md` puts a high finding before the next product phase, R30e.

The owner answered the question of the review on 2026-11-18 with "A, and merge": Q1 A, a section through the axis of a cone
shows the axis in each look, because an edge of a cut face that lies in one plane with the axis of a cone gives the axis
(E223). TASK-0416 corrects the code and the text of ADR-0169 item 3. The review merged to `main` at `a5adcf6`. By section 7 of
`docs/templates.md` the first commit of the next task records the answer: this commit.

## Goal

A read without the dragged view ends the drag, and no frame of the sheet page throws for a view that the read does not hold.

## Scope (in)

1. V202: the drag ends when its view is not in the read, and its release gives nothing; `SheetPage.Drag` gives null for a
   view that the read does not hold.
2. The text of ADR-0168 item 3 that says so.
3. The answer Q1 A of the owner, here and in TASK-0416.

## Scope (out)

- V204 and each other finding of the review. TASK-0416 holds them.

## Acceptance criteria

- [x] A test gives a read without the dragged view between two moves of a drag, and it fails on the code of v0.396.
- [x] Each rule has an injection that a test catches.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of V202 in the review: its place, its probe and its recommended correction. Section 5 of the review says that
V204 has the same cause; a step that clears each state of the page whose view is not in the read can serve TASK-0416 later.

## Outcome

Status: Done · commit (the merge of the branch p0166-drag-view) · v0.398

## Method

Mechanical: the test of the drag, which failed on v0.396 with the exception of the review, and four injections.
Judgement:  a release after an ended drag gives nothing, so a person does not select or place by the end of a lost drag.
Weakest:    the test removes the view with a command of the engine; no run of the desktop repeated the crash with a person.

## Progress

- 2026-11-18: opened under the order of the owner "then start TASK-0415", branch p0166-drag-view from the close of TASK-0414
  (`8c0ecb8`). The owner answered Q1 A; this commit records it. The review merged to `main` at `a5adcf6`. Ledger entry
  v0.398.
- 2026-11-18: run 44390598558 passed on the branch on each job, and run 44389451949 passed on `main` after the merge of the
  review. Closed at v0.398; the merge waits for the owner.
