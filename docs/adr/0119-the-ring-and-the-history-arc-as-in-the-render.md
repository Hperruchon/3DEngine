---
id: 0119
title: The ring gives Edit sketch and Rename in the span of four segments, and the history arc is a bright line with its nodes on it
status: Amended
topic: Clients, rendering
date: 2026-11-11
supersedes: []
superseded-by: []
amends: ['0081', '0098', '0104']
amended-by: ['0122', '0123']
affects:
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.ImageTests/RingToolsTests.cs
enforced-by: 3DEngine.ImageTests/RingToolsTests.cs
---

# ADR-0119 — The ring gives Edit sketch and Rename in the span of four segments, and the history arc is a bright line with its nodes on it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gaps G5 and G6. In the render "Mix 1" the ring of the
bracket has five outlined segments: Edit sketch, Move, Distance, Hide and Rename. The history arc is a bright line with a
glow, each node is on the line with its label beside it, and the node of the selected body is filled. The window gave the
ring no Edit sketch and no Rename, and drew the arc faint with a round button of 44 pixels and an icon for each node.

Slice S5 of the proposal closes G5 and G6. The owner accepted each recommended answer of the proposal on 2026-11-10 and
ordered R18e after R18d. Two facts limit the ring:

- A waiting proposal draws its arc on the band of the ring from 235 to 305 degrees, with Accept at 305 degrees
  (ADR-0092 item 7). The top segment of a landscape ring must end before that angle.
- The history arc is on the band from 90 to 135 degrees (ADR-0104). The bottom segment must end before it.

Four segments of 0.42 radians and three gaps span 1.80 radians, from about 308 to 52 degrees, which fits. Five segments
reach about 295 degrees, under the arc of a proposal. Before this record a selected face gave an extrude five segments, and
its top segment was under the arc of a proposal.

The session gave the choices of this record. The owner can change them.

## Decision

1. **The tools (amends ADR-0081 item 3 and ADR-0098 item 3).** The ring of a body that a sketch made starts with "Edit
   sketch", with a pencil; its tap opens that sketch, the sketch along the first input of each step from the body
   (ADR-0123), as the row "Made from" of ADR-0117 item 2. "Rename", with a letter T, follows "Dimensions" (ADR-0122) on a
   landscape display; its tap opens the name field of the rows (ADR-0099 item 2). A portrait display has no "Rename" on the thumb ring,
   because its panel keeps the name field and its button. The command line holds "Edit sketch" for a body that a sketch
   made.
2. **The span (amends ADR-0081 items 1 and 2).** The segments of a landscape body ring span at most the span of four
   segments, 1.80 radians. More than four segments are narrower, and a narrowed segment is at least 46 pixels at the inner
   radius. When the ring has no room for each tool in that span, "Rename", then "Edit sketch", then "Dimensions" leave the
   ring (ADR-0122); the rows still give the first two, and the panel and the command line give the third. A segment is
   never wider than 0.42 radians. A ring of a sketch keeps its own layout (ADR-0086).
3. **The history arc (amends ADR-0104 item 3).** The arc is the accent, 2 pixels wide, over two wider strokes of the accent
   with less alpha, its glow. The node of the selected body is a filled dot of the accent with a radius of 10 pixels and a
   glow; each other node is a ring of the accent with a radius of 6 pixels and the ground of the scene; a node of a fault is
   red. A node has no icon. The target of a tap stays a circle of 44 pixels, and a hover fills it lightly.
4. **No contract change, no engine change.** Each new tool gives an action that exists.

## Consequences

**Good:** the ring and the history look as in the render, and the top segment of a landscape ring never goes under the arc
of a proposal.

**Bad:** a ring of more than four tools has narrower segments, so their labels need more room. On a small display "Rename" and
"Edit sketch" are in the rows and in the command line only. The selected face of an extrude gives six tools, and on a small
display the two new tools leave the ring.

## History

- 2026-11-11 · TASK-0346 · The record, from slice S5 of the proposal and the judgement of the session.
- 2026-11-11 · TASK-0346 · Shipped at v0.329.
- 2026-11-11 · TASK-0348 · Amended by ADR-0122: "Dimensions" is a segment between "Focus" and "Rename", and it leaves a
  small ring after "Edit sketch".
- 2026-11-12 · TASK-0348 · Finding V172 of the codebase review of 2026-11-12: the span of four segments is 1.80 radians, from
  about 308 to 52 degrees, as `RingSpan` gives; the record said 1.84 radians and 307 to 53 degrees.
- 2026-11-12 · TASK-0350 · Amended by ADR-0123: "Edit sketch" opens the sketch along the first input (finding V168).
