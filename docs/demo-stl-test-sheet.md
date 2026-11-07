# The test sheet of the demonstration "Open the same STL on desktop and Android"

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

**Status: the first run of the owner.** Written 2026-11-07 by TASK-0266 (P9e), from the page of the demonstration
(https://claude.ai/artifact/NJVBcQYiheLRCcRgx9YQad) and ADR-0083. The rows P9c, P9d and P9e of `docs/roadmap.md` close only
after the owner does this run on the desktop and on the Galaxy S23 Ultra, as R14e does. No person ran these steps before
this run. The pipeline already holds criterion 10: at each push, Windows, Linux, macOS and the emulator export and import
the bracket and give one hash (TASK-0258).

## 1 Setup

The repository holds both files, so that each run uses the same bytes:

| File | What it is | Why |
|---|---|---|
| `testdata/stl/bracket.stl` | An L bracket, 60 by 40 by 30 mm, 4 mm thick, with two holes of 6 mm in its base: the STL export of the app (27,084 bytes, 540 triangles, binary). | Small and easy to see; the holes show at once if the view is wrong or the part is cut. |
| `testdata/stl/bracket-damaged.stl` | The first half of the same file (13,542 bytes). | The most common real fault: a download or a copy that stopped early. |

1. Copy both files to the folder Downloads on each device: on the desktop `C:\Users\<name>\Downloads`; on the S23 the
   folder Download, with the USB cable or from an email.
2. Desktop: Windows 11, the program built from `main` (`dotnet run --project 3DEngine`).
3. Phone: the APK of the pipeline or of this computer; section 2 of `docs/android-test-sheet.md` says how to install it.
   The app asks for no permission of storage.
4. The app is closed on both devices. The run starts when you open the app.

## 2 The script

Do the desktop first, then the phone. Do the same six steps on each device.

1. **Start.** Open the app. The home screen shows "Open file", "New part" and the recent files, if any. The phone has no
   recent file yet: it saves no document file.
2. **Find the file.** Select "Open file". The file picker of the system opens. Go to Downloads and select `bracket.stl`.
   - Desktop: the file dialog of Windows, with the filter "STL file".
   - Phone: the document picker of Android. The app asks for no permission of storage.
3. **See the part.** The bracket shows framed: the whole part is in the viewport, with a small margin, and both holes are
   visible. The panel names the body `bracket.stl`, and the line "Size" gives its size in "mm (assumed)".
4. **Turn and zoom.**
   - Desktop: turn with a left drag, zoom with the wheel, move with a middle drag.
   - Phone: turn with one finger, zoom with a pinch, move with two fingers.
   - Then select "Frame", on the row of Undo and Redo, to see the whole part again.
5. **Open the damaged file.** Select "Home" on the same row, then "Open file", and open `bracket-damaged.stl`. A message
   says in words what is wrong and gives the code `E-STL-TRUNCATED`. The message has "Choose another file". The app does
   not close and does not show a new part.
6. **Try again.** Select "Choose another file" and open `bracket.stl`. The bracket shows, framed, as in step 3.

On the phone, also do this between step 4 and step 5: put the app in the background for 10 seconds, and come back. The
bracket is still there and shows whole: the app frames it again from the start view. The turn and the zoom of step 4,
the selection and the line "Size" go at each trip into the background (ADR-0073); turn on "Dimensions" in the panel and
select the body to see its size again.

## 3 Pass criteria

The run passes only when each box is ticked on both devices. A box that fails becomes a correction task with the number of
its step.

| # | Criterion | Desktop | S23 |
|---|---|---|---|
| 1 | The app opens on the home screen, with "Open file" visible with no scroll. | [ ] | [ ] |
| 2 | You find `bracket.stl` in Downloads with the picker of the system, with no help, in less than 30 seconds. | [ ] | [ ] |
| 3 | The whole bracket shows framed in less than 2 seconds after the pick. No part of it is cut by the edge of the viewport. | [ ] | [ ] |
| 4 | Both holes are visible from the first view, or after one turn. | [ ] | [ ] |
| 5 | Turn, zoom and move answer at once, with no jump. Desktop: the mouse. Phone: one finger, a pinch, two fingers. | [ ] | [ ] |
| 6 | "Frame" brings back the whole part from each view. | [ ] | [ ] |
| 7 | The damaged file gives a message in words and a registered code. The app stays open and shows no new part. | [ ] | [ ] |
| 8 | "Choose another file" leads to a good open of `bracket.stl`. | [ ] | [ ] |
| 9 | Phone only: the same part shows whole, framed again, after 10 seconds in the background. | n/a | [ ] |
| 10 | The pipeline opens both files on Windows, Linux, macOS and the emulator with one result (the probe of ADR-0074 since TASK-0258). | [x] | [x] |

## 4 Observation sheet for the S23

Write one row each time you stop, look for something, or do a gesture again. Write it at once, not at the end. A
hesitation is a finding, also when the step passes.

| Step | What I wanted to do | What I did or expected | What happened | Seconds lost | Stuck? |
|---|---|---|---|---|---|
| 2 | Example: find Downloads | Looked for "Downloads" at the top | The picker opened on "Recent"; I had to open the menu | 8 | No |
| | | | | | |
| | | | | | |
| | | | | | |

Look especially at these moments:

- **Finding the file:** where the picker opens, whether `bracket.stl` shows at once, whether the name is clear.
- **Moving the model:** a turn that goes the wrong way, a pinch that also turns, a gesture that selects by mistake, a part
  that leaves the screen.
- **The thumb:** a control that is hard to reach with one hand, or under the S Pen.
- **The message:** whether you understand what went wrong without the code.

After the run, add one line: the one change that would help most.

## 5 Known limits of this run

- The phone has no document folder yet, so it shows no recent file and saves no document (ADR-0083 item 7, TASK-0260).
- The limit of 300,000 triangles is measured on this computer only (ADR-0083 item 6); the S23 is not measured.
- The size of a mesh body shows only with the toggle "Dimensions" on; an open turns it on.
- When Android starts the activity again during a session, the program ends that session quietly (ADR-0079 item 4). The
  emulator showed the end before the window; the end after a lost surface ran on no emulator and no device yet.
- After the background the phone keeps the document only: the view, the selection and the line "Size" start again
  (ADR-0073). Correction of TASK-0269 (finding E174 of the codebase review of 2026-11-07 (b)): until then the sheet said
  that the view stays.
