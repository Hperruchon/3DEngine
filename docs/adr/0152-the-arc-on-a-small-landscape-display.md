---
id: 0152
title: On a small landscape display the arc of a proposal is right of the rows on a smaller ring, and each part of a proposal hides the tools under it
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0134', '0139']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.ImageTests/ProposalPlaceTests.cs
enforced-by: 3DEngine.ImageTests/ProposalPlaceTests.cs
---

# ADR-0152 — On a small landscape display the arc of a proposal is right of the rows on a smaller ring, and each part of a proposal hides the tools under it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0134 item 1 moves the ring of the arc of a proposal right of the rows of the selected body; when the word Accept then
passes the right edge, as at 800 by 600 and 900 by 600, the arc stays on the ring of the body and lies on the rows (register
entry R-0066). ADR-0139 item 2 hides a tool under an end of the arc or its word while a proposal waits; the caption, the band
and the plate of the lines did not hide the tools under them.

The pictures of the pending proposal at 800 by 600 and 900 by 600, taken on 2026-11-14, showed Reject on the row "Made from"
and the plate of the lines on the values of the rows.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **A smaller ring (amends ADR-0134 item 1).** When the moved ring does not fit, a ring with the same height of its centre and
   a smaller inner radius does: from the word Reject 16 pixels right of the rows to the word Accept 16 pixels inside the right
   edge of the safe area. Its inner radius is at least 90 pixels; with less, the arc stays on the ring of the body.
2. **Each part of a proposal hides the tools under it (amends ADR-0139 item 2).** While a proposal waits, a tool under any
   rectangle of the proposal of the last frame does not show and takes no tap: the ends and their words, the box of the band
   from the end of Reject to the end of Accept, the caption, and the plate of the lines. It shows again after the accept or the
   reject.
3. **No contract change.** The arc is presentation.

## Consequences

**Good:** at 800 by 600 and 900 by 600 the arc and its lines are right of the rows, the rows show the value that changes, and no
tool shows under the proposal.

**Bad:** more tools hide while a proposal waits; the person reaches them after the accept or the reject.

## History

- 2026-11-14 · TASK-0388 · The record, from register entry R-0066 and the pictures at 800 by 600 and 900 by 600, under the order
  of the owner of 2026-11-14.
