---
id: 0082
title: Touch and mouse give the same intents, and a parity test holds it
status: Done
phase: R7
opened: 2026-10-17
depends-on: [0078, 0081]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024, 0026]
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
    - docs/adr/0023-camera-depth-and-draw-list.md
    - eng/test-floor.txt
    - tasks/TASK-0079-host-and-render-gaps-of-the-ninth-review.md
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
    - eng/write-set-check/**
    - eng/repo-gates/**
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

- [x] For each kind of intent, a touch script and a mouse script give the same intents; the coverage check
      fails on an injected kind of intent with no touch script.
- [x] A finger sequence with its synthetic mouse events gives one orbit, not two.
- [x] A changed setting of the map moves an action to another gesture, and a test holds it.
- [x] A picture of the window after an orbit, a pan and a zoom by mouse and by keyboard. The owner accepts it
      (2026-10-27).
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.83 · the commit that carries this block.

The camera of the desktop moves from intents. `3DEngine.Core/Input/` turns the samples of touch, pen, mouse
and keyboard into orbit, pan, zoom, select and cancel, from a map of gestures that is a setting of the client.
`SdlInput` translates the events of SDL and drops the synthetic mouse events of a touch. A test with no window
and no GPU proves that touch and mouse give the same intents, and that each kind of intent has a touch path.

## Method

**Mechanical.** Sixteen new tests. Four injections each failed a test: a kind of intent with no scripts, a
one-finger drag mapped to pan, an inverted pinch, and a translation that kept the synthetic mouse events. Run
40579390160 passed on the three runners. The owner accepted three pictures of the window: after the
demonstration, after a left drag, a middle drag and two steps of the wheel, and after the arrow keys, Shift and
the arrow keys, and the plus key of the pad.

**Judgement.** A pair of contacts moves once for each frame: each finger has its own samples, and a pair that
moved one finger at a time gave a false pinch in a pan. A gesture of motion gives orbit, pan or zoom, a gesture
of scale gives zoom, and a gesture of a point gives select, so the settings refuse a map that a gesture cannot
give. The translation keeps Shift from the key events and makes no call of SDL, so a test gives it events that
it builds.

**Weakest.** The touch path is unverified on a real screen: no computer of the project has one, and the scripts
are synthetic (decision D1). On 2026-10-27 the owner chose that the ledger and this task hold that, and that the
next codebase review carries it, because the register is at its limit. The selection draws nothing yet. The
intents write no command, so the comparison of logs of anti-objective 7 starts with R8.

## Notes for the implementer

- **Touch.** No computer of the project has a touch screen (D1). Report the touch path as unverified, and add
  a register entry with a date for a check on a touch screen; the register holds 15 of 15 entries, so the
  task first closes one, or asks the owner.
- **The size.** About 5 to 7 evenings.

## Progress

- 2026-10-27: the task is active, third in the order of the owner of 2026-10-27, after TASK-0081 (SDL 3.2.10).
  ADR-0026 records the decisions D1 to D8 and the selection of Q3, and amends ADR-0023 item 2. The write set now
  names `eng/test-floor.txt`, whose exact count each task that adds tests changes since TASK-0078, so the
  forbid of `eng/**` becomes the two programs of `eng/`; it also names `docs/adr/0023-camera-depth-and-draw-list.md`
  and `tasks/TASK-0079-host-and-render-gaps-of-the-ninth-review.md`, whose `governed-by` gains ADR-0026.
- 2026-10-27: the intent layer. `3DEngine.Core/Input/` holds the samples, the intents, the settings with the
  default map of D4, and `IntentMachine`; a pair of contacts moves once for each frame, so two fingers that
  move together pan and do not pinch. `Camera.Pan` moves the target in the plane of the view. `SdlInput` in
  `3DEngine.Vulkan` translates SDL events, keeps Shift from the key events, and drops each mouse event of
  `SDL_TOUCH_MOUSEID`; the window turns `SDL_HINT_TOUCH_MOUSE_EVENTS` off. The host applies the intents to the
  camera and keeps the point of a selection. Sixteen new tests; four injections each failed: a kind of intent
  with no scripts (the coverage check), a one-finger drag mapped to pan (the parity of orbit and cancel), an
  inverted pinch (the parity of zoom), and a translation that keeps the synthetic mouse events (the double
  count).
- 2026-10-27: the owner accepted the pictures, and chose option A for the note of the unverified touch path:
  the ledger and this task hold it, and the next review carries it. Closed, after a clean build and a full local
  run of 522 tests (460 in `Engine.Tests`, 55 in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`).
