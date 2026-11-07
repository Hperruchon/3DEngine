---
id: 0266
title: The test sheet of the STL demonstration
status: Active
phase: P9e
opened: 2026-11-07
depends-on: [0260]
governed-by: []
writes:
  create:
    - tasks/TASK-0266-the-test-sheet-of-the-stl-demonstration.md
    - docs/demo-stl-test-sheet.md
  modify:
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0266 — The test sheet of the STL demonstration

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The demonstration "Open the same STL on desktop and Android" (ADR-0083, the owner, 2026-11-07) closes only after a run
of the owner on the desktop and on the Galaxy S23 Ultra. Row e of the order of the owner asks for the test sheet of that
run, from the page of the demonstration (https://claude.ai/artifact/NJVBcQYiheLRCcRgx9YQad).

## Goal

The owner does the run from one document: the setup, the six steps, the ten pass criteria, an observation sheet for the
S23 and the known limits of the run.

## Scope (in)

1. `docs/demo-stl-test-sheet.md`, from the page of the demonstration and from the code of TASK-0258 to TASK-0260.
2. The line of the sheet in `docs/INDEX.md`, and the name of this task in the row P9e of `docs/roadmap.md`.

## Scope (out)

- The run itself. The rows P9c, P9d and P9e close only after the run of the owner, as R14e does.

## Acceptance criteria

- [ ] The sheet names each control by the label that the code draws: "Open file", "New part", "Frame", "Home" and
      "Choose another file".
- [ ] The sheet gives the sizes of both files as the repository holds them.
- [ ] Criterion 10 is ticked, because the pipeline runs the probe of TASK-0258 at each push.
- [ ] The gates of the repository pass.
- [ ] The run of the owner on the desktop and on the S23 passes the criteria of the sheet.

## Outcome

Status: Active

## Method

Mechanical: the labels and the file sizes are read from the code and from `testdata/stl/` of this branch.
Judgement:  the order of the steps and the criteria come from the page; the known limits come from ADR-0083 and from
            the emulator runs of this session.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: the sheet, its index line and the row P9e.
