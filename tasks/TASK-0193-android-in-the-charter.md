---
id: 0193
title: Android is a platform of the charter, with ADR-0061 and the rows of R14
status: Active
phase: R14a
opened: 2026-11-05
depends-on: [0190]
governed-by: [0055]
writes:
  create:
    - tasks/TASK-0193-android-in-the-charter.md
    - docs/adr/0061-android-platform.md
  modify:
    - docs/CHARTER.md
    - CLAUDE.md
    - docs/adr/0017-first-party-vulkan-layer.md
    - docs/adr/0027-text-and-panels.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/**
---

# TASK-0193 — Android is a platform of the charter, with ADR-0061 and the rows of R14

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-05 the owner gave the Android order (phases a to g) and a standing order for an autonomous run until
2026-11-08, and confirmed both in the chat of the session. This task is phase a. The order requires a check of each
library fact on disk before the ADR; the check of 2026-11-05 is in section 5 of
`docs/reviews/2026-11-05-b-codebase-review.md`. The review of that date puts P0.82 before this phase, and P0.83 and
P0.84 after it.

## Goal

The charter names Android, R-0020 is closed, ADR-0061 holds the eight decisions of the order, ADR-0017 and ADR-0027
are amended, and the roadmap holds R14a to R14g.

## Scope (in)

1. Objective 8 and anti-objective 16 of `docs/CHARTER.md`, and the sentence of the platforms in `CLAUDE.md`, section
   "Determinism rules".
2. ADR-0061, with the amendments of ADR-0017 item 4 and ADR-0027 item 9, and the index of the records.
3. R-0020 closed as decided, and R-0054 for the SDL of the pin of `ppy.SDL3-CS`.
4. The rows R14a to R14g and the order of the work.

## Scope (out)

- A change of code. The swap of the binding (R14c). The native builds (R14d).

## Acceptance criteria

- [ ] `ObjectiveReferenceGateTests`, `AdrGateTests`, `RegisterGateTests` and `TaskGovernanceGateTests` pass.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-11-05: opened with the Android order and the standing order of the owner of 2026-11-05; the check of the
  libraries found no false fact that blocks the order.
- 2026-11-05: the charter, CLAUDE.md, ADR-0061 and its amendments, the register and the roadmap rows.
