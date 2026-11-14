---
id: 0131
title: On a portrait display the rows of a selected body are under the part, and the panel has no controls of a selected body
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0084', '0099']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.ImageTests/BodyRowsTests.cs
  - 3DEngine.ImageTests/CutUiTests.cs
  - 3DEngine.ImageTests/DesktopFormsTests.cs
  - 3DEngine.ImageTests/DesktopUiTests.cs
  - 3DEngine.ImageTests/DimensionOverlayTests.cs
  - 3DEngine.ImageTests/DocumentPathTests.cs
  - 3DEngine.ImageTests/EdgeValueTests.cs
  - 3DEngine.ImageTests/FaceSelectionTests.cs
  - 3DEngine.ImageTests/NumericPadUiTests.cs
  - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
  - 3DEngine.ImageTests/RingToolsTests.cs
enforced-by: 3DEngine.ImageTests/BodyRowsTests.cs
---

# ADR-0131 — On a portrait display the rows of a selected body are under the part, and the panel has no controls of a selected body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison of the window with the render at the other sizes of the brief (R18j,
`docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H3) found that a portrait display shows the panel of the
document over the part while a body is selected, with the chips drawn over it. The render at 768 by 1024 gives the rows of
the selected body under the part. ADR-0099 item 7 kept the panel on a portrait display.

A trial of this slice showed that with the rows on a portrait display, the part of the panel for a selected body shows in
no orientation: a landscape display has the rows since ADR-0099, and an active sketch has its own rows (ADR-0097). Each
control of that part has another place: the tool ring (Move, Distance, Hide and Show, Rename, Dimensions, Sketch on face),
the rows (the name, the cut, the face, the edge, the size and the fault) and the status line (Show all, ADR-0124), except
"Hidden too" of the dimensions, which only the command line of a landscape display and the panel with no selection held.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question. The study took
its Q4 A: the controls of a selected body leave the panel.

## Decision

1. **The rows under the part (amends ADR-0099 items 1 and 7).** On a portrait display, while a body is selected and no
   sketch is active, the rows of ADR-0099 show in place of the panel, at the full width of the display inside its safe
   area. They take the room from the middle of the display down to 8 pixels above the top of the corner ring of the last
   frame, or above the anchors with no ring, and their last row is on the bottom of that room. While a proposal shows,
   the room ends 8 pixels above the sheet of the last frame (ADR-0111 item 4). A row that has no room does not show, as on
   a landscape display; its chip stays. The row of a cut and the row "Hidden too" (item 3) come right after the first row
   and always show, because the ring and the command line of a portrait display hold no cut. The first rows always show: when the room is lower than it, it moves up, and not
   above the top of the panel, so it covers no tool of the ring and no part of the sheet. A sheet that has no room above the
   ring covers the ring, as ADR-0111 item 4 permits.
2. **The chips (amends ADR-0084 item 4).** A chip that the placement puts on rows under the part goes above them, because
   the ring and the anchors are under them; on a landscape display it goes under them, as before.
3. **Hidden too.** While the dimensions are on, the rows have a row "Hidden too" with its state, "on" or "off"; a tap on
   it gives the dimensions of the hidden bodies too, or not, as the toggle of the panel does.
4. **The panel.** The panel has no controls of a selected body, because it shows only with no selected body and no active
   sketch. Its list of the bodies, its document controls, New, Sketch on and the toggles of the dimensions stay.
5. **No contract change.** Each row gives the action of the control that it replaces.

## Consequences

**Good:** a portrait display shows the part, its rows and its values as in the render, and no chip lies on a panel. One
place holds the controls of a selected body in each orientation.

**Bad:** on a portrait phone the room under the part is small, so the rows show the name and few values; the chips keep
the others. The tests that reached the panel of a selected body through a portrait display move to the rows and the ring.
No person used the change on a tablet or on the S23.

## Alternatives considered

- **Keep the panel on a portrait display (Q4 B of the study).** Refused under the order: the panel covers the part, and
  the render puts the rows under it.
- **The rows at the top of a portrait display, under the path line.** Refused: the part is at the middle, and the render
  puts the rows under it.

## History

- 2026-11-14 · TASK-0360 · The record, from gap H3 and Q4 A of the comparison at the other sizes, under the order of the
  owner of 2026-11-14.
- 2026-11-14 · TASK-0360 · Shipped at v0.345.
- 2026-11-14 · TASK-0360 · Item 1: the row of a cut and the row "Hidden too" come right after the first row on a portrait
  display and always show; the review of the branch found no room for the cut on a phone with a ring and a value.
