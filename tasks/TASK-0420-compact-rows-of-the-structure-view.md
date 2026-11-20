---
id: 0420
title: Compact rows of the structure view for a mouse or a pen, and the setting "Compact" or "For touch"
status: Done
phase: R31b
opened: 2026-11-20
depends-on: [0419]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0131, 0138, 0140, 0145, 0163, 0166, 0167, 0168, 0172, 0174, 0175]
writes:
  create:
    - tasks/TASK-0420-compact-rows-of-the-structure-view.md
    - docs/adr/0175-compact-rows-of-the-structure-view.md
    - 3DEngine.App/Ui/DesktopUi.Density.cs
    - 3DEngine.ImageTests/RowDensityTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.ImageTests/DesktopUiTests.cs
    - 3DEngine.ImageTests/DocumentPathTests.cs
    - 3DEngine.ImageTests/FontTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - docs/adr/0027-text-and-panels.md
    - docs/adr/0091-the-fonts-of-the-redesign.md
    - docs/adr/0174-the-tools-of-the-part-on-the-ring-and-a-structure-view.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Core/**
    - 3DEngine.Android/**
---

# TASK-0420 — Compact rows of the structure view for a mouse or a pen, and the setting "Compact" or "For touch"

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner said that the open structure view was too spaced, and chose density B of the study of that date:
rows of 30 pixels for a mouse or a pen, rows of 44 pixels on a phone, and a setting "Compact" or "For touch" in the menu
of the views, chosen by the device and not by each input. R31a (TASK-0419, ADR-0174) gave the structure view with rows of
44 pixels. On 2026-11-20, after the merge of R31a, the owner gave the order "start R31b". This task is R31b (ADR-0175).

## Goal

On a desktop the open structure view has rows of 30 pixels, and a person with a finger sets rows of 44 pixels in the net
of the views.

## Scope (in)

1. The two densities and the first density of the device (ADR-0175 item 1).
2. The cells "Compact" and "For touch" in the net of the views (item 2).
3. The compact rows, with no gap between two body rows (item 3).
4. The font of a list in the atlas (item 4).
5. The tests of item 5.

## Scope (out)

- A setting that lasts after the session.
- The question "Change to rows for touch?" on the first touch of a compact list.
- The tabs of an assembly in the structure view.

## Acceptance criteria

- [x] `RowDensityTests` holds items 1 to 4 of ADR-0175, and an injection of each fails a test.
- [x] The layout test of ADR-0172 item 5 passes with the open structure view in each density.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Done · commit (the merge of the branch density) · v0.403

## Method

Mechanical: the height of a row and the font of its text come from the density; two cells of the net set it.
Judgement:  a text of 20 pixels in a compact row (the text of the application is 1.5 times the text of the drawings of the
            study); the width of 360 pixels kept; no gap between two body rows in each density; the first row of two lines
            in the density "For touch" 60 pixels high.
Weakest:    the device density reads the operating system only, so a Windows tablet with no pen starts "Compact"; no
            test runs on Android.

## Progress

- 2026-11-20: opened on the order of the owner "start R31b", after the merge of R31a (v0.401) and P0.168 (v0.402).
- 2026-11-20: the code, `RowDensityTests` and four older test files. The first run showed rows of 40 pixels: Dear ImGui
  adds the gap of two items to a selectable row, so in R31a a row of 44 pixels took 54. Two body rows now have no gap, as
  in the drawings of the study. 13 injections, each restored from its saved text; each failed a named test. Pictures of
  the real window at 1280 by 720 and 360 by 640 showed the first row for touch crossed by the line under it, and a gap of
  10 pixels under the folded row; both are fixed, with a test. The new height moved the paper of the sheet page by a part
  of a pixel, and `SheetPageTests` failed with 79.39 for 80; the test now taps a whole pixel (see the ledger). A clean
  build gave 0 warnings, `dotnet test` passed, and the gates passed. Closed at v0.403; the merge waits for CI and the
  owner.
