---
id: 0154
title: The lag test of the events fills the queue of the subscriber with a count and no timing
status: Active
phase: P0.66
opened: 2026-10-31
depends-on: [0148]
governed-by: [0005, 0010, 0016, 0019, 0020]
writes:
  create:
    - tasks/TASK-0154-lag-test-with-no-timing.md
  modify:
    - Engine.Api.Http/WebSockets/SubscriberOptions.cs
    - Engine.Api.Http/WebSockets/Subscriber.cs
    - Engine.Api.Http/Endpoints/EventsEndpoint.cs
    - Engine.Tests/Http/EventsEndpointLagTests.cs
    - Engine.Tests/Http/HeartbeatTests.cs
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0154 — The lag test of the events fills the queue of the subscriber with a count and no timing

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0047 (TASK-0148): run 41214590078 of the review, a change of documents only, failed on Windows in
`EventsEndpointLagTests`. The test slowed the pump of the subscriber by 200 ms for each message, so the queue overflowed
only when the posts came faster than the delay. The comment of `SubscriberOptions` said that the delay is deterministic,
which is false. The owner told the session on 2026-10-31 to work alone until 2026-11-01 10:00; this task needs no
decision of the owner.

## Goal

The lag test gives the same result on a fast and on a slow runner.

## Scope (in)

1. `SubscriberOptions.PumpGate` replaces `PumpDelay`: a task that the pump awaits before each send, null in
   production.
2. The lag test holds the pump with the gate, and four events overflow a channel of two.
3. R-0047 closes.

## Scope (out)

- A change of the rule of ADR-0005 §6: a subscriber that lags is disconnected, and its queued messages go first.

## Acceptance criteria

- [ ] With a pause of 500 ms between the posts, the old test fails and the new test passes.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the gate, the lag test and the heartbeat test. With a pause of 500 ms between the posts the old test
  failed in 16 s, and the new test passed in 2 s. Five runs of the two tests passed.
