---
id: 0262
title: The check of an open reads the store of the assets
status: Done
phase: P0.105
opened: 2026-11-07
depends-on: [0261]
governed-by: [0004, 0007, 0015, 0019, 0025, 0029, 0061]
writes:
  create:
    - tasks/TASK-0262-the-check-of-an-open-reads-the-store.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine/DesktopSession.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
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

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0105-check-store-b) · v0.245

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the store is an optional parameter of the check, as of the composition of the engine, and the desktop gives
            the store of its session; the HTTP surface does not call the check.
Weakest:    the HTTP surface opens a file through its own folder and the next session, not through the check, so no test
            holds an open over HTTP of a document with a mesh body.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
- 2026-11-07: the store on the check and on the desktop document; one test, which an injection of a check with no store
  fails.
- 2026-11-07: run 42325122816 passed. Closed at v0.245.
