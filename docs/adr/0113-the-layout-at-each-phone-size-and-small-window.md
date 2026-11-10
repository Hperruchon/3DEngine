---
id: 0113
title: The layout keeps each control in the display and free for a tap at each phone size and in a small window
status: Amended
topic: Clients, layout
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0081', '0092', '0093', '0098']
amended-by: ['0108']
affects:
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
enforced-by: 3DEngine.ImageTests/ResponsiveLayoutTests.cs
---

# ADR-0113 — The layout keeps each control in the display and free for a tap at each phone size and in a small window

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked for work on the responsive layout: the phone sizes and the small windows. A layout that
works on the desktop is not a fault. The window has one layout for a landscape display and one for a portrait display
(ADR-0093, ADR-0099, ADR-0100). Each record gave the place of its controls at one or two sizes, and no test read all the
controls at all the sizes.

A probe of TASK-0336 at v0.316 drew the window at eleven sizes in four states and found these faults:

1. On a portrait display of 390 pixels or less, rows of the panel went past its right edge: "Sketch on XY XZ YZ Face",
   the field of the filter with "Clear", "Hidden too", the name with "Rename", and the face with "Sketch on face".
2. On a portrait display the path line of ADR-0100 covered "AGENT IS WAITING" of ADR-0092 item 7.
3. At 640 by 360 the command line of ADR-0098 (300 pixels) overlapped the top bar and the panel.
4. At 640 by 360 the ring of a body (an inner radius of at least 160, ADR-0081) was higher than the display: the status
   line covered Hide, and at 844 by 390 the label "Sketch on face" went below the display.
5. At 640 by 360 the rows of the body covered the caption and the first line of a proposal in the ring.

## Decision

1. **The sizes and the rule.** The window keeps each control in the display, no control goes past the right edge of its
   window, no window covers a control of another window or a tool of the ring, and no two fixed windows overlap, at each
   of these sizes in pixels of the window: 360 by 640, 360 by 780, 384 by 824, 390 by 844, 393 by 873, 412 by 915
   (phones in portrait), 640 by 360, 844 by 390, 915 by 412 (phones in landscape), 768 by 1024, 1024 by 768 (a tablet),
   1280 by 720 and 1440 by 900 (windows). A control that its panel scrolls out of view is not a fault. The sheet of a
   proposal may cover the panel (ADR-0092 item 7), but not a tool of the ring (ADR-0111 item 4). The test reads each
   control of the last frame through `DesktopUi.ItemRectangles`.
2. **A row that does not fit (fault 1, amends ADR-0093 item 2).** A control of the panel stays on its line when it fits
   there; else it starts a new line. A field that a button follows (the filter and "Clear", the name and "Rename") is
   220 pixels wide when the line has room; else it is the room less its label and the button, and at least 120 pixels.
3. **"AGENT IS WAITING" on a portrait display (fault 2, amends ADR-0092 item 7).** When the word does not fit in the top
   bar, it goes under the path line of ADR-0100, and the panel starts under the word.
4. **The command line on a narrow display (fault 3, amends ADR-0098 item 2).** The field is 300 pixels wide, or 30
   percent of the width of the display when that is less, and at least 200 pixels. On a landscape display the panel ends
   12 pixels left of the field.
5. **The ring on a short display (fault 4, amends ADR-0081).** On a landscape display the inner radius of the ring of a
   body is at most the half of the height between the top of the display and 8 pixels above the anchors, less the band
   and 16 pixels, and at least 100 pixels, as the ring of a sketch (finding V134). A ring that fits keeps its radius and
   its place, so the layout of the desktop does not change.
6. **The text of a proposal in the ring (fault 5).** The caption goes inside the ring when a fixed window covers its
   place above the arc, and the lines inside the ring start under a fixed window that covers the first line.
7. **The units.** The sizes are in the units of the window that the host gives the frame (`SDL_GetWindowSize`). This
   record does not know if the S23 Ultra gives pixels or points that do not depend on the density. Register entry
   R-0059 holds the question.

## Consequences

**Good:** one test reads each control at thirteen sizes in four states, so a new control that does not fit on a phone
fails the build. R-0058 closes: no tool of the ring is under a panel from 360 by 640.

**Bad:** on a narrow portrait display the panel is longer, because rows wrap, and the rows of a selected body are further
down in the scroll. The panel has no ground and covers the middle of the scene on a portrait display, so the body shows
under its rows; this record does not change that. The test does not cover the home screen, an active sketch, the pad or
the history arc.

## History

- 2026-11-10 · TASK-0336 · The record, from the probe at v0.316 and the order of the owner of 2026-11-10.
- 2026-11-10 · TASK-0336 · Shipped at v0.317.
- 2026-11-10 · TASK-0332 · ADR-0108 item 4 amends item 4: the field of the command line is at most 400 pixels wide, not
  300.
