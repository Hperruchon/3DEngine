# Third-party notices

This file lists each third-party component that this repository distributes, and the licence of each
one. Register entry R-0005 required it. ADR-0014 section 5 requires a checksum for each binary.

`Engine.Tests/Governance/NativePackageGateTests.cs` compares each checksum below against the file.
A change to the binary that does not change this file fails the build.

Written 2026-09-22 by TASK-0024. Section 4 added 2026-09-23 by TASK-0027. Section 5 added 2026-10-28 by
TASK-0084. Sections 1 and 2 changed 2026-11-06 by TASK-0226 for the packages of R14d.

## 1 The native geometry payload

The repository holds two binary artifacts:

```
nuget/Engine.Geometry.Manifold.Native.3.5.2.2.nupkg
nuget/Engine.Native.DearImGui.1.91.6.1.nupkg
```

The first package holds the native library of Manifold for five runtime identifiers: `linux-x64`, `osx-arm64`,
`win-x64`, `android-arm64` and `android-x64`. `.github/workflows/build-manifold-native.yml` builds it from source.
ADR-0014 gives the reason and the rules. The second package holds the native cimgui of section 5.2 for
`android-arm64` and `android-x64`, because the package `ImGui.NET` has no Android library;
`.github/workflows/build-cimgui-native.yml` builds it from source (ADR-0061 item 6). Its licences are in section 5.

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

### 2.1 The packages

| File | SHA-256 |
|---|---|
| `nuget/Engine.Geometry.Manifold.Native.3.5.2.2.nupkg` | `e595cf524084e4f594da690733e060b2eb0e7d801630f818cc5d2e90e14321f6` |
| `nuget/Engine.Native.DearImGui.1.91.6.1.nupkg` | `45e35494e5e0b8ff77403d941cf46b0ac8392d9f777fb05791163729c1067337` |

The package version 3.5.2.2 is the third build of the Manifold source 3.5.2 (TASK-0196). The workflow
`build-manifold-native.yml` built it in run 42145031039 from the commit `d1a447f` of this repository, the merge of
v0.203, and pushed it to the branch `native-package/3.5.2.2`. The package 3.5.2.1 of TASK-0048 had no Android library;
the files of `linux-x64` and the two files `libmanifold` of `osx-arm64` have the same bytes in both versions, and the
files of `win-x64` and the files `libmanifoldc` of `osx-arm64` differ. The workflow `build-cimgui-native.yml` built
the package of cimgui in run 42145031007 from the same commit, and pushed it to the branch
`native-package-cimgui/1.91.6.1`.

### 2.2 The native binaries inside the package of Manifold

A file with a version suffix and a file with no suffix hold the same bytes on Linux. On macOS each
copy is signed again under its own name, so some copies differ. The table gives one row for each
distinct file.

| File inside the package | SHA-256 |
|---|---|
| `runtimes/win-x64/native/manifold.dll` | `44b8a7cd00ae198420d5987ecaf732fbf609f6bd040e0b6a1a10ba2751067f6e` |
| `runtimes/win-x64/native/manifoldc.dll` | `afe4e9a68a2bf01eee9489fc2ecde8222c3098544ea38dcfefe63a24ef9aed85` |
| `runtimes/linux-x64/native/libmanifold.so.3.5.2` | `eb3b5a43120826c9124459ef9fb64c6b2f24e39a86cab146fc261b414adba552` |
| `runtimes/linux-x64/native/libmanifoldc.so.3.5.2` | `c7f5176d5174d71d4cb77513dc1b3b42cda4bc14cbda2709afdcfd9597a2f0b9` |
| `runtimes/osx-arm64/native/libmanifold.3.5.2.dylib` | `74fceb019c2aed47972109c3dd95b2b06005cc97581abc5cfc4e163a2579f9c2` |
| `runtimes/osx-arm64/native/libmanifold.dylib` | `c811b0f6de29eabf6c49b6b9718ed7e452243a4634f71aba0afb483a57d6d22e` |
| `runtimes/osx-arm64/native/libmanifoldc.3.5.2.dylib` | `9e6e7bbddd16ff81ca8bcf683776efffb216cc1fc304497d03e5bece1d2b1927` |
| `runtimes/osx-arm64/native/libmanifoldc.3.dylib` | `7bd033b52b91435b6c0f2a2c65e55344f74a754cdad5f5da3e2242e4f2831c9b` |
| `runtimes/osx-arm64/native/libmanifoldc.dylib` | `9ce1629e957e37960bf3b64caa03408a4d8ad7b94607203a80ba452e796cd550` |
| `runtimes/android-arm64/native/libmanifold.so` | `6b200c318fab7d9fb898ace0e221999572575f9d49ce4fa6f188b06978e2c177` |
| `runtimes/android-arm64/native/libmanifoldc.so` | `fba36d3d2ac11910d6f8d06e9e57f54db2484991e68c43795189e559365090de` |
| `runtimes/android-x64/native/libmanifold.so` | `0666994b1c2054a96cac6af9d2bd2cd3215bdcd9cb24f1e606f2e92dc2a37986` |
| `runtimes/android-x64/native/libmanifoldc.so` | `e7365a0bf015ddd6417261e2c7ad88266c36da67ac5e4fad1767f7cb66563892` |

### 2.3 The native binaries inside the package of cimgui

The name of the package comes before the path, so that the gate keeps the rows of the two packages apart.

| File inside the package | SHA-256 |
|---|---|
| `Engine.Native.DearImGui/runtimes/android-arm64/native/libcimgui.so` | `def79c080b50ed53797d856b4456d97654af3848e90e7cb182f6a798699b218c` |
| `Engine.Native.DearImGui/runtimes/android-x64/native/libcimgui.so` | `6d1be13e88c9e31e98d884b63d008e52b784068b2a36a412a6da194d987465fd` |

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

The NuGet packages `Vortice.Vulkan` and `ppy.SDL3-CS` are not in this list (since R14c, ADR-0061 item 5). The repository
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

## 5 The library of the user interface

TASK-0084 added the NuGet package ImGui.NET 1.91.6.1 to `3DEngine/3DEngine.csproj` (ADR-0027). The
repository does not hold the package; a restore downloads it. The build output of the desktop holds its
native library `cimgui`, which holds Dear ImGui 1.91.6 and its default font ProggyClean, so a package of
the desktop must give these four notices. Each one is the MIT License.

| Component | Source | Licence text |
|---|---|---|
| ImGui.NET 1.91.6.1 | https://github.com/ImGuiNET/ImGui.NET | `LICENSE` at the tag `v1.91.6.1` |
| cimgui 1.91.6 | https://github.com/cimgui/cimgui | `LICENSE` at the tag `1.91.6` |
| Dear ImGui 1.91.6 | https://github.com/ocornut/imgui | `LICENSE.txt` at the tag `v1.91.6` |
| ProggyClean, the default font of Dear ImGui | http://www.proggyfonts.net/ | `docs/FONTS.md` of Dear ImGui at the tag `v1.91.6` names it: by Tristan Grimmer, MIT License. Dear ImGui embeds it in `imgui.cpp`. |

### 5.1 ImGui.NET

```
The MIT License (MIT)

Copyright (c) 2017 Eric Mellino and ImGui.NET contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### 5.2 cimgui

```
The MIT License (MIT)

Copyright (c) 2015 Stephan Dilly

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### 5.3 Dear ImGui

```
The MIT License (MIT)

Copyright (c) 2014-2024 Omar Cornut

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
