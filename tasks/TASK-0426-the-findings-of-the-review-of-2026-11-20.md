---
id: 0426
title: The medium and low findings of the codebase review of 2026-11-20
status: Ready
phase: P0.171
opened: 2026-11-20
depends-on: [0425]
governed-by: []
writes:
  create:
    - tasks/TASK-0426-the-findings-of-the-review-of-2026-11-20.md
  modify: []
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0426 — The medium and low findings of the codebase review of 2026-11-20

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-20 (`docs/reviews/2026-11-20-codebase-review.md`, TASK-0425) found six medium and ten low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase, R20d.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: T183 (the concurrency test of the HTTP host decides its result with a time limit), V210 (a race of one frame in the
   host brings back V206), V211 (the file menu can open behind the structure view), V212 (a pad of a sketch on a phone measures
   its room at one top and opens at another), V221 (on the sheet page the paper covers the arc of a proposal, and a tap on
   Accept over the paper also acts on the paper), V222 (on the sheet page the arc hides the line of each command).
2. Low: E226, T184, V213 to V220.
3. The answers Q1, Q2, Q3 and Q8 of the review, each option A under the order of the owner of 2026-11-20.

## Scope (out)

- E222 beyond the cache: the cut of each polygon by the plane needs a task of the kernel.
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
