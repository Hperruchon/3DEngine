---
id: 0343
title: The 2D drawing of a part has a date in the charter, with ADR-0118 and its roadmap rows
status: Active
phase: P0.142
opened: 2026-11-10
depends-on: [0341]
governed-by: [0055, 0118]
writes:
  create:
    - tasks/TASK-0343-the-2d-drawing-in-the-charter.md
    - docs/adr/0118-the-2d-drawing-of-a-part.md
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

# TASK-0343 — The 2D drawing of a part has a date in the charter, with ADR-0118 and its roadmap rows

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked if the app has a 2D drawing export, then asked for renders of a drawing and a comparison
with the app. The owner answered the five questions of the renders with the recommended options, and then chose
"Drawing roadmap first" before R20a.

## Goal

The charter, an ADR and the roadmap hold the answers of the owner, so a later session can open the first phase of the
drawing with no question.

## Scope (in)

1. Objective 18 of `docs/CHARTER.md` and the list of the non-goals: 2D drawing and technical drawing of a part have a date;
   electrical schematics stay a direction.
2. ADR-0118 with the answers Q1 to Q5 and the order of the work.
3. The roadmap rows R21 and R22 (Track R), K9 (Track K), and P10, P10a, P10b and P10c (Track P).
4. The ledger entry.

## Scope (out)

- Code. Each phase opens its task at its start.
- The ADR of the sheet in the Document (P10) and the ADR of the edges of a view (K9).

## Acceptance criteria

- [ ] `ObjectiveReferenceGateTests`, `AdrGateTests` and `TaskGovernanceGateTests` pass.
- [ ] The write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on `main` after the
      merge.

## Method

Mechanical: the text of the answers of the owner.
Judgement:  the tracks of the rows; the order of the phases after R18.
Weakest:    the estimates of the evenings of P10 and K9 come before their ADRs.

## Progress

- 2026-11-10: opened with the record, as TASK-0193 did for Android; the ledger entry waits for the merge of R18c (v0.325). The numbers TASK-0343 and ADR-0118 were
  reserved with the session of R18c and R18d at `main` 6b614f1. ADR-0118 affects `docs/CHARTER.md` only, so no open task
  that writes the roadmap needs it in `governed-by`.
