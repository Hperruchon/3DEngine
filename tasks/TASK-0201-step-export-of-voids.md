---
id: 0201
title: A void inside a body leaves the engine as a BREP_WITH_VOIDS of its STEP file
status: Active
phase: K6b
opened: 2026-11-06
depends-on: [0200]
governed-by: [0004, 0048, 0070]
writes:
  create:
    - tasks/TASK-0201-step-export-of-voids.md
  modify:
    - Engine.Core/Export/StepExport.cs
    - Engine.Tests/Export/StepExportTests.cs
    - Engine.Tests/Export/StepReader.cs
    - docs/adr/0070-step-export-of-the-owned-kernel.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0201 — A void inside a body leaves the engine as a BREP_WITH_VOIDS of its STEP file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K6a (TASK-0200) refuses a body with a void, because a `MANIFOLD_SOLID_BREP` holds one shell only. A Subtract of a box
inside another box makes such a body from the command line, so the refusal stops a real export. This is the second
slice of K6, in the engine lane of the order of 2026-11-05. ADR-0070 holds the decisions.

## Goal

A body with a void gives a `BREP_WITH_VOIDS` whose voids are the voids of its outer shell, and the file reads back with
the counts of the B-Rep.

## Scope (in)

1. The outer shell of each void: the smallest outer shell that holds its first vertex, by an exact ray.
2. `BREP_WITH_VOIDS` with an `ORIENTED_CLOSED_SHELL` with `.F.` on the turned shell of each void.
3. The reader of the tests reads a `BREP_WITH_VOIDS`, and tests of one void, two voids, a nested void and a ray that
   meets an edge.
4. ADR-0070 items 2, 3 and 6, and the row of `E-EXPORT-BODY-FAILED`.

## Scope (out)

- The desktop button, the name and the colour of a body, and the surfaces of K5.
- A change of the kernel, of `Engine.Contracts`, of the log or of the document file.

## Acceptance criteria

- [x] A body with one void and a body with two voids read back with the counts of their B-Reps, and each face of a void
      points out of the void.
- [x] A void inside a solid inside a void belongs to the inner solid.
- [x] A ray that meets an edge goes to the next direction.
- [x] The hash of the fixed body of K6a does not change.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k6b-step-voids) · v0.TBD

## Method

Mechanical: the reader of a `BREP_WITH_VOIDS`; the turn of the faces of a void.
Judgement:  the owner of a void is the smallest outer shell that holds it, by parity of an exact ray; eight fixed
            directions, each one to the next on an edge, a vertex or a plane at the point.
Weakest:    no reader of STEP outside the repository read a file with a void. The rule of ISO 10303-42 for the sense of
            the shell of a void comes from the standard as the writer of Open CASCADE applies it, not from a check here.

## Progress

- 2026-11-06: opened by the engine lane of the order of 2026-11-05. The void as a `BREP_WITH_VOIDS`, the owner of a void
  by an exact ray, answered by the standing order of 2026-11-05. Four injections each fail a test: the largest outer
  shell as owner, no turn of the faces of a void, the parity turned, and no check of a point on an edge.
