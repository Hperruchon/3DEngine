---
id: 0494
title: The role of a body and the print export, one STL file for each part to print with a check of its mesh
status: Active
phase: P14
opened: 2026-11-28
depends-on: [0492]
governed-by: []
writes:
  create:
    - tasks/TASK-0494-the-role-of-a-body-and-the-print-export.md
    - docs/proposals/2026-11-28-the-role-of-a-body-and-the-print-export.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0494 — The role of a body and the print export, one STL file for each part to print with a check of its mesh

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P14 (ADR-0126 items 6 and 7). The role is a new field of `BodyRecord`, so it changes the public shape of `Engine.Contracts`, and
`CLAUDE.md` ("Stop and ask") asks the owner first. So the first commit is a proposal with questions, and the forbid holds
`Engine.Contracts/**` until the answers.

## Goal

A body has a role, and the export gives one checked STL file for each part to print, with an ADR in the same commit as the change of
`Engine.Contracts`.

## Scope (in)

1. The proposal `docs/proposals/2026-11-28-the-role-of-a-body-and-the-print-export.md`: the facts, a recommended model and the
   questions Q1 to Q5.
2. After the answers: the ADR, the field, the command, the export and a test of each rule. The write set grows then.

## Scope (out)

- The envelope (K13) and the 3MF export.

## Acceptance criteria

- [x] The proposal gives each fact with its file, and each question with a recommended option.
- [ ] The owner answers Q1 to Q5, and the first commit after the answers records them.
- [ ] The ADR and the change of `Engine.Contracts` are in one commit, with a test of each rule.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: the facts of section 2 of the proposal, each read in its file.
Judgement:  the recommended model; a proposal and not a decision, because the change of the public shape needs the owner.
Weakest:    the check of a closed mesh is not yet in code or in a test.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28; the proposal with the questions Q1 to Q5. Not merged before the
  answers.
