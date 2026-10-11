---
id: 0063
title: The decisions of the owner of 2026-10-11 are a record
status: Done
phase: governance
opened: 2026-10-11
depends-on: [0060]
governed-by: []
writes:
  create:
    - tasks/TASK-0063-owner-decisions-2026-10-11.md
    - tasks/TASK-0064-repository-gates-run-from-main.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - tasks/TASK-0055-event-payload-schemas.md
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

# TASK-0063 — The decisions of the owner of 2026-10-11 are a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-11 the owner answered the two questions of the codebase review of 2026-10-11:

1. Q1, yes: the gates that read the repository move out of the test assembly into a program that the
   pipeline builds from `main` (finding T31, option B).
2. Q2, option A: the project accepts that a commit that names `.github/workflows/ci.yml` exactly can
   change the job that judges it (findings T28 and T38).

## Goal

Each decision is in a task, a register entry or the roadmap.

## Scope (in)

1. TASK-0064 holds the answer to Q1, after TASK-0061.
2. Register entry R-0038, under "Accepted compromises", holds the answer to Q2 with its reason.
3. R-0031 records both answers.
4. The roadmap gives P0.25 and the new sum.
5. One ledger entry, v0.60.

## Scope (out)

- No code.

## Acceptance criteria

- [x] `TaskGovernanceGateTests` passes with TASK-0064 `Ready`.
- [x] `RegisterGateTests` passes with R-0038 under "Accepted compromises".
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.60 · the commit that carries this block.

## Method

**Judgement.** TASK-0064 comes after TASK-0061 and before R2, because T31 is high and the class reaches
each gate that reads the repository. It comes after TASK-0062 in the roadmap, because the gaps of
TASK-0062 touch the address check of each host and the move of the gates is larger. R-0038 is an
accepted compromise and not an open entry, so it does not age and does not count against the limit of
15; its reason is the one that the review gave.

## Progress

- 2026-10-11: opened and closed in one commit.
