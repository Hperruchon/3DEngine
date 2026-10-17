---
id: 0067
title: The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON
status: Done
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
    - docs/roadmap.md
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

- [x] A host with a partial request stops in less than 5 s.
- [x] A refused port gives a documented exit code, and no crash. The test uses an endpoint `localhost`
      on port 0, which fails the start in the same way on each operating system.
- [x] The JSON of a tessellation over HTTP holds no indent.
- [x] Each new test fails before its correction.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.76 · the commit that carries this block.

Each host of the surface waits at most 2 s for an open request when it stops. Each failure of the start
has a documented exit code: 5 is new, for each failure other than codes 1 to 4, and the app is disposed.
The HTTP API writes compact JSON; the command line keeps its indented output.

## Method

**Mechanical.** With the old rules put back, the partial request held the stop for 30.0 s, the server
ended with the exit code -532462766 of a crash, and the tessellation held line breaks; each new check
failed. The desktop program stopped with exit code 5 and the message for the same endpoint.

**Judgement.** The stop limit lives in the surface library, so both hosts get it from one line. Code 5 is
one code for each other failure of the start, with the message of the framework, and not one code for
each cause: the causes come from the system and from the framework, and a script needs to know only that
the start failed. A refused port is not portable (an excluded port on Windows, a port under 1024 on Linux,
none on macOS), so the test uses another failure of the start that is the same on each system.

**Weakest.** The desktop has no automatic test of its exit codes (R-0039). A request that takes more than
2 s is cut when the host stops.

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
- 2026-10-17: closed, after a clean build and a full local run of 461 tests (415 in `Engine.Tests`, 39
  in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`). The write set now names `docs/roadmap.md`, for the
  line of P0.27 in the done list.
