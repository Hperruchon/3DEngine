# Third-party notices

This file lists each third-party component that this repository distributes, and the licence of each
one. Register entry R-0005 required it. ADR-0014 section 5 requires a checksum for each binary.

`Engine.Tests/Governance/NativePackageGateTests.cs` compares each checksum below against the file.
A change to the binary that does not change this file fails the build.

Written 2026-09-22 by TASK-0024. Section 4 added 2026-09-23 by TASK-0027.

## 1 The native geometry payload

The repository holds one binary artifact:

```
nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg
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
| `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg` | `e2a1a359b5dec9dd330a4ef9aaa707ccb752b27077566f84a16391f996ba4b04` |

### 2.2 The native binaries inside the package

A file with a version suffix and a file with no suffix hold the same bytes. The table gives one row
for each distinct file.

| File inside the package | SHA-256 |
|---|---|
| `runtimes/win-x64/native/manifold.dll` | `22395895eb2af2843a293e94be009aca9660cc9092bce4560480a60d006542ec` |
| `runtimes/win-x64/native/manifoldc.dll` | `cf8c4e028d4201186764a8e03d00abe399d46b5381cb9672ddec4bb3debe711e` |
| `runtimes/linux-x64/native/libmanifold.so.3.5.2` | `466f58f308260ae6345d11e86d3abff2c1cbd0284df56029c718300c4daab976` |
| `runtimes/linux-x64/native/libmanifoldc.so.3.5.2` | `23eb15e51c477fe41df7e8fabf476baf92eef5834bc1d9a26b807faafbe0ad10` |
| `runtimes/osx-arm64/native/libmanifold.3.5.2.dylib` | `d6d3cbb89052836bde5be290adf5d0cb13fcec219eb9394fc41c06bfb78913f0` |
| `runtimes/osx-arm64/native/libmanifoldc.3.5.2.dylib` | `44c8cba4ab487cbcdc650bb2c7a9849abcf3b3b90b1315e22c451a63dc7d98ae` |

## 3 One provenance defect

The nuspec inside the package holds this element:

```xml
<repository type="git" commit="718eab0684178e4fdf7ef419cc4ff26484008705" />
```

That commit is not a Manifold commit. It is a commit in this repository, dated 2026-07-04, with the
subject "Merge pull request #8 from Hperruchon/p7b-finish". The description in the same nuspec says
"Version tracks the pinned Manifold commit", therefore the file gives incorrect information about the
origin of the binary.

The version 3.5.2 in the package identifies the source, and section 1.1 gives the commit that the tag
`v3.5.2` names. A reader must use section 1.1 and not the nuspec.

Register entry R-0018 holds this defect. A correction needs a new build, because the nuspec lives
inside the package.

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
