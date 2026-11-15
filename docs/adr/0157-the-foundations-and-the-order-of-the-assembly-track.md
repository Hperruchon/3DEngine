---
id: 0157
title: The assembly track has shared foundations, the checks and the preview of a proposal come before the joints, and the model of a move waits for a measure
status: Accepted
topic: Assembly, agent
date: 2026-11-15
supersedes: []
superseded-by: []
amends: ['0126']
amended-by: []
affects:
  - docs/proposals/2026-11-15-assembly-and-agent-design.md
enforced-by: Engine.Tests/Governance/TaskGovernanceGateTests.cs
notes: Governance only. Each phase opens its own ADR when it changes the Document or the contract.
---

# ADR-0157 — The assembly track has shared foundations, the checks and the preview of a proposal come before the joints, and the model of a move waits for a measure

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0126 put assemblies with joints and their geometric checks on the roadmap after P10c, with the tilting support of a light
as the reference case, and gave an order: K10, P11 and P13, R23, K11, K12, P14, K13, P12, R24, then K14.

On 2026-11-15 the owner asked for a design file on the design of assemblies and the work with an agent, for four families of
assemblies: a modular furniture, a fixed mount with screws, a moving mechanism and a multi-level assembly.
`docs/proposals/2026-11-15-assembly-and-agent-design.md` is that file. `docs/proposals/2026-11-15-plasticity-features.md`
compares the product page of Plasticity with the engine. Five facts of the two files shape this record:

- Each of the sixteen needs of the design file occurs in three families or more. So the work is a small set of shared
  foundations, and not one feature for each family.
- No query gives a distance between two bodies, an overlap, a volume or a mass. The dimension overlay of the window reads the
  meshes of the desktop (ADR-0056 item 1), so an agent cannot ask for the same number.
- The accept of a proposal applies its commands one after the other and stops at the first failure, and "the commands before
  it stay applied" (ADR-0092 item 6). The window shows no result before the accept. The owner answered "B until S9 is done,
  then A" to the preview of a proposal (S11) on 2026-11-10, and S9 shipped as R18i.
- A pivot has a gap of zero between its two parts. The check of a travel of ADR-0126 item 4 needs a gap at each sample, so
  it cannot pass at the pivot.
- A `Rotate` makes a new body, and the owned kernel reads the nine doubles of a turn exactly (ADR-0112 item 5). The design
  file deduces that many turns of one body make its numbers longer and change its shape by a very small amount. No test
  measured this.

On 2026-11-15 the owner answered each decision of the design file in the chat: option A for A1 and A3 to A15, and option C
for A2. The owner accepted the order of the features of the Plasticity file.

## Decision

1. **The scope and its stages (A1).** The assembly track has three stages, each after the one before it: the bodies of one
   part Document with groups (ADR-0126 item 2); then instances of a body inside one Document; then the resolution of the part
   references of an assembly Document (ADR-0071 item 7, row P8b). Three rules hold from the first stage, so that a later
   stage rewrites no log:
   - a record that names a part names it by the identity of its first feature, resolved through the chain of consumers, as
     a datum does (ADR-0059 item 3);
   - a reference to another file is a source at a fixed version, and only a command changes it (ADR-0071 item 3);
   - a group is rigid by default.
2. **The model of a move waits for a measure (A2, option C).** ADR-0126 item 3 stays in force: a move is a `Rotate`. K10
   measures 150 turns of the arm of the reference case: the time of a rebuild and of a replay, the size of the numbers of
   the body, and the change of one length. The ADR of P11.0 then decides between `Rotate` commands and a placement as data
   with the value of a joint in the log. Register entry R-0070 holds the question.
3. **Constraints (A3).** A part has connectors, and a joint connects two connectors. The joints make a kinematic tree with
   one parent for each part. The engine computes each place from the tree with `+ - * /` only. No solver is in the
   engine. A joint that closes a loop is refused with a diagnostic code. The first kinds are fixed, revolute and slider.
4. **A connector (A4).** A connector is a named datum of a body, or an implicit frame that a query gives from the faces of
   the body: the centre and the normal of a plane face, and the axis of a cylinder face.
5. **Intended contacts (A5).** The Document holds each pair of parts whose contact or overlap is intended. A joint declares
   its two parts as such a pair. A check does not report a declared pair as a fault, and the check of a travel exempts it.
   This amends ADR-0126 item 4.
6. **A mesh body in a check (A6).** A check accepts a mesh body. Its result then says that it is exact for the mesh only.
7. **The class of a result (A7).** Each result of a check states one class: guaranteed from the nominal geometry; depends on
   data that the Document holds; or a physical test only. The rule of a gap is a requirement (P13) with a value of the
   process, and its result has the class "depends on data".
8. **The preview of a proposal (A8).** The host runs the commands of a proposal on a second Document, a fork of the first at
   a stated version, and changes neither the first Document nor its log. The dry run gives the outcome of each command and
   four differences: the bodies, the state of the solver of each changed sketch, the measures, and the checks that change
   state. The ADR of P15 gives the design of the fork.
9. **A Document that changed before the accept (A9).** The accept runs the dry run again on the Document as it is. When
   the result equals the result that the person saw, the accept applies. When it differs, the window shows the new preview
   and the person answers again.
10. **The accept (A10).** The accept applies each command or none. One proposal is one step of undo. The ADR of P16 amends
    ADR-0092 items 4 and 6 and ADR-0130 item 4, and gives the group of steps in the history (ADR-0031).
11. **The order (A11).** This amends ADR-0126 item 8. After P10c:
    - K10;
    - K15, K16, R26, then K11 and P17;
    - P15, P16 and R25;
    - K17, P11 (P11.0 first), P18, R23 and R27, with P13 at the same time as P11;
    - K12, P14, K13, P12, then R24;
    - P20, K21, P19, K18, then R28;
    - K19, K20, K22, R29 and P8b, each on its own decision; K14 after R24, as before.
12. **The validation cases and the criteria (A12).** The tilting support of a light is the case of the fixed mount and of
    the mechanism. The bookcase `Bibliotheque120` of the owner is the case of the furniture, and of the multi-level assembly
    as modules. The owner gives the sizes. Each end-to-end row (R24, R28) uses the criteria C1 to C11 of section 13 of the
    design file.
13. **The agent (A13, A15).** The first picture for an agent is the SVG of a view (P10a), which each host can give. A
    rendered picture on the desktop host can follow. A guide for an agent comes first (P17); a server of the Model Context
    Protocol (MCP) that wraps the HTTP surface can follow.
14. **The advanced checks (A14).** After the base: an order of assembly along the six axis directions and the screw axes
    (K19), and the access of a tool as a cylinder on the screw axis (K20). No general path planner.
15. **The features from the Plasticity file.** Union and intersection (K16); the section view (R26); the drag of a face as a
    change of the value that owns the face (R29); linear and grid patterns (K21); the flat chamfer of one edge (K22); the
    snap of a body to a body (R27). No fillet (anti-objective 11). A STEP import has no row; a 3MF export stays in row P14.
16. **No code in this record.** The record changes ADR-0126, the roadmap, the register and the index of the ADRs. Each phase
    opens its task at its start, and its ADR when it changes the Document or the contract.

## Consequences

**Good:** an agent gets numbers and checks before it gets joints, so the first product rows of the track serve each
family. One fork serves the preview, the accept, the travel and the order search. A person accepts what the preview showed.

**Bad:** the track is longer: 19 new rows, and the joints come later than in ADR-0126. The model of a move stays open until
K10 measures it, so the estimate of P11 can change. The estimates of the new rows come from a read of the code and of the
documentation of other tools, not from a measure.

**A limit:** the field `affects` names the design file only. `AffectsCitationGateTests` refuses a code file that cites this
record and is not in that field. So the code of a later phase cites the ADR of its phase, or its task adds the path to
`affects` with a line of history.

**Next:** register entry R-0070 (the model of a move and the sizes of the two cases; the owner chose one entry on
2026-11-15, because the register held 14 of its 15 entries). Row K10 opens the first task after P10c.

## History

- 2026-11-15 · TASK-0394 · The record, from the answers of the owner of 2026-11-15 to A1 to A15 of the design file.
