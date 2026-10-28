# Decision sheet — the path to CAD on one part (2026-10-27)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

**The decision.** On 2026-10-27 the owner chose the recommended answer of each question in part 1 (KD1
to KD6) and the defaults of part 2, with one change: KD1 uses the binding ImGui.NET, because the owner
wants no question about a licence (ADR-0027). TASK-0084 copied this sheet from the scratch folder of
another session into the repository. One sentence of the last paragraph changed, because the marker gate
refuses one of its words with no register entry. `docs/roadmap.md`, ADR-0027 and the first commit of each
later task hold the decisions; this sheet is the analysis behind them.

Four read-only agents prepared this sheet from `origin/main` at `b0e6349` (v0.84). I checked the key
claims on disk. The working session continues P0.31 (TASK-0079) on `host-render-gaps`.

Part 1 holds the six decisions that need the owner. Part 2 holds the defaults for each phase. Part 3
is a block to paste into the working session. It already holds the recommended answers.

---

## Part 1 — Six decisions for the owner

### KD1. How does the application show text and panels? (closes R-0021)

The application draws no text today. No file holds a font, a glyph or a UI library.

| Option | Cost | Touch | Learning |
|---|---|---|---|
| **A** First-party immediate-mode UI on Vulkan | 10–15 evenings | full control | highest |
| **B** Dear ImGui core (cimgui binding), with a first-party Vulkan renderer for its draw data and the existing `SdlInput` | 4–6 evenings | one pointer, large controls | the Vulkan part stays first-party |
| **C** A separate client over HTTP, for example a browser page | 3–5 evenings | good | no Vulkan; you said a browser is "not the main idea" |

**Recommended: B.** Put the UI behind one host folder, so that A can replace it later. Do not use the
library backends: they bring a second SDL binding, and ADR-0017 item 4 permits one pin.

**Not verified:** a binding that supports .NET 10. The stable Hexa.NET.ImGui lists .NET 9 and earlier
(NuGet, checked 2026-10-27). The task must check this first, and stop if no binding works.

### KD2. The order to CAD on one part

| Order | Phase | Evenings | Note |
|---|---|---|---|
| 1 | **R7b** Text and panels (new, from KD1) | 4–6 | R8 needs a text field for a name |
| 2 | **R8a** Name and visibility in the engine | 3–4 | ADR, commands, event, query |
| 3 | **R8b** Name and visibility in the desktop | 4–5 | pick, highlight, intents, text field |
| — | Codebase review (4 product milestones since v0.76) | — | |
| 4 | **P8a** The document file | 3–5 | with KD3 and KD4 |
| 5 | **P8c** Undo and redo | 3–5 | |
| 6 | **R10** Pad and evaluator | 3–5 | text from R7b |
| 7 | **K2** Sketch model | 6–10 | |
| 8 | **K3 slice** Solver with five constraints | 8–12 | KD5 |
| 9 | **K0a** Extrude a polygon over Manifold (new) | 3–5 | KD5 |
| 10 | **K0 slice** The solid follows an edit of the sketch (new) | 6–10 | KD5 |

Total: about 43–67 evenings plus two reviews. At the speed of 2026-10-27 that is 5 to 9 evenings like
it. **R9 (the gizmo) moves after the milestone.** A sketch needs the pad, not a 3D gizmo.

**Recommended: this order.** The first commit of R7b changes the roadmap rows. Then no separate
governance entry counts toward the review limit.

### KD3. A command does not record its backend (a decision and the code disagree)

Anti-objective 10 says: "A command records the backend that performed the operation." `Command.cs`
has no such field. This must be correct before the first saved file. Otherwise a later change must
rewrite each saved file.

- **A** The header of the document file records one backend. This changes the anti-objective, because
  the kernel boundary lets a later operation fall back to Manifold one operation at a time.
- **B** The log entry records the backend of each applied command. The bus writes it. No command
  payload changes.
- **C** Each command payload gets a backend field. Each command shape changes.

**Recommended: B.** It meets the anti-objective as written, and it costs about 1 evening inside P8a.

### KD4. The document file (closes R-0025)

- **A** Keep ADR-0015: one log for each `engine-api-http` process. The desktop gets no file.
- **B** Rewrite ADR-0015: the document file is the command log in JSON Lines. The desktop opens it
  and saves it. Each applied command goes into the file before the result, with fsync. "Save" chooses
  a name only.

**Recommended: B.** The journal design exists in ADR-0015. Undo appends to the log, so nobody rewrites
the log. The header records one length unit for the document (default: millimetre). Each number stays
a double in that unit. P8e then shrinks to that header field and the unit in the pad.

### KD5. Smaller first versions of the kernel work

- **K0a** Approve a phase "extrude a closed profile of line segments over Manifold" before K4. No arcs:
  an arc needs sine and cosine, which is a determinism risk (rule 1). The command records the backend.
- **K0 slice** The extrude references its sketch, and a rebuild makes the solid again after an edit of
  the sketch. This conflicts with the "live bodies at apply" model of ADR-0021, so it needs an ADR.
  Without it, the result is not parametric (objective 11).
- **K3 slice** Five constraints first: coincident, horizontal, vertical, distance and fixed. The
  solver uses only `+ - * / sqrt`. The solver output stays outside the log.

**Recommended: yes to all three.**

**Not verified:** that the Manifold 3.5.2 package exports the extrude function. The build has
cross-section ON (`eng/manifold-native/build.sh:42`), but the binding binds only box, difference and
translate. K0a must check this in its first commit.

### KD6. The camera jumps after each change of a body

`SceneRenderer.cs:44` frames all bodies after each change (ADR-0025 item 6). With that behaviour,
editing is not usable: each rename, hide or move makes the camera jump.

- **A** Keep it.
- **B** Frame only when the set of bodies goes from empty to not empty. Amend ADR-0025 item 6 in R8b.

**Recommended: B.**

---

## Part 2 — Defaults for each phase ("A to all")

### R7b — Text and panels

- The UI code lives in one host folder. No other folder references the library.
- A first-party Vulkan renderer draws the UI: the first texture, descriptor set and 2D pipeline.
- The input comes from `SdlInput` through the intent layer. Touch targets are at least 44 points.
- Tests: a GPU-free test of the UI state, and an image test on lavapipe. The owner accepts a picture.

### R8 — Name and visibility (split into R8a and R8b; 7–9 evenings, not 4–6)

| Question | Default |
|---|---|
| R8-Q1 Visibility | A command in the log. A hidden body still works in each query and command. |
| R8-Q2 Contract | One new event kind `body.changed` with `{bodyId, name, visible}`, after `body.consumed`. `body.created` and the snapshot get `name` and `visible`. One ADR. |
| R8-Q3 Commands | `RenameBody{bodyId, name}`, `SetBodyVisibility{bodyId, visible}`, query `GetBodyInfo{bodyId}`. A command that sets the current value is applied. |
| R8-Q4 Name rules | No name until a rename. Not unique. 1 to 128 UTF-16 units. Control characters refused. No trim, no normalization, no culture rules. Empty string clears. One new code, registered. |
| R8-Q5 Consumed body | The result keeps the name and visibility of the operand: the source of Translate, the first operand of Subtract. |
| R8-Q6 Selection | One body. A ray test against the client triangles, in `3DEngine.Core`. A second pipeline for the highlight. A hidden body is not drawn, cannot be picked, and is not in the frame box. |
| R8-Q7 Input | Rename through the R7b text field. Hide: long press, right click or H. Show all: two-finger tap or Shift+H. Each gesture is a setting. |
| R8-Q8 Tests | CLI and HTTP chains, the replay fixture, inheritance, the event schema, the snapshot, picking, intent parity, and a log comparison of touch and mouse. |

**Note for the ADR of R8a:** a change to a body touches `BodyRecord`, the handler result, the bus and the
snapshot. So R8a cannot follow the rule "a new command changes only `Commands/`" (ADR-0016,
`DispatchSurfaceGateTests`). The ADR must say why, and the gate change must have an injection test.

### R10 — Pad and evaluator

| Question | Default |
|---|---|
| R10-Q1 Display | Through the R7b UI. No separate glyph work. |
| R10-Q2 Entry | Buttons for tap and click, and the real keyboard. One pad state machine. The pad takes the keys while it is open. |
| R10-Q3 Evaluator | In `3DEngine.Core`. The command records the result number only. The ADR says why this agrees with ADR-0007. |
| R10-Q4 Grammar | `.` as the decimal point; `+ - * /`; unary minus; parentheses; `pi`; `sqrt`. |
| R10-Q5 Units | The unit of the document header (KD4). No suffix. |
| R10-Q6 Values | The `dx, dy, dz` of Translate and the sizes of a new box. Sketch dimensions come with K2. |
| R10-Q7 Errors | A short text message in the pad. No new diagnostic code. |
| R10-Q8 Tests | Evaluator and pad unit tests, touch and keyboard parity, one image test, a picture for the owner. |

### R9 — The gizmo (after the milestone; decide now, build later)

| Question | Default |
|---|---|
| R9-Q1 Transforms | Translate only. Rotate gets its own roadmap row (a quaternion or matrix command, and an ADR). |
| R9-Q2 Drag | Preview in the client. One `Translate` at release. Cancel sends nothing. The selection moves to the new body. |
| R9-Q4 Handles | Three axis arrows and three plane squares, 100 window points. |
| R9-Q5 Start of a drag | The host gives `IntentMachine` a hit-test function. Gizmo intents for start, move, end and cancel. A touch script for each handle. |
| R9-Q6 Keyboard | Through the pad of R10. |
| R9-Q7 Scope | One body. The pivot is the centre of its box. The pick comes from R8b. |
| R9-Q8 Pipeline | An overlay pipeline after the scene: no depth, no culling, a colour in the push block. |

### Known gaps that stay

- The touch path stays unverified until a check on a real touch screen (decision D1 of R7).
- The register holds 15 of 15 entries. KD1 and KD4 close R-0021 and R-0025, so two places open.

---

## Part 3 — The block to paste into the working session

```text
Owner decisions of 2026-10-27 (second set): the path to CAD on one part. This order follows TASK-0079 (P0.31) and replaces each later order.

KD1 (closes R-0021): the application shows text and panels with the Dear ImGui core through a cimgui binding. A first-party Vulkan renderer draws its data, with the first texture, descriptor set and 2D pipeline. Input comes from SdlInput through the intent layer. Do not use the library backends: ADR-0017 item 4 permits one SDL pin. The UI code lives in one host folder. First check a binding that supports .NET 10; if none works, stop and ask.

KD2, the order:
1. R7b, text and panels (new phase). Its first commit changes the roadmap: new rows R7b, R8a, R8b, K0a and K0 slice; K3 split into a slice of five constraints and the rest; P8a new text; P8e shrinks; R9 moves after the milestone; a new rule in "Order".
2. R8a, name and visibility in the engine.
3. R8b, name and visibility in the desktop.
4. The codebase review, when the gate makes it due.
5. P8a, the document file. 6. P8c, undo and redo. 7. R10, pad and evaluator. 8. K2, sketch model. 9. K3 slice. 10. K0a, extrude. 11. K0 slice.

KD3: anti-objective 10 is correct and the code is not. Each log entry records the backend of its applied command. The bus writes it. No command payload changes. This lands in P8a, before the first saved file.

KD4 (closes R-0025): rewrite ADR-0015. The document file is the command log in JSON Lines. The desktop opens it and saves it. Each applied command goes into the file before the result, with fsync. Save chooses a name only. The header records one length unit for the document (default millimetre); each number stays a double in that unit.

KD5: approve K0a (extrude a closed profile of line segments over Manifold, no arcs; first check that the native package exports the extrude function), the K0 slice (the extrude references its sketch, and a rebuild makes the solid again; an ADR resolves the conflict with ADR-0021), and the K3 slice (coincident, horizontal, vertical, distance and fixed; only + - * / sqrt; the solver output stays outside the log).

KD6: the camera frames the bodies only when the set goes from empty to not empty. R8b amends ADR-0025 item 6.

Phase defaults: the decision sheet of 2026-10-27, part 2, at C:\Users\Henri\AppData\Local\Temp\claude\C--Users-Henri-source-repos-3DEngine\e9250e4e-3400-4586-b093-47e1a5da99b2\scratchpad\decision-sheet-2026-10-27.md, gives each default for R7b, R8 (R8-Q1 to Q8), R10 (R10-Q1 to Q8) and R9 (R9-Q1 to Q8). Apply each default. Record the decisions of a phase in the first commit of its task, as an ADR where a contract changes. R8a needs an ADR for the new event kind and for its exception to the rule of DispatchSurfaceGateTests, with an injection test.

Merge each task after a green run on the three runners, without asking. Stop at each stop-and-ask condition of CLAUDE.md, and for each visual check. Update the batch memory with this order.
```

The block gives the full path of this file, so the working session can read Part 2. It may ask you for
permission first, because the file is outside the repository. The file goes away later: the first commit
of each task must copy its decisions into the repository.
