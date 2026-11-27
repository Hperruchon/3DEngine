---
id: 0474
title: The model of placements, so that a move changes a stored placement and not the shape of a body
status: Active
phase: P11.0
opened: 2026-11-27
depends-on: [0468]
governed-by: []
writes:
  create:
    - tasks/TASK-0474-the-model-of-placements.md
    - docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0474 — The model of placements, so that a move changes a stored placement and not the shape of a body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P11 of the roadmap: P11.0 is the ADR of the joints, and it decides the model of a move from the measure of K10 (ADR-0126
items 2 and 3, ADR-0157 items 2 to 5, R-0070). On 2026-11-25 the owner answered Q1 A: P11.0 decides a model of placements. Each
body keeps its shape in its own frame, and a joint or a move changes only a stored placement, so the exact numbers do not grow.
K10a measured a numerator of 2576 digits after 150 turns.

The standing order of 2026-11-26 puts P11.0 after P0.189. The model changes the public shape of `Engine.Contracts`, and
`CLAUDE.md` ("Stop and ask") asks the owner before such a change. The direction is answered; its details are not. So the
first commit of this task is a proposal with the questions, and the forbid holds `Engine.Contracts/**` until the answers.

## Goal

The Document stores a placement for each body, a command sets it, and each reader of the geometry applies it, so that a move
keeps the identity and the exact shape of a body, with an ADR in the same commit as the change of `Engine.Contracts`.

## Scope (in)

1. The proposal `docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md`: the facts, a recommended model and the
   questions Q1 to Q7 for the owner.
2. After the answers: the ADR, the field of `BodyRecord`, the command, the change of each reader, and a test of each reader
   with a body that is not at the identity. The write set grows then.

## Scope (out)

- The connectors, the kinds of joint, the limits and the refusal of a loop: P11a and after.
- Each change of `Engine.Contracts` before the answers of the owner.

## Acceptance criteria

- [x] The proposal gives each fact with its file, and each question with a recommended option.
- [ ] The owner answers Q1 to Q7, and the first commit after the answers records them.
- [ ] The ADR and the change of `Engine.Contracts` are in one commit, with a test of each reader.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Notes for the implementer

- The branch `p110-placements` starts at `ea2f4dac` (v0.456). Do not merge it before the answers.
- The case of K10a (`docs/proposals/2026-11-21-the-bookcase-case-of-k10.md`) gives the numbers to measure again (Q7).

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: the facts of section 2 of the proposal, each read in its file.
Judgement:  the recommended model; a proposal and not a decision under the standing order, because the change of the public
            shape needs the owner.
Weakest:    The list of the readers of the geometry in section 4 of the proposal comes from the names of the features. No
            search of each reader was done yet.

## Progress

- 2026-11-27: opened under the standing order of 2026-11-26; the proposal with the questions Q1 to Q7.
