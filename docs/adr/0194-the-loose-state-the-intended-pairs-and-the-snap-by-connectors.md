---
id: 0194
title: A joint is tight or loose, each joint is an intended pair of the check, and PlaceByConnectors places a child one time
status: Accepted
topic: Contracts, geometry, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: ['0182', '0193']
amended-by: []
affects:
  - Engine.Contracts/Joints/JointRecord.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Commands/JointTree.cs
  - Engine.Core/Commands/JointResults.cs
  - Engine.Core/Commands/PlaceBodyCommandHandler.cs
  - Engine.Core/Commands/SetJointStateCommand.cs
  - Engine.Core/Commands/SetJointStateCommandHandler.cs
  - Engine.Core/Commands/PlaceByConnectorsCommand.cs
  - Engine.Core/Commands/PlaceByConnectorsCommandHandler.cs
  - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Tests/Commands/JointStateTests.cs
  - Engine.Tests/Persistence/DocumentFileTests.cs
enforced-by: Engine.Tests/Commands/JointStateTests.cs
notes: Phase P11b, TASK-0489. The owner answered "Yes, all A" for Q1 to Q7 of docs/proposals/2026-11-28-the-joints-of-p11.md on 2026-11-28; Q5 A and Q6 A decide this ADR.
---

# ADR-0194 — A joint is tight or loose, each joint is an intended pair of the check, and PlaceByConnectors places a child one time

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0193 item 10 (the answer Q7 A) left three parts of row P11 to P11b: the loose state of a joint (the answer Q5 A), the intended
pair of each joint in `CheckInterference` (ADR-0157 item 5), and a command that places a body by two connectors one time and
records no joint (the answer Q6 A). `CheckInterference` takes the intended pairs as the parameter `intendedPairs` "until P11 and
P13 put them in the Document" (ADR-0182 item 2). The state is a new field of `JointRecord`, so it changes the public shape of
`Engine.Contracts`, and this ADR comes in the same commit.

## Decision

1. **The state (Q5 A).** `JointRecord` has the field `State`, `tight` or `loose`, and `tight` by default, so a log of P11a gives
   the same records. `AddJoint` makes a tight joint. A tight joint gives the placement of its child (ADR-0193 item 3). A loose
   joint keeps its record, its connectors, its value, its limits and its intended pair, and gives no placement. `joint.created`
   and `joint.changed` carry the member `state`.
2. **The command `SetJointState`** takes `jointId` and `state`. The state `loose` moves nothing. The state `tight` places the
   child and each body under it at the value of the joint, in the same step, as `SetJointValue` does. A state that the joint has
   already changes nothing, so it is refused with `E-JOINT-INVALID`, as a text that is not a state is. A loose joint still counts
   for the one parent and for a loop (ADR-0193 item 6), because it keeps its record. A `SetJointValue` of a loose joint changes its
   record only. A `PlaceBody` of the child of a loose joint applies, and a `PlaceBody` of a body above a loose joint does not move
   the child of that joint or the bodies under it (amends ADR-0193 item 6, which refused a `PlaceBody` of each child).
3. **The intended pairs in the Document (amends ADR-0182 item 2).** Each joint of the Document, tight or loose, declares its two
   bodies as an intended pair of `CheckInterference` when the check holds the two bodies (ADR-0157 item 5). The pairs of the
   parameter `intendedPairs` stay, with their refusals; a pair of a joint that the parameter gives too is one pair. The
   requirements of P13 can add pairs that no joint declares.
4. **The command `PlaceByConnectors` (Q6 A).** `PlaceByConnectors` takes `parentBodyId`, `childBodyId` and a connector on each,
   with the fields and the rules of `AddJoint` (ADR-0193 item 2). It gives the child the placement of a fixed joint at the place
   of the parent now, and records no joint; each body under the child through a tight joint follows it in the same step. The child
   of a tight joint is refused with `E-JOINT-PLACED`; a parent under the child through tight joints is refused with
   `E-JOINT-LOOP`, because the move of the child would move the parent too; the same body for the two sides gives
   `E-JOINT-INVALID`. The log holds the command, and the frames come from the shapes at its place in the log, so a replay gives
   the same placement. The placements are a result of the step (ADR-0193 item 9).
5. **The diagnostic codes.** No new code. `E-JOINT-INVALID`, `E-JOINT-NOT-FOUND`, `E-JOINT-CONNECTOR-INVALID`, `E-JOINT-LOOP` and
   `E-JOINT-PLACED` are also raised by the two new commands, with the meaning of ADR-0193 item 8.

## Alternatives considered

- **No state; a person removes the joint and adds it again (Q5 B).** The connectors and the limits are then lost, and the
  intended pair with them.
- **A query that gives the seven numbers, and the client sends `PlaceBody` (Q6 B).** Two calls for one act, and the log holds no
  connector, so a reader of the log does not see why the body is there.
- **A loose joint that does not count for the tree.** A later state `tight` could then give a body two parents.

## Consequences

**Good:** a person can move the child of a joint away to examine it and put it back with one command, the check knows the
contacts of the joints with no parameter, and a body snaps to another by two faces with one command.

**Bad:**
- A loose joint keeps its child out of the tree, so a `PlaceBody` of the root no longer moves it; a client that wants the child to
  follow sets the state `tight` first.
- `PlaceByConnectors` reads the frames of the connectors at its place in the log. A later change of a shape does not move the
  child again (R-0083).
- The child of a joint whose parent a command consumed is still refused by `PlaceBody` when the joint is tight (finding E254 of
  the second review of 2026-11-28); P0.201 holds it with the answer Q1 A of that review.

## History

- 2026-11-28 · TASK-0489 · The record, Accepted: the answers Q5 A and Q6 A of the owner of 2026-11-28. Amends ADR-0182 item 2 and
  ADR-0193 item 6.
