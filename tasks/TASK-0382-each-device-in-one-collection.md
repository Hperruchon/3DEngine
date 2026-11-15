---
id: 0382
title: Each class of the image tests that creates a Vulkan device runs in one collection
status: Done
phase: P0.153
opened: 2026-11-14
depends-on: [0380]
governed-by: [0025, 0109, 0121, 0129]
writes:
  create:
    - tasks/TASK-0382-each-device-in-one-collection.md
    - 3DEngine.ImageTests/DeviceCollectionTests.cs
  modify:
    - 3DEngine.ImageTests/DearImGui.cs
    - 3DEngine.ImageTests/BodyEdgesImageTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/GizmoImageTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
    - 3DEngine.ImageTests/RebuildImageTests.cs
    - 3DEngine.ImageTests/TwentySecondReviewDesktopTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0382 — Each class of the image tests that creates a Vulkan device runs in one collection

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R-0062 (due 2026-12-10): on the computer of the owner a run of `3DEngine.ImageTests` stops at times after about 145 tests,
with an access violation in `vkCreateDevice`, while several classes create a Vulkan device at the same time. On 2026-11-14
a run stopped after 131 tests (TASK-0377). Fifteen classes that create a device are in the collection of Dear ImGui, which
xunit runs one test after the other; seven are in no collection, so they run at the same time as each other and as that
collection. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

No two Vulkan devices are created at the same time in the image tests, and ten runs end with no stop.

## Scope (in)

1. The seven classes go into the collection of Dear ImGui, which says why.
2. A test that holds the rule for each file that creates a device.
3. Ten local runs of `3DEngine.ImageTests`, and R-0062 closed by its exit.

## Scope (out)

- A change of the driver, or of `GraphicsDevice`.

## Acceptance criteria

- [x] A test proves that each file that creates a device puts its class in the collection.
- [x] Ten runs of `3DEngine.ImageTests` in a row end with each test passed and no stop.
- [x] R-0062 is closed with its exit.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0153-device-collection) · v0.367

## Method

Mechanical: the attribute on seven classes; the test of the source files.
Judgement:  one collection for the devices and Dear ImGui, in place of a second collection that runs at the same time.
Weakest:    ten runs prove a low rate, not the cause in the driver.

## Progress

- 2026-11-14: opened after P0.152, branch p0153-device-collection. A second collection for the devices would run at the
  same time as the collection of Dear ImGui, whose classes create devices too, so the seven classes join that collection.
  The first version of the test found its own text "new GraphicsDevice(", so the test builds that text in two parts. One
  injection failed the test; the file came back byte for byte.
- 2026-11-14: ten runs of 3DEngine.ImageTests in a row ran 706 tests each, each passed, with no stop. Run 43577525046 passed on
  the work commit. Closed at v0.367.
