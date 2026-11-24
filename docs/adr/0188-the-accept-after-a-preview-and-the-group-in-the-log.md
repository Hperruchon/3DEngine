---
id: 0188
title: The accept after a preview compares the result and applies a proposal as one group of steps, which the log holds and one Undo reverses
status: Accepted
topic: Contracts, surface, agent
date: 2026-11-24
supersedes: []
superseded-by: []
amends: ['0015', '0031', '0092', '0130', '0182', '0185', '0187']
amended-by: []
affects:
  - Engine.Contracts/Command.cs
  - Engine.Core/Persistence/EntryCodec.cs
  - Engine.Core/StepHistory.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/DocumentSession.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Sketch/SketchRebuild.cs
  - Engine.Core/Queries/CheckInterferenceQuery.cs
  - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
  - Engine.Core/Preview/**
  - Engine.Api.Http/Proposals.cs
  - Engine.Api.Http/Proposals.Preview.cs
  - Engine.Api.Http/PreviewContent.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.App/StepTexts.cs
  - 3DEngine.App/ProposalPreview.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Preview.cs
  - Engine.Tests/History/GroupHistoryTests.cs
  - Engine.Tests/Http/ProposalAcceptTests.cs
  - Engine.Tests/Http/ProposalPreviewTests.cs
  - Engine.Tests/Http/ProposalsEndpointTests.cs
  - Engine.Tests/Guides/AgentGuideExamplesTests.cs
  - 3DEngine.ImageTests/FixedStatusTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
  - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
  - 3DEngine.ImageTests/ProposedSheetTests.cs
enforced-by: Engine.Tests/Http/ProposalAcceptTests.cs
notes: P16 (TASK-0463). ADR-0157 items 9 and 10 give the row; the owner answered Q2, Q3, Q4 and Q6 of the codebase review of 2026-11-23 with option A on 2026-11-23, and ordered "Start P16" on 2026-11-24.
---

# ADR-0188 — The accept after a preview compares the result and applies a proposal as one group of steps, which the log holds and one Undo reverses

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P16 of the roadmap: "The accept after a preview: the accept runs the dry run again; it applies when the result equals the
result that the person saw, shows the new preview when it differs, and applies each command or none; one proposal is one step
of undo. Its ADR amends ADR-0092 items 4 and 6, ADR-0130 item 4 and ADR-0031 (ADR-0157 items 9 and 10)."

What exists (sections 5 and 6 of `docs/reviews/2026-11-23-codebase-review.md`):

- ADR-0092 item 6: the accept applies each command through the session, one serial section and one log entry each. It stops
  at the first refused command, and the commands before it stay applied. Each command is one step of undo.
- ADR-0031: `Undo` has no parameter, and an open rebuilds the history from the log (`Engine.Core/StepHistory.cs`). A group of
  steps that lives only in memory gives another history after an open, and a later `Undo` in the log then reverses one command
  in the replay. So the group must be in the log, which is a change of `Engine.Contracts`.
- ADR-0185: the queue keeps the last preview of each proposal, computed on a fork at a version of the Document. By the replay
  rule (ADR-0012), a fork at one version with the same commands gives the same result.
- `ProposalPreview` holds lists, and the equality of a record compares a list by reference.
- `Command.ExpectedDocumentVersion` guards one command, not a group, and a commit can come between the dry run and the apply.
- The bus has no rollback of the backend (finding E21, R-0033).
- ADR-0185 "Bad": the checks of one preview have no bound of their sum, and the accept now makes the person wait for a fork.

The owner answered the questions of the review on 2026-11-23 ("all A", recorded in row P16 and in the History of ADR-0185 by
TASK-0462), and ordered "Start P16" in the chat on 2026-11-24:

- **Q2 A:** a new optional field of a command, the identifier of its group, which the accept sets; the history joins the steps
  of one group. This is the change of the public shape of `Engine.Contracts` that `CLAUDE.md`, "Stop and ask", names; the
  answer of the owner is the approval, and this record comes in the same commit.
- **Q3 A:** each command commits as a step of the group, and a failure gives an `Undo` of the group, which the log holds; the
  proposal is `failed`. No rollback of the backend.
- **Q4 A:** an accept at the version of the last preview runs no new fork, because the replay rule makes that preview the
  result; a new fork runs only for a new version.
- **Q6 A:** a bound of the sum of the work of the checks of one preview: the check gives the work of a pair before it splits,
  the fork adds the work of its pairs, and each pair after the bound has the state `limit`. The owner put it in P16 or in a task
  before it; this record puts it here, because the accept now makes the person wait for a fork.

## Decision

1. **The field (Q2 A, amends ADR-0031 item 2).** `Command` gains `GroupId`, a `Guid?`, null by default. A command with no group
   is a step as before. The accept sets the identifier of the proposal as the group of each command of the proposal. No route
   takes the field: an envelope of `POST /commands` and of `POST /proposals` refuses an unknown member (finding E16), so only
   the queue of the proposals sets it.
2. **The log (amends ADR-0015 item 3).** The entry of a command with a group holds `"groupId":"…"` in its object `command`,
   after `expectedDocumentVersion` and before `parameters`. The entry of a command with no group has no such member, so its
   bytes are the bytes of the same entry before this record, and each older file, each replay and the hash of the determinism
   probe do not change. An open reads the member when it is present; a member that is not a string of a UUID refuses the open
   with `E-IO-LOAD-FAILED`. The format version stays 1.
3. **The unit of undo (amends ADR-0031 items 1, 2 and 4).** The history joins consecutive steps of one group: a unit of undo is
   the run of consecutive steps with the group of the last step, or the last step when it has no group. `Undo` reverses the
   whole last unit in one commit: the changes of ADR-0031 item 4 of each step, from the last step to the first. `Redo` applies
   the whole last undone unit again, from the first step to the last. The commit gives `command.applied` once, then the events
   of each step in the order of ADR-0031 item 5, one step after the other. The output `commandId` gives the last step of the
   unit. The log holds the group of each step, so an open and a replay rebuild the same units: `StepHistory` of the bus,
   `FeatureList` and `SketchRebuild.InEffect` of the engine, and `StepTexts` of the window apply the same rule.
4. **A group holds steps only.** An `Undo` or a `Redo` with a group is refused with the new code `E-CMD-GROUP-MOVE`, before the
   history and the file. A move in a group would move a unit that the group itself builds, and a failed accept could then
   reverse a step of the person. So a proposal with an `Undo` stops at that command in its preview, and the accept applies
   nothing.
5. **The apply (Q3 A and Q4 A; amends ADR-0092 item 6).** `DocumentSession.ApplyGroup` runs the commands of one group in one
   serial section, at a given version. When the Document has another version, nothing applies and the outcome is stale. Each
   command commits as a step of the group, with its own log entry. The first command that does not apply stops the group: a
   refusal, a cancellation, or a command whose identifier the bus knows (finding E198, found by the version). The section then
   applies one `Undo` with no cancellation, which reverses each step of the group that applied, and the log holds it. A handler
   that throws gets the same `Undo`, and its exception goes to the caller. So the accept applies each command or none, and the
   bus needs no rollback.
   While the group commits, its own commits ask for no preview of its proposal, so the kept preview stays the result that
   applied (ADR-0185 item 5). A caller that waits for a preview of a version later than the last preview asks for a new copy of
   the log itself.
6. **The comparison (ADR-0157 item 9; Q4 A).** The result that the person saw is the preview of the version that the window
   showed (`Proposals.Accept(id, seenVersion)`), or the last preview of the queue (`Proposals.Accept(id)`). The accept reads the
   version of the Document:
   - When it is the version of the preview that the person saw, that preview is the result, and no fork runs.
   - Else the worker of the queue runs the fork at the new version (ADR-0185 item 3), and the accept waits for it. The two
     results compare by content (`PreviewContent`): the outcome of each command with its outputs and the codes of its
     diagnostics and its error, the bodies, the sketches, the measures and the checks. The version, the sequence and the time
     of a result are not compared, and a message is not compared.
   - A result that differs, or no preview that the person saw, applies nothing. The proposal is `open` again with the new
     preview and the new shapes, and the member `held` of the proposal gives the new code `W-API-PROPOSAL-RESULT-CHANGED` and a
     message. The window shows the note "The result changed: check it" at the top of the panel of the
     preview, and the status line says "Not applied: the result changed. Check the new preview, then accept again". The next
     accept clears `held`.
   - An equal result applies with `ApplyGroup` at the version of the result. A commit between the comparison and the apply
     makes the group stale, and the accept compares again at the new version. So nothing applies on an old result.
   - A host with no factory of forks gives no preview, and its accept applies the group with no comparison.
7. **A result that stops (amends ADR-0092 items 4 and 6).** When the equal result stops at a command (`failedIndex`), or the
   fork failed (`E-API-PREVIEW-FAILED`), the accept applies nothing, and the proposal is `failed` with that index and that code.
   A failed proposal applied nothing: its `results` give the result of each command that ran before an `Undo` of the group
   reversed it. The arc says "N commands · 1 step of undo", and an accepted proposal says "Proposal accepted: N commands
   applied, 1 step of undo" (amends ADR-0130 item 4: the button Undo undoes the whole proposal).
8. **The bound of the checks of one preview (Q6 A; amends ADR-0182 item 8 and ADR-0185 item 10).** The fork gives each check of
   a pair the work that the bound of the preview leaves, 100,000 products of the counts of the pieces (the limit of one check)
   less the work of the checks before it, after the commands and then before them. The check counts its work before it splits,
   as ADR-0182 item 8 says, refuses the pair with `E-CHECK-WORK-LIMIT` when the work is above the limit that it got, and else
   gives the work that it counted to the fork. The first pair that its check refuses for the work, and each pair after it in the
   order of the plan, has the state `limit`, and the fork does not check it. So one preview costs at most the work of one check
   at its limit, about 13 s for an intersection on the computer of the owner. A client cannot give the limit: the query has no
   such parameter, and its answer does not change.
9. **The window (amends ADR-0187 items 3 and 6).** The window gives the accept the version of the preview that its panel
   showed. `Proposals.Kept` gives the code of the note of a held accept, and the panel shows the note first. The line of the
   outcome of an accept or a reject and the panel can now show together, so the panel ends 6 pixels above that line when it
   shows, and else 6 pixels above the status line.

## Consequences

**Good:** the person accepts what the preview showed, or sees the new result first. A proposal is one step of undo, also after
a save and an open, because the log holds the group. A failed accept leaves the Document as it was, with no rollback of the
backend. A preview has a bound of its work, so an accept never waits for minutes of checks. An entry with no group keeps its
bytes, so each older file opens and replays as before.

**Bad:**
- The public shape of `Engine.Contracts` changes: a new optional member of `Command`. The document file gains an optional
  member of an entry.
- An engine before this record reads a file with a group and ignores the member, so it replays a group as separate steps; an
  `Undo` of a group in that file then reverses one step in that engine, and its Document differs. The repository ships one
  engine, and the format version stays 1, so nothing refuses such a file.
- A failed accept writes the steps of its group and one `Undo` to the log: a failure costs N + 1 entries that change nothing.
- An accept after a new version waits for a fork. An accept after an unrelated commit by another client applies only after the
  new fork, and then applies.
- The comparison reads no message, so two results that differ only in a message compare equal.
- A pair after the bound shows `limit` on each side, so a pair that the bound skips on both sides shows no difference (as a
  pair above the bound of one check in ADR-0185 item 10). A preview with many pairs shows `limit` for the last pairs in the
  order of the identifiers, not for the most costly ones.
- The window gives the version of the preview of the last frame. A preview that came between that frame and the tap counts as
  a preview that the person did not see, so that accept applies nothing and shows the new preview.
- The proposal of an agent with an `Undo` or a `Redo` never applies (item 4).

## Alternatives considered

- **Q2 B: one command that holds the commands of a proposal and commits once.** Refused by the owner: a new command type with a
  list of envelopes, and a failure in the middle needs a rollback of the backend (Q3 B), which the bus does not have.
- **A group in memory only.** An open rebuilds another history, and the replay of a later `Undo` diverges (section 5 of the
  review).
- **Q4 B: a new fork for each accept.** The replay rule makes the preview of the same version the result; a fork costs a replay
  and the checks while the person waits.
- **A field `groupId` with the value null in each entry.** Each entry of each new file then has new bytes, and the hash of each
  fixture that writes a file changes for no decision.
- **A member `work` in the answer of `CheckInterference`.** An agent does not need it, and the guide and its examples then
  change; the fork reads the work in the process.

## History

- 2026-11-24 · TASK-0463 · The record, Accepted: the answers of the owner of 2026-11-23 (Q2 A, Q3 A, Q4 A and Q6 A of the
  codebase review of 2026-11-23) and the order "Start P16" of 2026-11-24.
- 2026-11-24 · TASK-0463 · Amends ADR-0015 item 3, ADR-0031 items 1, 2 and 4, ADR-0092 items 4 and 6, ADR-0130 item 4, ADR-0182
  item 8, ADR-0185 items 3 and 10, and ADR-0187 items 3 and 6.
