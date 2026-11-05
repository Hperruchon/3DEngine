---
id: 0194
title: The host owns the document session, the surface mounts on it as an option, and an open stays in the same process
status: Done
phase: R14b
opened: 2026-11-06
depends-on: [0193]
governed-by: [0007, 0016, 0019, 0025, 0029, 0030, 0061]
writes:
  create:
    - tasks/TASK-0194-the-host-owns-the-session.md
    - 3DEngine/DesktopSession.cs
    - 3DEngine.ImageTests/DesktopSessionTests.cs
  modify:
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/DesktopSurface.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/DocumentFolder.cs
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - docs/adr/0061-android-platform.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0194 — The host owns the document session, the surface mounts on it as an option, and an open stays in the same process

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase b of the Android order of 2026-11-05 (ADR-0061 item 9). The container of the surface made the session of the
desktop and disposed it, so a host with no surface had no session, and an open of a file started the program again
(`3DEngine/DesktopDocument.cs`, `StartAgain`). The phone has no surface (ADR-0061 item 4), and an Android app cannot
start itself again. The behaviour of the desktop must not change.

## Goal

The host makes the session, mounts the surface on it, and disposes it after the surface. An open of a file ends the
session, and the next session holds the file in the same process, on the same port and in the same window.

## Scope (in)

1. `DesktopSession`: the session, the document folder and the store of the assets, from the settings, with the exit
   codes of today.
2. A form of `AddEngineSurface` for a session that the host owns.
3. `DesktopSurface` mounts on the session at the point of its checks where it made the session before.
4. `DesktopEngineHost.RunSessions`: the sessions one after the other, on one window.
5. Tests: a session with no surface, an open in the same process, and a surface that does not dispose its session.

## Scope (out)

- The Android host (R14e). The swap of the SDL binding (R14c).

## Acceptance criteria

- [x] Each test of the desktop and of the surface passes, with no change of its expectation except the start again.
- [x] Each injection of the change fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14b-host-owns-session) · v0.196

## Method

Mechanical: the move of the session from the container of the surface to the host, with the order of the checks kept.
Judgement:  the session opens inside the start of the surface, so the exit codes keep their order.
Weakest:    the tests run the loop of the sessions with no window; only the probe made a second GPU device on the
            real window, and nobody looked at the picture of the second session.

## Progress

- 2026-11-06: opened with the Android order of 2026-11-05; the session, the surface on it, the loop of the sessions,
  3 tests, and 4 injections that each fail a test.
- 2026-11-06: a probe of the real desktop with --port 0: a box, a save as second, a box, and an open of second over
  HTTP gave 202; the next session served second.3de at the version 2 on a new port, in the same process, with a new
  Vulkan instance and no validation message. Nobody looked at the window.
- 2026-11-06: closed at v0.196.
