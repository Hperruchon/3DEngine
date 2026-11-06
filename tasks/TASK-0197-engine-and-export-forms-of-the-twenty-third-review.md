---
id: 0197
title: Each hole of a face has its owner, the STEP export refuses what a strict reader refuses, and the session of the desktop holds an open
status: Active
phase: P0.85
opened: 2026-11-06
depends-on: [0199]
governed-by: [0004, 0007, 0015, 0019, 0025, 0029, 0030, 0033, 0035, 0037, 0039, 0048, 0057, 0058, 0059, 0061, 0070]
writes:
  create:
    - tasks/TASK-0197-engine-and-export-forms-of-the-twenty-third-review.md
  modify:
    - Engine.Kernel/Brep.cs
    - Engine.Core/Export/StepExport.cs
    - Engine.Core/Sketch/FaceGone.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Tests/Kernel/PlanarSubtractTests.cs
    - Engine.Tests/Export/StepExportTests.cs
    - Engine.Tests/Export/StepReader.cs
    - Engine.Tests/Sketch/FaceGoneTests.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.ImageTests/DesktopSessionTests.cs
    - docs/adr/0070-step-export-of-the-owned-kernel.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0197 — Each hole of a face has its owner, the STEP export refuses what a strict reader refuses, and the session of the desktop holds an open

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 found E142, E143, E146 and E147 (medium) and E144, E145 and E148 to E151 (low). By
`docs/templates.md`, section 7, they come after the next product phase, K6b. The standing order of 2026-11-05 answers
Q1 to Q3 of the review with A. The first commit of this task corrects the write set and the field `governed-by`.

## Goal

Each hole of a face has its owner on each rotation of its profile, the STEP export refuses a pinched solid and a sliver
with a reason, its reader checks the senses, a face-gone fault stays, and the session of the desktop holds one open at a
time and keeps the window when a next file does not open.

## Scope (in)

1. E142 and E144: the owner of a hole from a point inside it, and a loop that repeats a vertex split at that vertex.
2. E143, E145 and E146: the refusals of `StepExport.Check` with their reasons (Q1 A).
3. E147: the four checks of `StepReader`, each proved by an injection.
4. E148: the earlier face-gone fault stays whatever end it names (Q2 A).
5. E149, E150 and E151: one open at a time, the previous file again after a failed next start (Q3 A), and the texts.

## Scope (out)

- V88 to V90 and T141 to T144 (TASK-0198).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the point inside a hole that the owner reads.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06.
- 2026-11-06: each finding has a test that failed before the change, or an injection. E142: the owner of a hole reads
  the midpoint of an edge of the hole; the test fails for two of the three rotations before the change. E144: a loop that
  holds a vertex two times is two loops. E143, E145 and E146: three refusals of `StepExport.Plan` with their reasons
  (Q1 A). E147: `StepReader` checks the sense of each loop, the direction of each LINE, the two points of each edge
  and a loop that repeats a vertex; four injections in the writer and the kernel each fail a test at one of these
  checks. E148: the earlier face-gone fault stays whatever end it names (Q2 A). E149: one open at a time. E150: the
  previous file again after a next session that does not start (Q3 A). E151: the texts of the old flow. The standing
  order of 2026-11-05 answered Q1 to Q3 with A.
