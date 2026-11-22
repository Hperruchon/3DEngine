---
id: 0459
title: The preview of a proposal gives the state of the solver, the measures and the checks that the commands change
status: Done
phase: P15b
opened: 2026-11-22
depends-on: [0458]
governed-by: [0004, 0019, 0185]
writes:
  create:
    - tasks/TASK-0459-the-preview-gives-the-solver-state-the-measures-and-the-checks.md
    - Engine.Core/Preview/PreviewDifferences.cs
  modify:
    - Engine.Core/Preview/DocumentFork.cs
    - Engine.Api.Http/Proposals.Preview.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - docs/guides/agent-loop.md
    - docs/adr/0185-the-preview-of-a-proposal-on-a-fork.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0459 — The preview of a proposal gives the state of the solver, the measures and the checks that the commands change

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P15b of the roadmap: the three other differences of the preview of a proposal (ADR-0185 items 8 to 10, ADR-0157 item 8).
P15a (TASK-0456, v0.440) gave the fork, the outcome of each command and the bodies. The owner answered Q1 to Q5 of the third
codebase review of 2026-11-21 with option A on 2026-11-22 (Q4 A: the checks of the changed pairs under the bound of work, with
the state `limit`; Q5 A: the measures to the double). The owner ordered "Start P15b" in the chat on 2026-11-22.

The branch `p15b-preview-differences` starts from `main` at `26bcd0dd` (v0.442).

## Goal

An agent reads, before the person decides, the state of the solver of each sketch, the measures of each body and each pair
check that its proposal changes, from the fork of the Document.

## Scope (in)

1. Item 8: for each sketch that the commands add or change, its state before and after them, from `GetSketch` on the fork.
2. Item 9: for each body that the commands add or change, `GetMassProperties` after them and, for a changed body, before them.
3. Item 10: `CheckInterference` for each pair with a body that the commands add, change or remove, one check for each pair
   under the bound of work of ADR-0182; only the pairs whose state changes.
4. "Before" from a second replay of the same copy of the log (the base), only when a value before is necessary.
5. The members `sketches`, `measures` and `checks` of `GET /proposals/{id}/preview`; the guide example shows them.
6. The amendment of ADR-0185 items 2, 3 and 8 to 11 with the decided form and the measured cost.

## Scope (out)

- P16 (the accept after a preview) and R25 (the preview in the window). The window does not change (`3DEngine.App/**`).
- A change of `Engine.Contracts/**`, of the log format, of a query, or a new event kind.
- `GetDistance` and `GetAngle` in the preview: each measures two items, not one body.
- A bound of the sum of the work of one preview (ADR-0185, "Bad").

## Acceptance criteria

- [x] A changed body gives its measures before and after the commands to the double, and the pair whose class changes.
- [x] An added body gives its measures after the commands, and its pairs that are not apart.
- [x] A removed body gives its pairs that were not apart.
- [x] A pair above the bound of work has the state `limit` with `E-CHECK-WORK-LIMIT`.
- [x] A sketch goes from `under` to `over` with the residual of the conflict, and from `under` to `solved`.
- [x] A proposal that changes nothing gives empty differences and makes no base; a rename makes no base and no check.
- [x] A new member of a sketch record fails a test, so the comparison of two sketches changes with it.
- [x] The guide example shows the three members, and `AgentGuideExamplesTests` sends it to a host.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass. Continuous integration is not run yet.

## Decisions

The answers of the owner (option A of Q4 and Q5) give the scope. ADR-0185 records each decision of this task:

- The values come from the queries `GetSketch`, `GetMassProperties` and `CheckInterference` on the session of the fork. No
  code of the preview computes a measure or a check, so the preview and a query of the accepted Document give the same values.
- "Before" is a second replay of the same copy of the log, the base, with a new backend of the factory. The session of the
  fork holds the state after the commands, and a Document is mutable, so the state before cannot stay in the same session.
  The base equals the first Document at the version of the copy by the replay rule (ADR-0012). It runs only when a value
  before is necessary.
- The rule of the same solid: a body whose geometry key and fault do not change keeps its solid (ADR-0059 item 2). Its
  measures before are its measures after, and its pairs are not checked when the other body keeps its solid too.
- The pairs of a removed body are checked before the commands. Q4 A names the bodies that the proposal changes; a `Translate`
  removes the old body, so without these pairs a move that ends an overlap shows no difference.
- One `CheckInterference` for each pair, so the bound of work of ADR-0182 item 8 applies to each pair and a pair above it is
  `limit`, not a refusal of the other pairs. A body that does not exist counts as apart.
- The state of a sketch is `solved`, `under` or `over` from `solved` and `freedom` of `GetSketch`; the residual is the largest
  absolute residual of its constraints. Two sketch records compare by content, member by member.
- `DocumentFork.Run` takes the factory of backends and frees each backend that it takes (amends ADR-0185 item 2).
- No new diagnostic code: `E-CHECK-WORK-LIMIT` and the refusals of the queries give each refused value.

## Outcome

Status: Done · commit (the merge of the branch p15b-preview-differences) · v0.443

## Method

Mechanical: the three members of the preview, the records, the guide example and its test, the rows of the roadmap, the ledger
entry and the History of ADR-0185.

Judgement: the base as a second replay, the rule of the same solid, the pairs of a removed body, one check for each pair, and
the names of the states of a sketch.

Weakest: the rule of the same solid. It trusts that a committed geometry key names one solid. The test
`A_Proposal_That_Changes_Nothing_Gives_Empty_Differences_And_No_Base` and the rename of Bas in the bookcase test show the
rule (J9). No test shows a command that changes a solid and keeps its key; I know of no such command, and I did not search
each command for one.

The tests came after the code: each new test passed at its first run. So the injections below are the proof that each test
can fail. A probe (a test file, removed) measured the cost of the fork on the bookcase in a Release build; ADR-0185 item 11
gives the numbers.

Injections (`inject.js` in `%TEMP%\p15b`, dotnet with shell:false, filter `ProposalPreviewTests` and `AgentGuideExamplesTests`,
20 tests), each restored byte for byte (sha256):

| Injection | Failing test |
|---|---|
| J1 a changed body with a new solid counts as the same solid | `A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes` |
| J2 each checked pair is a difference | `A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes`, `Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing` |
| J3 the pairs of a removed body are not checked | `Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing` |
| J4 a pair above the bound is refused, not limit | `A_Pair_Above_The_Bound_Of_Work_Has_The_State_Limit` |
| J5 a sketch with no free degree is under | `A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After` |
| J6 two sketches are always the same | `A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After` |
| J7 the fork makes no base | `A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes`, `Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing`, `A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After` |
| J8 the preview gives no measures | `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape`, `A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes`, `The_Route_Gives_The_Preview_And_Its_Refusals`, `A_Pair_Above_The_Bound_Of_Work_Has_The_State_Limit`, `A_Proposal_That_Changes_Nothing_Gives_Empty_Differences_And_No_Base`, `Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing` |
| J9 the same-solid rule is off | `A_Proposal_That_Changes_Nothing_Gives_Empty_Differences_And_No_Base` |
| J10 the guide example has no checks | `Each_Block_Of_The_Guide_Is_A_Marked_Example_And_The_Loop_Has_Each_Route` |
| J11 the comparison of two sketches skips the constraints | `A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After` |
| J12 the base reads the values after the commands | `A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes`, `Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing`, `A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After` |

## Progress

- 2026-11-22: opened, branch `p15b-preview-differences` from `26bcd0dd`. The records and the plan of the differences, the
  base, the three members of the preview, five tests and the extension of the bookcase test, the guide example, the
  injections, the amendment of ADR-0185, the row P15b and the ledger entry v0.443.
- 2026-11-22: run 45051658377 of `c895156f` passed on each job. Closed at v0.443.
