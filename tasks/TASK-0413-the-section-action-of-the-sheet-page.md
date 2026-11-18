---
id: 0413
title: On the sheet page a person cuts a view with "Section", a look and two taps, and the page draws the section
status: Active
phase: R30h
opened: 2026-11-18
depends-on: [0411, 0412]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0037, 0060, 0072, 0073, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0110, 0111, 0114, 0116, 0120, 0124, 0127, 0129, 0130, 0137, 0138, 0140, 0155, 0158, 0159, 0163, 0164, 0165, 0166, 0167, 0168, 0169, 0170, 0171]
writes:
  create:
    - tasks/TASK-0413-the-section-action-of-the-sheet-page.md
    - docs/adr/0171-the-section-action-of-the-sheet-page.md
    - 3DEngine.ImageTests/SheetSectionPageTests.cs
  modify:
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - 3DEngine.App/SheetPage.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0168-the-sheet-page.md
    - docs/adr/0169-the-section-view-of-a-sheet.md
    - docs/adr/README.md
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0413 — On the sheet page a person cuts a view with "Section", a look and two taps, and the page draws the section

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R30h of the roadmap, the second part: ADR-0169 item 8 puts the action "Section" of the sheet page after R30d. On
2026-11-18 the owner ordered "then start the Section action on the sheet page", after "merge R30h when green" and "merge R30d
after R30h". R30h (TASK-0411) merged at v0.394 and R30d (TASK-0412) at v0.395.

## Goal

A person makes a section of a view on the sheet page with "Section", a look, a tap on the view and a tap on the paper, and the
page draws the hatch, the cut line, the arrows and the letters as the files do.

## Scope (in)

1. The centre of each view in GetSheet (ADR-0171 item 1).
2. "Section", the looks, the cut line that follows the pointer and snaps, and the two taps (ADR-0171 items 2 to 4).
3. The drawing of the lines and the texts of a section on the page (ADR-0171 item 5).

## Scope (out)

- A drag of a cut line that exists, a section of a cut with steps, and the place of the projection for a section.

## Acceptance criteria

- [ ] Each rule has a test, each new test fails on the code before the change, and each rule has an injection that a test
      catches.
- [ ] A run of the desktop with the plate makes a section on the page, and a picture shows it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the row of the page as the row of a selected view, the action as the other actions of a sheet.
Judgement:  two taps and no drag for the cut; the point of the design from the centre of the view in the read.
Weakest:    the snap uses whole circles only.

## Progress

- 2026-11-18: opened under the order of the owner, branch r30h-section-page from `main` at v0.395.
- 2026-11-18: a run of the desktop with the plate with a hole made the section A-A on the page (picture of the window), and the
  cut snapped to the centre of the hole. Fifteen injections each failed a named test after the test took the look "Down"
  and the property PageDragging. The ledger entry corrects two placeholders of v0.395 and adds the shipped line of R30h.
