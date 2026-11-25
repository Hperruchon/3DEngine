---
id: 0466
title: The support of the lamp as the second case of K10, with stand-in sizes, its builder and the rows of its gaps
status: Done
phase: K10b
opened: 2026-11-25
depends-on: [0465]
governed-by: []
writes:
  create:
    - tasks/TASK-0466-the-lamp-support-case.md
    - Engine.Tests/Assembly/LampSupportCase.cs
    - Engine.Tests/Assembly/LampSupportCaseTests.cs
    - docs/proposals/2026-11-25-the-lamp-support-case-of-k10.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0466 — The support of the lamp as the second case of K10, with stand-in sizes, its builder and the rows of its gaps

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K10 builds the reference case of the assembly track, the tilting support of a lamp, with the commands that exist (ADR-0126,
`docs/proposals/2026-11-12-assembly-with-ai.md`). K10a built the bookcase of the owner first, because the sizes of the lamp were
not known (R-0070). On 2026-11-25 the owner answered Q2 A ("the owner gives the sizes"). The same day the owner asked for a
recommended size of the lamp; the session gave stand-in sizes, and the owner answered "use the stand-in sizes, start K10b after
R32a".

## Goal

The case of the lamp is a builder in `Engine.Tests` that a test checks against a table, and each gap of the commands that the
case finds has a roadmap row.

## Scope (in)

1. `docs/proposals/2026-11-25-the-lamp-support-case-of-k10.md`: the stand-in sizes, the first sizes of the bracket, the arm
   and the knob, the box of each body, the contacts at 0 degrees, and the gaps.
2. `LampSupportCase`: the commands of the seven bodies through a `DocumentSession`.
3. `LampSupportCaseTests`: the name and the box of each body, the envelope of the mount at 0 degrees, the class of each pair
   by `CheckInterference`, and a save and an open.
4. The new rows K23 (a hole command) and K24 (the pair check of bodies with holes), the row K10b, a progress line in R-0070,
   the test floor and the ledger entry v0.450.
5. Register entry R-0078 for the test of P16 that failed on Ubuntu in the run of the close of R32a.

## Scope (out)

- The real sizes of the owner. R-0070 stays open for them and for the ADR of P11.0.
- The picture of the case in the window (plan of K10, slice K10c). The owner denied the control of the window in this session.
- A change of the bound of work of ADR-0182 item 8. Row K24 decides it.

## Acceptance criteria

- [x] Each of the seven bodies has its name and the box of the table: exactly for a body of plane outside faces, within 0.5 mm
  for a curved body.
- [x] Each part of the mount is within 60 mm under the crossbar at 0 degrees.
- [x] The 13 pairs that the pair check answers have the class of the table, and no pair is a fault; the 8 other pairs give
  `E-CHECK-WORK-LIMIT`.
- [x] A save and an open give the same bodies and the same boxes.
- [x] Each gap of the case has a row, and the proposal names it.
- [x] `docs/register.md` holds 13 open entries of 15.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- `depends-on` names TASK-0465 (R32a): the branch starts from the close of R32a, because the owner ordered K10b after R32a and
  R32a is not merged yet.
- `CreateCylinder` centres a cylinder on the origin with its axis along +z. A `Rotate` by (1, 1, 0, 0) or (1, 0, 1, 0) is an
  exact turn of 90 degrees, because `Rotation.FromQuaternion` divides by the sum of the squares, 2.

## Outcome

Status: Done · commit (the merge of the branch k10b-lamp-support) · v0.450

## Method

Mechanical: the commands of the table, and the expected boxes and classes from the table, apart from the builder.
Judgement:  the stand-in sizes and the first sizes of the mechanism; a test that requires the 8 refusals of the bound of work,
            so that row K24 must change the test when it removes the gap.
Weakest:    the stand-in sizes are not measurements, so the case shows the gaps of the commands and not the fit of the real
            lamp. The bounds of a curved body agree to 0.5 mm, the size of its facets, and not exactly.

## Progress

- 2026-11-25: opened on the order of the owner "use the stand-in sizes, start K10b after R32a", branch `k10b-lamp-support`
  from the close of R32a.
- 2026-11-25: run 45576431753 of `edbf5715` passed on each job. Closed at v0.450.
