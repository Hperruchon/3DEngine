---
id: 0165
title: A flat view of the window shows the lines of the sheet on a light paper, with the switch "Lines" and the key L, and Frame fills the free part
status: Accepted
topic: Drawing, window
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0138', '0140']
amended-by: []
affects:
  - Engine.Core/Queries/GetViewLinesQuery.cs
  - Engine.Core/Queries/GetViewLinesQueryHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - 3DEngine.App/ViewLines.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/Theme.cs
  - 3DEngine.Core/Input/InputSamples.cs
  - 3DEngine.Vulkan/SdlInput.cs
  - 3DEngine.Core/Models/Camera.cs
  - Engine.Tests/Drawing/ViewLinesTests.cs
  - 3DEngine.ImageTests/FlatViewLinesTests.cs
  - 3DEngine.Tests/FrameFreePartTests.cs
enforced-by: 3DEngine.ImageTests/FlatViewLinesTests.cs
---

# ADR-0165 — A flat view of the window shows the lines of the sheet on a light paper, with the switch "Lines" and the key L, and Frame fills the free part

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 items 2 and 3 (the answers D2 A and D3 A of the owner of 2026-11-18) decide that a flat view of the window shows
the lines of the sheet: a new query of the lines of one frame from `ViewEdges`, the code of GetSheet, read off the frame
thread, and a switch "Lines" (key L) with a light background, visible lines of 2 pixels, hidden lines of 1 pixel with dashes,
no fill, no outline of a sketch and no grid. Row R30a also asks that Frame fill the free part of the scene, after a check.

**The check of Frame (gap G11 of the design file).** The run of 2026-11-17 saw no change of size after F in a flat view. The
key reaches the window: `WatchFrameKey` gives `FrameAction`, and the host calls `Camera.Frame` in the free part. The fault is
the size: in a flat view `Frame` took the sphere around the box of the perspective, at the `FrameFill` of 0.45 of the
session, so a plate of 60 x 40 x 10 filled about a third of the free part in the picture of 2026-11-17. The load of the
document had framed the plate in the same way, so F gave the same picture. The new test of item 4 fails on the code of
v0.388: in its part, the plate fills 0.25 of the tighter side from the front and from the top, and 0.17 from the right.

## Decision

1. **The query GetViewLines.** The query takes the nine numbers of an exact frame, as AddSheetView (ADR-0155 item 1), and
   gives each line of `ViewEdges` in units of the design on the plane of the view: a point p goes to (p · Right / L,
   p · Up / L), where L is the length of Right (ADR-0164 item 1). A line keeps its body, its visibility, its kind ("edge" or
   "outline"), and its circle or ellipse, as GetSheet gives them. The lines are those of GetSheet for a view of the same
   frame at 1 : 1, moved by one shift. A direction that is not a valid exact frame gives the new code
   `E-SHEET-FRAME-INVALID`. The result records live in `Engine.Core`, as those of GetSheet, so `Engine.Contracts` does not
   change. As GetSheet, the query has no cache (ADR-0155 item 7, R-0069).
2. **The read in the window.** With the switch on and a flat view, the window gives GetViewLines the frame of the flat view:
   nine whole numbers from the axes of the camera, equal to the frame of the standard view of the engine (ADR-0142 item 2).
   The read runs on the thread pool, one read at a time, for a new frame or a new version of the Document. Until it ends,
   the window keeps the last lines, and shows "Lines…" on the paper. Lines of another frame do not show. The window
   projects each point of a line with the camera, at the depth of the target, so a pan and a zoom move the lines with the
   view. An arc of a circle or an ellipse is 96 straight parts for a whole curve, counter-clockwise from its start.
3. **The switch "Lines" and its look (amends ADR-0138 items 1 and 3).** The net of the views has an eighth cell, "Lines", at
   its lower right, under Back. A tap changes the switch and closes the net, as a tap on a view does; the cell has the colour
   of a tool in use while the switch is on. The key L, with no Control, changes the switch, under the guard of the keys of
   the views. The switch stays across the views; the lines show in a flat view only. With lines, the window draws a light
   paper (#f2f0eb) with a corner of 6 pixels on the free part of the scene (ADR-0140 item 3) of the last frame, before each
   other mark of the scene. The paper covers the shaded bodies, their edges and the images, so the view has no fill. On the
   paper each visible line is ink (#1a1f26) of 2 pixels, and each hidden line is ink of 1 pixel in dashes of 6 pixels with
   gaps of 4, along the whole line. The lines stop at the edge of the paper. The outline of a sketch does not show. The
   scene has no grid, so nothing hides one. With the switch off, a flat view is shaded, as before.
4. **Frame in a flat view (amends ADR-0140 item 2).** In a flat view, Frame puts the box, as the view shows it, inside 0.9
   of the free part, on its tighter side: the width of the box along the right of the view, or its height along the up.
   The target and the distance of the eye stay as ADR-0140 gives them; only the scale of the flat view changes. With
   perspective, Frame does not change.
5. **No contract change.** The query and its records are in `Engine.Core`. The switch, the paper and the frame are
   presentation (ADR-0007). The query writes nothing to the log, so the determinism rules do not apply to the window; the
   query uses + - * / only.

## Consequences

**Good:** the front view of the plate of 2026-11-17 shows the two dashed walls of the hole, as the PDF does (criterion C1 of
the design file), from the same code as the sheet and the three files. F now gives a part that fills the free part.

**Bad:** each new frame or version reads the lines again with no cache: a body with many curved faces holds the session for
some seconds (R-0069), and the paper shows the old lines or "Lines…" in that time. The paper is the free part of the scene,
so a part that a pan moves out of it is cut. In a window with the window of the live sketches open, the free part is
narrow, and so is the paper. Only the six flat views have lines; a view at a slant waits for a flat view of any exact frame.

## Alternatives considered

- **The hidden edges of the GPU for each body (D2 B).** The owner chose the lines of the sheet.
- **A light clear of the whole scene.** The texts that lie over the scene (the step of Undo, the status line) are light, so
  they would not read on a light clear. The paper keeps them on the dark ground.
- **A frame of each view at its first show.** Not asked; a view keeps the scale of the last view, and F frames.

## History

- 2026-11-18 · TASK-0405 · The query, the switch, the paper and the frame of a flat view (phase R30a).
- 2026-11-18 · TASK-0405 · Shipped at v0.389.
