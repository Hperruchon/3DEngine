---
id: 0185
title: A datum on a face that a cut removed gets a fault, and a datum on the wall of a hole faces into the hole
status: Done
phase: K4c
opened: 2026-11-05
depends-on: [0184]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0051, 0052, 0053, 0054, 0055, 0058, 0059]
writes:
  create:
    - tasks/TASK-0185-references-by-the-identity-of-a-face.md
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - Engine.Core/Sketch/FaceGone.cs
    - Engine.Tests/Sketch/FaceGoneTests.cs
  modify:
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Sketch/FaceFinder.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - Engine.Tests/Sketch/FaceFinderTests.cs
    - eng/test-floor.txt
    - docs/diagnostics.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/adr/0053-desktop-sketch-on-a-face.md
    - docs/adr/0055-regeneration.md
    - docs/adr/0058-the-engine-reads-the-owned-kernel.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0185 — A datum on a face that a cut removed gets a fault, and a datum on the wall of a hole faces into the hole

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K4b (TASK-0184) gives the faces of each body, with their names, from the owned kernel. A datum still resolves its plane
from the parameters of its feature only, so a cut can remove its face with no warning (ADR-0051 item 8), and the
desktop cannot start a sketch on the wall of a hole (ADR-0053 item 2). On 2026-11-05 the owner answered the five
questions of K4c. ADR-0059 gives the design.

## Goal

A datum whose face a cut removed has the fault `W-DATUM-REFERENCE-FAILED`, and a tap on the wall of a hole gives a face
whose datum faces into the hole.

## Scope (in)

1. The check of a gone face after a `Subtract`, after an edit, and in `CreateDatum`.
2. The turn of the frame of a face of a subtrahend.
3. `GetFaceAt` from the kernel solid of the body.

## Scope (out)

- An edge or a vertex of a sketch reference that a cut removes.
- The choice of one part of a split face, and a datum on a curved face.
- The desktop does not change: it calls `GetFaceAt` as before.

## Acceptance criteria

- [x] A cut that removes the face of a datum gives the fault and one warning; an undo removes it, and a redo gives it.
- [x] An edit that moves a face out of the cut removes the fault, and an edit that moves it back gives it again.
- [x] `CreateDatum` refuses a gone face.
- [x] A datum on a hole wall faces into the hole, and two cuts turn it back.
- [x] `GetFaceAt` gives the wall of a hole, and no face where the cut removed one.
- [x] An injection into each new step makes a test fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k4c-references-by-identity) · v0.187

## Method

Mechanical: the faults, the warnings and the frames, against the kernel and Manifold.
Judgement:  the turn of a face of a subtrahend at the place of the reference.
Weakest:    a datum whose end the kernel cannot build has no check, and no test reaches that state.

## Progress

- 2026-11-05: opened after the five answers of the owner on K4c.
- 2026-11-05: the check, the turn and the finder; 5 tests and 4 changed tests, and 16 injections each fail a test.
- 2026-11-05: closed at v0.187.
