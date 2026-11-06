---
id: 0241
title: A save to a new name keeps a handle on the document at each moment
status: Ready
phase: P0.96
opened: 2026-11-06
depends-on: [0240]
governed-by: [0004, 0015]
writes:
  create:
    - tasks/TASK-0241-a-save-to-a-new-name-keeps-its-handle.md
  modify:
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Tests/Persistence/DocumentJournalTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0241 — A save to a new name keeps a handle on the document at each moment

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (d) found E162 (high), from the audit of Codex of the persistence and the determinism:
`DocumentJournal.MoveTo` releases the lock of the file during the move of a save, so a second host can append a line,
and the document then does not open. By `docs/templates.md`, section 7, a high finding comes before the next product
phase. The standing order of 2026-11-05 answers Q1 of the review with A.

## Goal

A save to a new name holds a handle on the document at each moment, so no second host can write between the old name
and the new one.

## Scope (in)

1. E162 (Q1 A): the journal makes the file at the new name with no share for a write, copies the lines of the old file
   into it, flushes it, and deletes the old file; the stream of the new file stays open.
2. A test proves that a second open of the new name fails during the save, and that the document opens after it.

## Scope (out)

- E163 and the other findings of the review (TASK-0242, TASK-0243).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (d).
