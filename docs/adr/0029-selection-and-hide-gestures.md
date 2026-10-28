---
id: 0029
title: The selection, the hide gestures and the frame of the camera in the desktop
status: Accepted
topic: Rendering, clients
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0025', '0026', '0027']
amended-by: []
affects:
  - 3DEngine.Core/Input/**
  - 3DEngine.Core/Picking/**
  - 3DEngine/**
enforced-by: 3DEngine.Tests/PickingTests.cs (TASK-0086 creates it)
---

# ADR-0029 — The selection, the hide gestures and the frame of the camera in the desktop

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R8b shows the name and the visibility of ADR-0028 in the desktop. The owner decided the defaults R8-Q6
and R8-Q7 on 2026-10-27 (decision sheet of that date, part 2), and decision KD6: the camera jumped after each
change of a body, because ADR-0025 item 6 frames the bodies at each change, and with names and visibility a
person changes bodies often. The selection is a state of the client (ADR-0007). The engine knows no
selection and no pick (ADR-0009, ADR-0028 item 4).

## Decision

1. **The selection (R8-Q6).** The desktop selects one body or none. A select intent picks the nearest visible
   body under the point: a ray from the eye through the pixel, tested against the triangles that the desktop
   holds for each body, in double precision. The test lives in `3DEngine.Core/Picking/` and needs no GPU.
2. **The highlight.** A second pipeline draws the selected body again over itself in one colour, with
   blending and a depth test that passes on equal depth. The mesh pipeline does not change.
3. **A hidden body.** The desktop does not draw a hidden body, cannot pick it, and leaves it out of the box
   that the camera frames. The engine still holds it (ADR-0028 item 4).
4. **The gestures (R8-Q7).** A long press of a finger or a pen, or a right click, hides the body under the
   point. The key H hides the selected body. A tap of two fingers, or Shift and H, shows each hidden body. Each
   gesture is a setting of the map of ADR-0026 item 3. A long press is a press with no motion that lasts at
   least the setting `LongPressNanoseconds`, 500 ms by default; the state machine decides it at the release,
   so it still reads no clock.
5. **The intents.** `HideIntent` names a point of the window or, with no point, the selection;
   `ShowAllIntent` has no field. The parity test has a touch script for each.
6. **Commands.** A hide is the command `SetBodyVisibility` with `visible` false; a show all is one such command
   with `visible` true for each hidden live body; a rename is the command `RenameBody`. The desktop sends each
   one to its session, as an agent does (ADR-0019 item 2). A test compares the log of a touch script with the
   log of a mouse script.
7. **The panel.** Each row shows the name of a body, or its kind and a short identifier when it has no name,
   and the word "hidden" for a hidden body. A tap or a click on a row selects the body. The selected body has a
   name field, a button "Rename", and a button "Hide" or "Show"; a button "Show all" shows each hidden body.
   Each control is at least 44 pixels high (ADR-0027 item 6).
8. **The frame of the camera (KD6).** The camera frames the visible bodies only when the set of visible bodies
   goes from empty to not empty. A later change of a body does not move the camera.

This record amends ADR-0025 item 6 (item 8 above), ADR-0026 items 2 and 3 (items 4 and 5), and ADR-0027
item 7 (item 7).

## Consequences

**Good:** A person selects, hides, shows and renames a body by touch or by mouse, and each change is a command
that an agent sees. The camera stays where the person put it. The pick needs no GPU, so a test reads it.

**Bad:** The desktop keeps a copy of the triangles of each body for the pick. A long press is known only at
its release, so the body hides when the finger lifts, not while it is down. A body that hides under a camera
that framed it leaves the camera where it is.

**Next:** TASK-0086 applies this decision. R9 (the gizmo) uses the selection.

## History

- 2026-10-28 · TASK-0086 · The record is written with the defaults R8-Q6 and R8-Q7 and decision KD6 of the
  owner of 2026-10-27.
