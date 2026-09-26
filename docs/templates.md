# Templates

**Status: Accepted** — 2026-09-20, TASK-0015. Reorganized 2026-09-26, TASK-0039.

This document holds the forms to fill in, the code conventions, the gate table and the rule for
rules. `CLAUDE.md` gives your position. `docs/register.md` gives the rules for deferred items.

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section
"Language".

Front matter exists so that a gate can read it. Each item that a check must read is a field. Each
item that a person must read is text. If you cannot write a rule as a field, the rule is a
convention.

---

## 1 Architecture Decision Record (ADR)

This form has four sections and two fields. The form is short by design. Ask this question: will
you write ADR number 15 at 22:40 on a Tuesday?

```markdown
---
id: 0015
title: <a short noun phrase. Do not use a sentence.>
status: Proposed
topic: <one or two words, for the index>
date: YYYY-MM-DD
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Contracts/Geometry/**
enforced-by: UNENFORCED (accepted risk)
notes: <optional. One line that the index reader needs.>
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

## History

- YYYY-MM-DD · TASK-nnnn · <the change, in one sentence>
```

### Field rules

| Field | Rule |
|---|---|
| `status` | Use one value: `Proposed`, `Accepted`, `Amended`, `Superseded`, `Withdrawn` or `Rejected`. `Amended` means that a later ADR gives the reason for a change, and this record holds the current decision. `Superseded` means that a later ADR replaces this one; this record does not apply. Do not write the number in the status. The fields `amended-by` and `superseded-by` give the number. `Withdrawn` means that the author changed the proposal, or that the owner took back an accepted record that no later record replaces. `Rejected` means that the project examined the proposal and refused it. |
| `amends` and `amended-by` | These fields are reciprocal. A record with `amended-by` set must carry the status `Amended`. |
| `supersedes` and `superseded-by` | These fields are reciprocal. If ADR-0011 supersedes ADR-0004, then ADR-0004 must give ADR-0011 in `superseded-by`. The gate fails if one field is absent. |
| `affects` | Give path patterns. `TaskGovernanceGateTests` compares these patterns with the write set of each open task. The result is the list of ADRs that the agent must read. Give the narrowest pattern that holds the decision. |
| `enforced-by` | Give a test name, or the text `UNENFORCED` with a reason. The gate fails if the quantity of unenforced ADRs increases. The gate counts a record in force only. `AdrEnforcementExistsGateTests` fails when the file is absent. While a Ready task creates the file, write `path (TASK-nnnn creates it)`. |

### An ADR agrees with the code

An accepted ADR describes the decision in force. Its text agrees with the code. An accepted ADR that
a Ready task implements is the one exception, until that task closes.

1. When a task changes a decision, the same task changes the ADR text. It adds one line to the
   section "History": the date, the task, and the change in one sentence. Git holds each old text.
2. The owner accepts each change to a section "Decision", as the owner accepts an ADR. A change to
   "Context", "Consequences", "History" or the front matter needs no acceptance.
3. Write a separate ADR when a decision is new, or when the change needs its own context. It then
   supersedes the old record, or it amends the old record and the old text changes in the same
   commit. "Amended" means: the amending ADR holds the why, and the base ADR holds the current what.
4. An ADR that no decision needs gets the status `Withdrawn` or `Superseded`. Its file moves to
   `docs/adr/archive/`. `docs/adr/README.md` lists the number under "Archived", so that the number
   is never used again.

A change to the public shape of `Engine.Contracts/**` needs an ADR in the same commit. The job
`contract-gate` in `.github/workflows/ci.yml` fails a push that changes `Engine.Contracts/**` with
no change under `docs/adr/`. Read the rejected decisions and the closed register entries before you
propose a decision. Do not propose a decision that the project refused.

---

## 2 Task

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

### Field rules

| Field | Rule |
|---|---|
| `status` | Use one value: `Ready`, `Active`, `Done` or `Deferred`. The task file is the authority for the question "did this phase ship". |
| `depends-on` | Give task identifiers. `TaskGovernanceGateTests` fails when an identifier names no task. A session takes the first Ready task in roadmap order whose dependencies are Done. |
| `governed-by` | Give ADR identifiers. This list must equal the set of ADRs in force whose `affects` patterns intersect `writes`. `TaskGovernanceGateTests` verifies it for a Ready or Active task. Therefore an absent ADR causes an error and not an assumption. |
| `writes` | This field is the write set. `Engine.Tests/Governance/WriteSetGateTests.cs` reads it. Continuous integration gives the list of changed paths in the variable `WRITE_SET_FILES` and the governing task in `WRITE_SET_TASK`, and the gate fails if a changed path is outside the set. |
| `forbid` | Give the paths that this task must not touch. This field is inexpensive, and it stops the most frequent increase of scope. A forbid binds the task that declares it. |

Before you start two agents, compare their `writes` fields. If the sets intersect, do not start the
agents together.

### The write-set check before a commit

```bash
set -o pipefail
WRITE_SET_TASK=TASK-0014 WRITE_SET_FILES="$(git diff --cached --name-only --no-renames)" \
  dotnet test Engine.Tests/Engine.Tests.csproj --no-build --filter 'FullyQualifiedName~Governance'
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

---

## 3 Commit message

```
<area>: <one line in the imperative. Use a maximum of 72 characters.>

<Give the reason. The change itself gives the actions. Use two or three lines.>

TASK-0014 · ADR-0013
```

Put one topic in one commit. A refactor and a change of behaviour must not share a commit.

The trailer line of the message, of the form `TASK-nnnn` or `TASK-nnnn · ADR-nnnn`, names the task
that governs the commit.
The write-set gate reads it. If the message names no task, the one task file that the commit
touches governs it. A commit that touches several task files and names none fails.

---

## 4 Code conventions

These conventions make a search cheap. Each one is convention only, except where a gate is named.

**One command in one file.** One command and one query in one file each, with the handler in a
sibling file:

```
Engine.Core/Commands/CreateBoxCommand.cs         record CreateBoxCommand : Command
Engine.Core/Commands/CreateBoxCommandHandler.cs  class  CreateBoxCommandHandler : ICommandHandler
Engine.Core/Queries/GetBoundingBoxQuery.cs        record GetBoundingBoxQuery : Query
Engine.Core/Queries/GetBoundingBoxQueryHandler.cs class  GetBoundingBoxQueryHandler : IQueryHandler
```

The wire name is the `Name` property on the record. It is a PascalCase string, for example
`"CreateBox"`, and it matches the prefix of the type. The version is the integer `SchemaVersion`,
not the type name. Commands live under `Engine.Core/Commands/`, queries under `Engine.Core/Queries/`
and backends under `Engine.Core/Geometry/`. `SchemaEndpointGateTests` fails when a registered
command has no schema entry.

**Type suffixes.** Match them when you add a peer.

| Suffix | Role | Example |
|---|---|---|
| `*Command`, `*Query` | A triad message. A `record` that extends `Command` or `Query`. | `CreateBoxCommand` |
| `*CommandHandler`, `*QueryHandler` | The handler. A `class` that implements `I*Handler`. | `GetBoundingBoxQueryHandler` |
| `I*Ops`, `IGeometry*` | A negotiated geometry capability interface. | `IMeshOps`, `IGeometryQuery` |
| `*Bus`, `*Registry`, `*Sink`, `*Cache` | `Engine.Core` infrastructure. | `CommandBus`, `IdempotencyCache` |
| `*Endpoint` | An `Engine.Api.Http` route handler. | `SchemaEventsEndpoint` |
| `*GateTests` | A test class that enforces a rule. | `WriteSetGateTests` |

A contract record is `sealed` by default. Only `Document` is a mutable `class`.

**Event kinds.** A domain event kind is `<noun>.<verb-past>`, lowercase, with a dot: `command.applied`,
`body.created`, `document.saved`. The list lives in `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs`.
A control frame is not a domain event and does not follow the grammar: `heartbeat`,
`subscription.resume`, `subscription.reset`. A new event kind is a contract change and needs an ADR.

**Cite the ADR at the decision point.** A file, and a decision inside it, cites the ADR and the
section that constrains it, in the form of the adjacent code:

```csharp
// Per ADR-0012 §4: handle is deterministic from CommandId. Replay against
// a fresh backend produces identical state.
```

A search for `ADR-0012` then finds each site that the record binds.

**Diagnostic codes.** `docs/diagnostics.md` gives the grammar and the registry.

---

## 5 What each gate reads

A gate is a test class whose name ends in `GateTests`. `DocumentPathGateTests` fails when a gate
class has no row here. Each class comment gives the rule that the class enforces and the incident
that made it necessary.

| Gate | It reads | It fails when |
|---|---|---|
| `AdrGateTests` | the ADR front matter and the index | a field is absent, a status is outside the set, a supersession or an amendment is not reciprocal, the index disagrees, or the count of unenforced ADRs grows |
| `AdrEnforcementExistsGateTests` | `enforced-by` of each ADR in force | the file is absent and no Ready task creates it |
| `TaskGovernanceGateTests` | `governed-by`, `depends-on` and `writes` of each task | `governed-by` differs from the intersection rule on an open task, or `depends-on` names no task |
| `WriteSetGateTests` | the `writes` block of each task and the change list | a changed path is outside the write set of the governing task, a task forbids what it writes, or the count of tasks without front matter grows |
| `DependencyDirectionGateTests` | each project file | a reference breaks the authority diagram, a project is in no class, or a binding has two pins |
| `DeterminismCallGateTests` | each source in the log path and each project file | a forbidden function is called, or a 32-bit runtime identifier exists |
| `RegisterGateTests` | the open entries of `docs/register.md` | a `due` date passed, a second extension exists, the count is above 15, or a class or a date is invalid |
| `MarkerGateTests` | each source, each configuration file and each workflow | a marker word has no register identifier |
| `DiagnosticsRegistryGateTests` | each `Engine.*` source and `docs/diagnostics.md` | a code in the source is absent from the registry |
| `DiagnosticsReserveGateTests` | the reserved table of `docs/diagnostics.md` | more than two codes are reserved, or a reserved code has no reason |
| `NativePackageGateTests` | `THIRD-PARTY-NOTICES.md` and the native package | a checksum differs, or a binary has no checksum |
| `ObjectiveReferenceGateTests` | `docs/CHARTER.md` and each document | an objective is not numbered 1 to 20, or a citation names an objective that does not exist |
| `CodebaseReviewGateTests` | `docs/reviews/*-codebase-review.md` and the ledger | a field or a measure is absent, a finding of the previous review has no state, or more than six milestones follow the last review |
| `DocumentPathGateTests` | `docs/INDEX.md` and this table | a path does not exist, or a gate class has no row |
| `DispatchSurfaceGateTests` | each host source and `HandlerCatalog` | a host names a command or a query |
| `SchemaEndpointGateTests` | the `/schema` endpoints and the handlers | the output differs from the declaration, or an endpoint names a command |
| `ReplayDeterminismGateTests` | the fixture | the replay result differs, except for the timestamp and the document identifier |

`.github/workflows/ci.yml` runs three jobs. `gate` builds, tests and runs two smoke tests on
Windows, Linux and macOS. `contract-gate` fails a push that changes `Engine.Contracts/**` with no
change under `docs/adr/`. `write-set-gate` runs `WriteSetGateTests` for each commit after the
cut-off in `eng/write-set-cutoff.txt`.

---

## 6 The rule for rules

1. A rule is born from one of two sources: an objective or an anti-objective in the charter, or a
   register entry that closed with the exit "decided". The rule names its source.
2. A rule has one of three forms: a gate, a form field that a gate reads, or one line of text.
3. A text rule that an agent broke two times becomes a gate, or the rule is deleted.
4. Each rule has one home. A second file points at the home in one line. It does not repeat the
   rule.
5. A rules review repeats with each codebase review. It repeats the measures of
   `docs/reviews/2026-09-25-rules-review.md`, section 1, so that growth is visible.

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

---

## 8 Register entry

The form, the classes, the exits and the rules are in `docs/register.md`. Copy the form from there.
