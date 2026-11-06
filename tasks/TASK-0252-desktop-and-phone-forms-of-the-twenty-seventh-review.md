---
id: 0252
title: The cache of a face, the fixed controls, the list New, the edges and the safe area hold their next forms
status: Active
phase: P0.102
opened: 2026-11-06
depends-on: [0251]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081]
writes:
  create:
    - tasks/TASK-0252-desktop-and-phone-forms-of-the-twenty-seventh-review.md
  modify:
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/EdgeSelectionTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.Vulkan/Window.cs
    - docs/adr/0027-text-and-panels.md
    - docs/adr/0075-a-second-tap-selects-the-face.md
    - docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md
    - docs/adr/0080-the-fixed-controls-of-the-redesign.md
    - docs/android-test-sheet.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0252 — The cache of a face, the fixed controls, the list New, the edges and the safe area hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (e) found V113 to V118 (low). By `docs/templates.md`, section 7, they come after the
next product phase, and after P0.101.

## Goal

A fault clears the kept face, the panel does not cover the anchors, the title keeps the file name, a tap outside the
open list closes it, a tap near an edge outside the silhouette selects the edge, and the fixed controls stay inside the
safe area of the phone.

## Scope (in)

1. V113: the kept entry of a body with a fault.
2. V114: the panel and the anchors.
3. V115: the title bar and a long file name.
4. V116: the open list New and a tap on the scene.
5. V117: a tap near an edge outside the silhouette.
6. V118: the safe area of the phone.

## Scope (out)

- The findings of TASK-0250 and TASK-0251.

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Decisions

The standing order of 2026-11-05 gives the recommended option for each question.

- **V113.** `BodyActions.Forget`, which the host calls each frame with the live bodies, removes the kept entry of a body
  with a fault (ADR-0075 item 3).
- **V114.** A constraint ends the height of the panel above the anchors, and each frame keeps its place between the top
  of the safe area and the row of the anchors (ADR-0080 item 2). The panel still moves, because the proof of the focus
  click (FocusClickTests) drags it by its title.
- **V115.** The unit comes before the name of the file in the title (ADR-0080 item 1).
- **V116.** While a list is open, each press belongs to the panel: the library closes the list, and the tap does not
  reach the scene (ADR-0027 item 4).
- **V117.** The host asks for the edge before the pick (ADR-0077 item 3).
- **V118.** `Window.SafeInsets` reads `SDL_GetWindowSafeArea`, and the panel and the fixed controls start inside the safe
  area (ADR-0080 item 6). The ring of ADR-0081 keeps its corner; a later slice of the redesign moves it.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the six decisions above; no device ran the safe area, so step 11 of the test sheet of the phone holds it.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (e).
