---
id: 0483
title: A datetime parameter is UTC on each computer
status: Done
phase: P0.197
opened: 2026-11-28
depends-on: [0482]
governed-by: [0004, 0016]
writes:
  create:
    - tasks/TASK-0483-a-datetime-is-utc.md
  modify:
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Tests/Hosting/ParameterBinderTests.cs
    - docs/adr/0013-command-query-schema-declaration.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0483 — A datetime parameter is UTC on each computer

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0027 holds part (a) of finding E6 of the codebase review of 2026-09-23: `ParameterBinder` reads a `"datetime"`
value with `DateTimeStyles.RoundtripKind`, so a value with an offset becomes the local time of the computer. A command that
records such a value would replay to another value on a computer in another time zone, against anti-objective 16. No handler
declares the type today, so the fault is latent. The next rows of the roadmap wait for the owner, and the standing order of
2026-11-28 runs until 09:00 on 2026-11-29.

## Goal

A `"datetime"` value binds as UTC: a value with an offset gives its instant in UTC, and a value with no offset is UTC.

## Scope (in)

1. The binder reads the text with `AdjustToUniversal` and `AssumeUniversal`, and gives a typed value the kind UTC.
2. A test of four values that checks the instant and the kind, and a History line in ADR-0013.

## Scope (out)

- The other parts of E6, C1 and P1 of R-0027.

## Acceptance criteria

- [x] The test fails when the binder reads the text with `RoundtripKind`.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch p0197-datetime-utc) · v0.466

## Method

Mechanical: the text of the finding and the code on disk.
Judgement:  a value with no offset is UTC, not local time, because local time depends on the computer; the test checks the kind
            too, because a runner in UTC gives the same instant with the old code.
Weakest:    no handler declares the type, so no command shows the change.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.196. The binder gives UTC, and
  `A_Datetime_Is_Utc_On_Each_Computer` fails in three of four cases with `RoundtripKind`. Ledger v0.466.
- 2026-11-28: run 46111499789 of `96bf0218` passed on each job. Closed at v0.466.
