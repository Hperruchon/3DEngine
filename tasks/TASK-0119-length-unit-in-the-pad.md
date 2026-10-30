---
id: 0119
title: The numeric pad shows the length unit of the document
status: Active
phase: P8e
opened: 2026-10-30
depends-on: [0091, 0118]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0119-length-unit-in-the-pad.md
  modify:
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/DesktopUiTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/SketchGapUiTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0119 — The numeric pad shows the length unit of the document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

P8a (TASK-0091) put the length unit of the document in the header of its file (KD4, ADR-0015 item 7). The session of
the host has the unit of the header, but the pad of the desktop shows the constant `mm` (ADR-0032 item 7). The
roadmap row P8e plans this change.

## Goal

Each pad of the desktop shows the length unit of the session of its host.

## Scope (in)

1. `DesktopUi` takes the length unit in its constructor, and each pad shows that unit.
2. The desktop host gives the unit of its session.
3. ADR-0032 item 7 names the unit of the document, not the text `mm`.

## Scope (out)

- A second length unit. The header permits the millimetre only (ADR-0015 item 7).
- A conversion of a number.
- The grid of the sketch, whose steps are in the unit of the document already.

## Acceptance criteria

- [ ] A test opens two pads in a panel with a unit that is not `mm`, and each pad shows that unit.
- [ ] An injection that puts back the constant `mm` fails the test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: the unit is a parameter of the constructor of `DesktopUi`; 1 new test case; the injection of the
  constant `mm` failed it.
