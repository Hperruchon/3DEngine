# The model of placements for P11.0

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-27. TASK-0474, phase P11.0. Written under the standing order of 2026-11-26.
- Status: a proposal. Not a rule. It changes no code, no contract and no ADR. The ADR of P11.0 comes after the answers of the
  owner, in the same commit as the change of `Engine.Contracts`.
- Why a proposal first: the model changes the public shape of `Engine.Contracts` (`BodyRecord`, a new command), and
  `CLAUDE.md` ("Stop and ask") asks the owner before such a change. The owner answered the direction on 2026-11-25 (Q1 A); the
  questions in section 5 are the details that the direction does not decide.

## 1 The question

On 2026-11-25 the owner answered Q1 A: P11.0 decides a model of placements. Each body keeps its shape in its own frame, and a
joint or a move changes only a stored placement, a rotation matrix and a translation, so the exact numbers do not grow. This
document gives the facts, a recommended model, and the questions that the ADR must answer.

## 2 The facts today

Each fact below was read in the repository on 2026-11-27.

1. **A move makes a new body.** `Rotate` (ADR-0112) and `Translate` make a new body with the handle of the command and consume
   the source body (`Engine.Core/Commands/RotateCommandHandler.cs`: `createdBodies` with `new BodyHandle(rotate.CommandId)`,
   `consumedBodies` with the source). So a part has a new identity after each move, and the chain of consumers grows by one step
   for each move (ADR-0112 item 6).
2. **The owned kernel turns the shape exactly.** `KernelSolid.Rotate(x, y, z)` maps each polygon with the images of the three
   axes as exact rationals (`Engine.Kernel/KernelSolid.cs`). Each turn multiplies the rationals, so their size grows with each
   turn.
3. **The measure of K10a.** 150 turns of a module of the bookcase (450 commands) gave a coordinate with a numerator of 2576
   digits. The first read of the solid after the turns took about 2 s. Each length changed by less than 1e-12 mm. The commands
   took 40 to 46 ms, and a replay of 507 commands took 17 to 19 ms (`docs/proposals/2026-11-21-the-bookcase-case-of-k10.md`,
   R-0070). So the cost is in the kernel, not in the log.
4. **The record of a body.** `BodyRecord` (`Engine.Contracts/BodyRecord.cs`) holds the handle, the kind, the name, the
   visibility, the key of the geometry and the fault. It has no placement.
5. **The turn.** `Engine.Contracts.Geometry.Rotation` holds the nine numbers of a turn, which come from the four numbers of a
   quaternion with `+ - * /` only (ADR-0112 items 2, 3 and 8). The log holds the quaternion and no angle (`CLAUDE.md`, rule 2).
6. **The file.** A document file is a header line and then the log of the commands, one entry on each line
   (`Engine.Core/Persistence/DocumentFile.cs`, `EntryCodec.cs`). An open applies each entry again. So the placement of a body is state that the commands make, and a file needs no new section.
7. **The desktop.** The window reads the mesh of a body with `GetTessellation` (`3DEngine.App/SessionBodies.cs`) and projects it
   into `3DEngine.Core` (ADR-0009). A tessellation is presentation (ADR-0007).

## 3 The recommended model

1. **A placement.** A body has a placement: a turn and an offset. A point `p` of the shape of the body is at `R·p + t` in the
   Document, where `R` is the turn and `t` is the offset. The identity is the placement of each body that exists today, so
   each old Document has the same geometry.
2. **The stored form.** The record holds the four numbers of a quaternion and the three numbers of the offset, seven doubles.
   The nine numbers of the turn come from the quaternion as ADR-0112 item 3 says. The log holds the same seven numbers, so a
   replay gives each placement bit for bit on each platform (anti-objective 16).
3. **The new command.** `PlaceBody` with `bodyId` and the seven numbers sets the placement of a body. It is absolute: the
   command holds the new placement, not a change of it. So a joint, a drag and an agent give the same numbers, a repeated
   command gives the same result, and no error grows over many moves. The body keeps its handle and its shape; the step
   records the record before and after (`Changed`), so `Undo` and `Redo` work as for a rename.
4. **`Rotate` and `Translate` keep their meaning.** An old log replays with the same result. The two commands still make a new
   shape, for a client that wants to change the shape itself, for example before a `Subtract`.
5. **The exact numbers stay small.** The kernel keeps the shape of each body in its own frame. A read of one body in the
   Document frame, or a check of two bodies, applies one placement to the exact shape one time, at the read. The placement of
   the second body in the frame of the first is computed in doubles with `+ - * /` first. So the size of the rationals grows by
   one product per read, and not by one product per move.
6. **The booleans.** `Subtract`, `Union` and `Intersect` of two bodies with other placements give a result in the frame of the
   first body, with the placement of the first body. The tool goes into that frame with the placement of the tool in the frame
   of the target. The result is a new body, as today.
7. **The readers.** Each query that gives a point, a face, a distance or an angle in the Document frame applies the placement
   to its result with `+ - * /` only. `GetTessellation` gives the mesh in the frame of the body and the placement, and the
   window draws the mesh with the placement as its model matrix; a tessellation stays presentation (ADR-0007). The export to STL
   and the views of a sheet apply the placement to each point.
8. **The joints of P11.** A joint gives the placement of the child from the placement of the parent and the value of the
   joint (a cosine and a sine for a pivot, a length for a slider), with `+ - * /` only. The log holds the value of the joint;
   the placement of the child is the result. A drag gives one command at its end, never one for each frame (ADR-0126 item 3).

## 4 What the model costs

- `BodyRecord` gets a new field. With a default of the identity, each existing constructor call and each old file stays
  valid, but the field is a new part of the public shape, so the ADR comes with it.
- Each reader of the geometry of a body must apply the placement: the queries, the checks of interference, the measures, the
  sketch on a face, the datums, the section, the views of a sheet, the export to STL, and the desktop. A reader that forgets
  it gives a wrong result for a placed body and the right result for each other body, so each reader needs a test with a body
  that is not at the identity. The ADR must give that list, and a gate can hold it.
- A version of the app before the change refuses a file that holds `PlaceBody`, because it does not know the command. This is
  the cost of each new command.
- The fork of a preview (ADR-0188) and the kept solids of a fork (E243, TASK-0473) read a solid by its key. The placement is in
  the record and not in the solid, so a move changes no key, and the cache of the solids keeps its entries after a move.

## 5 The questions for the owner

1. **Q1 (the command).** How does a client move a body? **A (recommended):** a new command `PlaceBody` with an absolute
   placement, and the body keeps its handle. **B:** `Rotate` and `Translate` get a schema version 2 that changes the placement,
   and version 1 keeps its meaning. **C:** a relative command `MoveBody` that composes a change with the placement.
2. **Q2 (the stored form).** **A (recommended):** a quaternion and an offset, seven doubles; the nine numbers come from the
   quaternion. **B:** the nine numbers of the turn and the offset, twelve doubles, with a check that the turn is a turn.
3. **Q3 (the frame of a boolean).** **A (recommended):** the frame and the placement of the first body. **B:** the Document frame
   with the identity: each boolean turns both shapes, as today, so the numbers grow at each boolean.
4. **Q4 (the mesh body of an STL file).** ADR-0112 item 1 says that a mesh body does not move. **A (recommended):** a mesh body
   also gets a placement, because a placement changes no point of the mesh. **B:** a mesh body keeps the identity.
5. **Q5 (the old moves).** **A (recommended):** `Rotate` and `Translate` keep their meaning and stay in the catalogue. **B:** they
   stay for the replay of old files only, and a new client gets a refusal with a new diagnostic code.
6. **Q6 (the order of the work).** **A (recommended):** P11.0 is the ADR with the field, the command and the test of each reader
   in one commit; the joints (P11a and after) follow. **B:** P11.0 is the ADR only, and a row P11.0b does the code.
7. **Q7 (the measure).** **A (recommended):** the task of the code runs the case of K10a again with `PlaceBody` and gives the
   numbers (the digits of a coordinate, the time of the first read) in its ledger entry. **B:** no new measure.

## 6 Out of this document

- The connectors, the kinds of joint, the limits, the states tight and loose and the refusal of a loop: P11a and after
  (ADR-0157 items 3 to 5).
- A tool of the window that moves a body.
