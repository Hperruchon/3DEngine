---
id: 0068
title: The decisions of the owner of 2026-10-12 are a record
status: Done
phase: governance
opened: 2026-10-12
depends-on: [0065]
governed-by: []
writes:
  create:
    - tasks/TASK-0068-owner-decisions-2026-10-12.md
    - tasks/TASK-0069-first-render-pipeline.md
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

# TASK-0068 — The decisions of the owner of 2026-10-12 are a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-12 the owner answered the two questions of the codebase review of 2026-10-12 with "A to
all, then continue the batch":

1. Q1, option A: TASK-0066 (P0.26) comes before R3.
2. Q2, option A for each of the four decisions of R3: the GLSL source and the SPIR-V in the repository
   with a test of the hash, the render pass that exists, a new test project `3DEngine.Tests`, and the
   computer of the owner as the platform of R3.

## Goal

Each decision is in a task or in the roadmap.

## Scope (in)

1. The roadmap puts P0.26 before R3, and names TASK-0069 for R3.
2. TASK-0069 holds the four decisions of Q2. Its first commit writes them as ADR-0022, because an ADR
   needs the code that it governs in reach, and this task forbids `docs/adr/**`.
3. One ledger entry, v0.66.

## Scope (out)

- No code and no ADR.

## Acceptance criteria

- [x] `TaskGovernanceGateTests` passes with TASK-0069 `Ready`.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.66 · the commit that carries this block.

## Method

**Judgement.** The decisions of Q2 go into ADR-0022 in the first commit of TASK-0069 and not here: that
commit also changes the dependency gate and the ADRs that it amends, so the record and the rule that it
changes land together. TASK-0069 depends on TASK-0066, which is the order of Q1.

## Progress

- 2026-10-12: opened and closed in one commit.
