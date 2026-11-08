# The selection of the document and the selection of one body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Status: the owner answered the questions on 2026-11-08 (section 4, "The answers of the owner"). This file is still
  not a rule: the ADR of the task that uses it records each answer.
- Date: 2026-11-08, at `main` 18297b8 (v0.297).
- Request of the owner: the window has no way to tell the selection of the whole document (the part or the
  assembly that the person works on) from the selection of one body or component that the person wants to review.

## 1. What the engine and the window hold today

### 1.1 The engine (design truth)

| Item | What exists | Source |
| --- | --- | --- |
| Document | `Document.Kind` is `part` or `assembly`, fixed when the Document is made. A part holds bodies, sketches, images and datums. | ADR-0071 item 1, `Engine.Contracts/Document.cs` |
| Body | `BodyRecord`, one for each live body of a part. The client reads `Id`, `Kind`, `Name`, `Visible` and `Fault`. A part can hold many bodies. There is no group and no folder of bodies. | `Engine.Contracts/BodyRecord.cs`, `3DEngine.App/SessionBodies.cs` |
| Reference of an assembly | `PartReference(Id, Part, Version, X, Y, Z)`: the SHA-256 name of the part file in the store, a count of log entries, and an origin. The engine does not resolve it: no read of the part, no bodies of the part in the assembly, no rotation. | ADR-0071 items 3 and 7, `Engine.Contracts/Assembly/PartReference.cs` |
| Visibility | A hide or a show is the command `SetBodyVisibility`. It goes into the log and into Undo. | ADR-0029, `Engine.Core/Commands/SetBodyVisibilityCommandHandler.cs` |
| Selection | None. The engine has no selection. The selection is a state of the client (ADR-0007, ADR-0029 item 1). | `3DEngine.App/NativeThreeDEngine.cs` line 77 |

The charter lists "Multi-Document in one runtime" and "Assemblies" as non-goals (`docs/CHARTER.md`, "Non-goals —
deferred"). So today a "component" of an assembly is data only: the window cannot draw it, and it cannot select it.

### 1.2 The window (`3DEngine.App`)

The selection is in `3DEngine.App/BodyActions.cs`. It has three levels, and each level is a field:

1. `Selection`: one body, or null.
2. `Face`: one face of the selected body, by its name, or null (ADR-0075 item 2).
3. `Edge`: one edge of the selected face, by the pair of the names of its faces, or null (ADR-0077 item 3).

The rules of a tap and of Escape (`BodyActions.Apply`, lines 69 to 101):

- A tap on a body that is not selected selects it.
- A tap on the selected body selects the face under the point.
- A tap near an edge of the selected face selects that edge.
- A tap on no body clears the selection.
- Escape goes from the edge to its face, and from the face to its body. Escape on a body does nothing: the
  `CancelIntent` falls to `default` and the body stays selected.

What the window shows for each state:

| State | Landscape display | Portrait display (phone) |
| --- | --- | --- |
| Nothing selected | The top bar: the program, the file or "unsaved", the unit (ADR-0093). The panel: the list "New", the toggles of the dimensions, "N live bodies" and one row for each body. | The panel, as on a landscape display. |
| A body selected | Today: the rows of the body at the upper right (ADR-0084), and the panel with the body row marked. In flight: TASK-0313 (branch `r16p-body-rows-left`, ADR-0099 planned) puts the rows at the upper left **in the place of the panel**. | The panel, with the controls of the selected body (ADR-0084 Q2 A). |
| A face or an edge selected | One more line in the rows or the panel. | One more line in the panel. |

### 1.3 The gap

- **No state "the document is the subject".** "Nothing selected" is the only document state. It shows the list of the
  bodies, but it does not show the data of the document as data: the kind, the count of bodies, the faults of all
  bodies, the version. The top bar shows the file name only, and the top bar takes no tap.
- **No path up from a body.** Escape stops at the body. A tap on the empty space is the only way back, and on a
  full display a person can miss the empty space.
- **After TASK-0313 the document leaves the display.** While a body is selected, the rows of the body replace the
  panel, so the list of the bodies is not visible. The person sees one body and does not see where it is in the
  document.
- **No focus on one body.** To review one body, the person must hide each other body. Each hide is a command in the
  log and a step of Undo, and "Show all" afterwards shows bodies that were hidden before on purpose.
- **No component.** The assembly level does not exist in the window (ADR-0071 item 4: "The desktop makes parts
  only"). I did not verify what the window shows when it opens an assembly file.

## 2. How three tools show the difference

The sources are at the end. I did not use the applications; each statement comes from their help pages or their
forums.

### Onshape

- A document holds tabs. A Part Studio tab and an Assembly tab are separate places. So "the whole document" and "one
  part" are separate tabs, not separate selections.
- In an assembly, the Instances list gives each part instance, group and mate.
- A double click on a part in the Instances list opens an edit in context: Onshape opens the Part Studio and shows the
  assembly dimmed around it. The command "Exit edit in context" goes back.
- Isolate (Shift and I) keeps the selected instances and makes the others transparent. It is also in a Part Studio.
- I found no help page that describes a breadcrumb.

### Shapr3D

- Shapr3D has no assemblies today. Its forum says that assemblies are planned, with no date.
- The Items Manager is a side list of each sketch, body and plane. Folders group items. "Reveal in Items" on a selected
  face, body or sketch scrolls the list to its entry, so the list shows where the selection is.
- Isolate shows the selected items only, and the person leaves Isolate when done.
- Forum posts describe a double tap that selects a whole body. I did not find this in the help pages.

### Plasticity

- Plasticity has no assemblies. The Outliner lists the objects in groups (Control and G).
- Selection modes choose the level: control point, edge, face, solid. The keys 1 to 4 are from an unofficial
  reference; I did not verify them.
- "Isolate selection" (the key period) exists. The official page and an unofficial page disagree on what it does:
  collapse the Outliner to the group of the object, or hide the other objects.

### What the three tools share

1. **A list of the document stays visible** while one item is selected, and the list marks the selected item.
2. **A way up**: a tab, a command or a mode returns from one item to the whole.
3. **Focus is a view state**, not an edit: Isolate does not change the model, and it ends with one action.
4. **"Go into a component"** exists only where assemblies exist (Onshape), and it opens a second place (the Part
   Studio), with the assembly dimmed around it.

## 3. Options for the window and the phone

Each option is presentation only (ADR-0007), except option C. A size is in the unit of the roadmap column "Size".

### Option A — A path line from the document to the selection

- A path line at the upper left, as the first line of the top bar: `bracket.3de › Body 2 › Face top`. With nothing
  selected it shows the document only.
- A tap on a segment of the path selects that level: a tap on the file name clears the body and shows the document
  rows; a tap on the body name goes back from the face to the body.
- Escape goes one level up and does not stop at the body: edge, face, body, document.
- **Document rows**: with nothing selected, the rows of ADR-0084 show the document: the kind, the unit, the count of
  bodies, the count of failed bodies with the code, then one row for each body. A tap on a body row selects it.
- Phone: the path line is the first line under the top bar, in portrait too. A tap on a segment acts as on the
  desktop. The panel stays below it.
- **Cost:** 1–2. One task in `3DEngine.App/Ui` and `BodyActions.cs`. No contract change, no engine change.
- **ADR changes:** one new ADR. It amends ADR-0093 item 1 (the top bar takes a tap), ADR-0075 item 2 and ADR-0077
  item 3 (Escape goes up to the document), and ADR-0084 or ADR-0099 (the rows with no selected body).

### Option B — Option A, and a focus on one body

- All of option A.
- **Focus**: a client state, not a command. The other bodies are dimmed, or not drawn, while the focus lasts. The
  log, Undo and the visibility of each body do not change.
- The person enters the focus by "Focus" on the tool ring, by "Focus" in the command line (ADR-0098), or by a double
  tap on the body. The path line shows `Focus · Body 2` with a close mark. Escape or the close mark ends the focus.
- A double tap needs a new gesture in `3DEngine.Core/Input/IntentMachine.cs`; today a second tap on the selected body
  selects a face (ADR-0075 item 2). The ring button and the command line need no new gesture.
- Phone: "Focus" is a button of the thumb ring, and the path line shows the close mark.
- **Cost:** 2–3. Option A, then one task for the focus. "Not drawn" uses the projection of the scene; "dimmed" needs
  a pass with transparency in the renderer and a picture test.
- **ADR changes:** the ADR of option A, and one new ADR for the focus: a client state, its entry, its exit, and the
  rule that a focus writes nothing to the log. If it adds the double tap, it amends ADR-0075 item 2.

### Option C — Components of an assembly that the window can enter

- The engine resolves a reference of an assembly: it reads the part from the store, replays it, and gives its bodies
  with the origin of the reference.
- The window shows an assembly with one row for each reference. A double tap or "Open" on a reference enters it: the
  part opens with the other references dimmed, as an edit in context in Onshape. The path line shows
  `frame.3de › Bracket (v12) › Body 2`.
- Phone: the same path line; "Open" on the thumb ring.
- **Cost:** 5–8 or more. It is the next slice of P8b and more.
- **ADR changes:** a decision of the owner on two non-goals of the charter ("Multi-Document in one runtime" and
  "Assemblies"); a new ADR for the resolution of a reference (ADR-0071 "Next"); a change of `Engine.Contracts` for
  the bodies of a reference and their place (CLAUDE.md, "Stop and ask"); the rotation of a reference as a matrix
  (determinism rule 2); new events or a new query; and the ADRs of options A and B.

### Recommendation

Do option A now, then the focus of option B. Keep option C until the owner decides on the two non-goals. Options A
and B make the path line and the focus that option C needs, so no work is lost.

## 4. Questions for the owner

| # | Question | Options | Recommended |
| --- | --- | --- | --- |
| Q1 | What is a "component" for you now? | A. A body of a part. B. A part of an assembly, by its reference. | **A.** The window has bodies now. A reference needs option C and two non-goals. |
| Q2 | Does Escape go up from a body to the document? | A. Yes, one level each time. B. No, Escape stops at the body as today. | **A.** It gives one rule for each level, as the path line shows. |
| Q3 | With nothing selected, what shows at the place of the rows? | A. The document rows: kind, unit, counts, faults, one row for each body. B. Nothing, as today. | **A.** It is the "document selected" state that the request asks for. |
| Q4 | How does a focus show the other bodies? | A. Not drawn. B. Dimmed. | **A first.** It needs no new pass in the renderer. "Dimmed" can come later, as in Onshape. |
| Q5 | How does a person enter a focus? | A. The ring and the command line. B. Also a double tap on the body. | **A.** A double tap conflicts with the tap that selects a face (ADR-0075 item 2). |
| Q6 | Does TASK-0313 (the rows at the upper left, in flight) wait for this? | A. No, it merges first. B. Yes. | **A.** Option A adds a line above those rows and does not replace them. |
| Q7 | Option C | A. Wait for your decision on the non-goals. B. Open it now. | **A.** ADR-0071 "Next" already names this decision. |

### The answers of the owner

On 2026-11-08 the owner accepted each recommended answer: Q1 A, Q2 A, Q3 A, Q4 A, Q5 A, Q6 A, Q7 A. So:

1. Option A comes first: the path line, Escape up to the document, and the document rows with nothing selected.
2. The focus of option B comes next. It is a state of the client, the other bodies are not drawn, and the person enters
   it from the ring or the command line, with no double tap.
3. TASK-0313 merges first, as it is.
4. Option C waits for the decision of the owner on the two non-goals.

On 2026-11-08 the owner put options A and B on the task list: R17a (TASK-0314) and R17b (TASK-0315), in
`docs/roadmap.md`.

## 5. The register

The register holds 15 open entries of 15 (`docs/register.md` item 4; the ledger entry v0.297). It has no room, so this
proposal adds no entry. The questions stay in section 4 of this file.

## 6. What I did not verify

- What the window shows when it opens an assembly file.
- The behaviour of the three tools in the applications. Each statement comes from their help pages and forums.
- The final form of TASK-0313 and ADR-0099. I read its commit message on branch `r16p-body-rows-left` (e3dfec3) only.

## Sources

- Onshape, Isolate: https://cad.onshape.com/help/Content/View/isolate.htm
- Onshape, Instances list: https://cad.onshape.com/help/Content/Assembly/instances_list.htm
- Onshape, Modeling in context: https://cad.onshape.com/help/Content/Assembly/modeling_in_context.htm
- Onshape forum, double click in the Instances list: https://forum.onshape.com/discussion/comment/51372
- Shapr3D, Items Manager: https://support.shapr3d.com/hc/en-us/articles/7873936188956
- Shapr3D, Section view and isolate: https://support.shapr3d.com/hc/en-us/articles/13079202547868
- Shapr3D forum, assemblies: https://discourse.shapr3d.com/t/assembly-in-2021/16480
- Plasticity, Outliner: https://doc.plasticity.xyz/plasticity-essentials/outliner
- Plasticity, unofficial reference: https://plasticity-document-revisions.vercel.app/plasticity-essentials/plasticity-interface/outliner
