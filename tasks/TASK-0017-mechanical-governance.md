---
id: 0017
title: Make each governance rule mechanical
status: Done
phase: P0.4
opened: 2026-09-20
depends-on: [0016]
governed-by: []
writes:
  create:
    - Engine.Tests/Governance/RegisterGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - tasks/TASK-0017-mechanical-governance.md
  modify:
    - docs/adr/0001-kernel-abstraction-capabilities.md
    - docs/adr/0002-headless-first-cli-as-canonical-client.md
    - docs/adr/0003-blazor-as-thin-viewer.md
    - docs/adr/0004-engine-runtime-is-authority.md
    - docs/adr/0005-event-stream-and-replay.md
    - docs/adr/0006-command-execution-model.md
    - docs/adr/0007-ui-ephemeral-state-boundary.md
    - docs/adr/0008-command-query-event-triad.md
    - docs/adr/0009-3dengine-core-peer-render-kernel.md
    - docs/adr/0010-subscription-reset-snapshot-format.md
    - docs/adr/0011-server-default-deployment-topology.md
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0013-command-query-schema-declaration.md
    - docs/adr/0014-manifold-native-interop.md
    - docs/adr/0016-handler-declared-construction.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - nuget.config
    - .github/workflows/build-manifold-native.yml
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0017 — Make each governance rule mechanical

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`docs/working-agreement.md` section 6.2 states this rule: a rule in text only is a rule that an agent
will break. `docs/templates.md` lists twelve gates. Three of them exist: the diagnostics register, the
schema parity and the replay determinism.

Four rules have no check today:

1. The register rules. No check reads a `due` date, a second extension or the limit of 20 entries.
2. The marker rule. A word such as `TODO` or `interim` needs a register identifier, and no check
   verifies it.
3. The ADR rules. Each ADR needs a status from a closed set and an `enforced-by` field. A supersession
   needs a reciprocal field. The index is maintained by hand and it is incorrect. Register entry
   R-0006 holds this item.
4. The dependency direction. `CLAUDE.md` has listed this gate for months. No check exists. Register
   entry R-0004 holds this item.

The 14 ADRs from 0001 to 0014 have no front matter. ADR-0016 has front matter, because
`docs/templates.md` was approved before it. Therefore a gate would exempt 14 of 15 records.

## Goal

Each of the four rules fails a build when a person or an agent breaks it.

## Scope (in)

1. Add front matter to the 14 ADRs. Record the status and the date that each `## Status` section
   already gives. Do not change the decision text.
2. Record the two drift items that the v0.17 review found. ADR-0011 amends ADR-0004. ADR-0012 carries
   an in-file amendment.
3. Regenerate `docs/adr/README.md` from the front matter.
4. Add four gate tests in `Engine.Tests/Governance/`.
5. Close register entries R-0004 and R-0006.

## Scope (out)

- Do not write a tool that generates the ADR index. A test that compares the index against the front
  matter gives the same protection at a much lower cost. A generator needs its own task.
- Do not add the write-set check. It needs a step in continuous integration and a decision about the
  local hook. It becomes its own register entry and its own task.
- Do not change the decision text of any ADR. Front matter is metadata about the record.
- Do not change code in a project other than `Engine.Tests`.

## Acceptance criteria

- [x] Each ADR has front matter with `id`, `title`, `status`, `date`, `affects` and `enforced-by`.
- [x] A supersession field and an amendment field are reciprocal, and a test proves it.
- [x] `docs/adr/README.md` agrees with the front matter, and a test proves it.
- [x] A test fails when a register entry passes its `due` date.
- [x] A test fails when a marker word has no register identifier.
- [x] A test reads each project file and verifies each dependency rule.
- [x] Each new gate was verified by injection: it failed when a violation existed.
- [x] `dotnet test` passes with more tests than before.
- [x] Register entries R-0004 and R-0006 are closed.

## Outcome

Shipped as v0.19. Four gates exist in `Engine.Tests/Governance/`. The test count went from 151 to
173. `dotnet build` gives zero errors.

Each ADR from 0001 to 0014 has front matter. No decision text changed. The amendment graph is
reciprocal: 0011 amends 0004, 0008 amends 0006, and 0016 amends 0013. Each amended record carries the
status `Amended`. `docs/adr/README.md` is regenerated from the front matter.

Register entries R-0004 and R-0006 are closed. R-0017 is open for the write-set check, with a limit of
2027-03-19. The roadmap gives that work the identifier P0.7.

Three corrections came from the gates themselves:

- The register that this session seeded broke its own rules. R-0013 was overdue. Eight entries gave a
  due date that did not agree with the lifetime of the class. Two repairs followed. First, the rule now
  fails only when a due date is later than the class default; an earlier date is a deliberate
  tightening. Second, each real exit was applied: an extension is recorded for R-0013, R-0014 and
  R-0015, and the date of R-0016 is corrected.
- The marker gate found two true violations. `nuget.config` called its feed an interim bootstrap with
  no identifier, and it now cites R-0007. The header of `build-manifold-native.yml` said "DRAFT — not
  yet executed", which was false, because the workflow had run for months. The header is corrected and
  R-0009 is resolved. The write set of this task was widened to include both files, and this line
  records that change.
- The dependency gate passed while a violation existed. See the Method block.
- The declared write set did not agree with the work. `docs/adr/0016-handler-declared-construction.md`
  needed the reciprocal `amends` field, and the list did not name that file. `docs/templates.md`
  needed no change. The list is corrected in both directions. Nothing verifies a write set today,
  therefore only this note and register entry R-0017 hold the item.

## Method

**Mechanical.** The front matter of each ADR: each field came from the `## Status` section that the
record already gave. The regeneration of the index table. The parse of the register.

**Judgement.** The scope of the marker gate. A gate that read an ADR, a closed task or the ledger
would demand a change to an immutable file, therefore it would set two rules against each other. The
gate reads live code and live configuration only. The choice of a test over a generator for the ADR
index: the protection against drift is identical and the cost is far lower.

**Weakest.** The injection test of the dependency gate, on the first attempt. The gate reported eight
passes while `Engine.Core` held a reference to `3DEngine.Core`. The cause was a set of three git
worktrees under `.claude/worktrees/`, each one a full copy of every project file at an older commit.
The graph is keyed by project name, therefore a stale copy shadowed the real project. Two changes
followed: the enumeration excludes that directory, and a new test fails when one project name appears
more than once. The gate then failed as designed.

The lesson is general. A gate that reads the working tree must define its own scope, and a gate that
nobody tried to break is not known to work. The acceptance criterion "verified by injection" found the
one defect that every other check missed.
