---
id: 0195
title: The query CheckTravel turns a pivot from limit to limit in memory and gives its first contact, with a step that passes no contact
status: Amended
topic: Kernel, queries, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: []
amended-by: ['0196']
affects:
  - Engine.Core/Queries/CheckTravelQuery.cs
  - Engine.Core/Queries/CheckTravelQueryHandler.cs
  - Engine.Core/Commands/JointTree.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - Engine.Tests/Queries/CheckTravelTests.cs
enforced-by: Engine.Tests/Queries/CheckTravelTests.cs
notes: Phase K12a, TASK-0490. The standing order of the owner of 2026-11-28 answers the choices of this ADR with the recommended option; no change of Engine.Contracts.
---

# ADR-0195 — The query CheckTravel turns a pivot from limit to limit in memory and gives its first contact, with a step that passes no contact

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K12 asks for a query that checks a pivot from one limit to the other, sampled with a margin so that no contact between two
samples is missed, with the intended contacts exempt, and that gives the first contact angle to a stated tolerance; a slider gets
one exact sweep. ADR-0126 item 4 (D4) gives the guarantee: at each sample the gap must be more than the largest movement of a point
of the bodies that turn before the next sample, and the report states the step, the radius and the tolerance. ADR-0157 item 5
exempts the pair of a joint. P11a and P11b (ADR-0193, ADR-0194) gave the joints, their limits and the pairs of the joints.

The query changes no file of `Engine.Contracts`: its records are in `Engine.Core/Queries`. The standing order of the owner of
2026-11-28 answers the choices below with the recommended option.

## Decision

1. **The query (K12a).** `CheckTravel` takes `jointId` and an optional `tolerance` in millimetres. The joint is a pivot with
   limits; another joint gives `E-CHECK-TRAVEL-INVALID`, and an unknown joint `E-JOINT-NOT-FOUND`. The query writes nothing to the
   log and changes nothing: it computes the placements in memory, not with commands on a fork of the Document (recommended over a
   fork, which replays the whole log for each sample). The slider and its exact sweep are row K12b.
2. **The bodies.** The bodies that turn are the child of the joint and each body under it through tight joints, with the
   placements that the tree gives at each value (`JointTree.Subtree`); each other live body does not turn. A pair of the two
   bodies of any joint of the Document is exempt, as in `CheckInterference` (ADR-0194 item 3). A mesh body or a body with a fault
   gives the refusal of `MeasureTarget.SolidOf`. The value of a loose joint does not matter: the travel turns the joint as if it
   were tight.
3. **The step (ADR-0126 item 4).** The radius is the largest distance of a vertex that turns from the axis of the pivot, with a
   margin of 1e-9 for the rounding. At each sample the gap g is the smallest exact distance of a pair (`NearestPoints`), and the
   first sample also finds an overlap (`PlanarBoolean.Intersect`). A point at the radius r moves at most r times the chord of the
   turn, so a step whose chord times r is below g passes no contact. The step is the rest of the travel, halved by the formula of
   the half angle with `sqrt` only until it is not above a half turn and its chord is small enough; the travel ends with a sample
   at the upper limit. The tolerance is 0.01 mm by default, and the travel stops at the first sample whose gap is at most the
   tolerance. Each step uses `+ - * /` and `sqrt`, so each platform gives the same answer.
4. **The answer.** `TravelInfo` gives `free`, or the first `contact` (the value of the joint as a cosine and a sine of a length
   of one, the pair, and its gap), and the count of the samples, the radius, the tolerance and the two limits. No contact comes
   before the value of `contact`, and its gap is at most the tolerance.
5. **The limits of the work.** One sample may need at most the work of `CheckInterference` (ADR-0182 item 8), and a travel at
   most 4096 samples; above either, the query gives `E-CHECK-WORK-LIMIT`. A tolerance that is not a finite length above zero gives
   `E-CHECK-TRAVEL-INVALID` (new).

## Alternatives considered

- **Commands on a fork of the Document (P15).** The fork replays the whole log, so each sample costs a replay.
- **A fixed count of samples with a margin.** A fixed step needs a margin of the largest step everywhere, so it either misses
  nothing and is slow, or is fast and stops far from the contact. The conservative advance takes long steps where the gap is
  large.
- **Angles with `Sin` and `Cos`.** `CLAUDE.md` rule 1 covers the log only, but the answer of an agent must be the same on each
  platform, and the formula of the half angle needs `sqrt` only.

## Consequences

**Good:** an agent asks if a pivot can turn from limit to limit, and gets the first contact with its pair, with a guarantee that
no contact comes before it.

**Bad:**
- A travel whose bodies come near but do not touch over a long arc takes many short steps; the limit of 4096 samples refuses it.
- The radius bounds each point that turns, so a long arm makes the steps short near a small part.
- The answer is in doubles: the cosine and the sine of the contact have the rounding of the products of the steps.
- The slider has no travel until K12b.

## History

- 2026-11-28 · TASK-0490 · The record, Accepted under the standing order of the owner of 2026-11-28.
- 2026-11-28 · TASK-0491 · Amended by ADR-0196: item 1, a slider with limits has a travel, by one exact sweep.
