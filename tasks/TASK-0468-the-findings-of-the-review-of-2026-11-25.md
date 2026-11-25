---
id: 0468
title: The medium and low findings of the codebase review of 2026-11-25
status: Ready
phase: P0.189
opened: 2026-11-25
depends-on: [0467]
governed-by: []
writes:
  create:
    - tasks/TASK-0468-the-findings-of-the-review-of-2026-11-25.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0468 — The medium and low findings of the codebase review of 2026-11-25

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-25 (`docs/reviews/2026-11-25-codebase-review.md`, TASK-0467) found two medium and eleven low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E242 (after a failed accept, one `Redo` applies the part of the proposal that ran), with the answer Q2 of the owner.
2. Medium: T197 (a race of the worker and the accept decides
   `A_Commit_That_Does_Not_Change_The_Result_Runs_One_Fork_And_Applies`), with the answer Q3; the exit of register entry R-0078.
3. Low: E243, E244, E245, V236, V237, V238, T198, T199, T200, T201, T202.
4. ADR-0188 items 5, 7 and 9 and "Bad" say what the code does after the corrections (E242, E245, V237).

## Scope (out)

- The answers Q1 to Q4 of the review: the first commit of the next task of each records them. Q4 goes to K24.
- The forbid is `Engine.Contracts/**`: when the answer Q2 needs a mark in the log that changes a contract, stop and ask the owner
  first (`CLAUDE.md`, "Stop and ask").
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
