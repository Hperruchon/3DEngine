---
id: 0040
title: Each ADR in force holds its current decision in one text, and the archive holds the rest
status: Ready
phase: governance
opened: 2026-09-26
depends-on: [0039]
governed-by: []
writes:
  create:
    - tasks/TASK-0040-living-adrs.md
    - docs/adr/archive/**
  modify:
    - docs/adr/0004-engine-runtime-is-authority.md
    - docs/adr/0007-ui-ephemeral-state-boundary.md
    - docs/adr/0011-server-default-deployment-topology.md
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0013-command-query-schema-declaration.md
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/adr/0003-blazor-as-thin-viewer.md
    - docs/adr/README.md
    - docs/INDEX.md
    - CLAUDE.md
    - docs/CURRENT-STATE.md
    - Engine.Tests/Governance/AdrGateTests.cs
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/CHARTER.md
    - docs/adr/0006-command-execution-model.md
    - docs/adr/0008-command-query-event-triad.md
    - docs/adr/0010-subscription-reset-snapshot-format.md
    - docs/adr/0018-tessellation-capability.md
    - docs/adr/0020-document-version-meaning.md
    - docs/adr/0021-operand-consumption.md
---

# TASK-0040 — Each ADR in force holds its current decision in one text, and the archive holds the rest

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner decided on 2026-09-26 that an ADR that evolved is updated with the code, and the code
with the ADR, and that an ADR that nobody needs is cleaned. `docs/templates.md`, section 1, "An ADR
agrees with the code", holds the rule since TASK-0039. `docs/reviews/2026-09-25-rules-review.md`,
section 4.7, gives the reason and the work list.

Seven ADRs have the status `Amended`, therefore a reader reads two files for one decision, and
three for ADR-0006. ADR-0007 lists `BlazorApp/**` in `affects`, and no such project exists. ADR-0003
is `Withdrawn` and stays in the index.

## Goal

Each ADR in force gives its current decision in its own text, and a withdrawn or superseded record
is in the archive.

## Scope (in)

Each fold changes a section "Decision", therefore the owner accepts each one. Make one commit for
each ADR, so that the owner reads one change at a time.

1. ADR-0003 moves to `docs/adr/archive/`. `docs/adr/README.md` lists its number under "Archived".
2. ADR-0004 folds the deployment default of ADR-0011 into its text and gains a section "History".
3. ADR-0007 loses `BlazorApp/**` in `affects`.
4. ADR-0019 supersedes ADR-0011: the fields `supersedes` and `superseded-by` are set, ADR-0011 gets
   the status `Superseded` and moves to the archive, and `amends`/`amended-by` between them are
   cleared. ADR-0004 then names ADR-0019 where it named ADR-0011. `CLAUDE.md` and `docs/INDEX.md`
   point at ADR-0019 only.
5. ADR-0012 folds its in-file "Amendment 1" into its sections 1 and 6 and gains a section
   "History". The amendments by ADR-0018 and ADR-0021 wait (see "Scope (out)").
6. ADR-0013 folds the construction step of ADR-0016 into its text and gains a section "History".
7. `AdrGateTests` accepts a row under "Archived" for a number with no file in `docs/adr/`, and it
   does not read `docs/adr/archive/`.

## Scope (out)

- ADR-0006, ADR-0008 and ADR-0010 wait for TASK-0035, because the code holds the old version
  meaning until that task ships. TASK-0035 folds them, as the rule says: the task that changes the
  decision changes the record.
- ADR-0012 waits for TASK-0028 (ADR-0018) and TASK-0037 (ADR-0021) for those two folds.
- ADR-0015 waits for register entry R-0025.
- No change to the text of a "Decision" without the acceptance of the owner.
- No change to engine code.

## Acceptance criteria

- [ ] `docs/adr/` holds no record with the status `Withdrawn` or `Superseded`.
- [ ] No ADR in force has the status `Amended` because of ADR-0011 or ADR-0016.
- [ ] Each folded ADR has a section "History" with one line for each fold.
- [ ] `AdrGateTests` passes, and an injected index row for an archived number with a file in
      `docs/adr/` fails.
- [ ] The owner accepted each change to a "Decision" section, and the task names the commit of each
      acceptance.

## Notes for the implementer

Read `docs/templates.md`, section 1, before you start. Make one commit for each ADR. Run the
governance tests and the write-set check before each commit, with `WRITE_SET_TASK=TASK-0040`.
