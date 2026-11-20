---
id: 0427
title: The scenarios of R20a and R20c in the window, and the answer of the owner on the ring of the document
status: Active
phase: P0.170
opened: 2026-11-20
depends-on: [0422]
governed-by: []
writes:
  create:
    - tasks/TASK-0427-the-window-scenarios-of-r20a-and-r20c.md
    - docs/adr/0178-show-hidden-on-the-ring-of-the-part-and-the-path-line.md
    - tasks/TASK-0424-show-hidden-on-the-ring-of-the-part.md
  modify:
    - docs/adr/0124-the-selection-after-a-hide-and-the-hidden-bodies-in-the-status-line.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0427 — The scenarios of R20a and R20c in the window, and the answer of the owner on the ring of the document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entries R-0063 and R-0065 (due 2026-11-25 and 2026-11-26) wait for a run of the scenarios of R20a and R20c in the
window, and R-0064 (due 2026-11-26) waits for the owner. On 2026-11-20 the owner ordered a run until 2026-11-22 09:00, granted
the control of the window for the two scenarios, and chose "On the path line" for R-0064.

## Goal

The results of the two scenarios and the answer on R-0064 are a record, and each failure has a roadmap row.

## Scope (in)

1. Close R-0063 (the scenario passes) and R-0064 (ADR-0178). R-0065 stays open with the result, until R20d.
2. ADR-0178, which amends ADR-0124 items 2 and 4.
3. The roadmap: the rows P0.170, R20d and R20e, and the sentence of the order.
4. TASK-0424 (R20e) as a Ready task, because ADR-0178 names its test (`docs/templates.md`, section 1).

## Scope (out)

- Each change of code: R20d and R20e open their tasks.
- The pictures of the scenarios: they stay out of the repository, as each picture of a visual check.

## Acceptance criteria

- [x] The register holds the result of each scenario, and its count of open entries falls from 14 to 12.
- [x] ADR-0178 records the answer, with the reading of the answer and its reason.
- [ ] The write-set check, `eng/repo-gates` and `dotnet test` pass, and continuous integration passes on the branch.

## Outcome

Status: Active · commit (the merge of the branch p0170-window-scenarios) · v0.407

## Method

- Built the desktop host of v0.405 (`8dbaafb`) and started it with `--file` on a copy of the folder
  `Documents/3DProjects/Bibliotheque120` in the scratchpad, at 1280 by 720. Controlled the window by computer use, with the
  grant of the owner for that program only.
- R20a: selected "Fond" in the list of the bodies, zoomed with the wheel, clicked a module under the tile of Move away from
  its icon and its label, then clicked the icon of Move.
- R20c: selected "Cote gauche" in the list, turned on "Dimensions" on the ring, and looked at each label with a zoom of the
  picture.
- Mechanical: the register entries and the rows. Judgement: the reading of the answer on R-0064 (ADR-0178, "Bad").

## Progress

- 2026-11-20: opened under the order of the owner of 2026-11-20, branch p0170-window-scenarios from `main` at `8dbaafb`.
- 2026-11-20: the scenarios ran in the window; the register, ADR-0178 and the rows. Ledger entry v0.407.
- 2026-11-20: run 44816204676 of `218dac6`, the first branch, passed on each job.
- 2026-11-20: a parallel session merged R30f as TASK-0422, ADR-0177 and v0.406. This task became TASK-0427 with ADR-0178
  and v0.407 on the branch p0170-window-scenarios-b from `main` at `05aafda`; the first branch is not merged.
