---
id: 0095
title: The ring has the look of the orbit of the render, with a guide, icons and a caption
status: Amended
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0081', '0086']
amended-by: ['0108']
affects:
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0095 — The ring has the look of the orbit of the render, with a guide, icons and a caption

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The third slice that the owner chose on 2026-11-08 is the orbit ring. In the render "mix", a faint circle goes around the
part; the tools are segments on the right of that circle, each with a thin outline, a light ground, an icon and a short
label under it; and a caption "TOOLS FOR BRACKET" is to the right of the ring. ADR-0081 and ADR-0086 put the segments in the
right places, with a filled ground and a text label only.

### Q1. Where the icons come from

- **A.** Lines and arcs of the draw list of Dear ImGui, one small drawing for each tool, in the colour of the text.
- **B.** An icon font, which needs the download of a file and a decision of the owner, as for the fonts (ADR-0091).

A needs no new file. **Recommended: A.**

## Decision

The owner chose the slice; the recommendation answers Q1.

1. **The guide (amends ADR-0081).** A landscape ring has a faint circle at the inner and at the outer radius of its band.
   A portrait ring has no guide.
2. **The ground.** A segment that is not in use and not under the pointer has the colour of a button at 35 % opacity, and
   70 % under the pointer; the tool in use keeps the full colour (ADR-0086 item 2). The outline stays.
3. **The icon and the label (Q1 A, amends ADR-0086).** A segment draws its icon above its label, both in the middle of the
   segment: Move, Distance, Hide, Show, Sketch, Line, Select, Circle, Arc, Constrain, Finish and Back. A segment of a kind
   of a constraint keeps its label only. The labels are the short ones of the render: "Set distance" shows "Distance", and
   "Sketch on face" shows "Sketch"; the key of each segment does not change.
4. **The caption.** To the right of a landscape body ring, 24 pixels from its band: "TOOLS FOR" and the name of the body, in
   capitals with a space after each letter, in the dim colour; it shows only when it fits inside the display.
5. **No change of the places, the taps or the contract.**

## Consequences

**Good:** the ring reads as the orbit of the render, and the icons make a tool known before its label.

**Bad:** the icons are simple drawings. The caption does not show at 1280 x 720, where it does not fit, as in the render at
that size.

## History

- 2026-11-08 · TASK-0306 · The record, with the slice (the choice of the owner of 2026-11-08).
- 2026-11-08 · TASK-0306 · Shipped at v0.291.
- 2026-11-10 · TASK-0332 · ADR-0108 item 2 amends item 2: a segment that is not in use and not under the pointer has no
  fill; its icon is in the accent.
