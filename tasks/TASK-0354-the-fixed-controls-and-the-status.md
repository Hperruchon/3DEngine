---
id: 0354
title: The row of the anchors holds Undo, Redo and the step of Undo, and the status gives the log, the agent and the save
status: Active
phase: R18g
opened: 2026-11-12
depends-on: [0347, 0353]
governed-by: [0007, 0009, 0015, 0017, 0019, 0022, 0023, 0024, 0025, 0026, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127]
writes:
  create:
    - tasks/TASK-0354-the-fixed-controls-and-the-status.md
    - docs/adr/0127-the-fixed-controls-and-the-status-as-in-the-render.md
    - 3DEngine.App/StepTexts.cs
    - 3DEngine.ImageTests/FixedStatusTests.cs
    - 3DEngine.ImageTests/FixedStatusImageTests.cs
    - Engine.Tests/Http/EngineHostListenersTests.cs
  modify:
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/ISessionHost.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.Core/Input/InputSamples.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - Engine.Api.Http/EngineHost.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/HiddenBodiesTests.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0354 — The row of the anchors holds Undo, Redo and the step of Undo, and the status gives the log, the agent and the save

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Slice S7 of the study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G9) is roadmap row
R18g. The render has only Undo and Redo at the lower left, "Undo: C3 = 75" beside them, the status line "log 7 · all
correct · agent connected · address", and "mm · saved" in the top bar. The owner took Q3 A on 2026-11-10, and answered four
questions of this task on 2026-11-12, each with the recommended option (ADR-0127, section "Context").

## Goal

The row of the anchors and the status line read as in the render: a person sees what Undo reverses, how long the log is,
whether an agent listens, and whether the document has its own file.

## Scope (in)

1. ADR-0127, which amends ADR-0083, ADR-0093 items 1 and 3, and ADR-0098 item 3.
2. The count of the log and the step of Undo from the log and the events of the session (`StepTexts`).
3. "agent connected" from a count of the clients of the event stream (`EngineHost.Listeners`).
4. Frame and Home out of the row of a landscape display; Home in the command line; the key F.
5. "saved" or "not saved" in the top bar.
6. Tests for items 2 to 5, and a picture.

## Scope (out)

- The text of a step in the log, and the author of a step (S12).
- The text of the steps of a file that the host opens: their names show.
- A scenario of a person in the window with an agent.

## Acceptance criteria

- [ ] ADR-0127 exists. ADR-0083, ADR-0093 and ADR-0098 show the amendment.
- [ ] A test proves the count of the log and the step of Undo through Undo, Redo, a new step, an expected text and a
      rejected command; it fails when an Undo is a step.
- [ ] A test proves the text of a value, of an angle, of a distance and of a proposal.
- [ ] A test proves the status line with the log, the state and the agent, and the order in which a narrow room drops its
      parts.
- [ ] A test proves that a landscape row has the step of Undo and no Frame and no Home, and a portrait row has Frame and
      Home.
- [ ] A test proves that F frames, and that F with Control or in a field does not.
- [ ] A test proves that a client of the event stream is a listener until it closes, and that the host is not.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the status texts, the top bar, the Home command and the key F.
Judgement:  the history of the steps in the window, the count of the listeners as the state of the agent, the width of
            the step, and the drop order of the status line.
Weakest:    the window knows the text of its value edits and of the accepted proposals only; each other step shows the
            name of its command. A client that listens is not always an agent. No person used the change in the window.

## Progress

- 2026-11-12: opened on the order of the owner ("Poursuis R18g"), at `main` 3166752 (v0.338), branch r18g-fixed-status,
  with the answers of the owner of 2026-11-12. TASK-0354, ADR-0127 and the phase R18g are free on `main` and on each branch.
- 2026-11-12: the key F is a key of the window and not an intent: an intent needs a touch script (`IntentParityTests`), and
  a touch has no gesture for it. 14 injections each failed a named test; each file came back byte for byte.
