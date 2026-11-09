---
id: 0321
title: The small engine findings of R-0027, R-0028, R-0033 and R-0036 are fixed
status: Done
phase: P0.134
opened: 2026-11-09
depends-on: [0319]
governed-by: [0002, 0004, 0005, 0006, 0010, 0014, 0015, 0016, 0018, 0019, 0020, 0021, 0024, 0028, 0030, 0031, 0033, 0036, 0037, 0045, 0083, 0089]
writes:
  create:
    - tasks/TASK-0321-the-small-engine-findings-of-the-old-reviews.md
    - Engine.Tests/Cli/CliEncodingTests.cs
    - Engine.Tests/CommandBusBackendFailureTests.cs
    - Engine.Tests/CommandBusCancellationTests.cs
    - Engine.Tests/Http/EventsHandshakeTests.cs
  modify:
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/EventsEndpoint.cs
    - Engine.Api.Http/Endpoints/JsonParameters.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/Errors/ApiErrorEnvelope.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Api.Http/WebSockets/Subscriber.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Cli/Program.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/Geometry/InProcessMeshBackend.cs
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/Replay.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Hosting/ParameterBinderTests.cs
    - Engine.Tests/Http/EventsEndpointInvalidSubscribeTests.cs
    - Engine.Tests/Http/EventsEndpointResetTests.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - Engine.Tests/Http/TransportErrorTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0321 — The small engine findings of R-0027, R-0028, R-0033 and R-0036 are fixed

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-09 the owner chose a cleanup of the old findings before the codebase review that is due, with the small findings
fixed. A read-only check of the code on 2026-11-09 gave the state of each engine finding. An agent made the changes in a
worktree under these limits: no change of `Engine.Contracts`, no new diagnostic code, no change of the meaning of a code,
no change of the log. This session reviewed the diff, repeated an injection, and changed one point (the console encoding).

## Goal

Each small engine finding that needs no contract change is fixed with a test, and the register gives the state of each
finding.

## Scope (in)

1. E5 and E12: a whole JSON number stays a long, an integer field gets the exact value, and a number field takes a long
   or an int.
2. T2, the open part: a test of a backend that throws through the command bus.
3. E22: only a cancellation of the caller is Cancelled, and a cancelled result is not cached.
4. E24 parts 3, 4, 5, 6, 7 and 9: the comments of the cache, the heartbeat after the handshake, a client that leaves before
   its frame, the bind guard and the Host filter on one list, one source of the options, and two false comments.
5. E17 parts: HTTP 415 with the error envelope, a negative cursor, the output of the command line in UTF-8, and a
   subscribe frame with an unknown member.

## Scope (out)

- Each change of `Engine.Contracts` (C1, E13, E21, part of E19), each new diagnostic code (E17 parts 1 and 2), E6 (d)
  (an event kind is a contract, ADR-0005), and the large findings (E6 b and e, P1, E34). They go to the owner.

## Acceptance criteria

- [x] Each change has a test, and an injection of the old behaviour makes it fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the replay determinism holds in the
      pipeline.
- [x] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0134-engine-findings) · v0.304

## Method

Mechanical: the text of each finding and the code on disk.
Judgement:  the bind guard now refuses 127.0.0.2, 127.1 and the long form of ::1 (E24 part 6); the command line gives the
            console its earlier encoding back at the end.
Weakest:    the restore of the encoding of the console has no test, and the parts of E24 that need a run of a client
            that leaves are tested with a test socket only.

## Progress

- 2026-11-09: the changes and 23 tests. Each injection of the old behaviour failed its tests (the record of the agent);
  this session repeated the injection of the cache of a cancelled result, and its test failed. The agent set the output
  encoding of the command line; on Windows the code page of the console stayed UTF-8 after the program, so the program now
  restores the earlier encoding at the end. No test reads the code page of a console.
- 2026-11-09: run 42550718602 passed, with equal determinism hashes. Closed at v0.304.
