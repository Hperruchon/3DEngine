---
id: 0093
title: The layout of the mix has a top bar, a panel with no chrome and a status line at the lower right
status: Amended
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0080']
amended-by: ['0099', '0100', '0113']
affects:
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.cs
enforced-by: 3DEngine.ImageTests/FixedControlsTests.cs
---

# ADR-0093 — The layout of the mix has a top bar, a panel with no chrome and a status line at the lower right

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner compared the desktop with the render "mix" of the designer brief and found it too far from the
render. The owner chose five slices, in this order: no panel chrome, framing, the orbit ring, the sketch plane and the
command line; each slice merges after green CI, with a picture at 1440 x 900 beside the render.

In the render, the title of the document is text at the upper left with no ground, the data of the part are rows with
no window, and the status line is small mono text at the lower right. Until this record, the panel was a window with a
title bar, a dark ground and a border; the title of the document was its title bar (ADR-0080 item 1); and the status line
held the address of the surface, right of Redo.

The panel still holds about twenty controls: the file, the list "New", "Sketch on", the toggles of the dimensions, the list
of the bodies and the actions of the selected body. The later slices move the actions to the ring and to the command line.
This slice keeps each control, so that no action is lost between two slices.

### Q1. Where the rows of the selection go in this slice

- **A.** They stay at the upper right (ADR-0084) until the panel holds no actions; then they take the left column, as in
  the render.
- **B.** They move to the left now, above the panel.

B puts the rows and about twenty controls in one column at 900 pixels high, and the panel then scrolls. **Recommended: A.**

## Decision

The owner chose the slice and its merge after green CI; the recommendation answers Q1.

1. **The top bar (amends ADR-0080 item 1).** At the upper left, 16 pixels inside the safe area, with no ground: the
   program, then the file or "unsaved", then the unit in the dim colour. A name wider than the rows is cut with "...", so
   the unit stays. A host with no files gives the program and the unit.
2. **The panel with no chrome.** The panel has no title bar, no ground and no border. It stays under the top bar, 8 pixels
   below it; the person does not move it, fold it or size it. Its height is the height of its content in the last frame, up
   to the room above the anchors, and it scrolls past that room. The room under it is the scene.
3. **The status line (amends ADR-0080).** At the lower right, on the row of the anchors, in JetBrains Mono: the count of
   the live bodies, "all correct" or the count that failed, and the address of the surface.
   It starts at least 12 pixels right of the anchors (Undo, Redo, Frame and Home) with the text that fits there: the whole
   line, else the line with no address, else none (finding V146 of the codebase review of 2026-11-08 (c), TASK-0310).
4. **The rows (Q1 A).** The rows of the selection stay at the upper right in this slice.
5. **The portrait ring** stays above the row of the anchors; it reads the top of that row, not the top of each fixed window,
   because the top bar is a fixed window too.
6. **No contract change.**

## Consequences

**Good:** the scene shows through the panel, the title reads as in the render, and the status line says the state of the
document.

**Bad:** the panel still holds each control, so the left column is far fuller than in the render, and its buttons keep the
ground of the theme. The person can no longer move the panel out of the way. The rows stay at the right until a later slice.

## History

- 2026-11-08 · TASK-0304 · The record, with the slice (the choice of the owner of 2026-11-08).
- 2026-11-08 · TASK-0304 · Shipped at v0.289.
- 2026-11-08 · TASK-0310 · Item 3: the status line leaves the anchors free; it drops the address, or does not show, where
  it does not fit (V146).
- 2026-11-08 · TASK-0310 · Shipped at v0.294.
- 2026-11-08 · TASK-0313 · ADR-0099 item 1 amends item 4: the rows take the left column while a body is selected, and
  the panel does not show; ADR-0099 items 2 to 5 give the controls of the panel a place. Item 6 amends item 1: a long name
  ends left of the command line.
- 2026-11-09 · TASK-0314 · ADR-0100 item 6 amends item 1: on a landscape display the file of the top bar is the first
  segment of the path line; on a portrait display the path line is under the top bar.
- 2026-11-10 · TASK-0336 · ADR-0113 item 2 amends item 2: a control of the panel that does not fit on its line starts a
  new line. Item 4: on a landscape display the panel ends 12 pixels left of the command line.
