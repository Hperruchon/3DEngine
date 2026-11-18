---
id: 0408
title: A sheet page replaces the scene with the paper of a sheet, its views and its values, and MoveSheetViews keeps the alignment of a drag
status: Active
phase: R30c
opened: 2026-11-18
depends-on: [0407, 0409]
governed-by: [0004, 0007, 0012, 0016, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0060, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0112, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0140, 0145, 0153, 0155, 0158, 0162, 0163, 0166, 0167, 0168]
writes:
  create:
    - tasks/TASK-0408-the-sheet-page.md
    - docs/adr/0168-the-sheet-page.md
    - Engine.Core/Commands/MoveSheetViewsCommand.cs
    - Engine.Core/Commands/MoveSheetViewsCommandHandler.cs
    - Engine.Tests/Drawing/SheetMoveTests.cs
    - 3DEngine.App/SheetPage.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Sheet.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/0138-the-controls-of-the-flat-views.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0167-add-a-view-to-a-sheet-from-the-window.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/proposals/2026-11-18-2d-view-workflow.md
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/Export/**
    - Engine.Core/Drawing/**
---

# TASK-0408 — A sheet page replaces the scene with the paper of a sheet, its views and its values, and MoveSheetViews keeps the alignment of a drag

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 items 1, 5 and 10 record the answers D1 A, D5 A and B, and D13 A of the owner of 2026-11-18, and row R30c holds the
slice. The owner ordered "start R30b and R30c" on 2026-11-18, then "review after R30b". R30b merged at v0.391, and the codebase
review of 2026-11-18 (TASK-0409) merged at v0.392. Its section 5 asks R30c to send its commands off the frame thread (V196) and
to let the page and its pads hold their keys (V195, V199). TASK-0410 holds the other findings.

## Goal

A person sees the sheet that the files print in the window, on the desktop and on the phone, and moves, scales, removes and
places its views, and sets their lines, with taps and drags.

## Scope (in)

1. The button "Sheet", the page, the paper, the frame, the title block, the views and the values from GetSheet, read off the
   frame thread (ADR-0168 item 1).
2. The actions of a selected view (item 2), the drag and MoveSheetViews (item 3), the tap on the free paper (item 5).
3. The commands off the frame thread (item 4) and the keys (item 6).
4. The record of the answer of the owner on the free place (item 7).

## Scope (out)

- The export from the page (R30d), "Put on sheet" (R30e), the proposal of an agent on the page (R30f).
- The other findings of the review of 2026-11-18 (TASK-0410), and the commands of R30b on the frame thread.

## Acceptance criteria

- [ ] MoveSheetViews moves a view, and with `aligned` each view on a line of projection through it, in one step of undo,
      and refuses each wrong input with the codes of a view.
- [ ] The page shows the paper of the sheet in the free part, on the desktop and on a phone, and its controls are 44 pixels
      high and inside the display.
- [ ] A tap on a view gives "Go to", "Scale", "Remove" and the switches of its lines; a drag gives one MoveSheetViews with the
      alignment; a tap on the free paper places a view.
- [ ] The commands of the page run off the frame thread, and the page and the pad of a sheet hold their keys.
- [ ] A run of the desktop with the plate drags the views and shows the page at phone size.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active

## Method

Mechanical: the command as MoveSheetView with a flag, the read and the queue as the read of the lines, the controls as the
            header of a window.
Judgement:  a flag in place of a list of moves; the base view is the first view; the room of three rows over the paper; the
            page in place of the panel on a portrait display.
Weakest:    the alignment rests on exact places; the texts of the title block are small on a small paper.

## Progress

- 2026-11-18: opened from row R30c. A run of the desktop with the plate of R30b opened the page, selected the front view,
  dragged it (MoveSheetViews with `aligned`, so the top and the side view went with it) and dragged the top view, which slid on
  the vertical line of the front view only (its X stayed 224.22). The runs found that the button "Sheet" popped a colour that
  it had not pushed (an assert of ImGui), that the paper moved when the header grew, and that the panel covered the page on a
  portrait display; each is corrected. The test of the keys found that the Escape that closed the pad of a sheet still
  reached the intents (V199), because the router read the state after the key; the router now reads it before the keys.
- 2026-11-18: thirteen injections; three passed at first (one read at a time, the queue on the thread pool, the home screen
  that closes the page), so those tests now ask a second read while the first runs, block the work while Poll returns, and
  leave the home screen; then each injection failed a test, and each file came back byte for byte (SHA-256). A clean build
  gives 0 warnings, the phone project builds, and each test project passes (2858 tests). Pushed as `r30c-sheet-page`.
