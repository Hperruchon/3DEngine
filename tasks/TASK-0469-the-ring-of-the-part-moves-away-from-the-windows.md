---
id: 0469
title: On a landscape phone the ring of the part moves away from the windows of the top left, and its labels side by side get smaller
status: Active
phase: R32b
opened: 2026-11-26
depends-on: [0467]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0093, 0095, 0097, 0101, 0108, 0111, 0113, 0114, 0116, 0119, 0120, 0122, 0131, 0132, 0139, 0152, 0163, 0172, 0174, 0184]
writes:
  create:
    - tasks/TASK-0469-the-ring-of-the-part-moves-away-from-the-windows.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - eng/test-floor.txt
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/0132-the-labels-of-the-corner-ring.md
    - docs/adr/0184-the-section-view.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0468-the-findings-of-the-review-of-2026-11-25.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0469 — On a landscape phone the ring of the part moves away from the windows of the top left, and its labels side by side get smaller

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0076 (2026-11-21): on a landscape phone of 640 by 360 pixels the ring of the part with six segments (New, Sketch,
Bodies, Section, Images and Show hidden) has its inner radius at the floor of 100. "New" went under the windows of the top left,
and `ResponsiveLayoutTests` had an exception for the state `SixSegments` at that size. On 2026-11-25 the owner answered Q4 A: the
ring moves away from the windows of the top left, and each segment keeps its size (TASK-0464).

At the start the task found two more facts:

- "Show hidden", the last tool of a ring of five or six, was on the status line at 640 by 360. The layout test did not see it,
  because its rule of a scrolled control skips a tool of the ring under the structure view.
- A search of a model of the layout (the centre in steps of 4 pixels, the turn in steps of 0.02 radians) found no place of the
  ring of six where each target is free of the windows and no two labels at the size of the font meet. The room between the
  windows of the top and the windows of the bottom is 182 pixels, and the tools side by side are about 61 pixels apart.

On 2026-11-26 the owner saw the pictures before and after and answered "A: smaller labels": on a moved landscape ring the labels
side by side get smaller, down to the half of the size of the font, as on the portrait thumb ring (ADR-0132).

The same day the owner answered "All A, merge it" to Q1 to Q4 of the codebase review of 2026-11-25
(`docs/reviews/2026-11-25-codebase-review.md`, last section). The first commit of this task records them.

## Goal

At 640 by 360 the ring of the part with five or six tools has no tool under a window and no label on another label, and each
segment keeps its angle.

## Scope (in)

1. `DesktopUi.AwayFromWindows`: when a target of a tool of the landscape ring of the part meets a window above and left of its
   centre, the span turns down in steps of 0.01 radians, and the centre moves up or down as little as it can. The first step
   where each target is in the display and meets no window, and no two faces (a label with its icon) meet, gives the place.
2. `RingTextSize`: the labels of a moved ring follow the rule of the corner ring (ADR-0132 item 1), with the height of the icon
   in the test of a neighbour.
3. `ResponsiveLayoutTests`: no exception for `SixSegments`, and a new theory for 640 by 360 (six and five tools) and 844 by 390.
4. ADR-0113 item 5 and ADR-0132 item 2 hold the rule; ADR-0184 item 11 loses the sentence on R-0076.
5. The answers of the owner of 2026-11-26 to the review of 2026-11-25: Q1 A in the section "Order" of the roadmap, Q2 A and Q3 A
   in TASK-0468 and a progress line in R-0078, Q4 A in row K24.
6. R-0076 closed, register entry R-0079, the roadmap row, the test floor and the ledger entry v0.452.

## Scope (out)

- A body ring. It does not move: its tools leave the ring when it has no room (ADR-0119 item 2), and its rows read its boxes.
- The rule of a scrolled control in `Faults` for a tool of the ring (R-0079). Without it, the state Proposal fails at portrait
  sizes, and that is a separate fault.
- The target of "Show hidden" at 844 by 390, which meets the status line by about 17 pixels while its middle does not (R-0079).
  No window of the top left meets that ring, so it does not move.

## Acceptance criteria

- [x] `The_Ring_Of_The_Part_Moves_Away_From_The_Windows_Of_The_Top_Left` passes: at 640 by 360 with six and five tools each target
  is in the display and meets no window, no two faces meet, the segment is 0.42 radians and the inner radius 100; at 844 by 390
  the ring keeps the start of `Layout`.
- [x] `ResponsiveLayoutTests` has no exception, and each of its tests passes.
- [x] With the rule removed, the two cases of 640 by 360 and `Each_Control_Stays_In_The_Display_And_Free_For_A_Tap(640, 360)` fail.
  With the labels at the size of the font, the same three tests fail. With no test of the faces, the case of six fails.
- [x] Of 8,670 layouts of the two theories of each size and each state, only three change: the ring of the part at 640 by 360 in
  the states Images, Hidden and SixSegments.
- [x] R-0076 is closed, and `docs/register.md` holds 13 open entries of 15.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- A segment of a landscape ring is at most 0.42 radians (ADR-0119 item 2). At the inner radius of 100 that is 42 pixels, not 46 as
  R-0076 and row R32b said: the cap holds before the rule of 46 pixels. This task keeps the angle.
- `SegmentFace` takes the labels as an argument now. Before, it read `_ringLabels`, which is empty while the ring of the frame is
  not set, so a search of a place would not see the neighbours.

## Method

Mechanical: the test of the owner's case, from the register entry; the boxes of each window and each tool of the frame.
Judgement:  the turn down and the move of the centre, not a move to the right, because a move to the right puts "New" under the
            command line; only the ring of the part, because a body ring has its own rule; the height of the icon in the test of
            a neighbour, so that a label does not lie on the icon of the next tool.
Weakest:    "Show hidden" is at the half of the size of the font at 640 by 360, about 10 pixels high; no person read it on the
            phone.

## Progress

- 2026-11-26: opened on the order of the owner "Start R32b", branch `r32b-ring-landscape` from `origin/main` at `8f0ee112`
  (v0.451). The first form moved the ring with labels at the size of the font; the labels side by side met. The owner answered
  "A: smaller labels".
