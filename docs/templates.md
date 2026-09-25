# Templates

**Status: Accepted** — 2026-09-20, TASK-0015.

This document holds the forms to fill in. `docs/working-agreement.md` gives the behaviour rules.
`docs/register.md` gives the rules for deferred items.

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section
"Language".

Front matter exists so that a gate can read it. Each item that a check must read is a field. Each
item that a person must read is text. If you cannot write a rule as a field, the rule is a
convention.

---

## 1 Register entry

Copy this form into the section "Open" in `docs/register.md`. Identifiers are sequential. Do not
use an identifier again.

```markdown
### R-0000 · One line. State the problem. Use the present tense.
- class: risk | question | debt | interim
- opened: YYYY-MM-DD
- due: YYYY-MM-DD
- extended: no
- refs: <path:line>, TASK-nnnn, ADR-nnnn
Give the problem and its cost in two or three sentences.
Exit: state the condition that closes this entry.
```

The `class` field gives the `due` date. A `risk` has 30 days. A `question` has 90 days. A `debt`
has 180 days. An `interim` item has 365 days. Use a different date only with a reason.

### To close an entry

Move the entry to the section "Closed". Add one line. Use one of these three forms:

```markdown
Closed 2026-09-14 · resolved · commit a1b2c3d
Closed 2026-09-14 · decided  · ADR-0016
Closed 2026-09-14 · accepted · the reason to keep the condition
```

An entry with the outcome `accepted` moves to the section "Accepted compromises".

### To extend an entry

```markdown
- extended: 2026-09-14 (the OCCT shim must land first)
- due: 2026-12-14
```

You can extend an entry one time. The gate refuses a second extension. Then you must resolve the
entry, decide it, or accept it.

---

## 2 Architecture Decision Record (ADR)

This form has four sections and two fields. The form is short by design. Ask this question: will
you write ADR number 15 at 22:40 on a Tuesday?

```markdown
---
id: 0015
title: <a short noun phrase. Do not use a sentence.>
status: Proposed
date: YYYY-MM-DD
supersedes: []
superseded-by: []
affects:
  - Engine.Contracts/Geometry/**
enforced-by: UNENFORCED (accepted risk)
---

# ADR-0015 — <title>

## Context

<Give the conditions that make this a decision and not a preference. Give the constraint that
removes the obvious alternative. A weak context makes the record useless in two years. The text
"we need a scalable solution" is a weak context.>

## Decision

We use <...>.

## Consequences

**Good:** <what the project gets>
**Bad:** <what the project pays. An ADR with no cost is an ADR that nobody examined.>
**Next:** <the register entries and the tasks that this decision creates>
```

### Field rules

| Field | Rule |
|---|---|
| `status` | Use one value: `Proposed`, `Accepted`, `Amended`, `Superseded`, `Withdrawn` or `Rejected`. `Amended` means that a later ADR changes one part; both records apply. `Superseded` means that a later ADR replaces this one; this record does not apply. Do not write the number in the status. The fields `amended-by` and `superseded-by` give the number. `Withdrawn` means that the author changed the proposal, or that the owner took back an accepted record that no later record replaces. `Rejected` means that the project examined the proposal and refused it. |
| `amends` and `amended-by` | These fields are reciprocal, in the same way. A record with `amended-by` set must carry the status `Amended`. |
| `supersedes` and `superseded-by` | These fields are reciprocal. If ADR-0011 supersedes ADR-0004, then ADR-0004 must give ADR-0011 in `superseded-by`. The gate fails if one field is absent. This check finds the error in register entry R-0006. |
| `affects` | Give path patterns. A gate compares these patterns with the write-set of a task. The result is the list of ADRs that the agent must read. This field lets the project hold more than 40 ADRs. |
| `enforced-by` | Give a test name, an analyzer rule, or the text `UNENFORCED` with a reason. The gate fails if the quantity of unenforced ADRs increases. The gate counts a record in force only, because a record with the status `Proposed`, `Withdrawn` or `Rejected` describes work that nobody built. |

After the status becomes `Accepted`, the text is permanent. You can change only `status`,
`superseded-by` and `enforced-by`. To correct an approved ADR, write a new ADR.

---

## 3 Task

```markdown
---
id: 0014
title: <what the task delivers. Do not give the activity.>
status: Ready
phase: P0.3
opened: YYYY-MM-DD
depends-on: [0013]
governed-by: [0006, 0013]
writes:
  create:
    - Engine.Core/Hosting/HandlerCatalog.cs
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
  forbid:
    - Engine.Contracts/**
---

# TASK-0014 — <title>

## Context
<Give the reason for this task now. Give the ADR or the register entry that caused it.>

## Goal
<Use one sentence. If you need two sentences, make two tasks.>

## Scope (in)
## Scope (out)
<The list "out" is important. It stops an agent that makes the change larger.>

## Acceptance criteria
- [ ] <a condition that a person can observe and check>

## Notes for the implementer
```

### To close a task

Add this block:

```markdown
## Outcome
Status: Done · commit <hash> · v0.nn

## Method
Mechanical: <the work that needed no decision>
Judgement:  <the work that needed a decision, and the decision>
Weakest:    <the part that is most probably incorrect, and the test that shows it>
```

This block has high value. It tells the reader where to look. It costs one minute. It is difficult
to write incorrectly. It is necessary for each change that an agent made.

### Field rules

| Field | Rule |
|---|---|
| `depends-on` | Give task identifiers. A tool reads this field to find the tasks that it can start now. |
| `governed-by` | Give ADR identifiers. This list must equal the intersection of `affects` and `writes`. A gate can verify this. Therefore an absent ADR causes an error and not an assumption. |
| `writes` | This field is the write-set. `Engine.Tests/Governance/WriteSetGateTests.cs` reads it. Continuous integration gives the list of changed paths in the variable `WRITE_SET_FILES`, and the gate fails if a changed path is outside the set or inside the forbid list. A hook before each commit does not exist; the gate is the protection. |
| `forbid` | Give the paths that this task must not touch. This field is inexpensive, and it stops the most frequent increase of scope. |

Before you start two agents, compare their `writes` fields. If the sets intersect, do not start the
agents together.

---

## 4 Commit message

```
<area>: <one line in the imperative. Use a maximum of 72 characters.>

<Give the reason. The change itself gives the actions. Use two or three lines.>

TASK-0014 · ADR-0013
```

Put one topic in one commit. A refactor and a change of behaviour must not share a commit.

---

## 5 Checklist to end a session

A session ends green, or the session does not end. `CLAUDE.md` gives the same conditions in the
section "Workflow".

```markdown
- [ ] The build passes and the tests pass. The gate ran, not only the narrow tests.
- [ ] The status in the task file agrees with the work. The Outcome block and the Method block
      are complete.
- [ ] CURRENT-STATE.md has a new entry, or you updated an entry.
- [ ] Each new diagnostic code is in the three necessary positions.
- [ ] Each new ADR has an `enforced-by` field. Each superseded ADR has the reciprocal field.
- [ ] Each guess, each deferred item and each temporary repair has a register entry with a date.
- [ ] The register has no entry after its `due` date. No entry has a second extension. The
      quantity of open entries is at or below the limit.
- [ ] No new `TODO`, `interim`, `temporary` or `DRAFT` marker exists without a register
      identifier.
```

---

## 6 What each gate reads

This table shows the purpose of each field. It also shows which checks do not exist yet.

| Gate | It reads | It fails if |
|---|---|---|
| Register age | entries in `register.md` | A `due` date passed, or a second extension exists |
| Register limit | the quantity in section "Open" | The quantity is above the limit |
| Marker check | tracked source and documentation | A marker word has no `R-nnnn` identifier |
| ADR schema | ADR front matter | A status value is incorrect, `enforced-by` is absent, or an identifier does not exist |
| ADR reciprocity | all ADR front matter | `supersedes` has no reciprocal `superseded-by` |
| ADR index | the generated index and the committed index | The two files are different |
| Unenforced quantity | the `enforced-by` field | The quantity of unenforced ADRs increased |
| Write-set | `writes` in the active task, and the change | A changed path is outside the declared set |
| Dependency direction | each project file | An edge breaks the authority diagram. See R-0004. |
| Diagnostic register | source files and `diagnostics.md` | A code exists in one position only. Check both directions. |
| Schema parity | handlers and the `/schema` output | The output is different from the declaration |
| Replay determinism | the fixture | The replay result is different, except for the timestamp and the document identifier |
| Codebase review | `docs/reviews/*.md` and the ledger | A field or a measure is absent, a finding of the previous review has no state, or more than six milestones follow the last review |

Ten of these twelve gates are small scripts that read text. Two gates exist today.

---

## 7 Codebase review

A codebase review describes the code at one commit. Each review is one file in `docs/reviews/`, with
the name `YYYY-MM-DD-codebase-review.md`. Do not change an earlier review to agree with the code. Write
the next one. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` reads this form.

**When.** A review is due before the seventh milestone after the ledger version that the last review
examined. The gate counts ledger entries in `docs/CURRENT-STATE.md`, not days, because objective 12
says that time is irregular.

```markdown
---
date: YYYY-MM-DD
commit: <the hash of the commit that the review examines>
ledger: v0.nn
previous: <the file name of the previous review, or none>
---

# Codebase review — YYYY-MM-DD

## Summary
## Measures
| Measure | Value | Method |
|---|---|---|
| Projects in the solution | | |
| Production source lines | | |
| Test source lines | | |
| Tests | | |
| Gate classes | | |
| Build warnings (clean build) | | |
| Open register entries | | |
| ADRs | | |
| Findings: critical | | |
| Findings: high | | |
| Findings: medium | | |
| Findings: low | | |

## Findings of the previous review
<One line for each identifier of the previous review: fixed, open or worse, with the evidence.>

## 1 Current architecture
## 2 Patterns in use
## 3 Review of the code
#### E1 · Critical · <one line> · Confirmed
## 4 The next planned task
## 5 Research for that task
## 6 Recommended approach
## Questions for the owner
```

### Rules

| Item | Rule |
|---|---|
| Identifier | Each finding has a heading `#### <letter><number> · <severity> · <title> · <state>`. An identifier stays with its finding in each later review. A new finding gets a new number. |
| Measures | Use the same method as the previous review, so that the values compare. Give the method in the third column. |
| Previous findings | The gate fails when a finding of the previous review has no line. "Open" is a correct answer. |
| Labels | Mark each statement **[Observed]**, **[Inferred]** or **[Recommended]**. |
| Closing a finding | A task, a register entry or an ADR closes a finding. The next review records the result. |
