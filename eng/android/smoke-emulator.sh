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
  "$adb" devices -l | while IFS= read -r line; do echo "::warning title=adb devices::${line:0:300}"; done
  exit 1
}

timeout 300 "$adb" wait-for-device || fail "no device came in 300 seconds"
booted=0
for _ in $(seq 1 150); do
  if [ "$("$adb" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; then
    booted=1
    break
  fi
  sleep 4
done
[ "$booted" = 1 ] || fail "the emulator did not complete its boot in 600 seconds"
"$adb" shell getprop ro.build.version.release | tr -d '\r' | sed 's/^/Android /'

timeout 300 "$adb" install -r "$apk" || fail "the install of the APK did not end in 300 seconds"
"$adb" logcat -c
"$adb" shell am start -n org.threedengine.phone/org.threedengine.phone.MainActivity --ez probe true > /dev/null

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

grep "3DEngine:" "$report" | head -60 | while IFS= read -r line; do echo "::notice title=Program::${line:0:300}"; done || true
grep -E "FATAL|AndroidRuntime" "$report" | head -12 | while IFS= read -r line; do echo "::warning title=Android::${line:0:300}"; done || true
echo "started=$started drawn=$drawn"
[ "$started" = 1 ] || fail "the program did not write its line of the start in 120 seconds"
# Finding T149 of the codebase review of 2026-11-06 (c) (TASK-0231): PhoneHost writes the line "ended" only for an
# exception, and the line of the start comes before the session, the window and the engine. An exception after the start
# passed the job. Now the line "ended", or a fatal exception of the process of the program, fails it.
if grep -q "3DEngine: the session of the phone ended" "$report"; then
  fail "the session of the phone ended with an exception; see the lines of the program"
fi
if grep -A 3 "FATAL EXCEPTION" "$report" | grep -q "Process: org.threedengine.phone"; then
  fail "the process of the program ended with a fatal exception; see the lines of Android"
fi
if [ -n "$hash_file" ]; then
  hash=$(grep -o "3DEngine: determinism [0-9a-f]\{64\}" "$report" | head -1 | cut -d' ' -f3 || true)
  [ -n "$hash" ] || fail "the program wrote no determinism hash"
  mkdir -p "$(dirname "$hash_file")"
  { echo "$hash"; grep -o "3DEngine: determinism-line .*" "$report" | sed 's/^3DEngine: determinism-line //' | tr -d '\r'; } > "$hash_file"
  echo "::notice title=Determinism::android-x64 gives $hash"
fi
if [ "$drawn" != 1 ]; then
  echo "::warning title=Android smoke test::the program started and drew no first frame on the emulator; see the report"
fi
