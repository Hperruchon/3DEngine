---
id: 0072
title: The drawing code of a host is a library with no surface
status: Accepted
topic: Rendering, clients, platform
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0027']
amended-by: []
affects:
  - 3DEngine.App/**
  - 3DEngine/HostedSession.cs
enforced-by: Engine.Tests/Governance/DependencyDirectionGateTests.cs
---

# ADR-0072 — The drawing code of a host is a library with no surface

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14e of ADR-0061 item 9 is the Android host. ADR-0061 item 4 mounts no HTTP surface and no WebSocket surface on
the phone: the person sends the same commands through the `CommandBus` of the same process. Until this record the
drawing code, the user interface and the actions were in the program `3DEngine`. That program takes the framework
`Microsoft.AspNetCore.App` for the surface, and the drawing code reached the session through `EngineHost` of the
surface library `Engine.Api.Http`, which takes the same framework.

A probe of 2026-11-06 (TASK-0222) built two Android projects. A library that references `Engine.Api.Http` built. A
program that references it stopped with NETSDK1082: no runtime pack of `Microsoft.AspNetCore.App` exists for
`android-arm64`. A framework reference flows through each project reference, so an Android program can reference
neither the surface library nor the program `3DEngine`. The owner ordered R14e under the standing order of
2026-11-05, which answers each question with the recommended option. This record is Accepted under that order; the
owner checks it on 2026-11-08.

The options:

- **A.** A library `3DEngine.App` holds the drawing code, the user interface and the actions. It reaches the session
  through an interface of its own, and the desktop adapts `EngineHost` to it. The Android program composes its own
  session.
- **B.** `EngineHost`, `DocumentFolder` and the broadcaster of the events move from the surface library into a new
  library with no framework of ASP.NET Core, and the drawing code moves into a library that references it.
- **C.** `Engine.Api.Http` builds a second target for Android, with no endpoint and no framework reference.

A changes the render side only. B changes the surface library of ADR-0019 item 3 and moves internal types of the
WebSocket surface across two assemblies. C puts two builds with conditional items in one project, and the gates of
the dependency graph read items, not conditions. **Recommended: A.**

## Decision

1. **The library.** `3DEngine.App` holds the scene renderer, the projection of the session, the user interface, the
   overlays and the actions, which were in `3DEngine` until TASK-0222. It references `Engine.Contracts`,
   `Engine.Core`, `3DEngine.Core` and `3DEngine.Vulkan` only, and no file that MSBuild reads for it names a
   `FrameworkReference`. `DependencyDirectionGateTests.The_Application_References_No_Surface` holds the rule. A host
   that draws references the library; the library references no host.
2. **The session.** `ISessionHost` gives the application the `DocumentSession`, the name of the document file and a
   subscription to the committed events, as `EngineHost` gives them (ADR-0024 item 1). The desktop gives
   `HostedSession`, an adapter of its `EngineHost`, so the person and an agent still work on one document
   (ADR-0019). The phone gives `LocalSessionHost` (R14e2, TASK-0226): a `DocumentSession` on the native backend with
   a fan-out of the committed events, `FanOutSink`, as `EngineHost` gives them, and no file.
3. **The files.** `IDocumentFiles` gives the actions Save, Open and Export of the panel a path for a plain name and
   the open of a file. The desktop gives `DesktopDocument`, with the rules of the document folder (ADR-0030 item 2).
   The phone gives the picker of Android in a later slice of R14e. Two actions of the panel still use a path of the
   file system: the placement of an image reads the path that the person types, and the STL export writes on the path
   that `Resolve` gives (finding V95 of the codebase review of 2026-11-06 (b)). A picker gives a stream, so the slice of
   the picker decides how the phone reads an image and writes an export: a stream through `IDocumentFiles`, or a path
   in the private storage of the program.
4. **The user interface.** It moves with the application: `3DEngine.App/Ui/` holds the only files that name the
   namespace `ImGuiNET`, and `3DEngine.App/3DEngine.App.csproj` holds the only pin of `ImGui.NET`. This amends
   ADR-0027 items 1, 8 and 9, which name `3DEngine/Ui/` and `3DEngine/3DEngine.csproj`.
5. **Internal types.** The library gives its internal types to the desktop program and to the image test through
   `InternalsVisibleTo`, as the program gave them to the image test before. The moved files keep the namespace
   `ThreeDEngine.Desktop`; a new name is a later change with no cost to the log or to a document.
6. **No change of behaviour.** The desktop draws and acts as before. The image tests and the tests of the desktop
   session pass with the same count.
7. **The platform.** Each project that the program of the phone reaches names `<SupportedPlatform Include="android" />`
   (R14f2, TASK-0229), so the analyzer of platform compatibility (CA1416) refuses an API that Android does not have, and
   the rule of zero warnings stops the build. `DependencyDirectionGateTests.Each_Project_That_The_Phone_Reaches_Supports_Android`
   holds the item. The item changes no public type of `Engine.Contracts`.

## Consequences

**Good:** an Android program can reference the whole application of the desktop and give it a session of its own. A
gate refuses a reference to the surface or to ASP.NET Core in the application, so the phone cannot lose that path by
accident.

**Bad:** the desktop is two projects. Two programs reference the application, `3DEngine` and `3DEngine.Android`; four
test files of `Engine.Tests` and the solution name it, and the `affects` fields of other ADRs name its paths. The namespace `ThreeDEngine.Desktop` holds code that is not only of the
desktop.

**Next:** R14e2, the Android program: a `net10.0-android` project, an activity that subclasses `SDLActivity`, a session
of its own, and the APK.

## History

- 2026-11-06 · TASK-0222 · The record, under the standing order of 2026-11-05.
- 2026-11-06 · TASK-0226 · Item 2: the session of the phone is `LocalSessionHost` of the library.
- 2026-11-06 · TASK-0229 · Item 7: the platform Android in each project of the APK; the third run of the emulator ended on
  `Console.ForegroundColor`.
- 2026-11-06 · TASK-0225 · Item 3 names the two actions that use a path (finding V95), and the count of the
  consequences is true (finding V96): until TASK-0225 it said "four test projects".
- 2026-11-06 · TASK-0232 · The consequences name the two programs and give no count of ADRs (finding V103): the
  correction of V96 said "the program" and "fifteen ADRs", and the Android project already referenced the application.
- 2026-11-07 · TASK-0263 · Item 7: "the rule of zero warnings stops the build" was false for `3DEngine.App`,
  `3DEngine.Core` and `3DEngine.Vulkan`, which did not set `TreatWarningsAsErrors` (finding T166 of the codebase review
  of 2026-11-07). Each project that the phone reaches sets it now, and the gate and the platform step require it.
- 2026-11-07 · TASK-0272 · Item 7: the host of the phone makes CA1416 an error with `WarningsAsErrors`, because the
  Android SDK can give warnings that are not ours; the step of the platforms reads it (finding T168 of the codebase review
  of 2026-11-07 (b)).
