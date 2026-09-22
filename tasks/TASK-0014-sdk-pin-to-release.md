---
id: 0014
title: Pin a released SDK so each computer can build the solution
status: Done
phase: P0.1
opened: 2026-09-20
depends-on: []
governed-by: []
writes:
  create:
    - tasks/TASK-0014-sdk-pin-to-release.md
  modify:
    - global.json
    - 3DEngine/Documentation/Running.md
    - BlazorApp/BlazorApp/Documentation/Running.md
    - docs/CURRENT-STATE.md
    - docs/register.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - .github/workflows/**
---

# TASK-0014 — Pin a released SDK so each computer can build the solution

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`global.json` pins the SDK version `10.0.300-preview.0.26177.108`. It sets `rollForward` to
`disable`. No public feed supplies that preview SDK.

The owner computer has the SDK versions 8.0.302, 10.0.300 and 10.0.400. It does not have the
preview SDK. Therefore `dotnet build` fails at SDK resolution, and the solution does not build.

TASK-0006 recorded this risk at line 102. Its text says: "either bump global.json to a public SDK or
wait for one".

Register entry R-0001 holds this problem. Its class is `risk`. Its `due` date is 2026-09-24.

Two documentation files also give an incorrect SDK version. They give `10.0.200-preview.0.26103.119`,
which no file pins.

## Goal

Each computer with a released .NET 10 SDK builds the solution and runs the tests.

## Scope (in)

1. Change `global.json` to a released version with a roll-forward policy.
2. Correct the SDK version in the two `Running.md` files.
3. Verify that the build passes and that the tests pass.
4. Add an entry to `docs/CURRENT-STATE.md`.
5. Close register entry R-0001.

## Scope (out)

- Do not change `.github/workflows/ci.yml`. It already installs the SDK from `global.json`, which
  is correct.
- Do not add the Windows runner and the macOS runner to the gate. That work is register entry
  R-0002 and a separate task.
- Do not change a project file. Each project targets `net10.0` and that target is correct.
- Do not change any file in `Engine.Contracts/` or `Engine.Core/`.

## Rule 10 note

This task changes two files in `3DEngine/` and in `BlazorApp/`. Rule 10 in `CLAUDE.md` permits this
change, because this task gives that scope. The change corrects a statement about the SDK. It does
not change code in either project.

## Acceptance criteria

- [ ] `global.json` gives a released SDK version.
- [ ] `dotnet build 3DEngine.sln` passes on a computer with SDK 10.0.300 or 10.0.400.
- [ ] `dotnet test` passes. The result is 134 tests.
- [ ] Each `Running.md` file gives the same SDK version as `global.json`.
- [ ] `docs/CURRENT-STATE.md` has a new entry.
- [ ] Register entry R-0001 moves to the section "Closed".

## Notes for the implementer

The roll-forward policy must accept a higher feature band. The owner computer has 10.0.400, and the
version floor is 10.0.300. The policy `latestFeature` accepts each SDK with the same major version
and minor version, and it selects the highest one.

Set `allowPrerelease` to `false`. A preview SDK must not enter the build without a decision.

## Outcome

Status: Done. Version v0.16.

`global.json` gives the version `10.0.300`, the policy `latestFeature` and `allowPrerelease: false`.
The resolved SDK on the owner computer is 10.0.400.

`dotnet build 3DEngine.sln` passes: zero warnings, zero errors, 3.3 seconds.
`dotnet test` passes: 134 tests, zero failures, zero skipped.

Each `Running.md` file now gives "10.0.300 or higher" and points to `global.json`.
`docs/CURRENT-STATE.md` has the v0.16 entry. Register entry R-0001 is closed.

## Method

Mechanical: the change to `global.json`; the text replacement in two `Running.md` files; the ledger
entry; the move of the register entry.

Judgement: three decisions. First, the version floor is `10.0.300` and not `10.0.400`, because a
floor with `latestFeature` also works on a computer that has only 10.0.300. Second,
`allowPrerelease` becomes `false`, because a preview SDK must not enter the build without a
decision. Third, `ci.yml` needs no change, because it installs the SDK from `global.json` already.

Weakest: the policy `latestFeature` accepts each SDK with the major version 10 and the minor
version 0. A future SDK 10.0.500 with a behaviour change would enter the build with no decision. A
test does not detect this condition. The gate on three operating systems, in register entry R-0002,
reduces the risk, because it would show a difference between two runners.
