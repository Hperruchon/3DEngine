---
id: 0246
title: The desktop and the phone take the theme "Proposed (harmonized)" of the redesign
status: Active
phase: R16a
opened: 2026-11-06
depends-on: [0245]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0072, 0073, 0075, 0078]
writes:
  create:
    - tasks/TASK-0246-the-theme-of-the-redesign.md
    - docs/adr/0078-the-theme-of-the-redesign.md
    - 3DEngine.App/Ui/Theme.cs
    - 3DEngine.ImageTests/ThemeTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/CurveImageTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0246 — The desktop and the phone take the theme "Proposed (harmonized)" of the redesign

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the redesign of the desktop on 2026-11-06 (evening), from the designer brief of 2026-11-05. Its first
slice is the theme "Proposed (harmonized)" and the fonts Instrument Sans and JetBrains Mono, with no change of layout.
The design system of the brief gives the theme as tokens. The fonts need a download, which waits for the owner.

## Goal

The panels, the clear of the scene and the marks of the viewport take the theme of the redesign, and each mark lies over
a casing; no control changes its size or its place.

## Scope (in)

1. ADR-0078 items 1 and 2.
2. `Theme` with the tokens that differ from the dark style; the panel applies them.
3. The clear of the scene, the three colours of the marks, and the casing.
4. The image tests that read the clear or the colour of a snap.

## Scope (out)

- The fonts and the amendment of ADR-0027 item 6: the next slice, after the download.
- Each change of layout.

## Acceptance criteria

- [ ] The panel takes the colours of the theme and keeps its sizes.
- [ ] A line of a sketch lies over a casing.
- [ ] The image tests pass with the new clear, and the pictures are kept for the owner.
- [ ] An injection of each item fails a test.

## Outcome

Status: Active

## Method

Mechanical: the tokens of the theme `proposed` of `tokens.json`, as they differ from the first theme.
Judgement:  which marks take a casing: each line, point, ring and square of the viewport, and no label.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the theme, the casing and two tests; an injection that leaves the theme out and one that leaves the casing
  out each fail a test. Four image tests read the new clear or the new colour of a snap. The pictures of the image tests
  before and after the change are kept for the owner.
