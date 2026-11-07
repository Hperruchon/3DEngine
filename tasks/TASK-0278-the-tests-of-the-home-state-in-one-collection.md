---
id: 0278
title: The tests of the home state of the desktop run in one collection
status: Active
phase: P0.114
opened: 2026-11-07
depends-on: [0276]
governed-by: [0025]
writes:
  create:
    - tasks/TASK-0278-the-tests-of-the-home-state-in-one-collection.md
    - 3DEngine.ImageTests/HomeStateCollectionTests.cs
  modify:
    - 3DEngine.ImageTests/DesktopSessionTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0278 — The tests of the home state of the desktop run in one collection

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Run 42371538462, of the close of R16g, failed on macOS in `StlOpenTests.An_Accepted_Open_By_Name_Ends_The_Home_Screen`:
the first session showed no home screen. The close changed documents only, and the run of its change passed.

The home screen and the pending import of the desktop belong to the process: they are static fields of
`DesktopDocument` (ADR-0083 D6, TASK-0270). The test sets the home screen and then starts the first session.
`DesktopSessionTests` was in its own collection ("desktop sessions"), and xUnit runs two collections at the same time. Its
opens of a file go to `Reopen`, which ends the home screen. So an open of that class between the two steps of the test
gives the failure. Twenty local runs of the two classes together did not fail, so the cause comes from the code and not
from a reproduction.

## Goal

Two test classes that change the home state of the process never run at the same time, and a test keeps it so.

## Scope (in)

1. `DesktopSessionTests` in the collection of DearImGui, with the other classes of that state.
2. A test that reads the source of each image test class: a class that uses `DesktopDocument` or `RunSessions` and
   changes or reads the state is in the collection of DearImGui.

## Scope (out)

- A home state for each session in the product code: the state belongs to the process by ADR-0083 D6.

## Acceptance criteria

- [ ] The new test fails on the code before the change, and names `DesktopSessionTests.cs`.
- [ ] The new test passes after the change, and the image tests pass.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the new test on the code before and after the change.
Judgement:  the cause from the code, because no local run gave the failure; the text rules of the test (the names of the
            calls) are a list that a new kind of call can pass.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: the cause from the code; the new test fails before the change with `DesktopSessionTests.cs` and passes after
  it.
