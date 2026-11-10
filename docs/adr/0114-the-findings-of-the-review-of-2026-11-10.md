---
id: 0114
title: A turn refuses a quaternion of very small parts, a proposal shows an angle in degrees, and an accept holds less
status: Accepted
topic: Geometry, clients
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0110', '0111', '0112', '0113']
amended-by: []
affects:
  - Engine.Contracts/Geometry/Rotation.cs
  - Engine.Core/Commands/RotateCommandHandler.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Tests/Commands/RotateCommandTests.cs
  - 3DEngine.App/ProposedValues.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/AcceptHold.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.ImageTests/PendingChipTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
  - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
enforced-by: Engine.Tests/Commands/RotateCommandTests.cs
---

# ADR-0114 — A turn refuses a quaternion of very small parts, a proposal shows an angle in degrees, and an accept holds less

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-10 (`docs/reviews/2026-11-10-codebase-review.md`) found one medium finding and three low
findings. TASK-0339 (P0.141) holds them.

- **E201 (low).** `Rotation.FromQuaternion` refused only a zero, a value that is not finite and an entry that is not
  finite. With parts near 1e-162 the squares are subnormal doubles, which have fewer than 53 bits. A probe gave the
  entries {1, 2, 2, 2, -1, -2, 2, 2, -1}, and a cube of 10 became a cube of 50, with the status `Applied`.
- **V164 (medium).** The line of a proposal named each `SetSketchConstraintValue` as a length. An angle constraint holds
  its cosine and its sine (ADR-0033 item 3), so a proposal of 45 degrees on an angle of 60 degrees showed
  `Plate C1  0.5 → 0.71 mm`.
- **V165 (low).** While an accept ran, the window held a tap on the path line. That tap only selects a level (ADR-0100),
  and Escape, which goes up one level too, was free.
- **T181 (low).** No test read the decision of the host to hold an action or an intent. The layout test checked the
  overlap of two windows only when one of them was fixed, so one panel over a control of another panel could pass.

On 2026-11-10 the owner answered the question of E201: the command refuses such a quaternion with a new code (A). The
other choice (B) was to divide the quaternion by its length first.

## Decision

1. **The limit of a quaternion (E201, amends ADR-0112 item 2).** With s = w² + x² + y² + z², `Rotation.FromQuaternion`
   gives no turn when s is below 2^-969. A double below 2^-1022 is subnormal. When s is at least 2^-969, each product
   of two parts that changes an entry by one unit in the last place or more is at least s · 2^-53, so it is a normal
   double with 53 bits. `Rotate` refuses a quaternion with four finite parts, at least one part that is not zero, and a
   finite s that is below the limit, with the new code `E-GEOM-QUATERNION-TOO-SMALL`. The sum s can round to zero, as
   for parts of 1e-162. Each other quaternion that gives no turn keeps `E-GEOM-INVALID-PARAM`. The check stays before the
   check of the capability. `Rotation` keeps its public shape: the limit is private, and the method keeps its parameters
   and its result.
2. **An angle in the line of a proposal (V164, amends ADR-0110 item 3).** The line names the value of a constraint only
   when the line can show it in its unit. A `distance` and a `radius` are lengths, in the length unit of the document. An
   `angle` shows in degrees, from the cosine and the sine of the constraint now and from `value` and `secondValue` of the
   command, for example `Plate C1  60 → 45 deg`. A command for an angle with no `secondValue`, and each other kind (a
   `tangent`, a `fixed` point), keep the line of their command (ADR-0092 item 5). The degrees are presentation: the
   client computes them with `Math.Atan2`, and nothing of them enters the log, so determinism rule 1 does not apply.
3. **The free actions of an accept (V165, amends ADR-0111 item 3).** `PathAction` is a free action, and `FocusAction` and
   `EndFocusAction` of R17b (ADR-0101) are free actions too. None of them writes to the log.
4. **The tests of the hold and of the layout (T181, amends ADR-0113 item 1).** `AcceptHold.Held` and `AcceptHold.Pass`
   make the decision of the host from the task of the accept, and the host calls them, so a test reads the decision with
   no window. The rule of the layout is: no two windows overlap, two panels included. The sheet of a proposal may still
   cover the panel (ADR-0092 item 7).
5. **Replay.** The log keeps the four numbers that the client sent. A log entry of a `Rotate` with such a quaternion that
   applied between v0.318 and this record now gives the refusal on a replay. Only a probe of a review is known to have
   sent one. Each platform gives the same refusal, because s and the limit use `*` and `+` and an exact power of two.

## Consequences

- An agent that sends a quaternion of very small parts gets a code that names the cause, and the body does not grow.
- A person reads an angle of a proposal in degrees before an accept.
- A tap on the path line and a focus work while an accept runs.
- The layout test fails when a panel covers another panel. An injection of a second panel at the place of the first
  failed the test at eight sizes; the check before this record passed it.

## Alternatives considered

- **Divide the quaternion by its length first (E201, B).** It keeps the command valid, but the turn of the body then
  differs from what a reader of the log computes from the four numbers, and `sqrt` adds a step to each reader. The owner
  chose A.
- **Name only the lengths (V164).** It is simpler, but a person then reads the raw command of an angle.

## History

- 2026-11-10 · TASK-0339 · The record, for findings E201, V164, V165 and T181 of the codebase review of 2026-11-10, with
  the answer A of the owner for E201.
