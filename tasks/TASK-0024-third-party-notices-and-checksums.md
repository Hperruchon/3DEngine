---
id: 0024
title: Give the native payload a licence notice and a verified checksum
status: Done
phase: P0.10
opened: 2026-09-22
depends-on: [0023]
governed-by: [0014]
writes:
  create:
    - THIRD-PARTY-NOTICES.md
    - Engine.Tests/Governance/NativePackageGateTests.cs
    - tasks/TASK-0024-third-party-notices-and-checksums.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0024 — Give the native payload a licence notice and a verified checksum

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0005 recorded this problem and its limit was 2026-09-24. The repository holds a
binary of 4.4 MB. Its nuspec has no licence element. The repository has no third-party notices file.
ADR-0014 section 5 requires a checksum and no checksum exists.

## Goal

A reader can tell what the binary contains, under which licence it arrives, and whether the bytes
changed.

## Scope (in)

1. Write `THIRD-PARTY-NOTICES.md`. Name each component and its licence.
2. Give a SHA-256 value for the package and for each distinct native binary.
3. Add a gate that compares each value against the file.
4. Close register entry R-0005.

## Scope (out)

- Do not rebuild the package. A rebuild needs the native build workflow, three runners and a new
  binary in the repository. Nothing here requires it.
- Do not change the nuspec. It lives inside the package.
- Do not change code in a project other than `Engine.Tests`.

## Acceptance criteria

- [x] `THIRD-PARTY-NOTICES.md` names each component and its licence.
- [x] Each licence claim comes from the source repository and not from memory.
- [x] A SHA-256 value exists for the package and for each distinct native binary.
- [x] A gate fails when a value does not match the file.
- [x] A gate fails when the package holds a binary that no value records.
- [x] Each direction was verified by injection.
- [x] Register entry R-0005 is closed.

## Outcome

Shipped as v0.25.

**Two components, not one.** The package holds Manifold under the Apache License 2.0. It also holds
Clipper2 under the Boost Software License 1.0, which the first reading missed. The package ships no
separate Clipper2 binary, therefore the code is compiled into the Manifold library. The evidence is a
search of the raw bytes: the name Clipper appears in `libmanifold.so.3.5.2`, in `manifold.dll` and in
`libmanifold.3.5.2.dylib`. The build sets `MANIFOLD_CROSS_SECTION=ON` and
`cmake/manifoldDeps.cmake` fetches Clipper2 at commit `46f6391`, which agrees with that evidence.

**The checksums are a rule.** `Engine.Tests/Governance/NativePackageGateTests.cs` reads each value
from the notices and compares it against the file, in both directions. A change to a binary that does
not change the notices fails. A binary inside the package that no value records fails. Both
directions were verified by injection.

**The package names the wrong repository.** Its nuspec holds
`<repository type="git" commit="718eab0684178e4fdf7ef419cc4ff26484008705" />`. That commit is not a
Manifold commit. It is a commit in this repository, dated 2026-07-04, with the subject "Merge pull
request #8 from Hperruchon/p7b-finish". The description in the same nuspec says "Version tracks the
pinned Manifold commit", therefore the package gives incorrect information about the origin of its
own binary. Register entry R-0018 holds the defect, because a correction needs a new build.

**A second correction to the record.** The package holds three runtime identifiers and not one.
TASK-0020 and the comment in the workflow both said `win-x64` only. v0.24 corrected them.

## Method

**Mechanical.** The checksums. The gate. The tables.

**Judgement.** Two decisions. First, the notices record the Manifold commit that the tag `v3.5.2`
names, and they state plainly that the nuspec is wrong and that a reader must not use it. The
alternative was to correct the nuspec, which is impossible without a rebuild and which would hide a
defect that a person must see. Second, the gate reads the notices rather than a separate checksum
file, so that one document holds the licence and the checksum and neither can drift from the other.

**Weakest.** Each licence claim comes from the GitHub API and from the CMake files of Manifold at the
pinned commit, and not from an audit of the compiled binary. A component with no string and no build
record would not appear in this file. The search of the raw bytes found Clipper2, which the first
reading missed, so the method has some power, but it is a search and not a proof.

A note on method that cost time here: a first attempt used `strings`, which this computer does not
have. The command returned nothing and the absence read as "no Clipper2 inside". A check of the total
count caught the error, and `grep` on the raw bytes gave the true answer. A tool that is absent
returns an empty result, and an empty result is not evidence.
