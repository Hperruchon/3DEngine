# Third-party notices

This file lists each third-party component that this repository distributes, and the licence of each
one. Register entry R-0005 required it. ADR-0014 section 5 requires a checksum for each binary.

`Engine.Tests/Governance/NativePackageGateTests.cs` compares each checksum below against the file.
A change to the binary that does not change this file fails the build.

Written 2026-09-22 by TASK-0024. Section 4 added 2026-09-23 by TASK-0027.

## 1 The native geometry payload

The repository holds one binary artifact:

```
nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg
```

The package holds the native library for three runtime identifiers: `linux-x64`, `osx-arm64` and
`win-x64`. `.github/workflows/build-manifold-native.yml` builds it from source. ADR-0014 gives the
reason and the rules.

### 1.1 Manifold

| Item | Value |
|---|---|
| Component | Manifold, and its C binding `manifoldc` |
| Source | https://github.com/elalish/manifold |
| Version | 3.5.2 |
| Commit | `11235e6b8ebea2dbed8aec4285685aafd3d95667`, which is the tag `v3.5.2` |
| Licence | Apache License 2.0 |
| Licence text | https://github.com/elalish/manifold/blob/v3.5.2/LICENSE |

The build uses `MANIFOLD_PAR=OFF`, therefore the payload holds no parallel backend and it links no
threading library. ADR-0014 section 3 gives the reason: a serial build is a condition for replay
determinism.

### 1.2 Clipper2

| Item | Value |
|---|---|
| Component | Clipper2 |
| Source | https://github.com/AngusJohnson/Clipper2 |
| Commit | `46f639177fe418f9689e8ddb74f08a870c71f5b4`, dated 2026-03-05 |
| Licence | Boost Software License 1.0 |
| Licence text | https://github.com/AngusJohnson/Clipper2/blob/main/LICENSE |

Manifold uses Clipper2 for its cross-section operations. The build sets `MANIFOLD_CROSS_SECTION=ON`,
and `cmake/manifoldDeps.cmake` fetches Clipper2 at the commit above when the host supplies no copy.

The package holds no separate Clipper2 binary. The code is compiled into the Manifold library: a
search of the raw bytes of `libmanifold.so.3.5.2`, of `manifold.dll` and of `libmanifold.3.5.2.dylib`
finds the name Clipper in each one. The notice above is therefore necessary.

## 2 Checksums

Each value is SHA-256. Compute one with `sha256sum <path>`.

### 2.1 The package

| File | SHA-256 |
|---|---|
| `nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg` | `ee6a86f16189967490b360129c1f17d905f1212649fbe6a39800a0bc17917b00` |

The package version 3.5.2.1 is the second build of the Manifold source 3.5.2 (TASK-0048). The workflow
`build-manifold-native.yml` built it in run 37538228336 from the commit `27e65b6` of this repository,
and pushed it to the branch `native-package/3.5.2.1`.

### 2.2 The native binaries inside the package

A file with a version suffix and a file with no suffix hold the same bytes on Linux. On macOS each
copy is signed again under its own name, so some copies differ. The table gives one row for each
distinct file.

| File inside the package | SHA-256 |
|---|---|
| `runtimes/win-x64/native/manifold.dll` | `898f6ca8993b437e4ae9f7098ffd3324af593af94b2664b79dbc4ccc326a3f45` |
| `runtimes/win-x64/native/manifoldc.dll` | `7acc59ea545253c5ebfd71aff125b33e37a87dcd6bb32f7f40b07398906283f0` |
| `runtimes/linux-x64/native/libmanifold.so.3.5.2` | `eb3b5a43120826c9124459ef9fb64c6b2f24e39a86cab146fc261b414adba552` |
| `runtimes/linux-x64/native/libmanifoldc.so.3.5.2` | `c7f5176d5174d71d4cb77513dc1b3b42cda4bc14cbda2709afdcfd9597a2f0b9` |
| `runtimes/osx-arm64/native/libmanifold.3.5.2.dylib` | `74fceb019c2aed47972109c3dd95b2b06005cc97581abc5cfc4e163a2579f9c2` |
| `runtimes/osx-arm64/native/libmanifold.dylib` | `c811b0f6de29eabf6c49b6b9718ed7e452243a4634f71aba0afb483a57d6d22e` |
| `runtimes/osx-arm64/native/libmanifoldc.3.5.2.dylib` | `e0557879572874937a418c04dfee6748281c21b6acded2e2e898fec5ab09d6ad` |
| `runtimes/osx-arm64/native/libmanifoldc.3.dylib` | `336c8e2d06379136c524fd5d86573abec467909a8ad47869d6de644d3a9c4ffd` |
| `runtimes/osx-arm64/native/libmanifoldc.dylib` | `cf5ad252ff94203953c55effccb59f69d3767e94eb22eacb88c214151902f440` |

## 3 Two defects of the first package, corrected

The package 3.5.2 of 2026-09 had two defects, and the package 3.5.2.1 corrects both (TASK-0048):

- Its nuspec recorded `commit="718eab0684178e4fdf7ef419cc4ff26484008705"`, a commit of this repository,
  as the source of the binary (register entry R-0018). The nuspec of 3.5.2.1 records the Manifold
  address and the commit of section 1.1, and `NativePackageGateTests` holds the two equal.
- On Linux and macOS, `libmanifoldc` stored the build folder of a runner as the search path of
  `libmanifold`, so the library loaded on no other computer (register entry R-0035). The libraries
  of 3.5.2.1 use `$ORIGIN` and `@loader_path`, and a gate test refuses a path of a build machine.

## 4 Source code from the Vortice.Vulkan samples

`3DEngine.Vulkan/` holds five source files that come from the sample framework of the Vortice.Vulkan
binding: `GraphicsDevice.cs`, `Swapchain.cs`, `Window.cs`, `Log.cs` and `Utils.cs`. TASK-0027 moved
them into a first-party project and changed them. ADR-0017 gives the rules. Each file keeps the
copyright line of its author.

Before TASK-0027 the same code sat in `Vortice.Vulkan.SampleFramework/` and
`Vortice.Vulkan.Sample/`. Each file said "See LICENSE in the repository root". That file is the
licence of this repository, therefore the notice for the sample author was absent.

| Item | Value |
|---|---|
| Component | The sample framework of Vortice.Vulkan |
| Source | https://github.com/amerkoleci/Vortice.Vulkan |
| Commit | Not recorded. Commit `939e23f` of this repository, dated 2024-10-12, added the files and names no source commit. |
| Licence | MIT License |
| Licence text | Below. It is a copy of `LICENSE` at commit `ef01519051c1ea9ab92cf6f381ab7ca897ab293f`, which the nuspec of `Vortice.Vulkan` 3.2.3 names. |

The NuGet packages `Vortice.Vulkan` and `Alimer.Bindings.SDL` are not in this list. The repository
does not hold them. A restore downloads each one.

```
The MIT License (MIT)

Copyright (c) Amer Koleci and Contributors

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
```
