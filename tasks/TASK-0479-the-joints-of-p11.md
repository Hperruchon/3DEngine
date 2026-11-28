---
id: 0479
title: The joints of P11, a record of a joint between two bodies in a kinematic tree with no solver
status: Active
phase: P11a
opened: 2026-11-28
depends-on: [0477]
governed-by: []
writes:
  create:
    - tasks/TASK-0479-the-joints-of-p11.md
    - docs/proposals/2026-11-28-the-joints-of-p11.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0479 — The joints of P11, a record of a joint between two bodies in a kinematic tree with no solver

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P11 of the roadmap: joints in the Document. P11.0 (TASK-0474, ADR-0191) gave each body a placement. The order of the review
of 2026-11-28 (TASK-0475, the answer Q1 A) puts K25 and P0.193 next, then R34 and R35, which wait for a check on the phone, so
P11 is the next phase that needs no person. The joints change the public shape of `Engine.Contracts`, and `CLAUDE.md` ("Stop and
ask") asks the owner before such a change. So the first commit of this task is a proposal with the questions, and the forbid
holds `Engine.Contracts/**` until the answers.

## Goal

The Document holds joints between bodies in a kinematic tree, and each tight joint gives the placement of its child, with an
ADR in the same commit as the change of `Engine.Contracts`.

## Scope (in)

1. The proposal `docs/proposals/2026-11-28-the-joints-of-p11.md`: the facts, a recommended model and the questions Q1 to Q7.
2. After the answers: the ADR, the record, the commands and a test of each rule. The write set grows then.

## Scope (out)

- Each change of `Engine.Contracts` before the answers of the owner.
- The travel (K12), the envelope (K13) and the tools of the window (R23, R27).

## Acceptance criteria

- [x] The proposal gives each fact with its file, and each question with a recommended option.
- [x] The owner answers Q1 to Q7, and the first commit after the answers records them.
- [ ] The ADR and the change of `Engine.Contracts` are in one commit, with a test of each rule.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: the facts of section 2 of the proposal, each read in its file.
Judgement:  the recommended model; a proposal and not a decision under the standing order, because the change of the public
            shape needs the owner.
Weakest:    the formula of the place of a child (section 3 item 3) is not yet in code or in a test.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28; the proposal with the questions Q1 to Q7. Not merged before the
  answers.
- 2026-11-28: the owner answered "Yes, all A" for Q1 to Q7: a connector is a face or a named datum in the frame of the body;
  the two normals are opposite; the value of a pivot is a cosine and a sine; each command of a joint sets the placements of the
  subtree in the same step; a loose joint keeps its record; a command `PlaceByConnectors` records no joint; P11a is the record,
  the three kinds, the values, the limits, the tree and the refusals, and P11b is the loose state, the intended pairs and
  `PlaceByConnectors`. This commit records them.
