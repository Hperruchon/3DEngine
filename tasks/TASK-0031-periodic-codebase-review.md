---
id: 0031
title: The codebase review is periodic, and the build fails when a review is late
status: Done
phase: governance
opened: 2026-09-23
depends-on: [0030]
governed-by: []
writes:
  create:
    - tasks/TASK-0031-periodic-codebase-review.md
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
  modify:
    - docs/reviews/2026-09-23-codebase-review.md
    - docs/templates.md
    - docs/INDEX.md
    - CLAUDE.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - docs/adr/**
    - docs/register.md
---

# TASK-0031 — The codebase review is periodic, and the build fails when a review is late

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner decided on 2026-09-23: "give a date to the review, we will need to do it periodically so
that we can keep track of how the code base evolve".

The review of 2026-09-23 had a date in its name and its title, but nothing else made it periodic or
comparable. A rule in text only is a rule that an agent breaks (working agreement 6.2). The plan
examination in `CLAUDE.md` shows the risk: it is due after three milestones, and eleven milestones
passed with no examination, because nothing checked it.

## Goal

A codebase review happens at a fixed interval of milestones, and each review can be compared with the
one before it.

## Scope (in)

1. Front matter on each review: `date`, `commit`, `ledger` and `previous`.
2. A table of twelve measures in each review, with the method for each one.
3. Stable identifiers for findings, and a section in each later review that gives the state of each
   earlier finding.
4. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`, which checks the three items above and fails
   when more than six milestones follow the last review.
5. The form in `docs/templates.md`, section 7, and one rule in `CLAUDE.md`, section "Workflow".

## Scope (out)

- Do not act on a finding of the review.
- Do not make the plan examination mechanical. The rule check of 2026-09-23 proposed a gate for it,
  and the owner has not decided it.

## Acceptance criteria

- [x] The review of 2026-09-23 has the four fields and the twelve measures, for commit `01a42a1`.
- [x] Each check failed on an injected violation: a review that examined v0.20 (ten milestones
      followed, the limit is six), a date field that differs from the name, a measure that is absent,
      and a second review that gives no state for finding E1.
- [x] A complete second review passed, as the positive control. The temporary file was then deleted.
- [x] The form is in `docs/templates.md`, section 7, and `CLAUDE.md` names the rule.

## Outcome

Status: Done · v0.31 · the commit that carries this block.

**The period is six milestones, not a number of days.** Objective 12 says that time is irregular. A
calendar rule would fail after a break in which the code did not change. A milestone is a ledger entry,
and the code changes with each one. Six is twice the interval of the plan examination, because a
review costs more work. The next review must come before ledger entry v0.35.

**The first baseline.** At `01a42a1`: 11 projects, 5,292 production source lines, 5,153 test source
lines, 198 tests, 11 gate classes, zero warnings, 3 open register entries, 17 ADRs, and 15 findings (2
critical, 6 high, 5 medium, 2 low).

## Method

**Mechanical.** The gate follows the form of `ObjectiveReferenceGateTests`. The measures come from
`git show` of the reviewed commit, not from the working tree.

**Judgement.** Milestones instead of days. Six instead of three. The gate checks that each earlier
finding has a line, and it does not judge whether the line is true: "open" is always a correct answer.

**Weakest.** The gate cannot check that a measure is correct, only that it is present. Two reviews can
use different methods for one measure and still pass. The method column is the defence, and the
template asks the next review to repeat each method.
