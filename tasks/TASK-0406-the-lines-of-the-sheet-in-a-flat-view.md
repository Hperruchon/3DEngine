---
id: 0406
title: A flat view of the window shows the lines of the sheet, with the switch "Lines" and the key L
status: Active
phase: R30a
opened: 2026-11-18
depends-on: [0400, 0403]
governed-by: [0004, 0007, 0009, 0013, 0016, 0017, 0018, 0019, 0022, 0023, 0025, 0026, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0037, 0038, 0041, 0060, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0094, 0096, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0112, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0139, 0140, 0141, 0145, 0150, 0153, 0155, 0158, 0163, 0166]
writes:
  create:
    - tasks/TASK-0406-the-lines-of-the-sheet-in-a-flat-view.md
    - docs/adr/0166-the-lines-of-the-sheet-in-a-flat-view.md
    - Engine.Core/Queries/GetViewLinesQuery.cs
    - Engine.Core/Queries/GetViewLinesQueryHandler.cs
    - Engine.Tests/Drawing/ViewLinesTests.cs
    - 3DEngine.App/ViewLines.cs
    - 3DEngine.ImageTests/FlatViewLinesTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.App/Ui/Theme.cs
    - 3DEngine.Core/Input/InputSamples.cs
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Tests/FrameFreePartTests.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/adr/0138-the-controls-of-the-flat-views.md
    - docs/adr/0140-frame-in-the-free-part-of-the-scene.md
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
---

# TASK-0406 — A flat view of the window shows the lines of the sheet, with the switch "Lines" and the key L

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-17 the owner compared the sheet of a plate with a hole and the flat views of the window: the window showed no
hidden lines, drew the outline of the sketch and kept the part small. ADR-0162 items 2 and 3 record the answers D2 A and
D3 A of 2026-11-18, and row R30a holds the slice. On 2026-11-18 the owner ordered "Start them" for R30g and R30a. R30g runs
in another session at the same time and changes `Engine.Contracts/**`, `Engine.Core/Export/**` and
`Engine.Core/Queries/GetSheet*`, so this task does not write them.

## Goal

In a flat view with the switch "Lines" on, the window shows the visible and the hidden lines of the sheet on a light paper,
and F makes the part fill the free part of the scene.

## Scope (in)

1. The query GetViewLines from `ViewEdges`, with the code `E-SHEET-FRAME-INVALID` (ADR-0166 item 1).
2. The read off the frame thread, the last lines kept, and the strokes in pixels (item 2).
3. The switch "Lines" in the net and the key L; the paper, the ink, the dashes; no outline of a sketch (item 3).
4. Frame in a flat view fills 0.9 of the free part (item 4), after a check of the key F.

## Scope (out)

- A cache of the lines in the engine (R-0069), "Add to sheet" and the sheet page (R30b, R30c), and the centre lines (R30g).
- A flat view of a frame at a slant.

## Acceptance criteria

- [x] GetViewLines gives the lines of the plate of 2026-11-17 in units of the design: from the front, four visible lines and
      two hidden walls at x = 12 and x = 28; the lines of GetSheet at 1 : 1 are the same lines moved by one shift.
- [x] The switch "Lines" is in the net and on the key L; a field, Control and the home screen keep L.
- [x] A picture of the front view of the plate with "Lines" on has the paper inside the plate, solid ink on the visible
      lines and dashes on the hidden walls; with "Lines" off it has the shaded plate.
- [x] In a flat view Frame fills 0.9 of the free part; the test fails on the code of v0.388.
- [x] A capture of the desktop with `plate.3de` in the Front, Top and Right views with "Lines" on, shown to the owner.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active

## Method

Mechanical: the query from `ViewEdges` as GetSheet uses it, the new key, the cell of the net, the code of the diagnostics.
Judgement:  the paper on the free part in place of a light clear of the whole scene, so the texts over the scene stay
            readable; a frame from the axes of the camera; one read at a time; the fill of 0.9 for a flat view.
Weakest:    no test runs the read of the host in a frame of the desktop; the run of the desktop with `plate.3de` covers it.

## Progress

- 2026-11-18: opened from row R30a under the order of the owner of 2026-11-18. The check of F: the key reaches the window,
  and Frame in a flat view took the sphere of the perspective at FrameFill 0.45, so the plate kept its size after the load.
- 2026-11-18: the query, the switch, the paper and the frame. Each new test of the frame failed on the code of v0.388; the
  other new tests name types that v0.388 does not have. Eleven injections each failed a test; a first injection in the map
  of SDL changed M into L, which the pair of keys of the test cancels, so a second one maps L to F. A run of the desktop
  with `plate.3de` and the keys L, 1, 7, 3 and F gave the three views with their lines. Pushed as `r30a-view-lines`.
- 2026-11-18: run 44285835681 passed on the branch, then run 44287000956 on the close. Closed at v0.389.
- 2026-11-18: the work first had TASK-0405 and ADR-0165 on the branch `r30a-view-lines`. R30g, in the main session, takes
  those numbers, so this task takes TASK-0406 and ADR-0166 on the branch `r30a-lines`, from `origin/main`, with the same
  code. The old branch is not merged. The first of R30a and R30g to merge closes at v0.389; the second closes at v0.390
  after it merges `main`. The merge waits for the owner.
- 2026-11-18: the commit of the renumbered work (b14cce2) set the status to Done before the close, so this commit sets
  it to Active for the close (docs/templates.md, section 3).
