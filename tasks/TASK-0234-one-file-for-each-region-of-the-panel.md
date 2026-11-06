---
id: 0234
title: The user interface of the desktop is one partial file for each region
status: Active
phase: R15
opened: 2026-11-06
depends-on: [0232]
governed-by: [0007, 0019, 0027, 0029, 0030, 0032, 0034, 0072, 0073]
writes:
  create:
    - tasks/TASK-0234-one-file-for-each-region-of-the-panel.md
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.App/Ui/DesktopUi.Image.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - docs/adr/0073-the-phone-cuts-and-keeps-its-document.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0234 — The user interface of the desktop is one partial file for each region

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`3DEngine.App/Ui/DesktopUi.cs` held 1365 lines: the labels, the state, the frame, the panel, the overlays, the window
of the sketch, the window of the images and the pad. The fallback queue of the owner (2026-11-05) starts with its split
into one file for each region, with no visual change. The redesign of the desktop (R14g and the UX brief of 2026-11-04)
changes the regions one at a time, so one file for each region keeps each change small.

## Goal

`DesktopUi` is a partial class: the main file keeps the labels, the state, the constructor, the frame and the routing,
and each region has its own file. No line of code changes.

## Scope (in)

1. `DesktopUi.Panel.cs`: the panel of the bodies, the controls of the selected body and the line of the document.
2. `DesktopUi.Overlay.cs`: the overlays of the sketches and of the dimensions, with their colours and sizes.
3. `DesktopUi.Sketch.cs`: the window of the sketch.
4. `DesktopUi.Image.cs`: the window of the images.
5. `DesktopUi.Pad.cs`: the numeric pad.
6. Each part keeps only the usings that it needs.
7. The affects field of ADR-0073 names `DesktopUi.Panel.cs`.

## Scope (out)

- A change of a label, of a layout or of a behaviour.
- A split of the labels or of the state of the class.

## Acceptance criteria

- [ ] A script shows that each line of code of the old file is in exactly one new file, and that the new files add only
  their usings, their class lines, their braces and their header comments.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the tests pass with no change.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a script cuts the class into its members by the depth of the braces and moves each member of a list to its
            file; a second script removes each using whose removal still builds with no warning.
Judgement:  the regions, and the labels and the state stay in the main file.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the split, the check of the lines, and the removal of the usings that a part does not need.
