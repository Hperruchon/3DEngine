---
id: 0297
title: The arc of an agent proposal is a proposal for the owner
status: Active
phase: governance
opened: 2026-11-08
depends-on: [0292]
governed-by: []
writes:
  create:
    - tasks/TASK-0297-the-proposal-of-the-agent-arc.md
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
  modify:
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
---

# TASK-0297 — The arc of an agent proposal is a proposal for the owner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner chose the agent proposal arc as the first redesign slice that changes the contract. The arc
changes the HTTP surface that an agent reads, so the standing order records the shape and does not answer it.

## Goal

The owner finds the options of the arc in one record, and answers it in one step.

## Scope (in)

1. ADR-0092, Proposed: where a proposal lives, the preview, the count of proposals and the undo; each option with its
   cost, and the slices that follow the answers.

## Scope (out)

- Code. A change of the HTTP surface or of `Engine.Contracts`. Each waits for the answer of the owner.

## Acceptance criteria

- [ ] `AdrGateTests` passes with the Proposed record.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the routes of ADR-0030 and the pattern of the contour proposal of ADR-0047, read on disk.
Judgement:  the options and the recommendations are mine; the brief of 2026-11-04 and the designer brief give the arc.
Weakest:    no agent ran the arc, so the shape of a proposal on the wire is not tried.

## Progress

- 2026-11-08: ADR-0092, Proposed.
