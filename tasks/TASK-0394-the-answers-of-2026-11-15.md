---
id: 0394
title: The answers of the owner of 2026-11-15 on the assemblies and the work with an agent are a record, with ADR-0157, its roadmap rows and the new order
status: Active
phase: P0.160
opened: 2026-11-15
depends-on: [0393]
governed-by: [0157]
writes:
  create:
    - tasks/TASK-0394-the-answers-of-2026-11-15.md
    - docs/adr/0157-the-foundations-and-the-order-of-the-assembly-track.md
    - docs/proposals/2026-11-15-assembly-and-agent-design.md
    - docs/proposals/2026-11-15-plasticity-features.md
    - docs/proposals/2026-11-15-answer-sheet.md
    - docs/proposals/2026-11-15-plan-k10-the-first-assembly-row.md
  modify:
    - docs/adr/0126-assemblies-with-joints-and-geometric-checks.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/register.md
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

# TASK-0394 — The answers of the owner of 2026-11-15 on the assemblies and the work with an agent are a record, with ADR-0157, its roadmap rows and the new order

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-15 the owner asked for a design file on the design of assemblies and the work with an agent, for four families of
assemblies, to discuss before a task enters the roadmap. A side session wrote
`docs/proposals/2026-11-15-assembly-and-agent-design.md`, a comparison with the product page of Plasticity, a plan of K10 and
an answer sheet. On 2026-11-15 the owner answered in the chat: "All A, C for A2".

## Goal

An ADR, the roadmap and the register hold the answers of the owner, so that a later session opens K10 and each row after it
with no question on the scope or the order.

## Scope (in)

1. The four proposal files, as the side session wrote them, with the record of the answers.
2. ADR-0157, with A1 to A15 and the order of the features of the Plasticity file.
3. ADR-0126: items 4 and 8 change, `amended-by` and the status `Amended`, and one line of history.
4. The roadmap: row P0.160; the new text of K10, K11, K12, P11, P13 and R24; the rows K15 to K22, P15 to P20 and R25 to
   R29; one paragraph in the section "Order".
5. One register entry, R-0070, for the model of a move of a part and the sizes of the two validation cases. The proposal had
   two entries; on 2026-11-15 the owner chose one joined entry, because the register held 14 of its 15 entries.
6. The index of the ADRs, and the ledger entry.

## Scope (out)

- Each code change. Each phase opens its task at its start, and its ADR when it changes the Document or the contract.
- The charter. Objective 11 already gives assemblies with joints and their checks a place after P10c. If a session finds
  that the stage "instances inside one Document" needs a change of an objective, it reports it to the owner.
- ADR-0092, ADR-0130 and ADR-0031. Their text agrees with the code in force. The ADR of P15 and the ADR of P16 amend them.
- The answers D1 to D7 of P10.3 and the plan of P10.3: the task of P10.3 records them in its ADR.
- The seven questions of P10.2: the owner answered them with A on 2026-11-15, and ADR-0155 (TASK-0393) records them.

## Acceptance criteria

- [ ] ADR-0157 exists and records A1 to A15, and ADR-0126 shows the amendment in its items 4 and 8.
- [ ] Each new row of the roadmap names ADR-0157 or its own later ADR, and the section "Order" holds the new paragraph.
- [ ] `docs/register.md` holds R-0070 with its dates, and at most 15 entries are open.
- [ ] `AdrGateTests`, `AdrEnforcementExistsGateTests`, `TaskGovernanceGateTests`, `ObjectiveReferenceGateTests` and
      `MarkerGateTests` pass.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- The proposed text of each change is in `docs/proposals/2026-11-15-governance-record/` in the worktree of the side session.
  That folder is working text: do not commit it. Copy each text to its place, and put the numbers in.
- Read each file before you commit it. The web sources of the design file have marks: an agent read the page, a snippet
  only, or from memory. The side session did not open the web pages itself.
- `governed-by` must equal the ADRs in force whose `affects` meet the write set. At f7786c1 only ADR-0055, ADR-0118 and
  ADR-0126 affect a file under `docs/`, and each affects `docs/CHARTER.md`, which this task forbids. So the list is the new
  ADR only. Let the gate confirm it.
- `depends-on` names TASK-0393, the last task that exists on `main` at v0.378, when this task started.
- `enforced-by` of ADR-0157 names `TaskGovernanceGateTests`: the order of the rows becomes the `depends-on` of each task.
  `AdrEnforcementExistsGateTests` needs a path that exists, and that file exists on `main`. The count of records with no
  enforcement does not grow.
- Only row P0.160 of the roadmap names this task. `CodebaseReviewGateTests` counts a product milestone when a row of
  track R, K, P or V names the task of a ledger entry. Do not put the number of this task in such a row.
- `AffectsCitationGateTests` refuses a code file that cites ADR-0157, because its field `affects` names the design file
  only. This task changes no code file, so the rule has no effect now.
- The register has 14 open entries of 15 at v0.378 (P10.2 added R-0068 and R-0069). One joined entry gives 15.

## Outcome

Status: Active

## Method

Mechanical: the copy of the proposed text, with the numbers, from the folder of the side session.
Judgement:  one joined register entry (the choice of the owner), and the corrections of the lines that the merges of P10.2 and
            P0.159 made false.
Weakest:    the web sources of the design file were read by research agents only, and the estimates of the new rows come
            from a read, not from a measure.

## Progress

- 2026-11-15: opened on the order of the owner ("Main session" applies the record; "Join the two" for the register), branch
  p0160-assembly-record from v0.378. The proposed text came from the side session "Prepare P10.3 and the assembly plan".
  The session read the four proposal files and the four texts of the folder before the copy, and corrected: the words "Not
  committed", the lines that said no task records the answers, the two files that the answer sheet names and that the
  repository does not hold, the answer of the owner to the questions of P10.2, the numbers (TASK-0394, ADR-0157, R-0070),
  and the count of open register entries.
