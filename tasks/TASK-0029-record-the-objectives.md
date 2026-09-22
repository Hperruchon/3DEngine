---
id: 0029
title: The twenty objectives are in the charter, and each citation of one resolves
status: Done
phase: governance
opened: 2026-09-23
depends-on: [0027]
governed-by: []
writes:
  create:
    - tasks/TASK-0029-record-the-objectives.md
    - Engine.Tests/Governance/ObjectiveReferenceGateTests.cs
  modify:
    - docs/CHARTER.md
    - docs/glossary.md
    - docs/INDEX.md
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
---

# TASK-0029 — The twenty objectives are in the charter, and each citation of one resolves

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-09-23 the owner asked for a check of each rule against the objectives. The check found no
list of objectives in the repository.

- `CLAUDE.md` cites "Objective 11". `docs/roadmap.md` cites "Objective 15". No file defined either
  number.
- TASK-0015 says that the documents "agree with the twenty approved objectives". Its commit,
  `c5279ef`, holds the charter, the rules and the roadmap, and not the list.
- The list exists in the transcript of the session of 2026-09-20. The owner confirmed each of the
  twenty statements there, and refined objectives 18 and 19.

The owner decided on 2026-09-23: "objective should be recorded so we can follow what is happening
between session". Objective 12 gives the same reason: a person must be able to continue after three
weeks.

## Goal

The charter holds the twenty objectives with their numbers, and a gate fails on a citation that
names no objective.

## Scope (in)

1. A section "The twenty objectives" in `docs/CHARTER.md`, in the order and the words of the
   review, with the two refinements of the owner.
2. `Engine.Tests/Governance/ObjectiveReferenceGateTests.cs`: the list is numbered from 1 to 20, each
   cited objective exists, and each cited anti-objective exists.
3. Two glossary terms: objective and anti-objective.

## Scope (out)

- Do not change an objective. Only the owner changes one.
- Do not resolve a conflict between an objective and a rule. The check of 2026-09-23 found more
  items, and each one is its own task.
- Do not change an ADR.

## Acceptance criteria

- [x] `docs/CHARTER.md` numbers each objective from 1 to 20.
- [x] Each objective citation in `CLAUDE.md`, `docs/**` and `tasks/**` resolves. The archive is
      excluded, because its header forbids its use.
- [x] Each check failed on an injected violation: a citation of objective 21, a list with no
      objective 13, a citation of anti-objective 17, and a citation of anti-objective 25. The last
      injection proves that the objective check ignores the word "anti-objective".
- [x] The glossary defines objective and anti-objective.

## Outcome

Status: Done · v0.29 · the commit that carries this block.

**One change of words.** Objective 1 in the transcript names a product of the employer of the owner.
The repository is public, and an earlier decision of the owner keeps the employer out of each
artifact of this project. The charter says "separate from work for an employer". The meaning does
not change.

**The two refinements.** Objective 18 adds "They have no date", from the answer "not the moment to
work on it yet". Objective 19 gives the answer "i use c# per default, we can use other language but
i want to learn them then".

**Objectives 17 to 20** were marked "I infer" in the review. The owner confirmed each one, therefore
the charter gives them as objectives with no mark.

## Method

**Mechanical.** The text of each objective, from the transcript. The gate follows the form of
`DiagnosticsReserveGateTests`.

**Judgement.** The list goes into the charter and not into a new file, because the charter already
holds "The long objective" and the anti-objectives. A new file would give two places for the purpose
of the project.

**Weakest.** The source is a transcript, and a transcript is not the repository. The owner must read
the section once and confirm that it says what the owner approved. The gate checks the numbers only. It
cannot check that a citation uses the correct objective for its argument.
