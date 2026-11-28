---
id: 0484
title: The time limit of the subscribe frame is 30 seconds, after a red main run with 10 seconds
status: Active
phase: P0.198
opened: 2026-11-28
depends-on: [0483]
governed-by: [0005, 0010, 0019, 0020]
writes:
  create:
    - tasks/TASK-0484-the-subscribe-limit-is-30-seconds.md
  modify:
    - Engine.Api.Http/WebSockets/SubscriberOptions.cs
    - Engine.Tests/Http/EventsEndpointKestrelTests.cs
    - docs/adr/0005-event-stream-and-replay.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0484 — The time limit of the subscribe frame is 30 seconds, after a red main run with 10 seconds

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

TASK-0481 (P0.195, v0.464) gave the subscribe frame of `/events` a time limit of 10 seconds (ADR-0005 section 5 step 2). Main run
46113600404 of the merge 0f811442 (P0.197) failed on Windows:
`EventsEndpointResetTests.Subscribe_With_A_Negative_Cursor_On_An_Empty_Ring_Receives_Reset` sent its frame at once after the
connect and got the close 1007 with `E-API-WS-INVALID-SUBSCRIBE`. The frame is valid, and the path of a malformed frame did not
change, so the time limit gave the close. The probable cause is that the threads of the pool were busy for more than 10 seconds
while the tests of `Engine.Tests` ran in parallel on a slow runner.

## Goal

The limit is 30 seconds, the interval of the heartbeat, so a client of a busy host does not get the close of a missing frame.

## Scope (in)

1. `SubscriberOptions.SubscribeTimeout` is 30 seconds, and ADR-0005 section 5 step 2 says so.

## Scope (out)

- A limit that does not use the clock. A time limit can always meet a host that is busy for longer.

## Acceptance criteria

- [x] The test of the default asks for 30 seconds.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0198-subscribe-limit) · v0.467

## Method

Mechanical: the value and the text of the ADR.
Judgement:  30 seconds, the interval of the heartbeat, because a host that is busy for that long also delays each heartbeat.
Weakest:    the cause is inferred: the annotation of the run gives no time, and no local run gave the failure.

## Progress

- 2026-11-28: opened after the red main run 46113600404. Under the standing order of 2026-11-28 a red main run stops the merges,
  so this branch waits for the owner. Ledger v0.467.
