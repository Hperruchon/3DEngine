---
id: 0477
title: The medium and low findings of the codebase review of 2026-11-28
status: Active
phase: P0.193
opened: 2026-11-28
depends-on: [0476]
governed-by: [0004, 0007, 0008, 0015, 0019, 0025, 0029, 0031, 0072, 0092, 0099, 0109, 0111, 0121, 0131, 0184, 0185, 0187, 0188, 0190, 0191]
writes:
  create:
    - tasks/TASK-0477-the-findings-of-the-review-of-2026-11-28.md
  modify:
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/SectionView.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/PipelineStartTests.cs
    - 3DEngine.ImageTests/SectionViewTests.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Contracts/BodyPlacement.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/Placements.cs
    - Engine.Tests/Commands/PlaceBodyTests.cs
    - Engine.Tests/History/GroupHistoryTests.cs
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/0191-the-placement-of-a-body.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid: []
---

# TASK-0477 — The medium and low findings of the codebase review of 2026-11-28

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-28 (`docs/reviews/2026-11-28-codebase-review.md`, TASK-0475) found two medium and seven low
findings. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding of this task has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: V239 (the window keeps the faces and the edges of the selected body by its key only) and E247 (ADR-0037 item 8 tells
   a client to read a mesh again only when the key changes).
2. Low: E248, E249, E250, V240, T203, T204 and T205, each with the correction that the review recommends. E249 takes the
   correction of the answer to Q2 of the review.
3. The write set grows when the task starts (`docs/templates.md`, section 2).

## Scope (out)

- E246: TASK-0476.
- Until 2026-11-28 the forbid was `Engine.Contracts/**`. E248 adds `[JsonIgnore]` to `BodyPlacement.IsIdentity`, so the JSON
  holds the seven numbers of ADR-0191 item 8; the C# shape does not change, and ADR-0191 has a History line.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0193-findings) · v0.462

## Method

Mechanical: the corrections that the review names.
Judgement:  V239: a key of the mesh from the geometry key and the bits of the placement, so the caches of `BodyActions` keep
            their type; E248: `[JsonIgnore]` and not a removed member, so the C# shape of `Engine.Contracts` stays; T203: one
            test that asks for the layer, and not an assert in each of the 26 files that count its messages.
Weakest:    the path of `Proposals` for E250 has no test of its own: it needs a host whose catalogue throws outside the fork. The
            first scene of the section test had two overlapping bodies, and their lines depended on the order of the random
            identifiers of the scene of `Rotate` and `Translate`; the test now has one body, and that order dependence is not
            examined.

## Progress

- 2026-11-28: opened by TASK-0475.
- 2026-11-28: started under the standing order of 2026-11-28, after K25. V239: `KeyOf` gives a key of the geometry key and the
  placement; V240: the merge of V55 takes the placement; T205: the window test runs the section view, and `MeshKey` has no
  conversion from a key alone; E248: `[JsonIgnore]` on `IsIdentity`; E249: `Relative` and `Compose` scale each quaternion by a
  power of two, and a placement that gives no turn stops a read; E250: `ReversedKey` on the exception, and the accept gives
  `E-API-PROPOSAL-NOT-REVERSED`; E247: ADR-0191 item 8, ADR-0037 and the guide give the rule of a client; T203: a test asks for
  the validation layer; T204: the faces test compares the outlines and the segments, the view lines have a placed cylinder,
  and a new test cuts a placed cylinder with a section of a sheet. Six injections each make a named test fail. Ledger v0.462.
