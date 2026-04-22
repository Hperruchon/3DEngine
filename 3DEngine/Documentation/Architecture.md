# 3DEngine Architecture

## Purpose
`3DEngine` is the native desktop host for the engine. It owns the executable entry point, the SDL window lifecycle, 
the Vulkan-backed renderer setup, and the runtime loop that drives a scene using contracts defined in `3DEngine.Core`.

## What Belongs Here
- Application bootstrap and startup flow
- Native window creation and event handling
- Vulkan and other native graphics integration
- Desktop-only implementations of shared engine contracts

## What Does Not Belong Here
- Shared scene/domain model definitions
- Blazor UI components or ASP.NET Core setup
- Tooling-only editor view models that are not needed by the native runtime

## Dependency Rules
- `3DEngine` may depend on `3DEngine.Core` and native rendering libraries.
- `3DEngine` may temporarily depend on the sample framework while the native runtime is being extracted.
- No other production project should depend on `3DEngine`.
