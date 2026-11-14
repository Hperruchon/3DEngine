---
id: 0125
title: Each dimension says what it measures, on a plate outside its box, and no control covers it
status: Amended
topic: Clients, rendering
date: 2026-11-12
supersedes: []
superseded-by: []
amends: ['0056']
amended-by: ['0129']
affects:
  - 3DEngine.App/DimensionOverlay.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.ImageTests/DimensionLabelsTests.cs
  - 3DEngine.ImageTests/DimensionOverlayTests.cs
  - 3DEngine.ImageTests/DimensionImageTests.cs
enforced-by: 3DEngine.ImageTests/DimensionLabelsTests.cs
---

# ADR-0125 — Each dimension says what it measures, on a plate outside its box, and no control covers it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`) found two problems in the
dimension overlay of ADR-0056:

- **Problem 3.** A value does not say what it measures. The dimensions of the whole were orange, near the colour of the
  selection. The dimensions of the selected panel were white. No label named a piece or said "overall". A cube of 400
  showed "Thickness 400 mm".
- **Problem 4.** The labels cover each other and other controls. The labels of the selected body had no plate and lay on
  the edges. "Y 400 mm" of the whole was under the status line. The ring and its caption covered "Z 2350 mm", and the
  history arc lay on "X 3400 mm".

The owner took improvement C on 2026-11-10 (roadmap row R20c), after R18c, which gave the values on the edges their form
(ADR-0115). The owner started R20c with "Go" on 2026-11-12. The session gave the choices of items 2, 4 and 5 below. The
owner can change them.

## Decision

1. **The colours (amends ADR-0056 items 4 and 6).** The dimensions of the selected body have the colour of the
   selection, the colour of its edges in the scene (`SceneRenderer.SelectedEdgeColour`, 1.0, 0.55, 0.1). The dimensions
   of the whole have a neutral grey, the colour of a quiet text of the chrome (#8a939f). The orange of `DimensionColour`
   stays the colour of a pending value and of a proposal.
2. **The words (amends ADR-0056 item 5).** Each label starts with the word of its box and " · ": "Overall · X 3400 mm"
   for the whole, and "Cote gauche · X 18 mm" for the selected body. A body with no name has its kind and a short
   identifier, as its row (ADR-0029 item 7). A name wider than 180 pixels ends with "...". The value keeps the form of
   ADR-0056 item 5.
3. **The plate (amends ADR-0056 item 4).** Each label is on a plate, as a value on an edge (ADR-0115 items 2 and 3): the
   text in the mono font, with the padding of a pill, round at its ends, with the ground of the scene and an outline in
   the colour of its box. The outline of a box on the screen is its corners in front of the eye. The plate is past each
   corner by a gap of 16 pixels and the half of the plate, along the normal of the measured edge, on the side away from
   the centre of the box. A box is convex, so the plate is outside the outline of its box. The dimension line goes
   through the middle of the plate, parallel to the edge, with an arrow at each end. An extension line goes from 4
   pixels off each end of the edge to 6 pixels past the dimension line. A plate takes no tap. The lines come first and
   the plates after them, and the plates of the selected body come over those of the whole. Until this record a
   dimension line was out from its edge by one twentieth of the largest extent of the box, and the label was 20 pixels
   above its middle.
4. **No control covers a plate.** The placement of ADR-0056 (V79) also moves a plate off each rectangle of the fixed
   controls with the status line, the panels, the rows, the icons, the labels and the caption of the ring, and the
   nodes, the texts and the caption of the history arc. A plate moves down while it can, else up. When neither frees it,
   it moves right of the control from its place, else left of it. The plates of the selected body are placed first, so
   they stay nearest to their body. To know those rectangles, the window places the dimensions after the ring and the
   history arc. It draws them on the first of two channels of the background list, so they stay under the outline of the
   face, the ring, the arc, the chips and the rows.
5. **The thickness (amends ADR-0056 item 6).** The size of a body shows its thickness only when the body is a board: the
   thickness (ADR-0056, Q1 A) is more than zero and at most a quarter of the middle extent of its box. A panel of 18 by
   400 by 800 is a board. A cube of 400, a bar of 40 by 40 by 800, and an extrude whose distance is its longest extent
   are not.
6. **No contract change, no engine change.** The overlay is presentation (ADR-0007). It uses + - * / and comparisons
   only.

## Consequences

**Good:** a person reads which value is of the selected body and which is of the whole by its word and its colour. No
label lies on the body that it measures, on the status line, on the ring or on the arc. A cube shows no thickness.

**Bad:** a plate is longer than a label with no word, so the plates of a small window move more. A plate that the
placement moves along its line is no longer at the middle of the line (ADR-0115 item 4). A plate that the placement
moves far from its edge has long extension lines across the model; in the niche of the UX test, "X 18 mm" of a panel
goes about 150 pixels down. A plate can lie on a body that it does not measure, for example a plate of the selected body
over its neighbours. When no move frees a plate, it stays under the control. The ratio of a board is a choice of the
session, not a rule of the trade. No person used the change in the window.

## Alternatives considered

- **One caption for each box, as "Cote gauche 18 × 400 × 800", and the axis only on each line.** Refused for now: the
  caption needs a place of its own, and each plate with its word reads alone.
- **The three decimals of ADR-0115 on the plates.** Refused in this record: the improvement is the words and the place,
  and the form of a value of the overlay stays the form of ADR-0056 item 5.
- **The whole band of the ring as a cover.** Refused: the ring is around the selected body, so its band would push each
  plate of that body far from it. The icons and the labels of the ring are the covers.

## History

- 2026-11-12 · TASK-0352 · The record, from improvement C of the UX test of 2026-11-10.
- 2026-11-12 · TASK-0352 · Shipped at v0.337.
- 2026-11-14 · TASK-0356 · ADR-0129 item 2 amends item 4: a plate that no move frees stays at its place, and the covers
  hold the windows of the last frame (findings V173 and V176 of the codebase review of 2026-11-14).
