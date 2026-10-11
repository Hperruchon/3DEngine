# Running 3DEngine Desktop

## Purpose
This guide covers the native desktop host located in the `3DEngine` project.

## Requirements
- .NET SDK 10.0.300 or higher, as pinned by the root `global.json`
- A Vulkan-capable machine and drivers
- Native SDL runtime assets restored through NuGet packages during build
- The Vulkan SDK, if you want the validation layer. Without it the host runs with no validation.

## Build
```powershell
dotnet build .\3DEngine\3DEngine.csproj
```

## Build The Full Solution
```powershell
dotnet build .\3DEngine.sln
```

## Run
```powershell
dotnet run --project .\3DEngine\3DEngine.csproj
```

To serve the surface on another port, give the setting `port`. The value 0 takes a free port.

```powershell
dotnet run --project .\3DEngine\3DEngine.csproj -- --port 5181
```

## What To Expect
- The line `3DEngine serves the document at http://127.0.0.1:5180`. An agent sends commands and
  queries to that address, on the document that the window holds (ADR-0019, TASK-0054). The surface
  stops when the window closes.
- A window of 1280 by 720 pixels, cleared to green.
- The console gives the Vulkan instance version, the validation layer, each instance extension, and
  the name of the sample scene.
- A validation message appears in the console as `[WARN]` or `[ERROR]`. A correct run gives none.

## Useful Notes
- The Vulkan code lives in `3DEngine.Vulkan`. See ADR-0017.
- If Vulkan initialization fails, check GPU driver support and the presence of the Vulkan runtime on the machine.
- The desktop stops before the window with exit code 1 for an address that is not loopback, 2 for a
  port that is not a number from 0 to 65535, and 3 when the native geometry library does not load.
