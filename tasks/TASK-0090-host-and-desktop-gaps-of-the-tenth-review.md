---
id: 0090
title: A name is a JSON string of well-formed text, the server disposes at each stop, and the desktop ends a text field at a click on the scene
status: Done
phase: P0.36
opened: 2026-10-28
depends-on: [0088]
governed-by: [0004, 0007, 0009, 0012, 0015, 0016, 0017, 0019, 0021, 0022, 0023, 0024, 0025, 0026, 0027, 0028, 0029, 0030]
writes:
  create:
    - tasks/TASK-0090-host-and-desktop-gaps-of-the-tenth-review.md
  modify:
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Core/Commands/BodyNames.cs
    - Engine.Api.Http/Endpoints/JsonParameters.cs
    - Engine.Api.Http.Server/Program.cs
    - Engine.Tests/Commands/**
    - Engine.Tests/Http/**
    - Engine.Tests/Hosting/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0028-body-name-and-visibility.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0090 — A name is a JSON string of well-formed text, the server disposes at each stop, and the desktop ends a text field at a click on the scene

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-28 found gaps in the hosts, the render side and the desktop. Each finding gives
its evidence in `docs/reviews/2026-10-28-codebase-review.md`.

- E72 (low): the binder accepts each kind of JSON value for a string.
- E73 (low): a name with a lone surrogate is accepted, and each JSON view changes it.
- E74 (low): `engine-api-http` does not dispose its app at a normal stop.
- V18 (low, in part): a retry after a failed remake passes a retired swapchain, and a throw leaks the depth image.
- V26 (medium): a click on the scene does not end a text field, and Rename can rename the wrong body.
- V27 (medium): each frame costs time in the square of the count of bodies.
- V28 (low): no test sees the highlight, and the vertex shader does not declare an invariant position.
- V30 (low): a pitch set to 90 degrees gives a view of NaN.

`docs/templates.md`, section 7, puts a medium or low finding into a task after the next product phase, so this
task comes after P8a.

## Goal

The text input of the engine holds the rule of a name, each stop of a host frees its backend by the rule of E66,
and the desktop sends the command that a person asks for.

## Scope (in)

1. E72: a field of type string accepts only a JSON string over HTTP; the command line keeps its text path.
2. E73: `BodyNames.Refusal` refuses a lone surrogate, as an amendment of ADR-0028, and a string that is not
   well-formed UTF-16 in a request gives `E-API-BAD-REQUEST`, not HTTP 500.
3. E74: the server disposes its app at a normal stop and at each exception of the composition, with a process test.
4. V18: a remake after a failed remake passes no old swapchain, and the swapchain frees its depth image on a throw.
5. V26: a press outside the panel ends the active text field, and the two probes of the review are tests.
6. V27: the visibility is kept by identifier, and the count runs only after a change.
7. V28: an image test that selects a body and counts the tinted pixels, and `invariant gl_Position` with a new
   lock of the SPIR-V.
8. V30: the camera refuses or clamps a pitch outside the range of `Orbit`.

## Scope (out)

- V29, the touch path on a real screen: the review carries it, as the owner chose on 2026-10-27.
- The gates (TASK-0088, TASK-0089).

## Acceptance criteria

- [x] Each form of the review fails a test before its correction, or an injection shows that the test can fail,
      except E74 and the free of the depth image of V18, which no test reaches (see Method).
- [x] The replay gate passes with the new rule of a name.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.93 · the commit that carries this block.

A click on the scene ends the text field of the panel, so Rename sends the command that a person asks for (V26),
and a frame reads the visibility of the bodies in constant time (V27). A string field takes only a well-formed JSON
string, and a name is well-formed UTF-16 (E72, E73). The server disposes its app at each stop (E74). A retry of a
remake gives no retired swapchain, a failed swapchain frees its depth image (V18), an image test sees the highlight
of the selection with an invariant position (V28), and the pitch of the camera is clamped (V30).

## Method

**Mechanical.** 19 new tests, with each theory case counted: 9 in `Engine.Tests`, 5 in `3DEngine.Tests` and 5 in
`3DEngine.ImageTests` (one image test runs on Ubuntu and on a computer with a GPU only). The two probes of V26
failed before the fix. Each other fix with a test failed it under an injection of its old rule. Run 40691527334
passed on the three runners with V26 to V30, E72, E74 and the HTTP part of E73.

**Judgement.** The focus of Dear ImGui drops in the frame after a press on the scene, and the keys of the same frame
go to the scene at once. The visibility lives in a small type, so a test reads it with no GPU. The reader of a JSON
request marks each value that is not a string, and the binder refuses it, so the command line keeps its text path.
The owner accepted the new rule of a name in ADR-0028 item 3 on 2026-10-28.

**Weakest.** E74 has no test: no test stops the server with a signal on each runner, and none makes its composition
throw. The free of the depth image of V18 has no test, because only a driver that fails reaches it. The highlight
test passed on the GPU of this computer before the invariant too.

## Notes for the implementer

- **The ADR.** E73 changes the rule of a name in ADR-0028, so the first commit amends ADR-0028. The public shape of
  `Engine.Contracts` does not change.
- **The governance.** `governed-by` must equal the ADRs whose `affects` intersect the write set; the gate
  `TaskGovernance` gives the list.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-28: opened by the codebase review of 2026-10-28 (TASK-0087).
- 2026-10-28: the task is active. After P8f (v0.92) the owner put P0.36 and then P0.35 before P8c, because V26 can
  put a wrong rename into the document file; the roadmap records the order.
- 2026-10-28: V26. A press that starts outside the panel while a text field is active drops the focus of Dear
  ImGui in the next frame, and the keys of the same frame go to the scene. The two probes of the review are tests,
  and both failed before the fix (the key went to the field; Rename gave the second body the text "X"). An
  injection that keeps the focus failed both.
- 2026-10-28: V27. `BodyVisibility` keeps the visibility of each body by the identifier of its mesh and keeps the
  count of the visible bodies that have a mesh until the next change; `SessionBodies` reads it for the count, the
  box and the pick. Two tests; an injection that counts at each read failed one. The full image tests, with the
  first demonstration on the GPU of this computer, pass.
- 2026-10-28: E72 and the HTTP part of E73. The reader of a JSON request gives an object, an array and a string
  that is not well-formed UTF-16 as values that are not strings, and the binder takes only a string for a field of
  type string; the command line gives each value as text. A name 42, true, an object, an array and "\ud800" each
  give HTTP 400 with `E-API-BAD-REQUEST` (five test cases); "\ud800" gave HTTP 500 before. The old rule of the
  binder failed the five cases, and a reader with no catch of a bad string failed one. The rule of a name in
  `BodyNames` (E73, ADR-0028 item 3) waits for the owner.
- 2026-10-28: V18 and V30. A remake after a failed remake gives the driver no old swapchain
  (`Presenter.OldSwapchain`), and the swapchain frees its depth image when its creation fails. The setter of
  `Camera.PitchDegrees` clamps to the range of `Orbit` and gives 0 for NaN. Five tests; the old rule of each failed
  its tests (1 and 3 cases). The free of the depth image has no test, because only a driver that fails reaches it.
- 2026-10-28: E74. `engine-api-http` disposes its app after a normal stop, so the session closes and waits for a
  running command before the backend goes (E66), and each other exception of the composition gives exit code 5
  with the app disposed. No test: no test stops the server with a signal on each runner, and none can make the
  composition throw; the full run of the process tests passes.
- 2026-10-28: V28. An image test renders the first demonstration without and with the selection and requires the
  tint on at least 99 % of the pixels of the body; a highlight with the depth test Never failed it (0 of 24,820
  pixels). `mesh.vert` declares `invariant gl_Position`, compiled with shaderc v2023.8, and the lock changes for
  `mesh.vert` only. The test passed on the GPU of this computer before the invariant too, so the invariant is a
  rule for other drivers; lavapipe runs the test in the pipeline.
- 2026-10-28: E73, the rule of a name. The owner chose on 2026-10-28 that a name is well-formed UTF-16: ADR-0028 item
  3 and `BodyNames.Refusal` refuse a lone surrogate with `E-VAL-BODY-NAME-INVALID`, and a pair stays valid. Four test
  cases, which build each name in code because the data of a theory changes a lone surrogate; the old rule failed the
  three cases of a lone surrogate. Closed after a clean build and a full local run of 738 tests (591 in
  `Engine.Tests`, 94 in `3DEngine.Tests`, 53 in `3DEngine.ImageTests`).
