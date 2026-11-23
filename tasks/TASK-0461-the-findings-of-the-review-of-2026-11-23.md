---
id: 0461
title: The medium and low findings of the codebase review of 2026-11-23
status: Ready
phase: P0.187
opened: 2026-11-23
depends-on: [0460]
governed-by: []
writes:
  create:
    - tasks/TASK-0461-the-findings-of-the-review-of-2026-11-23.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0461 — The medium and low findings of the codebase review of 2026-11-23

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-23 (`docs/reviews/2026-11-23-codebase-review.md`, TASK-0460) found one medium and nine low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E239 (each fork and each base of a preview makes each boolean of the log again, and pushes out the kept booleans of
   the first Document).
2. Low: E240, E241, T192, V233, V234, T193, T194, T195, T196.
3. ADR-0185 names the store of the kept booleans and its cost in items 7 and 11 and in "Bad" (E239, T192).

## Scope (out)

- The answers Q1 to Q6 of the review: the first commit of the next task of each records them.
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
