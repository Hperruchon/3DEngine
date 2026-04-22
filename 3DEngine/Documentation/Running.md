# Running 3DEngine Desktop

## Purpose
This guide covers the native desktop host located in the `3DEngine` project.

## Requirements
- .NET SDK `10.0.200-preview.0.26103.119` as pinned by the root `global.json`
- A Vulkan-capable machine and drivers
- Native SDL runtime assets restored through NuGet packages during build

## Build
```powershell
dotnet build .\3DEngine\3DEngine.csproj
```

## Build The Full Solution
```powershell
dotnet build .\3DEngine.sln -m:1
```

## Run
```powershell
dotnet run --project .\3DEngine\3DEngine.csproj
```

## What To Expect
- A lot of weird code to generate a 3D engine

## Useful Notes
- This host currently consumes `Vortice.Vulkan.SampleFramework` while the native runtime is being separated from sample code.
- With the current .NET 10 preview SDK, the solution build is most reliable in single-node mode via `-m:1`.
- If Vulkan initialization fails, check GPU driver support and the presence of the Vulkan runtime on the machine.
