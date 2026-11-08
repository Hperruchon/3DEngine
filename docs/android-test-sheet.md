# The test sheet of the phone

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

**Status: the first test of R14e.** Written 2026-11-06 by TASK-0227. The row R14e of `docs/roadmap.md` closes only after
the owner does this test on the reference device (ADR-0061 items 2 and 9). No phone ran the program before this test.
Since TASK-0229 the pipeline starts it at each push on the emulator of Android 14 for x86-64, with Vulkan in software,
up to its first frame, and since TASK-0233 it compares the determinism hash of the emulator with the desktop runners.
No touch, no background and no other step of section 3 runs there, and the emulator runs no code of arm64.

## 1 What you need

- The Samsung Galaxy S23 Ultra, with Android 13 or later.
- This computer, with .NET 10 and its workload for Android (the command `dotnet workload list` names `android`).
- A USB cable, and the option USB debugging on the phone (Settings, About phone, Software information: tap Build
  number seven times; then Settings, Developer options, USB debugging). Or a way to copy a file to the phone.

## 2 Build and install

1. In the root folder of the repository, build the program:

   ```bash
   dotnet build 3DEngine.Android
   ```

2. The file of the program is `3DEngine.Android/bin/Debug/net10.0-android/org.threedengine.phone-Signed.apk`.
3. With the cable and USB debugging, install it:

   ```bash
   "/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" install -r 3DEngine.Android/bin/Debug/net10.0-android/org.threedengine.phone-Signed.apk
   ```

4. With no cable, copy the file to the phone, open it there, and permit the install of an unknown app for that one
   time.
   Since TASK-0228 each run of the pipeline also keeps the APK for 14 days, as the artifact "apk-of-the-phone" of the
   job "Build the APK of the phone"; it is the same program, built on Ubuntu.
5. Keep the log open in a second terminal while you test:

   ```bash
   "/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" logcat -v time DOTNET:V mono-stdout:V SDL:V *:E
   ```

## 3 The steps

Write the result of each step in the table of section 4. A step that fails does not stop the test: go to the next step
when you can.

| Step | Do this | Expect this |
|---|---|---|
| 1 | Open the app 3DEngine, and tap "New part" on the home screen. | The home screen shows "Open file", "New part" and "No recent file" (ADR-0083, TASK-0260); after "New part", a window in portrait with the panel of the desktop and an empty scene. |
| 2 | In the panel, tap New, then Box, give 10, 10 and 2, and tap Apply. | One body in the list of the panel, and a box in the scene. |
| 3 | Tap New, then Cylinder, give 2 and 8, and tap Apply. | Two bodies in the list, and a cylinder through the box. |
| 4 | Tap the first body in the list. | Its controls show: Name, Rename, Hide, Move and "Cut with". |
| 5 | Tap "Cut with", then tap the second body. | One body in the list: the plate with a round hole. |
| 6 | Drag one finger in the scene, and pinch with two fingers. | The camera turns and zooms; the hole shows. |
| 7 | Tap Undo, then Redo. | Undo gives the two boxes again; Redo gives the plate with the hole. |
| 8 | Press Home, wait 10 seconds, and open the app again. | The same plate with the hole. The selection is gone. In the log, the line "goes into the background" comes at the return, just before the line "in the foreground again" (ADR-0073 item 3). |
| 9 | Close the app from the list of recent apps. | The app ends. A new start gives an empty scene: the phone saves no file yet. |
| 10 | With the cable, start the app with the probe: `"/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" shell am start -n org.threedengine.phone/org.threedengine.phone.MainActivity --ez probe true` | In the log, the line "3DEngine: determinism e67c318bf264fbf291b22e27ee7887b4fcd1943cee58f3e2dd2531d1773fc873", the hash of the desktop runners (ADR-0074); since TASK-0258 it holds the export and the import of the bracket of ADR-0083, and since TASK-0299 the cone, the sphere and the torus of ADR-0089. Another hash is a failure of anti-objective 16: copy each line "determinism-line" of the log. |
| 11 | In the settings of the phone, choose the navigation with three buttons (Display, Navigation bar, Buttons). Open the app, tap New, then Box, give 10, 10 and 2, tap Apply, and tap Undo. | Undo and Redo are above the bar of the three buttons, and the panel starts under the status bar. Undo removes the box (finding V118, TASK-0252). Set the navigation back after the step. |

## 4 Results

| Step | Result (pass, fail, or what you saw) | Lines of the log that start with "3DEngine:" |
|---|---|---|
| 1 | | |
| 2 | | |
| 3 | | |
| 4 | | |
| 5 | | |
| 6 | | |
| 7 | | |
| 8 | | |
| 9 | | |
| 10 | | |
| 11 | | |

## 5 Known limits of this test

- The phone has no Save and no Open: the picker of Android comes in a later slice of R14e.
- The screen stays in portrait, until a slice of the pre-rotation (ADR-0073 item 4).
- The panel is the panel of the desktop; the layout of the phone is R14g.
- The keyboard of the screen can show at the start, because the window starts the text input of SDL.
