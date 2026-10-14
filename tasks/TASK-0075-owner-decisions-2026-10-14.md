---
id: 0075
title: The decisions of the owner of 2026-10-14 are a record
status: Done
phase: governance
opened: 2026-10-14
depends-on: [0072]
governed-by: []
writes:
  create:
    - tasks/TASK-0075-owner-decisions-2026-10-14.md
    - tasks/TASK-0076-observe-a-cut.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/**
    - CLAUDE.md
    - docs/adr/**
---

# TASK-0075 — The decisions of the owner of 2026-10-14 are a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-14 the owner answered the three questions of the codebase review of 2026-10-13 with "A to all,
then continue the batch":

1. Q1, option A: TASK-0073 (P0.28) comes before R6.
2. Q2, option A for each of the six decisions of R6: the demonstration with an offset of (5, 5, 5), a new
   project `3DEngine.ImageTests`, lavapipe on Ubuntu with a skip elsewhere, a structural comparison, a
   camera that frames the live bodies, and a script of the demonstration.
3. Q3, option A: R6 needs 6 to 8 evenings.

## Goal

Each decision is in a task or in the roadmap.

## Scope (in)

1. The roadmap puts P0.28 before R6, names TASK-0076 for R6, and gives R6 6 to 8 evenings.
2. TASK-0076 holds the six decisions of Q2. Its first commit writes them as ADR-0025.
3. One ledger entry, v0.72.

## Scope (out)

- No code and no ADR.

## Acceptance criteria

- [x] `TaskGovernanceGateTests` passes with TASK-0076 `Ready`.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.72 · the commit that carries this block.

## Method

**Judgement.** The decisions of Q2 go into ADR-0025 in the first commit of TASK-0076, with the change of the
dependency gate and of ADR-0022 that they need. TASK-0076 depends on TASK-0073, which is the order of Q1.

## Progress

- 2026-10-14: opened and closed in one commit.
