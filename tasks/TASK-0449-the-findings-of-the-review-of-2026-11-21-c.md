---
id: 0449
title: The medium and low findings of the third codebase review of 2026-11-21
status: Ready
phase: P0.180
opened: 2026-11-21
depends-on: [0448]
governed-by: []
writes:
  create:
    - tasks/TASK-0449-the-findings-of-the-review-of-2026-11-21-c.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0449 — The medium and low findings of the third codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The third codebase review of 2026-11-21 (`docs/reviews/2026-11-21-c-codebase-review.md`, TASK-0448) found two medium and nine
low findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E235 (GetAngle gives a wrong angle for the side of an arc that bites into the profile), V229 (the section finds the
   edge lines of a cut mesh on the frame thread, with no limit of triangles, against ADR-0121).
2. Low: E236, E237, E238, V230, V231, V232, T189, T190, T191.
3. The answer Q6 of the review (E235), option A under the order of the owner of 2026-11-20: the angle takes the side of the
   material.

## Scope (out)

- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
