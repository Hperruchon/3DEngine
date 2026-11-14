---
id: 0136
title: The arc of a proposal ends before each tool of the ring, and the whole caption under it wraps
status: Amended
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0092', '0133']
amended-by: ['0139']
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
  - 3DEngine.ImageTests/ProposalCaptionTests.cs
enforced-by: 3DEngine.ImageTests/ProposalCaptionTests.cs
---

# ADR-0136 — The arc of a proposal ends before each tool of the ring, and the whole caption under it wraps

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (c) (`docs/reviews/2026-11-14-c-codebase-review.md`) gave two findings against rules of
ADR-0092 and ADR-0133:

- V184: Accept at 305 degrees, or its word, lay on the target of "Edit sketch" in 12 of 16 layouts, so that tool took no tap
  there while a proposal waited, although the tools of the ring "still take a tap" (ADR-0092 item 7).
- V181: the whole caption under the arc was one line cut to the chord of the ring, so a summary of more than about 45
  characters lost its end, although ADR-0133 item 1 says that no word of the summary is lost.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The end of the arc (amends ADR-0092 item 7).** The arc starts at 235 degrees. It ends at 305 degrees, or earlier, down
   to 255 degrees one degree at a time, at the first angle where Accept and its word lie on no target of a tool of the ring.
   With no such angle it ends at 305 degrees. A ring with no tools, as the free ring and the ring of a portrait display, ends
   at 305 degrees.
2. **The middle of the caption (amends ADR-0133 item 1).** The caption on the band has its middle at the middle of the arc,
   not at the top of the ring, and the room is the arc between the two circles.
3. **The whole caption wraps (amends ADR-0133 item 1).** The whole caption under the arc wraps at its spaces into lines that
   each fit the chord at their height. Only a word wider than the chord is cut.
4. **No contract change.** The arc and its lines are presentation (ADR-0007).

## Consequences

**Good:** each tool of the ring takes its tap at its target while a proposal waits, and each word of a summary shows.

**Bad:** on most landscape layouts the arc is shorter than 70 degrees, so less of the caption fits on the band. A long
summary takes several lines over the part.

## Alternatives considered

- **Move the first tool of the ring while a proposal waits.** Refused: the tools would move each time an agent proposes.
- **Turn the whole arc to the left.** Refused: Reject would go nearer the rows, which ADR-0134 moves the arc away from.

## History

- 2026-11-14 · TASK-0367 · The record, from the findings V181 and V184 of the codebase review of 2026-11-14 (c), under the
  order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0367 · Shipped at v0.353.
- 2026-11-14 · TASK-0373 · ADR-0139 items 2 and 3 amend item 1: Reject starts at 235 degrees or later, and a tool under
  an end hides while the proposal waits.
