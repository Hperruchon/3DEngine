---
id: 0410
title: The medium and low findings of the codebase review of 2026-11-18
status: Ready
phase: P0.165
opened: 2026-11-18
depends-on: [0409]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0060, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0109, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0121, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0140, 0142, 0143, 0144, 0145, 0147, 0155, 0158, 0163, 0164, 0165, 0166, 0167]
writes:
  create:
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetViewLinesQueryHandler.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/SheetPlacement.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Sheet.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - Engine.Tests/Drawing/ViewLinesTests.cs
    - 3DEngine.ImageTests/SheetAddTests.cs
    - docs/adr/0165-the-hidden-and-centre-lines-of-a-view.md
    - docs/adr/0166-the-lines-of-the-sheet-in-a-flat-view.md
    - docs/adr/0167-add-a-view-to-a-sheet-from-the-window.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0410 — The medium and low findings of the codebase review of 2026-11-18

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-18 (`docs/reviews/2026-11-18-codebase-review.md`) gives the medium findings E218, V194, V195,
V196, V197 and V198, and the low findings E219, E220, V199, V200 and V201. Section 7 of `docs/templates.md` puts them after the
next product phase, R30c (TASK-0408). E220 and V200 are a decision that disagrees with the code; the answers Q1 and Q2 of the
owner say which side changes.

## Goal

Each of the eleven findings has a correction or a recorded answer, and a test that fails on the code before the correction.

## Scope (in)

1. E218: a cross on each whole circle of the design whose arcs cover the turn.
2. E219: the comment of the label of a dimension.
3. E220: after Q1, the centre lines in GetViewLines, or the text of ADR-0166 item 1.
4. V194: a new sheet checks each place of its group, and reports the views that went on and each one that does not fit.
5. V195 and V199: the keys of the pad of a sheet rest while the home screen shows, and the pad holds its keys.
6. V196: the commands of a sheet go off the frame thread, and a read of the lines starts again only when the bodies change.
7. V197: the box of a new view is the box of the bodies that the sheet draws.
8. V198: no gizmo, or a gizmo above the paper, while the paper shows.
9. V200: after Q2, the code or the text of ADR-0167 item 3.
10. V201: the reason of each skipped body in a flat view with "Lines" on.

## Scope (out)

- R30c and each other row of the roadmap.

## Acceptance criteria

- [ ] Each finding has a test that failed on the code of v0.391 or of the version before this task, and each rule has an
      injection that a test catches.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of each finding in the review: its place, its probe and its recommended correction. R30c may change the same
files of the window first; read them on `main` at the start, and keep the commands of R30c off the frame thread too.
