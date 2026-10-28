---
id: 0086
title: The desktop selects, hides, shows and renames a body, and the camera stays where it is
status: Done
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

- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A touch script and a mouse script give the same command log.
- [x] A picture of the desktop with a selected body and its highlight, a renamed body, and a hidden body. The
      owner accepts it. The owner accepted four pictures and the log on 2026-10-28.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.88 · the commit that carries this block.

A person selects a body by a tap or a click, and a highlight shows it. A long press, a right click or H hides
a body; a tap of two fingers or Shift and H shows each body. The panel renames the selected body. Each change
is a command of the session, and the camera frames the bodies only when the first visible body comes.

## Method

**Mechanical.** 30 new tests: 14 in `3DEngine.Tests` (six of the gestures, eight of the pick) and 16 in
`3DEngine.ImageTests`. Twelve injections each failed their tests, four for each of the three code commits. A
touch script and a mouse script give the same command log. The visual check ran under synchronization
validation with no validation message, and the owner accepted its four pictures.

**Judgement.** The selection stays in `BodyActions`, a state of the client (ADR-0007), and only the hide, the
show and the rename go to the session. A hidden body leaves the scene, the pick and the framed box, and it
cannot stay selected. The pick computes in double precision on the render side, where a transcendental
function is permitted, because no pick writes to the log. A hide at a point hides the body at that point, not
the selection.

**Weakest.** No test with no GPU reads `SessionBodies`; the visual check reads it. No computer of the project
has a touch screen, so the touch path is proven by synthetic samples only. A rename sends one command for each
press of Rename, not for each key, so ADR-0028 item 2 does not fill the log from the desktop.

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
- 2026-10-28: the pick (ADR-0029 items 1 and 3). `3DEngine.Core/Picking/Picker.cs` gives the ray of a pixel
  and the nearest visible body that it hits (Möller and Trumbore, in double precision). Eight tests; a point that
  the camera projects lies on the ray of its pixel, within 1e-4. Four injections each failed (y not flipped, a
  hidden body picked, the first hit and not the nearest, a hit behind the origin).
- 2026-10-28: the desktop (ADR-0029 items 1 to 3 and 6 to 8). `BodyActions` keeps the selection and sends
  `SetBodyVisibility` and `RenameBody` to the session; `SessionBodies` reads `body.changed`, keeps the triangles
  of each body for the pick, and keeps a hidden body out of the scene and out of the framed box; the highlight
  pipeline (`highlight.frag`) draws the selected body again; the camera frames only when the first visible body
  comes; the panel rows select, and the selected body has a name field, Rename, Hide or Show, beside Show all.
  16 new tests, with the comparison of the logs of a touch script and a mouse script. Four injections each
  failed their tests (a hide at a point that hides the selection, a frame at each change, a row with no action,
  a hidden body that stays selected). No test with no GPU reads `SessionBodies`; the visual check reads it.
- 2026-10-28: closed after the acceptance of the owner, a clean build and a full local run of 636 tests (507 in
  `Engine.Tests`, 89 in `3DEngine.Tests`, 40 in `3DEngine.ImageTests`).
