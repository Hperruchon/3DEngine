#!/usr/bin/env bash
# Phase R14f1 (ADR-0061 items 5 to 7, TASK-0228): the check of the APK of the phone. The APK holds the ABIs arm64-v8a
# and x86_64 only, so no file of 32-bit x86 (CLAUDE.md, determinism rule 5), and each ABI holds the native libraries of
# SDL, cimgui and Manifold. The checksum of each library of cimgui and Manifold is a row of THIRD-PARTY-NOTICES.md, so the
# APK holds the bytes of the packages of R14d and no other library of the same name. Each ABI holds the assemblies of
# the program (TASK-0229).
# Usage: eng/android/check-apk.sh <file.apk>
set -euo pipefail

apk="$1"
notices="$(dirname "$0")/../../THIRD-PARTY-NOTICES.md"
[ -f "$apk" ] || { echo "::error title=APK::the file $apk does not exist"; exit 1; }

abis=$(unzip -Z1 "$apk" | grep '^lib/' | cut -d/ -f2 | sort -u | tr '\n' ' ')
echo "ABIs: $abis"
if [ "$abis" != "arm64-v8a x86_64 " ]; then
  echo "::error title=APK::the ABIs are '$abis', and only arm64-v8a and x86_64 are permitted"
  exit 1
fi

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
unzip -q "$apk" -d "$work"
failed=0
for abi in arm64-v8a x86_64; do
  # Finding T150 of the codebase review of 2026-11-06 (c) (TASK-0231): the checksum was accepted from any row, so the
  # libraries of arm64 in the folder x86_64 passed. Now each library has one row, of its own ABI and its own file.
  case "$abi" in
    arm64-v8a) rid=android-arm64 ;;
    x86_64) rid=android-x64 ;;
  esac
  for library in libSDL3.so libcimgui.so libmanifold.so libmanifoldc.so; do
    file="$work/lib/$abi/$library"
    if [ ! -f "$file" ]; then
      echo "::error title=APK::lib/$abi/$library is absent"
      failed=1
      continue
    fi
    sum=$(sha256sum "$file" | cut -d' ' -f1)
    echo "lib/$abi/$library $sum"
    row="runtimes/$rid/native/$library"
    [ "$library" = libcimgui.so ] && row="Engine.Native.DearImGui/$row"
    if [ "$library" != libSDL3.so ] && ! grep -Fq "| \`$row\` | \`$sum\` |" "$notices"; then
      echo "::error title=APK::lib/$abi/$library has the checksum $sum, which is not the checksum of the row $row of THIRD-PARTY-NOTICES.md"
      failed=1
    fi
  done
done
# Phase R14f2 (TASK-0229): the APK holds the assemblies of the program for each ABI. A Debug build left them out for the
# fast deployment of an IDE, and the APK of R14e2 to R14f1 could not start on a phone.
for abi in arm64-v8a x86_64; do
  for assembly in 3DEngine.Android 3DEngine.App 3DEngine.Vulkan 3DEngine.Core Engine.Core Engine.Contracts Engine.Kernel Engine.Geometry.Manifold ImGui.NET SDL3-CS Vortice.Vulkan; do
    if [ ! -f "$work/lib/$abi/lib_$assembly.dll.so" ]; then
      echo "::error title=APK::the assembly $assembly is absent for $abi"
      failed=1
    fi
  done
done
exit $failed
