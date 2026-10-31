---
id: 0140
title: A sink of events does not throw, and a test holds each sink to it
status: Done
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

- [x] A test of each part fails when a violation is injected.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p059-sink-contract) · v0.141

## Method

Mechanical: one rule in a comment of the interface, one gate by reflection, two tests, and two comments.
Judgement:  the owner gave the correction in Q4, so this task only puts it in a test; the gate reads the two
            production assemblies that implement a sink today, and a sink in a new assembly escapes it.
Weakest:    a sink can still throw for a cause that no test injects, for example a lock that a defect breaks.

## Progress

- 2026-10-31: the rule, 3 new test cases and the two comments.
- 2026-10-31: 3 injections caught; `governed-by` names the ten ADRs of the write set; closed at v0.141.
