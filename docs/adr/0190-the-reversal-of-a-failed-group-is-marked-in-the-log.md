---
id: 0190
title: The reversal of a failed group is an Undo with the group of the proposal, and it puts nothing on the redo list
status: Accepted
topic: History, agent
date: 2026-11-27
supersedes: []
superseded-by: []
amends: ['0188']
amended-by: []
affects:
  - Engine.Contracts/Command.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/StepHistory.cs
  - Engine.Core/DocumentSession.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Sketch/SketchRebuild.cs
  - 3DEngine.App/StepTexts.cs
  - Engine.Tests/History/GroupHistoryTests.cs
enforced-by: Engine.Tests/History/GroupHistoryTests.cs
notes: Finding E242 of the codebase review of 2026-11-25. The owner answered Q2 A on 2026-11-26 and R-0081 A on 2026-11-27 ("All A"), TASK-0473.
---

# ADR-0190 — The reversal of a failed group is an Undo with the group of the proposal, and it puts nothing on the redo list

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0188 item 5 applies the commands of an accepted proposal as one group. When command k does not apply, the session reverses
the steps 0 to k−1 with one `Undo`. Until TASK-0473 that `Undo` was an ordinary move, so the unit of the group went to the redo
list. The next `Redo`, of the person or of a client, applied the steps 0 to k−1 again: a part of a proposal that no preview
showed (finding E242 of the codebase review of 2026-11-25). The window offers that `Redo`.

The owner answered Q2 A of that review on 2026-11-26: the reversal of a failed group puts nothing on the redo list, and the log
marks it, so that each walker of the log agrees. Each walker sees an `Undo` after the steps of a group also when the person undoes
an accept that applied, so the log needs a mark. The smallest mark gives the field `GroupId` of `Engine.Contracts` a new meaning
on an `Undo`, and an older app then refuses a newer file at an open. Register entry R-0081 asked the owner, and the owner answered
A on 2026-11-27: accept the mark and its cost.

## Decision

1. **The mark (amends ADR-0188 item 1).** The `Undo` that reverses a failed group has the group of the proposal in `GroupId`.
   The file holds it as each command with a group (`groupId`). An `Undo` with no group keeps its meaning.
2. **Who applies it (amends ADR-0188 item 4).** The session applies the reversal through `CommandBus.Reverse`, in its serial
   section and with no cancellation. An open and a replay apply an `Undo` with a group as an entry of the log: the bus refused each
   other `Undo` with a group before the file, so each such entry is a reversal. Each other `Undo` or `Redo` with a group is
   refused with `E-CMD-GROUP-MOVE`, as before. A reversal reverses only a last unit with its own group; else it is refused with
   the same code.
3. **No redo (amends ADR-0188 item 3).** A reversal removes the last unit and puts nothing on the redo list: `StepHistory.Reversed`,
   `FeatureList`, `SketchRebuild.InEffect`, and `StepTexts` when the log gives the group. A `Redo` after a reversal is refused with
   `E-CMD-NOTHING-TO-REDO`, and the next `Undo` reverses the unit before the group. An event gives no group, so a live reversal
   moves the unit to the undone list of `StepTexts`; the bus refuses the next `Redo`, so no event moves it back, and the text of
   the next `Undo` is the same.
4. **The files of before.** A file of before TASK-0473 holds the reversal as an `Undo` with no group, so an open gives the same
   history as before: that unit stays on the redo list. No file is rewritten.
5. **The format version stays 1.** In a version of the app before TASK-0473, the bus refuses an `Undo` with a group with
   `E-CMD-GROUP-MOVE`, and the open then fails with `E-IO-LOAD-FAILED`, so that version cannot open a file that holds a
   reversal. The owner accepted this cost (R-0081 A).

## Alternatives considered

- **ADR-0188 "Bad" records the `Redo` (Q2 B).** Rejected by the owner: a `Redo` must not apply a part of a proposal.
- **The window refuses its `Redo` after a failed accept (Q2 C).** Rejected: a client of the surface can still send the `Redo`,
  and the bus and the window would disagree.
- **A new command `ReverseGroup`.** A new name in the catalogue and in the schema for one case, with the same cost for an older
  app. The mark on `Undo` keeps one command for each move.
- **A new format version.** An older app would refuse each newer file, also one with no reversal. The refusal of the entry
  refuses only a file that needs it.

## Consequences

**Good:** a failed accept leaves no `Redo` that applies a part of the proposal, in a live session, after an open and in a replay,
and each walker of the log follows one rule.

**Bad:** an older app cannot open a file that holds a reversal. A file of before TASK-0473 keeps the old behaviour for its old
reversals. `StepTexts` keeps a unit on its undone list after a live reversal; no event can move it back, but its state differs
from the state of the bus until the next step clears the list.

## History

- 2026-11-27 · TASK-0473 · The record, Accepted: the answers Q2 A of 2026-11-26 and R-0081 A of 2026-11-27 of the owner.
  Amends ADR-0188 items 1, 3 and 4.
