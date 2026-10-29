---
id: 0035
title: The constraint solver of a sketch
status: Accepted
topic: Sketch, determinism
date: 2026-10-29
supersedes: []
superseded-by: []
amends: ['0033']
amended-by: []
affects:
  - Engine.Core/Sketch/**
  - Engine.Core/Queries/GetSketchQuery.cs
  - Engine.Core/Queries/GetSketchQueryHandler.cs
enforced-by: Engine.Tests/Sketch/SketchSolverTests.cs (TASK-0102 creates it)
---

# ADR-0035 — The constraint solver of a sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K3a gives the engine a solver for the five constraints of K2a: coincident, horizontal, vertical,
distance and fixed. It uses only `+ - * / sqrt`, and its output stays outside the log (KD5 of the decision sheet of
2026-10-27). Until K3a a constraint shows its residual, and no point moves (ADR-0033 item 7, ADR-0034).

Five facts constrain the design:

- **ADR-0033 item 4 (Q6 of the second review of 2026-10-28).** The positions of the points are a projection of the
  record, and from K3a the solver writes them. The log holds the inputs only. A step of the history holds the record
  before and after, so an undo gives back the solved positions with no new solve.
- **Determinism (anti-objective 16, `CLAUDE.md`, rules 1, 3 and 4).** A replay must give the same bits on each
  platform. A sketch command is a command of the log, so the solver must use `+ - * / sqrt` and comparisons only,
  with one fixed order of the operations. It must not use a parallel loop, a vector type of the processor, or a
  library whose order of the operations can change.
- **Saved files.** Files of v0.100 to v0.102 can hold constraints that the positions do not satisfy. An open applies
  each entry again (ADR-0015). A command that the engine accepted at the save must not get a rejection at the open.
- **ADR-0006.** The engine rejects a command as one unit, with a code. A command that gives a state that the person
  cannot see is not permitted (anti-objective 4).
- **The size.** A sketch of the milestone holds tens of points. A dense linear solve of a few hundred equations takes
  less than a millisecond. No sparse method is necessary now.

Research. **[Not verified in this session]** The solver of SolveSpace (`src/system.cpp`) uses the same method as
option Q1 A: a Newton iteration whose step is the step of minimum norm, from a linear solve of J·Jᵀ. FreeCAD and
Onshape keep a constraint that conflicts with the others, and show it in red; they do not refuse it.

On 2026-10-29 the owner answered the questions Q1 to Q4 below, A to each.

### Questions for the owner

**Q1. The method of the solver.**
- A: A Newton iteration on the equations of the constraints. Each step is the step of minimum norm: the solver solves
  (J·Jᵀ)·λ = r by Gaussian elimination with partial pivoting, and moves the positions by −Jᵀ·λ. J holds the
  derivatives, which the code computes by formula and not by a difference. A free point moves as little as possible.
  A row with a pivot near zero is a redundant equation: the solver drops it.
- B: A least-squares method with damping (Levenberg–Marquardt). It also gives a result for a set that conflicts, but
  it needs a damping factor and more iterations, and a person sees a result that satisfies no constraint exactly.
- C: A relaxation that satisfies one constraint at a time. It is simple, but it is slow and it can oscillate.

**Recommended: A.** It converges in a few steps near a solution, it moves only what it must, and the elimination uses
`+ - * /` and comparisons only. The drop of a redundant row also gives the rank, so the count of free degrees in Q4
costs nothing more.

**Q2. A set of constraints with no solution.**
- A: The engine applies the command. When the solver does not converge, the record keeps the positions that the
  command gave, with no solve. `GetSketch` says that the sketch is not solved, and the residual of each constraint
  shows which ones fail. The person deletes a constraint or changes a value.
- B: The engine refuses the command with a new code, `E-SKETCH-NOT-SOLVED`, and the record does not change.

**Recommended: A.** A file of v0.100 to v0.102 that holds such a constraint still opens. FreeCAD and Onshape keep the
constraint too. With B an open can fail on a file that the engine accepted at the save, and a command that is valid
now can fail later when a different solve order is necessary.

**Q3. The point that a move gives.**
- A: For `MoveSketchPoint` the solver first holds the moved point at its new position, as a temporary fixed
  constraint. When that solve fails, it solves again with no hold. So the point goes where the person put it, and the
  rest of the sketch follows. Each other command holds no point.
- B: No point is held. The moved point and the other points move by the minimum norm together.

**Recommended: A.** A person who moves a point expects the point to stay there. With B a move of a point on a line of
fixed length moves the point only half of the way. A point with a `fixed` constraint still goes back, because the
first solve fails and the second one has no hold.

**Q4. What `GetSketch` reports.**
- A: Two new fields of the result: `solved` (true when each residual is less than the tolerance) and `freedom` (the
  number of free degrees: two for each point other than the origin, less the rank of J). The window "Sketch" of the
  desktop shows one line, for example "Solved · 3 free" or "Not solved".
- B: The residuals only, as today.

**Recommended: A.** A person needs to know when a profile is fully constrained, and an agent needs the same fact
without a computation of its own. The fields are additive, and the records of `GetSketch` live in `Engine.Core`, so
`Engine.Contracts` does not change.

## Decision

The decision below applies the recommended answers: Q1 A, Q2 A, Q3 A and Q4 A.

1. **The place.** The solver lives in `Engine.Core/Sketch/SketchSolver.cs`. The determinism gate reads
   `Engine.Core`, so it reads the solver. The solver is a pure function: a record and an optional held point in, a
   record and a status out.
2. **When it runs.** Each sketch command that gives a changed record (ADR-0033 item 8) gives it through the solver,
   in `SketchResults.Changed`. The record that the bus commits holds the solved positions. A replay runs the same
   solver on the same inputs, so it gives the same record. The log holds no solved position (KD5).
3. **The variables.** The variables are the x and the y of each point other than the origin, in the order of the
   record. The origin is a constant (ADR-0033 item 2).
4. **The equations (Q1).** The constraints give the equations in the order of the record:

   | Kind | Equations |
   |---|---|
   | `coincident` p, q | pₓ − qₓ = 0 and p_y − q_y = 0 |
   | `horizontal` a→b | b_y − a_y = 0 |
   | `vertical` a→b | bₓ − aₓ = 0 |
   | `distance` p, q | \|p − q\| − L = 0 |
   | `distance` p, line a→b | cross(b − a, p − a) / \|b − a\| − s·L = 0, with s the side of p at the start of the solve (+1 when p is on the line) |
   | `distance` line a→b | \|b − a\| − L = 0 |
   | `fixed` p | pₓ − x = 0 and p_y − y = 0 |

   Where a length is zero, the derivative takes the direction (1, 0), so the step is defined and the same on each
   platform. The residual of `GetSketch` stays as ADR-0033 item 3 gives it.
5. **The iteration.** Each step computes the residuals r and the matrix J of the derivatives, solves
   (J·Jᵀ)·λ = r by Gaussian elimination with partial pivoting in a fixed order, and subtracts Jᵀ·λ from the
   variables. A pivot whose absolute value is at most 1e-12 times the largest diagonal entry is zero, and the solver
   drops that row. The solve converges when each residual has an absolute value less than 1e-9 in the length unit of
   the Document. It stops after 50 steps, or when a value is not finite.
6. **No solution (Q2).** When the solve does not converge, the record holds the positions that the command gave. The
   command is applied. No new diagnostic code exists.
7. **The held point (Q3).** `MoveSketchPoint` gives its point to the solver as held. The solver first solves with
   two more equations, pₓ − x = 0 and p_y − y = 0 for that point. When that solve does not converge, it solves again
   with no hold.
8. **The query (Q4).** `GetSketch` gives `solved` and `freedom`. `freedom` is 2·(number of points − 1) less the rank
   of J at the positions of the record. The window "Sketch" of the desktop shows the two values in one line.
9. **The proof of determinism.** A test solves fixed sketches and compares the bits of each solved position with
   constants. The pipeline runs it on Windows, Linux and macOS, on x64 and arm64. A difference of one bit fails it.
10. **Saved files.** A file of v0.100 to v0.102 opens with no change of the log. The open solves each sketch, so its
    points can move to positions that satisfy the constraints. The schema version of each sketch command stays 1,
    because the meaning of each field stays the same: a position of a command is an input of the solver.

This record amends ADR-0033 items 4 and 7: the solver writes the positions in each changed record, and `GetSketch`
gives `solved` and `freedom`.

## Consequences

**Good:** A constraint moves the points, so a person makes an exact rectangle from four rough taps and four
constraints. A replay gives the same record on each platform, and a test holds it. An undo needs no solve. A file of
an earlier version still opens. The person and an agent see if a sketch is fully constrained.

**Bad:**
- A set that conflicts stays in the record, not solved, until the person changes it. The residuals show where, but
  the engine does not name the constraint that causes the conflict.
- A dense solve costs the cube of the number of equations. A sketch of some thousand constraints is slow. A sparse
  method waits for a measured need.
- A Newton iteration can find a solution that the person did not expect, for example a mirrored triangle. The
  minimum-norm step makes it rare, because the points move as little as possible.
- An open of an earlier file can show points at other positions than before the save.

**Next:** TASK-0102 applies this record. K0a extrudes a closed profile of the solved sketch. Phase K3 gives the
equations of the kinds 6 to 14.

## History

- 2026-10-29 · TASK-0102 · Proposed, with the questions Q1 to Q4 for the owner.
- 2026-10-29 · TASK-0102 · The owner accepted the record on 2026-10-29 with Q1 to Q4 A, and no change.
