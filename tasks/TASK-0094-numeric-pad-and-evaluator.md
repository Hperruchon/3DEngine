---
id: 0094
title: The numeric pad, the expression evaluator and the keys of undo and redo
status: Active
phase: R10
opened: 2026-10-28
depends-on: [0093]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0032]
writes:
  create:
    - tasks/TASK-0094-numeric-pad-and-evaluator.md
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - 3DEngine.Core/Numeric/Expression.cs
    - 3DEngine.Core/Numeric/NumericPad.cs
    - 3DEngine.Tests/ExpressionTests.cs
    - 3DEngine.Tests/NumericPadTests.cs
  modify:
    - 3DEngine.Core/Input/**
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine/Ui/**
    - 3DEngine/BodyActions.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Documentation/Running.md
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0026-intent-layer.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/README.md
    - docs/register.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0094 — The numeric pad, the expression evaluator and the keys of undo and redo

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R10, after P8c (TASK-0093, v0.95), in the path to "CAD on one part". The decision sheet of
2026-10-27 gives the defaults R10-Q1 to R10-Q8. On 2026-10-28 the owner chose to put the keys of undo and redo
(register entry R-0041) into this phase. ADR-0032 records the design and three questions that the sheet does not
answer. The owner must accept ADR-0032 before the code. This phase changes no shape of `Engine.Contracts`.

## Goal

A person types the sizes of a new box and the offsets of a move, by touch, by mouse or by keyboard, and undoes and
redoes with the keys.

## Scope (in)

1. ADR-0032 is accepted. ADR-0026 and ADR-0031 get the amendment and a History line.
2. The evaluator (ADR-0032 items 1 and 2).
3. The pad state machine (item 3).
4. The pads "New box" and "Move" in the desktop, their window, their keys and their errors (items 4 to 8).
5. The keys Z and Y, the intents `UndoIntent` and `RedoIntent`, and their touch gestures (item 9). R-0041 closes.
6. Tests (item 11).

## Scope (out)

- A numeric equivalent of a drag of the camera.
- Sketch dimensions (K2).
- A unit suffix in an expression.
- The gizmo (R9).

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] The evaluator gives the expected number for each form of the grammar, and an error for each bad form.
- [ ] A script of buttons and a script of keys give the same pad states and the same command.
- [ ] Each new intent has a touch script and a mouse or keyboard script in the parity test.
- [ ] The visual check: the owner sees the pad, a new box with typed sizes, and a move with a typed offset.
- [ ] `dotnet test` passes, the build has 0 warnings, and continuous integration is green on the three runners.

## Notes for the implementer

- The pad is a state of the client (ADR-0007). Only the result number enters a command.
- `3DEngine.Core` references no `Engine.*` project, so the pad gives numbers, and the desktop makes the command.

## Progress

- 2026-10-28: opened after TASK-0093 (v0.95). The first commit holds ADR-0032, accepted by the owner with Q1 A,
  Q2 A and Q3 A, and the amendment links of ADR-0026 and ADR-0031.
- 2026-10-28: the code (ADR-0032 items 1 to 9). `Expression` and `NumericPad` in `3DEngine.Core/Numeric/`; the
  pads "New box" and "Move" in the panel, which take the keys while they are open; `InputKey.Z` and `Y` by key
  code, so the letter and not the place of the key counts; `UndoIntent` and `RedoIntent` with the taps of three
  and four fingers. 50 tests in `3DEngine.Tests`, 9 in `3DEngine.ImageTests`. 16 injections: 15 were caught at
  once; a bad field that did not become active passed until its test selected another field first.
