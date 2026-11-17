---
id: 0397
title: A hole in a plate is one whole circle from the top and from the bottom, and the hidden circle of v0.381 is a blind hole
status: Done
phase: P0.161
opened: 2026-11-17
depends-on: [0396]
governed-by: [0142]
writes:
  create:
    - tasks/TASK-0397-the-circle-of-a-hole-from-the-top-and-the-bottom.md
  modify:
    - Engine.Tests/Drawing/ViewEdgesTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0397 — A hole in a plate is one whole circle from the top and from the bottom, and the hidden circle of v0.381 is a blind hole

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The ledger entry v0.381 (P10a, TASK-0396) says, under "Found", that the top view of a plate with a through-hole gives the
circle of the top face as one hidden circle, from the lines of a view (K9, `Engine.Core/Drawing/ViewEdges.cs`). On
2026-11-17 the owner asked for a task that finds the cause, corrects it, and adds a test of the case from the top and
from the bottom. The reproduction of the owner:

1. A sketch on Datums.XY with the rectangle (0,0), (60,0), (60,40), (0,40), and Extrude with Distance 10.
2. CreateCylinder with Radius 8 and Height 20, Translate by (20, 20, -5), and Subtract it from the plate.
3. CreateSheet, and AddSheetView with right (1,0,0), up (0,1,0) and eye (0,0,1), at 1 : 1.
4. GetSheet gives the four lines of the plate and one whole circle of radius 8, with Visible = false.

## Goal

A test holds the lines of a hole in a plate from the top and from the bottom, and the ledger gives the true cause of the
hidden circle of v0.381.

## Scope (in)

1. The cause of the hidden circle in the reproduction.
2. A correction of `ViewEdges` if the cause is in it.
3. A test through GetSheet of the case of the owner and of a through-hole, from the top (eye (0,0,1)) and from the bottom
   (eye (0,0,-1), up (0,-1,0)).

## Scope (out)

- A change of CreateCylinder: its place on the origin is the decision of its handler and of the test of its command.
- A change of the SVG export (P10a).

## Acceptance criteria

- [x] The cause of the hidden circle has a proof from the engine.
- [x] A test of the two cases fails on each injection of a fault in the visibility of a piece of an edge, and each file
      comes back byte for byte.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Findings

The cause is not in `ViewEdges`. CreateCylinder puts the cylinder from z = -height / 2 to z = +height / 2
(`Engine.Core/Commands/CreateCylinderCommandHandler.cs`, line 82). A cylinder of height 20 moved by -5 along z goes from
z = -15 to z = 5, not from z = -5 to z = 15. The Subtract makes a hole of depth 5 that is open at the bottom face, with
its floor at z = 5. The reproduction is not a through-hole.

The lines of the view are correct for that solid. From the top, the floor of the hole (z = 5) and its edge on the bottom
face (z = 0) have one place on the view, and both lie under the top face (ADR-0142 item 4). The top view gives one hidden
circle (ADR-0142 item 5). From the bottom, the edge on the bottom face is visible, and the floor lies under it.

A probe of the engine on 2026-11-17 gave these lines (the curve of the side has its base at z = -15, and the edges of
the floor of the hole are at z = 5):

| Move along z | Cylinder | Hole | Top view | Bottom view |
|---|---|---|---|---|
| -5 | z = -15 to 5 | depth 5, open at the bottom | one hidden circle | one visible circle |
| 0 | z = -10 to 10 | through | one visible circle | one visible circle |
| 5 | z = -5 to 15 | through | one visible circle | one visible circle |

With CreateBox, the box is on the origin too, so the cylinder at (20, 20) cuts the edge of the box. That case is not a
second view of the fault: the hole there is a notch through the whole height, and its half circle is visible.

## Outcome

Status: Done · commit (the merge of the branch p0161-hole-circle) · v0.382

## Method

Mechanical: the test of the two cases through GetSheet, with the helpers of the other tests of a session.
Judgement:  no change of `ViewEdges`, because the read is correct for the solid that the commands make. The test keeps
            the case of the owner (the hidden circle is the expected result) and adds the through-hole.
Weakest:    the test reads one circle and its visibility; it does not read the centre of the circle on the paper.

## Progress

- 2026-11-17: opened under the order of the owner of 2026-11-17. A probe found that the cylinder of the reproduction
  ends at z = 5, so the hole is blind and the hidden circle is correct. No change of `Engine.Core`. The test
  `A_Hole_In_A_Plate_Is_One_Whole_Circle_From_The_Top_And_From_The_Bottom` holds the two cases. Three injections in
  `ViewEdges.cs` each failed it (a face that covers a point at its own depth, a hidden piece before a visible one, no face
  that covers a point), and the file came back byte for byte (SHA-256).
- 2026-11-17: run 44159241574 passed on the branch. The owner approved the push and the merge. Closed at v0.382.
