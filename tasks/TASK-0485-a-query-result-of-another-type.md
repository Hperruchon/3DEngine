---
id: 0485
title: A query result of another type than the caller asks for has its own code
status: Active
phase: P0.199
opened: 2026-11-28
depends-on: [0484]
governed-by: [0004, 0013, 0016, 0019, 0083, 0114, 0153, 0155, 0158, 0166, 0179, 0188]
writes:
  create:
    - tasks/TASK-0485-a-query-result-of-another-type.md
  modify:
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/QueryBus.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/QueryBusTests.cs
    - docs/diagnostics.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0485 — A query result of another type than the caller asks for has its own code

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0028 holds a part of finding E17 of the codebase review of 2026-09-30: `QueryBus` gives the code
`E-QRY-UNKNOWN` when a handler gives a result of another type than the caller asks for. `docs/diagnostics.md` gives that code
the meaning "no handler matches the query". TASK-0321 left this part to the owner because it needs a new diagnostic code.
Under the standing order of 2026-11-28 the recommended option applies: a new code in the namespace `QRY`.

## Goal

A result of another type gives `E-QRY-RESULT-TYPE`, and `E-QRY-UNKNOWN` keeps one meaning.

## Scope (in)

1. The code `E-QRY-RESULT-TYPE` in `DiagnosticCodes`, in `/schema/diagnostics` and in `docs/diagnostics.md`.
2. `QueryBus` gives the new code, and a test holds it.

## Scope (out)

- The other part of E17 that needs a new code: an exception of a handler, other than a cancellation, leaves the command bus.
  A rejection in its place changes the commit of the bus (ADR-0006), so it needs the owner.
- E13: `QueryResult<T>` of `Engine.Contracts` gives a value of zeros on a failure, a change of `Engine.Contracts`.

## Acceptance criteria

- [x] The test fails when the bus gives `E-QRY-UNKNOWN` for a result of another type.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0199-query-result-type) · v0.468

## Method

Mechanical: the text of the finding, the code on disk and the three lists of the codes.
Judgement:  a new code and not a new meaning of `E-QRY-UNKNOWN`, because a code never changes its meaning.
Weakest:    a host asks for `object`, so no client of a host can get the new code; only a caller in the process can.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.198. `A_Result_Of_Another_Type_Gives_E_QRY_RESULT_TYPE`
  fails with the old code. Ledger v0.468.
