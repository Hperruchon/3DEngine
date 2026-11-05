---
id: 0190
title: A document whose log holds a datum on a face that a cut removed opens again
status: Ready
phase: P0.82
opened: 2026-11-05
depends-on: [0189]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0037, 0051, 0059, 0060]
writes:
  create:
    - tasks/TASK-0190-an-older-datum-on-a-gone-face-opens.md
  modify:
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Tests/Sketch/FaceGoneTests.cs
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0190 — A document whose log holds a datum on a face that a cut removed opens again

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-05 (b) found E134 (high): since K4c (v0.187) `CreateDatumCommandHandler` refuses a
datum on a face that a cut removed, also in the replay of a log. A client of v0.186 or earlier could record such a
datum, so the document no longer opens. By `docs/templates.md`, section 7, a high finding comes before the next
product phase. The standing order of 2026-11-05 answers Q1 of the review with A. The first commit of this task corrects
the write set and the field `governed-by` for the files that its tests need.

## Goal

A log with a `CreateDatum` on a face that a cut removed opens, and the datum has the fault `W-DATUM-REFERENCE-FAILED`.
A new command keeps the refusal.

## Scope (in)

1. In a replay (`SketchResults.ApplyingEntry`), the refusal of a gone face does not act, and the datum gets the fault.
2. A test of the open of such a log, which fails before the change, and a test that a new command is still refused.
3. A line of ADR-0059, "Older documents", and its history line.

## Scope (out)

- A migration of older documents. A change of the log.

## Acceptance criteria

- [ ] The test of the open fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test of the open before the change.
Judgement:  the fault of the datum in a replay.
Weakest:    to be written at the close.

## Progress

- 2026-11-05: opened by the codebase review of 2026-11-05 (b).
