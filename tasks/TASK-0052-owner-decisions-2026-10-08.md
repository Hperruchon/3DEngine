---
id: 0052
title: The decisions of the owner of 2026-10-08 are a record
status: Done
phase: governance
opened: 2026-10-08
depends-on: [0037]
governed-by: []
writes:
  create:
    - tasks/TASK-0052-owner-decisions-2026-10-08.md
    - tasks/TASK-0053-write-set-check-outside-the-tests.md
    - tasks/TASK-0054-desktop-serves-the-surface.md
    - tasks/TASK-0055-event-payload-schemas.md
  modify:
    - tasks/TASK-0038-hybrid-topology.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - CLAUDE.md
    - docs/adr/**
---

# TASK-0052 — The decisions of the owner of 2026-10-08 are a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

After v0.49 the agent gave three recommendations, and on 2026-10-08 the owner answered "follow
recommendation":

1. Register entry R-0037: `/schema/events` gives the payload fields of each event kind, in a task after
   TASK-0038.
2. Question Q1 of the codebase review of 2026-10-08 (finding T14): option A. The dynamic write-set check
   runs as a program in `eng/` that the pipeline builds from `main`, as the next task, before TASK-0038.
3. TASK-0038 splits in two: the library and the program first, then the desktop host.

## Goal

Each decision is in a task, a register entry or the roadmap, and the order is in the field
`depends-on`, which a gate reads.

## Scope (in)

1. R-0037 closes with the outcome "decided", ADR-0008. ADR-0008 §9 already gives the payload schemas,
   so the entry was a decision and not a question.
2. R-0031 records the answer to Q1.
3. Three new tasks: TASK-0053 (the write-set check outside the tests), TASK-0054 (the desktop host
   serves the surface) and TASK-0055 (the payload fields).
4. TASK-0038 keeps the library and the program, and depends on TASK-0053.
5. The roadmap gives P0.17 to P0.20, the order and the new sum.
6. One ledger entry, v0.50.

## Scope (out)

- No code. No change to an ADR or to `CLAUDE.md`.

## Acceptance criteria

- [x] `TaskGovernanceGateTests` passes with the three new tasks `Ready`, so each `governed-by` field
      agrees with the ADRs.
- [x] `RegisterGateTests` passes with 14 open entries.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.50 · the commit that carries this block.

## Method

**Mechanical.** The `governed-by` field of each new task comes from `TaskGovernanceGateTests`.

**Judgement.** The split of TASK-0038 puts the in-process test of the surface, which ADR-0019 names in
`enforced-by`, in the first half, because the library alone makes it possible. The address checks of
TASK-0051 move into the library, so the desktop uses them and keeps no copy. TASK-0055 comes after
TASK-0054, as recommended, and the roadmap says that R5 does not need it.

**Weakest.** My recommendation said that a yes to R-0037 needs an amendment of ADR-0008 §9. That was
false: §9 already says "index of event kinds + payload schemas". I read the ADR only after the owner
answered. The code has disagreed with ADR-0008 since `/schema/events` exists, and the closed entry says
so. The first version of TASK-0038 named sections of `CLAUDE.md` that do not exist; the new version
corrects it.

## Progress

- 2026-10-08: opened and closed in one commit.
