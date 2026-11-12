---
id: 0353
title: Assemblies with joints and their geometric checks have a date in the charter, with ADR-0126 and its roadmap rows
status: Done
phase: P0.145
opened: 2026-11-12
depends-on: [0352]
governed-by: [0055, 0118, 0126]
writes:
  create:
    - tasks/TASK-0353-assemblies-in-the-charter.md
    - docs/adr/0126-assemblies-with-joints-and-geometric-checks.md
    - docs/proposals/2026-11-12-assembly-with-ai.md
  modify:
    - docs/CHARTER.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0353 — Assemblies with joints and their geometric checks have a date in the charter, with ADR-0126 and its roadmap rows

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-12 the owner asked to plan a work package "design and validate an assembly with the AI", with the tilting support
of a light as the reference case, after the current queue and the 2D drawing track up to P10c. The charter listed assemblies
and simulation as non-goals. The owner answered seven questions, D1 to D7, on 2026-11-12.

## Goal

The charter, an ADR, a proposal and the roadmap hold the answers of the owner, so a later session can open the first phase of
the assembly work with no question.

## Scope (in)

1. The proposal `docs/proposals/2026-11-12-assembly-with-ai.md`: what exists, the phases, their order and their acceptance.
2. ADR-0126 with D1 to D7.
3. Objectives 11 and 16 of the charter, and the list of the non-goals.
4. The roadmap rows K10 to K14, P11 to P14, R23 and R24, the row P0.145, and the note of the order.

## Scope (out)

- Each code change. Each phase opens its task, and its ADR, at its start.
- The checks R-0063 and R-0065 and the decision R-0064: they keep their state.

## Acceptance criteria

- [x] ADR-0126 exists and records D1 to D7.
- [x] `ObjectiveReferenceGateTests`, `AdrGateTests` and `TaskGovernanceGateTests` pass.
- [x] `eng/repo-gates` and the write-set check pass; continuous integration passes on the branch and on `main`.

## Outcome

Status: Done · commit (the merge of the branch assembly-roadmap) · v0.338

## Method

Mechanical: the rows and the record from the answers of the owner.
Judgement:  the order of the phases; the change of two objectives in place of a new one.
Weakest:    the estimates of the phases are from a reading of the code, before K10 models the case.

## Progress

- 2026-11-12: opened on the order of the owner, at `main` bf864f0 (v0.337), branch assembly-roadmap. TASK-0353, ADR-0126
  and P0.145 are free on `main` and on each branch. The objectives keep their count of twenty, because the gate reads the
  section "The twenty objectives" by its title: objectives 11 and 16 change, as objective 18 changed for the drawing.
- 2026-11-12: two citations of ADR-0112 in the first draft of the proposal were wrong (items 3 and 4); they are
  items 6, 7 and 9. `eng/repo-gates` passes. Closed at v0.338.
