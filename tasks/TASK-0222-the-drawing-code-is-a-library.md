---
id: 0222
title: The drawing code of the desktop becomes a library that an Android program can reference
status: Done
phase: R14e1
opened: 2026-11-06
depends-on: [0196, 0198]
governed-by: [0007, 0017, 0019, 0025, 0027, 0029, 0030, 0031, 0032, 0034, 0038, 0041, 0043, 0046, 0047, 0053, 0056, 0061, 0072]
writes:
  create:
    - tasks/TASK-0222-the-drawing-code-is-a-library.md
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - 3DEngine.App/**
    - 3DEngine.App/3DEngine.App.csproj
    - 3DEngine/HostedSession.cs
  modify:
    - 3DEngine/**
    - 3DEngine/3DEngine.csproj
    - 3DEngine.sln
    - 3DEngine.ImageTests/**
    - 3DEngine.ImageTests/3DEngine.ImageTests.csproj
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Governance/BuiltAssemblyReferenceTests.cs
    - docs/adr/0007-ui-ephemeral-state-boundary.md
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/adr/0027-text-and-panels.md
    - docs/adr/0029-selection-and-hide-gestures.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - docs/adr/0034-sketch-tools.md
    - docs/adr/0038-translate-gizmo.md
    - docs/adr/0041-snap-of-a-sketch-tap.md
    - docs/adr/0043-textured-quad.md
    - docs/adr/0046-image-in-the-desktop.md
    - docs/adr/0047-contour-proposal.md
    - docs/adr/0053-desktop-sketch-on-a-face.md
    - docs/adr/0056-dimension-overlay.md
    - docs/adr/README.md
    - tasks/TASK-0113-translate-gizmo.md
    - tasks/TASK-0153-image-in-the-desktop.md
    - tasks/TASK-0177-desktop-and-gate-forms-of-the-twentieth-review.md
    - tasks/TASK-0178-dimension-overlay.md
    - Engine.Tests/EventSinkContractTests.cs
    - docs/INDEX.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0222 — The drawing code of the desktop becomes a library that an Android program can reference

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14e of the Android order of 2026-11-05 is the Android host (ADR-0061 item 9). The drawing code, the user
interface and the actions of the desktop are in the program `3DEngine`, which takes the framework
`Microsoft.AspNetCore.App` for the surface, and the drawing code reaches the session through `EngineHost` of the
surface library. A probe of 2026-11-06 built an Android program that references `Engine.Api.Http`: the build stopped
with NETSDK1082, because no runtime pack of ASP.NET Core exists for `android-arm64`. ADR-0061 item 4 mounts no surface
on the phone. So the Android host can reach the drawing code only when it is in a library with no surface. This
first slice of R14e makes that library and changes no behaviour.

## Goal

The library `3DEngine.App` holds the drawing code, the user interface and the actions, references no surface and no
ASP.NET Core, and the desktop behaves as before.

## Scope (in)

1. ADR-0072 with the options and the recommended one, answered by the standing order of 2026-11-05.
2. The project `3DEngine.App`, the move of each file of the drawing code, `ISessionHost` and `IDocumentFiles`, and
   the adapter of the desktop.
3. The gates: the class of the new project, a rule that it references no surface and no ASP.NET Core, the folder of
   the user interface, and the list of the packages of each project of the desktop.

## Scope (out)

- The Android program, the APK and each behaviour of the phone (R14e2 and later slices).
- A new name of the namespace `ThreeDEngine.Desktop` of the moved files.

## Acceptance criteria

- [x] Each test passes, among them the image tests, with the same count.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] An injection of a reference to `Engine.Api.Http` in `3DEngine.App` fails the new rule.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14e1-drawing-library) · v0.207

## Method

Mechanical: the move of the files with git, and the adapter of `EngineHost`.
Judgement:  the boundary of the library: two interfaces of the host, and not a move of `EngineHost`.
Weakest:    no Android program references the library yet, so the claim that one can rests on the rule and on the
            probe of a library; R14e2 builds the first APK with it.

## Progress

- 2026-11-06: opened with the probe of NETSDK1082.
- 2026-11-06: ADR-0072 with options A, B and C; the standing order of 2026-11-05 chose A. The drawing code, the user
  interface and the actions moved with git into `3DEngine.App`; `ISessionHost` and `IDocumentFiles`, the adapter
  `HostedSession`, and `DesktopDocument` as the files of the desktop. The gates: the class `Applications`, the rule
  `The_Application_References_No_Surface`, the folder of the user interface, the sinks of both assemblies, the built
  assembly of the application, and the list of the packages. Fifteen ADRs and four Done tasks named a moved path; each
  one names the new path, and each task says so in a progress line. `docs/INDEX.md` named `Alimer.Bindings.SDL` as a
  pin of the Vulkan layer, which was false since R14c. A reference to `Engine.Api.Http` and a framework reference of
  ASP.NET Core in the application each fail the new rule. The image tests pass with the same count, 200.
- 2026-11-06: closed at v0.207. The row R14e stays open.
