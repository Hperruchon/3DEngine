---
id: 0370
title: A button "Views" opens a net of the six flat views and the 3D view, and the keys 1, 3, 7 and 0 give them
status: Done
phase: R21b
opened: 2026-11-14
depends-on: [0369]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0135, 0137, 0138]
writes:
  create:
    - tasks/TASK-0370-the-controls-of-the-flat-views.md
    - docs/adr/0138-the-controls-of-the-flat-views.md
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.ImageTests/FlatViewControlsTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Core/Input/InputSamples.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/adr/0100-a-path-line-from-the-document-to-the-selection.md
    - docs/adr/0127-the-fixed-controls-and-the-status-as-in-the-render.md
    - docs/adr/0137-the-flat-views-of-the-camera.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0370 — A button "Views" opens a net of the six flat views and the 3D view, and the keys 1, 3, 7 and 0 give them

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R21a (ADR-0137) gave the camera six flat views and the command line their commands. Row R21b gives their controls and their
keys. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

Each flat view and the 3D view have a control at each size and on each device, and a key on a keyboard.

## Scope (in)

1. ADR-0138, which amends ADR-0100 item 1, ADR-0127 item 4 and ADR-0137 item 5.
2. The net of the views, the button "Views" on each orientation, and the keys.
3. Tests of the button, the net, the keys and the mapping of the digits; pictures of the net open.

## Scope (out)

- Frame in the free part of the scene (R21c).

## Acceptance criteria

- [x] ADR-0138 exists. ADR-0100, ADR-0127 and ADR-0137 show the amendment.
- [x] A test proves at five sizes that "Views" opens the net inside the display, on no other fixed control, and that a view
      gives its action and closes the net.
- [x] A test proves the keys and the guard of the home screen; a test proves the mapping of the digits by the place of the
      key.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r21b-view-controls) · v0.355

## Method

Mechanical: the cells of the net; the place of the button on each orientation; the keys.
Judgement:  the net behind a button, so a view takes two taps; the look of the cells.
Weakest:    no person used the net; a view takes two taps, not one.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R21a, branch r21b-view-controls. TASK-0370 and
  ADR-0138 are free. A first form drew the net all the time; the layout test found it on the arc of a proposal and on the
  tools of the ring at 1024 by 768 and 640 by 360, and a button "Views" past the right edge of the row of the anchors of a
  phone. So "Views" opens the net, after Redo on a landscape display and at the upper right on a portrait display. The path
  line of a portrait display then lay on the button, so it ends left of it. The first cells of 60 pixels cut "Front" and
  "Bottom", and the net of a phone passed the path line and the left edge; the cells now take the widest word, at most a
  quarter of the safe width, and the net of a phone is under the path line. A rule that kept "AGENT IS WAITING" left of the
  button was caught by no injection, because the word goes under the path line on each tested phone, so it went. Seven
  injections each failed a named test. Each file came back byte for byte.
- 2026-11-14: run 43562594395 passed on the work commit. The pictures show the net open over the front view at 1440 by
  900 and on a phone. Closed at v0.355.
