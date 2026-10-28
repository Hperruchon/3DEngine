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

To serve the surface on another port, give `--port` or the variable `Desktop__Port`. The value 0
takes a free port. The variable `PORT` has no effect.

```powershell
dotnet run --project .\3DEngine\3DEngine.csproj -- --port 5181
```

## The Document File
Give `--file` to open a document file, or to make it when it does not exist (ADR-0030):

```powershell
dotnet run --project .\3DEngine\3DEngine.csproj -- --file C:\Users\me\Documents\3DEngine\plate.3de
```

With no `--file`, the desktop writes into the folder of unsaved documents,
`<local application data>/3DEngine/unsaved`, from its first command. The setting
`Desktop:UnsavedFolder` changes that folder. Each applied command is in the file before its result.

The panel shows the name of the file, or "unsaved". Type a name in "File name":

- Save as moves the file to that name in the document folder, `<documents>/3DEngine`. The setting
  `Desktop:DocumentFolder` changes that folder. A file with that name must not exist.
- Open checks the file, then starts the desktop again with that file on the same port. A file that
  does not open gives its code in the panel, and the window stays.

An agent does the same with `GET /document`, `POST /document/save {"name": "plate"}` and
`POST /document/open {"name": "plate"}`.

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
  port that is not a number from 0 to 65535, 3 when the native geometry library does not load, 4
  when another process holds the port, 5 when the start fails for another reason, for example a
  port that the system refuses, and 6 when the document file does not open.
- When the window closes, the surface waits at most 2 seconds for an open request.
- Ctrl+C in the console stops the surface and closes the window.
