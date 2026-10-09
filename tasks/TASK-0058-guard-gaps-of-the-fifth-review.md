---
id: 0058
title: The address guard, the session guard and the consumed list close the gaps of the fifth review
status: Ready
phase: P0.19
opened: 2026-10-09
depends-on: [0057]
governed-by: [0004, 0006, 0011, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0058-guard-gaps-of-the-fifth-review.md
  modify:
    - Engine.Api.Http/Program.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/CommandBus.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - Engine.Tests/DocumentSessionReentryTests.cs
    - Engine.Tests/Commands/OperandConsumptionTests.cs
    - docs/glossary.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0058 — The address guard, the session guard and the consumed list close the gaps of the fifth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-09 found gaps in the guards of TASK-0051 and TASK-0037. Each finding
gives its evidence in `docs/reviews/2026-10-09-codebase-review.md`.

- E40, E41 (medium): an address with a user part, and a change to `appsettings.json` after the start,
  each make the HTTP host bind a foreign address.
- E42, E43 (low): the check of a call back into the session refuses a detached task by timing, and
  misses a call from a flow with no execution context.
- E44 (low): a command that creates and consumes the same handle reports a body that does not exist.
- E45, E47 (low), T27 (low): texts that claim too much, the order of `Document.Bodies`, and a process
  test that cannot see a loss of the start check.

TASK-0038 moves the address checks into a library, so this task comes first.

## Goal

The host binds a loopback address only in each form that the review found, and the bus refuses a
consumed list that overlaps the created list.

## Scope (in)

1. The start check refuses an address with a user part, and the tests hold the two forms of E40.
2. The endpoint configuration does not reload after the start, and a test writes `appsettings.json`
   after the start (E41).
3. The comment in `Engine.Core/DocumentSession.cs` gives the two limits of E42 and E43 and the rule for
   a sink: do not wait for work on another thread, and use `ExecutionContext.SuppressFlow` for detached
   work.
4. The bus refuses a consumed handle that is also created, and a created handle that is already live
   (E44).
5. The texts of E45 agree with the code. The glossary term "live body" says that the order of
   `Document.Bodies` is not history (E47).
6. The process test of `ASPNETCORE_HTTP_PORTS` asserts that no address was bound (T27).

## Scope (out)

- No change to `Engine.Contracts`. With item 4, the comment at `Engine.Contracts/Document.cs:27` is true.
- No authentication on the surface (ADR-0019, item 5).

## Acceptance criteria

- [ ] Each new test fails before its correction.
- [ ] The host exits with code 1 for `http://evil@localhost:5000`, and binds no new endpoint when
      `appsettings.json` changes after the start.
- [ ] The bus throws before the commit for the run of E44, and the Document does not change.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.
