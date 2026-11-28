---
id: 0182
title: The pair check CheckInterference at one position, from the exact solids of the owned kernel
status: Amended
topic: Kernel, queries, assembly
date: 2026-11-21
supersedes: []
superseded-by: []
amends: ['0083']
amended-by: ['0188', '0192']
affects:
  - Engine.Core/Queries/CheckInterferenceQuery.cs
  - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
  - Engine.Tests/Queries/CheckInterferenceQueryTests.cs
  - Engine.Tests/Http/HttpInterferenceScenarioTests.cs
enforced-by: Engine.Tests/Queries/CheckInterferenceQueryTests.cs
notes: K11. ADR-0157 items 5 to 7 give the decisions of the owner; this record gives the shape of the query.
---

# ADR-0182 — The pair check CheckInterference at one position, from the exact solids of the owned kernel

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K11 of the roadmap asks for the pair check at one position: for each pair of bodies, a query gives the class (apart,
touching or overlapping), the volume and the box of the overlap, the minimum gap, a mark for an intended contact, and a mark
when the answer is exact for a mesh only. The intended pairs are a parameter of the query until P11 and P13 put them in the
Document. The owner decided three rules on 2026-11-15 (ADR-0157):

- item 5 (A5): a check does not report a declared pair as a fault;
- item 6 (A6): a check accepts a mesh body, and its result then says that it is exact for the mesh only;
- item 7 (A7): each result of a check states one class: guaranteed from the nominal geometry, depends on data, or a physical
  test only.

Card N3 of `docs/proposals/2026-11-15-assembly-and-agent-design.md` names the query `CheckInterference(bodyIds?)`, with the
fields `class`, `volume`, the box, `gap`, `intended` and `exact`, and gives the cost to watch: 19 bodies give 171 pairs, and a
first test of the boxes removes most of them.

K15a and K15b (ADR-0180) give the exact solid of a body (`MeasureTarget.SolidOf`) and the exact nearest pair of the pieces of
two solids (`NearestPoints`). K16 (ADR-0181) gives the exact intersection of the owned kernel (`PlanarBoolean.Intersect`), and
its item 3 says why the exact kernel decides an empty intersection: Manifold gives a flat solid of no volume for two bodies
that touch on a face. The command `Intersect` consumes its two bodies and writes to the log, so a check cannot use it.

Two constraints remove the obvious alternatives. A query writes nothing to the log (ADR-0016), so the check computes each
intersection in memory, in the kernel. A parameter of a query is flat, and the binder refuses the type "array" (ADR-0013 §2,
ADR-0016): a list of identifiers as an array needs an item type in `FieldSchema` of `Engine.Contracts`, which is a change of
the contract for a parameter that P11 and P13 replace.

## Decision

1. **One query.** `CheckInterference`, schema version 1, in `Engine.Core/Queries`, registered in `HandlerCatalog`. The route
   `/queries` of HTTP, the command `engine query` and `DocumentSession.Query` reach it through the registry, with no new route.
   The query reads the solids of the owned kernel and computes each overlap with `PlanarBoolean.Intersect` in memory: it
   writes nothing to the log, makes no body and changes no version. No record of `Engine.Contracts` changes: the result uses
   `Aabb` and `DatumPoint`, as `GetBoundingBox` and `GetDistance` do. In `Engine.Kernel` only `PlanarBoolean.Intersect` gets
   a form with a cancellation token (item 8, TASK-0438).
2. **The parameters.** Two optional strings:
   - `bodyIds`: the identifiers of the bodies to check, separated by commas, two or more, each once. With no value, each live
     body of the Document is checked, in the order of the Document.
   - `intendedPairs`: the pairs whose contact or overlap is intended, separated by commas, each as two identifiers separated
     by a colon (`a:b`), in either order, each pair once, each body a body of the check. With no value, no pair is intended.

   White space round an item is permitted. The result is `pairs`: one entry for each pair of the bodies, A before B in the
   order of the bodies, so a replay of the same log gives the same order.
3. **The class.** Each class is decided on exact rationals:
   - `Overlapping`: the exact intersection of the two solids has a volume above zero;
   - `Touching`: the intersection is empty and the exact minimum distance between the pieces of the two solids is zero, on a
     face, a part of a face, an edge or a point;
   - `Apart`: the intersection is empty and the minimum distance is above zero.

   The intersection runs only when the exact boxes of the two bodies share a length above zero on each axis; else the bodies
   cannot share a volume. With no shared volume, one body cannot lie inside the other, so the gap needs no test of inside.
4. **The values.** For `Overlapping`: `volume`, the exact volume of the intersection in mm³, and `box`, its exact box, each as
   the nearest double; `gap` is 0 and `pointA` and `pointB` are null (the box says where). For `Touching` and `Apart`: `volume`
   is 0 and `box` is null; `gap` is the root of the exact square of the minimum distance, and `pointA` and `pointB` are the
   exact nearest pair of ADR-0180 item 4, the first in the order of the pieces. `meshOnly` is true when a body is a mesh body of
   an STL file, or has a curved face (a face name on more than one plane, ADR-0180 item 6): the class and each value are then
   exact for the triangles or the facets, and not for the curve of the design (ADR-0157 item 6). The closed forms of the curve
   of K15b are not used, because they refuse many pairs of curved faces, and a check must answer each pair.
5. **The intended pairs (ADR-0157 item 5).** `intended` is true for a pair of `intendedPairs`. `fault` is true for a pair that
   overlaps and is not intended. A contact is not a fault: the modules of the furniture of ADR-0157 item 12 touch by design,
   and the tools of the design file keep "touching" as its own class. An intended pair that is apart is not a fault in K11;
   the joints of P11 give the rule for a pair that must touch.
6. **The basis (ADR-0157 item 7).** Each pair gives `basis`, with the value `Guaranteed`: the class, the volume, the box and
   the gap come from the geometry of the Document alone. The marks `intended` and `fault` repeat the data that the caller
   gives. A later check adds the value of its own class, as the rule of a gap of P13 adds "depends on data".
7. **The refusals.** One refusal refuses the whole query, as for the measure queries. A text of `bodyIds` or
   `intendedPairs` that is not valid gives `E-CHECK-PAIR-INVALID` (new): an item that is not an identifier, a body twice, one
   body only, an empty text, a pair that is not two identifiers separated by a colon, a pair of one body, a pair with a body
   that the check does not hold, or a pair twice. An identifier that is not a live body gives `E-GEOM-BODY-NOT-FOUND`. A body
   with a fault, or that the kernel cannot build, gives `E-GEOM-BODY-FAULTED` or `E-GEOM-FACES-FAILED`, as for the measure
   queries (ADR-0180 item 7). A mesh body whose triangles do not close a solid (each edge with one coedge in each direction,
   `Brep.Valid`, and a volume above zero) gives `E-MESH-UNSUPPORTED`; a backend with no `ITessellationOps` gives
   `E-GEOM-CAP-MISSING`, and a tessellation that fails `E-GEOM-NATIVE-OP`. A closed mesh with two outer shells that share a
   volume also gives `E-MESH-UNSUPPORTED` (TASK-0438, finding E234): the tree of the kernel takes a solid that does not cross
   itself, and with two shells that overlap a box inside the mesh was "apart". Two triangles with a common edge of two points
   are in one shell; a shell whose signed volume has the sign of the whole mesh is an outer shell, and the other shells are
   voids. Each pair of outer shells whose exact boxes share a volume runs the exact intersection of the kernel, before the
   first pair of bodies. This test is the smallest correct option of the review: a mark "not exact" needs the same test to
   find the mesh, and a new value of `basis` that ADR-0157 item 7 does not give. TASK-0449 (finding E236 of the third
   codebase review of 2026-11-21): an island in a void is an outer shell inside the box of the shell around the void, and the
   two share the volume of the island. So each void whose box shares a volume with the shared volume is subtracted from it,
   and only a volume of material in both shells refuses the body. A hollow cube with a cube in its void is checked.
8. **The cost.** For n bodies the check has n (n - 1) / 2 pairs. Each body gives its solid, its exact box and its pieces
   once. For each pair: one comparison of the boxes; the intersection only for boxes that share a volume, which partitions the
   f_A polygons of one solid by the f_B polygons of the other (O(f_A × f_B) splits in the worst case); else the nearest pair,
   at most f_A × f_B pairs of pieces, pruned by the boxes of the pieces (ADR-0180 item 4). The worst case is
   O(n² × f_A × f_B). The bookcase of K10a (19 boxes, 171 pairs) takes 0.2 s on the computer of the owner.

   **The bound of work (TASK-0438, finding E233, Q5 A of the second codebase review of 2026-11-21).** A query holds the
   serial lock of the session for its whole work. So, after it resolves the bodies and before its first pair, the check
   adds the product f_A × f_B of the counts of the pieces of each pair of bodies, and the product of the counts of the
   polygons of each pair of outer shells of item 7 whose boxes share a volume. Above 100,000 it is refused with
   `E-CHECK-WORK-LIMIT` (new), and the message gives the sum. The measures on the computer of the owner set the limit: the
   intersection of a sphere of 3,968 triangles and a plate (23,808 products) took 3.2 s, and the gap of two spheres apart
   (15,745,024 products) took 101 s. At the limit an intersection takes about 13 s and a gap about 0.6 s; the bookcase needs
   6,156. The cancellation token of the query is read before each body, before each pair, for each piece of the first side
   of the nearest pair, and at each node of each walk of the tree of the kernel that splits polygons
   (`PlanarBoolean.Intersect` with a token). The token changes no result. The review also proposed to sort the pieces by the
   gap of their boxes; that changes which pair of points comes first when several pairs give the minimum, so it is not done.

   **The order of the bound (TASK-0449, finding E237 of the third codebase review of 2026-11-21).** The products of the pairs
   come before the solid of a mesh body: a mesh body counts one piece for each triangle of the backend, which is the count of
   the polygons of its solid. Above the limit the check is refused before the exact volume, the B-Rep and the shells of each
   mesh body. After the solids, the products of the shells are added, with the product of the polygons of the two shells
   and of each void whose box shares a volume with both, and the limit applies again. So a refusal of a body by item 7 can
   come after the refusal of the work.

   **The bound of a preview (TASK-0463, ADR-0188 item 8).** The fork of a preview gives a check a smaller limit, the work that
   the bound of the preview leaves, and reads the work that the check counted. A client cannot give the limit, and the answer
   of the query does not change.
9. **Determinism.** Each step that decides a class uses `+`, `-`, `*`, `/` and comparisons on exact rationals. A double of
   the triangles of a mesh body enters exactly. The gap is one `Math.Sqrt` of the nearest double to an exact square; each other
   value is the nearest double to an exact value. No transcendental function is called.
10. **A mesh body in a check.** This amends ADR-0083 item 4: the pair check reads the triangles of a closed mesh body and gives
    its result with `meshOnly` true; each other operation of item 4 still refuses a mesh body.

## Alternatives considered

- **The command `Intersect` of K16.** Refused: it consumes the two bodies and writes to the log.
- **The intersection of Manifold.** Refused: it gives a flat solid of no volume for a contact on a part of a face (ADR-0181
  item 3), so it does not separate `Touching` from `Overlapping`.
- **Lists as parameters of the type "array".** Refused for K11: the binder refuses the type, and an item type is a change of
  `FieldSchema` in `Engine.Contracts` (ADR-0013 §2) for two parameters that P11 and P13 move into the Document.
- **A contact as a fault.** Refused: each module of the bookcase touches its neighbours by design, so the check of the
  furniture would give 48 faults with no meaning.
- **A pair with no answer when one body is refused.** Refused for K11: one answer for each pair, or a refusal that names
  the body, keeps the result simple. A client can give `bodyIds` without the refused body.

## Consequences

**Good:** the window, an agent and the preview of P15 get the contacts and the overlaps of a part by one query, with one
answer on each platform. The bookcase gives its 48 contacts and no overlap. The travel of K12 can run the same query on a
fork at each sample.

**Bad:** the exact intersection and the exact nearest pair have a cost: a body of many facets, as a sphere, gives many
polygons, and each pair of two such bodies whose boxes share a volume partitions both. The bound of item 8 refuses such a
check, also two spheres apart, and a part of many bodies with one curved body; a client then gives fewer bodies. Below the
bound a check can still hold the session for about 13 s. The test of the shells of item 7 does not test a void shell against
an outer shell, nor a shell that crosses itself, so such a mesh can still give a wrong class. Each void costs a subtract from
each shared volume of two outer shells. A curved body is checked on its facets,
so a gap below the size of the facets is not the gap of the curve, and the mark says so. One faulted body refuses the whole
check. The two lists are text, which a client must build; a typo gives a refusal and not a wrong answer.

**Next:** P11 and P13 put the intended pairs in the Document; K12 runs the check on a fork at each sample of a travel.

## History

- 2026-11-21 · TASK-0436 · The record: the query `CheckInterference`, the code `E-CHECK-PAIR-INVALID`, and the amendment of
  ADR-0083 item 4, from the answers A5 to A7 of the owner of 2026-11-15 (ADR-0157 items 5 to 7).
- 2026-11-21 · TASK-0438 · Items 1, 7 and 8 and "Bad": the bound of work with `E-CHECK-WORK-LIMIT` and the token in the
  inner loops (finding E233, Q5 A), and the refusal of a mesh with two outer shells that share a volume (finding E234).
- 2026-11-21 · TASK-0449 · Items 7 and 8 and "Bad": the voids leave the volume that two outer shells share, so an island in
  a void is checked (finding E236), and the products of the pairs come before the solid of a mesh body (finding E237).
- 2026-11-24 · TASK-0463 · ADR-0188 amends item 8: the fork of a preview gives the check the work that the bound of the preview
  leaves, and reads the work that the check counted.
- 2026-11-28 · TASK-0478 · ADR-0192 amends item 2: with no `bodyIds` the check takes each live body in the order of the log, not in the order of the Document.
