---
id: 0403
title: The answers of the owner of 2026-11-18 on the 2D views are a record, with ADR-0162, the rows R30a to R30i and the new order
status: Done
phase: P0.163
opened: 2026-11-18
depends-on: [0402]
governed-by: []
writes:
  create:
    - tasks/TASK-0403-the-answers-of-2026-11-18.md
    - docs/adr/0162-the-workflow-of-the-2d-views.md
  modify:
    - docs/adr/0118-the-2d-drawing-of-a-part.md
    - docs/adr/0157-the-foundations-and-the-order-of-the-assembly-track.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - docs/register.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0403 — The answers of the owner of 2026-11-18 on the 2D views are a record, with ADR-0162, the rows R30a to R30i and the new order

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-17 the owner asked for a workflow to see and design the 2D views of a part, and a side session wrote the design
file `docs/proposals/2026-11-18-2d-view-workflow.md` and its answer sheet (TASK-0402). On 2026-11-18 the owner answered in the
chat of that session: "1a 2a 3a 4a 5a/b 6a 7a 8a 9a 10a 11a 12a" and "13a 14a 15a 16a 17a 18a 19a". To the question where
the ADR and the rows get written, the owner answered "Main session". The owner then chose to merge the branch of the design
file with this record ("Merge it").

## Goal

The answers are a record: one ADR, the roadmap rows of the slices, and the new order.

## Scope (in)

1. The merge of the branch `claude/design-2d-views-8762fa` (TASK-0402): the design file, the answer sheet and its task.
2. ADR-0162, which amends ADR-0118 item 7 (the hatch of a section and a note of the general tolerances) and ADR-0157 item
   11 (the order after P10c).
3. The roadmap: the rows R30a to R30i, the row P0.163, and the sentence of the order.

## Scope (out)

- Each change of code and of `Engine.Contracts`: each slice opens its task and its ADR.
- A register entry: the register is full (15 of 15). R-0068 keeps its text until a slice proves a pictorial view.

## Acceptance criteria

- [x] ADR-0162 holds each answer, with Q5 read as A and B together and Q16 A in place of Q12 A.
- [x] Each slice of the design file has its roadmap row, with its size and its place in the order.
- [x] The write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch and on `main` after
      the merge.

## Outcome

Status: Done · commit (the merge of the branch p0163-2d-view-answers) · v0.386

## Method

Mechanical: the rows and the sentence of the order, from sections 8 and 13.3 of the design file.
Judgement:  one ADR for the record, which amends only the two texts that change now; each contract change waits for the
            ADR of its slice.
Weakest:    the reading of "5a/b" as both options together; the design session asked the owner and no correction came.

## Progress

- 2026-11-18: opened under the answers of the owner of 2026-11-18, branch p0163-2d-view-answers from `main` at v0.385. The
  session read the answers of the owner in the transcript of the design session before it started. The branch of the
  design file merged into this branch first, with no conflict.
- 2026-11-18: run 44200951503 passed on the branch. Closed at v0.386.
