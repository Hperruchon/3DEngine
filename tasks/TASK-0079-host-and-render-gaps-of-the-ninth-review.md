---
id: 0079
title: A stop waits for a running command, and the present path is checked for hazards
status: Active
phase: P0.31
opened: 2026-10-17
depends-on: [0040]
governed-by: [0004, 0005, 0007, 0009, 0010, 0013, 0016, 0017, 0018, 0019, 0020, 0021, 0022, 0023, 0024, 0025, 0026]
writes:
  create:
    - tasks/TASK-0079-host-and-render-gaps-of-the-ninth-review.md
  modify:
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Core/DocumentSession.cs
    - Engine.Tests/Http/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
    - docs/adr/**
---

# TASK-0079 — A stop waits for a running command, and the present path is checked for hazards

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-17 found gaps in the hosts and in the render side. Each finding gives its
evidence in `docs/reviews/2026-10-17-codebase-review.md`.

- E66 (medium): a stop frees the geometry backend while a command still runs.
- E67 to E71 (low): the report of a removed action inside the commit, a crash of the server for a
  configuration file, a broken comment line, the comment of the stop limit, and two start paths with no
  disposal.
- V16 to V24 (low): the present path under synchronization validation, the message of a failed image test,
  a failed remake of the swapchain, a failed submit, the composite alpha, the messenger with no debug
  utils, the values of the camera, a minimised window, and the comment of the push constant test.
- V25 goes with decision D7 of question Q2 of the review (the version of SDL), and not with this task.

## Goal

A stop of a host never frees the backend under a running command, each start path ends in a documented
code with the app disposed, and the present path of the desktop has run once under synchronization
validation.

## Scope (in)

1. E66: before the backend goes, the session refuses new commands and waits until its serial section is
   free; a test with a slow backend holds it.
2. E67, E68, E70, E71: the report cannot throw or block into a commit; the server gives code 5 for a
   configuration file that it cannot read; the comment gives the measured wait; each early return disposes
   the app. E69: the broken comment line goes.
3. V16: a switch runs the desktop with synchronization validation; the owner runs it once with a minimise,
   a restore and the demonstration.
4. V17 to V24: the image test gives the text of each message; a failed remake keeps the old state; a
   failed submit is reported and each wait has a limit; the composite alpha comes from the surface; the
   messenger needs debug utils; the camera refuses a distance of 0 and NaN and reports a box that the far
   plane cannot hold; a minimised window waits for an event; the comment of the push constant test agrees
   with the test.

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A run of the desktop with synchronization validation, a minimise, a restore and the demonstration
      gives no validation message. The owner accepts the picture and the log.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The size.** About 2 evenings.

## Progress

- 2026-10-27: opened after TASK-0040 (v0.84). Since TASK-0078 a task that adds tests sets the exact count
  in `eng/test-floor.txt`, so the write set names that file, and the forbid of `eng/**` becomes the two
  programs of `eng/`, as in TASK-0082.
- 2026-10-27: E66. `DocumentSession.Close` refuses each later call and waits until the serial section is
  free, and `EngineHost.Dispose` calls it before it frees the backend. `EngineHostStopTests` holds it with a
  backend whose `CreateBox` waits: a disposal with no close failed the test ("The backend was freed while
  the command still ran in it"). Close has no time limit, because a commit does not stop.
- 2026-10-27: E67 and E69. The report of a removed action runs on the thread pool, out of the commit, and
  a report that throws is lost. Two new tests: a report that throws, and a report that blocks. A report in
  the commit failed both. The first version of the blocking test passed under that injection, because a
  commit with no real wait ends before `Apply` returns its task; the test now starts `Apply` on the
  thread pool. The broken comment line of the broadcaster goes.
- 2026-10-27: E68, E70 and E71. `engine-api-http` gives code 5 for a configuration file that it cannot
  read; a process test holds it, and the old program failed it with an unhandled exception and a crash
  code. The comment of the stop limit gives the measured wait: three runs gave 3.01 s to 3.02 s, as the
  review measured. Each early return of the two hosts disposes the app. E71 has no test: the review found
  it by reading, and the two paths have no failure that a test can cause on purpose.
- 2026-10-27: V17 and V21. The device keeps the text of the first 100 messages of the validation layer,
  and the image test gives each one in its assertion. An injected viewport of width 0 failed the test
  with the text of VUID-VkViewport-width-01770; before, the assertion gave a count only. The device
  reads debug utils from the implementation and from the enabled layers, and it makes the messenger only
  when debug utils is present. V21 has no test: each computer and runner of the project has debug utils.
