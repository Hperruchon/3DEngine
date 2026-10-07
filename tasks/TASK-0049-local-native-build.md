---
id: 0049
title: One build script makes the native libraries in the pipeline and on this computer
status: Done
phase: governance
opened: 2026-10-08
depends-on: [0048]
governed-by: []
writes:
  create:
    - tasks/TASK-0049-local-native-build.md
    - eng/manifold-native/build.sh
    - eng/manifold-native/build.ps1
    - eng/manifold-native/README.md
  modify:
    - .github/workflows/build-manifold-native.yml
    - docs/INDEX.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - nuget/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
    - CLAUDE.md
---

# TASK-0049 — One build script makes the native libraries in the pipeline and on this computer

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-06 the owner decided that a local build of the native libraries is for development and
test, and that it is welcome because it makes the use of GitHub "surgical": test here first, push only
a tested change. The official package in `nuget/` still comes from the workflow
`build-manifold-native.yml`, because macOS cannot be built on this computer and a runner gives a clean,
public build. TASK-0048 needed five builds on GitHub; a local pack of one minute found the cause of the
last one.

Today the recipe of the build lives only in the workflow. A local build that copies it can drift from
it. This computer has CMake and the x64 compiler in Visual Studio 18 Insiders. WSL is installed, and it
has no Linux distribution yet.

On 2026-10-08 the owner permitted the merge of each task of the night after a green run on the three
runners.

## Goal

The pipeline and this computer build the native libraries with one recipe.

## Scope (in)

1. **`eng/manifold-native/build.sh`** builds Linux and macOS: configure with the search-path flags of
   TASK-0048, build, stage the libraries with their aliases, remove the absolute search path on macOS,
   and run the check of TASK-0048.
2. **`eng/manifold-native/build.ps1`** builds Windows: configure with the Visual Studio generator,
   build, and stage. It finds CMake on the path, or through `vswhere` in Visual Studio.
3. **The workflow calls the two scripts** and keeps no copy of their steps. Its result does not
   change: the build jobs pass on the three runners.
4. **An immutable version.** The pack job does not overwrite a branch `native-package/<version>` that
   exists. It writes a notice and pushes nothing. A change to the workflow then rebuilds and checks,
   and does not replace the package that `THIRD-PARTY-NOTICES.md` names.
5. **`eng/manifold-native/README.md`** gives the local commands: a clone of the pinned Manifold commit
   into `artifacts/`, the build into `artifacts/native/<platform>/`, and a test of the engine against
   those libraries.

## Scope (out)

- No change to `nuget/` or to the package version.
- No Linux build on this computer until the owner installs a distribution (`wsl --install -d Ubuntu`).
  The script is ready for it.
- No local build of macOS.

## Acceptance criteria

- [ ] On this computer, `build.ps1` builds `manifoldc.dll` from the pinned commit, and the native tests
      of the engine pass against it.
- [x] The workflow, with the scripts, passes its build jobs on the three runners.
- [x] The run of this task finds the branch `native-package/3.5.2.1` and pushes nothing.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.46 · the commit that carries this block.

One criterion is not met: the local Windows build. This computer has no Windows SDK, so the linker
cannot open `kernel32.lib`. The owner installs the SDK through the Visual Studio Installer, and Ubuntu
in WSL. The scripts are proved on the three runners (runs 37694784464 and 37696665835), and the README
gives the local commands.

## Method

**Mechanical.** The two scripts from the steps of the workflow, the README, and the calls in the
workflow.

**Judgement.** A version is immutable, because the notices name one package per version and a rebuild
gives other bytes. Only `main` publishes, because a branch push could claim a version for good. The
check of a build path also looks for the build folder of the machine that runs the script, so that it
works on a computer and not only on a runner. Five defects that the review agents of the night found in
this new code were corrected here before the merge, because the code was not merged yet.

**Weakest.** No local build ran to the end; the first criterion waits for the SDK. A new package now
needs two merges: the pin, then the package. The Windows script checks no build path (T23).

## Progress

- 2026-10-08: the task is open, the first task of the night.
- 2026-10-08: the two scripts, the README, and the workflow that calls them. A local run of
  `build.ps1` on the pinned commit found CMake in Visual Studio 18 and failed at the configure step:
  the linker cannot open `kernel32.lib`, and MSBuild says that `WindowsSDKDir` is not defined. This
  computer has no Windows SDK (no folder `Windows Kits/10/Lib`). Its install is an action of the owner.
  A clean build of the solution restores the library of the package after a local test: an overwritten
  `manifoldc.dll` had the checksum of the package again.
- 2026-10-08: run 37694784464 built and checked with the scripts on the three runners, and the pack job
  found `native-package/3.5.2.1` and pushed nothing. The review agents of the night found five defects
  in this new code, and this task corrects them: a second build into one staging folder failed (T21);
  `build.ps1` stopped at a CMake warning in PowerShell 5.1 with a redirected output (T22); a push of
  any branch could claim a version, and a tag push started a run (T18); the pack job kept its token on
  disk (T19); the pin values reached the scripts unchecked (T20). Only `main` now publishes a package.
  Each correction was tested here: the staging line, the PowerShell function under redirection, and the
  two patterns.
- 2026-10-08: run 37696665835 built and checked on the three runners, and on this branch the pack job
  wrote "only main pushes a package" and pushed nothing. Closed with one criterion open, and ledger
  entry v0.46 records the work. The owner permitted the merge after a green run on 2026-10-08.
