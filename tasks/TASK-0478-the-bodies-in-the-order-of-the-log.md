---
id: 0478
title: The snapshot of the events and CheckInterference give the live bodies in the order of the log
status: Done
phase: K25
opened: 2026-11-28
depends-on: [0476]
governed-by: [0004, 0005, 0010, 0016, 0018, 0019, 0020, 0021, 0028, 0033, 0037, 0045, 0060, 0182, 0188, 0189, 0191, 0192]
writes:
  create:
    - tasks/TASK-0478-the-bodies-in-the-order-of-the-log.md
    - docs/adr/0192-the-bodies-of-the-snapshot-and-of-the-check-in-the-order-of-the-log.md
    - Engine.Core/LogOrder.cs
    - Engine.Tests/Queries/BodyLogOrderTests.cs
  modify:
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Queries/GetBodiesQueryHandler.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0182-the-pair-check-at-one-position.md
    - docs/adr/0189-the-lists-of-the-sheets-and-of-the-bodies.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0478 — The snapshot of the events and CheckInterference give the live bodies in the order of the log

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K25 of the roadmap and register entry R-0080: `Document.Bodies` gives the values of a .NET `Dictionary`, so the snapshot of
`/events` and `CheckInterference` with no `bodyIds` give an order that the log does not predict. On 2026-11-26 the owner answered
"task fixes". The codebase review of 2026-11-28 asked if K25 comes before R34 and R35 (Q1), and the standing order of 2026-11-28
answers A (TASK-0476): K25 is the next product phase after P0.192.

## Goal

The snapshot, the check and `GetBodies` give the live bodies in one order, the order of the log, with an ADR in the same commit.

## Scope (in)

1. `Engine.Core/LogOrder.cs`, the one function of the order, and its three readers.
2. ADR-0192, which amends ADR-0012 section 6, ADR-0182 item 2 and ADR-0189 item 3.
3. `BodyLogOrderTests`, with the scene of R-0080, and the close of R-0080.

## Scope (out)

- `Document.Sheets` and the first read of the window, which keep the order of the collection (ADR-0192, "Bad").
- A change of `Engine.Contracts`.

## Acceptance criteria

- [x] A test fails when the snapshot or the check takes the order of the collection.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch k25-log-order) · v0.461

## Method

Mechanical: one function of the order of the log from `GetSheetsQueryHandler.InLogOrder`, and its readers.
Judgement:  the scene of the test: the bus adds the new body of a command before it removes the consumed one, so the free place
            goes to the next new body, not to the body of the same command.
Weakest:    the test shows the order of the collection only with the .NET of today; a version of .NET with another rule of the
            free places would keep the test green with no defect to show.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.192 (Q1 A of the review of 2026-11-28). `LogOrder.Bodies`
  gives the order; the snapshot, `CheckInterference` and `GetBodies` take it. `A_Body_In_The_Free_Place_Of_A_Consumed_One_Comes_In_The_Order_Of_The_Log`
  fails when the snapshot takes `doc.Bodies` (line 48) and when the check takes `document.Bodies` (line 50). R-0080 closes.
  Ledger entry v0.461.
- 2026-11-28: run 46095867274 of `ff89a2ed` passed on each job. Closed at v0.461.
