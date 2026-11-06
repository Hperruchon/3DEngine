# The test sheet of the phone

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

**Status: the first test of R14e.** Written 2026-11-06 by TASK-0227. The row R14e of `docs/roadmap.md` closes only after
the owner does this test on the reference device (ADR-0061 items 2 and 9). No device and no emulator ran the program
before this test.

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
5. Keep the log open in a second terminal while you test:

   ```bash
   "/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" logcat -v time DOTNET:V mono-stdout:V SDL:V *:E
   ```

## 3 The steps

Write the result of each step in the table of section 4. A step that fails does not stop the test: go to the next step
when you can.

| Step | Do this | Expect this |
|---|---|---|
| 1 | Open the app 3DEngine. | A window in portrait, with the panel of the desktop and an empty scene. |
| 2 | In the panel, tap New box, give 10, 10 and 2, and tap Apply. | One body in the list of the panel, and a box in the scene. |
| 3 | Tap New box again, give 4, 4 and 4, and tap Apply. | Two bodies in the list, and a second box through the first one. |
| 4 | Tap the first body in the list. | Its controls show: Name, Rename, Hide, Move and "Cut with". |
| 5 | Tap "Cut with", then tap the second body. | One body in the list: the plate with a square hole. |
| 6 | Drag one finger in the scene, and pinch with two fingers. | The camera turns and zooms; the hole shows. |
| 7 | Tap Undo, then Redo. | Undo gives the two boxes again; Redo gives the plate with the hole. |
| 8 | Press Home, wait 10 seconds, and open the app again. | The same plate with the hole. The selection is gone. |
| 9 | Close the app from the list of recent apps. | The app ends. A new start gives an empty scene: the phone saves no file yet. |

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

## 5 Known limits of this test

- The phone has no Save and no Open: the picker of Android comes in a later slice of R14e.
- The screen stays in portrait, until a slice of the pre-rotation (ADR-0073 item 4).
- The panel is the panel of the desktop; the layout of the phone is R14g.
- The keyboard of the screen can show at the start, because the window starts the text input of SDL.
