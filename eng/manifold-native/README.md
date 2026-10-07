# The native Manifold build

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This folder holds the packaging project of the native Manifold payload, the pin of its build, and
the two build scripts. The workflow `.github/workflows/build-manifold-native.yml` and a local build
call the same scripts, so the two recipes cannot differ (TASK-0049).

| File | Use |
|---|---|
| `manifold-ref.txt` | The pin: the Manifold reference and the package version. A push that changes it starts the workflow. |
| `build.sh` | Builds, stages and checks the libraries on Linux and macOS. |
| `build.ps1` | Builds and stages the libraries on Windows. |
| `Engine.Geometry.Manifold.Native.csproj` | Packs the libraries of each platform into one NuGet package. |

## Two kinds of build

**A local build is for development and test.** It is fast, and it needs no runner and no sign-in.
Its libraries go into `artifacts/`, which git ignores. A local build never replaces the package in
`nuget/`.

**The official package comes from the workflow.** It builds Windows, Linux and macOS in a clean,
public environment, and it pushes the package to the branch `native-package/<version>`. A version is
immutable: the workflow does not overwrite a branch that exists. Copy the package into `nuget/`,
record its checksums in `THIRD-PARTY-NOTICES.md`, and change the version in
`Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj`.

The owner decided this on 2026-10-06. macOS cannot be built on a Windows computer.

## A local build

1. Clone the pinned Manifold source into `artifacts/`. Use the reference of `manifold-ref.txt`:

   ```bash
   git clone --depth 1 --branch v3.5.2 --recurse-submodules --shallow-submodules https://github.com/elalish/manifold artifacts/manifold-src
   ```

2. Build for the platform of this computer.

   On Windows, in PowerShell. Visual Studio must have the C++ tools and a Windows SDK:

   ```powershell
   .\eng\manifold-native\build.ps1 -Source artifacts\manifold-src -Staging artifacts\native\win-x64
   ```

   On Linux, or in WSL with Ubuntu (`wsl --install -d Ubuntu` installs it). Install the tools one
   time with `sudo apt-get install -y cmake g++ git`:

   ```bash
   bash eng/manifold-native/build.sh artifacts/manifold-src artifacts/native/linux-x64
   ```

3. Test the engine against the local libraries. Build the solution, then copy the staged libraries
   over the libraries of the package in the output folder of the tests, and run the native tests:

   ```powershell
   dotnet build 3DEngine.sln
   Copy-Item artifacts\native\win-x64\*.dll Engine.Tests\bin\Debug\net10.0\runtimes\win-x64\native\ -Force
   dotnet test Engine.Tests\Engine.Tests.csproj --no-build --filter "FullyQualifiedName~Manifold"
   ```

   Run `dotnet build 3DEngine.sln --no-incremental` after the test, so that the output folder holds the
   libraries of the package again.

## The checks

`build.sh` fails when a library holds a path of the build machine, or when `libmanifoldc` does not
find `libmanifold` in its own folder. Each failure is also an `::error` line, which the pipeline shows
as a public annotation. Register entries R-0035 and R-0018 give the history of the two defects that
these checks prevent.
