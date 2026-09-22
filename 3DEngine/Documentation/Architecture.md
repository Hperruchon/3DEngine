# 3DEngine Architecture

## Purpose
`3DEngine` is the native desktop host for the engine. It owns the executable entry point, the SDL
event loop, and the runtime loop that drives a scene using contracts defined in `3DEngine.Core`.
It draws through `3DEngine.Vulkan`, the first-party Vulkan layer (ADR-0017).

## What Belongs Here
- Application bootstrap and startup flow
- The event loop and the handling of each window event
- The projection of engine events into `3DEngine.Core` state (ADR-0009)
- Desktop-only implementations of shared engine contracts

## What Does Not Belong Here
- Shared scene/domain model definitions. These live in `3DEngine.Core`.
- The Vulkan device, the swapchain and the window surface. These live in `3DEngine.Vulkan`.
- Blazor UI components or ASP.NET Core setup
- Tooling-only editor view models that are not needed by the native runtime

## Dependency Rules
- `3DEngine` may depend on `3DEngine.Core` and `3DEngine.Vulkan`.
- `3DEngine` names no Vulkan package and no SDL package. It receives both bindings through
  `3DEngine.Vulkan`, which holds the only pin of each one.
- No other production project should depend on `3DEngine`.
- `Engine.Tests/Governance/DependencyDirectionGateTests.cs` checks each rule above.
