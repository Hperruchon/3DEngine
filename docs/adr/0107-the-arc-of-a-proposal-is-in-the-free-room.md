---
id: 0107
title: With no ring of a body, the arc of a proposal is in the room right of the panel
status: Accepted
topic: Clients
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0092']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: 3DEngine.ImageTests/ProposalArcTests.cs
---

# ADR-0107 — With no ring of a body, the arc of a proposal is in the room right of the panel

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0092 item 7 puts the arc of a proposal on the band of the ring of the selected body. With no ring of a body, the arc
uses a ring at the centre of the display, with the inner radius of a ring of a body. Finding V159 of the codebase review of
2026-11-09 (b): in the default window of 1280 by 720, with a live sketch and no selection, the window of the live sketches
covered Accept, so a tap on Accept gave nothing, and the panel covered the word "Reject". The tests of R16r used one display
size, 1440 by 900.

## Decision

1. **The place (amends ADR-0092 item 7).** With no ring of a body, the ring of the arc has its centre at the middle of the
   room between the right edge of each panel at the left half of the display, with a margin of 16 pixels, and the right edge
   of the safe area less 16 pixels. Its height is that of a ring of a body at that place (ADR-0081). Its inner radius is that
   of a ring of a body, less where the room is narrow, so that each end and its word stay in the room; the inner radius is
   at least 100 pixels, as the ring of a sketch (ADR-0086 item 1).
2. **The window of the live sketches.** While a proposal waits, the window of the live sketches does not show, on a landscape
   display and on a portrait display. The rows of an active sketch show as before.
3. **No other change.** The arc on the ring of a selected body, the angles from 235° to 305°, the sheet of a portrait display
   and the taps stay as ADR-0092 says.

## Consequences

**Good:** at each display size from 900 by 600 to 1920 by 1080, with a live sketch and no selection, each end of the arc and
its word are out of each panel, and a tap on each end gives its action.

**Bad:** the list of the live sketches does not show while a proposal waits. On a display narrower than about 900 pixels the
room can be too narrow for a ring of 100 pixels, and an end can still be under the panel.

## History

- 2026-11-10 · TASK-0330 · The record, for finding V159 of the codebase review of 2026-11-09 (b).
