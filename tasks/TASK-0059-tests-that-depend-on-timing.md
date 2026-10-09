---
id: 0059
title: The three tests of finding T12 no longer depend on the speed of the runner
status: Done
phase: governance
opened: 2026-10-10
depends-on: [0057]
governed-by: []
writes:
  create:
    - tasks/TASK-0059-tests-that-depend-on-timing.md
    - Engine.Tests/Http/RealHostCollection.cs
  modify:
    - Engine.Tests/Http/HeartbeatTests.cs
    - Engine.Tests/Http/HttpConcurrencyTests.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/**
    - docs/adr/**
---

# TASK-0059 — The three tests of finding T12 no longer depend on the speed of the runner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding T12 of the codebase review of 2026-10-04, register entry R-0034, limit 2026-11-03. Three tests
depend on the speed of the runner, and they failed in three of the four runs before 2026-10-10:

- `HeartbeatTests` measures the gap between two frames on the client. A late client reads two frames
  that wait in the queue, with a gap of about zero. Run 37963719832 (Windows) and run 37997004258
  (Windows) failed so.
- The deadlock test of `HttpConcurrencyTests` starts its clock before the host starts, and each loop
  runs while the clock is below one second. When the start takes more than one second, no loop runs,
  and the test reports "No command completed". Run 37997004258 (macOS) failed so.
- The collection "real host" has no definition, so its tests run beside the other tests, against the
  comment in `HostGuardTests`.

## Goal

A slow runner cannot fail these tests, and each test still fails on the defect that it guards.

## Scope (in)

1. `HeartbeatTests` counts the heartbeat frames against the time that passed since the connection, and
   fails when the count is more than one frame for each interval, with a margin. Slowness can only make
   the count smaller.
2. The deadlock test starts its clock after the host is up, and each loop runs at least one time.
3. `Engine.Tests/Http/RealHostCollection.cs` defines the collection "real host" with parallel runs off.

## Scope (out)

- No change to the host. The heartbeat frame carries no time of the server, and this task does not add
  one.
- The cause of the abort of one local run on 2026-10-08 (R-0034). A second abort gets a new register entry.

## Acceptance criteria

- [x] With a delay that copies a slow runner, the old form of each of the two tests fails with the
      message of the pipeline, and the new form passes.
- [x] With the defect of finding E8 injected into the host, the new heartbeat test fails.
- [x] With the order of the locks inverted, the new deadlock test still fails at its time limit.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.55 · the commit that carries this block.

The heartbeat test counts the frames that arrive in at least one second and fails when the count
passes the time since before the connection divided by the interval, plus two. The deadlock test starts
its clock after the host is up, and each of its loops runs at least one time. The collection "real
host" has a definition with parallel runs off.

## Method

**Mechanical.** Each proof is an injection, removed after its run.

- A wait of 1,100 ms after the start of the clock of the old deadlock test gave "No command completed",
  the message of macOS in run 37997004258. A wait of 350 ms in the client after the reset of the old
  heartbeat test gave "Heartbeat 2 came 0 ms after heartbeat 1", the message of Windows in runs
  37963719832 and 37997004258. The new forms passed with the same two waits.
- The defect of finding E8, the loop of the heartbeat with no new time of the last send, gave 6,368
  frames in 1,026 ms against a most of 12, and the new heartbeat test failed.
- The handshake with the inverted order of the locks, the lock of the broadcaster and then the session,
  stopped the commands, and the new deadlock test failed at the time limit of a request
  (`TaskCanceledException` after 10 s).
- Five full local runs passed, with 312 tests each.

**Judgement.** A count and not a gap, because the heartbeat frame carries no time of the server, and
the client can only measure when a frame arrives. A count cannot rise with slowness; a gap can fall to
zero when two frames wait in the queue. The margin of two frames covers the first frame and the
rounding of the division.

**Weakest.** The new heartbeat test sees a loop that sends too often, and not one that sends too
seldom, except a host that sends no heartbeat in 15 s. The unexplained abort of one local run of
2026-10-08 did not come back in at least 26 later runs, and its cause stays unknown.

## Progress

- 2026-10-10: the task is open, after TASK-0057, in the order of the owner of 2026-10-10.
- 2026-10-10: closed. Each injection gave the expected result, and five full runs passed.
