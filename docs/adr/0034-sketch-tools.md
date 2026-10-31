---
id: 0034
title: The sketch tools of the desktop
status: Amended
topic: Clients, sketch
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0026', '0029', '0032']
amended-by: ['0041']
affects:
  - 3DEngine.Core/Input/**
  - 3DEngine.Core/Picking/**
  - 3DEngine.Core/Numeric/**
  - 3DEngine.Core/Sketching/**
  - 3DEngine/Ui/**
  - 3DEngine/SketchActions.cs
  - 3DEngine/SketchOverlay.cs
enforced-by: 3DEngine.Tests/SketchToolTests.cs
---

# ADR-0034 — The sketch tools of the desktop

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K2b gives the desktop the tools to build a sketch: a sketch mode in the intent layer, a ray to the
plane of the sketch, a pad for a line, and a panel of constraints (Q8 of the second codebase review of 2026-10-28).
K2a (ADR-0033, TASK-0100) gave the engine side: the seven sketch commands, the query `GetSketch` with the residual of
each constraint, and a drawing of each live sketch over the scene, read only (`SessionSketches`, `SketchOverlay`).

Five facts constrain the design:

- **Objective 9 and ADR-0026 item 5.** Each operation works by touch and by mouse. Each kind of intent needs a touch
  script in the parity test.
- **ADR-0007.** The tool, the selection of sketch items and the camera are states of the client. Only a command
  changes the Document. A position that the client computes from a ray is a number of a command, as a value of the
  pad is (ADR-0032 item 2). The log never holds the ray, so a replay computes no ray.
- **No solver before K3a.** A constraint does not move a point in K2b. Two points at one position stay two points.
  So a closed profile for the extrude of K0a must reuse its first point: the tool must join a line to a point that
  exists.
- **The camera.** The camera has the axis y up, and its pitch stops at ±89° (`Camera.MaximumPitchDegrees`). A ray
  that is parallel to the plane of a sketch gives no point.
- **The pad.** The pad has three fields (ADR-0032 item 3). A point of a sketch has two numbers, and a distance has
  one.

On 2026-10-28 the owner answered the questions Q1 to Q6 below, A to each.

### Questions for the owner

**Q1. The sketch mode in the intent layer.**
- A: The `IntentMachine` gets a mode, `Scene` or `Sketch`, which the host sets. In the mode `Sketch` a second map of
  the settings applies: a tap or a click gives `SketchPointIntent(x, y)`, a point of the window; a long press, a right
  click, a tap of two fingers, H, and Shift and H give nothing; the drags, the pinch, the wheel, the keys of the camera and the keys of undo
  and redo stay as they are. The host gives the intent to the active tool.
- B: The machine does not change. The host reads a `SelectIntent` as a point of the sketch while a sketch is active.

**Recommended: A.** The roadmap row names a mode in the intent layer. The map stays a setting (ADR-0026 item 3), and
the parity test covers the new intent. With B, a hide by a long press still hides a body during a sketch.

**Q2. The tool for a line.**
- A: A chain. Each tap adds the next point and a line from the last point of the chain. A tap within 22 pixels of a
  point of the sketch uses that point and adds no point. A tap on the first point of the chain closes the chain and
  ends it; a tap on the last point ends it. The button "End" ends it too. Each tap sends its commands at once.
- B: Each tap adds a free point. A line joins two points that the person selects.
- C: As A, but the chain is a preview of the client (ADR-0007, "in-progress sketch points"), and its commands go to
  the session only at the end of the chain.

**Recommended: A.** A closed profile needs a shared first point, and no solver can join two points before K3a. An
agent sees each line when it is made. With C, a crash of the desktop loses the chain, and the preview needs its own
drawing. 22 pixels is half the height of a control (ADR-0027 item 6), so a finger hits a point.

**Q3. The position of a tap.**
- A: The client rounds each coordinate of a tap to a step of the series 1, 2 and 5 times a power of 10 mm. The step is
  the smallest one that is at least 8 pixels on the plane at the current zoom. A person sees round numbers, and two
  taps on one row give the same y.
- B: No rounding. The pad gives each exact value.
- C: A fixed step of 1 mm.

**Recommended: A.** Without a solver, a tapped rectangle is a rectangle only if its numbers agree. A fixed step is
too coarse near the part and too fine far from it. The step is presentation, not a snap engine (R11): it has no grid
to show and no target to find.

**Q4. The camera at the start of the sketch mode.**
- A: The camera turns to look along the normal of the plane, from the side of the eye now, and keeps its target and
  its distance. For the plane XZ the pitch stops at 89°, so the view is almost along the normal.
- B: The camera does not move. The person orbits.

**Recommended: A.** A view along the normal gives no ray parallel to the plane, and the person sees the shape of the
profile. The person can orbit after the turn.

**Q5. The pad for a line.**
- A: The pad holds one, two or three fields (an amendment of ADR-0032 item 3). The tool for a line gets the button
  "Next point". It opens a pad of two fields: `dx` and `dy` from the last point of the chain, or `x` and `y` when no
  chain is open. An apply does what a tap at that position does. A button "Close" joins the last point to the first
  point and ends the chain.
- B: A pad of four fields, `x1`, `y1`, `x2` and `y2`, gives one free line with two new points.
- C: The fields `length` and `angle` in degrees; the client computes the end point with a cosine and a sine.

**Recommended: A.** A tap and the pad then give one chain, so a rectangle of exact size is four applies and "Close":
`0, 0`, then `40, 0`, `0, 20`, `-40, 0`. B gives no shared point, so no closed profile. C needs four fields or a
second pad for the start, and a cosine gives numbers that a person did not type.

**Q6. The selection of sketch items, and the panel of constraints.**
- A: The sketch mode has two tools, "Line" and "Select". With "Select", a tap selects the nearest point of the sketch
  within 22 pixels, or else the nearest line within 22 pixels, and a second tap on a selected item clears it. The
  selection holds two items at most. The window of the sketch lists the points, the lines and the constraints, and a
  row selects as a tap does. The buttons of the five kinds show when the selection fits the kind.
- B: As A, but only the rows of the window select. A tap in the scene always adds a point.

**Recommended: A.** A person finds a line in the scene faster than in a list of rows. The pick needs no GPU: the
desktop already projects each point to pixels for the drawing. With B the rows still show labels in the scene, so B
costs a little less and is slower to use.

## Decision

The decision below applies the recommended answers: Q1 A, Q2 A, Q3 A, Q4 A, Q5 A and Q6 A.

1. **The mode (Q1).** `IntentMachine` gets the property `Mode`, `IntentMode.Scene` or `IntentMode.Sketch`. The host
   sets it. `IntentSettings` gets a second map, `SketchMap`, for the mode `Sketch`. Its default: a tap or a click
   gives the action `SketchPoint`; a long press, a right click, a tap of two fingers, H, and Shift and H give `None`;
   each other gesture has the action of `Map`. The action `SketchPoint` gives `SketchPointIntent(X, Y)` in pixels of
   the window. `Validated` permits `SketchPoint` only for a gesture of a point. A change of the mode ends the gesture
   in progress, as a lost pointer does.
2. **The ray to a plane.** `Picker.OnPlane(ray, origin, u, v)` in `3DEngine.Core/Picking/` gives the position
   (x, y) = (dot(p − origin, u), dot(p − origin, v)) of the point p where the ray meets the plane, with
   n = u × v. It gives null when |dot(direction, n)| < 1e-9, or when the point is behind the origin of the ray. It
   uses `+ - * /` only. `3DEngine.Core` references no project, so the host gives the origin, u and v of the datum
   from `Engine.Contracts/Sketch/Datums.cs`.
3. **The position of a tap (Q3).** `SketchGrid.Step(pixelsPerMillimetre)` gives the smallest step of the series
   1, 2 and 5 × 10ᵏ mm that is at least 8 pixels, for k from −3 to 6. The host measures the pixels of 1 mm on the
   plane at the hit point from two rays. Each coordinate is rounded to a multiple of the step, half away from zero.
   The minimum step is 0.001 mm.
4. **The tools (Q2, Q6).** `3DEngine.Core/Sketching/SketchTool.cs` is a pure state machine with the tools `Line` and
   `Select`. Its input is a point intent with the pixels of each point and each line of the active sketch, and the
   rounded position of the tap or null. It gives a list of steps: add a point, use a point, add a line, end the chain,
   toggle an item. It references no command. `3DEngine/SketchActions.cs` turns each step into a command of the
   session (`AddSketchPoint`, `AddSketchLine`), as `BodyActions` does for a body. The chain of the tool `Line`:
   - With no chain, a tap starts one at its point.
   - A tap within 22 pixels of a point uses that point. On the first point of the chain it adds the last line, and
     the chain ends. On the last point the chain ends. On another point it adds a line, and the chain continues.
   - Each other tap adds a point and a line from the last point.
   - A tap whose ray gives no position does nothing.
   The tool `Select` toggles the nearest point within 22 pixels, or else the nearest line within 22 pixels. The
   selection holds three items at most; a fourth item replaces the first. Until K2g (TASK-0127) it held two.
   Phase K2e (TASK-0122) adds the tools `Circle` and `Arc` (`AddSketchCircle`, `AddSketchArc`):
   - `Circle`: a tap gives the centre, and a second tap a point on the circle. The radius is the distance in the
     plane, with `sqrt` only, and the tool adds no point on the circle.
   - `Arc`: a tap gives the centre, then the start, then the end, counter-clockwise from the start (ADR-0033 item 2).
     A new end goes on the circle of the start in the direction of the tap, so the equation of the arc holds.
   - A tap on a point uses that point. A tap on the centre or on the start again ends the curve. The pad gives a
     position, not an offset.
   - The tool `Select` also picks the nearest curve within 22 pixels of its polyline.
   The window of the sketch has the buttons Circle and Arc, and the kinds on a curve: On curve (a point and a curve),
   Tangent (a line and a curve, or two curves outside), Inside tangent, Equal (two curves) and Radius, which opens the
   pad with the current radius. A curve draws as a polyline of 64 segments for a full turn; the polyline is
   presentation (ADR-0007), so the desktop computes it with a sine and a cosine. A circle has the label R1, R2 and
   onward, and an arc A1, A2 and onward.
   Phase K2f (TASK-0125) adds the kinds of K2c on lines: On line and Midpoint (a point that is not an end of the
   line), and Equal, Parallel, Perpendicular and Angle (two lines). Angle opens a pad of degrees with the angle from
   the first line to the second, and the client gives the cosine and the sine to the command (ADR-0033 item 3). A
   multiple of 90 degrees gives exact numbers. Phase K2g (TASK-0127) adds Symmetric: two points that are not ends of
   the line, and the line.
5. **The undo.** Each command is one step of the history (ADR-0033 item 10). A tap that adds a point and a line gives
   two steps. An undo or a redo of the session can change the active sketch; the tool then drops each item of the
   chain and of the selection that is not in the record, and ends the chain when its last point is gone.
6. **The start and the end of the mode (Q4).** The panel "Document" gets the buttons "Sketch XY", "Sketch XZ" and
   "Sketch YZ". Each one sends `CreateSketch` with the datum, and the new sketch becomes active. The window "Sketch"
   lists each live sketch, and its button "Edit" makes a sketch active. A sketch that becomes active puts the
   machine in the mode `Sketch` and turns the camera: a yaw and a pitch that look along the normal of the datum, from
   the side of the eye, with the pitch limit of the camera. The button "Finish" makes no sketch active and puts the
   machine in the mode `Scene`. When the active sketch leaves the session, for example by an undo of its
   `CreateSketch`, the mode ends too.
7. **The pad (Q5).** `NumericPad` holds one, two or three fields. Each pad of ADR-0032 item 4 keeps its three fields.
   The tool `Line` gets the button "Next point": a pad of `dx` and `dy` from the last point of the chain, or `x` and
   `y` with no chain, each with the text `0`. An apply sends the steps of a tap at that position, without the
   rounding of item 3 and without the 22 pixels of item 4: an apply uses a point only at the exact position. The
   button "Close" adds the line from the last point to the first point, and ends the chain.
8. **The window "Sketch".** A third window of the R7b user interface, shown while a sketch is active (ADR-0027). It
   shows:
   - the name or the short identifier of the sketch, its datum, and the buttons "Line", "Select", "End", "Close",
     "Next point" and "Finish";
   - a row for each point (`P1 (x, y)`), each line (`L1 P1–P2, length`) and each constraint (`C1 horizontal L1,
     residual`), in the order of the record. The labels are display numbers of the client, not identifiers;
   - for the selection, the buttons of the kinds that fit it: "Coincident" for two points, "Horizontal" and
     "Vertical" for one line, "Distance" for two points, a point and a line, or one line, and "Fixed" for one point;
   - "Delete", which sends `DeleteSketchItem` for each selected item, and "Move point" for one selected point.
   A kind with a value opens the pad: "Distance" with one field, `length`, and the current distance as its text;
   "Fixed" and "Move point" with the fields `x` and `y`, and the current position. A selected constraint row gives
   the button "Value", which opens the pad with its values and sends `SetSketchConstraintValue`. Each control is at
   least 44 pixels high (ADR-0027 item 6). A rejected command shows its code and its message in the window.
9. **The drawing.** The active sketch draws in yellow, as in K2a, and each other live sketch in grey. A selected item
   draws in white and wider. Each point and each line of the active sketch has its label of item 8 beside it. The
   first point of an open chain has a ring, so that a person sees where "Close" goes. The drawing stays on the
   background list of the UI layer (ADR-0033 item 13).
10. **The parity tests.** The parity test gets a touch script and a mouse script for `SketchPointIntent`. A test in
    `3DEngine.ImageTests` gives a touch script and a mouse script that draw one closed rectangle to a session through
    the machine, the tool and `SketchActions`, and compares the two logs. The window "Sketch" has tests of its
    controls with no GPU, as the pad has.
11. **No new code.** K2b adds no diagnostic code. A refusal of the engine shows its code of the namespace `SKETCH`.

This record amends ADR-0026 items 2 and 3 (the mode, the second map and the new intent), ADR-0029 item 4 (the
gestures of a hide and of a show all give nothing in the mode `Sketch`), and ADR-0032 items 3 and 4 (a pad of one to three fields, and
the pads of a sketch).

## Consequences

**Good:** A person draws a closed profile and adds the five constraints of K3a by touch, by mouse or with exact
numbers, and an agent sees each command. The parity test and the comparison of two logs hold the touch path. The
engine does not change. The tool is a pure state machine, so a test reads it with no window.

**Bad:**
- A tap that adds a point and a line gives two steps of undo, so a rectangle is eight steps. A group of steps waits
  for a measured need (ADR-0033 item 10).
- With no solver, a constraint that the positions do not satisfy only shows its residual. A person moves a point
  with the pad.
- The rounding of a tap depends on the zoom, so two taps at two zooms can give two steps. The pad gives exact values.
- A tap in the scene during the sketch mode never selects a body.
- No touch screen exists, so the touch path is proven by synthetic samples only.
- The panel grows by three buttons, and a third window can cover the scene.

**Next:** TASK-0101 applies this record. K3a solves the constraints, and K0a extrudes the closed profile.

## History

- 2026-10-28 · TASK-0101 · Proposed, with the questions Q1 to Q6 for the owner.
- 2026-10-28 · TASK-0101 · The owner accepted the record on 2026-10-28 with Q1 to Q6 A, and no change.
- 2026-10-28 · TASK-0101 · The window of the sketch is 480 pixels wide, and the origin has the label O; the points
  are P1, P2 and onward. A tap on the first point of a chain of one line ends the chain and adds no line.
- 2026-10-30 · TASK-0122 · Item 4: the tools Circle and Arc, the pick of a curve, the kinds on a curve in the window,
  and the polyline of a curve (phase K2e).
- 2026-10-31 · TASK-0125 · Item 4: the kinds of K2c on lines in the window (phase K2f).
- 2026-10-31 · TASK-0127 · Item 4: the selection holds three items, and the window has Symmetric (phase K2g, the
  answer "1 3 4" of the owner).
