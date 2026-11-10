# A UX test of a project of many pieces, and three improvements

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-10.
- Asked by: the owner, on 2026-11-10: test the window on a real project of many pieces, as a person who opens it for the
  first time, before reading the code; give the problems by impact and at most three improvements, each with a scenario
  that checks it. The owner added: the ring is under-used for the actions on a model, and a hidden piece needs a way back.
- Status: accepted by the owner on 2026-11-10 ("All three"). It changes no code and no ADR. The improvements are the
  roadmap rows R20a, R20b and R20c. The session that opens a later row gives it its task number and its ADR number at
  that time (gate P0.80).

## 1. The test

- **The project.** A copy of `Bibliotheque120.3de` (the folder `Documents/3DProjects/Bibliotheque120`): 19 bodies, a niche
  of five panels (Cote gauche, Cote droit, Haut, Bas, Fond) in the middle of modules of indicative volume. It is the only
  project of 19 bodies on the computer of the owner. The original file did not change.
- **The window.** `main` at v0.324 (6e8d1a7), 1280 by 720, with a mouse.
- **The four steps.** (1) Open the project, put the niche in front of the eye, find the modules. (2) Select a panel of
  the niche and show its dimensions; say at once if a value is of the panel or of the whole. (3) Go to the back of the
  niche, then to a neighbour module, with the dimensions on; count each return to the side panel. (4) Turn and zoom with
  a piece selected; check if the ring, the history arc or the dimensions cover the model or take a tap meant for a
  neighbour piece.
- **The pictures.** The session kept them out of the repository. The owner received them on 2026-11-10.

## 2. The problems, by impact

Each problem has a type: **Hidden** (the function exists, and a person does not find it), **Impossible** (the action
cannot start from the 3D view), or **Overlap** (one element covers another).

1. **The ring takes a tap meant for a neighbour piece (Overlap, Impossible).** With "Fond" selected, a click on the
   module to its right, under the band of the ring, opened the pad of Move of "Fond". The band keeps its size on the
   screen, so a zoom puts it over more neighbours. ADR-0081 says: "The band of the ring is a part of the panels for a
   tap".
2. **The dimensions are hard to find (Hidden).** The ring has no Dimensions, and the panel of a selected body has none.
   The box "Dimensions" (ADR-0056) is at the end of the list of the bodies, with nothing selected, after the 19 rows,
   the buttons of the files and "Sketch on". The session found it through the command line, with the word "dim".
3. **A value does not say what it measures.** The dimensions of the whole show in orange, the colour of the selection;
   the dimensions of the selected panel show in white; no label names a piece or says "overall". The panel shows the
   clear answer, "Size 18 x 400 x 800 mm", in small grey text. A cube of 400 shows "Thickness 400 mm".
4. **The labels cover each other (Overlap).** The labels of the selected body have no plate and lie on the edges and on
   the arrows of the gizmo; "Y 400 mm" of the whole is under the status line; the ring and "TOOLS FOR FOND" cover
   "Z 2350 mm"; the history arc and its nodes lie on the model and on "X 3400 mm".
5. **A hidden piece is hard to bring back (Hidden, Impossible).** Hide on the ring works, but it ends the selection and
   the list comes back over the model. The status line still says "19 bodies". The only way back is "Show all" at the
   end of the list, and it shows each hidden body.
6. **The niche does not come in front of the eye in one step (Hidden, Impossible).** No view of the front exists; Frame
   does not fill the free part of the scene; a drag of about 90 pixels turns the model by about 90 degrees; the wheel
   over the panel scrolls the list. Six drags put the niche in front. The flat view of the decision of 2026-11-10 on the
   drawing (Q5 A) answers it.
7. **The panels cover the model (Overlap).** With nothing selected, the list (about 440 pixels wide) covers the left
   modules; with a selection, "Cut with" and "Images" lie on the model. R18g and R18j look at the fixed controls and at
   this size.

Step 3 kept the dimensions on through the changes of the selection. It needed the side panel or the command line one
time to show the dimensions and one time to bring back a hidden piece.

## 3. The improvements

### A. The ring does not take a tap meant for the model (problem 1) — R20a

Only the icon and the label of a segment take a tap. A tap on a body under the rest of the band selects that body. The
ring fades while the view turns or zooms. This amends ADR-0081.

**Scenario.** Select "Fond", zoom three steps of the wheel, then click the module to the right of the niche under the
band, away from an icon and a label. The module is selected and no pad opens. A click on the icon of Move still opens
the pad of Move.

### B. The actions on the model are on the ring (problems 2 and 5) — R20b

"Dimensions" is a segment of the ring of a body, with the state of a tool in use while it is on. After a Hide the
selection stays on a neighbour or on nothing, and the status line shows "N hidden · Show". With nothing selected, the
ring of the document has "Show hidden", which lists the hidden bodies and shows one or each.

**Scenario.** Open the project. Select a panel and turn on Dimensions on the ring. Select the back, then a module, and
hide the back. Bring it back from the status line. The side panel never opens.

### C. Each dimension says what it measures and no element covers it (problems 3 and 4) — R20c

The dimensions of the selected piece have the colour of the selection and its name ("Cote gauche 18 × 400 × 800").
The dimensions of the whole have a neutral colour and the word "Overall". Each label has a plate, as a chip of a value,
outside the silhouette of the body, and no label goes under the status line, the ring or the history arc. A body that
is not a board shows no thickness. This amends ADR-0056. It comes after R18c, which changes the values on the edges.

**Scenario.** At 1280 by 720 with Dimensions on, select "Cote gauche". Each label can be read and no element covers it.
A person says in less than two seconds which value is of the panel and which is of the whole.

## 4. Out of this document

- Problem 6 waits for the flat view (the drawing decisions of 2026-11-10, Q5 A).
- Problem 7 is in the slices R18g and R18j.
- A test with a finger on the phone. This test used a mouse only.
