---
id: 0062
title: The hosts of the surface refuse each bracket form, stop well, and own their session
status: Done
phase: P0.24
opened: 2026-10-11
depends-on: [0061]
governed-by: [0004, 0005, 0006, 0007, 0010, 0011, 0013, 0016, 0017, 0018, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0062-host-gaps-of-the-sixth-review.md
  modify:
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - 3DEngine/**
    - Engine.Core/CommandBus.cs
    - Engine.Tests/**
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0062 — The hosts of the surface refuse each bracket form, stop well, and own their session

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-11 found gaps in the hosts of the surface. Each finding gives its
evidence in `docs/reviews/2026-10-11-codebase-review.md`.

- E48 (medium): seven forms with brackets pass the address check before the start, and Kestrel binds
  each interface for them.
- E49, E50 (medium): a subscriber holds the stop of a host for 30 s, and Ctrl+C stops the surface of
  the desktop and leaves the window.
- E51 to E57 (low): a port in use, the disposal of the backend, the check of created handles, the
  setting `port`, the refusal messages, the public backend of `EngineHost`, and texts that the
  topology made false.

## Goal

Each host refuses each form that Kestrel binds on each interface, stops when it must, and disposes the
session that it owns.

## Scope (in)

1. The address check accepts brackets only as one pair around an IPv6 address, and the seven forms are
   in the tests (E48).
2. A host ends each subscriber when it stops (E49). The desktop ends its window loop when the surface
   stops (E50).
3. A port in use gives a documented exit code, and the desktop disposes its session on each path
   (E51, E52).
4. A test gives a handle two times in the created list, and the clause that cannot decide leaves the
   bus (E53).
5. The setting of the port has a prefix, the refusal messages name the host, `EngineHost.Backend` is
   internal, and the texts of E57 agree with the code (E54 to E57).

## Acceptance criteria

- [x] Each new test fails before its correction. The case of E53 passed before, because the bus
      already refused a handle two times; it fails when that clause is removed.
- [x] A host with one subscriber stops in less than 5 s. Before the correction it took 31.0 s.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.62 · the commit that carries this block.

The address check accepts brackets only as one pair around an IPv6 loopback address, and a host with
no brackets only as `localhost` or an IPv4 loopback address (E48). The `/events` endpoint ends its
subscriber when the host stops, so a host with a subscriber stops at once (E49). The window loop of
the desktop ends when the surface stops (E50). A port in use gives exit code 4 in both hosts (E51).
The container of the desktop makes the session, so the disposal of the app disposes the backend
(E52). The bus lost a clause that never decided, and a test gives one handle two times in the created
list (E53). The port of the desktop is the setting `Desktop:Port`, which `--port` gives, and the
variable `PORT` has no effect (E54). The refusal messages name the surface and not `engine-api-http`
(E55). `EngineHost.Backend` is internal (E56). ADR-0019, `EngineHost` and `DesktopSurfaceTests` no
longer describe the desktop surface as work to come (E57).

## Method

**Mechanical.** The tests first. On the code of v0.61 the seven forms of E48 passed the check, the
second `engine-api-http` on a port in use ended with exit code -532462766, and the stop of a host
with one subscriber took 31.0 s. After the corrections the three tests pass, and the full suite of
337 tests passes. The new case of E53 fails when the clause `!created.Add` is removed. A run of the
desktop executable on a port that another process held, with `PORT=abc`, stopped before the window
with exit code 4 and the message of the port in use. Before the correction, that value of `PORT`
gave exit code 2.

**Judgement.** Each `/events` request registers the stop of its own subscriber on
`ApplicationStopping`, and the read of the subscribe frame also ends at the stop. This needs no list
of subscribers and does not make the session at the stop of a host that never made it. The close
status at the stop stays "normal closure"; ADR-0005 gives no other status for it. Exit code 4 is new
in both hosts, so each host has the same code for the same cause.

**Weakest.** No test calls `DesktopSurface`, so E50, E52 and E54 have no automatic test. E50 is not
observed: a check needs the window, and an agent must not open it. E52 is correct by reading only.
Register entry R-0039 holds the gap.

## Progress

- 2026-10-11: the task is open, in the order of the owner of 2026-10-11.
- 2026-10-11: closed, after a full local run of 337 tests.
