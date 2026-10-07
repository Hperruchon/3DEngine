---
id: 0036
title: A host refuses to start without the native backend, and a native test cannot skip in the pipeline
status: Done
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
    - Engine.Tests/Governance/DiagnosticsReserveGateTests.cs
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
    - docs/register.md
    - docs/roadmap.md
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

- [x] With the native library hidden from the output folder, `Engine.Cli` and `Engine.Api.Http` each
      stop with the message of scope item 1 and a non-zero exit code.
- [x] With `CI=true` and the library hidden, a native test fails. Verified by injection, and the
      Outcome block records the result.
- [x] `GET /schema/backend` gives the name and the version of the active backend.
- [x] The smoke test of scope item 5 passes on each runner.
- [x] The refusal gives `E-GEOM-BACKEND-INIT`, and `DiagnosticsReserveGateTests` passes with the
      code in the raised list.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

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

## Outcome

Status: Done · v0.44 · the commit that carries this block.

With `CI=true` and the native library hidden from the output folder of the tests, 7 of the 13 native
tests failed and none skipped. Before the change all 13 skipped, and the run passed.

## Method

**Mechanical.** The option records, the exit code 3 in the usage text, the endpoint, the version
constant, the move of the code in `docs/diagnostics.md`, the budget of the reserve, and the comments.

**Judgement.** A test selects the managed backend with an option record and not with a setting or an
argument, so that a person cannot select it. The HTTP host builds its engine in `Program.cs` at the
start, because the host built it at the first request, and a host that waits for a request does not
"stop before its first command". Both hosts use exit code 3, so that a script reads one meaning. The
version comes from a constant that a test holds equal to the pinned package, because manifoldc exports no version
function: a search of the raw bytes of the win-x64 library on 2026-10-06 found 294 `manifold_`
symbols and none with "version", while the controls `manifold_cube` and `manifold_volume` were found. The attribute reads `CI` and not a variable of its own,
because GitHub sets `CI` on each runner and a person sets it nowhere.

**Judgement, later.** The first run showed that the native library never loaded on Linux and macOS.
The backend loads the dependency first by its full path, in place of a rebuild of the package, because
the rebuild needs a manual run of the build workflow with a sign-in, and this task excludes a native
build. R-0035 holds the rebuild.

**Weakest.** The workaround relies on the loader of each system: Linux finds a loaded library by its
name, and macOS by its install name. The run of 2026-10-06 shows both, and a change of the runner image
can break it before R-0035 is closed. The test of the HTTP stop builds `EngineHost` directly, and only
the run on the real program shows the exit code 3. (Corrected 2026-10-08, TASK-0050: this line said
that `WebApplicationFactory` stops `Program.cs` at its build. That is false. An injection after the
start check failed 46 of 59 HTTP tests, so the factory runs the check in each test.) Six of the 13 native tests pass with the library hidden, because they do not
call into it; they prove nothing about the library. A runner on which `CI` is not `true` would skip
again.

## Progress

- 2026-10-06: the state before the change, with the native library hidden from the three output
  folders. `engine apply CreateBox` gave `Applied` and exit code 0 with the managed backend and no
  message. `engine-api-http` started and listened. With `CI=true` the 13 native tests skipped, and
  the run passed. The gate file `DiagnosticsReserveGateTests.cs` joins the write set by its exact
  name (TASK-0047), with `docs/register.md` and `docs/roadmap.md` for the close.
- 2026-10-06: scope items 4 and 5. With `CI=true` the attribute no longer skips: with the library
  hidden, 7 of the 13 native tests failed and none skipped. A smoke test applies `CreateBox` two
  times, `Translate` and `Subtract` on the native backend through a session.
- 2026-10-06: scope items 1, 2, 3 and 6. Each host requires the native backend. With the library
  hidden, `engine apply CreateBox` writes `E-GEOM-BACKEND-INIT` and a sentence that names the library
  `manifoldc`, Manifold 3.5.2 and the platform `win-x64`, and stops with exit code 3; `engine-api-http`
  writes the same and stops with exit code 3 before it listens. A test selects the managed backend
  with an option record (`BackendOptions`, `HostBackendOptions`), and no argument does.
  `GET /schema/backend` gives `manifold` and `3.5.2`, and a test holds the version equal to the
  pinned package. The code moved from the reserve to the raised codes, and the budget of the reserve
  gate is one: with the reserved row put back, the gate failed with "2 and the budget is 1". The three
  places that read ADR-0014 as a rule for a fallback agree with the code. The comment of
  `EngineHost.cs` that cited the removed clamp of v0.17 is corrected too (a part of finding C1).
- 2026-10-06: the first pipeline run, 37526197179, was green on Windows and red on Linux and macOS:
  the native library did not load there. The hosts stopped with `E-GEOM-BACKEND-INIT`, and the native
  tests failed. The cause is in the package: `libmanifoldc` stores the build folder of the runner as
  the search path of `libmanifold` (`/home/runner/work/3DEngine/3DEngine/build/src` on Linux, the
  same under `/Users/runner` on macOS). So the native backend never loaded on those runners, and the
  skip hid it. The backend now loads the dependency first by its full path, and register entry R-0035
  holds the rebuild of the package, which is the correct end state.
- 2026-10-06: the run after the loader change, 37527244390, was green on the three runners with `CI`
  set to `true`, so the native tests ran on Linux and macOS for the first time.
- 2026-10-06: closed. R-0027 has a progress line, R-0035 is new, the roadmap lists P0.15 as shipped,
  and ledger entry v0.44 records the work.
