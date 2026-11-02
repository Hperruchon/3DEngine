---
id: 0050
title: The exact predicates of the profile of a sketch
status: Accepted
topic: Geometry, sketch, determinism
date: 2026-11-02
supersedes: []
superseded-by: []
amends: ['0036', '0040']
amended-by: []
affects:
  - Engine.Core/Exact/**
  - Engine.Core/Sketch/SketchProfile.cs
enforced-by: Engine.Tests/Exact/PredicateTests.cs (TASK-0164 creates it)
---

# ADR-0050 — The exact predicates of the profile of a sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase K1 of `docs/roadmap.md` is "Exact predicates. A deterministic arithmetic layer." K4 needs K0, K1 and K2. A
predicate is a test whose result is a sign: for example, a point is left of a line, right of it, or on it.

Up to v0.165 `SketchProfile` computes each sign with rounded doubles. The result is deterministic, because `+ - *`
give one result on each platform. But it is not always the true sign. A search with exact arithmetic on 2026-11-02
found these cases:

- Three points on one line exactly give a rounded value that is not zero. A loop that touches itself at such a point
  passes the check of a touch, and `Extrude` accepts it.
- A point that is near a line by less than one unit in the last place gives the rounded value zero, or the wrong
  sign.
- A sliver triangle gives the rounded area zero, and `Extrude` refuses it.
- When two products are subnormal, their rounding errors are larger than each relative bound, so a filter with no
  limit gives the wrong sign.

So the profile that the engine accepts is not always the profile of the true coordinates. A kernel that decides
topology from such signs (K4) cannot be correct.

On 2026-11-02 the owner answered two questions:

- **Q1 A.** `SketchProfile` uses the exact predicates for the live command, the open and the rebuild. A saved file
  that only the rounded rule accepted no longer opens, and the format version stays 1.
- **Q2 A.** K1 gives the 2D predicates that the profile uses. The 3D predicate arrives with K4, its first consumer,
  by the rule of `CLAUDE.md` against an abstraction with no requirement.

ADR-0035 (context, "Saved files") and ADR-0037 (context) say that a command that the engine accepted at the save must
not get a rejection at the open. Q1 A is an exception to that rule, for an `Extrude` whose profile only the rounded
checks accepted. The owner chose it with this cost named in the question. A rebuild stays a projection: a profile
that the exact checks refuse gives a fault on the body, and the edit applies (ADR-0037 item 6).

`docs/CHARTER.md` defers "exact booleans with exact arithmetic". An exact predicate is not an exact boolean: it gives
a sign, not a solid. Manifold still computes each solid.

## Decision

1. **The place.** `Engine.Core/Exact/Predicates.cs`, the namespace `Engine.Core.Exact`. It is not a new project:
   a later move to a kernel project is a change of namespace only.
2. **`Orient2D(a, b, c)`.** It gives the sign of (b − a) × (c − a). It first computes the rounded value, and its
   permanent |left| + |right|. When the permanent is at least 1e-270 and the absolute rounded value is more than
   4ε times the permanent, with ε = 2^-53, the rounded sign is the true sign. Shewchuk proves the bound (3 + 16ε)ε
   with no underflow. The excess, at least 0.99ε times the permanent, covers the error of an underflow, which is at
   most 2^-1074, when the permanent is at least 1e-270. Each other case, an overflow included, goes to item 3.
3. **The exact path.** Each finite double is an integer of at most 53 bits times a power of two. The predicate
   scales all its coordinates to the smallest exponent among them, as `BigInteger` values, and computes the
   determinant with integers. The result is exact for each finite coordinate. A coordinate that is not finite gives
   an `ArgumentException`, because no sign exists for it.
4. **`AreaSign(loop)`.** It gives the sign of twice the signed area of a loop, with integers each time, because a
   profile calls it once. `SketchProfile` uses `Orient2D` for the check of a cross and a touch, and `AreaSign` for
   the area and the turn. Before the checks, a vertex of the polygon that is not finite gives a reason under
   `E-SKETCH-PROFILE-INVALID`, because the polygon of an arc with very large values can overflow.
5. **Determinism.** Both paths use `+ - *`, comparisons and integers only. They use no transcendental function, no
   fused multiply-add and no estimate (`CLAUDE.md`, determinism rules 1, 3 and 4).
6. **Replay (Q1 A).** The live command, the open of a document file and the rebuild of an extrude use the same rule.
   A file whose `Extrude` only the rounded rule accepted gives `E-IO-LOAD-FAILED` at the open, as ADR-0015 item 8 says
   for an entry that does not apply. The format version stays 1. Such a file has a profile with a point exactly on
   another edge, or an area that is nearly zero.

## Consequences

**Good:** The profile that the engine accepts is the profile of the true coordinates, on each platform. K4 can build
on predicates whose sign is correct. The rounded path decides almost every case, so the cost is small.

**Bad:**
- A saved file that only the rounded rule accepted no longer opens. The engine has no tool to repair it.
- A case near the bound runs with `BigInteger` and is slower. A profile with many arcs has many such cases only when
  its points are nearly on one line.
- The 3D predicate does not exist until K4.

**Next:** TASK-0164 applies this record. K4 adds `Orient3D` with the same form: a filter with its own bound, and the
exact path of item 3.

## History

- 2026-11-02 · TASK-0164 · Accepted under Q1 A and Q2 A of the owner of 2026-11-02. The place, the filter and the exact
  path follow from those answers and the cases of the context.
