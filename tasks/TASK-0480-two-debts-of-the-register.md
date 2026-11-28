---
id: 0480
title: Two debts of the register, the kept solids of a fork and two starts of a renderer that leak
status: Active
phase: P0.194
opened: 2026-11-28
depends-on: [0477]
governed-by: [0004, 0017, 0023, 0025, 0027, 0043, 0054, 0058, 0059, 0076, 0083, 0089, 0112, 0143, 0180, 0181, 0185, 0191]
writes:
  create:
    - tasks/TASK-0480-two-debts-of-the-register.md
  modify:
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Tests/Features/KeptBooleanTests.cs
    - 3DEngine.Vulkan/ImageRenderer.cs
    - 3DEngine.Vulkan/Ui/UiRenderer.cs
    - 3DEngine.ImageTests/PipelineStartTests.cs
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0480 — Two debts of the register, the kept solids of a fork and two starts of a renderer that leak

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

After P0.193 the next rows of the roadmap need the owner: R34 and R35 wait for a check on the phone (the review of 2026-11-28,
section 4), and P11a waits for the answers to its proposal (TASK-0479). Two debts of the register have an exit that a task does
with no decision: R-0082 (finding E243, the kept solids of a fork) and R-0077 (two starts of a renderer that leak at a failure).
The standing order of 2026-11-28 runs until 09:00 on 2026-11-29, so this task does them.

## Goal

A fork pushes out no kept solid of its source, and the starts of `ImageRenderer` and `UiRenderer` free each object at a failure.

## Scope (in)

1. R-0082 (E243): a fork reads the kept solids of its source by their keys and keeps its own solids in a store that `EndFork`
   removes, with the probe of the review as a test.
2. R-0077: `ImageRenderer` and `UiRenderer` take the form of TASK-0461, with a test that fails each start at each object.

## Scope (out)

- Each other entry of the register.

## Acceptance criteria

- [x] Each correction has a test that fails when the correction is undone.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0194-register-debts) · v0.463

## Method

Mechanical: the corrections that the exits of R-0082 and R-0077 name.
Judgement:  a fork reads a solid of its source under the same key and placement, because a fork replays the log of its source and
            a committed key names one solid (ADR-0059 item 2); a read of the mass, not of the faces, fills the store in the test,
            because `GetFaces` builds its solid with no store.
Weakest:    the test of the renderers counts the messages of the validation layer, so it asks that the layer is on.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.193. E243: `KernelProjection.OfRecord` of a fork reads its
  own store, then the store of its source, and puts its solids in its own store, at most 64; `A_Fork_Pushes_Out_No_Kept_Solid_Of_Its_Source`
  fails when the fork puts them in the store of the Documents. R-0077: the starts of `ImageRenderer` and `UiRenderer` have a hook
  of a test at each object and free each object at a failure;
  `A_Failed_Start_Of_A_Renderer_Of_The_Images_Or_Of_The_Interface_Frees_Each_Object_That_It_Made` fails with 27 and 16 messages of
  the layer when the start frees nothing. R-0082 and R-0077 close. Ledger v0.463.
