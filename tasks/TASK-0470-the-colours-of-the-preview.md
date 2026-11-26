---
id: 0470
title: While a preview shows, the selected body has the accent of the theme, and the new shape in "After" has a faint amber fill
status: Done
phase: R33
opened: 2026-11-26
depends-on: [0462]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0072, 0075, 0077, 0078, 0101, 0109, 0120, 0125, 0129, 0184, 0186, 0187, 0188]
writes:
  create:
    - tasks/TASK-0470-the-colours-of-the-preview.md
  modify:
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.Vulkan/RenderPipeline.cs
    - 3DEngine.Vulkan/DrawListExecutor.cs
    - 3DEngine.Vulkan/Shaders/highlight.frag
    - 3DEngine.Vulkan/Shaders/highlight.frag.spv
    - 3DEngine.Vulkan/Shaders/shaders.lock
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
    - 3DEngine.Tests/PushConstantInterfaceTests.cs
    - eng/test-floor.txt
    - docs/adr/0187-the-preview-of-a-proposal-in-the-window.md
    - docs/adr/0109-the-edges-of-the-body.md
    - docs/adr/0125-each-dimension-says-what-it-measures.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0470 — While a preview shows, the selected body has the accent of the theme, and the new shape in "After" has a faint amber fill

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0187 (R25, TASK-0462) draws the new shape of a proposal in dashed amber lines (#ffa040). Two of its points of "Bad" were:

- The orange of the selected body (#ff8c1a, ADR-0109) is near the amber of a proposal. In "Both" the edges of the selected body
  and the new shape differ by the dashes only.
- A shape has edge lines only, with no fill, so a body behind the new shape shows through it in "After".

On 2026-11-25 the owner answered Q6 A and Q7 A (TASK-0464, ledger v0.448): while a preview shows, the outline of the selected
body takes a colour apart from the amber of a proposal, and the new shape in "After" gets a faint amber fill. Row R33 holds
both. The owner ordered "Start R33" on 2026-11-26.

At the start the task found two facts:

- The colour of the fill of the selected body was a constant of `highlight.frag`, so a new colour of the fill needs a change
  of the shader. The depth pipeline also used `highlight.frag`.
- ADR-0125 item 1 gives the dimensions of the selected body the colour of its edges in the scene. A change of the edges
  alone makes that item false while a preview shows.

## Goal

While a preview shows, the selected body has a colour that the eye does not take for the amber of a proposal, and in "After"
the new shape has a faint amber fill that does not hide its dashed lines.

## Scope (in)

1. The colour of the selection while a preview shows: the accent of the theme (`Theme.Accent`, #30eaff) for the visible and
   hidden edges, the fill at alpha 0.25 and the dimensions of the selected body. No new colour.
2. The fill of each shape in "After" at alpha 0.15, in the amber of the lines, drawn after the lines: the depth of the shapes,
   then one tint of each pixel where a shape is nearest. "Before" and "Both" have no fill.
3. `highlight.frag` reads its colour from a push constant at offset 128; `RenderPipeline.Tint` and an optional tint of
   `DrawListExecutor.Execute` give it. The depth pipeline takes `mesh.frag`. The SPIR-V and the lock come from `compile.js`.
4. ADR-0187 item 9, item 5 and "Bad"; ADR-0109 item 3 and ADR-0125 item 1 get the change, with a History line in each.
   The index of the ADRs (`docs/adr/README.md`) gives the new links "Amends" and "Amended by".
5. The tests, the test floor, the roadmap row and the ledger entry v0.453.

## Scope (out)

- The colour of the selection with no preview. It stays the orange of ADR-0109, and a picture with no preview is the same
  byte for byte.
- The other uses of the orange and of the amber in the panels (the arc of a proposal, a pending value).
- A depth bias for the fill. A face of a present body in the plane of a face of a new shape takes the fill in parts only, as
  the fill of a selected body does today (ADR-0187 "Bad").
- A light theme. The scene has one dark ground (ADR-0108, ADR-0186). The light paper is for the sheet page only, and the sheet
  page shows no preview (ADR-0187 item 7).

## Acceptance criteria

- [x] `While_A_Preview_Shows_The_Selection_Has_The_Accent_And_After_Has_A_Faint_Amber_Fill` passes: with no preview the
  selected bracket has orange pixels and no accent pixels; while a preview shows it has accent pixels and no orange pixels;
  in "After" each free point of the top face of the new shape is the fill over the ground within 3 for each channel; in
  "Both" the same points are as in "Before"; about half of the points along the edges of the new shape stay amber.
- [x] `While_A_Preview_Shows_The_Selected_Body_Has_The_Colour_Of_Its_Edges_Then` passes: the dimensions have the colour of the
  edges, and no vertex of the frame has the orange of the selection while a preview shows.
- [x] `The_Tint_Of_The_Highlight_Is_Four_Floats_After_The_Two_Matrices` passes.
- [x] Each injection of the old behaviour fails its test: the orange while a preview shows, no fill in "After", a fill in
  "Both", the dimensions in the orange, and a tint at another offset. An opaque fill fails the test of the dashed lines.
- [x] A picture of the selected body with no preview is the same byte for byte before and after the change.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- The fill comes after the lines and has their colour. So a pixel of a line under the fill keeps its colour, and the soft
  border of a line moves a little to the amber (15 of 255 at most in the pictures).
- The highlight pipeline has two push constant ranges: the vertex range of the two matrices, and the fragment range of the
  tint. A pipeline with no tint throws in `Tint`.

## Outcome

Status: Done · commit (the merge of the branch r33-preview-colours) · v0.453

## Method

Mechanical: the push constant of the tint, the shader and its lock, the colours of the draw, the tests and the records.
Judgement:  the accent, because it is the colour of the theme that is farthest from the amber and already marks a tool in use
            and a selected view; the fill after the lines in their colour, so that it cannot hide them; the dimensions follow
            the edges, so that ADR-0125 item 1 stays true; alpha 0.15 for "faint", below the 0.25 of a selected body.
Weakest:    the alpha 0.15 is a choice of the session from the pictures, and no person saw it in the window. The test of the
            fill fixes the value, so a change of the value changes the test.

## Progress

- 2026-11-26: opened on the order of the owner "Start R33", branch `r33-preview-colours` from `origin/main` at `64b1c07f`
  (v0.452).
- 2026-11-26: run 45891292110 of `e8258473` passed on each job. Closed at v0.453.
