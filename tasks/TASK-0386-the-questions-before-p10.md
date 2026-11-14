---
id: 0386
title: The questions before P10 are a record, and the register holds them for the owner
status: Active
phase: P0.155
opened: 2026-11-14
depends-on: [0385]
governed-by: []
writes:
  create:
    - tasks/TASK-0386-the-questions-before-p10.md
    - docs/proposals/2026-11-14-the-sheet-in-the-document.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0386 — The questions before P10 are a record, and the register holds them for the owner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

After R22c (v0.369) each product row waits for P10, the sheet in the Document. P10 changes the public shape of
`Engine.Contracts`, so its ADR and a question to the owner come first. Under the order of the owner of 2026-11-14, "stop and
ask" becomes "record it and take the next task".

## Goal

The owner finds the questions of P10, each with a recommended option, in one place.

## Scope (in)

1. The proposal `docs/proposals/2026-11-14-the-sheet-in-the-document.md`, with Q1 to Q8.
2. Register entry R-0067, a question with a date.
3. The rows P0.155 and P10 of the roadmap.

## Scope (out)

- An ADR or code of P10.

## Acceptance criteria

- [ ] The proposal exists, and R-0067 names it.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the register entry and the rows.
Judgement:  the questions and their recommended options.
Weakest:    no answer is taken in the place of the owner, so P10 waits.

## Progress

- 2026-11-14: opened after P0.154, branch p0155-sheet-proposal. R-0067 is free; the register holds 13 of 15 before it.
