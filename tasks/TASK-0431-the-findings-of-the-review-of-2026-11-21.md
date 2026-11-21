---
id: 0431
title: The medium and low findings of the codebase review of 2026-11-21
status: Ready
phase: P0.172
opened: 2026-11-21
depends-on: [0430]
governed-by: []
writes:
  create:
    - tasks/TASK-0431-the-findings-of-the-review-of-2026-11-21.md
  modify: []
  forbid:
    - Engine.Kernel/**
---

# TASK-0431 — The medium and low findings of the codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-21 (`docs/reviews/2026-11-21-codebase-review.md`, TASK-0430) found one medium and ten low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: V225 (on a portrait phone with a live sheet, "N hidden" can lie under "Views").
2. Low: E227, E228, E229, T185, T186, V223, V224, V226, V227, V228.
3. The answers Q5, Q6 and Q7 of the review, each option A under the order of the owner of 2026-11-20.

## Scope (out)

- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
