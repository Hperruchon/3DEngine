---
id: 0092
title: An agent proposes commands, and the person accepts or rejects them
status: Amended
topic: Surface, clients
date: 2026-11-08
supersedes: []
superseded-by: []
amends: []
amended-by: ['0107', '0108', '0110', '0111', '0113']
affects:
  - Engine.Api.Http/Proposals.cs
  - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Api.Http/EngineHost.cs
  - Engine.Tests/Http/ProposalsEndpointTests.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: Engine.Tests/Http/ProposalsEndpointTests.cs
---

# ADR-0092 — An agent proposes commands, and the person accepts or rejects them

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This record was **Proposed** on 2026-11-08. On the same day the owner answered Q1 A, Q2 A, Q3 B and Q4 A. Q3 B is not the
recommendation of the record: the owner chose a queue of proposals. TASK-0326 (phase R16r) accepts the record.

## Context

The brief of the owner (2026-11-04) says: "the agent proposes, the person sees a preview and accepts or rejects", with the
pattern of the contour proposal (ADR-0047). The "mix" direction of the designer brief (2026-11-05) draws it as an arc: the
line "Agent · 1 proposal", the change ("Widen the bracket to 75 mm", `SetSketchConstraintValue C3 75`), its scope ("1 value
· 1 rebuild"), and the buttons Reject and Accept. The research board of the brief names five points of agent guides: show
the change before it applies, show its scope, design the undo first, show a line of activity, and record who did what.

What exists:

- ADR-0047: a contour proposal stays outside the log until the person accepts it; an accept sends ordinary commands, which
  the log holds as the commands of a person.
- ADR-0082 item 4: a value on an edge has a pending form, a chip with a dashed border in the colour of the dimensions.
- ADR-0019 and ADR-0030: an agent reaches the desktop over HTTP on 127.0.0.1, with `POST /commands` and the events.
- Undo takes one command at a time; no command records its author (the slice "author of a command" waits for the owner).

### Q1. Where a proposal lives

- **A.** In the surface of the host (`Engine.Api.Http`), outside the log. An agent posts a proposal: a summary and a list of
  command envelopes, as `POST /commands` takes them. The desktop shows it; an accept applies the commands in their order
  through the session, as the commands of a person; a reject drops it. `Engine.Core` and `Engine.Contracts` do not change.
- **B.** In the session of `Engine.Core`, with new events `proposal.created` and `proposal.closed` for each client.

A follows ADR-0047 and changes no contract of the engine; B lets each client see a proposal, at the cost of new event kinds.
**Recommended: A.**

### Q2. The preview

- **A.** No simulated result in the first slice: the arc shows the summary, the name and the parameters of each command,
  and the count of the commands; a command that sets a value of ADR-0082 shows that value as a pending chip.
- **B.** A dry run: the engine applies the commands to a copy of the session and gives the bodies of the result.

A needs no copy of a session; B shows the true result but needs a fork of the session, which does not exist.
**Recommended: A.**

### Q3. How many proposals

- **A.** One pending proposal at a time. A second proposal while one is pending gets HTTP 409 and a code, so no proposal
  replaces another one with no sign.
- **B.** A queue of proposals.

**Recommended: A.**

### Q4. The undo

- **A.** An accept of N commands gives N steps of undo, as N commands of a person; the arc says so.
- **B.** One step of undo for one accept, which needs a group of commands in the log.

B changes the log. **Recommended: A.**

## Decision

The owner chose A for Q1, Q2 and Q4, and B for Q3.

1. **The place (Q1 A).** A proposal lives in the queue of the host, `EngineHost.Proposals`, in `Engine.Api.Http`, outside
   the log. `Engine.Contracts` does not change, and nothing enters the log before an accept. `Engine.Core` gets only the
   constants of the three new codes of item 3, beside the other codes of the surface.
2. **The routes.** The surface serves four routes, with the checks of `POST /commands` (the Host filter and the open of a
   file, ADR-0030 item 4):
   - `POST /proposals` takes `{ "summary": string, "commands": [envelope, …] }`. An envelope is the body of
     `POST /commands`. The summary is required and has at most 120 characters; the list has from 1 to 16 envelopes. The
     surface checks each envelope at the post, as `POST /commands` does: a known name and version, and parameters that bind.
     A refusal is HTTP 400 with `E-API-BAD-REQUEST` and the index of the envelope. An accepted post is HTTP 201 with the
     proposal.
   - `GET /proposals` gives the open proposals, the oldest first.
   - `GET /proposals/{id}` gives one proposal, open or closed. The queue keeps the last 32 closed proposals, so an agent
     reads the outcome of its proposal.
   - `DELETE /proposals/{id}` withdraws an open proposal; the agent changed its mind.
   No route accepts or rejects. Only the person in the window does, so an agent cannot accept its own proposal.
3. **The queue (Q3 B).** The queue holds at most 8 open proposals, first in, first out. The window shows the oldest one, with
   its place ("1 of 3"). The codes are:
   - `E-API-PROPOSAL-QUEUE-FULL`: a ninth open proposal, HTTP 409.
   - `E-API-PROPOSAL-NOT-FOUND`: no open or kept proposal has the identifier, HTTP 404.
   - `E-API-PROPOSAL-CLOSED`: a withdraw of a closed proposal, HTTP 409.
4. **A proposal.** A proposal has an identifier, its summary, its envelopes, the version of the Document at the post, and a
   state: `open`, `applying`, `accepted`, `rejected`, `withdrawn` or `failed`. A closed proposal keeps the results of the
   commands that ran. When the version of the Document is not the version at the post, the window says "The document
   changed after the proposal". The accept still runs; an envelope with `expectedDocumentVersion` guards itself.
5. **The preview (Q2 A).** The window shows the summary, the count of the commands and the text of each command: its name
   and its parameters, at most four lines, and "and N more". It shows no simulated result. The pending chip of a value of
   ADR-0082 is a later slice.
6. **The accept and the undo (Q4 A).** An accept applies the envelopes in their order through the session, as the commands
   of a person, off the thread of the frame. Each command is one step of undo, and the arc says "N steps of undo". The
   apply stops at the first command that the engine does not apply: the proposal is `failed`, with the index and the code
   of that command, and the commands before it stay applied. A reject closes the proposal with no command. The status line
   gives the outcome.
7. **The place in the window.** On a landscape display the proposal is an amber arc on the band of the ring of the selected
   body, above it, from 235° to 305° (y down), with Reject at its left end and Accept at its right end, each a circle of at
   least 44 pixels. With no selection the arc uses a ring at the centre of the display, with the inner radius of a ring of a
   body. The caption "AGENT · summary" is above the arc, and the command lines are under it, inside the ring. The tools of
   the ring stay, but dim, while a proposal waits. The top bar says "AGENT IS WAITING" in amber. On a portrait display the
   proposal is a sheet above the anchors, with the same lines, Reject and Accept. The arc does not show on the home screen.
8. **No phone.** The phone has no HTTP surface (ADR-0019), so it shows no proposal.

## Consequences

**Good:** an agent proposes and the person decides, with no change of the engine contract and no change of the log. Several
agents, or one agent with several steps, wait in order and none replaces another.

**Bad:** the preview shows the commands and not their result; the phone shows no proposal; an accept is N steps of undo; the
log does not record that an agent proposed a command; a queue lets an agent post eight proposals before the person sees the
first one.

## History

- 2026-11-08 · TASK-0297 · The record, Proposed, for the check of the owner (the answer of the owner of 2026-11-08: start the
  agent proposal arc).
- 2026-11-09 · TASK-0326 · Accepted, with the answers of the owner of 2026-11-08 (Q1 A, Q2 A, Q3 B, Q4 A) and the decision.
- 2026-11-09 · TASK-0326 · Shipped at v0.309.
- 2026-11-10 · TASK-0330 · ADR-0107 amends item 7: with no ring of a body, the arc is in the room right of the panel, and
  the window of the live sketches does not show while a proposal waits.
- 2026-11-10 · TASK-0334 · ADR-0110 amends item 5: a command that changes a value with a chip shows the change on the chip
  and names the value in its line.
- 2026-11-10 · TASK-0331 · ADR-0111 amends items 2, 6 and 7: a repeated or known command identifier, the actions held while
  an accept runs, and the sheet of a portrait display above the ring.
- 2026-11-10 · TASK-0336 · ADR-0113 item 3 amends item 7: on a portrait display "AGENT IS WAITING" goes under the path
  line, and the panel starts under it. Item 6: the caption and the lines inside the ring stay out of a fixed window.
- 2026-11-10 · TASK-0332 · ADR-0108 item 6 amends item 7: on a landscape display "AGENT IS WAITING", after an amber
  dot, ends 12 pixels left of the command line.
