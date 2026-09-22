---
id: 0015
title: Replace the charter and correct each operational rule
status: Done
phase: P0.2
opened: 2026-09-20
depends-on: [0014]
governed-by: []
writes:
  create:
    - tasks/TASK-0015-charter-and-rules.md
  modify:
    - docs/CHARTER.md
    - CLAUDE.md
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/register.md
    - docs/working-agreement.md
    - docs/templates.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - docs/adr/**
    - .github/workflows/**
---

# TASK-0015 — Replace the charter and correct each operational rule

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

A review of five steps examined the objectives, the anti-objectives, the deferred non-goals and the
operational rules. The owner approved each result. The review produced these findings:

1. The charter mission says "We do not build a 3D app". The owner states that the true meaning is
   different: the solution gives the parts, and it is not one product.
2. Two anti-objectives block approved work. "No partial command application" blocks a feature tree.
   "No needless future-proofing" blocks four irreversible decisions.
3. Six deferred items are now required. Examples are persistence and undo.
4. The scope clamp "no persistence" contradicts an approved objective.
5. The scope test says that absence from the roadmap is a stop signal. The roadmap has no track for
   the renderer and no track for the kernel. Therefore the test blocks each approved objective.
6. `docs/INDEX.md` gives an absolute prohibition and points to a section of `CLAUDE.md` that no
   longer exists.
7. The owner decided to own a geometry kernel with a closed feature boundary. Four rungs of ambition
   become refusals.
8. Research on floating-point behaviour produced nine operational rules.

## Goal

The governing documents agree with the twenty approved objectives, and each rule is one that a
person can obey today.

## Scope (in)

1. Replace `docs/CHARTER.md`. Give the platform mission. Give sixteen anti-objectives in six groups.
   Give the deferred items as one item on each line. Keep the scope test.
2. Correct `CLAUDE.md`. Remove the persistence clamp. Amend the future-proofing rule. Add nine
   operational rules. Add one reevaluation condition.
3. Correct `docs/INDEX.md`. Use the conditional form. Repair the broken reference. Add the three new
   documents.
4. Correct `docs/roadmap.md`. Add the renderer track and the kernel track, so that the scope test
   operates again.
5. Change the status of `register.md`, `working-agreement.md` and `templates.md` from `Proposed` to
   `Accepted`.
6. Add an entry to `docs/CURRENT-STATE.md`.

## Scope (out)

- Do not change an ADR. The ADR corrections are register entry R-0006 and a separate task.
- Do not write the ADR for persistence. That is ADR-0015 and a separate task.
- Do not change code. This task changes documentation only.
- Do not build a gate. Each gate is register entry R-0004 and later tasks.
- Do not remove `docs/open-questions.md` in this task. The register replaces it, but the file
  removal needs one more check for references.

## Acceptance criteria

- [ ] `docs/CHARTER.md` gives the platform mission and does not say "we do not build a 3D app".
- [ ] Each approved amendment from step 2 is in the charter.
- [ ] Each promoted item from step 3 is absent from the deferred list.
- [ ] `CLAUDE.md` has no persistence clamp.
- [ ] `CLAUDE.md` has the nine new rules.
- [ ] `docs/INDEX.md` and `CLAUDE.md` agree about the projects outside the engine spine.
- [ ] `docs/roadmap.md` has a renderer track and a kernel track.
- [ ] The three documents have the status `Accepted`.
- [ ] `dotnet build` and `dotnet test` give the same result as before this task.

## Outcome

Status: Done. Version v0.17.

`docs/CHARTER.md` is replaced. It gives the platform mission, sixteen anti-objectives in six groups,
the closed kernel boundary, and fourteen deferred items with one on each line. The scope test is
unchanged except for one sentence about the roadmap.

`CLAUDE.md` has no scope clamp. It has nine new operational rules and one new reevaluation condition.
One anti-pattern is marked inactive until milestone P0.3.

`docs/INDEX.md` agrees with `CLAUDE.md`. The broken reference is repaired.

`docs/roadmap.md` is replaced. It has five tracks, therefore each approved objective has a track.

The three governance documents have the status `Accepted`.

Verified: `dotnet build` gives zero warnings and zero errors. `dotnet test` gives 134 passed, zero
failed, zero skipped. No contraction and no sentence above 25 words exists in the changed documents.

## Method

Mechanical: the status change in three documents; the table rows in `docs/INDEX.md`; the ledger
entry; the check for references to removed rules.

Judgement: four decisions. First, the determinism rules go in `CLAUDE.md` and the determinism
property goes in the charter, because the charter holds a property and `CLAUDE.md` holds a mechanism.
Duplication in both would produce drift. Second, the refused kernel rungs go in the charter, because
they change what the project is. Third, `docs/roadmap.md` needed a replacement in this task, because
the charter scope test reads it and would otherwise block each approved objective. Fourth, the
anti-pattern about a command and a central file stays in the document and is marked inactive, because
removal would lose the rule before P0.3 can enforce it.

Weakest: the charter now holds sixteen anti-objectives and fourteen deferred items. That quantity is
large for one document, and an agent may read only the group that appears first. No test detects that
failure. The `affects` field from `docs/templates.md` solves the same problem for an ADR, and the
charter has no equivalent. I did not add one, because the charter is one file and the field needs a
gate that does not exist. Register entry R-0004 covers the gate work.
