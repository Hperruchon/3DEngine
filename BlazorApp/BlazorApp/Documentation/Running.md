# Running BlazorApp

## Purpose
This guide covers the web/editor host located in the `BlazorApp` project.

## Requirements
- .NET SDK `10.0.200-preview.0.26103.119` as pinned by the root `global.json`
- ASP.NET Core support from the installed .NET 10 SDK

## Build
```powershell
dotnet build .\BlazorApp\BlazorApp\BlazorApp.csproj
```

## Build The Full Solution
```powershell
dotnet build .\3DEngine.sln -m:1
```

## Run
```powershell
dotnet run --project .\BlazorApp\BlazorApp\BlazorApp.csproj
```

## What To Expect
- ASP.NET Core starts the web host.
- The browser app exposes the workspace dashboard, project responsibilities, and architecture view.
- Shared sample scene data is loaded from `3DEngine.Core`.

## Useful Notes
- This host is intended for editor and tooling workflows, not native rendering.
- `BlazorApp.Client` is implemented as a server-hosted Razor UI project, not as a WebAssembly renderer.
- With the current .NET 10 preview SDK, the solution build is most reliable in single-node mode via `-m:1`.
