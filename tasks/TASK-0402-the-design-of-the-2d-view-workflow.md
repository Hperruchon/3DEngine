---
id: 0402
title: A design file gives the workflow to see and design the 2D views of a part, with questions for the owner
status: Done
phase: governance
opened: 2026-11-18
depends-on: [0401]
governed-by: []
writes:
  create:
    - tasks/TASK-0402-the-design-of-the-2d-view-workflow.md
    - docs/proposals/2026-11-18-2d-view-workflow.md
    - docs/proposals/2026-11-18-answer-sheet.md
  modify: []
  forbid:
    - docs/CHARTER.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0402 — A design file gives the workflow to see and design the 2D views of a part, with questions for the owner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-17 the owner saw the PDF of a sheet (v0.385) and the flat views of the app side by side, and said: "we should be
able to define the view for the 2d drawing so the app should be able to show a view similar to the sheet if not the sheet",
then "open a design session we need a workflow to properly see and design 2d view". The main session started a side
session for a design file to discuss before a task enters the roadmap.

## Goal

The owner has one file with the current state, the gaps, the options of each decision with a recommendation, the slices in
order with their size, and numbered questions with the recommended answer A; and one answer sheet.

## Scope (in)

1. `docs/proposals/2026-11-18-2d-view-workflow.md`.
2. `docs/proposals/2026-11-18-answer-sheet.md`.
3. This task file.

## Scope (out)

- No code, no ADR, no roadmap row, no ledger entry. A later task records the answers of the owner, with its ADR, its rows
  and its ledger entry, as TASK-0394 did for the design file of 2026-11-15.
- No register entry. The register holds 15 of 15 entries, and the main session said to add none without the owner. The
  questions are in the design file, section 9, and in the answer sheet.

## Done when

- The two files exist, in Simplified Technical English.
- `dotnet run --project eng/write-set-check -- --staged --task TASK-0402` and `dotnet run --project eng/repo-gates -- --root .`
  pass.
- The branch is pushed, not merged.

## Progress

- 2026-11-18 · The design file and the answer sheet, with twelve questions (commit b9f70ec).
- 2026-11-18 · The owner answered in the chat of the side session: "1a 2a 3a 4a 5a/b 6a 7a 8a 9a 10a 11a 12a". The answer
  sheet and the design file hold the answers; Q5 reads as A and B together. No ADR and no roadmap row record them yet.
- 2026-11-18 · The task is Done again after the record of the answers (commit fb35f47).
- 2026-11-18 · The owner asked for one coherent view, a drag, a section view, hidden and centre lines, and the conventions. Section 13 of the design file and block 2 of the answer sheet hold Q13 to Q19; the visual page is one working prototype.
- 2026-11-18 · The task is Done again after section 13.
