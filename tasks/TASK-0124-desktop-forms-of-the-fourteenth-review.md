---
id: 0124
title: The gizmo stays in front of the eye, and the overlay keeps each visible part of a line
status: Done
phase: P0.50
opened: 2026-10-30
depends-on: [0122, 0123]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0026, 0029, 0031, 0032, 0034, 0038]
writes:
  create:
    - tasks/TASK-0124-desktop-forms-of-the-fourteenth-review.md
  modify:
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine/GizmoActions.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.ImageTests/GizmoActionsTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - docs/adr/0038-translate-gizmo.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0124 — The gizmo stays in front of the eye, and the overlay keeps each visible part of a line

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 (b) found V51 and V52 (medium) and V53 to V55 (low) in the desktop.
`docs/templates.md`, section 7, puts them into a task after the next product phase. K2e changes
`3DEngine/SketchOverlay.cs`, so this task comes after it. V53 waits for the answer to Q2 of the review (register entry
R-0046).

## Goal

A drag of the gizmo never puts the body behind the eye, the overlay draws each visible part of a line, and the
release, the other input and the meshes of a frame follow the review.

## Scope (in)

1. V51: an arrow drag gives no offset for a point that is not in front of the eye.
2. V52: the overlay clips a segment to the window before the limit of a pixel.
3. V53, as Q2 decides, and its item in ADR-0038.
4. V54: the release sends nothing when the body has no pivot. V55: a creation takes the state of the last change of
   its frame.

## Scope (out)

- A change of the engine.
- V53 when the owner did not answer Q2. It then stays open with R-0046.

## Acceptance criteria

- [x] Each form of the review fails a test.
- [x] An injection that restores each old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p050-desktop-fourteenth) · v0.126

## Method

Mechanical: the check of the ray of an arrow, the drop of other input during a drag, the clip of a segment, the check
            of the pivot at the release, and the state of a creation.
Judgement:  the clip keeps a margin of one window on each side, so a stroke of the UI library stays far from a large
            magnitude; Q2 A drops the input, and Escape still cancels.
Weakest:    no person dragged the gizmo past a vanishing point in the real window; the tests use the functions of
            the gizmo and the intent machine with no device.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (b) (TASK-0120).
- 2026-10-30: the owner answered Q2 A. V51: an arrow gives no offset past the vanishing point of its axis. V53: other
  input during a drag gives nothing. ADR-0038 items 5 and 8 hold both, and R-0046 is closed.
- 2026-10-30: V52: the overlay clips a segment to the window with a margin of one window; the test of V49 now checks
  that no vertex goes beyond the limit, as a line through the window draws its visible part. V54 and V55. 6 new test
  cases, one of them on a GPU; 6 injections caught.
- 2026-10-30: closed at v0.126. The last progress line said 7 new test cases; the count is 6.
