---
id: 0058
title: The address guard, the session guard and the consumed list close the gaps of the fifth review
status: Done
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

- [x] Each new test fails before its correction.
- [x] The host exits with code 1 for `http://evil@localhost:5000`, and binds no new endpoint when
      `appsettings.json` changes after the start.
- [x] The bus throws before the commit for the run of E44, and the Document does not change.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.56 · the commit that carries this block.

The start check reads each address with `BindingAddress.Parse`, the parser of Kestrel, so the check
and the bind read the same host; an address with a user part is refused (E40). The endpoint
configuration does not reload after the start (E41). The bus refuses a created handle that it created
before, live or consumed, a created handle that the same command consumes, and a created handle two
times (E44); with that rule the comment at `Engine.Contracts/Document.cs:27` is true. The comment of
the session gives the two limits of the check of a call back and the rule for a sink (E42, E43). The
texts of E45 and the glossary term of E47 agree with the code. The process test of a port key asserts
that no address was bound (T27).

## Method

**Mechanical.** The tests first. Six new tests of a defect failed on the old code: the three cases of
E44 gave no exception, the two forms of E40 gave no refusal, and the host started a new endpoint after
a change of `appsettings.json` (E41). The assertion of T27 cannot fail while the start check is in
place, so a probe test that was never committed started the host on 127.0.0.1 with an injected check
after the start that read the address as foreign: the host exited with code 1 and the loopback
message, and the new assertion failed on "Now listening on". The probe and the injection are removed.

**Judgement.** The parser of Kestrel and not a second check with `System.Uri`, because a check that
reads an address in another way than the bind is the defect itself. The bus keeps a set of each handle
that it created, because ADR-0021 item 1 says that a consumed body never becomes live again, and the
Document holds the live bodies only; the set needs no change to `Engine.Contracts`. One bus serves one
Document from its first command, so the set is complete.

**Weakest.** The test of E41 sees the log line of Kestrel; a later version of Kestrel that starts an
endpoint with no log line would pass it. The set of created handles grows with each created body for
the life of the bus, as the log does. A flow with no execution context still escapes the check of a
call back; the comment gives the rule, and no code holds it.

## Progress

- 2026-10-10: the task is open, after TASK-0059.
- 2026-10-10: closed. 318 tests pass in three full local runs.
