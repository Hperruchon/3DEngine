# BlazorApp.Client Architecture

## Purpose
`BlazorApp.Client` contains the interactive UI for the web/editor side of the solution. 
It is a Razor component library hosted by `BlazorApp`, and it presents shared engine data,
architecture information, and future scene tooling without taking a dependency on the native desktop renderer.

## What Belongs Here
- Blazor pages and components
- Client-side navigation and presentation logic
- UI-facing services that transform shared core data into dashboards or editor views

## What Does Not Belong Here
- SDL or Vulkan code
- ASP.NET Core server startup
- Ownership of engine contracts or scene domain models

## Dependency Rules
- `BlazorApp.Client` may depend on `3DEngine.Core`, ASP.NET Core framework references, and Blazor packages.
- `BlazorApp.Client` is hosted by `BlazorApp`.
- `BlazorApp.Client` must not depend on `3DEngine`.
