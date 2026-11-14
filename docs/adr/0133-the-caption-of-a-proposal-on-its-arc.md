---
id: 0133
title: The caption of a proposal is on the band of its arc, and the command lines under the arc lie on no chip
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0092']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/ProposalCaptionTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: 3DEngine.ImageTests/ProposalCaptionTests.cs
---

# ADR-0133 — The caption of a proposal is on the band of its arc, and the command lines under the arc lie on no chip

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison at the other sizes of the brief (R18j, `docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H1)
found that at 1280 by 720 the room above the arc of a proposal is under the top bar. So the caption "AGENT · summary" was
the first line inside the ring, and the box of the caption and the command lines lay on the chip of the width: the person
could not read "60.000 → 75.000 mm", the value that the proposal changes. The render gives the caption on the arc, above the
part, and no box over the chip.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The caption on the band (amends ADR-0092 item 7).** The caption is on the band of the arc, along its top. Its letters
   are on the middle circle of the band, at the size of the command lines, in amber, with the middle of the text at the top
   of the ring; each letter turns to the tangent of the circle. The caption is cut with "..." to the arc between the two
   circles, less 8 pixels at each end. When it is cut, the whole caption is the first line under the arc, so that no word of
   the summary is lost. When the place of the caption is above the top bar or under a fixed window, the caption is the first
   line under the arc, as before (ADR-0113 item 6).
2. **The command lines off each chip.** The block of the lines starts at its place of ADR-0092 item 7. When its plate lies on
   a chip of the frame, the block goes down one line at a time until it lies on no chip, while its plate ends above 0.6 of
   the inner radius under the centre of the ring. With no such place it stays at its start.
3. **No contract change.** The caption and the lines are presentation (ADR-0007). The sines and cosines of the letters write
   nothing to the log.
4. **The portrait display.** Gap H5, the arc in place of the sheet on a portrait display, is a separate slice, R18o, after
   R18n. Its change of the sheet and of its tests is larger than this slice.

## Consequences

**Good:** at 1280 by 720 and 1440 by 900 the chip of the width shows "60.000 → 75.000 mm" free of the lines, and the caption
is on the arc, as the render.

**Bad:** a summary of more than about 35 characters is cut on the arc, and its whole text is a line under the arc too, so
it shows two times. The lines still have a plate over the part. No person read the caption on the arc.

## Alternatives considered

- **A straight caption on the band.** Refused: the band is curved, so a straight text longer than about 150 pixels leaves
  the band at its ends.
- **The lines outside the ring.** Refused: the brief puts them under the arc, and the place outside the ring holds the rows
  and the tools.
- **The caption in capitals, as the render.** Refused for now: the summary is the text of the agent, and capitals make it
  wider, so more of it is cut.

## History

- 2026-11-14 · TASK-0364 · The record, from gap H1 of the comparison at the other sizes, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0364 · Shipped at v0.349.
