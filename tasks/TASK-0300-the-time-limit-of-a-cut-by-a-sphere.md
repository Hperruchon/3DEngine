---
id: 0300
title: The test of a cut by a sphere has a time limit that a slow runner keeps
status: Done
phase: P0.126
opened: 2026-11-08
depends-on: [0298]
governed-by: []
writes:
  create:
    - tasks/TASK-0300-the-time-limit-of-a-cut-by-a-sphere.md
  modify:
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0300 — The test of a cut by a sphere has a time limit that a slow runner keeps

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The main run of v0.282 (42445348622) failed on the runner of macOS: the cut of a plate by a sphere took 64 s, and the
test of TASK-0289 has a limit of 60 s. The run of the branch passed under the limit. This computer takes about 10 s.

## Goal

The limit catches the form of the kernel before TASK-0289, and a slow runner passes.

## Scope (in)

1. The limit of `A_Sphere_Cuts_A_Hole_Of_One_Name` is 300 s, with the reason in its comment.

## Scope (out)

- A change of the kernel.

## Acceptance criteria

- [x] The test passes on this computer.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0126-sphere-time-limit) · v0.284

## Method

Mechanical: the times of the run 42445348622 and of this computer.
Judgement:  the runner of macOS took about six times as long as this computer, so the form before TASK-0289 would take
            about 18 minutes there; 300 s keeps that difference.
Weakest:    a limit of time on a shared runner stays a weak guard; a count of the exact signs would not depend on the
            runner.

## Progress

- 2026-11-08: the limit is 300 s.
- 2026-11-08: run 42446580070 passed. Closed at v0.284.
