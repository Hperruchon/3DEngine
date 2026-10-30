---
id: 0128
title: The roadmap row K3 closes, because K3a, K2c and K2d hold the fourteen kinds
status: Done
phase: governance
opened: 2026-10-31
depends-on: [0102, 0110, 0117]
governed-by: []
writes:
  create:
    - tasks/TASK-0128-close-the-row-k3.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - docs/adr/**
---

# TASK-0128 — The roadmap row K3 closes, because K3a, K2c and K2d hold the fourteen kinds

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The row K3 of `docs/roadmap.md` says "The rest of the constraint solver, after K3a. Write it." ADR-0033 item 3 names
fourteen kinds of constraint. K3a (TASK-0102) gave the solver five of them, K2c (TASK-0110) seven, and K2d
(TASK-0117) tangent and radius. `SketchConstraints.Given` in `Engine.Core/Sketch/SketchConstraints.cs` holds the
fourteen. On 2026-10-31 the owner chose to close the row ("1 3 4", item 4).

## Goal

The row K3 says that it is done, and names the tasks that did the work.

## Scope (in)

1. The row K3 and a done line in `docs/roadmap.md`, and one ledger entry.

## Scope (out)

- The speed of the dense solve (finding P1, register entry R-0027) stays a finding.
- No change to code. This task is not a product milestone: it adds no work, and the row names the tasks of the work.

## Acceptance criteria

- [x] The row K3 names TASK-0102, TASK-0110 and TASK-0117, and the gates pass.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k3-row-close) · v0.129

## Method

Mechanical: the text of the row K3, one done line and one ledger entry.
Judgement:  a governance entry, because the row closes and no work is new; the row names the three tasks of the work.
Weakest:    the review counter counts this entry as an entry only, not as a product milestone.

## Progress

- 2026-10-31: `SketchConstraints.Given` holds the fourteen kinds of ADR-0033 item 3 (read on disk).
- 2026-10-31: closed at v0.129.
