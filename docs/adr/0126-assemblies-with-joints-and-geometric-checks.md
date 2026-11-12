---
id: 0126
title: Assemblies with joints and their geometric checks have a date after P10c, and a load check follows them
status: Accepted
topic: Charter, assembly
date: 2026-11-12
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - docs/CHARTER.md
enforced-by: Engine.Tests/Governance/ObjectiveReferenceGateTests.cs
---

# ADR-0126 — Assemblies with joints and their geometric checks have a date after P10c, and a load check follows them

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The charter listed "Assemblies" and "Simulation and analysis" as non-goals. On 2026-11-12 the owner asked for a work package
"design and validate an assembly with the AI", after the current queue and the 2D drawing track up to P10c, with the tilting
support of a light as the reference case. On that project a view at 45 degrees showed neither the collisions during the turn
nor the absence of a real locking link, so several variants were made again.

The proposal `docs/proposals/2026-11-12-assembly-with-ai.md` gives what exists at `main` bf864f0, the phases, their order and
their acceptance. Two facts of that study shape this record:

- No command gives a revolve, a union, a fillet, a hole or a thread, so the reference case may need features that the engine
  does not have.
- A mesh body from an STL file takes no edit (ADR-0083 item 4), so the parametric loop works on native parts only.

On 2026-11-12 the owner answered seven questions, D1 to D7.

## Decision

1. **The charter (D1).** Objective 11 changes: assemblies of the bodies of one part with joints, and the geometric checks of
   their motion (interference, gap, travel and envelope), have a place in the roadmap after P10c. Objective 16 changes: a
   load check of a printed part follows the assembly work. The list of the non-goals no longer names assemblies, and the
   non-goal "Simulation and analysis" excepts the geometric checks and the load check of this record.
2. **One part Document (D2).** The parts of a mechanism are bodies of one part Document, and a joint connects two bodies. An
   assembly of several part files (ADR-0071) stays later work.
3. **A move is a `Rotate` (D3).** A joint moves a body with `Rotate` commands (ADR-0112), one at the end of a drag or of an
   apply, and never one for each frame. The ADR of P11 studies the effect on the log first: the identity of a part through
   the chain, the turn about an axis that is not through the origin (ADR-0112 item 9), the length of the log, and the rule
   that the check of a travel is a query that writes nothing.
4. **The guarantee of a travel (D4).** The check samples the angle with a margin: at each sample the gap between two bodies
   must be more than the largest movement of a point of the bodies that turn before the next sample, so no contact between
   two samples is missed. The first contact angle is found to a stated tolerance, and the report states the step, the
   radius and the tolerance.
5. **Requirements in the Document (D5).** A requirement or an assumption is an entry of the Document: text, value, unit, its
   references, and a status: measured, estimated, to verify physically, or verified physically with a date and a result.
6. **The role of a body (D6).** An attribute of a body in the Document says that it is a part to print, a context volume, or
   excluded from an envelope.
7. **The print export (D7).** One STL file for each chosen part, in millimetres, after a check that its mesh is closed and has
   no degenerate triangle. A 3MF export comes later.
8. **The order.** The roadmap holds K10 (the reference case and its missing features), P11 (joints), P13 (requirements),
   R23 (a joint in the window), K11 (interference), K12 (the travel), P14 (roles and print export), K13 (the envelope),
   P12 (the parametric loop), R24 (the end-to-end scenario) and K14 (the load check), after P10c. Each phase opens its task,
   and its ADR when it changes the Document or the contract, at its start.
9. **No code in this record.** The record changes the charter, the roadmap and the index of the ADRs only.

## Consequences

**Good:** the reference case becomes a test that a computed check passes, not a view that a person reads. The AI uses the
same commands and queries as the window.

**Bad:** the work is long, about 16 to 24 evenings before the load check, and it starts after P10c. A `Rotate` for each move
adds a body to the log for each move. The reference case may need features that come first.

## History

- 2026-11-12 · TASK-0353 · The record, with the answers D1 to D7 of the owner of 2026-11-12.
- 2026-11-12 · TASK-0353 · Shipped at v0.338.
