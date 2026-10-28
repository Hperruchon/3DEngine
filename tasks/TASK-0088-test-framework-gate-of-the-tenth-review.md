---
id: 0088
title: No file outside the gate files can replace the test framework of a test project
status: Done
phase: P0.34
opened: 2026-10-28
depends-on: [0087]
governed-by: []
writes:
  create:
    - tasks/TASK-0088-test-framework-gate-of-the-tenth-review.md
  modify:
    - Engine.Tests/Governance/TestAttributeGateTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0088 — No file outside the gate files can replace the test framework of a test project

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-28 found one high gap in the checks of the pipeline (finding T62 of
`docs/reviews/2026-10-28-codebase-review.md`). A support file in `Engine.Tests` can declare a test framework of
xUnit that gives each failed test as passed. The exact count of tests does not change, and the gate program
passes the tree. `docs/templates.md`, section 7, puts a high finding into a task before the next product phase,
so this task comes before P8a.

## Goal

No file outside the gate files can change the result of a test in a test project.

## Scope (in)

1. `TestAttributeGateTests` refuses, in each test project, the attributes `TestFramework`, `TestCaseOrderer` and
   `TestCollectionOrderer`, and each type that derives from a type of `Xunit.Sdk` or implements a message sink of
   `Xunit.Abstractions`.
2. The form of the review is a test case of the gate: the framework file of T62 fails the gate.

## Scope (out)

- The three gates that run engine code stay in the test process (T31). A move of them to a build of `main` is a
  larger change, and register entry R-0031 holds it.
- The medium and low findings of the review (TASK-0089, TASK-0090).

## Acceptance criteria

- [x] The framework file of T62 fails the gate from the gate program, which passes it before the change.
- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.90 · the commit that carries this block.

No file outside the gate files can give xUnit a framework, a runner, a message sink, an order or a discoverer of
its own in a test project, and no product project can declare a name of xUnit where a test sees it. The file of
T62 fails the gate program, which passed it before.

## Method

**Mechanical.** 26 new tests, with each theory case counted: the two gates on the repository, one permitted case and 23 cases of the
forms. The file of T62 in `Engine.Tests/Http/` failed the gate program with six offences, and a type
`Engine.Assert` in `Engine.Core` failed it with one. Four injections into the new checks each failed their cases.

**Judgement.** The gate refuses each point where a framework, a runner or a discoverer enters, and not each type
of `Xunit.Sdk` by name: each other type of that namespace needs the namespace, which the gate refuses in a test
project. `Xunit.Abstractions` stays permitted, because a test uses its output helper; the interfaces of that
namespace that a sink or a discoverer implements are refused by name. A product type counts when a test namespace
sees it: in the global namespace, in a namespace that encloses a test namespace, or under one.

**Weakest.** The gate reads syntax and binds no name, so an alias chain through a type that is not of xUnit, or
reflection at run time, is not seen. The three gates that run engine code still run in the test process (T31),
so a new form that reaches them is a new finding.

## Notes for the implementer

- **The form.** The review gives the framework file: a class that derives from `XunitTestFramework`, an executor
  that derives from `XunitTestFrameworkExecutor`, and a sink that derives from `LongLivedMarshalByRefObject` and
  gives each `ITestFailed` as `TestPassed`. The analyzer of xUnit requires that base class of the sink.
- **The write set.** Each file of `Engine.Tests/Governance/` is a gate file, which the write set must name exactly.
- **The size.** About 1 evening.

## Progress

- 2026-10-28: opened by the codebase review of 2026-10-28 (TASK-0087).
- 2026-10-28: the task is active. The owner answered the questions of the review on 2026-10-28: A to each of Q1
  to Q8. The roadmap holds the answers in the rows of P8a and of a new slice P8f, which Q8 makes: the review gate
  reads a slice as a number and one letter, so "P8a1" would not count, and P8b to P8e exist. Q2 and Q3 change the
  public shape of `Engine.Contracts`; the owner accepted that, and the ADR comes with P8a.
- 2026-10-28: the gate. `TestAttributeGateTests` refuses in each test project the attributes that give xUnit a
  framework, an order or a discoverer, each type that derives from a type of that runner surface, the namespace
  `Xunit.Sdk` in a using directive or a dotted name, an alias of `Xunit` or of such a type, and an extern alias.
  It also refuses a type with a name of xUnit in a product project, in the global namespace or in a namespace
  that a test namespace sees. 26 new tests. The file of T62 failed the gate program with six offences, which
  passed it before the change, and a type `Engine.Assert` in `Engine.Core` failed it. Four injections each
  failed their cases (no attribute check: 5, no base type check: 3, no check of `Xunit.Sdk`: 5, no product
  namespace seen: 4).
- 2026-10-28: closed after a clean build and a full local run of 662 tests (533 in `Engine.Tests`, 89 in
  `3DEngine.Tests`, 40 in `3DEngine.ImageTests`).
