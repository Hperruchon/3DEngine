---
id: 0032
title: The numeric pad, the expression evaluator and the keys of undo and redo
status: Accepted
topic: Clients, rendering
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0026', '0031']
amended-by: []
affects:
  - 3DEngine.Core/Numeric/**
  - 3DEngine.Core/Input/**
  - 3DEngine.Vulkan/SdlInput.cs
  - 3DEngine/Ui/**
  - 3DEngine/BodyActions.cs
enforced-by: 3DEngine.Tests/ExpressionTests.cs (TASK-0094 creates it)
---

# ADR-0032 — The numeric pad, the expression evaluator and the keys of undo and redo

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R10 gives a numeric pad and an expression evaluator, so that a person can type a value that a drag
gives. The decision sheet of 2026-10-27, part 2, gives the defaults R10-Q1 to R10-Q8. On 2026-10-28 the owner put
the keys of undo and redo (register entry R-0041) into the same phase, because both change the input of the
desktop.

Three facts constrain the design:

- ADR-0007: a value that a person types is a state of the client until a command records it.
- ADR-0026 items 2 and 5: only the intent layer reads a key for the scene, and each intent needs a touch script in
  the parity test. A key for undo therefore needs a touch gesture too.
- The window always receives text input (`Window.cs`), and a text sample goes to the user interface when a text
  field has the focus (ADR-0027 items 4 and 5). The pad can read the typed characters through that path, with no
  new key for a digit.

The decision sheet does not answer three questions. On 2026-10-28 the owner answered A, A and A:

- **Q1, the touch gestures of undo and redo.** A: a tap of three fingers undoes, and a tap of four fingers redoes.
  B: a swipe of three fingers to the left undoes, and to the right redoes. C: no touch gesture; ADR-0026 item 5
  exempts the two intents, and touch uses the buttons. **Recommended: A.** A tap of two fingers already shows each
  hidden body (ADR-0029 item 4), and a tap is the gesture that the machine already measures.
- **Q2, how the pad opens.** A: two buttons in the panel, "New box" and "Move". Each opens the pad with three
  fields: the sizes of a box, or the offsets of the selected body. B: the pad edits one value at a time.
  **Recommended: A.** A box and a move each need three numbers in one command.
- **Q3, the selection after a move.** `Translate` consumes the body and makes a new one. A: the new body becomes the
  selection. B: no body is selected. **Recommended: A.** The person can then move the body again.

## Decision

1. **The evaluator (R10-Q3, R10-Q4).** `3DEngine.Core/Numeric/Expression.cs` evaluates one expression to a finite
   double, or gives a short error text. The grammar:

   ```
   expression = term { ("+" | "-") term }
   term       = unary { ("*" | "/") unary }
   unary      = "-" unary | primary
   primary    = number | "pi" | "sqrt" "(" expression ")" | "(" expression ")"
   number     = digits [ "." digits ] | "." digits
   ```

   A space is permitted between two tokens. The point `.` is the only decimal mark. The evaluator reads each
   number with the invariant culture and uses only `+ - * /`, `Math.Sqrt` and the constant `Math.PI`. An empty
   text, an unknown token, a missing parenthesis, the square root of a negative number and a result that is not
   finite are errors. An expression has 64 characters at most.
2. **The command records the number (R10-Q3).** The client evaluates, and the command holds the result as a
   double. The log never holds the text, so a replay never evaluates. This agrees with ADR-0007: the text is a
   state of the client, as the filter of the panel is. The determinism rules of `CLAUDE.md` bind the code that
   writes the log, and the evaluator is not that code.
3. **The pad state machine (R10-Q2).** `3DEngine.Core/Numeric/NumericPad.cs` holds three fields, each with a name,
   a text and the unit, and the active field. Its inputs are: append a text, delete the last character, clear the
   field, go to the next field, apply and cancel. A button appends the text of its label (`7`, `+`, `pi`,
   `sqrt(`), and a typed character appends the same text, so a tap, a click and the keyboard give the same states.
   The machine accepts only the characters of the grammar. Apply evaluates the three fields and gives three
   numbers, or the error of the first field that fails.
4. **The two pads (R10-Q6, Q2 A).** The panel gets the buttons "New box" and "Move". "New box" opens the pad with
   the fields `x`, `y` and `z`, each with the text `10`, and its apply sends `CreateBox`. "Move" shows only while a
   body is selected. It opens the pad with the fields `dx`, `dy` and `dz`, each with the text `0`, and its apply
   sends `Translate` for the selected body. After a move, the new body is the selection (Q3 A). Sketch dimensions
   come with K2.
5. **The display (R10-Q1).** The pad is a second window of the R7b user interface. It shows the three fields, the
   value of the active field or its error, and the buttons `7 8 9 ( )`, `4 5 6 * pi`, `1 2 3 - sqrt(`, `0 . + /`
   and Delete, Clear, Next, Apply and Cancel. Each control is at least 44 pixels high (ADR-0027 item 6).
6. **The keys of the pad (R10-Q2).** While the pad is open, it takes each key and each text of the frame, and the
   scene gets none. Tab goes to the next field, Enter applies, Escape cancels, and Backspace deletes. A pointer
   gesture outside the panels still moves the camera.
7. **The unit (R10-Q5).** Each value is in the length unit of the document, the millimetre (ADR-0015 item 7). The
   pad shows `mm` beside each field. An expression has no unit suffix.
8. **The errors (R10-Q7).** The pad shows a short text: the error of the evaluator, or the code and the message of
   a rejected command. No new diagnostic code.
9. **The keys of undo and redo (R-0041).** `InputKey` gains `Z` and `Y`, and `SdlInput` gives them. The intent
   layer gains `UndoIntent` and `RedoIntent`: Control and Z undoes, and Control and Y, or Control, Shift and Z,
   redoes. The touch gestures are those of Q1. The desktop applies each intent as the button of ADR-0031 item 11.
   A key reaches the intents only when no text field and no pad has the focus.
10. **This record amends ADR-0026 items 2 and 3** (the two intents and their gestures) **and ADR-0031 item 11**
    (the keys come back, through the intent layer).
11. **Tests (R10-Q8).** Unit tests of the evaluator and of the pad; a parity test of the buttons and the keyboard on
    the pad; the touch and mouse scripts of the two new intents; tests of the panel controls; one image test of
    the pad; and a picture for the owner.

## Consequences

**Good:** A person types an exact size or offset, by touch, by mouse or by keyboard, and an agent already sends the
same commands. The log holds numbers only, so a replay is the same on each platform. Ctrl+Z and Ctrl+Y work, and
R-0041 closes.

**Bad:**
- The drags of the camera (orbit, pan and zoom) get no numeric equivalent. They change the view, not the document.
  The gizmo of R9 brings the first drag that changes the document, and its numeric equivalent is this pad.
- The pad accepts the letters of `pi` and `sqrt` one by one, so a partial word such as `sq` is an error until the
  word is complete.
- No touch screen exists, so the gestures of three and four fingers are proven by synthetic samples only.
- The panel grows by two buttons, and the pad is a second window that can cover the scene.

**Next:** TASK-0094 implements this record and closes R-0041.

## History

- 2026-10-28 · TASK-0094 · Proposed, and accepted by the owner on the same day with Q1 A, Q2 A and Q3 A.
