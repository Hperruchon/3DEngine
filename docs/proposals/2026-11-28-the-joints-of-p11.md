# The joints of P11

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-28. TASK-0479, phase P11a. Written under the standing order of 2026-11-28.
- Answers: on 2026-11-28 the owner answered "Yes, all A" for Q1 to Q7 (TASK-0479). The ADR of P11a records them.
- Status: a proposal. Not a rule. It changes no code, no contract and no ADR. The ADR of P11a comes after the answers of the
  owner, in the same commit as the change of `Engine.Contracts`.
- Why a proposal first: the joints are a new record of the Document, new commands and new events, so they change the public
  shape of `Engine.Contracts`, and `CLAUDE.md` ("Stop and ask") asks the owner before such a change. ADR-0157 items 3 to 5
  decide the frame of the joints; the questions in section 5 are the details that those items do not decide.

## 1 The question

Row P11 of the roadmap: joints in the Document, between two bodies of one part, in a kinematic tree with no solver: connectors,
a fixed joint, a pivot and a slider; limits as a cosine and a sine; the states tight and loose; a refusal of a joint that
closes a loop; a joint declares its two parts as an intended contact; a command places a body by two connectors one time.
P11.0 (ADR-0191) gave each body a placement and the absolute command `PlaceBody`. This document proposes the model of a joint
on that base, and the questions that its ADR must answer.

## 2 The facts today

Each fact below was read in the repository on 2026-11-28.

1. **The placement of a body.** A point p of the shape of a body is at R·p + t in the Document, and `PlaceBody` sets the seven
   numbers absolutely; the body keeps its handle and its shape (ADR-0191 items 1 and 3, `Engine.Core/Placements.cs`).
   `Placements.Compose`, `Inverse` and `Relative` give the products of placements with `+ - * /` only.
2. **The frame of the joints.** ADR-0157 item 3: a part has connectors, and a joint connects two connectors; the joints make a
   kinematic tree with one parent for each part; the engine computes each place from the tree with `+ - * /` only; no solver
   is in the engine; a joint that closes a loop is refused with a diagnostic code; the first kinds are fixed, revolute and
   slider. ADR-0157 item 5: a joint declares its two parts as an intended pair.
3. **A connector.** ADR-0157 item 4 and ADR-0183: `GetConnectors` gives the named datums of a body and one entry for each face
   name: the centre and the outward normal of a plane face, and the base and the axis of the side of a cylinder
   (`Engine.Core/Queries/GetConnectorsQueryHandler.cs`). These values are in the frame of the Document.
4. **A datum does not follow a later move.** A datum keeps the frame of its place in the log (ADR-0051 item 2, ADR-0183 item
   3), and ADR-0191 item 5 keeps that rule for a `PlaceBody`: a later `PlaceBody` of the body does not move its datum. A named
   datum as a connector therefore stays where it was when the body moves.
5. **The intended pairs.** `CheckInterference` takes the intended pairs as the parameter `intendedPairs` "until P11 and P13 put
   them in the Document" (row K11, ADR-0182 item 2, `Engine.Core/Queries/CheckInterferenceQuery.cs`).
6. **The travel.** Row K12 checks a pivot from one limit to the other on a fork of the Document, so the limits of a pivot are a
   value of the Document that a query reads.
7. **No record of a joint.** `Engine.Contracts/Document.cs` holds bodies, sketches, images, datums, part references and
   sheets, and no joint.

## 3 The recommended model

1. **A record of a joint.** The Document holds joints. A joint has an identifier (the identifier of its command), a kind
   (`fixed`, `pivot` or `slider`), a parent body and a child body, a connector on each, its value, its limits and its state.
2. **A connector of a joint.** A connector names a face of the body as `GetConnectors` names it (a plane face, or the side of a
   cylinder), or a named datum of the body. The joint reads the frame of the connector in the frame of the body, from its
   shape, so the frame follows each later `PlaceBody` of the body. A plane face gives its centre and its outward normal; the
   side of a cylinder gives the centre of its base and its axis.
3. **The place of the child.** A tight joint gives the placement of its child:
   child = parent · frame(parent connector) · joint(value) · flip · frame(child connector)⁻¹, with `+ - * /` only. `flip` turns
   the child connector so that the two normals are opposite, as two faces that meet. `joint(value)` is the identity for a
   fixed joint, a turn about the normal for a pivot, and a move along the normal for a slider.
4. **The value.** The value of a pivot is a cosine and a sine; the log holds the two numbers and no angle (`CLAUDE.md`,
   determinism rule 2), and the turn is the quaternion of the half angle that a client computes, as for `Rotate`. The value of
   a slider is a length in millimetres. A fixed joint has no value.
5. **The commands.** `AddJoint` (kind, the two bodies, the two connectors, the first value, the limits), `SetJointValue` (the
   joint, the value) and `RemoveJoint` (the joint). Each command sets the placement of each body of the subtree of the child in
   the same step, so `Undo` and `Redo` move the joint and the bodies as one step, and a replay gives each placement bit for
   bit. The log holds the joint commands; the placements are their result.
6. **The tree.** A body has at most one parent joint. A joint whose child already has a parent, or whose child is an ancestor of
   its parent, is refused with a new diagnostic code. A `PlaceBody` of a body with a tight parent joint is refused; a
   `PlaceBody` of a root body moves its whole tree.
7. **The limits.** A pivot has a lower and an upper limit, each a cosine and a sine; a slider has a lower and an upper length.
   A value outside the limits is refused. The limits are what K12 checks.
8. **Tight and loose.** A tight joint gives the placement of its child. A loose joint keeps the record, the connectors and the
   intended pair, and gives no placement, so the person can move the child away to examine it, and a later command makes the
   joint tight again at its value.
9. **The intended pair.** Each joint declares its two bodies as an intended pair: `CheckInterference` takes them with the pairs
   of its parameter.
10. **A placement by two connectors one time.** `PlaceByConnectors` (the two bodies and the two connectors) gives the child
    the placement of a fixed joint and records no joint: it is a `PlaceBody` whose seven numbers the engine computes.

## 4 What the model costs

- `Engine.Contracts` gets a record of a joint, a list of the joints in `Document`, new events (`joint.created`,
  `joint.changed`, `joint.removed`) and a field of the snapshot. Each is a new part of the public shape, so the ADR comes
  with it.
- Each command of a joint changes the placements of a subtree. A body with many children changes many records in one step;
  the events give each change.
- A connector on a face needs the face to stay: a later cut can remove it. The joint then gets a fault, as a datum on a face
  that a cut removes (ADR-0059 item 3), and gives no placement until a command repairs it.
- A version of the app before P11a refuses a file that holds a joint command, as for each new command.

## 5 The questions for the owner

1. **Q1 (the connector).** **A (recommended):** a face as `GetConnectors` names it, or a named datum, read in the frame of the
   body so it follows the body. **B:** only a named datum, in the frame of the Document as today, so a joint needs a new datum
   after each move.
2. **Q2 (the meeting of the connectors).** **A (recommended):** the two normals are opposite, as two faces that meet. **B:** the
   two normals are the same, and a client turns one connector itself.
3. **Q3 (the value of a pivot).** **A (recommended):** a cosine and a sine, and the engine computes the turn. **B:** a quaternion
   of the turn, as `Rotate` takes it.
4. **Q4 (the moves of the tree).** **A (recommended):** each command of a joint sets the placements of the subtree in the same
   step; a `PlaceBody` of a child with a tight parent joint is refused, and a `PlaceBody` of a root moves its tree. **B:** a
   joint stores only its value, and each reader computes the placement of a child from the tree at each read.
5. **Q5 (tight and loose).** **A (recommended):** a loose joint keeps its record and its intended pair and gives no placement.
   **B:** no state; a person removes the joint and adds it again.
6. **Q6 (the snap by connectors).** **A (recommended):** a command `PlaceByConnectors` that records no joint. **B:** a query that
   gives the seven numbers, and the client sends `PlaceBody`.
7. **Q7 (the order of the work).** **A (recommended):** P11a is the record, the three kinds, the values, the limits, the tree and
   the refusals, with an ADR and a test of each rule; P11b is the loose state, the intended pairs in `CheckInterference` and
   `PlaceByConnectors`. **B:** one row for all of P11.

## 6 Out of this document

- The travel of a pivot (K12), the envelope (K13) and the tool of the window (R23, R27).
- Groups of bodies (P18) and requirements (P13).
