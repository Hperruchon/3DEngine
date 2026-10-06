---
id: 0048
title: The native package finds its own dependency on each platform, and a commit rebuilds it with no manual step
status: Active
phase: governance
opened: 2026-10-06
depends-on: [0036]
governed-by: [0014, 0018]
writes:
  create:
    - tasks/TASK-0048-native-package-search-path.md
    - eng/manifold-native/manifold-ref.txt
  modify:
    - .github/workflows/build-manifold-native.yml
    - nuget/**
    - Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Governance/NativePackageGateTests.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
    - THIRD-PARTY-NOTICES.md
    - docs/INDEX.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
    - CLAUDE.md
---

# TASK-0048 — The native package finds its own dependency on each platform, and a commit rebuilds it with no manual step

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0035. TASK-0036 found that the native backend had never loaded on the Linux and the
macOS runners. In the package of 2026-09, `libmanifoldc` stores the build folder of a runner as the
search path of its dependency `libmanifold`:

- Linux: `/home/runner/work/3DEngine/3DEngine/build/src`, and not `$ORIGIN`.
- macOS: `/Users/runner/work/3DEngine/3DEngine/build/src`, and not `@loader_path`. The dependency has
  the install name `@rpath/libmanifold.3.dylib`.

On another computer the system loader does not find the dependency. Windows searches the folder of the
library and was not affected. TASK-0036 added a workaround: `ManifoldGeometryBackend.TryLoadNative`
loads the dependency first by its full path. A search of the raw bytes on 2026-10-06 found both paths.

Today two steps need a person with a GitHub sign-in: the workflow `build-manifold-native.yml` starts
only by `workflow_dispatch`, and its output is a workflow artifact, which needs a sign-in to download.
The owner did both by hand in 2026-09. `docs/reviews/2026-09-25-rules-review.md` gives the test: when a
process makes the owner the bottleneck, fix the process.

## Goal

The rebuilt package loads on each runner with no workaround, and the next rebuild needs no manual step.

## Scope (in)

1. **The search path.** The configure step gives `-DCMAKE_BUILD_RPATH_USE_ORIGIN=ON` and
   `-DCMAKE_BUILD_RPATH='$ORIGIN'` on Linux, and the same with `@loader_path` on macOS. The first plan
   used the install flags, and Manifold's own `CMakeLists.txt` overrides those.
2. **A check in the build job.** After the build, the job fails when `libmanifoldc` holds a path of the
   build machine, or when it does not hold `$ORIGIN` or `@loader_path`. It also asks the system tool
   (`ldd` on Linux, `otool -L` on macOS) to resolve the dependency in the staging folder.
3. **No manual start.** The workflow also starts on a push that changes `eng/manifold-native/manifold-ref.txt`,
   which holds the Manifold reference and the package version. `workflow_dispatch` stays.
4. **No manual download.** The pack job pushes the package to the branch
   `native-package/<version>`, so a session fetches it with git and no sign-in. Only the pack job gets
   `contents: write`. The build jobs, which compile the third-party source, stay `contents: read`.
   Question Q1 below.
5. **A new package version.** A computer keeps an extracted package in its NuGet cache by its version,
   so the rebuild cannot reuse `3.5.2`. The package becomes `3.5.2.1`. `NativeVersion` stays `3.5.2`,
   because it names the Manifold source, and the test of TASK-0036 compares it with the first three
   parts of the package version.
6. **The records of the binary.** The new package replaces the old one in `nuget/`, and
   `THIRD-PARTY-NOTICES.md` gives each new checksum. `NativePackageGateTests` reads the new file name,
   and a new test fails when a native binary of the package holds `/home/runner/` or `/Users/runner/`.
8. **The source commit (R-0018).** The pin job resolves the Manifold reference to its commit, the build
   jobs check out that commit, and the pack step records it in the nuspec with the Manifold
   repository. A gate test holds the nuspec commit equal to the commit of `THIRD-PARTY-NOTICES.md`,
   section 1.1. Register entry R-0018 asks for exactly this rebuild, so one rebuild closes two entries.
7. **No workaround.** `TryLoadNative` loses the load of the dependency, and its comment and R-0035
   close. The pipeline then passes on the three runners with `CI` equal to `true`.

## Scope (out)

- No new platform. Register entry R-0020 holds the platform list.
- No new Manifold version. The source stays `v3.5.2`.
- No publication to a feed. TASK-0012 §6 keeps the step disabled.

## Acceptance criteria

- [ ] The build job fails on the package of 2026-09: an injection that removes the flags of item 1
      gives a red build job that names the path of the build machine.
- [ ] A push of `manifold-ref.txt` starts the workflow, and the package arrives on its branch with no
      manual step.
- [ ] The new gate test fails on the package of 2026-09 and passes on the new package.
- [ ] With the workaround removed, the pipeline passes on `ubuntu-latest`, `windows-latest` and
      `macos-latest`, and no native test skips.
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

- **Question Q1 for the owner, before scope item 4.** May the pack job of `build-manifold-native.yml`
  have `contents: write`, to push the package to a branch `native-package/<version>`? Recommendation:
  yes. It removes the last manual step of a rebuild. The job runs only the packaging project of this
  repository and `dotnet pack`, and the third-party source compiles in jobs that keep `contents: read`.
  If the answer is no, the owner downloads the artifact by hand, and scope item 4 is out.
- **The order.** Commit the workflow change and the pin file first. The push starts the build. Then
  fetch the branch of the package, copy it to `nuget/`, and record the checksums. Remove the workaround
  last, so that the pipeline proves the package alone.
- **The cache.** The new version `3.5.2.1` avoids the old extracted package in
  `~/.nuget/packages/engine.geometry.manifold.native/3.5.2/` on each computer.
- **The marker gate.** `nuget.config` says "interim bootstrap" with R-0007. Do not change it here.

## Progress

- 2026-10-06: the task is open, prepared on the request of the owner. Question Q1 waits for an answer.
- 2026-10-06: the owner answered yes to question Q1: the pack job may push the package to a branch.
- 2026-10-06: the first form of the workflow: the pin file, a push trigger on the pin file and on the
  workflow, a job that reads the pin, the check of scope item 2, and the push of the package to its
  branch. The flags of scope item 1 are absent on purpose, so that the check must fail on Linux and
  macOS on this push.
- 2026-10-06: run 37528596155 of the first form started on the push, by itself. The check failed on
  Linux and on macOS, Windows built, and the pack job did not run. The public annotation said only
  "exit code 1", so the check now writes each failure as an `::error` line. The second form adds the
  flags of scope item 1.
- 2026-10-06: a correction. The line above says that the check writes each failure as an `::error`
  line. Commit `fc0102e` did not hold that change: the script that made it failed, and the next
  command of the shell ran anyway. This commit adds the `::error` lines.
- 2026-10-06: run 37529057135 of the flags commit failed, and the API limit of 60 requests an hour hid
  the failed job until 21:15 UTC. The check had a defect of its own: with `pipefail`, a pipe from
  `readelf`, `ldd` or `otool` into `grep -q` can break and read as a failure. Each tool now writes into
  a variable first. The first run is not affected: its failure came from the build path, which a
  check with no pipe found.
- 2026-10-06: scope item 8 joins the task. Register entry R-0018 asks the next build to record the
  Manifold commit and not a commit of this repository, and the rebuild of this task is that build.
  The tag `v3.5.2` resolves to `11235e6b8ebea2dbed8aec4285685aafd3d95667`, the commit of the notices.
- 2026-10-06: run 37529508917 of the flags commit failed its check on Linux and macOS, and the
  annotations now gave each reason in public: the `RUNPATH` and the `LC_RPATH` were still the build
  folder. Manifold's `CMakeLists.txt` at v3.5.2, lines 255-259, sets `CMAKE_BUILD_WITH_INSTALL_RPATH` to
  `FALSE` and `CMAKE_INSTALL_RPATH` to an absolute folder as normal variables, which hide the flags of
  the same name. The configure step now gives `CMAKE_BUILD_RPATH_USE_ORIGIN` and `CMAKE_BUILD_RPATH`,
  which Manifold does not set.
- 2026-10-06: run 37532724996: Linux passed each check, and macOS still held the build folder, with
  `@loader_path` now present. `CMAKE_BUILD_RPATH_USE_ORIGIN` does not act on macOS, so a step after the
  staging removes each absolute `LC_RPATH` with `install_name_tool` and signs the library again.
- 2026-10-06: run 37534598105 passed on the three platforms, and the pack job pushed the package to
  `native-package/3.5.2.1`; git fetched it with no sign-in. Its nuspec records the Manifold commit and
  address, and also `branch="refs/heads/native-rebuild-task-2026-10-06"`, a branch of this repository.
  No computer has restored 3.5.2.1 yet, so the pack step now gives an empty `RepositoryBranch`, and
  the same version is built again.
- 2026-10-07: run 37536428788 passed, and its nuspec still named the branch. A local pack of the
  packaging project, with one dummy file, reproduced it in a minute: an empty `RepositoryBranch` does
  not stop the SDK, which asks git for the branch. `-p:EnableSourceControlManagerQueries=false` gives a
  repository element with the Manifold address and commit only. The pack step uses it now.
- 2026-10-07: run 37538228336 passed, and git fetched the package from its branch. Its nuspec records
  the Manifold address and commit `11235e6b…` and no branch. It replaces 3.5.2 in `nuget/`, the project
  file references 3.5.2.1, and `THIRD-PARTY-NOTICES.md` gives its checksums; on macOS each copy is signed
  under its own name, so the notices give one row for each distinct file. The two new gate tests, which
  failed on 3.5.2 (the build path in six files, and the commit `718eab0`), pass. 260 tests pass, and the
  test output uses the new `manifoldc.dll`.
- 2026-10-07: scope item 7. `TryLoadNative`, the resolver and the load of the dependency are gone, and
  `IsNativeAvailable` loads the library by name again. The pipeline on Linux and macOS, where `CI` is
  `true` and no native test skips, is the proof for the package alone.
