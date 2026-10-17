---
id: 0067
title: The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON
status: Active
phase: P0.27
opened: 2026-10-12
depends-on: [0065]
governed-by: [0005, 0007, 0010, 0011, 0013, 0016, 0017, 0018, 0019, 0020, 0021, 0024]
writes:
  create:
    - tasks/TASK-0067-host-gaps-of-the-seventh-review.md
  modify:
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - 3DEngine/**
    - Engine.Tests/Http/**
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0067 — The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-12 found three small gaps in the hosts of the surface. Each finding gives
its evidence in `docs/reviews/2026-10-12-codebase-review.md`.

- E58: an open HTTP request holds the stop of a host for 30 s.
- E59: a port that the system refuses ends each host with a crash, and the rest of E36.
- E60: the surface writes indented JSON, which makes a tessellation about 2.4 times larger.

## Goal

Each host stops within a short limit, gives a documented exit code for each failure of its start, and
writes compact JSON.

## Scope (in)

1. A short `ShutdownTimeout` in both hosts, and a test with a partial request (E58).
2. Each failure of the start has a documented exit code in both hosts, and the app is disposed (E59).
3. Compact JSON for each HTTP response; the command line keeps its own output (E60).

## Acceptance criteria

- [ ] A host with a partial request stops in less than 5 s.
- [ ] A refused port gives a documented exit code, and no crash.
- [ ] The JSON of a tessellation over HTTP holds no indent.
- [ ] Each new test fails before its correction.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The size.** About 1 evening.

## Progress

- 2026-10-17: the task is active, second in the order of the owner of 2026-10-16. E58: the surface
  library sets a stop limit of 2 s for each host. E59: each other failure of the start gives exit code 5
  and a message in both hosts, and the app is disposed; the desktop also catches a failure before its
  surface starts. E60: the HTTP API writes compact JSON. The three tests each failed with the old rule put
  back: the stop took 30.0 s, the server crashed with -532462766, and the tessellation held line breaks.
  The desktop has no test of its own for E59 (R-0039); a run with an endpoint `localhost` on port 0
  stopped with exit code 5 and the message.
