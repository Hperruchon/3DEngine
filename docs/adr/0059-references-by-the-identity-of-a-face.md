---
id: 0059
title: The references by the identity of a face, and a datum on a face that a cut removed
status: Amended
topic: Kernel, feature graph, sketch
date: 2026-11-05
supersedes: []
superseded-by: []
amends: ['0051', '0053', '0055', '0058']
amended-by: ['0112']
affects:
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/FaceGone.cs
  - Engine.Core/Sketch/FaceFinder.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Core/Commands/SubtractCommandHandler.cs
  - Engine.Core/Commands/CreateDatumCommandHandler.cs
enforced-by: Engine.Tests/Sketch/FaceGoneTests.cs (TASK-0185 creates it)
---

# ADR-0059 — The references by the identity of a face, and a datum on a face that a cut removed

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

A datum names a face by its provenance: the feature, the element and the item (ADR-0051 item 1). Its plane comes from
the parameters of the feature, never from the solid. So a cut can remove the face, and the datum stays where the face
was with no warning (ADR-0051 item 8). The desktop cannot start a sketch on the wall of a hole (ADR-0053 item 2). Since
K4b (ADR-0058) the engine has the faces of each body, with their names, from the owned kernel. K4c is the last slice
of K4. On 2026-11-05 the owner answered five questions:

- **A gone face.** The datum gets the existing fault `W-DATUM-REFERENCE-FAILED` with the reason that the face is gone.
  It keeps its plane, and the edit applies.
- **The cost.** The check runs at the commit, only for a datum whose feature leads into a `Subtract`, and only when that
  chain changes. Only a `Subtract` removes a whole face.
- **A hole wall.** A datum can be on the wall of a hole: the face of the tool, with its normal turned into the hole. No
  field of a contract changes.
- **A split face.** The two parts of a face have one plane, so a datum keeps its plane and its frame.
- **Older documents.** The turn applies to each replay, also of a document made before this record. Before this
  record a client could create a datum on a face that a cut had removed, so a replay of such an entry gives the datum
  the fault of item 3 and does not refuse it (finding E134, TASK-0190).

## Decision

1. **The state of an edit.** The projection of ADR-0058 item 2 also reads the state of an edit before its commit: the
   sketch and the distance of each extrude, and the frame of each datum, after the edit.
2. **The kept solids.** The projection keeps the solid of the last 64 committed geometry keys. A key comes from the
   command and the body (ADR-0037 item 3), so a committed key names one solid in one Document. The projection keeps it
   under the identity of the Document and the key (E135, TASK-0192). A body with a fault gives no solid.
3. **A gone face.** The chain of a feature is the walk from its body through each consumer, a `Translate`, a `Rotate`
   (ADR-0112 item 6) or a `Subtract`, to the live body at its end. A datum with no other fault, whose chain
   holds a `Subtract`, and whose name is not a face of the kernel solid of that end, gets the fault
   `W-DATUM-REFERENCE-FAILED`. The message says that the
   face is gone. The datum keeps the frame of its reference (ADR-0051 item 8), so each sketch on it stays. The warning
   comes when the fault is new or changes. The check runs at three places:
   - `Subtract` checks each datum whose chain ends at one of its operands, against the names of the kernel subtract of
     the two kept solids.
   - The regeneration (ADR-0055) checks each datum in effect after its two walks. An end that the edit changes takes
     its names from the state after the edit; an end that it does not change takes the kept solid of its key.
   - `CreateDatum` refuses a gone face with `E-DATUM-REFERENCE-INVALID` (ADR-0051 item 7). In the replay of an entry of
     a log it gives the datum the fault and the warning instead, so a document of an earlier version opens.
   When the kernel cannot build an end, a datum keeps the face-gone fault of that end from the state before (E139,
   TASK-0192), and another datum has no check. An undo restores the record before (ADR-0031), so it
   removes the fault of the step.
4. **A face of a subtrahend.** The face of a subtrahend is a wall of the result, and its outward normal points into the
   hole. This amends ADR-0051 items 2 and 8. Each `Subtract` before the place of the reference that takes the body of
   the chain as its subtrahend turns the frame: (u, v, n) becomes (u, −v, −n), which is right-handed again. Two such
   steps turn it back. A datum made before the cut does not turn, because a step after its place does not move it. The
   turn applies to each replay, also of an older document; no document in the repository holds such a datum.
5. **The face under a hit.** `GetFaceAt` takes the face from the kernel solid of the body. This amends ADR-0053 items 2
   and 3. The first convex piece whose normal is the normal of the hit (a dot product of at least 0.999), whose plane is
   within t of the point, and whose edges hold the point within t gives the name, with t of ADR-0053 item 3. A name
   whose pieces have more than one plane is curved, and it gives `E-DATUM-FACE-NOT-FOUND`. The frame comes from
   `DatumResolver.Resolve` at the end of the log, so it includes each `Translate` and each turn. A wall of a hole is a
   face now, and a part of a face that a cut removed is not.
6. **A split face.** A name with two parts has one plane, so a datum on it keeps its plane and its frame. A later need,
   such as an edge or a vertex, can choose a part.
7. **Determinism.** The check and the turn use the exact kernel and the order of the log. The fault and the frame are a
   projection of the log, so a replay gives the same records on each platform.
8. **Out of this record.** An edge or a vertex of a sketch reference (ADR-0052) that a cut removes. A choice of one part
   of a split face. A datum on a curved face.

## Consequences

- A cut that removes the face of a datum says so, and a user can see which datum lost its face.
- A tap on the wall of a hole starts a sketch that extrudes into the hole.
- A datum that the API made on a face of a cut tool after the cut turns in a replay of an older document.
- Each `Subtract` and each edit of a chain with a `Subtract` runs the kernel. The subtract of the kernel takes about
  0.2 s for a cylinder of 64 facets (ADR-0058, "Consequences").

## History

- 2026-11-05 · TASK-0185 · Accepted with the five answers of the owner of 2026-11-05.
- 2026-11-05 · TASK-0190 · Finding E134: a replay of an older `CreateDatum` on a gone face gives the fault, not
  a refusal (Q1 A of the codebase review of 2026-11-05 (b), answered by the standing order of 2026-11-05).
- 2026-11-05 · TASK-0192 · Findings E135 and E139 of the codebase review of 2026-11-05 (b): the kept solids are keyed
  by the Document too, and a face-gone fault stays while the kernel cannot build the end. Q4 A, answered by the standing
  order of 2026-11-05.
- 2026-11-10 · TASK-0335 · ADR-0112 item 6 amends item 3: a `Rotate` is a step of the chain of a feature.
