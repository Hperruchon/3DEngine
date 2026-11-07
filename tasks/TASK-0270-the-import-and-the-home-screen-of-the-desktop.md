---
id: 0270
title: The import of a chosen STL reaches only its new part, and an open by name ends the home screen
status: Ready
phase: P0.110
opened: 2026-11-07
depends-on: [0269]
governed-by: [0007, 0019, 0025, 0029, 0030, 0061, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083]
writes:
  create:
    - tasks/TASK-0270-the-import-and-the-home-screen-of-the-desktop.md
  modify:
    - 3DEngine/DesktopDocument.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/ISessionHost.cs
    - Engine.Api.Http/DocumentFolder.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0270 — The import of a chosen STL reaches only its new part, and an open by name ends the home screen

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (b) found two high findings in the desktop: V125 (the import of a chosen STL is not
bound to its request of a new part, so a named document can receive it) and V126 (a recent file of the home screen opens
under the home screen again). A high finding comes before the next product phase.

## Goal

The import of a chosen STL reaches only the new part that its request makes, and an accepted open by name ends the home
screen.

## Scope (in)

1. V125: the request and its import as one record; the target session takes it; the surface refuses an open while a
   request waits; the probes of the review as tests.
2. V126: an accepted open by name ends the home screen; a test through the sessions.

## Scope (out)

- The medium and low findings (TASK-0271, TASK-0272).

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

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
