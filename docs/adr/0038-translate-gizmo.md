---
id: 0038
title: The translate gizmo of the desktop
status: Accepted
topic: Rendering, clients
date: 2026-10-30
supersedes: []
superseded-by: []
amends: ['0026']
amended-by: []
affects:
  - 3DEngine.Core/Input/**
  - 3DEngine.Core/Gizmo/**
  - 3DEngine/GizmoActions.cs
  - 3DEngine.Vulkan/OverlayRenderer.cs
enforced-by: 3DEngine.Tests/GizmoTests.cs (TASK-0113 creates it)
---

# ADR-0038 — The translate gizmo of the desktop

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R9 gives the desktop a gizmo: handles on the selected body that a person drags to move it. The owner
chose the defaults R9-Q1 to Q8 on 2026-10-27 (`docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`, part 2),
and put R9 after the milestone "CAD on one part" (KD2). The sheet has no row R9-Q3; the roadmap row gives "a
constant screen size". The owner approved the plan of 2026-10-30, and its answer Q2 A permits a record of these
decisions as Accepted, because no public shape of `Engine.Contracts` changes.

Four facts constrain the design:

- **ADR-0007.** A drag is a state of the client. The client shows a preview, and the engine gets one command at
  the release. A cancel sends nothing.
- **ADR-0026.** The intent layer turns the samples of each kind of device into intents with no kind of device, and
  `3DEngine.Tests/IntentParityTests.cs` requires a touch script and a mouse script for each intent.
- **ADR-0029.** A tap or a click selects one body, and the panel and the highlight follow the selection.
- **ADR-0032.** The numeric pad of R10 already moves the selected body by dx, dy and dz, so the gizmo needs no keys.

## Decision

1. **Translate only (R9-Q1).** The gizmo moves a body. A rotation gets its own roadmap row, with a command that
   records a matrix or a quaternion (`CLAUDE.md`, determinism rule 2).
2. **The drag (R9-Q2).** The desktop moves the drawing of the body during the drag and sends nothing. At the
   release it sends one `Translate` with the offset of the drag as dx, dy and dz, and the selection moves to the new
   body (`BodyActions.Move`). A cancel sends nothing, and an offset of zero sends nothing.
3. **The size (the roadmap row).** Each axis of the gizmo is 100 window points long at the depth of the pivot, so
   the gizmo keeps its size on the screen at each zoom. A window point is the unit of the samples (`Window.Extent`).
4. **The handles (R9-Q4).** Three arrows from the pivot along +X, +Y and +Z, in red, green and blue, and three
   squares in the planes XY, YZ and ZX, from 25 % to 45 % of the length of the axes. A press inside a square takes
   the square. Otherwise a press within 8 window points of an arrow takes the nearest arrow. The handle under a drag
   is yellow.
5. **The start of a drag (R9-Q5).** The host gives `IntentMachine` a hit-test function, a point of the window in and
   a handle or null out. A press of the left button, or the first contact of a touch or a pen, that starts on a handle
   in the mode `Scene` is a drag of the gizmo. When it moves beyond the slop, the machine gives `GizmoStartIntent`
   with the handle and the point of the press, then `GizmoMoveIntent` for each motion, and `GizmoEndIntent` at the
   release. A second contact, the key Escape, a lost pointer, a loss of the focus and a change of the mode give
   `GizmoCancelIntent`. A press on a handle that does not move is a tap or a click, as before. This amends ADR-0026
   item 2: four new intents and the hit-test function. While a drag runs, the wheel, the arrow keys, plus, minus,
   H, Control+Z and Control+Y give nothing, so the view and the document stay still; Escape still cancels (V53, Q2 A
   of the codebase review of 2026-10-30 (b)).
6. **The keyboard (R9-Q6).** The pad of R10 is the keyboard of the gizmo. The gizmo adds no key other than Escape.
7. **The scope (R9-Q7).** The gizmo shows on the selected body when the body is live and visible, the mode is
   `Scene`, and no pad is open. The pivot is the centre of the box of the body. The pick comes from R8b.
8. **The offset.** An arrow moves the body along its axis: the offset is the change of the point of the axis that is
   nearest to the ray through the pointer. A square moves the body in its plane: the offset is the change of the
   point where the ray meets the plane through the pivot. A ray parallel to the axis or to the plane keeps the last
   offset, and so does a ray whose nearest point to the axis is nearer than the near plane, or behind the eye (V51). The functions live in `3DEngine.Core/Gizmo/`, with no device.
9. **The pipeline (R9-Q8).** `OverlayRenderer` draws the gizmo after the highlight and before the user interface,
   in the same render pass: triangles in window points, a push block with the scale, the translation and the colour,
   no depth test, no culling, and the blend of the alpha. The shaders are `overlay.vert` and `overlay.frag`.
10. **Determinism.** The client computes the offset with the camera, which uses a tangent. The command records three
    numbers, so the log holds no transcendental function (`CLAUDE.md`, determinism rule 1).

## Consequences

**Good:** A person moves a body with one finger or the mouse, and the engine gets one command. Touch and mouse give
the same intents, and a test holds it. The pad stays the exact way to move.

**Bad:**
- The gizmo hides a part of the body under it. A press on a handle cannot select another body behind it.
- An arrow that points to the eye is short on the screen, and its drag moves fast.
- No snap and no grid: the offset is the exact motion of the pointer.

**Next:** TASK-0113 applies this record. A rotation and a snap get their own rows.

## History

- 2026-10-30 · TASK-0113 · Accepted, from the defaults R9-Q1 to Q8 of the owner of 2026-10-27, under the answer Q2 A
  of the plan of 2026-10-30.
- 2026-10-30 · TASK-0124 · Item 5: other input during a drag gives nothing (V53, Q2 A of the owner). Item 8: a ray whose
  nearest point to the axis is nearer than the near plane keeps the last offset (V51).
