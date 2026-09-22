---
id: 0021
title: Correct the status vocabulary, the register limit and one false statement
status: Done
phase: P0.8
opened: 2026-09-21
depends-on: [0018]
governed-by: []
writes:
  create:
    - tasks/TASK-0021-correct-three-governance-items.md
  modify:
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/adr/0015-command-log-persistence.md
    - tasks/TASK-0018-salvage-the-unmerged-branch.md
    - tasks/TASK-0019-command-log-persistence.md
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Governance/RegisterGateTests.cs
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
---

# TASK-0021 — Correct the status vocabulary, the register limit and one false statement

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Three items came out of the review of v0.19 and v0.20. The owner decided each one on 2026-09-21.

**1. Three status vocabularies for one field.** `docs/templates.md` gave one set,
`Engine.Tests/Governance/AdrGateTests.cs` gave a second, and the legend in `docs/adr/README.md` gave a
third. `CLAUDE.md`, section "Stop and ask", forbids an agent from correcting either side alone, so the
v0.20 report gave both and stopped.

The gate also contradicted itself. `ValidStatuses` did not contain `Superseded`, and
`A_Status_Agrees_With_Its_Supersession_And_Amendment_Fields` required that value. The first superseded
record would therefore have failed one test whichever document was right. That part is a defect and
not a preference.

**2. The register limit.** Register entry R-0016 asked whether a limit of 20 open entries is too high
to have an effect.

**3. A false statement in three records.** ADR-0015, TASK-0018 and TASK-0019 each said that the clamp
in `CLAUDE.md` forbids persistence, and each used that fact to support the status `Proposed`. No such
clamp exists. Milestone v0.17 removed it, in TASK-0015, four days before TASK-0018 ran.

## Goal

Each governance statement agrees with each other governance statement, and with the file that it
describes.

## Scope (in)

1. Correct the status set in `docs/templates.md`. Add `Superseded` to the gate.
2. Lower the register limit. Close R-0016.
3. Remove the false statement from ADR-0015 and TASK-0019. Append a correction block to TASK-0018.

## Scope (out)

- Do not change the decision text of any ADR. ADR-0015 has the status `Proposed`, therefore
  `docs/templates.md` permits a change to the note that this project added. The design text from the
  branch stays as written.
- Do not change the closed text of TASK-0018. A correction block goes after it.
- Do not change any entry in `docs/CURRENT-STATE.md`. Working agreement rule 3.2 requires a new entry
  instead.
- Do not change the status of ADR-0015. The owner examined it and kept `Proposed`.

## Acceptance criteria

- [x] `docs/templates.md`, the gate and the index legend give the same status set.
- [x] The gate accepts `Superseded`, therefore it no longer contradicts itself.
- [x] Rule 4 in `docs/register.md` and `RegisterGateTests.OpenLimit` give the same limit.
- [x] R-0016 is closed with the exit `decided`.
- [x] No record states that a persistence clamp exists.
- [x] `dotnet test` passes.

## Outcome

Shipped as v0.21.

**The status set.** The set is now `Proposed`, `Accepted`, `Amended`, `Superseded`, `Withdrawn` and
`Rejected`. `docs/templates.md` gained `Amended` and `Superseded`, and it lost `Superseded-by-NNNN`.
The number belongs in the field `superseded-by`, not in the status, and the front matter already
carries it. `ValidStatuses` gained `Superseded`. Two rows in the field table are new: one for the pair
`amends` and `amended-by`, and one that records that the unenforced budget counts a record in force
only.

**The register limit.** The limit is 15 and not the 12 that R-0016 proposed. The register held 11
open entries on the day of the decision. A limit of 12 gives one free slot, therefore the next new
problem would force an exit on an old one immediately. A limit of 15 gives four free slots. The limit
has an effect and it does not stop work. Rule 4 and the gate both give 15.

**The false statement.** ADR-0015 now gives two reasons for the status `Proposed` and not three, with
a correction note. TASK-0019 now gives one unblock condition and not two, with a correction note.
TASK-0018 is closed, therefore its text stays and a correction block follows it. The conclusion does
not change: the owner examined the question and kept the status `Proposed`.

## Method

**Mechanical.** The status set. The limit in two positions. The closure of R-0016.

**Judgement.** The shape of each correction. A closed task takes an appended block, because the record
of what a person believed at the time has value. A record with the status `Proposed` takes an edit,
because `docs/templates.md` makes the text permanent only after the status becomes `Accepted`. The
ledger takes a new entry, because working agreement rule 3.2 forbids a change to history.

**Weakest.** The cause of the false statement. The agent read the copy of `CLAUDE.md` that arrived in
its context at the start of the session, and not the file. The copy was correct when the session
began and the agent then changed the file itself, in v0.17. No gate can catch this class of error,
because the wrong statement was about a file and it appeared in prose. The only defence is the habit
of reading the file before citing it. This is the same lesson as the dependency gate defect in
TASK-0017: a statement about the repository must come from the repository.
