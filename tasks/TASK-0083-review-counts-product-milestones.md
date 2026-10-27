---
id: 0083
title: A codebase review follows the product milestones
status: Done
phase: P0.33
opened: 2026-10-27
depends-on: [0080]
governed-by: []
writes:
  create:
    - tasks/TASK-0083-review-counts-product-milestones.md
  modify:
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/test-floor.txt
    - CLAUDE.md
    - docs/templates.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0040-living-adrs.md
    - tasks/TASK-0079-host-and-render-gaps-of-the-ninth-review.md
    - tasks/TASK-0081-sdl-upgrade.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
    - docs/adr/**
---

# TASK-0083 — A codebase review follows the product milestones

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` made a review due after six ledger entries of any kind.
The review itself and the record of the answers of the owner used two of the six, and a small task of track 0
used one more, so the interval ended before the next product phase. On 2026-10-27 the owner decided (the
answer "1a 2a" and "yes" to the decision of that date):

- D1. A review is due when more than 4 product milestones follow the ledger version of the last review, or
  when more than 12 entries of any kind follow it.
- D2. A product milestone is a ledger entry whose task has a phase of track R, K, P or V (for example R7, K0,
  P8a, V1). Track 0 phases (P0.nn) and governance entries do not count.
- D3. The gate reads the phase from the task file that the ledger heading names, not from the heading. It
  counts a product phase only when the row of that phase in `docs/roadmap.md` names the same task. An entry
  with no task counts toward the limit of 12 only.
- D4. Critical and high findings of a review go into a task that comes before the next product phase. Medium
  and low findings go into a task that comes after it. The review asks no question about this order.
- D5. The answers of the owner to a review go into the first commit of the next task, not into a separate
  ledger entry.

The order from now: this task, TASK-0081 (P0.32), TASK-0082 (R7), TASK-0040, then TASK-0079 (P0.31), whose
findings are medium or low. On TASK-0040 the owner chose option A: the mechanical part, and one summary of the
four folds for one acceptance.

## Goal

The gate makes a review due by the product milestones that follow it, and the documents give the same rule.

## Scope (in)

1. `A_Review_Is_Not_Overdue` applies D1 to D3, through a function that a test can call with a ledger, the
   phases of the tasks and the roadmap.
2. `CLAUDE.md`, section "Workflow", and `docs/templates.md`, section 7, paragraph "When" and the rule for the
   findings, give D1 to D5.
3. The roadmap gives this task as P0.33 and the order of the owner; the fields `depends-on` of TASK-0081,
   TASK-0040 and TASK-0079 hold the same order.
4. One ledger entry.

## Acceptance criteria

- [x] Before the change, each injected violation fails the gate of `main`; after it, 7 governance entries
      with 0 product entries pass, 5 product entries fail, and 13 entries of any kind fail.
- [x] A heading that names a governance phase for a task of a product phase counts that product phase.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.81 · the commit that carries this block.

A codebase review is due when more than four product milestones or more than twelve entries of any kind
follow the last review. The gate reads the phase of each entry from its task file and the roadmap, and
`CLAUDE.md` and `docs/templates.md` give the rule, the order of the findings and the place of the answers of
the owner.

## Method

**Mechanical.** Before the change, three probe ledgers on the real repository failed the gate of `main`.
After it, the same probes gave the results of the decision: 3 more governance entries passed (0 product, 7
entries), 5 entries of TASK-0076 failed (5 product), and 9 more governance entries failed (13 entries). Eight
test cases hold the rule with a ledger of their own, among them a product task under a governance heading and a
phase whose roadmap row names another task.

**Judgement.** The rule is a function of the ledger, of the phase of a task and of the roadmap, so a test feeds
it each case; the gate gives it the files of the repository. A phase of track P matches `P` and a number that
is not 0, with an optional letter or part, as in P8a and P6.1. D4 and D5 are rules for the person who writes a
review, so they are text in `docs/templates.md`; no gate holds them.

**Weakest.** A task whose phase is a product phase but whose roadmap row does not name it counts toward the
limit of twelve only, as D3 says; a review must check that each product task has its row. D4 and D5 have no
gate.

## Progress

- 2026-10-27: the task is active. Before the change, three probe ledgers failed the gate of `main`: 3 more
  governance entries (7 after v0.76), 5 entries of TASK-0076 (R6; 9 after v0.76) and 9 more governance
  entries (13 after v0.76), each against the limit of 6.
- 2026-10-27: the gate applies D1 to D3, and the documents give D1 to D5. After the change, the three probe
  ledgers gave the results of the decision. The fields `depends-on` of TASK-0081, TASK-0040 and TASK-0079 hold
  the order of the owner. Closed, after a clean build and a full local run of 506 tests (460 in `Engine.Tests`,
  39 in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`).
