#!/usr/bin/env bash
# Builds the native cimgui of ImGui.NET 1.91.6.1 for Android from a cimgui source folder with its imgui submodule, stages
# libcimgui.so, and checks it (phase R14d, ADR-0061 item 6, TASK-0196). The workflow build-cimgui-native.yml and a local
# build call this one script.
#
# Usage: eng/cimgui-native/build-android.sh <cimgui source folder> <staging folder> <Android ABI>
#
# - The ABI is arm64-v8a (the phone) or x86_64 (the emulator of the pipeline only, ADR-0061 item 1).
# - The NDK comes from $ANDROID_NDK_LATEST_HOME or $ANDROID_NDK_HOME; API level 33 is the minimum of ADR-0061 item 2.
# - Each LOAD segment is aligned to pages of 16 KB, which Android 15 and later require.
# - The check reads the machine, the alignment, and the exported functions that ImGui.NET calls first.
set -euo pipefail

[ $# -eq 3 ] || { echo "usage: $0 <cimgui source folder> <staging folder> <Android ABI>" >&2; exit 2; }
source_dir=$(cd "$1" && pwd)
mkdir -p "$2"
staging=$(cd "$2" && pwd)
abi="$3"
case "$abi" in
  arm64-v8a) machine='AArch64' ;;
  x86_64)    machine='Advanced Micro Devices X86-64' ;;
  *) echo "the Android ABI must be arm64-v8a or x86_64, and '$abi' is not" >&2; exit 2 ;;
esac
ndk="${ANDROID_NDK_LATEST_HOME:-${ANDROID_NDK_HOME:-}}"
[ -n "$ndk" ] && [ -f "$ndk/build/cmake/android.toolchain.cmake" ] \
  || { echo "::error title=cimgui (Android)::no NDK: set ANDROID_NDK_LATEST_HOME or ANDROID_NDK_HOME"; exit 1; }
[ -f "$source_dir/imgui/imgui.cpp" ] || { echo "::error title=cimgui (Android)::the imgui submodule is absent"; exit 1; }

build="$source_dir/build-$abi"
find "$staging" -mindepth 1 -maxdepth 1 -type f -delete

echo "--- configure (Android $abi) ---"
cmake -S "$source_dir" -B "$build" \
  "-DCMAKE_TOOLCHAIN_FILE=$ndk/build/cmake/android.toolchain.cmake" \
  "-DANDROID_ABI=$abi" \
  -DANDROID_PLATFORM=android-33 \
  -DCMAKE_BUILD_TYPE=Release \
  -DIMGUI_STATIC=no \
  "-DCMAKE_SHARED_LINKER_FLAGS=-Wl,-z,max-page-size=16384"

echo "--- build ---"
cmake --build "$build" --config Release --parallel

echo "--- stage ---"
cp -v "$build/libcimgui.so" "$staging/libcimgui.so"
ls -la "$staging"

echo "--- check ---"
failed=0
readelf="$(compgen -G "$ndk/toolchains/llvm/prebuilt/*/bin/llvm-readelf" | head -1)"
header=$("$readelf" -h "$staging/libcimgui.so")
grep -q "Machine: *$machine" <<<"$header" \
  || { echo "::error title=cimgui (Android $abi)::libcimgui.so is not built for $machine"; failed=1; }
segments=$("$readelf" -lW "$staging/libcimgui.so")
small=$(awk '$1 == "LOAD" && $NF != "0x4000" { print $NF }' <<<"$segments")
[ -z "$small" ] || { echo "::error title=cimgui (Android $abi)::a LOAD segment has the alignment $small, not 0x4000"; failed=1; }
symbols=$("$readelf" --dyn-syms -W "$staging/libcimgui.so")
for name in igCreateContext igNewFrame igRender igGetIO igDockSpace; do
  grep -q " $name$" <<<"$symbols" \
    || { echo "::error title=cimgui (Android $abi)::libcimgui.so does not export $name"; failed=1; }
done
if grep -a -q -E '/(home|Users)/runner/' "$staging/libcimgui.so"; then
  echo "::error title=cimgui (Android $abi)::libcimgui.so holds the path of the build machine"
  failed=1
fi
exit $failed
