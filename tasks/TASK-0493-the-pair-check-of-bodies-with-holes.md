---
id: 0493
title: The pair check of bodies with holes, with fewer pieces and a faster gap search
status: Active
phase: K24
opened: 2026-11-28
depends-on: [0492]
governed-by: []
writes:
  create:
    - tasks/TASK-0493-the-pair-check-of-bodies-with-holes.md
    - docs/proposals/2026-11-28-the-pair-check-of-bodies-with-holes.md
  modify: []
  forbid:
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0493 — The pair check of bodies with holes, with fewer pieces and a faster gap search

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K24: the bound of work of ADR-0182 item 8 refuses 8 of the 21 pairs of the lamp case. The row says that its ADR decides the
method. The method changes the time that one query can hold the session, so the first commit is a proposal with questions for the
owner, and the forbid holds the code until the answers.

## Goal

The pair check gives the class of each pair of the lamp case, with an ADR that decides the method.

## Scope (in)

1. The proposal `docs/proposals/2026-11-28-the-pair-check-of-bodies-with-holes.md`: the measures, a recommended method and the
   questions Q1 to Q3.
2. After the answers: the ADR, the code and the tests. The write set grows then.

## Scope (out)

- The order of K12 and K24: P0.202 (TASK-0492).

## Acceptance criteria

- [x] The proposal gives each fact with its source or its measure, and each question with a recommended option.
- [ ] The owner answers Q1 to Q3, and the first commit after the answers records them.
- [ ] `LampSupportCaseTests` gives the class of each pair.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: two probe tests that were not committed: the pieces of each body, a merge of the pieces that share an edge, and the
            time of the gap search for three pairs.
Judgement:  the recommended method; a proposal and not a decision, because the method changes the time of a query.
Weakest:    the times come from one run of a Debug build.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.202; the proposal with the questions Q1 to Q3. Not merged
  before the answers.
