---
id: 0140
title: A sink of events does not throw, and a test holds each sink to it
status: Active
phase: P0.59
opened: 2026-10-31
depends-on: [0139]
governed-by: [0004, 0005, 0006, 0015, 0020, 0021, 0028, 0031, 0033, 0037]
writes:
  create:
    - tasks/TASK-0140-a-sink-does-not-throw.md
    - Engine.Tests/EventSinkContractTests.cs
  modify:
    - Engine.Core/IEventSink.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/InMemoryEventSink.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0140 — A sink of events does not throw, and a test holds each sink to it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E18 of the codebase review of 2026-10-04 (register entry R-0033): a sink that throws leaves a gap in the
sequence, and nothing sends a reset. The owner answered question Q4 on 2026-10-04: a sink must not throw, by contract,
and a test holds each sink to it. TASK-0047 recorded the answer, and no task made the test. The comment of the bus
still said that a reset recovers a lost event. R11b (TASK-0134) waits for the visual check of the owner, and this task
changes no file of the desktop.

## Goal

The rule of Q4 is in `IEventSink` and in a test, and no comment says that a reset recovers a lost event.

## Scope (in)

1. `IEventSink` states that `Append` must not throw for a record of the bus.
2. `Engine.Tests/EventSinkContractTests.cs`: a gate that finds each class of `Engine.Core` and `Engine.Api.Http`
   that implements `IEventSink`, and requires it in a list with a test; a test of the ring when it is full; a test of
   the broadcasting sink when an action of a subscription throws.
3. The comments of `CommandBus.cs` and `InMemoryEventSink.cs` say the rule.

## Scope (out)

- E33 of R-0036 (the `seq` of a reset comes from the ring). Its only cause was a sink that throws, so the next
  codebase review judges it again.
- A sink of the desktop: `3DEngine` has no class that implements `IEventSink`.

## Acceptance criteria

- [ ] A test of each part fails when a violation is injected.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the rule, 3 new test cases and the two comments.
