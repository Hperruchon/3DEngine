---
id: 0044
title: The host binds to a loopback address only, checks the Origin and the Host, and sends one heartbeat in each interval
status: Done
phase: governance
opened: 2026-09-30
depends-on: [0043]
governed-by: [0005, 0010, 0011, 0019, 0020]
writes:
  create:
    - tasks/TASK-0044-host-checks.md
    - Engine.Tests/Http/HeartbeatTests.cs
    - Engine.Tests/Http/HostGuardTests.cs
  modify:
    - Engine.Api.Http/Program.cs
    - Engine.Api.Http/WebSockets/Subscriber.cs
    - Engine.Api.Http/WebSockets/SubscriberOptions.cs
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Tests/Governance/**
    - docs/adr/**
---

# TASK-0044 — The host binds to a loopback address only, checks the Origin and the Host, and sends one heartbeat in each interval

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Findings E8 and E15 of `docs/reviews/2026-09-30-codebase-review.md`. An idle subscriber received
1,025 heartbeat frames at second 30 in a run on the real host, where ADR-0005 asks for one frame in
each interval; register entry R-0029 holds this with a limit of 30 days. A WebSocket upgrade with the
header `Origin: https://evil.example` was accepted, a command with a foreign `Host` header was
applied, and no source checks the bind address, although `docs/CHARTER.md` says that the host binds
to localhost only. The owner answered yes to question Q5 of the review on 2026-09-30: correct both
before TASK-0038 mounts the surface in the desktop host.

## Goal

The host refuses an address that is not a loopback address, refuses a foreign Origin and a foreign
Host, and sends one heartbeat in each interval.

## Scope (in)

1. The heartbeat loop waits one interval after each frame that it writes
   (`Engine.Api.Http/WebSockets/Subscriber.cs:117-132`). A test with an interval of 100 ms counts
   the frames over one second and expects at most eleven.
2. The host refuses to start on an address whose host part is not a loopback address. A test starts
   the host with `--urls http://0.0.0.0:0` and expects a refusal with a message.
3. The WebSocket options list the permitted origins: the loopback origins of the host itself, and
   none other. A test sends an upgrade with a foreign Origin and expects HTTP 403.
4. The host filters the Host header to loopback names. A test sends a command with `Host:
   evil.example` and expects HTTP 400.
5. Register entry R-0029 closes.

## Scope (out)

- No authentication. The charter lists it as a non-goal.
- No change to the wire shape of any message.
- No change to the close frame after a cancelled token (finding E17). R-0028 holds it.

## Acceptance criteria

- [x] The four tests above pass, and each one failed on the code before the change.
- [x] The run of the review, one idle subscriber for 36 seconds, gives one heartbeat frame at
      second 30 and one more at second 60 at most.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes on the
      three runners.

## Notes for the implementer

Read ADR-0005 §5 for the heartbeat and ADR-0019 for the loopback surface that the desktop host
mounts later. The origin list must accept the origin that the desktop host will use, so keep the
list in `SubscriberOptions` and not in a literal.
- 2026-09-30: the heartbeat loop waits one interval; the test counted a burst before the change.
- 2026-09-30: the loopback guard, the Origin check and the Host filter. The first form of the Origin check ran before the WebSocket middleware, passed the in-process test and let a foreign Origin through on Kestrel; a run on the real host found it, and a test now spawns the real host.

## Outcome

Status: Done · v0.37 · the commit that carries this block.

## Method

**Mechanical.** One line in the heartbeat loop. The list of permitted origin hosts in
`SubscriberOptions`. The loopback guard, the Origin check and the Host filter in `Program.cs`.

**Judgement.** The Origin check is a middleware with a list of hosts, and not the origin list of the
framework, because that list needs an exact origin with a port and the port of the host is not fixed.
A request with no Origin header passes, because a script and an agent send none, and the charter puts
them first. The value "null" is refused. The Host filter is the middleware of the framework with the
same three loopback names. A refusal of the address is an exit code and a message, so a script sees
it. Each of the four tests failed on the code before the change: the heartbeat count, the foreign
Origin, the foreign Host, and the process that ran on 0.0.0.0 until the test stopped it.

**Weakest.** The heartbeat test counts frames in one second, so a slow runner can give a count below
three and fail falsely; the upper limit is the assertion that matters. The process test spawns
`dotnet` and takes thirty seconds when it fails. The Host filter was tested with `localhost` and a
foreign name, and not with the IPv6 literal.

## Progress

- 2026-09-30: the four tests written first; each behaviour test failed on the code of today.
- 2026-09-30: the run of the review on the real host: one idle subscriber for 36 seconds gives 1 heartbeat frame at second 30; a foreign Origin gets 403; a foreign Host gets 400; the host refuses http://0.0.0.0 with exit code 1.
- 2026-09-30: closed. R-0029 is closed, and ledger entry v0.37 records the work.
- 2026-09-30: a correction before the merge. The ledger entry said 15 new tests and 232; the count is 16 and 233, because the test on the real host came last.
