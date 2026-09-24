---
id: 0032
title: The decisions of 2026-09-25 are records, and each code change is a ready task
status: Done
phase: governance
opened: 2026-09-25
depends-on: [0031]
governed-by: []
writes:
  create:
    - tasks/TASK-0032-record-the-decisions-of-2026-09-25.md
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/adr/0020-document-version-meaning.md
    - docs/adr/0021-operand-consumption.md
    - docs/reviews/2026-09-23-architecture-challenge.md
    - tasks/TASK-0034-document-session.md
    - tasks/TASK-0035-version-counts-applied-commands.md
    - tasks/TASK-0036-honest-geometry-backend.md
    - tasks/TASK-0037-operand-consumption.md
    - tasks/TASK-0038-hybrid-topology.md
  modify:
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/adr/0006-command-execution-model.md
    - docs/adr/0008-command-query-event-triad.md
    - docs/adr/0010-subscription-reset-snapshot-format.md
    - docs/adr/0011-server-default-deployment-topology.md
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0018-tessellation-capability.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0028-tessellation-capability.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
---

# TASK-0032 — The decisions of 2026-09-25 are records, and each code change is a ready task

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-09-23 the session wrote an architecture challenge. It tested each decision against the
product and the code, and it ended with questions for the owner. On 2026-09-25 the owner answered
nine questions: "1 c / 2 no / 3 accepted / 4 yes / 5 yes, but maybe we should check how to make it as
simple as possible / 6 a browser client is not the main idea, we may need to rethink how to show the
app / 7 yes / 8 yes / 9 yes".

The answers change three accepted ADRs, one proposal, one anti-objective and the topology section of
`CLAUDE.md`. Each code change that they require needs a task that an agent can start.

## Goal

Each decision of 2026-09-25 is in a record that a gate reads, and each code change is a task with
the status `Ready`.

## Scope (in)

1. `docs/reviews/2026-09-23-architecture-challenge.md`: the challenge in Markdown, and a new section
   "Decisions of the owner, 2026-09-25" with each question, each answer and the record for each.
2. ADR-0018: correct the two errors, amend ADR-0012 only, and set the status `Accepted`.
3. Three new ADRs, each `Accepted` on the decision of the owner: ADR-0019 (the hybrid topology),
   ADR-0020 (the meaning of the version) and ADR-0021 (operand consumption).
4. The front matter of each amended ADR, and the ADR index.
5. Anti-objective 2 keeps the drag rule of ADR-0007.
6. `CLAUDE.md`, section "Deployment topology", gives ADR-0019 as the target.
7. Seven register entries, R-0020 to R-0026.
8. TASK-0028 becomes `Ready`, and five new tasks, TASK-0034 to TASK-0038, are `Ready`.
9. The roadmap gives the new phases P0.13 to P0.17 and the order.

## Scope (out)

- No code and no test. Each code change is a task.
- No removal of Blazor. TASK-0033 does it.
- No change to an objective. Objective 8 waits for the study of R-0020.
- No rewrite of ADR-0015. The owner did not decide it (R-0025).

## Acceptance criteria

- [x] Each of the nine answers has a record, and the new section of the challenge names it.
- [x] Each of the eight critical and high findings of the codebase review of 2026-09-23 has a task
      or a register entry.
- [x] Each amendment is reciprocal, and each status agrees with its fields. The ADR gate passes.
- [x] Each cited objective and anti-objective exists. The objective gate passes.
- [x] The register holds 10 open entries of 15, and the register gate passes.
- [x] The write set of this task covers each changed file. The write-set gate passes with the list
      of changed files of this commit. An injected file that no task permits,
      `3DEngine.Vulkan/Swapchain.cs`, fails the gate.

## Outcome

Status: Done · v0.32 · the commit that carries this block.

**Four statements of the plan were wrong, and the files won.**

1. The plan said that ADR-0020 amends ADR-0005. ADR-0005 defines `Seq` and does not define the
   version. The rule "version = `Seq`" is in ADR-0006 §6, ADR-0008 §2 and ADR-0010, validation rule
   2. ADR-0020 therefore amends those three, and ADR-0005 does not change.
2. The plan gave TASK-0028 one dependency, TASK-0034. The sequence of the challenge also puts the
   native test rule before R2, and that rule moved from TASK-0028 to TASK-0036. TASK-0028 therefore
   depends on TASK-0034 and TASK-0036.
3. ADR-0014 does not require the silent fallback to the managed backend. Two comments and
   `docs/diagnostics.md` read its word "default" as that rule. TASK-0036 records the disagreement
   and corrects the three places.
4. The reset snapshot uses `version` as the cursor of a client today (ADR-0010, validation rule 2).
   ADR-0020 therefore adds the field `seq`. The plan did not see this.

**A gap in the write-set gate.** The first injection was `Engine.Core/CommandBus.cs`, and the gate
passed. This commit creates TASK-0034, TASK-0035 and TASK-0037, and each one permits that file. A
commit that touches a task file gets all the permits of that task. Register entry R-0026 records
the risk. The second injection used a file that no task permits, and it failed.

**Two corrections in passing.** `docs/adr/README.md` had a legend with four statuses of six and a
section that said that ADR-0015 exists only on a branch. Both were in R-0019, and both are corrected.
R-0019 stays open for its other statements.

## Method

**Mechanical.** The front matter of the five amended ADRs and the index rows. The Markdown form of
the challenge came from its web page through a small converter. The words did not change.

**Judgement.** The three new ADRs have the status `Accepted` and not `Proposed`. The owner decided the
substance of each one on 2026-09-25, and each task that implements one needs it in force. The owner
did not read the text of the three records. Each detail that goes past the answer is listed in the
report of the session for the owner: the field `seq` in ADR-0020, the order of events in ADR-0021,
and the loopback limit in ADR-0019.

**Weakest.** The field `enforced-by` of each new ADR names a test that does not exist yet. The ADR
gate does not check that the file exists, so the count of unenforced ADRs does not show this gap.
Each implementing task removes the text "(TASK-nnnn creates it)" when the test exists.
