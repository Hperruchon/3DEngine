---
id: 0086
title: The desktop selects, hides, shows and renames a body, and the camera stays where it is
status: Active
phase: R8b
opened: 2026-10-28
depends-on: [0085]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024, 0025, 0026, 0027, 0029]
writes:
  create:
    - tasks/TASK-0086-selection-and-visibility-in-the-desktop.md
    - docs/adr/0029-selection-and-hide-gestures.md
  modify:
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0025-observe-a-cut.md
    - docs/adr/0026-intent-layer.md
    - docs/adr/0027-text-and-panels.md
    - docs/adr/README.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0086 — The desktop selects, hides, shows and renames a body, and the camera stays where it is

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R8b, the third phase of the path to "CAD on one part" (rule 4 of the order of the roadmap).
R8a (TASK-0085, ADR-0028) gave each body a name and a visibility in the engine. The owner decided the defaults
R8-Q6 to R8-Q8 and decision KD6 on 2026-10-27 (`docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`).
This phase changes no shape of `Engine.Contracts`.

## Goal

A person selects, hides, shows and renames a body in the desktop by touch or by mouse and keyboard, each change
is a command of the session, and the camera frames the bodies only when the first one comes.

## Scope (in)

1. ADR-0029 records the decision. It amends ADR-0025 item 6, ADR-0026 items 2 and 3, and ADR-0027 item 7.
2. `3DEngine.Core/Input/`: the gestures long press, right click, H, two-finger tap and Shift and H; the actions
   hide and show all; the intents `HideIntent` and `ShowAllIntent`; their settings and their touch scripts.
3. `3DEngine.Core/Picking/`: the ray of a pixel and the nearest visible body that it hits.
4. `3DEngine/`: the selection, the commands of a hide, a show all and a rename, the highlight pipeline, no draw
   and no pick of a hidden body, the frame of KD6, and the rows and the controls of the panel.
5. Tests: the gestures and the parity, the pick, a log comparison of a touch script and a mouse script, the
   rename through the panel, and the frame of KD6.

## Scope (out)

- More than one selected body, and a selection of a face or an edge (R11).
- The gizmo (R9), after the milestone.

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A touch script and a mouse script give the same command log.
- [ ] A picture of the desktop with a selected body and its highlight, a renamed body, and a hidden body. The
      owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **Touch.** No computer of the project has a touch screen; the touch path is proven by synthetic samples.
- **The size.** About 4 to 5 evenings.

## Progress

- 2026-10-28: opened after TASK-0085 (v0.87). The first commit holds ADR-0029 and the text changes of the
  three ADRs that it amends.
- 2026-10-28: the gestures of ADR-0029 item 4. A long press (500 ms by default, decided at the release), a
  right click, a tap of two fingers, H, and Shift and H, with the actions hide and show all and the intents
  `HideIntent` and `ShowAllIntent`. A press keeps its largest displacement, because `Hold` marks each pair as
  moved. The coverage check of the parity test failed on the two new kinds before their scripts existed. Six new
  tests; four injections each failed one (no long press, no tap of two fingers, a right click as a click, Shift
  ignored on H).
