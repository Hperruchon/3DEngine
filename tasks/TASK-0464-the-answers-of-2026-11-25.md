---
id: 0464
title: The answers of the owner of 2026-11-25 to the open questions are a record, with four new rows and their order
status: Active
phase: P0.188
opened: 2026-11-25
depends-on: [0461]
governed-by: []
writes:
  create:
    - tasks/TASK-0464-the-answers-of-2026-11-25.md
  modify:
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0169-the-section-view-of-a-sheet.md
    - docs/adr/0187-the-preview-of-a-proposal-in-the-window.md
    - docs/adr/0188-the-accept-after-a-preview-and-the-group-in-the-log.md
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

# TASK-0464 — The answers of the owner of 2026-11-25 to the open questions are a record, with four new rows and their order

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-23 the session gave the owner a sheet of nine open questions, Q1 to Q9, from the register, the ADRs and R25. Q8
went into P0.187 (TASK-0461, V235). On 2026-11-24 P16 (TASK-0463) added three questions on its record ADR-0188. On 2026-11-25,
after the merge of P0.187 (v0.447), the session wrote each question again in the chat, and the owner answered "all A". Then
the owner ordered "Start P0.188".

The answers:

| Question | Answer A |
|---|---|
| Q1, P11.0 (R-0070) | A model of placements: each body keeps its shape in its own frame, and a joint or a move changes only a stored placement, a rotation matrix and a translation, so the exact numbers do not grow. It changes the public shape of `Engine.Contracts`; the ADR of P11.0 comes in the same commit. |
| Q2, K10b (R-0070) | The owner gives the sizes of the crossbar, of its two fixing points and of the volumes of the lamp, and K10b builds the case. |
| Q3, R-0072 | At a portrait width of 360 pixels with a sheet, "N hidden" leaves the path line. The ring keeps it as "Show hidden". |
| Q4, R-0076 | On a landscape phone of 640 by 360, the ring moves away from the windows of the top left. Each segment keeps its 46 pixels. |
| Q5, R-0075 | Two queries: the sheets of the Document, and the live bodies with their names. An ADR and tested examples of the guide come with them; they change `Engine.Contracts`. |
| Q6, ADR-0187 "Bad" | While a preview shows, the outline of the selected body takes a colour apart from the amber of a proposal. |
| Q7, ADR-0187 "Bad" | A later slice adds a faint amber fill to the new shape in "After". |
| Q9, ADR-0155 item 7, ADR-0057 item 10, ADR-0169 item 3 | Keep both decisions of 2026-11-21: a read of a sheet keeps the B-Rep of each body and no line, and the owned kernel cuts a solid by a plane for the section of a sheet. |
| P16 question 1, ADR-0188 "Bad" | The format version stays 1. |
| P16 question 2, ADR-0188 item 4 | The accept refuses an `Undo` or a `Redo` with `E-CMD-GROUP-MOVE`. The post does not refuse it earlier. |
| P16 question 3, ADR-0188 item 7 | An accept after a failed fork (`E-API-PREVIEW-FAILED`) applies nothing. |

## Goal

The roadmap, the register and the ADRs hold the answers of 2026-11-25, so that a later session opens each new row with no
question on its scope.

## Scope (in)

1. The roadmap: row P0.188; the answers in rows P11 and K10b; the new rows R32a, R32b, R33 and P21; a paragraph in the section
   "Order"; and the lines of the section "Shipped" for v0.445 to v0.448.
2. The register: one progress line in each of R-0070, R-0072, R-0075 and R-0076. Each entry stays open until its row does the
   exit.
3. One History line in each of ADR-0057, ADR-0155, ADR-0169, ADR-0187 and ADR-0188. No item of an ADR changes.
4. The ledger entry v0.448.

## Scope (out)

- Each code change. Each new row opens its task at its start, and its ADR when it changes the Document or the contract.
- The ADR of P11.0. Q1 A gives its direction; the record and the change of `Engine.Contracts` come in the task of P11.0.
- The sizes of K10b. The owner did not give them yet, so K10b stays open with R-0070.
- The charter. No answer changes an objective.

## Acceptance criteria

- [x] Each answer of the table has a place in the roadmap, the register or an ADR, and the ledger entry names each place.
- [x] The new rows name the answer and the register entry that they close, and no row of track R, K, P or V names this task.
- [x] `docs/register.md` holds 13 open entries of 15, as before.
- [x] `AdrGateTests`, `TaskGovernanceGateTests`, `RegisterGateTests` and `MarkerGateTests` pass.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- `depends-on` names TASK-0461, the last task that `main` merged at v0.447. TASK-0462 and TASK-0463 merged before it.
- `CodebaseReviewGateTests` counts a product milestone when a row of track R, K, P or V names the task of a ledger entry. Only
  row P0.188 names this task.
- The section "Shipped" had no line for v0.445 (R25), v0.446 (P16) and v0.447 (P0.187). This is a false statement about the
  repository, so this task corrects it (`CLAUDE.md`, "Stop and ask").
- `governed-by` must equal the ADRs in force whose `affects` meet the write set. Let the gate confirm the empty list.

## Outcome

Status: Active · commit (the merge of the branch p0188-owner-answers) · v0.448

## Method

Mechanical: the text of each answer, from the questions as the chat gave them, in the row, the entry or the History line.
Judgement:  the order of the four new rows (after P0.188 and before K17), and a History line, not an item change, for each
            confirmation, because no answer changes a rule in force.
Weakest:    the order of the new rows is a choice of the session, not of the owner; the owner can move them. The ADR item that
            R32b amends is not named, because the task of R32b finds it.

## Progress

- 2026-11-25: opened on the order of the owner "Start P0.188", after the answer "all A", branch `p0188-owner-answers` from
  `origin/main` at `efd1e73a` (v0.447).
