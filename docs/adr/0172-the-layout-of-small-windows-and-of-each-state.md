---
id: 0172
title: The layout of small windows and of each state, down to 360 by 560
status: Accepted
topic: Clients, layout
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0097', '0099', '0104', '0113', '0163']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
  - 3DEngine.App/Ui/DesktopUi.Sketch.cs
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.App/Ui/DesktopUi.Home.cs
  - 3DEngine.App/Ui/DesktopUi.Image.cs
  - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
enforced-by: 3DEngine.ImageTests/ResponsiveLayoutTests.cs
---

# ADR-0172 — The layout of small windows and of each state, down to 360 by 560

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked for a review of the responsive layout at each size, not only at the thirteen sizes of
ADR-0113, and for a fix of the small windows that are almost square (ADR-0163 fact 1). The review ran the desktop, resized
it by hand and by a script, and probed a grid of sizes from 360 by 360. It found these facts:

1. **Near-square windows.** At 360 by 360 the command field covered the path line, the rows went past the right edge,
   and the rows covered the ring. In a landscape window less than about 600 pixels wide, the rows went on under Move.
2. **States that no test held.** The test of ADR-0113 had four states: no selection, a body, a face and a proposal. A
   probe with six more states (the home screen, an active sketch, an open pad, the history arc, the folded images and the
   open images) found 367 faults at 17 sizes, also at the phone sizes of ADR-0113: 208 for the pad, 106 for the open
   images, 38 for the sketch, 8 for the folded images, 6 for the history arc and 1 for the home screen. For example, the
   pad had keys of 108 pixels in five columns, so on a phone three columns were outside the display.
3. **The images.** The images were a window that the person moved, which started folded at x 492. On a narrow display
   its bar covered "Open project..." and "Save as...". After a resize down and back, it stayed on "Cut with". Open, it
   covered the ring at each width less than about 900.
4. **The corner near 360 by 360.** Below a height of about 480, the top bar, the rows, the anchors and a ring of five
   tools of 46 pixels do not fit together. A layout for that corner needs a new design (a "compact layout").

The owner answered four questions on 2026-11-18, each with the recommended option A: fix the six states in this task
(Q1); fix the faults now, put the edge of the window at 360 by 560, and propose a compact layout for a smaller window
later (Q2); make the images a fixed part of the layout (Q3); and open the images from the panel or the command line, in
the place of the rows (Q4).

With a window of at least 560 pixels high, each landscape display is more than 560 pixels wide. A rule for the path line
on a narrow landscape display (fact 1) then had no case at a size of the window or of a phone, so this record has none.

## Decision

1. **The rows of a body beside the ring (amends ADR-0099 item 1).** The rows are at most 360 pixels wide. On a landscape
   display they end 12 pixels left of a tool of the ring beside the first row, and they are at least 200 pixels wide. A
   row after the first one ends 8 pixels above the highest tool of the ring, or end of the arc of a proposal, in their
   column; a row with no room does not show, as a value row with no room. The targets of the tools are the targets before
   a proposal hides tools, so the column does not change when a tool hides. "Save as..." and "Import STL..." follow the
   rule of ADR-0113 item 2.
2. **The pad is a fixed part of the layout (amends ADR-0097 item 3).** The pad has no title bar, and the person does not
   move it. It takes the place of the panel and of the rows, which do not show while it is open, under the top bar and
   above the anchors. Its keys fill its width. On a landscape display it ends 12 pixels left of the command line when five
   keys of 52 pixels fit there, else it goes under the command line. A pad of a sketch opens to the right of the rows of
   the sketch when five keys fit there, else under the rows. When five lines of keys do not fit above the anchors on a
   landscape display, the pad is wide: it goes under the command line at the full width, its keys go on lines of ten (two
   lines of the pad on one line), its fields go on one line, its title goes on the line of the value, and the anchors and
   the status line do not show while it is open. The ring and the history arc do not show while the pad is open, as
   before.
3. **The sketch, the history arc and the home screen (amends ADR-0097 items 1 and 4 and ADR-0104 item 3).**
   - The rows of a sketch end 12 pixels left of the command line and of the band of the ring of the last frame on a
     landscape display, and 8 pixels above the ring on a portrait display when one row still fits.
   - The ring of a sketch stays inside the display when the rows leave no room at their right. On a landscape display its
     first tool starts under the command line.
   - A text of a node of the history arc that is on a fixed control goes to the right of its node. The caption "HISTORY"
     goes over the upper end of the arc when it has no room under the lower end, inside the display and off the fixed
     controls.
   - The home screen is at most as high as the display less its margins, and its list scrolls.
4. **The images are a fixed part of the layout (amends ADR-0163 item 5).** Folded, they are the button "Images..." in the
   file group of the panel and the command "Images". Open, they take the place of the panel, or of the rows of a selected
   body, under the top bar, with "Close"; the person does not move them. On a landscape display they end 12 pixels left of
   the command line, and on a portrait display 8 pixels above the ring. They do not show while a sketch is active or a pad
   is open, and they come back after.
5. **The minimum window and the test (amends ADR-0113 item 1 and ADR-0163 item 2).** The desktop window is at least 360
   by 560. The test of ADR-0113 item 1 has ten states: the four states of ADR-0113, the home screen, an active sketch, an
   open pad, the history arc, the button "Images..." in the panel, and the open images with a selected body. Its two
   theories apply the rule in each state at each size of ADR-0113, and at each step of the resize of ADR-0163 item 4 from
   360 by 560. A third theory holds each key of an open pad inside the pad on three landscape phones, because a pad that
   scrolls is not a fault of the rule.

## Consequences

**Good:** the six states pass at each size of the test, also on each phone size. The desktop window goes down to 360 by
560 with no fault, at 496 sizes of a resize in ten states. The pad and the images no longer cover the top bar, the
command line, the anchors or the ring, and the person never moves them.

**Bad:** while the pad is open, the panel and the rows do not show, so the person cannot read them during an entry. On a
landscape phone the keys of the pad are on lines of ten, which is not the order of a numeric keypad, and Undo and Redo do
not show until Apply or Cancel. The rows of a body can show fewer rows next to the ring in a small window. A window less
than 560 high is still not possible on the desktop. The images do not show while a sketch is active.

**Next:** a proposal for a compact layout below 560, with pictures (Q2 A). On 2026-11-18 the owner also chose to replace
the left panel with the tools of the part on the ring and a structure view, with rows of 30 pixels for a mouse or a pen;
that is a later task with its own record, and it moves the button "Images..." of item 4 to the ring of the part.

## History

- 2026-11-18 · TASK-0418 · The record, from the review of the responsive layout and the answers of the owner (Q1 to Q4).
