---
id: 0034
title: Commands, queries and snapshots pass through one serial boundary
status: Ready
phase: P0.13
opened: 2026-09-25
depends-on: []
governed-by: [0002, 0004, 0005, 0006, 0010, 0011, 0014, 0016, 0018, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0034-document-session.md
    - Engine.Core/DocumentSession.cs
    - Engine.Tests/DocumentSessionConcurrencyTests.cs
    - Engine.Tests/Http/HttpConcurrencyTests.cs
  modify:
    - Engine.Core/CommandBus.cs
    - Engine.Core/QueryBus.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Cli/Cli.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Program.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Api.Http/Endpoints/EventsEndpoint.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/CommandBusTests.cs
    - Engine.Tests/QueryBusTests.cs
    - Engine.Tests/Hosting/EngineHostingTests.cs
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0034 — Commands, queries and snapshots pass through one serial boundary

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0008 §6 says that a query is "snapshot-consistent" and "serialized against commands". ADR-0014
§3 says that the native backend is single-threaded from the view of the engine. The code does not
keep either rule. Three readers use three different locks:

- A command writes under the serial section of the bus (`Engine.Core/CommandBus.cs:21,45`).
- A query reads with no lock (`Engine.Core/QueryBus.cs:23-43`).
- The WebSocket handshake reads `Document.Bodies` under the lock of the broadcaster
  (`Engine.Api.Http/WebSockets/EventBroadcaster.cs:32-72`).

`Document` and both backends keep their state in a plain `Dictionary`. On 2026-09-23 a probe ran
20,000 `CreateBox` commands on one task and four tasks of reads. The reads gave 6,532
`ArgumentException`, 20 `InvalidOperationException` and 1 `IndexOutOfRangeException`, and one query
reported an existing body as absent. This is finding E2 of
`docs/reviews/2026-09-23-codebase-review.md`.

Finding E4 of the same review: the commit appends the command to the log
(`Engine.Core/CommandBus.cs:102`) and then calls `_events.Append(..., ct)` with the token of the
caller. A cancellation after the log append leaves a commit that is only partly done.

The comment at `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:13-14` says that each call runs
inside the serial section of the bus. That is false for a query.

## Goal

A command, a query and a snapshot never overlap, so that a reader never sees a partial commit.

## Scope (in)

1. **A failing test first.** `Engine.Tests/DocumentSessionConcurrencyTests.cs` repeats the probe:
   commands on one task, and queries and snapshot copies on other tasks. Run it on the code of
   today, and record in the Outcome block that it fails.
2. **The document session.** `Engine.Core/DocumentSession.cs` owns one Document, its backend, its
   command bus, its query bus and its event sink. Commands, queries and snapshot reads enter one
   serial section. The implementer chooses the shape of the class. The behaviour is fixed.
3. **The hosts.** `Engine.Cli` and `Engine.Api.Http` use the session. The WebSocket handshake reads
   its snapshot through the session.
4. **E4.** After the log append, the commit uses `CancellationToken.None`. A cancellation can stop a
   command before the commit and never inside it.
5. **The comment** in `ManifoldGeometryBackend.cs:13-14` agrees with the code.
6. A glossary term: document session.

## Scope (out)

- No snapshot isolation and no reader-writer lock. The architecture challenge of 2026-09-23, concern
  6, gives the reason: a single queue is correct now, and a later change needs no change to its
  callers.
- No change to the meaning of the version. TASK-0035 does that.
- No change to `Engine.Contracts`. The session is an `Engine.Core` type.
- No desktop host. TASK-0038 connects it.

## Acceptance criteria

- [ ] The concurrency test fails on the code before the change. The Outcome block records the
      exceptions that it saw.
- [ ] After the change, the concurrency test passes 100 times in a row.
- [ ] An HTTP test sends queries and commands in parallel and gets no HTTP 500.
- [ ] A test cancels the token after the log append. The log, the bodies, the events and the version
      then agree with each other.
- [ ] No lock order can deadlock: the handshake takes the session first and the broadcaster second,
      in the same order as a commit. A test runs subscriptions and commands in parallel for at least
      one second with no timeout.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The lock order.** A commit holds the serial section and then appends to the sink, and the sink of
  the HTTP host takes the lock of the broadcaster. Today the handshake takes the lock of the
  broadcaster and then reads the Document. If it also takes the serial section, the two orders are
  opposite, and two threads can each wait for the other. Take the session first in each path.
- **A long query delays a command.** This is the accepted cost. TASK-0028 measures the time of a
  tessellation, and the architecture challenge names the condition to change the model: a read that
  takes longer than one frame of interaction (about 16 ms) while a person edits.
- **ADR-0008.** Its field `affects` names `Engine.Contracts/**` only, therefore it is not in
  `governed-by`. Its §6 gives the rule that this task implements.
