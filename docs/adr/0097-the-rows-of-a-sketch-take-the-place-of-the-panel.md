---
id: 0097
title: The rows of an active sketch take the place of the panel, and its ring is on the right
status: Amended
topic: Clients, sketch
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0034', '0086', '0087']
amended-by: ['0120']
affects:
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Sketch.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0097 — The rows of an active sketch take the place of the panel, and its ring is on the right

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The fourth slice that the owner chose on 2026-11-08 is the sketch plane. In the render "Mix 3", the sketch is under the
body that it makes, the rows of the sketch are at the left with no window ("Plate on XY  solved · 2 free", then a row for
each item), and the ring of the sketch tools is on the right with the caption "TOOLS FOR PLATE ON XY". ADR-0085 already
moves the sketch under its body, and ADR-0096 gives the camera the z axis up, so the sketch is now under the body on the
screen. The window of the sketch (ADR-0034 item 8) is a window with a title bar and a dark ground at the right, and the
ring of a sketch is on the left of the sketch (ADR-0086 item 1), because that window was at the right.

### Q1. Where the rows of an active sketch go

- **A.** In the place of the panel, at the left under the top bar, with no title bar, no ground and no border; the panel
  does not show while a sketch is active.
- **B.** At the right, in the place of the rows of a body, with the panel at the left.

A is the render, and the panel holds nothing that a person uses while a sketch is active. **Recommended: A.**

### Q2. The caption "SKETCH · UNDER THE BODY IT MAKES"

- **A.** Not in this slice.
- **B.** Under the sketch, in spaced capitals.

The first form of B crossed the band of the ring and the rows at 1280 x 720, where the sketch is small. A caption that
fits needs a rule of its own. **Recommended: A.**

## Decision

The owner chose the slice and the render as the target; the recommendations answer Q1 and Q2.

1. **The rows (Q1 A, amends ADR-0034 item 8).** While a sketch is active, its window is at the left under the top bar,
   in the place of the panel, as wide as the window of the sketch was, with no title bar, no ground, no border and a
   scrollbar with no ground. Its height is its content in the last frame, inside the room above the anchors (as the
   panel of ADR-0093). The panel does not show. The window of the live sketches, with no active sketch, does not change.
2. **The heading.** The name of the sketch and its state are on one line, the state in the dim colour.
3. **The pad.** A pad of the sketch opens to the right of the rows; when it has no room there, below the row of Next
   point, as before (finding V42).
4. **The ring (amends ADR-0086 item 1 and ADR-0087 item 5).** On a landscape display the segments of the ring of a
   sketch go down on the right of its centre, as the ring of a body. The centre moves left only as far as the band needs
   to stay 16 pixels inside the display, and it is never left of the right edge of the rows of the last frame. A tool
   with an icon (ADR-0095) keeps its icon and its label in the middle of its segment; a kind of a constraint wider than
   the band starts 6 pixels outside the inner edge and goes out to the right, inside the display. The ring has the caption
   "TOOLS FOR" and the title of the sketch when it fits.
5. **No caption under the sketch (Q2 A).**

## Consequences

**Good:** the sketch screen reads as the render: the rows at the left, the sketch under its body, and its tools on the
right. On a phone, the rows no longer cover the ring.

**Bad:** while a sketch is active, the controls of the panel (the file, New, Dimensions) do not show; Finish brings them
back. A long list of rows scrolls inside the room above the anchors.

## History

- 2026-11-08 · TASK-0308 · The record, with the slice.
- 2026-11-08 · TASK-0308 · Shipped at v0.295.
- 2026-11-11 · TASK-0347 · ADR-0120 items 1 and 2 amend items 1 and 2: the rows are the constraints with their value or
  residual, and the row "Makes".
