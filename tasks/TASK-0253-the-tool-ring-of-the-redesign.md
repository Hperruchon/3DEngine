---
id: 0253
title: The tools of the selected body are the segments of a ring that follows it
status: Active
phase: R16b2
opened: 2026-11-06
depends-on: [0248, 0250]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0077, 0078, 0080, 0081]
writes:
  create:
    - tasks/TASK-0253-the-tool-ring-of-the-redesign.md
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/RingImageTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/README.md
    - tasks/TASK-0252-desktop-and-phone-forms-of-the-twenty-seventh-review.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0253 — The tools of the selected body are the segments of a ring that follows it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening); after the fixed controls (R16b1)
comes the tool ring that follows the selection, with the tools that exist today. The designer brief draws it around the
part on a large display and under the thumb on the phone.

## Goal

The tools of the selected body that need no text are segments of a ring next to it, and a tap or a click on a segment
gives the tool.

## Scope (in)

1. ADR-0081, Accepted under the standing order of 2026-11-05.
2. The layout of the ring for a landscape and a portrait display, its segments and their labels.
3. The taps on a segment, and the band of the ring as a part of the panels.
4. The centre and the size of the selected body on the screen, from the host.

## Scope (out)

- Rename and "Cut with" on the ring; the removal of the buttons of the panel.
- The ring of the tools of a sketch.

## Acceptance criteria

- [ ] The ring of a landscape display is around the body, and each segment is at least 44 pixels deep and 54 across.
- [ ] The ring of a portrait display is in the lower right corner.
- [ ] A tap and a click on a segment give its tool, and the inside of the ring is the scene.
- [ ] With no centre or with a pad open, no ring shows.
- [ ] A picture of the ring on the GPU is kept for the owner.

## Outcome

Status: Active

## Method

Mechanical: the segments as quads on the background list, and a hit test in polar coordinates.
Judgement:  the tools on the ring (Q1 A) and the buttons that stay (Q2 A); the inner radius that keeps the ring outside
            the body on the screen.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the ring, five tests with no GPU and one image test. The first picture showed a segment filled as one
  convex path, which a ring segment is not; the segments are now rows of quads. The first ring lay over a body that was
  large on the screen; the inner radius now takes the size of the body.
