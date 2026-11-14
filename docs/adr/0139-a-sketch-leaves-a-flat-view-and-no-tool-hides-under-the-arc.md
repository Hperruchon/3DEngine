---
id: 0139
title: A sketch leaves a flat view, and a tool under an end of the arc of a proposal hides while the proposal waits
status: Amended
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0136', '0137']
amended-by: ['0152']
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.Core/Sketching/SketchCamera.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.Tests/FlatViewTests.cs
  - 3DEngine.ImageTests/ProposalPlaceTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: 3DEngine.ImageTests/ProposalPlaceTests.cs
---

# ADR-0139 — A sketch leaves a flat view, and a tool under an end of the arc of a proposal hides while the proposal waits

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) gave two high findings:

- V185: a sketch that starts in a flat view of ADR-0137 kept the flat view, because the view of a sketch sets only the yaw
  and the pitch. From the top view a sketch on the front plane showed the plane edge-on and took no point.
- V186: ADR-0136 item 1 ended the arc before the tools of the ring from Accept only. Reject was not tested, and on a small
  landscape display, as a phone turned on its side, no angle freed Accept, so an end lay on Move or Set distance. The arc
  takes the tap there first (finding V160), so that tool took no tap, and its picture said that it did.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **A sketch leaves a flat view (amends ADR-0137 item 2).** The view of a sketch leaves a flat view first, as an orbit does,
   so a sketch has the view with perspective that ADR-0053 gives it.
2. **Both ends of the arc (amends ADR-0136 item 1).** Accept ends at 305 degrees or earlier, as before. Reject starts at 235
   degrees or later, up one degree at a time, at the first angle where Reject and its word lie on no target of a tool, while
   the arc keeps at least 40 degrees.
3. **A tool under an end hides.** While a proposal waits, a tool whose target lies on an end of the arc of the last frame, or
   on its word, is not drawn and takes no tap. It shows again after the accept or the reject.

## Consequences

**Good:** a sketch draws from each view, and no tool shows where it takes no tap.

**Bad:** on a small landscape display a tool can be away while a proposal waits; the person rejects or accepts first, or
types the command. No person used it.

## Alternatives considered

- **A ring of the arc inside the ring of the tools.** Refused: on a small display the ring of the tools is at its least
  radius, and an arc inside it has no room for Reject, Accept and their words.
- **The arc at the top, as on a portrait display.** Refused: the ring of the tools of a landscape display reaches the top
  too, and the rows of the body are at the upper left.

## History

- 2026-11-14 · TASK-0373 · The record, from the findings V185 and V186 of the codebase review of 2026-11-14 (d), under the
  order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0373 · Shipped at v0.357.
- 2026-11-14 · TASK-0388 · ADR-0152 item 2 amends item 2: each rectangle of a proposal hides the tools under it.
