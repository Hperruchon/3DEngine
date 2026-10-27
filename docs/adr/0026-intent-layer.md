---
id: 0026
title: Device-tagged pointer samples, an intent layer, and a parity test of touch and mouse
status: Amended
topic: Rendering, clients
date: 2026-10-27
supersedes: []
superseded-by: []
amends: ['0023']
amended-by: ['0027']
affects:
  - 3DEngine.Core/Input/**
enforced-by: 3DEngine.Tests/IntentParityTests.cs (TASK-0082 creates it)
---

# ADR-0026 — Device-tagged pointer samples, an intent layer, and a parity test of touch and mouse

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R7 asks for device-tagged pointer samples, an intent layer, and a parity test. It serves
objective 9 of `docs/CHARTER.md`: each operation works by touch, and also by mouse and keyboard. Until R7 the
desktop read a left drag and the wheel from SDL events in its host (ADR-0023 item 2). The codebase review of
2026-10-17, sections 5 and 6, gives the research:

1. SDL 3 gives mouse, touch, pen and keyboard events, each with a device identifier and a time. A touch also
   gives synthetic mouse events with the mouse identifier `SDL_TOUCH_MOUSEID`, so a layer that reads both
   kinds counts one touch two times.
2. No computer of the project has a touch screen, and no runner has one.
3. `3DEngine.Core` references no project, so a layer there has no SDL and a test can run it with no window.
4. ADR-0007 makes the camera, the selection and the user settings states of the client.

On 2026-10-17 the owner chose the decisions D1 to D8 of question Q2 of that review, and on 2026-10-27 the
order that puts R7 after the move to SDL 3.2 (TASK-0081).

## Decision

1. **Samples (D1, D2).** A pointer sample records the kind of device (mouse, touch or pen), the device, the
   pointer, the position in pixels of the window, the pressure, the buttons, the modifier keys, the time and
   the phase (down, move, up or cancel). A wheel sample and a key sample are their own records. The host
   collects the samples of one frame into one input frame. A pen takes the path of one finger; the kind Pen
   stays in the record. A text sample records the text that the keyboard gives, and the keys of text editing
   are keys of a key sample too (ADR-0027 item 5). The host gives each sample to the user interface first,
   and a gesture that starts over a panel does not reach the state machine (ADR-0027 item 4).
2. **Intents.** A pure state machine in `3DEngine.Core/Input/` turns the frames into intents with no kind of
   device: orbit, pan, zoom, select and cancel. It reads no clock and no device of its own, so a test gives it
   any sequence.
3. **The map (D4, D6).** A setting of the client maps each gesture to an action. The default: one finger or a
   left drag orbits; two fingers, a middle drag, or Shift and a left drag pan; a pinch or the wheel zooms; a
   tap or a click selects; the arrow keys orbit, Shift and the arrow keys pan, and the keys plus and minus
   zoom. The setting is a user setting, which ADR-0007 permits; it needs no command.
4. **The translation (D3, D8).** The translation of SDL events into samples lives in `3DEngine.Vulkan`, beside
   `Window`. The window turns the hint `SDL_HINT_TOUCH_MOUSE_EVENTS` off, and the translation also drops each
   mouse event whose device is `SDL_TOUCH_MOUSEID`.
5. **The parity test (D5).** For each kind of intent, a script of touch samples and a script of mouse samples
   give the same intents, and a check fails when a kind of intent has no touch script. The test runs in
   `3DEngine.Tests`, with no window and no GPU.
6. **The selection (Q3 of the review).** The selection is a state of the client (ADR-0007). In R7 the host
   keeps the point of a select intent and draws nothing for it.

This record amends ADR-0023 item 2: the intent layer, and not the host, reads the mouse.

## Consequences

**Good:** One map of gestures serves touch, mouse and keyboard, and a test holds that each action has a touch
path. The layer needs no SDL, so its tests run on each runner. The map is a setting, so a person changes a
gesture with no change of code.

**Bad:** No touch screen exists, so the touch path is proven by synthetic samples only, and a real screen can
behave in another way (the register holds the check). The intents of R7 move the camera only, so they write no
command, and the comparison of logs of anti-objective 7 starts with R8.

**Next:** TASK-0082 applies this decision.

## History

- 2026-10-27 · TASK-0082 · The record is written with the decisions D1 to D8 of the owner of 2026-10-17.
- 2026-10-27 · TASK-0084 · ADR-0027 amends item 1: the text sample, the keys of text editing, and the user
  interface that takes a sample before the state machine.
