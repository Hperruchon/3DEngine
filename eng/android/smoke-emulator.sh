#!/usr/bin/env bash
# Phase R14f2 (ADR-0061 item 9, TASK-0229): the smoke test of the APK on the x86-64 emulator of the runner. It installs
# the APK, starts the program, and reads the log of Android for the lines of PhoneHost. The line of the start is the
# condition: it shows that .NET, the activity of SDL and the managed main ran. The line of the first frame is a fact
# of the report only, because the emulator draws Vulkan in software, and its version of Vulkan can be below the floor of
# the program (ADR-0061 item 3). The log of the program goes to the file of the report for a person to read.
# Each wait has a limit, and a failure gives the last lines of the emulator as annotations, which a person with no
# sign-in can read; the first run of the job waited 40 minutes for a device and showed nothing.
# Phase R14f3 (ADR-0074, TASK-0233): the program starts with the extra "probe" and writes the determinism hash and its
# lines; the script writes them to the hash file, in the form of the hash file of the desktop runners, and fails
# with no hash.
# Usage: eng/android/smoke-emulator.sh <file.apk> <report file> <log of the emulator> <hash file>
set -euo pipefail

apk="$1"
report="$2"
emulator_log="${3:-}"
hash_file="${4:-}"
adb="$ANDROID_HOME/platform-tools/adb"

fail() {
  echo "::error title=Android smoke test::$1"
  if [ -n "$emulator_log" ] && [ -f "$emulator_log" ]; then
    tail -n 12 "$emulator_log" | while IFS= read -r line; do echo "::warning title=Emulator::${line:0:300}"; done
  fi
  # Finding T158 of the codebase review of 2026-11-06 (d) (TASK-0242): each call of adb has a limit.
  timeout 30 "$adb" devices -l | while IFS= read -r line; do echo "::warning title=adb devices::${line:0:300}"; done || true
  exit 1
}

timeout 300 "$adb" wait-for-device || fail "no device came in 300 seconds"
booted=0
for _ in $(seq 1 150); do
  if [ "$(timeout 30 "$adb" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r' || true)" = "1" ]; then
    booted=1
    break
  fi
  sleep 4
done
[ "$booted" = 1 ] || fail "the emulator did not complete its boot in 600 seconds"
timeout 30 "$adb" shell getprop ro.build.version.release | tr -d '\r' | sed 's/^/Android /' || true

timeout 300 "$adb" install -r "$apk" || fail "the install of the APK did not end in 300 seconds"
# Finding V105 of the codebase review of 2026-11-06 (d) (TASK-0243): the boot flag comes before the launcher shows, and a
# start in that time gave a process that ended with the code 0 and no line. The start waits until the launcher is the
# resumed activity, for 120 seconds at most.
launcher=""
for _ in $(seq 1 60); do
  launcher=$(timeout 30 "$adb" shell dumpsys activity activities 2>/dev/null | tr -d '\r' | grep -iE "(mResumedActivity|topResumedActivity).*launcher" | head -1 || true)
  [ -n "$launcher" ] && break
  sleep 2
done
echo "::notice title=Launcher::${launcher:0:200}"
timeout 30 "$adb" logcat -c || true
# TASK-0235: the output of the start is a public annotation; until TASK-0235 it went to /dev/null, and a start that
# failed gave no cause.
# TASK-0239: GitHub keeps 10 annotations of each level for each step. The seven lines of the start took seven notices,
# and the line of the exception of the program was lost (run 42270121672), so the start is one notice.
start=$(timeout 60 "$adb" shell am start -W -n org.threedengine.phone/org.threedengine.phone.MainActivity --ez probe true 2>&1 \
  | tr -d '\r' | paste -sd '|' - || true)
echo "::notice title=am start::${start:0:600}"

started=0
drawn=0
for _ in $(seq 1 60); do
  sleep 2
  timeout 30 "$adb" logcat -d > "$report" 2>&1 || true
  grep -q "3DEngine: the session of the phone starts" "$report" && started=1
  grep -q "3DEngine: the first frame is drawn" "$report" && drawn=1
  grep -q "3DEngine: the session of the phone ended" "$report" && break
  [ "$drawn" = 1 ] && break
done

# TASK-0239: the lines of the program, with no line of the determinism hash, which the hash file holds. The lines of
# the stack of an exception go into errors below.
grep "3DEngine:" "$report" | grep -v "3DEngine: determinism-line \|3DEngine: | " | head -8 \
  | while IFS= read -r line; do echo "::notice title=Program::${line:0:300}"; done || true
grep -E "FATAL|AndroidRuntime" "$report" | tail -n 4 | while IFS= read -r line; do echo "::warning title=Android::${line:0:300}"; done || true
echo "started=$started drawn=$drawn"
if [ "$started" != 1 ]; then
  # TASK-0235: the lines of Android about the package, its process and the runtime of .NET, as public annotations.
  # Finding 1 of the review of Codex of v0.220 (TASK-0239): each command has a limit, so a device that does not answer
  # does not hold the job. Finding 2: the last lines of the filter, which are the lines before the exit, and not the
  # first 40, because GitHub shows 10 warnings only.
  timeout 30 "$adb" shell pm list packages org.threedengine.phone 2>&1 | tr -d '\r' | while IFS= read -r line; do echo "::warning title=Package::$line"; done || true
  echo "::warning title=Process::pid $(timeout 30 "$adb" shell pidof org.threedengine.phone 2>/dev/null | tr -d '\r' || true)"
  grep -iE "threedengine|monodroid|mono-rt|DOTNET|SDL|libc  |DEBUG   |ActivityManager: (Start|Process|Kill)" "$report" \
    | tail -n 3 | while IFS= read -r line; do echo "::warning title=Android log::${line:0:300}"; done || true
  fail "the program did not write its line of the start in 120 seconds"
fi
# Finding T149 of the codebase review of 2026-11-06 (c) (TASK-0231): PhoneHost writes the line "ended" only for an
# exception, and the line of the start comes before the session, the window and the engine. An exception after the start
# passed the job. Now the line "ended", or a fatal exception of the process of the program, fails it.
# TASK-0239: the failure gives the line "ended" itself, and the first lines of the stack as errors.
if ended=$(grep -m 1 "3DEngine: the session of the phone ended" "$report"); then
  grep "3DEngine: | " "$report" | head -8 | while IFS= read -r line; do echo "::error title=Stack::${line:0:300}"; done || true
  fail "${ended:0:400}"
fi
if grep -A 3 "FATAL EXCEPTION" "$report" | grep -q "Process: org.threedengine.phone"; then
  fail "the process of the program ended with a fatal exception; see the lines of Android"
fi
# Finding T153 of the codebase review of 2026-11-06 (d) (TASK-0242): a native crash writes "Fatal signal" and a tombstone
# of DEBUG, and no FATAL EXCEPTION, so it passed the job.
if signal=$(grep -m 1 -E "Fatal signal .*org\.threedengine\.phone|Fatal signal .*threedengine\.phone|DEBUG +: .*>>> org\.threedengine\.phone" "$report"); then
  fail "the process of the program ended with a native crash: ${signal:0:300}"
fi
if [ -n "$hash_file" ]; then
  hash=$(grep -o "3DEngine: determinism [0-9a-f]\{64\}" "$report" | head -1 | cut -d' ' -f3 || true)
  [ -n "$hash" ] || fail "the program wrote no determinism hash"
  mkdir -p "$(dirname "$hash_file")"
  # Finding T157 of the codebase review of 2026-11-06 (d) (TASK-0242): with no line of the probe, grep gave 1 and the
  # shell left with no reason.
  grep -q "3DEngine: determinism-line " "$report" || fail "the program wrote the hash and no line of the probe"
  { echo "$hash"; grep -o "3DEngine: determinism-line .*" "$report" | sed 's/^3DEngine: determinism-line //' | tr -d '\r'; } > "$hash_file"
  echo "::notice title=Determinism::android-x64 gives $hash"
fi
if [ "$drawn" != 1 ]; then
  echo "::warning title=Android smoke test::the program started and drew no first frame on the emulator; see the report"
fi
