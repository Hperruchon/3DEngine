---
id: 0415
title: The high finding of the codebase review of 2026-11-18 (b)
status: Ready
phase: P0.166
opened: 2026-11-18
depends-on: [0414]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0168, 0170, 0171]
writes:
  create:
    - tasks/TASK-0415-the-high-finding-of-the-review-of-2026-11-18-b.md
  modify:
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

## Goal

A read without the dragged view ends the drag, and no frame of the sheet page throws for a view that the read does not hold.

## Scope (in)

1. V202: the drag ends when its view is not in the read; `SheetPage.Drag`, the draw of the views and `OnLineOf` take no view
   that the read does not hold.
2. The text of ADR-0168 item 3 that says so.

## Scope (out)

- V204 and each other finding of the review. TASK-0416 holds them.

## Acceptance criteria

- [ ] A test gives a read without the dragged view between two moves of a drag, and it fails on the code of v0.396.
- [ ] Each rule has an injection that a test catches.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of V202 in the review: its place, its probe and its recommended correction. Section 5 of the review says that
V204 has the same cause; a step that clears each state of the page whose view is not in the read can serve TASK-0416 later.
