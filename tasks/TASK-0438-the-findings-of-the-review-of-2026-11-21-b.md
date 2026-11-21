---
id: 0438
title: The medium and low findings of the second codebase review of 2026-11-21
status: Ready
phase: P0.174
opened: 2026-11-21
depends-on: [0437]
governed-by: []
writes:
  create:
    - tasks/TASK-0438-the-findings-of-the-review-of-2026-11-21-b.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0438 — The medium and low findings of the second codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-21 (`docs/reviews/2026-11-21-b-codebase-review.md`, TASK-0437) found three medium and
four low findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E233 (CheckInterference can hold the serial lock for minutes, and a cancel acts only between two pairs), E234 (a
   closed STL mesh whose shells cross each other passes the check of a mesh, and the class can be wrong), T187 (the work
   counters compare the analysis with the status only).
2. Low: E230, E231, E232, T188.
3. The answers Q5, Q6 and Q7 of the review, each option A under the order of the owner of 2026-11-20.
4. The review asks for a register entry for the crash of the test host of `Engine.Tests` on Linux (run 44869058349).

## Scope (out)

- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
