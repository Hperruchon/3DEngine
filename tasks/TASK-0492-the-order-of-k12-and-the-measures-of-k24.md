---
id: 0492
title: The roadmap records that K12a and K12b came before K24, and the measures of the pieces of the lamp case
status: Done
phase: P0.202
opened: 2026-11-28
depends-on: [0491]
governed-by: []
writes:
  create:
    - tasks/TASK-0492-the-order-of-k12-and-the-measures-of-k24.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
---

# TASK-0492 — The roadmap records that K12a and K12b came before K24, and the measures of the pieces of the lamp case

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The order of 2026-11-25 says: "K24 comes before K12, because the travel of the pivot needs the pair check of the parts with holes".
The session of 2026-11-28 did K12a (TASK-0490) and K12b (TASK-0491) before K24, and found the order only after. The travel refuses
each pair above the bound of work of ADR-0182 item 8, as `CheckInterference` does, so it is right, but the lamp case of K10b has no
travel until K24. A false order in the record is a fault of the session; the roadmap must say what happened.

## Goal

The order and the row K24 say that K12a and K12b came first, and the row K24 gives the measures of 2026-11-28 for its ADR.

## Scope (in)

1. A sentence in item 1 of the section "Order" of `docs/roadmap.md`.
2. The measures in the row K24.

## Scope (out)

- K24 itself: its ADR decides the method (fewer pieces, or a faster search and a larger bound), and the owner can choose.

## Acceptance criteria

- [x] The order and the row K24 hold the facts, each read in its file or measured in this session.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch p0202-order-of-k12) · v0.476

## Method

Mechanical: a probe test that was not committed: the count of the pieces, of the planes and of the face names of each body of
            `LampSupportCase`, and the time of `NearestPoints.Nearest` for three pairs, in a Debug build on the computer of the owner.
Judgement:  none.
Weakest:    the times come from one run of a Debug build.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after K12b (v0.475). Ledger v0.476.
- 2026-11-28: run 46163933317 of `319183fd` passed on each job. Closed at v0.476.
