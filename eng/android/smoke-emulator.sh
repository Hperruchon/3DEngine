#!/usr/bin/env bash
# Phase R14f2 (ADR-0061 item 9, TASK-0229): the smoke test of the APK on the x86-64 emulator of the runner. It installs
# the APK, starts the program, and reads the log of Android for the lines of PhoneHost. The line of the start is the
# condition: it shows that .NET, the activity of SDL and the managed main ran. The line of the first frame is a fact
# of the report only, because the emulator draws Vulkan in software, and its version of Vulkan can be below the floor of
# the program (ADR-0061 item 3). The log of the program goes to the file of the report for a person to read.
# Usage: eng/android/smoke-emulator.sh <file.apk> <report file>
set -euo pipefail

apk="$1"
report="$2"
adb="$ANDROID_HOME/platform-tools/adb"

"$adb" wait-for-device
for _ in $(seq 1 120); do
  [ "$("$adb" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ] && break
  sleep 2
done
"$adb" shell getprop ro.build.version.release | tr -d '\r' | sed 's/^/Android /'

"$adb" install -r "$apk"
"$adb" logcat -c
"$adb" shell monkey -p org.threedengine.phone -c android.intent.category.LAUNCHER 1 > /dev/null

started=0
drawn=0
for _ in $(seq 1 60); do
  sleep 2
  "$adb" logcat -d > "$report" 2>&1 || true
  grep -q "3DEngine: the session of the phone starts" "$report" && started=1
  grep -q "3DEngine: the first frame is drawn" "$report" && drawn=1
  grep -q "3DEngine: the session of the phone ended" "$report" && break
  [ "$drawn" = 1 ] && break
done

grep "3DEngine:" "$report" || true
grep -E "FATAL|AndroidRuntime|Vulkan|vk[A-Z]" "$report" | head -40 || true
echo "started=$started drawn=$drawn"
if [ "$started" != 1 ]; then
  echo "::error title=Android smoke test::the program did not write its line of the start in 120 seconds"
  exit 1
fi
if [ "$drawn" != 1 ]; then
  echo "::warning title=Android smoke test::the program started and drew no first frame on the emulator; see the report"
fi
