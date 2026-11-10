---
id: 0333
title: The edges of the body as lines, the hidden edges dashed, and a transparent selection
status: Done
phase: R18b
opened: 2026-11-10
depends-on: [0330]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0029, 0031, 0038, 0043, 0072, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0102, 0104, 0105, 0106, 0109, 0110, 0111]
writes:
  create:
    - tasks/TASK-0333-the-edges-of-the-body.md
    - docs/adr/0109-the-edges-of-the-body.md
    - 3DEngine.ImageTests/BodyEdgesImageTests.cs
    - 3DEngine.Vulkan/EdgeLines.cs
    - 3DEngine.Vulkan/EdgePipeline.cs
  modify:
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Vulkan/RenderPipeline.cs
    - 3DEngine.Vulkan/RenderPasses.cs
    - 3DEngine.Vulkan/ShaderLibrary.cs
    - 3DEngine.Vulkan/OverlayRenderer.cs
    - 3DEngine.Vulkan/GpuMesh.cs
    - 3DEngine.Vulkan/Shaders/**
    - 3DEngine.ImageTests/MixImageTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - docs/adr/0029-selection-and-hide-gestures.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - docs/register.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0333 — The edges of the body as lines, the hidden edges dashed, and a transparent selection

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner accepted the proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` (each recommended
answer). This task is slice S2 (gap G2). In the renders Mix 1 to Mix 4 the body is transparent, its edges are bright
lines, the selected body has an orange outline, its hidden edges are dashed, and a soft shadow is on the ground under it.
The window at v0.311 draws a solid shaded body with no edge line. R18a (TASK-0332, the look) can run at the same time: it
changes the window, and this task changes the renderer.

## Goal

The body shows its edges as in the render, and the selected body shows its hidden edges.

## Scope (in)

1. ADR-0109: the source of the edges, the pass that draws them, and the rule of the hidden edges.
2. **The edges.** Each live, visible body draws its edges as lines of a fixed width in pixels. The source is a choice of
   ADR-0109: the edges of `GetFaces` (ADR-0077), which the client reads already, or the feature edges of the mesh, with an
   angle limit. A tessellation stays presentation (ADR-0007, determinism rule 6).
3. **The selection.** The selected body has a transparent fill, its visible edges in the colour of the selection, and its
   hidden edges dashed.
4. **The shadow.** A soft ellipse on the ground under the selected body, if the cost is low; else ADR-0109 defers it.
5. Tests: the edge pass draws the edges of a box (a count of edge pixels on a known line); a hidden edge of the selected
   body is dashed; the validation layer gives no message; the pictures of `MixImageTests` at 1440 by 900.

## Scope (out)

- The ground and the controls (R18a). The dimension lines of the values (R18c).
- A glow. It can come after R18a, when the ground is dark.
- Any change of the engine, of the contract or of the log.

## Acceptance criteria

- [x] ADR-0109 exists, with the source of the edges and its cost.
- [x] A test fails when the edge pass draws nothing.
- [x] The pictures at 1440 by 900 show the edges, and the dashed hidden edges of the selected body; the owner gets them
      beside the render.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds; the APK
      starts on the emulator.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge (Q7 A).

## Notes for the implementer

- A hidden edge needs a second draw with the depth test reversed, or a line pass with no depth test and a depth compare in
  the shader. Measure the time of a frame before and after on the bracket of `MixImageTests`.
- The phone uses the same renderer: keep the pass inside the features of the Vulkan device of the S23 Ultra (no wide
  lines; draw a line as two triangles if the device has no `wideLines`).
- Check the numbers at commit: TASK-0333 and ADR-0109 were free on each branch on 2026-11-10 at `main` 0125894.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list. Add a file to the write set when the work needs it, and say why in the progress.

## Outcome

Status: Done · commit (the merge of the branch r18b-edges-b) · v0.322

## Method

Mechanical: the line pass from the existing pipelines; the edges from the existing query.
Judgement:  the source of the edges; the dash of a hidden edge; the cost of the shadow.
Weakest:    no measure of the cost of the pass on the phone yet.

## Progress

- 2026-11-10: opened from the proposal, with the answers of the owner.
- 2026-11-10: started at main 09f1827 (v0.318), branch r18b-edges, after the order of the owner "Start R18a and R18b".
  ADR-0109 takes the feature edges of the triangles (limit 30 degrees) as the source, draws each edge as two triangles
  with a width in pixels, and defers the shadow. The write set adds `EdgeLines.cs` (the edges, with no GPU) and
  `EdgePipeline.cs` (the pass); `FirstDemonstrationImageTests.cs`, because the highlight is now a transparent fill and its
  test reads the orange of each pixel of the body in place of a change from the shaded body; and ADR-0029, which ADR-0109
  amends (item 2). `NativeThreeDEngine.cs`, `RenderPasses.cs`, `ShaderLibrary.cs`, `OverlayRenderer.cs` and
  `MixImageTests.cs` needed no change. Three injections failed the tests of `BodyEdgesImageTests`: an edge pass that draws
  nothing (both picture tests), solid hidden edges, and no hidden pass (the test of the selection). A frame of 1440 by 900
  took the same time before and after (median 22.2 ms with the copy to the CPU). The write set adds the file of TASK-0315,
  whose `SceneRenderer.cs` is in the affects of ADR-0109, so its `governed-by` names ADR-0109. The write set adds
  `docs/register.md` for R-0061: the cost of the lines on the phone is not measured.
- 2026-11-10: the merge of main at v0.321, which holds R18a, resolved four conflicts: the README of the ADRs keeps
  both rows, the register keeps R-0060 and R-0061, the floor of the image tests takes the tests of both slices (473,
  linux 503), and TASK-0315 names ADR-0108 and ADR-0109. The branch r18b-edges-b replaces the branch r18b-edges, whose
  merge commit had no line here and failed the write-set check; a force push is not permitted.
- 2026-11-10: run 42824773499 passed on the work commit, with the start of the APK on the emulator. The phone project
  builds with zero warnings. Closed at v0.322.
