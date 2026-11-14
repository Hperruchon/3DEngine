---
id: 0132
title: A label of the corner ring is drawn smaller until it lies on no other label and fits inside the display
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0081']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.ImageTests/CornerRingLabelsTests.cs
enforced-by: 3DEngine.ImageTests/CornerRingLabelsTests.cs
---

# ADR-0132 — A label of the corner ring is drawn smaller until it lies on no other label and fits inside the display

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison at the other sizes of the brief (R18j, `docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H4)
found that on the corner ring of a portrait display the labels of the two last segments, "Focus" and "Dimensions", lie on
each other. The corner ring of ADR-0081 has its segments from the left of the quarter up to its top. Near the top the
segments are side by side, so their middles are about 61 pixels apart across at 390 by 844, and "Dimensions" is 117
pixels wide in the font of the panels. The clamp of a label to the right edge of the display moved it further onto its
neighbour. The render gives each label in its segment, in a small text.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The size of a label (amends ADR-0081).** On the corner ring of a portrait display, a label is drawn smaller, down to
   half of the size of the font, until it is no wider than the horizontal distance to the middle of each neighbour that is at
   its height (less than its height and 2 pixels apart), less 4 pixels, and until its half fits between its middle and the
   right edge of the display, 4 pixels inside the safe area. Each of two neighbours then takes at most half of the distance
   between them, so no label lies on another, and the clamp to the edge moves none. The target of a tap follows the drawn
   label and its icon, at least 44 pixels on each side (ADR-0116 item 1).
2. **A landscape ring keeps the size of the font**, because its segments are wider than its labels.
3. **No contract change.** The labels are presentation (ADR-0007).

## Consequences

**Good:** each label of the corner ring is in its segment at each portrait size of the list of R19, as the render.

**Bad:** a long label, such as "Dimensions", is in a text of half the size on a phone; it is still in its segment and under
its icon. No person read it on the S23.

## Alternatives considered

- **Short texts, such as "Dims".** Refused: the brief uses the full words, and a short form is a second name for a tool.
- **A larger ring.** Refused: a ring whose segments hold "Dimensions" at the size of the font would cover half of a phone.
- **End the quarter before its top.** Refused: the labels near the top lie on each other at each angle of the end, because
  the segments there are side by side.

## History

- 2026-11-14 · TASK-0363 · The record, from gap H4 of the comparison at the other sizes, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0363 · Shipped at v0.347.
