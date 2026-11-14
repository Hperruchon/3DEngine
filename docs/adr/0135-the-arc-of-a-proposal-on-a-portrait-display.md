---
id: 0135
title: On a portrait display the arc of a proposal is at the top, and a sheet of its lines is under it
status: Amended
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0092', '0111', '0131']
amended-by: ['0141']
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.ImageTests/ProposalPortraitTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
  - 3DEngine.ImageTests/MixImageTests.cs
enforced-by: 3DEngine.ImageTests/ProposalPortraitTests.cs
---

# ADR-0135 — On a portrait display the arc of a proposal is at the top, and a sheet of its lines is under it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison at the other sizes of the brief (R18j, `docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H5)
found that on a portrait display the proposal was a sheet with the command lines, Reject and Accept, over the part and its
chips (ADR-0092 item 7). The render gives the arc with Reject and Accept above the part, as on a landscape display. ADR-0133
item 4 split this gap to row R18o.

On a phone the room between the middle of the display, where the rows of the selected body start (ADR-0131 item 1), and the
top of the corner ring is about 130 pixels, and the first rows take about 90 of them. So a sheet above the ring has no room
while the rows show.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The arc at the top (amends ADR-0092 item 7).** On a portrait display the proposal is an arc, as on a landscape display,
   with its caption on the band (ADR-0133 item 1), Reject and Accept. Its ring has its centre at the middle of the safe area
   across, and the top of its band is 8 pixels under the top bar, the path line and "AGENT IS WAITING". Its inner radius fits
   the width, as the free ring of ADR-0107, so that each end and its word stay in the display, from 100 to 160 pixels.
2. **The sheet of the lines.** The command lines are in a sheet with no buttons. The sheet gives the whole caption, wrapped,
   first, only when the arc does not hold it.
3. **The place of the sheet (amends ADR-0111 item 4 and ADR-0131 item 1).** The sheet is 8 pixels under the arc and ends at
   most 8 pixels above the middle of the display; lines that do not fit scroll in it. So the sheet covers no tool of the
   corner ring, and it no longer ends above the ring or above the anchors. The rows under the part no longer end above the
   sheet; while a proposal waits they do not go above the bottom of the sheet of the last frame.
4. **The chips keep off the proposal.** While a proposal waits on a portrait display, the placement of the chips takes the
   arc, the sheet and the rows of the last frame as covers, so the chip of a proposed value shows free. A landscape display
   keeps ADR-0133 item 2, where the lines move off the chips.
5. **No contract change.** The place of a proposal is presentation (ADR-0007).

## Consequences

**Good:** on a phone and on a tablet the part and the chip of the proposed value show while a proposal waits, and Reject
and Accept are on the arc above the part, as in the render. The corner ring keeps each tool free for a tap.

**Bad:** Reject and Accept are at the top of a phone, far from the thumb at the corner ring; the render puts them there, and
no person used them on the S23. The small arc of a phone cuts most captions, so the caption shows two times: cut on the arc
and whole in the sheet. On a small display the lines scroll.

## Alternatives considered

- **The sheet above the corner ring, without its buttons.** Refused: on a phone it has no room while the rows show.
- **The lines inside the ring of the arc, as on a landscape display.** Refused: the ring of a phone is small, and the chord
  of its inner circle cuts a line of about 25 characters.
- **The arc at the bottom, near the thumb.** Refused: the render puts the arc above the part, and the corner ring is at the
  bottom.

## History

- 2026-11-14 · TASK-0368 · The record, from gap H5 of the comparison at the other sizes, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0368 · Shipped at v0.352.
- 2026-11-14 · TASK-0374 · ADR-0141 item 3 amends item 3: while a proposal waits on a portrait display, the rows do not go
  above the middle of the display, also in the first frame.
