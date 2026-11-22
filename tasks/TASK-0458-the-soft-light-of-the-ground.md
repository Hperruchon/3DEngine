---
id: 0458
title: The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners
status: Active
phase: R18p
opened: 2026-11-20
depends-on: [0332, 0333, 0457]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0072, 0078, 0101, 0108, 0109, 0122, 0129, 0166, 0184, 0186]
writes:
  create:
    - tasks/TASK-0458-the-soft-light-of-the-ground.md
    - docs/adr/0186-the-soft-light-of-the-ground.md
    - 3DEngine.Vulkan/GroundPipeline.cs
    - 3DEngine.Vulkan/Shaders/ground.vert
    - 3DEngine.Vulkan/Shaders/ground.frag
    - 3DEngine.Vulkan/Shaders/ground.vert.spv
    - 3DEngine.Vulkan/Shaders/ground.frag.spv
    - 3DEngine.ImageTests/GroundImageTests.cs
    - 3DEngine.ImageTests/GroundPixels.cs
  modify:
    - 3DEngine.Vulkan/Shaders/shaders.lock
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/Ui/Theme.cs
    - 3DEngine.ImageTests/ThemeTests.cs
    - 3DEngine.ImageTests/BodyFocusTests.cs
    - 3DEngine.ImageTests/BodyRowsImageTests.cs
    - 3DEngine.ImageTests/EdgeValueImageTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/FixedStatusImageTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
    - 3DEngine.ImageTests/RingImageTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
    - docs/adr/0108-the-look-of-the-render.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Android/**
---

# TASK-0458 — The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0108 item 1 (R18a) gave the scene one almost black colour, `#070d14`, and kept the soft light at the middle of the render
"mix" for later. No later row held it. On 2026-11-20 the owner asked if the window has the gradient of the render, and then
said "Yes, add it". Row R18p holds this slice, and ADR-0186 records it.

## Goal

The ground of the scene goes from a dark colour at the corners to a lighter colour at the middle, as in the render, and each
body lies over it. A person sees it in the window and on the phone.

## Scope (in)

1. The colour of the ground at each pixel, on the GPU and on the CPU for a test (ADR-0186 item 1).
2. A pipeline of the ground, the first draw of the render pass of the scene (item 2).
3. Each image test that compared a pixel with the one colour of the clear compares it with the ground at that pixel (item 4).

## Scope (out)

- A dither of the steps of 8 bits, and a light at the middle of the free part of the scene.
- The soft shadow under the selected body (ADR-0109 item 6).
- A change of `Engine.Core/**`, of `Engine.Contracts/**` or of the phone project.

## Acceptance criteria

- [ ] With no body, each pixel of a picture at 1280 by 720 and at 360 by 780 is `Theme.GroundAt` at that pixel, within 1.
- [ ] A body at the middle lies over the light, and a corner stays the ground.
- [ ] Each point of the ground is almost black, and each colour keeps its contrast on the middle of the ground.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active · commit (the merge of the branch r18p-ground-light) · v0.442

## Method

- Read ADR-0108 (item 1 and its context), ADR-0109 and ADR-0022 (the lock of the shaders) before the code.
- Compiled the shaders with `node 3DEngine.Vulkan/Shaders/compile.js` (shaderc v2023.8, the compiler of the lock). The SPIR-V
  of each other shader did not change.
- Ran `GroundImageTests` and `ThemeTests` after each change, then six injections with `inject.js` in the scratchpad; each file
  came back byte for byte (sha256).

## Progress

- 2026-11-20: opened under the words of the owner "Yes, add it", branch ground-light from `main` at `d7936b4`. P0.167
  (TASK-0416) held v0.405 on its branch, so this slice took TASK-0422, ADR-0177 and v0.406.
- 2026-11-20: the work: the colour, the pipeline, three tests in `GroundImageTests`, and eleven files of image tests that
  read the ground at each pixel. Six injections each made a named test fail: no draw of the ground, the ground after the bodies, a
  computation on the CPU without the height, a middle that is not almost black, the two colours swapped in the push block, and
  the old comparison with the one clear colour (nine tests fail).
- 2026-11-20: run 44813962866 of `1c82d1b` passed on each job. P0.167 came to `main` at v0.405 (`8dbaafb`), so the branch
  merges `main`. The one conflict was the count of `3DEngine.ImageTests` in `eng/test-floor.txt`: 809, and 856 on Linux.
- 2026-11-20: run 44815558882 of the close commit `0e0b1d6` passed on each job. Closed at v0.406.
- 2026-11-20: the owner ordered "Merge". Before the push, `main` took TASK-0422, ADR-0177 and v0.406 for R30f (the merge
  `05aafda` of the branch r30f-sheet-proposal), so nothing was pushed. The task moves to TASK-0423, ADR-0178 and v0.407 on
  the branch ground-light-b from `main` at `05aafda`. Each code file is the file of `0e0b1d6` with the new numbers. The
  branch ground-light stays unmerged.
- 2026-11-22: the owner ordered "Finish R18p". The close of TASK-0423 waited for a codebase review that the gate asked for,
  and in that time P0.170 took ADR-0178 and v0.407, and R20d took TASK-0423, on `main`. The commit `75b30af` of the branch
  ground-light-b has no ledger entry. So the task moves to TASK-0458, ADR-0186 and v0.442 on the branch r18p-ground-light from
  `main` at `90097dd4` (v0.441), by a cherry-pick of `75b30af` with the new numbers. Three conflicts: `SceneRenderer.cs` keeps
  the fields of the section view (ADR-0184) and adds the pipeline of the ground; the ground stays the first draw after the
  begin of the render pass, before the bodies, the caps and the hatch of a section; `docs/adr/README.md` keeps the rows 0178
  to 0185 of `main` and adds row 0186; `eng/test-floor.txt` is 909, and 960 on Linux. The new comments of `ground.vert` and
  `ground.frag` changed the hash of their source, so `compile.js` (shaderc v2023.8) wrote the lock again; each SPIR-V file is
  the same. No image test that `main` added after `05aafda` reads the ground: the one test that reads pixels,
  `SectionViewTests`, counts the accent colour of the hatch. ADR-0184 joins governed-by. Three injections in `SceneRenderer.cs`
  ran again, and each file came back byte for byte (sha256):

  | Injection | The tests of `GroundImageTests` that fail |
  |---|---|
  | No draw of the ground | `An_Empty_Scene_Shows_The_Soft_Light_Of_The_Ground`, `A_Body_At_The_Middle_Lies_Over_The_Light` |
  | The ground after the bodies | `A_Body_At_The_Middle_Lies_Over_The_Light` |
  | The two colours swapped in the push block | `An_Empty_Scene_Shows_The_Soft_Light_Of_The_Ground`, `A_Body_At_The_Middle_Lies_Over_The_Light` |

  With no injection the three tests pass. The branch ground-light-b stays unmerged.
