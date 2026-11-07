---
id: 0262
title: The check of an open reads the store of the assets
status: Ready
phase: P0.105
opened: 2026-11-07
depends-on: [0261]
governed-by: [0004, 0007, 0015, 0019, 0029]
writes:
  create:
    - tasks/TASK-0262-the-check-of-an-open-reads-the-store.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - 3DEngine/DesktopDocument.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0262 — The check of an open reads the store of the assets

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 found E167 (high): `DocumentFile.Check` makes its scratch session with no store of the
assets, so the desktop refuses each saved document with a mesh body, also when the store holds its asset. A high finding
comes before the next product phase, the close of P9c.

## Goal

The check of an open reads the asset of an import from the store of the host, as the next session does.

## Scope (in)

1. E167: a store on `DocumentFile.Check`, which the desktop document gives.

## Scope (out)

- The other findings of the review (TASK-0263, TASK-0264).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
