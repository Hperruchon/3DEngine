---
id: 0073
title: The person cuts a body in the panel, and the phone keeps its document through the background
status: Accepted
topic: Clients, platform
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.App/PhoneLifecycle.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.Android/**
enforced-by: 3DEngine.ImageTests/CutUiTests.cs
---

# ADR-0073 — The person cuts a body in the panel, and the phone keeps its document through the background

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The goal of R14e (ADR-0061 item 9) is a box, a subtract of a second box, and the cut on the S23 Ultra. Until this
record the panel of the desktop gave a person no subtract: an agent sent `Subtract` through the surface, and the person
watched the cut. The phone mounts no surface (ADR-0061 item 4), so a person on the phone could not cut. Anti-objective 7
holds when the panel sends a command of `Engine.Core` that the surface also gives to an agent; `Subtract` is one.

Android destroys the surface of the window when the program goes into the background, and gives a new one when it
comes back. The Vulkan surface and the swapchain of the program then name a surface that does not exist. The
swapchain takes the transform of the surface as its `preTransform`, and the renderer does not turn its picture, so a
turned phone would show a turned picture until a slice of the pre-rotation.

The owner ordered R14e under the standing order of 2026-11-05, which answers each question with the recommended option.
This record is Accepted under that order; the owner checks it on 2026-11-08.

The options for the cut:

- **A.** The controls of the selected body give a list "Cut with" of the other visible bodies; a tap on one cuts.
- **B.** A selection of two bodies, and a button Cut.
- **C.** No cut in the panel; an agent cuts.

A fits the panel of one selection that exists. B changes the selection of ADR-0029. C leaves the phone with no cut.
**Recommended: A.**

The options for the background:

- **A.** The session stays, and each time in the foreground has its own engine, with its device and its surface.
- **B.** The engine stays, and the presenter makes a new surface and a new swapchain after a loss.

A uses the path of the desktop, which makes a new engine on one window for each session (R14b), and keeps the code of
the Vulkan layer. B keeps the state of the panel through the background and needs a new path in the Vulkan layer that
no test can run before a device does. **Recommended: A.**

## Decision

1. **The cut.** The controls of the selected body give a list "Cut with" (`DesktopUi.CutLabel`) of the other visible
   bodies, on the line of Move when it fits there, else on a line of its own. A tap on a body gives
   `CutBodyAction(Body, Tool)`. The list shows only with a selected body and another visible body. The open list is a
   rectangle of the panels, so a tap on it does not reach the scene.
2. **The command.** `BodyActions.Cut` sends `Subtract` with the selected body as the minuend and the tool as the
   subtrahend. The new body becomes the selection, as after a move; a rejection goes to the line of the document. A host
   with no files, as the phone, has a line of the document with the message only.
3. **The background.** `PhoneLifecycle` gives a step for each event of SDL: a quit, a close and `TERMINATING` end the
   program; `WILL_ENTER_BACKGROUND` and `DID_ENTER_BACKGROUND` end the engine; `DID_ENTER_FOREGROUND` makes a new engine
   on the same window and the same session. The document stays; the selection and the state of the panel do not. The
   host sets `SDL_HINT_ANDROID_BLOCK_ON_PAUSE` to 1: SDL blocks the thread of the program while it is paused, so the
   loop reads the events of the background at the return, and the engine ends then, just before the new engine starts.
4. **The orientation.** The activity stays in portrait until a slice of the pre-rotation.

## Consequences

**Good:** a person cuts a body on the desktop and on the phone, with the command that an agent uses. A trip into the
background keeps the document of the phone.

**Bad:** the selection, the open pad and the camera go at each trip into the background. The phone has no landscape.
No device ran the life cycle before the test of the owner.

**Next:** the test of the owner on the S23 Ultra (`docs/android-test-sheet.md`); the picker of Android for Save and Open;
the pre-rotation.

## History

- 2026-11-06 · TASK-0227 · The record, under the standing order of 2026-11-05.
- 2026-11-06 · TASK-0232 · The codebase review of 2026-11-06 (c): item 1 gives the line of its own for an extrude (V98)
  and the rectangle of the open list; item 2 gives the line of a host with no files (V99); item 3 gives the order of a
  blocked thread, with the hint of SDL (V101, Q1 B of that review, answered by the standing order of 2026-11-05); the
  affects field names the files of items 1 and 2 (V102).
