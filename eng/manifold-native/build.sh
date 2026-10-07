#!/usr/bin/env bash
# Builds the native Manifold C API (manifoldc) on Linux or macOS from a Manifold source folder,
# stages the libraries with their aliases, and checks that each library finds its dependency in
# its own folder. The workflow build-manifold-native.yml and a local build call this one script,
# so the recipe of the pipeline and the recipe of a computer cannot differ (TASK-0049).
#
# Usage: eng/manifold-native/build.sh <Manifold source folder> <staging folder>
#
# The flags and the checks come from TASK-0048 (register entries R-0035 and R-0018):
#   - MANIFOLD_CROSS_SECTION must stay ON: MANIFOLD_CBIND is a cmake_dependent_option forced OFF
#     when cross-section is OFF (v3.5.2), which would silently drop manifoldc.
#   - MANIFOLD_PAR=OFF is the serial (no-TBB) backend per ADR-0014 section 3.
#   - Each library finds its dependency in its own folder: $ORIGIN on Linux and @loader_path on
#     macOS. Manifold's own CMakeLists.txt (v3.5.2, lines 255-259) sets
#     CMAKE_BUILD_WITH_INSTALL_RPATH and CMAKE_INSTALL_RPATH as normal variables, which hide a flag
#     of the same name. It does not set CMAKE_BUILD_RPATH_USE_ORIGIN or CMAKE_BUILD_RPATH, so those
#     two flags act on Linux. On macOS CMAKE_BUILD_RPATH_USE_ORIGIN does not act, so a step after
#     the staging removes each absolute LC_RPATH and signs the library again.
#   - Each failure of the check is also an ::error line, a public annotation in the pipeline.
set -euo pipefail

[ $# -eq 2 ] || { echo "usage: $0 <Manifold source folder> <staging folder>" >&2; exit 2; }
source_dir=$(cd "$1" && pwd)
mkdir -p "$2"
staging=$(cd "$2" && pwd)
# A second build into the same folder found its own aliases from the first build, and cp then
# refused to copy a file onto itself (codebase review of 2026-10-08, finding T21). The files at
# the top of the staging folder go first; a folder inside it is not touched.
find "$staging" -mindepth 1 -maxdepth 1 -type f -delete
build="$source_dir/build"

case "$(uname -s)" in
  Linux)  os=Linux;  search_path='$ORIGIN' ;;
  Darwin) os=macOS;  search_path='@loader_path' ;;
  *) echo "build.sh builds Linux and macOS; use build.ps1 on Windows" >&2; exit 2 ;;
esac

echo "--- configure ($os) ---"
cmake -S "$source_dir" -B "$build" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DMANIFOLD_CROSS_SECTION=ON \
  -DMANIFOLD_CBIND=ON \
  -DMANIFOLD_PAR=OFF \
  -DMANIFOLD_TEST=OFF \
  -DMANIFOLD_DOWNLOADS=ON \
  -DCMAKE_BUILD_RPATH_USE_ORIGIN=ON \
  "-DCMAKE_BUILD_RPATH=$search_path"

echo "--- build ---"
cmake --build "$build" --config Release --parallel

echo "--- stage ---"
# A shared build emits manifoldc + core manifold (+ Clipper2 from cross-section): ship all.
find "$build" -type f \( \
  -iname 'libmanifoldc.so*' -o -iname 'libmanifoldc*.dylib' -o \
  -iname 'libmanifold.so*'  -o -iname 'libmanifold*.dylib'  -o \
  -iname 'libclipper2*.so*' -o -iname 'libclipper2*.dylib' \
\) -exec cp -v {} "$staging/" \;
# The build emits versioned files; .NET loads the unversioned name (libX.so / libX.dylib), and a
# library names its dependency as libX.so.<major> / libX.<major>.dylib. Create those aliases as
# copies, because a symbolic link does not survive the artifact zip.
shopt -s nullglob
for f in "$staging"/*.so.*; do                      # Linux: libX.so.3.5.2
  stem="${f%.so.*}"; major="$(printf '%s' "${f#*.so.}" | cut -d. -f1)"
  cp -f "$f" "$stem.so"
  cp -f "$f" "$stem.so.$major"
done
for f in "$staging"/lib*.*.dylib; do                # macOS: libX.3.5.2.dylib
  b="$(basename "$f")"; name="${b%%.*}"
  ver="${b#*.}"; ver="${ver%.dylib}"; major="$(printf '%s' "$ver" | cut -d. -f1)"
  cp -f "$f" "$staging/$name.dylib"
  cp -f "$f" "$staging/$name.$major.dylib"
done
ls -la "$staging"
compgen -G "$staging/libmanifoldc*" > /dev/null || { echo "::error title=Native build ($os)::manifoldc was not built"; exit 1; }

if [ "$os" = macOS ]; then
  echo "--- remove the absolute search paths ---"
  for f in "$staging"/*.dylib; do
    commands=$(otool -l "$f")
    paths=$(awk '/cmd LC_RPATH/ { getline; getline; print $2 }' <<<"$commands")
    for p in $paths; do
      case "$p" in
        /*) echo "$f: delete $p"; install_name_tool -delete_rpath "$p" "$f" ;;
      esac
    done
    codesign --force --sign - "$f"
  done
fi

echo "--- check ---"
# Each tool writes into a variable first, and grep reads the variable. A pipe into grep -q can close
# before the tool ends, and with pipefail the broken pipe then reads as a failed check.
failed=0
for f in "$staging"/*; do
  # The folders of a GitHub runner, and the build folder of the machine that runs this script.
  if grep -a -q -E '/(home|Users)/runner/' "$f" || grep -a -q -F "$build" "$f"; then
    found=$( (grep -a -o -E '/(home|Users)/runner/[^[:cntrl:]]*' "$f"; grep -a -o -F "$build" "$f") || true)
    echo "::error title=Native build ($os)::$f holds the path of the build machine ${found%%$'\n'*}"
    failed=1
  fi
done
if [ "$os" = Linux ]; then
  dynamic=$(readelf -d "$staging/libmanifoldc.so")
  search=$(grep -E 'RPATH|RUNPATH' <<<"$dynamic" || true)
  echo "search path: $search"
  grep -q 'ORIGIN' <<<"$search" \
    || { echo "::error title=Native build ($os)::libmanifoldc.so has no search path with \$ORIGIN: $search"; failed=1; }
  resolved=$(ldd "$staging/libmanifoldc.so" || true)
  echo "$resolved"
  dependency=$(grep 'libmanifold.so' <<<"$resolved" || true)
  grep -q "$staging/" <<<"$dependency" \
    || { echo "::error title=Native build ($os)::ldd does not resolve libmanifold inside the staging folder: $dependency"; failed=1; }
else
  commands=$(otool -l "$staging/libmanifoldc.dylib")
  search=$(grep -A2 LC_RPATH <<<"$commands" || true)
  echo "$search"
  grep -q '@loader_path' <<<"$search" \
    || { echo "::error title=Native build ($os)::libmanifoldc.dylib has no LC_RPATH with @loader_path: ${search//$'\n'/ }"; failed=1; }
  otool -L "$staging/libmanifoldc.dylib"
fi
exit $failed
