---
id: 0111
title: A proposal takes its own taps, its own command identifiers and its own steps
status: Amended
topic: Surface, clients
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0092']
amended-by: ['0114', '0135']
affects:
  - Engine.Api.Http/Proposals.cs
  - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
  - Engine.Tests/Http/ProposalsEndpointTests.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/AcceptHold.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: 3DEngine.ImageTests/ProposalArcTests.cs
---

# ADR-0111 — A proposal takes its own taps, its own command identifiers and its own steps

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0092 lets an agent propose commands that the person accepts or rejects. The codebase review of 2026-11-09 (b) found four
places where a proposal and the work of the person mix:

- V160: Reject and Accept are on the band of the ring of the selected body, and one tap on Accept also used the tool of the
  ring under it.
- E198: the post kept a `commandId` that repeats in the proposal, and the bus gives the cached result for an identifier that
  it knows. The proposal counted that result as applied, although nothing applied.
- V162: on a portrait display the sheet of the proposal ended above the anchors and covered each tool of the ring, although
  item 7 says that the tools stay, dim, while a proposal waits.
- V163: the accept applies its commands one at a time, and an Undo of the person could run between two of them.

E199, a refusal of a withdraw with a wrong reason, is a text only. TASK-0331 (phase P0.139) holds these findings.

## Decision

1. **The taps (item 7).** Reject and Accept, with their words, take a tap before the ring, at the press and at the release.
   The ring reads the targets of the last frame, as it reads the chips of the last frame (finding V120).
2. **The identifiers (amends items 2 and 6).** The post refuses an envelope whose `commandId` an earlier envelope of the same
   proposal holds: HTTP 400, `E-API-BAD-REQUEST`, and the index of that envelope. At the accept, a command whose identifier
   the session knows gets the cached result of the bus, and its commit sends no `command.applied`. The accept stops there,
   as at a rejected command: the commands before it stay applied, and the proposal is failed with
   `E-API-PROPOSAL-COMMAND-KNOWN`, the index of that command, and no result for it.
3. **The accept runs alone (amends item 6).** While the commands of an accept apply, the window holds each action and each
   intent that can change the Document, and the line of the document says "The commands of the proposal apply now. Do it
   again when they are done." A pad stays open with that text. A file that a dialog gives waits for the end of the accept.
   The camera, the selection, the frame, the home screen, Accept and Reject, and the tools that open a pad or choose a tool
   stay free. The end of a drag of the gizmo is a cancel. An action that the list of free actions (`AcceptHold`) does not
   name is held.
4. **The sheet of a portrait display (amends item 7).** The sheet ends 8 pixels above the highest point of the band of the
   ring, so each tool of the ring takes its tap, when the sheet of the last frame fits between the top bar and the ring. When
   it does not fit, or with no ring, the sheet ends above the anchors, as before, and covers the ring.
5. **The withdraw.** The refusal `E-API-PROPOSAL-CLOSED` of a withdraw names the state of the proposal; for the state
   `applying` it says that the person accepted the proposal and that its commands apply now.

## Consequences

**Good:** a tap on an end of the arc gives only the action of the proposal. The count of applied commands and the steps of
undo of an accept agree with the log. No command of the person falls between two commands of an accept. On a phone of 390 by
844 the person can use each tool of the ring while a proposal waits.

**Bad:** an agent that posts a proposal again with the same identifiers, after a lost answer, gets a failed proposal and
not a quiet success; it must read `failedCode`. During an accept the person cannot undo, and each held action must be done
again. On a small portrait display the sheet still covers the ring.

## History

- 2026-11-10 · TASK-0331 · The record, for findings V160, E198, V162, V163 and E199 of the codebase review of 2026-11-09 (b).
- 2026-11-10 · TASK-0331 · Shipped at v0.315.
- 2026-11-10 · TASK-0339 · ADR-0114 item 3 amends item 3: the path line, the focus and the end of a focus are free during an
  accept (finding V165). ADR-0114 item 4: AcceptHold.Held and AcceptHold.Pass make the decision of the host (T181).
- 2026-11-14 · TASK-0368 · ADR-0135 item 3 amends item 4: the sheet of a portrait display is under the arc at the top, so
  it no longer ends above the ring or above the anchors.
