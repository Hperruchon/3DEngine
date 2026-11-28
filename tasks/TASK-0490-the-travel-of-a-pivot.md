---
id: 0490
title: The query CheckTravel turns a pivot from limit to limit and gives its first contact
status: Done
phase: K12a
opened: 2026-11-28
depends-on: [0488]
governed-by: [0004, 0012, 0013, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0037, 0060, 0083, 0089, 0112, 0114, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0188, 0191, 0193, 0194, 0195]
writes:
  create:
    - tasks/TASK-0490-the-travel-of-a-pivot.md
    - docs/adr/0195-the-travel-of-a-pivot.md
    - Engine.Core/Queries/CheckTravelQuery.cs
    - Engine.Core/Queries/CheckTravelQueryHandler.cs
    - Engine.Tests/Queries/CheckTravelTests.cs
  modify:
    - Engine.Core/Commands/JointTree.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0490 — The query CheckTravel turns a pivot from limit to limit and gives its first contact

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K12: the travel of a pivot from one limit to the other, sampled with a margin so that no contact between two samples is missed,
with the intended contacts exempt, and the first contact to a stated tolerance (ADR-0126 item 4). P11a and P11b gave the joints, their
limits and the pairs of the joints. R34 and R35 wait for a check on the phone, and R23 and R27 are tools of the window, which no one
can check this weekend, so K12 is the next row that needs no person.

## Goal

A query gives the first contact of the travel of a pivot, or says that the travel is free, with no change of the Document.

## Scope (in)

1. The query `CheckTravel` and ADR-0195. No change of `Engine.Contracts`, so the standing order of 2026-11-28 answers its choices
   with the recommended option.

## Scope (out)

- The exact sweep of a slider: row K12b.
- The envelope (K13) and the tools of the window (R23, R27).

## Acceptance criteria

- [x] A test of each item of ADR-0195, and two injections that make a test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch k12a-pivot-travel) · v0.474

## Method

Mechanical: the contact of the test is where 10 s + 5 c = 8, the height of the far corner of the child, so no angle is needed.
Judgement:  the travel in memory and not on a fork; the conservative advance with the half angle; the tolerance of 0.01 mm.
Weakest:    the answer is in doubles, and a long arc near a body takes many samples (the limit is 4096).

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.201 (v0.473). ADR-0195, the query, and 6 tests in
  `Engine.Tests/Queries/CheckTravelTests.cs`. Two injections (no bound of the step; no exempt pair) each made a test fail. Ledger
  v0.474.
- 2026-11-28: run 46160928191 of `24ef0a00` passed on each job. Closed at v0.474.
