---
id: 0339
title: The medium and low findings of the codebase review of 2026-11-10
status: Done
phase: P0.141
opened: 2026-11-10
depends-on: [0338]
governed-by: [0001, 0004, 0007, 0008, 0012, 0013, 0016, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0036, 0037, 0060, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0112, 0113, 0114]
writes:
  create:
    - tasks/TASK-0339-medium-and-low-findings-of-the-review-of-2026-11-10.md
    - docs/adr/0114-the-findings-of-the-review-of-2026-11-10.md
  modify:
    - 3DEngine.App/ProposedValues.cs
    - 3DEngine.App/AcceptHold.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - Engine.Contracts/Geometry/Rotation.cs
    - Engine.Core/Commands/RotateCommandHandler.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Commands/RotateCommandTests.cs
    - 3DEngine.ImageTests/PendingChipTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - eng/test-floor.txt
    - docs/adr/0110-a-proposal-shows-each-value-on-its-chip.md
    - docs/adr/0111-a-proposal-takes-its-own-taps-and-commands.md
    - docs/adr/0112-a-command-turns-a-body.md
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Kernel/**
---

# TASK-0339 — The medium and low findings of the codebase review of 2026-11-10

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-10 (`docs/reviews/2026-11-10-codebase-review.md`) found one medium finding and three low
findings. Section 7 of `docs/templates.md` puts them after the next product phase (R18a or R18b).

## Goal

Each of V164, E201, V165 and T181 is fixed, or has a register entry with its reason.

## Scope (in)

1. V164: the line of a proposal shows an angle in degrees, or does not name an angle constraint as a length.
2. E201: a quaternion whose entries are not a turn is refused or scaled first; a new code goes into `docs/diagnostics.md`.
   A change of the refusals of `Rotate` changes a decision of ADR-0112: an ADR or an amendment records it.
3. V165: a tap on the path line is free while an accept runs.
4. T181: a test of the hold through the host, or a register entry; the layout test checks each pair of panels.
5. A test for each fix that fails without the fix.

## Scope (out)

- E200, which TASK-0338 holds.

## Acceptance criteria

- [x] Each fix has a test that fails on an injection of the old code.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0141-review-findings) · v0.324

## Notes for the implementer

- E201 changes `Engine.Contracts/Geometry/Rotation.cs`. CLAUDE.md asks the owner before a change of the public shape of
  `Engine.Contracts`; a new refusal inside `FromQuaternion` keeps the shape, but say so in the ADR.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the list.
  Add a file to the write set when the work needs it, and say why in the progress.

## Method

Mechanical: the probes of the review as the tests.
Judgement:  the limit of a quaternion; the form of an angle in the line of a proposal.
Weakest:    T181 needs the window, or a seam that the code does not have yet.

## Progress

- 2026-11-10: opened from the codebase review of 2026-11-10.
- 2026-11-10: started at main 7defcec (v0.323), branch p0141-review-findings, by the order of the owner (A). The owner
  answered E201 with A: the command refuses the quaternion with a new code, E-GEOM-QUATERNION-TOO-SMALL. ADR-0114 records
  the four fixes and amends ADR-0110, 0111, 0112 and 0113. The write set adds the handler of Rotate and the codes (E201),
  DesktopUi.Proposal.cs, which forms the line (V164), NativeThreeDEngine.cs, which calls the new decision of AcceptHold
  (T181), PendingChipTests.cs, which holds the tests of the proposed values, and the ADR files, the index and the registry
  of the codes; SchemaDiagnosticsEndpoint.cs publishes the new code, as the gate of the schema asks. V165 also frees FocusAction and EndFocusAction of R17b, which came after the review and write nothing to
  the log. The first test of E201 found that parts of 1e-162 give s = 0, so the code depends on a part that is not zero,
  not on s > 0. Six injections: the old limit, the old line, PathAction held, the accept ignored, and a second panel at the
  place of the first, each failed its test; the old check of the layout passed the last one. Each file came back byte
  for byte.
- 2026-11-10: `governed-by` adds 0108 (TASK-0332): ADR-0108 affects `3DEngine.ImageTests/ProposalArcTests.cs`.
- 2026-11-10: run 42897717382 passed on the branch. Closed at v0.324: the ledger entry, the roadmap line and the
  History of ADR-0114.
