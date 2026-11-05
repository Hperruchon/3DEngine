---
id: 0187
title: A gate fails when an open task plans an ADR number that another ADR file or another open task has
status: Active
phase: P0.80
opened: 2026-11-05
depends-on: []
governed-by: []
writes:
  create:
    - tasks/TASK-0187-a-planned-adr-number-is-free.md
  modify:
    - Engine.Tests/Governance/AdrGateTests.cs
    - eng/test-floor.txt
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine/**
    - eng/write-set-check/**
---

# TASK-0187 — A gate fails when an open task plans an ADR number that another ADR file or another open task has

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0052. On 2026-11-05 TASK-0186 planned `docs/adr/0059-unit-kind-in-the-command-schema.md` in its
write set, and K4c (TASK-0185) committed `docs/adr/0059-references-by-the-identity-of-a-face.md` 12 minutes later. The
merge of the two was clean and each gate passed, because `AdrGateTests` reads only the ADR files that exist. A hand
change gave ADR-0060 to TASK-0186. Two sessions can work at the same time, so a rule in text does not stop the next
collision. The register holds 15 open entries, the maximum, and P8h (TASK-0186) must add one. On 2026-11-05 the owner
put this task before P8h.

## Goal

`AdrGateTests` fails when a task that is not Done plans to create an ADR file whose number another ADR file, in
`docs/adr/` or in `docs/adr/archive/`, or the plan of another task that is not Done already has.

## Scope (in)

- A new fact in `AdrGateTests`, with a pure function that a theory gives each form.
- The row of `AdrGateTests` in `docs/templates.md`, section 5.
- The close of R-0052 by the exit "Resolve".

## Scope (out)

- The numbers of tasks. A task file has its number in its name, and two task files of one number fail
  `TaskGovernanceGateTests` at the merge.
- A task that is Done. Its plan is history, and its file exists or its plan changed by decision.
- The write-set judge. It judges one commit, not the plans of other tasks.

## Acceptance criteria

- [ ] An injection of the collision of 2026-11-05 (TASK-0186 plans `docs/adr/0059-unit-kind-in-the-command-schema.md`)
      passes each gate before the change and fails the new fact after it.
- [ ] The new fact passes on the repository with no injection.
- [ ] A theory shows each form: a number of a file in `docs/adr/`, a number of a file in `docs/adr/archive/`, two
      open tasks with one number, the own file of an Active task that is no problem, and a Done task that is no problem.
- [ ] R-0052 is closed with the exit "Resolve".
- [ ] `dotnet test` passes; the write-set check and `eng/repo-gates` pass; the ledger has an entry.

## Notes for the implementer

- A planned path is a path under `create:` that matches `docs/adr/NNNN-*.md`. Its number is the first four digits of
  the file name.
- A plan is no problem when the existing file of its number has the same name. An Active task that made its ADR has
  that file.

## Progress

- 2026-11-05: opened from R-0052, before P8h, by the order of the owner.
- 2026-11-05: the fact, its theory and the fact of the names; 3 injections and 4 mutations each fail a test.
