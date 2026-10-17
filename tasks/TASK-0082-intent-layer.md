---
id: 0082
title: Touch and mouse give the same intents, and a parity test holds it
status: Ready
phase: R7
opened: 2026-10-17
depends-on: [0078, 0081]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024]
writes:
  create:
    - tasks/TASK-0082-intent-layer.md
    - docs/adr/0026-intent-layer.md
  modify:
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - docs/adr/README.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/**
---

# TASK-0082 — Touch and mouse give the same intents, and a parity test holds it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R7: "Device-tagged pointer samples. The intent layer and the parity test." It serves objective
9 of `docs/CHARTER.md`: each operation works by touch, and also by mouse and keyboard. Today the desktop reads
a left drag and the wheel from SDL events in `3DEngine/NativeThreeDEngine.cs`. The codebase review of
2026-10-17, sections 5 and 6, gives the research. On 2026-10-17 the owner decided (question Q2):

1. (D1) Synthetic scripts of samples only; the touch path stays unverified until a check on a touch screen.
2. (D2) A pen takes the path of one finger; the record of a sample keeps the kind Pen.
3. (D3) The samples, the intent state machine and the intents live in a folder of `3DEngine.Core`; the
   translation of SDL events into samples lives in `3DEngine.Vulkan`.
4. (D4) The default map: one finger or a left drag orbits; two fingers, a middle drag, or Shift and a left
   drag pan; a pinch or the wheel zooms; a tap or a click selects. The action of each gesture is a setting of
   the client (ADR-0007 permits a user setting).
5. (D5) The parity test: for each kind of intent, a touch script and a mouse script give the same intents,
   and a check fails when a kind of intent has no touch script.
6. (D6) The keyboard alone moves the camera: orbit, pan and zoom.
7. (D8) The hint `SDL_HINT_TOUCH_MOUSE_EVENTS` is off, and the translator drops each mouse event with
   `which == SDL_TOUCH_MOUSEID`.

On question Q3 the owner decided that the selection is a state of the client (ADR-0007). On question Q4: 5
to 7 evenings. TASK-0081 moves to SDL 3.2 or later first (D7).

## Goal

The camera of the desktop moves from intents, and a test with no window and no GPU proves that touch, mouse
and keyboard give the same intents.

## Scope (in)

1. An ADR records D1 to D8, the record of a sample, the kinds of intent, the setting of the map, and where the
   code lives. It amends ADR-0023 item 2 (the reading of the mouse in the host).
2. `3DEngine.Core`: the record of a sample, the frame of samples, the pure state machine from samples to
   intents, the intents (orbit, pan, zoom, select, cancel), the settings of the map with the default of D4,
   and `Camera.Pan`.
3. `3DEngine.Vulkan`: the translator from SDL events to samples, with D8.
4. `3DEngine`: the host collects the samples of each frame, runs the state machine, and applies the intents
   to the camera. The selection is kept by the client and draws nothing yet.
5. `3DEngine.Tests`: the parity test and the coverage check of D5, a test of the double count of D8, a test
   of a lost release (cancel), and the keyboard camera of D6.

## Acceptance criteria

- [ ] For each kind of intent, a touch script and a mouse script give the same intents; the coverage check
      fails on an injected kind of intent with no touch script.
- [ ] A finger sequence with its synthetic mouse events gives one orbit, not two.
- [ ] A changed setting of the map moves an action to another gesture, and a test holds it.
- [ ] A picture of the window after an orbit, a pan and a zoom by mouse and by keyboard. The owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **Touch.** No computer of the project has a touch screen (D1). Report the touch path as unverified, and add
  a register entry with a date for a check on a touch screen; the register holds 15 of 15 entries, so the
  task first closes one, or asks the owner.
- **The size.** About 5 to 7 evenings.
