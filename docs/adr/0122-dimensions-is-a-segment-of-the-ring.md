---
id: 0122
title: Dimensions is a segment of the ring of a body
status: Accepted
topic: Clients, rendering
date: 2026-11-11
supersedes: []
superseded-by: []
amends: ['0119']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.ImageTests/RingDimensionsTests.cs
  - 3DEngine.ImageTests/RingToolsTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
  - 3DEngine.ImageTests/RingImageTests.cs
enforced-by: 3DEngine.ImageTests/RingDimensionsTests.cs
---

# ADR-0122 — Dimensions is a segment of the ring of a body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`, problem 2) found that the
dimensions are hard to find. The toggle "Dimensions" is in the panel (ADR-0056 item 2) and in the command line (ADR-0099
item 4). The ring of a body has none, so a person must open the panel to show the dimensions. The owner took improvement B
on 2026-11-10 (roadmap row R20b). On 2026-11-11 the owner asked again: "i want the dimention option on the wheel".

Row R20b also holds the hidden bodies: the selection after a Hide, "N hidden · Show" in the status line, and "Show hidden"
on a ring of the document. This record holds the dimensions only, because the owner asked for them first. A later task holds
the hidden bodies.

ADR-0119 item 2 lets "Rename" and then "Edit sketch" leave a landscape ring that has no room in the span of four segments.

## Decision

1. **The segment (amends ADR-0119 items 1 and 2).** The ring of a body has "Dimensions" after "Focus" and before "Rename".
   Its icon is a dimension line with an arrow head at each end, between two extension lines. A tap turns the dimensions on
   or off. It changes the same state as the toggle of the panel and the command, and it sends no action to the engine. On a
   landscape ring with no room in the span, "Rename" leaves first, then "Edit sketch", then "Dimensions". The panel and the
   command line keep the toggle.
2. **The state.** While the dimensions are on, the segment has the colour and the outline of a tool in use, as "Focus" has
   while a focus lasts (ADR-0101 item 3).
3. **No contract change, no engine change.** The toggle is a state of the client.

## Consequences

**Good:** a person turns the dimensions on and off on the ring, with no visit to the panel. The ring shows if they are on.

**Bad:** a ring of seven tools has narrower segments. On a landscape phone (844 by 390 and 915 by 412) and at 640 by 360
the ring of an extrude has no room, so "Dimensions" leaves it; the ring of a box keeps it, and a portrait ring keeps it. The
order puts "Focus" before "Dimensions", so a small ring keeps "Focus", which has no toggle in the panel.

## Alternatives considered

- **"Focus" leaves before "Dimensions".** A landscape phone then keeps "Dimensions" for an extrude. Refused for now: "Focus"
  has no route in the panel, and "Dimensions" has two. The owner can change the order.
- **A toggle in the status line.** Refused: the owner asked for the ring, and the status line has no room on a phone.

## History

- 2026-11-11 · TASK-0348 · The record, from improvement B of the UX test and the request of the owner of 2026-11-11.
- 2026-11-12 · TASK-0348 · Shipped at v0.334.
