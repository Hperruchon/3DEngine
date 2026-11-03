---
id: 0169
title: A datum and a reference refuse a frame or a point that is not finite, and give their warnings and codes as a body does
status: Active
phase: P0.72
opened: 2026-11-03
depends-on: [0168]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0049, 0051, 0052]
writes:
  create:
    - tasks/TASK-0169-engine-forms-of-the-nineteenth-review.md
  modify:
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - docs/diagnostics.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/adr/0052-edge-and-vertex-references-in-a-sketch.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/repo-gates/**
---

# TASK-0169 — A datum and a reference refuse a frame or a point that is not finite, and give their warnings and codes as a body does

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-03 found E125 to E127 (low) in the datums of K0g. It comes after K0h, by
`docs/templates.md`, section 7. K0h (TASK-0167) gives the references of a sketch the same walk, so the task covers them
too; its first commit adds the files of K0h to the write set. E126 waits for Q3 of the review, and E127 for Q2.

## Goal

A datum and a reference of a sketch never hold a value that is not finite, a warning comes when a fault is new or
changes, and each row of the registry names each target of its code.

## Scope (in)

1. E125: a frame with an axis that is not finite or has the length zero, or an origin or a projected point that is not
   finite, gives `E-DATUM-REFERENCE-INVALID` or `E-SKETCH-REFERENCE-INVALID` at the add, and a fault with the last
   value at a rebuild. The length of the side scales as E123 does.
2. E127 (after Q2): the warning of a datum or a reference with a fault comes only when the fault is new or changes.
3. E126 (after Q3): the rows of `W-SKETCH-REBUILD-FAILED`, `W-FEATURE-REBUILD-FAILED` and `E-SKETCH-DATUM-NOT-FOUND`,
   and the data of the warning.

## Scope (out)

- The findings of the desktop and of the gates (P0.73).

## Acceptance criteria

- [ ] Each finding has a test that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the checks of a finite value, and the rows of the registry.
Judgement:  the answers Q2 and Q3 of the owner.
Weakest:    the bits of a NaN on arm64 are inferred, not run.

## Progress

- 2026-11-03: opened by the codebase review of 2026-11-03.
- 2026-11-03: the owner answered Q1 A, Q2 A and Q3 A. ADR-0052 item 8 states the rule of the warning of a reference,
  so the write set gains ADR-0052. The handlers need no change: each one refuses through `DatumResolver`.
