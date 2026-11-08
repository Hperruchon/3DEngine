---
id: 0092
title: An agent proposes commands, and the person accepts or rejects them
status: Proposed
topic: Surface, clients
date: 2026-11-08
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects: []
enforced-by: none
---

# ADR-0092 — An agent proposes commands, and the person accepts or rejects them

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This record is **Proposed**. On 2026-11-08 the owner chose to start the agent proposal arc of the redesign. The arc changes
the HTTP surface, which an agent reads, so this record gives the shape for the check of the owner before code.

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

None yet. The owner answers Q1 to Q4. With A to each, the slices are: the routes `POST /proposals`, `GET /proposals` and
the accept and the reject in the surface, with their codes in `docs/diagnostics.md`; then the arc of the desktop (the line of
the agent, the summary, the scope, Reject and Accept); then the pending chip for a proposed value.

## Consequences

**Good (with A):** an agent proposes and the person decides, with no change of the engine and no change of the log.

**Bad (with A):** the preview shows the commands and not their result; the phone has no HTTP surface, so it shows no
proposal; an accept is N steps of undo; the log does not record that an agent proposed a command.

## History

- 2026-11-08 · TASK-0297 · The record, Proposed, for the check of the owner (the answer of the owner of 2026-11-08: start the
  agent proposal arc).
