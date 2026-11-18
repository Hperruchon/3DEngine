---
id: 0407
title: A person adds the flat view or the standard views to a sheet from the window, and ScaleSheetView changes the scale
status: Active
phase: R30b
opened: 2026-11-18
depends-on: [0406]
governed-by: [0004, 0007, 0012, 0016, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0060, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0112, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0128, 0129, 0130, 0131, 0137, 0138, 0140, 0145, 0150, 0153, 0155, 0158, 0163, 0166, 0167]
writes:
  create:
    - tasks/TASK-0407-add-a-view-to-a-sheet-from-the-window.md
    - docs/adr/0167-add-a-view-to-a-sheet-from-the-window.md
    - Engine.Core/Commands/ScaleSheetViewCommand.cs
    - Engine.Core/Commands/ScaleSheetViewCommandHandler.cs
    - Engine.Tests/Drawing/SheetScaleTests.cs
    - 3DEngine.App/SheetPlacement.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/SessionSheets.cs
    - 3DEngine.App/Ui/DesktopUi.Sheet.cs
    - 3DEngine.ImageTests/SheetAddTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/0138-the-controls-of-the-flat-views.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/Export/**
    - Engine.Core/Queries/GetSheet*
    - Engine.Core/Drawing/**
---

# TASK-0407 — A person adds the flat view or the standard views to a sheet from the window, and ScaleSheetView changes the scale

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 items 4 to 6 record the answers D4 A, D5 A and B, and D6 A of the owner of 2026-11-18, and row R30b holds the slice.
R30a merged at v0.389. On 2026-11-18 the owner ordered "Start R30b". R30g (TASK-0405) waits for its merge at v0.390 in
another session and changes `Engine.Contracts/**`, `Engine.Core/Export/**`, `Engine.Core/Drawing/**` and the GetSheet
records, so this task does not write them.

## Goal

From the window, a person adds the flat view or the standard views to a sheet at the places of the projection and at a scale
that fits, makes the first sheet with a pad of the paper and the scale, and changes the scale of the views in one step.

## Scope (in)

1. The command ScaleSheetView (ADR-0167 item 1).
2. "Add to sheet" and "Standard views" in the net and in the command line, and the live sheets in the window (item 2).
3. The place from the projection and the scale that fits (items 3 and 4).
4. The pad of a new sheet and of the scale of the views (item 5).

## Scope (out)

- The tap on the paper that places a view (D5 B) and the sheet page (R30c).
- A choice among several sheets: the views go to the first live sheet.

## Acceptance criteria

- [ ] ScaleSheetView changes the scale of one view or of each view in one step of undo, and refuses each wrong input with its
      code; the places and the dimensions stay.
- [ ] The net and the command line have the controls of a sheet; the pad gives the paper and the scale, with Enter and
      Escape.
- [ ] The plate of 2026-11-17 gets the front, the top and the side view at 2:1 on A3 at the places of first angle, and a view
      that the sheet holds is not added again.
- [ ] A run of the desktop with the plate makes the sheet with the controls, and its PDF shows the views of first angle.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active

## Method

Mechanical: the command as MoveSheetView, its handler in the catalog, the controls as the cells of the net, the pad as the
            numeric pad.
Judgement:  the size of a view from the box of the mesh; the side from the shared axis of two frames; the group of the
            standard views at the centre of the area; a new sheet as several commands; no refusal for a sheet with no view.
Weakest:    the first live sheet only; the free place is a search on a grid of 5 mm, not the largest free part.

## Progress

- 2026-11-18: opened from row R30b under the order of the owner "Start R30b". A run of the desktop with the plate made the
  sheet by clicks on "Views", "Add to sheet", "Add" and "Standard views": the log has CreateSheet and three AddSheetView at
  (255, 203.5), (255, 143.5) and (145, 203.5) at 2:1, and the PDF shows the views of first angle. The run found that a long
  message went past the panel, so the message now wraps.
- 2026-11-18: thirteen injections each failed a test, and each file came back byte for byte (SHA-256). A clean build gives
  0 warnings, the phone project builds, and each test project passes (2836 tests). Pushed as `r30b-add-to-sheet`.
- 2026-11-18: run 44297261851 passed on the branch with its seven jobs. R30g merged first at v0.390, so this branch merges
  `main`: the conflicts were lists only (the catalog of the handlers, the codes of the sheet, the index of the ADRs and the
  amended-by of ADR-0155, and the count of `Engine.Tests`, now 1689). The views of a sheet now carry the flags of R30g with
  their defaults, and each test project passes on the merged tree (2841 tests).
