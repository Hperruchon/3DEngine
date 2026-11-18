---
id: 0412
title: The sheet page and the command line export a sheet as PDF, SVG or DXF through the save dialog of the system
status: Done
phase: R30d
opened: 2026-11-18
depends-on: [0408]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089, 0092, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0108, 0110, 0111, 0113, 0114, 0116, 0119, 0120, 0124, 0127, 0128, 0129, 0130, 0137, 0138, 0140, 0150, 0163, 0166, 0167, 0168, 0170]
writes:
  create:
    - tasks/TASK-0412-export-a-sheet-from-the-window.md
    - docs/adr/0170-export-a-sheet-from-the-window.md
    - 3DEngine.App/SheetExports.cs
    - 3DEngine.ImageTests/SheetExportTests.cs
  modify:
    - 3DEngine.App/ProjectFiles.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - docs/adr/0168-the-sheet-page.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Android/**
---

# TASK-0412 — The sheet page and the command line export a sheet as PDF, SVG or DXF through the save dialog of the system

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 9 records the answer D9 A of the owner of 2026-11-18, and row R30d holds the slice. On 2026-11-18 the owner
ordered "start R30h" and "and r30d" in the main session, which gave this task TASK-0412 and ADR-0170. R30h (TASK-0411) runs at
the same time and changes `Engine.Contracts/**`, GetSheet and `Engine.Core/Export/**`, so this task does not write
`Engine.Core/**`.

## Goal

A person exports the sheet of the page as PDF, SVG or DXF from the page or the command line, through the save dialog of the
system, on the desktop and on the phone.

## Scope (in)

1. "Export" and the row of the formats on the sheet page, and the three words of the command line (ADR-0170 item 1).
2. The save dialog with a purpose of each format, and its start beside the project (item 2).
3. The file of the engine written through SDL off the frame thread, and the texts (item 3).

## Scope (out)

- A share sheet of Android, and a test on a phone.
- A change of the files of a sheet (R30h, R30i).

## Acceptance criteria

- [x] The page and the command line give the three formats, PDF first, with 44 pixels high controls on a desktop and a phone.
- [x] Each format writes the bytes of the engine at the chosen path with its extension; a content URI keeps its name; each
      failure gives its code and its reason.
- [x] A run of the desktop exports the sheet of the plate as PDF and SVG through the dialog of Windows.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Done · branch r30d-sheet-export · v0.394

## Method

Mechanical: the dialog as "Export STL...", the writers of the engine as the routes call them, the row as the header of the page.
Judgement:  the dialog of SDL as the path of the phone too, with a write through SDL for a content URI; the queue of the page
            for the read and the write; `E-EXPORT-WRITE-FAILED` for a write that fails.
Weakest:    no test on a phone; a share sheet waits.

## Progress

- 2026-11-18: opened under the order of the owner of 2026-11-18. A run of the desktop with the plate of R30b opened the page,
  tapped "Export" and "PDF", typed a path in the save dialog of Windows, and the file `r30d-out.pdf` (2470 bytes) is the sheet
  of the plate; "SVG" gave `r30d-out2.svg` (2413 bytes). The panel said "Exported Sheet 1 as PDF to r30d-out.pdf, 2470 bytes."
- 2026-11-18: ten injections each failed a test, and each file came back byte for byte (SHA-256). A clean build gives 0
  warnings, the phone project builds, and each test project passes (2868 tests). Pushed as `r30d-sheet-export`.
- 2026-11-18: run 44327710142 passed on the branch with its seven jobs. Closed at v0.394; the merge waits for the owner.
