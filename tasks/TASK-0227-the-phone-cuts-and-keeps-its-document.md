---
id: 0227
title: A person cuts a body in the panel, and the phone keeps its document through the background
status: Done
phase: R14e3
opened: 2026-11-06
depends-on: [0225, 0226]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073]
writes:
  create:
    - tasks/TASK-0227-the-phone-cuts-and-keeps-its-document.md
    - docs/adr/0073-the-phone-cuts-and-keeps-its-document.md
    - docs/android-test-sheet.md
    - 3DEngine.App/PhoneLifecycle.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
  modify:
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.Android/PhoneHost.cs
    - docs/adr/README.md
    - docs/INDEX.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0227 — A person cuts a body in the panel, and the phone keeps its document through the background

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The goal of R14e is a box, a subtract and the cut on the S23 Ultra (ADR-0061 item 9). The panel gave a person no
subtract, and the phone has no surface for an agent, so a person on the phone could not cut. Android destroys the
surface of the window in the background, and the program of R14e2 had one engine for its whole life. No emulator and no
system image are on this computer, so the owner is the first person to run the program.

## Goal

A person cuts a body in the panel, the phone keeps its document through a trip into the background, and the owner has a
test sheet for the S23 Ultra.

## Scope (in)

1. ADR-0073, with the options of the cut and of the background, answered by the standing order of 2026-11-05.
2. The list "Cut with" in the controls of the selected body, and `BodyActions.Cut`.
3. `PhoneLifecycle`, a new engine at each return to the foreground, and the portrait.
4. `docs/android-test-sheet.md`.

## Scope (out)

- The picker, the pre-rotation, the layout of the phone (R14g), and a test on a device by an agent.

## Acceptance criteria

- [x] A test of the panel, a test of the cut on a session, and a test of the steps of the life cycle.
- [x] The APK builds on this computer with zero warnings.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and each test passes.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14e3-phone-session) · v0.212

## Method

Mechanical: the list of the panel as the pad of Move, and the loop of the phone as the loop of the desktop.
Judgement:  a new engine at each return to the foreground, and not a new surface for the same engine; the portrait
            until the pre-rotation.
Weakest:    no device ran the life cycle; the order of the events of SDL in the background is from its
            documentation, not from a run.

## Progress

- 2026-11-06: ADR-0073 with options A, B and C for the cut and A and B for the background; the standing order chose A
  for each. The list "Cut with" stays on the line of Move, because a new line pushed Show all below the panel at 1024
  by 768 (the test of V50 failed). Three tests of the cut and nine rows of the steps. The APK builds with zero
  warnings. No device ran it.
- 2026-11-06: closed at v0.212. The row R14e stays open until the test of the owner.
