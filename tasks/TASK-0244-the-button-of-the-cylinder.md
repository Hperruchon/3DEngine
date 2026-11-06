---
id: 0244
title: The panel of the desktop and of the phone makes a cylinder with a pad of two values
status: Done
phase: K5b
opened: 2026-11-06
depends-on: [0238, 0241]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076]
writes:
  create:
    - tasks/TASK-0244-the-button-of-the-cylinder.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - docs/adr/README.md
    - docs/android-test-sheet.md
    - docs/adr/0076-the-cylinder.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0244 — The panel of the desktop and of the phone makes a cylinder with a pad of two values

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K5a (v0.223, ADR-0076) gives the command `CreateCylinder`. No client has a button for it: only the command line, the
HTTP surface and a script make one. ADR-0076, section "Next", gives the button of the desktop and of the phone, with a
pad of two values. The desktop and the phone draw the same panel of the library `3DEngine.App` (ADR-0072), so one change
gives the button to both. The box has the same form: a button "New box", the pad of ADR-0032 item 4, and
`BodyActions.CreateBox`.

## Goal

A person makes a cylinder from the panel, with a finger or the mouse, by its radius and its height.

## Scope (in)

1. ADR-0076 Q5 and item 7: a list "New" in the place of the button "New box", with the entries "Box" and "Cylinder";
   the first line of the panel has no room for a second button, and the panel has no room for a new line.
2. The pad of ADR-0032 with the fields `radius` (5) and `height` (10), in the length unit of the document.
3. `CreateCylinderAction` and `BodyActions.CreateCylinder`, which sends `CreateCylinder`; a rejection gives its code for
   the pad.

## Scope (out)

- The cone, the sphere and the torus.
- A place or an axis other than the origin and +z: the gizmo moves the cylinder after it is made.

## Acceptance criteria

- [x] A tap and a click of "New" and then "Cylinder" open the pad of two values, and an apply gives one action with
      the typed values.
- [x] Each test that opened the pad of a box with one tap opens it through the list.
- [x] Steps 2, 3 and 5 of the test sheet of the phone use the list, and step 5 cuts a round hole.
- [x] `BodyActions.CreateCylinder` gives one `CreateCylinder` with the values, and a rejection gives its code.
- [x] The list and each entry are at least 44 pixels high (ADR-0027 item 6).
- [x] An injection that swaps the two values fails a test.

## Outcome

Status: Done · commit (the merge of the branch k5b-cylinder-list) · v0.227

## Method

Mechanical: the form of the list "Cut with" for the list "New", and the pad of "New box" with two fields.
Judgement:  a list and not a second button (Q5 A): a button on a new line moved Show all 16 pixels below the panel in
            DesktopFormsTests and hid a tool of CutUiTests; the default sizes, 5 and 10.
Weakest:    no person tapped the list on the desktop or on the phone; the case of the host that sends the action has no
            test, as for the box; no test proves the rectangle of the open list, because it lies inside the panel.

## Progress

- 2026-11-06: a button "New cylinder" first: it took a new line, and two layout tests failed. The label "Cylinder" did
  not fit either (the line holds 306 of its pixels already). ADR-0076 Q5 A: the list "New".
- 2026-11-06: the list, the pad, the action and three new tests; an injection that swaps the radius and the height
  fails the two rows of the pad. An injection that leaves the open list out of the rectangles of the panels fails no
  test, because the list lies inside the panel at this layout.
- 2026-11-06: run 42282849670 passed. Closed at v0.227.
