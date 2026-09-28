---
id: 0036
title: A host refuses to start without the native backend, and a native test cannot skip in the pipeline
status: Ready
phase: P0.15
opened: 2026-09-25
depends-on: [0034]
governed-by: [0002, 0004, 0011, 0013, 0014, 0016, 0018, 0019]
writes:
  create:
    - tasks/TASK-0036-honest-geometry-backend.md
    - Engine.Api.Http/Endpoints/SchemaBackendEndpoint.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Program.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/**
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Api.Http/WebSockets/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0036 — A host refuses to start without the native backend, and a native test cannot skip in the pipeline

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E3 of `docs/reviews/2026-09-23-codebase-review.md`: each host takes the managed backend with
no message when the native library does not load (`Engine.Cli/Cli.cs:192-194`,
`Engine.Api.Http/EngineHost.cs:39-41`). The managed backend holds boxes only. It cannot move or cut a
solid, so the product starts and then refuses the first demonstration. Anti-objective 9 refuses "a
silent fallback when a capability is absent".

Finding T1 of the same review: `NativeManifoldFactAttribute`
(`Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs:10-15`) skips a test when the library does not
load. A runner that loses the library therefore reports green.

**ADR-0014 is not clear, and three places read it as a rule for a fallback.** ADR-0014 §4 calls the
managed backend "the deterministic default and replay backend". Its section "Consequences" says that
each host constructs the Manifold backend, and it names `E-GEOM-BACKEND-INIT` for "native lib not
found". It does not say what a host does when the library does not load. Three places read the word
"default" as a fallback rule:

- `Engine.Cli/Cli.cs:188-191`: "so the CLI runs on any platform (ADR-0014 section 4)".
- `Engine.Core/Hosting/EngineHosting.cs`: "ADR-0014 section 4 gives each host two implementations to
  choose between".
- `docs/diagnostics.md:26` and `:55` reserve `E-GEOM-BACKEND-INIT`, because "the host's
  native-availability fallback (ADR-0014 §4) selects the managed stub instead".

Anti-objective 9 is clear: it refuses "a silent fallback when a capability is absent". The owner
decided on 2026-09-25 to turn this finding into a task (item 9 of the decisions of the owner in
`docs/reviews/2026-09-23-architecture-challenge.md`). This task follows anti-objective 9, raises
`E-GEOM-BACKEND-INIT`, and corrects the three places.

GitHub Actions sets the variable `CI` to `true` on each runner. The GitHub reference for variables
says "Always set to true" (verified 2026-09-23).

## Goal

A host with no native backend stops with a clear message, and a green pipeline proves that the
native tests ran.

## Scope (in)

1. **Fail fast.** `Engine.Cli` and `Engine.Api.Http` require native Manifold. When the library does
   not load, each one stops before its first command, and the message names the platform and the
   library that it did not find.
2. **An explicit test option.** The managed backend stays in `Engine.Core` for tests and for the
   canonical replay gate (ADR-0014). A host uses it only when a test sets an explicit option. The
   option is not in the usage text for a person.
3. **The backend in `/schema`.** `GET /schema/backend` gives the name and the version of the active
   backend, for example `manifold` and `3.5.2`. The host reads them from the class that it composed,
   so `Engine.Contracts` does not change.
4. **No skip in the pipeline.** When `CI` is `true`, `NativeManifoldFactAttribute` does not skip. A
   test that needs the library then fails when the library does not load.
5. **A smoke test** for `Translate` and `Subtract` on the native backend, through the command bus.
6. **The three places** of the context agree with the code. `E-GEOM-BACKEND-INIT` moves from the
   reserved codes to the raised codes in `docs/diagnostics.md`. The code keeps its name and its
   meaning.

## Scope (out)

- No new platform and no native build. Register entry R-0020 holds the platform question.
- No change to `BackendCapabilities` or to `TryGet`.
- No change to the canonical replay gate, which stays on the managed backend (ADR-0014). Register
  entry R-0023 holds the comparison between platforms.

## Acceptance criteria

- [ ] With the native library hidden from the output folder, `Engine.Cli` and `Engine.Api.Http` each
      stop with the message of scope item 1 and a non-zero exit code.
- [ ] With `CI=true` and the library hidden, a native test fails. Verified by injection, and the
      Outcome block records the result.
- [ ] `GET /schema/backend` gives the name and the version of the active backend.
- [ ] The smoke test of scope item 5 passes on each runner.
- [ ] The refusal gives `E-GEOM-BACKEND-INIT`, and `DiagnosticsReserveGateTests` passes with the
      code in the raised list.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **Tests of the HTTP host.** Many tests start the host with `WebApplicationFactory`, and each one
  gets the backend that the host selects. A test that needs the managed backend sets the option of
  scope item 2. That is the reason for `Engine.Tests/**` in the write set.
- **A platform with no payload.** `win-arm64`, `linux-arm64` and `osx-x64` have no native package.
  After this task a host refuses to start there. That is the honest result until the platform study
  of R-0020 ends.
- **The reserve.** `docs/diagnostics.md` says that a later phase that raises a reserved code "must
  state that it does so". This task states it. The reserve then holds one code, so lower the budget
  in `DiagnosticsReserveGateTests` to one in the same commit.
