---
id: 0298
title: The list "New" holds the cone, the sphere and the torus
status: Done
phase: K5e
opened: 2026-11-08
depends-on: [0289]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091]
writes:
  create:
    - tasks/TASK-0298-cone-sphere-and-torus-in-the-list-new.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - docs/adr/0076-the-cylinder.md
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0298 — The list "New" holds the cone, the sphere and the torus

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0089 item 5 names phase K5e: the three entries of the list "New" of ADR-0076 item 7, after the commands of the cone
(K5c) and of the sphere and the torus (K5d).

## Goal

A person on the desktop or on the phone makes a cone, a sphere or a torus from the list "New", with a pad of its sizes.

## Scope (in)

1. The entries Cone (radius 5, height 10), Sphere (radius 5) and Torus (radius 10, tube radius 3), each with its pad in the
   length unit of the document, and each apply sends its command.
2. Tests: each entry and its pad, for a finger and for the mouse, and the body of each command with a refusal.

## Scope (out)

- A change of a command. The determinism probe with a sphere.

## Acceptance criteria

- [x] Each new test passes, and a test fails with an injection of the change.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k5e-new-list) · v0.283

## Method

Mechanical: the form of the entry "Cylinder" of ADR-0076 item 7 for each new entry.
Judgement:  the defaults of the torus keep the tube inside the ring.
Weakest:    no run on the S23 showed the three entries; a cut by a sphere holds the frame for about 10 s.

## Progress

- 2026-11-08: the three entries, their actions and their tests; a wrong default of the torus fails the test of the pads.
- 2026-11-08: run 42444399314 passed. Closed at v0.283.
