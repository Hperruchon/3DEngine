# 3DEngine.Core Architecture

## Purpose
`3DEngine.Core` is the shared engine/domain library for the solution. It defines the contracts, scene model, and resource descriptors that can be consumed by both the desktop runtime and the Blazor tooling without bringing native rendering dependencies into shared code.

## What Belongs Here
- Engine lifecycle contracts such as `IThreeDEngine`
- Scene/domain types such as `Scene`, `Entity`, `Transform`, `Camera`, and `Light`
- Platform-neutral resource descriptors for meshes, materials, and textures
- Shared services that are safe to use from multiple hosts, such as scene factories, loaders, or in-memory catalogs

## What Does Not Belong Here
- SDL window management
- Vulkan device setup or render loop code
- ASP.NET Core host configuration
- Blazor-specific components, pages, or UI state

## Dependency Rules
- `3DEngine.Core` may depend on the .NET base class library and other platform-neutral packages.
- `3DEngine.Core` must not depend on `3DEngine`, `BlazorApp`, or `BlazorApp.Client`.
- `3DEngine`, `BlazorApp`, and `BlazorApp.Client` are expected to depend on this project when they need shared engine contracts or data models.
