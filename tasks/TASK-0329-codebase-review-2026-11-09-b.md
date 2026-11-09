---
id: 0329
title: The codebase review of 2026-11-09 (b) is a record
status: Done
phase: governance
opened: 2026-11-09
depends-on: [0327]
governed-by: []
writes:
  create:
    - tasks/TASK-0329-codebase-review-2026-11-09-b.md
    - docs/reviews/2026-11-09-b-codebase-review.md
    - tasks/TASK-0330-the-high-finding-of-the-review-of-2026-11-09-b.md
    - tasks/TASK-0331-medium-and-low-findings-of-the-review-of-2026-11-09-b.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.App/**
    - 3DEngine.Android/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - 3DEngine.VisionTests/**
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0329 — The codebase review of 2026-11-09 (b) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (V0b, R16q, R16r and R16s) and six ledger entries follow the review of v0.304, so the next product
milestone would make the review late. On 2026-11-09 the owner asked for the review, then the pending chip of a proposal.

## Goal

The review of `main` at v0.310 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V159 to V163, E198, E199 and T180.
2. TASK-0330 (P0.138) for the high finding V159, before the next product phase.
3. TASK-0331 (P0.139) for the medium and low findings, after the next product phase.

## Scope (out)

- A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.

## Outcome

Status: Done · this commit · v0.311

## Method

Mechanical: the measures by the script of the last review, and a run of each probe of the agent by the session.
Judgement:  the severity of each new finding; V159 is high because it is in the default window.
Weakest:    the lines of the old findings that v0.305 to v0.310 did not change are carried, not examined again; V161 is
            confirmed by reading only, because its probe needs the window.

## Progress

- 2026-11-09: one agent examined the change from v0.304 to v0.310 with probes; the session ran each probe again and read each
  finding in the code.
