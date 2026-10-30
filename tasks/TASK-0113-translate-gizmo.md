---
id: 0113
title: The desktop moves the selected body with a translate gizmo
status: Done
phase: R9
opened: 2026-10-30
depends-on: [0112]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0029, 0031, 0032, 0034, 0038]
writes:
  create:
    - tasks/TASK-0113-translate-gizmo.md
    - docs/adr/0038-translate-gizmo.md
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine/GizmoActions.cs
    - 3DEngine.Vulkan/OverlayRenderer.cs
    - 3DEngine.Vulkan/Shaders/overlay.vert
    - 3DEngine.Vulkan/Shaders/overlay.frag
    - 3DEngine.Vulkan/Shaders/overlay.vert.spv
    - 3DEngine.Vulkan/Shaders/overlay.frag.spv
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.ImageTests/GizmoActionsTests.cs
    - 3DEngine.ImageTests/GizmoImageTests.cs
  modify:
    - 3DEngine.Core/Input/Intents.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine.Vulkan/Shaders/shaders.lock
    - 3DEngine.Tests/IntentParityTests.cs
    - docs/adr/0026-intent-layer.md
    - docs/adr/README.md
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

# TASK-0113 — The desktop moves the selected body with a translate gizmo

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R9 gives the desktop a gizmo. The owner chose its defaults R9-Q1 to Q8 on 2026-10-27 and put R9 after
the milestone "CAD on one part" (KD2). The plan of 2026-10-30 puts R9 after P0.44, and its answer Q2 A permits
ADR-0038 as Accepted, because no public shape of `Engine.Contracts` changes. ADR-0038 gives the decisions.

## Goal

A person drags an arrow or a square of the gizmo on the selected body with one finger, a pen or the mouse, the
drawing of the body follows, and the release sends one `Translate`.

## Scope (in)

1. The intents of the gizmo and the hit test of the intent machine, with a touch script and a mouse script each.
2. The layout, the hit test, the offset and the shapes in `3DEngine.Core/Gizmo/`, with no device.
3. `GizmoActions`: the drag, the preview and the command at the release.
4. `OverlayRenderer` and its two shaders: the gizmo after the scene and before the panels.
5. Tests with no GPU, an image test, and a visual check of the owner.

## Scope (out)

- A rotation, a snap and a grid (ADR-0038, "Next").
- A gizmo on more than one body.

## Acceptance criteria

- [x] Touch and mouse give the same intents of the gizmo, and the coverage test holds the four new kinds.
- [x] The end of a drag sends one `Translate` with the offset, and a cancel and a zero offset send nothing.
- [x] The image test shows the arrows in front of the body.
- [x] An injection that breaks each part fails a test.
- [x] The owner accepts the pictures of the visual check.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r9-gizmo-on-main) · v0.119

## Method

Mechanical: the four intents and their scripts, the layout, the hit test, the offset, the overlay pipeline and its
            shaders, and the tests.
Judgement:  the hit test is a function of the host, so the intent layer keeps no geometry; the drag starts after
            the slop, so a still press on a handle selects as before. The preview moves the transform of the entity
            and the engine sees nothing until the release.
Weakest:    no touch screen ran the gizmo; its touch path is synthetic, as for the sketch tools. An arrow that
            points to the eye is short and moves fast.

## Progress

- 2026-10-30: opened by the plan of 2026-10-30, after P0.44.
- 2026-10-30: the intents, the geometry, the drag, the overlay pipeline and the tests; 10 injections caught. The visual
  check ran on this computer: the gizmo on a box, and one Translate (dx 11.89, dy 0, dz 0) from a drag of the arrow X.
- 2026-10-30: the owner said yes to the pictures of the visual check. The commit moves onto main by a cherry-pick on
  the branch r9-gizmo-on-main, with the index of the ADRs and the test floor merged by hand.
- 2026-10-30: closed at v0.119.
