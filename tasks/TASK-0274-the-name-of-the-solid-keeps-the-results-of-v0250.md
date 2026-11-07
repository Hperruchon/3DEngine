---
id: 0274
title: The name of the solid keeps the results of v0.250, and the test sheet says how the size comes back
status: Active
phase: P0.113
opened: 2026-11-07
depends-on: [0273]
governed-by: [0004, 0083]
writes:
  create:
    - tasks/TASK-0274-the-name-of-the-solid-keeps-the-results-of-v0250.md
  modify:
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Import/StlImportTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/demo-stl-test-sheet.md
    - tasks/TASK-0271-desktop-forms-of-the-twenty-ninth-review.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0274 — The name of the solid keeps the results of v0.250, and the test sheet says how the size comes back

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of v0.254 found a high defect in the fix of E173 (TASK-0269): the name of the solid ended at "facet
normal" and one number, so a file whose name is "facet normal 0" gave `E-STL-SYNTAX`, and the reader of v0.250 to
v0.253 read it. ADR-0083 item 3 says that a change of the reader keeps the result of each file that an earlier reader
accepted. The reviews of Codex of v0.253 and v0.254 also found that the test sheet tells the person to select the body to
see its size again, and a selection alone does not bring back the line "Size". The review of Codex of v0.253 found that
TASK-0271 could run before the product phase that it must follow. A high defect comes before the next product phase.

## Goal

Each file that the reader of v0.243 or of v0.250 accepted gives its result again, the test sheet says how the size comes
back, and TASK-0271 comes after this task.

## Scope (in)

1. The name ends at the start of a whole facet: "facet normal", three numbers and "outer".
2. The test sheet: turn on "Dimensions" and select the body.
3. `depends-on` of TASK-0271.

## Scope (out)

- The other leads of Codex of v0.250 to v0.255 and of the review draft after v0.243: TASK-0271 and TASK-0272.

## Acceptance criteria

- [ ] A test of the forms that the reader of v0.250 read fails with the rule of TASK-0269 and passes with the reader of
      v0.250 and with the new rule; the test of the forms of v0.243 still passes.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the new test runs with the reader of HEAD, of v0.250 (`60a9ea1`) and with the new rule.
Judgement:  a whole facet on the line of "solid" had two earlier results (v0.243 read it, v0.250 took it as the name);
            the rule gives the result of v0.243, which was in force longer, and the history of ADR-0083 says so.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: the rule of a whole facet, the test of four forms of v0.250, the sheet and the order of TASK-0271. The new test
  failed with the rule of TASK-0269 (`E-STL-SYNTAX` for "facet normal 0") and passed with the reader of `60a9ea1`; each of
  the 26 tests of the STL import passes with the new rule.
