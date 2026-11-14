---
id: 0134
title: The arc of a proposal moves right of the rows of the selected body, and its lines keep off the tools of the ring
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0092', '0133']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/ProposalPlaceTests.cs
enforced-by: 3DEngine.ImageTests/ProposalPlaceTests.cs
---

# ADR-0134 — The arc of a proposal moves right of the rows of the selected body, and its lines keep off the tools of the ring

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison at the other sizes of the brief (R18j, `docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H2)
found that at 1024 by 768 the arc of a proposal and Reject lay on the rows "Made from" and "Distance" of the selected body.
Reject then covered "Plate on XY", a target of the rows. The render puts the arc right of the rows. ADR-0107 already keeps
the free ring of the arc right of each panel; the ring of a selected body had no such rule.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The ring of the arc right of the rows (amends ADR-0092 item 7).** When the arc on the ring of the selected body, with
   its two circles and their words, lies on the rows of the body, the ring of the arc moves right, at its height and with
   its radius, until the word Reject ends 16 pixels right of the rows. When the word Accept then passes the right edge of
   the safe area less 16 pixels, the arc stays on the ring of the body.
2. **The lines off the tools (amends ADR-0133 item 2).** The target of each tool of the ring is an obstacle of the block of
   the lines, as a chip is. At each height the block first has its middle at the centre of the ring of the arc; when it
   lies on an obstacle, it moves left until it ends 8 pixels left of each obstacle that it meets, while it stays inside the
   inner circle and right of the rows.
3. **No contract change.** The place of the arc is presentation (ADR-0007).

## Consequences

**Good:** from 1024 pixels wide no item of the arc lies on the rows, and neither the caption nor a line lies on a tool of
the ring, with the body at three places.

**Bad:** the ring of the arc is no longer the ring of the body when it moves, so the band of the arc is not on the band of
the tools. Under 1024 pixels wide, as at 900 by 600, the arc still lies on the rows, and its lines can lie on the tools
(R-0066). The circle of Accept can lie on the target of "Edit sketch", as before this record; the arc takes the tap first
(finding V160 of the codebase review of 2026-11-09 (b)).

## Alternatives considered

- **A smaller ring right of the rows.** Refused: at 800 by 600 and 900 by 600 a smaller ring put the caption and the lines on
  the tools of the ring, which is worse than the rows.
- **The free ring of ADR-0107 for a selected body.** Refused: its centre is at the middle of the room right of the rows, far
  from the body; the move of item 1 is the smallest that frees the rows.

## History

- 2026-11-14 · TASK-0365 · The record, from gap H2 of the comparison at the other sizes, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0365 · Shipped at v0.350.
