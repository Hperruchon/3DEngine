# The loop of an agent: read, measure, propose and check

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This guide is for an agent that works on the Document of a person through the HTTP surface. It gives the loop of four
steps, the exact request of each step, the shape of each answer, the refusals and the limits. Row P17 (TASK-0439) wrote it,
and ADR-0157 item 13 asks for it. The guide covers the HTTP surface only; the command line comes later.

A test keeps the guide true. `Engine.Tests/Guides/AgentGuideExamplesTests.cs` reads this file, sends each example request
to a host in the order of the guide, and compares each answer with the shape under it. When a request of the guide is
refused, or an answer does not have its shape, the test fails.

## 1 The rules

- **The person decides.** An agent proposes a change with `POST /proposals`. Only the person accepts or rejects it, in the
  window of the desktop. No route accepts or rejects a proposal, so an agent cannot accept its own proposal (ADR-0092).
- **Nothing enters the log before the person accepts.** A proposal waits in a queue of the host, outside the log. Each
  query reads the Document and changes nothing.
- **Do not change the Document of a person with `POST /commands`.** That route applies a command at once, with no accept.
  Use it only on a Document that is yours, for example a host that you started for your own work.
- **An accept of N commands gives N steps of undo.** The apply stops at the first command that the engine does not apply.
  The commands before it stay applied.
- **Each length is in millimetres.** An answer gives a number with no unit (register entry R-0053).
- **A turn is a quaternion, never an angle.** The command `Rotate` takes `qw`, `qx`, `qy` and `qz`. If you think in
  degrees, compute the sine and the cosine yourself, and send the numbers (determinism rule 2 of `CLAUDE.md`).
- **Give each command a new identifier.** The identifier `commandId` is a UUID. The body that a command makes has the
  identifier of that command, so a later command of the same proposal can name it. The session keeps the last 1024
  identifiers: a command with a known identifier does not apply again, and gives the earlier answer.
- **Guard the first command only.** The field `expectedDocumentVersion` refuses a command when the Document has another
  version. Each applied command adds 1 to the version, so put the field on the first envelope of a proposal only.

## 2 How to read the examples

Each request is a block `http agent-request`. Its first line gives the method and the route. Header lines follow, then an
empty line and the body. Each POST needs the header `Content-Type: application/json`.

The block `json agent-result` under a request gives the shape of the answer. The answer holds each member of the shape,
with the same value. The answer can hold more members. An array of the shape gives items that the answer holds in the
same order, with other items between them. An empty array of the shape needs an empty array. A block `text agent-result`
gives the start of an answer that is not JSON.

A text between `<` and `>` is a placeholder. The first time that a placeholder occurs in an answer, it takes the value of
the answer at that place; after that, each answer must give the same value. A placeholder in a request is that value. A
placeholder that no earlier answer gave is a new UUID that you make. For example `<Bas>` is the identifier of the body
"Bas"; your identifiers are different. A placeholder that is the whole JSON string `"<version>"` stands for the value of
the answer with its type, so in your request the version is a number with no quotes.

A block `http agent-refusal` is a request that the host refuses, and its `json agent-result` gives the code. A block
`text agent-person` is an action of the person in the window, not a request.

## 3 The example document

The example is the bookcase `Bibliotheque120` of the owner, the first case of K10 (`Engine.Tests/Assembly/BookcaseCase.cs`).
It has 19 bodies: 14 modules and an open case of 5 panels, "Cote gauche", "Cote droit", "Bas", "Haut" and "Fond". The two
shelves "Bas" and "Haut" are 764 mm apart. The Document also holds one sheet with a front view of the bookcase; `<sheet>` is
its identifier.

The task of the agent: add a middle shelf, "Milieu", at the same distance from "Bas" and from "Haut".

The desktop serves the surface on `http://127.0.0.1:5180` by default. The program `engine-api-http` serves it too, but it
has no window, so no person accepts a proposal there.

## 4 Find the surface

The surface describes itself. `GET /schema/commands` and `GET /schema/queries` give each name and version.
`GET /schema/commands/{name}@{version}` and `GET /schema/queries/{name}@{version}` give the parameters of one of them.
`GET /schema/diagnostics` gives each code of a refusal, and `GET /schema/events` gives each kind of event.

```http agent-request
GET /schema/queries/GetDistance@1
```

```json agent-result
{
  "name": "GetDistance",
  "schemaVersion": 1,
  "parameters": {
    "bodyIdA": { "type": "guid", "required": true },
    "bodyIdB": { "type": "guid", "required": true }
  }
}
```

## 5 Read

Read the version of the Document first. Keep it: the first command of your proposal uses it as a guard.

```http agent-request
GET /document
```

```json agent-result
{ "lengthUnit": "mm", "version": "<version>" }
```

Then open the WebSocket `GET /events` and send `{}` as the first frame. The first message is a snapshot of the Document:
each live body with its identifier `handle`, its kind and its name, in the order of the Document. After the snapshot, the
stream gives each event of each applied command, for example `body.created`. To continue after a break, send
`{ "documentId": "<document>", "lastSeenSeq": n }` with the last `seq` that you read. When the host cannot continue the
stream from that event, it sends a new snapshot.

```http agent-request
GET /events

{}
```

```json agent-result
{
  "kind": "subscription.reset",
  "documentId": "<document>",
  "snapshot": {
    "version": "<version>",
    "bodies": [
      { "handle": "<Cote gauche>", "name": "Cote gauche", "visible": true },
      { "handle": "<Cote droit>", "name": "Cote droit", "visible": true },
      { "handle": "<Bas>", "name": "Bas", "visible": true },
      { "handle": "<Haut>", "name": "Haut", "visible": true },
      { "handle": "<Fond>", "name": "Fond", "visible": true }
    ]
  }
}
```

The query `GetBodyInfo` gives the kind, the name and the visibility of one body. Each query has the same envelope: a
name, a version of the schema and the parameters. The answer gives `result`, and `error` is null.

```http agent-request
POST /queries
Content-Type: application/json

{ "name": "GetBodyInfo", "schemaVersion": 1, "parameters": { "bodyId": "<Bas>" } }
```

```json agent-result
{
  "queryName": "GetBodyInfo",
  "result": { "bodyId": "<Bas>", "kind": "Solid", "name": "Bas", "visible": true },
  "error": null
}
```

## 6 Measure

`GetDistance` gives the minimum distance between two bodies, with a point of each body at that distance. It also takes a
face or an edge of each body; `GetFaces` gives their names.

```http agent-request
POST /queries
Content-Type: application/json

{
  "name": "GetDistance",
  "schemaVersion": 1,
  "parameters": { "bodyIdA": "<Bas>", "bodyIdB": "<Haut>" }
}
```

```json agent-result
{
  "result": { "distance": 764, "pointA": { "z": 618 }, "pointB": { "z": 1382 } },
  "error": null
}
```

The top of "Bas" is at z = 618 and the bottom of "Haut" is at z = 1382. The middle of the gap is at z = 1000.

`GetMassProperties` gives the volume in mm³ and the centre of a body. When `facets` is true, a face of the body is curved
and the values are those of the plane facets, not of the curve.

```http agent-request
POST /queries
Content-Type: application/json

{ "name": "GetMassProperties", "schemaVersion": 1, "parameters": { "bodyId": "<Bas>" } }
```

```json agent-result
{
  "result": { "volume": 8255088, "centre": { "x": 300, "y": -3, "z": 609 }, "facets": false },
  "error": null
}
```

So "Bas" is 1164 by 394 by 18 mm, with its centre at x = 300 and y = -3. The new shelf gets the same size, with its centre
at (300, -3, 1000). `GetAngle` gives the cosine and the sine between two plane faces, when you need an angle.

`GetConnectors` gives the places on a body that a later step can name: each named datum of the body, and one entry for
each face, with the name that `GetFaces` gives. A plane face gives `point`, the centre of its area, and `direction`, its
outward unit normal. The whole side of a cylinder gives a point of its axis, the direction of the axis, and `radius`.
Another curved face gives its `kind` only. "Bas" has no datum, and its top face `+z` is at z = 618:

```http agent-request
POST /queries
Content-Type: application/json

{ "name": "GetConnectors", "schemaVersion": 1, "parameters": { "bodyId": "<Bas>" } }
```

```json agent-result
{
  "result": {
    "datums": [],
    "faces": [
      {
        "reference": { "element": "+z" },
        "kind": "Plane",
        "point": { "x": 300, "y": -3, "z": 618 },
        "direction": { "x": 0, "y": 0, "z": 1 }
      }
    ]
  },
  "error": null
}
```

## 7 Propose

A proposal has a summary of at most 120 characters, and from 1 to 16 envelopes. An envelope is the body of
`POST /commands`. `CreateBox` makes a box with its centre at the origin, `Translate` moves it, and `RenameBody` gives it its
name. `Translate` makes a new body with the identifier of the `Translate` command, so `<Milieu>` is the identifier of the
new shelf. The first envelope has the guard `expectedDocumentVersion`.

```http agent-request
POST /proposals
Content-Type: application/json

{
  "summary": "Add the middle shelf Milieu between Bas and Haut",
  "commands": [
    {
      "name": "CreateBox", "schemaVersion": 1, "commandId": "<new box>", "expectedDocumentVersion": "<version>",
      "parameters": { "sizeX": 1164, "sizeY": 394, "sizeZ": 18 }
    },
    {
      "name": "Translate", "schemaVersion": 1, "commandId": "<Milieu>",
      "parameters": { "bodyId": "<new box>", "dx": 300, "dy": -3, "dz": 1000 }
    },
    {
      "name": "RenameBody", "schemaVersion": 1, "commandId": "<new name>",
      "parameters": { "bodyId": "<Milieu>", "name": "Milieu" }
    }
  ]
}
```

The host checks each envelope at the post: a known name and version, and parameters that bind. The answer is HTTP 201
with the proposal. Its state is `open`, and `results` is empty because no command ran.

```json agent-result
{
  "id": "<proposal>",
  "summary": "Add the middle shelf Milieu between Bas and Haut",
  "state": "open",
  "documentVersion": "<version>",
  "stale": false,
  "commands": [
    { "name": "CreateBox", "commandId": "<new box>" },
    { "name": "Translate", "commandId": "<Milieu>" },
    { "name": "RenameBody", "commandId": "<new name>" }
  ],
  "results": []
}
```

The Document did not change: its version is the version before the post.

```http agent-request
GET /document
```

```json agent-result
{ "version": "<version>" }
```

`GET /proposals` gives the open proposals, the oldest first. The window shows the oldest one. The queue holds at most 8
open proposals.

```http agent-request
GET /proposals
```

```json agent-result
[ { "id": "<proposal>", "state": "open" } ]
```

To withdraw an open proposal, send `DELETE /proposals/{id}`.

## 8 The person accepts or rejects

The window shows the proposal as an amber arc, with its summary, its commands and the buttons Reject and Accept. Wait for
the person. In this example the person accepts.

```text agent-person
accept <proposal>
```

`GET /proposals/{id}` gives the proposal, open or closed. The host keeps the last 32 closed proposals. The state is one of
`open`, `applying`, `accepted`, `rejected`, `withdrawn` and `failed`. A failed proposal gives `failedIndex` and
`failedCode`. `results` gives the answer of each command that ran.

```http agent-request
GET /proposals/<proposal>
```

```json agent-result
{
  "id": "<proposal>",
  "state": "accepted",
  "results": [
    { "commandName": "CreateBox", "status": "Applied", "outputs": { "bodyId": "<new box>" } },
    { "commandName": "Translate", "status": "Applied", "outputs": { "bodyId": "<Milieu>" } },
    { "commandName": "RenameBody", "status": "Applied" }
  ],
  "failedIndex": null,
  "failedCode": null
}
```

The stream of events also gives each change: `body.created` for the box and for the shelf, `body.consumed` for the box that
`Translate` moved, and `body.changed` for the name.

## 9 Check

`CheckInterference` gives the class of each pair of bodies: `Apart`, `Touching` or `Overlapping`. The parameter `bodyIds`
is one text, with the identifiers separated by commas. With no `bodyIds`, the query checks each live body. A pair that
overlaps is a fault, unless `intendedPairs` names it, as `"a:b"`.

```http agent-request
POST /queries
Content-Type: application/json

{
  "name": "CheckInterference",
  "schemaVersion": 1,
  "parameters": { "bodyIds": "<Cote gauche>,<Cote droit>,<Bas>,<Haut>,<Fond>,<Milieu>" }
}
```

The answer gives the pairs in the order of the bodies. Here are the five pairs of the new shelf. The shelf touches the two
sides and the back, and it is 373 mm from each other shelf. No pair is a fault.

```json agent-result
{
  "result": {
    "pairs": [
      { "bodyIdA": "<Cote gauche>", "bodyIdB": "<Milieu>", "class": "Touching", "gap": 0, "fault": false },
      { "bodyIdA": "<Cote droit>", "bodyIdB": "<Milieu>", "class": "Touching", "gap": 0, "fault": false },
      { "bodyIdA": "<Bas>", "bodyIdB": "<Milieu>", "class": "Apart", "gap": 373, "fault": false },
      { "bodyIdA": "<Haut>", "bodyIdB": "<Milieu>", "class": "Apart", "gap": 373, "fault": false },
      { "bodyIdA": "<Fond>", "bodyIdB": "<Milieu>", "class": "Touching", "gap": 0, "fault": false, "meshOnly": false }
    ]
  },
  "error": null
}
```

A pair with `fault` true, or a gap that is not the gap of your plan, starts the loop again: read, measure, and propose a
correction. To remove an accepted change, propose the opposite change; the person can also undo it in the window.

## 10 The picture

`GetViewLines` gives the lines of the live bodies along one exact frame. It changes nothing and needs no sheet. The nine
numbers are the right, the up and the direction to the eye, each as x, y and z. The front view has the right (1, 0, 0), the
up (0, 0, 1) and the eye at (0, -1, 0). Each line gives its body, its two ends on the plane of the view in millimetres,
`visible`, and its kind. Draw the visible lines with a full stroke and the other lines with a dashed stroke.

```http agent-request
POST /queries
Content-Type: application/json

{
  "name": "GetViewLines",
  "schemaVersion": 1,
  "parameters": {
    "rightX": 1, "rightY": 0, "rightZ": 0,
    "upX": 0, "upY": 0, "upZ": 1,
    "eyeX": 0, "eyeY": -1, "eyeZ": 0
  }
}
```

```json agent-result
{
  "result": {
    "direction": [1, 0, 0, 0, 0, 1, 0, -1, 0],
    "lines": [ { "body": "<Milieu>", "visible": true, "kind": "edge" } ],
    "skipped": []
  },
  "error": null
}
```

When the Document has a sheet, `GET /export/svg?sheetId={id}` gives the sheet as an SVG file, with its views, its
dimensions and its title block. The views of a sheet follow the bodies, so the new shelf is on the sheet. `GetSheet` gives
the same sheet as JSON. `/export/dxf` and `/export/pdf` take the same parameter.

```http agent-request
GET /export/svg?sheetId=<sheet>
```

```text agent-result
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg"
```

## 11 The refusals

A refusal gives a code. `GET /schema/diagnostics` gives each code with its meaning, and `docs/diagnostics.md` gives each
place that raises it. A query or a command that the engine refuses gives HTTP 200 with `error`; a command also gives the
status `Rejected`. A request that the surface refuses gives HTTP 4xx with `{ "error": { "code", "message" } }`.

| Code | The cause | What to do |
|---|---|---|
| `E-API-BAD-REQUEST` | The body is not valid: malformed JSON, a member that the envelope does not name, a parameter that does not bind. For a proposal, `index` names the envelope. | Read the schema of the command or the query, and correct the body. |
| `E-CMD-UNKNOWN`, `E-QRY-UNKNOWN` | No command or query has the name and the version. | Read `GET /schema/commands` or `GET /schema/queries`. |
| `E-GEOM-BODY-NOT-FOUND` | The identifier is not a live body. A command that moves or cuts a body consumes it, and makes a new body. | Read the snapshot or the events again, and use the new identifier. |
| `E-MEASURE-NAME-NOT-FOUND` | A face or an edge that the body does not have. | Read the faces with `GetFaces`. |
| `E-MEASURE-CURVED` | A curved face in a case that the measure does not do (see section 12). | Measure a plane face or an edge, or a body of plane faces. |
| `E-MESH-UNSUPPORTED` | A body of an STL file, in an operation that does not take it, or in a check when its triangles do not close one solid. | Measure or check it as a whole body only. |
| `E-CHECK-PAIR-INVALID` | A list of `CheckInterference` that is not valid. | Give two bodies or more, each once, and pairs as `a:b`. |
| `E-CHECK-WORK-LIMIT` | The work of a `CheckInterference` is above its limit (see section 12). | Give fewer bodies with `bodyIds`. |
| `E-CMD-VERSION-STALE` | The Document changed after you read its version. | Read again, measure again, and propose again. |
| `E-API-PROPOSAL-QUEUE-FULL` | 8 proposals are open (HTTP 409). | Wait for the person, or withdraw a proposal. |
| `E-API-PROPOSAL-NOT-FOUND` | No open or kept proposal has the identifier (HTTP 404). | Post the proposal again if you need it. |
| `E-API-PROPOSAL-CLOSED` | A withdraw of a proposal that is not open (HTTP 409). | Read the proposal; the person decided. |
| `E-API-PROPOSAL-COMMAND-KNOWN` | The `failedCode` of a proposal whose command had a known `commandId`. | Make a new UUID for each command. |
| `E-API-OPEN-PENDING` | The host opens another file (HTTP 503). | Connect again after the start. |

Here are three refusals that an agent meets often. The box that `CreateBox` made is not live after the `Translate`:

```http agent-refusal
POST /queries
Content-Type: application/json

{ "name": "GetMassProperties", "schemaVersion": 1, "parameters": { "bodyId": "<new box>" } }
```

```json agent-result
{ "result": null, "error": { "code": "E-GEOM-BODY-NOT-FOUND" } }
```

A check needs two bodies or more:

```http agent-refusal
POST /queries
Content-Type: application/json

{ "name": "CheckInterference", "schemaVersion": 1, "parameters": { "bodyIds": "<Milieu>" } }
```

```json agent-result
{ "error": { "code": "E-CHECK-PAIR-INVALID" } }
```

A parameter name that the command does not have refuses the whole proposal, and `index` names the envelope:

```http agent-refusal
POST /proposals
Content-Type: application/json

{
  "summary": "Make the middle shelf thicker",
  "commands": [ { "name": "CreateBox", "schemaVersion": 1, "parameters": { "sizeX": 1164, "sizeY": 394, "thickness": 22 } } ]
}
```

```json agent-result
{ "error": { "code": "E-API-BAD-REQUEST", "index": 0 } }
```

A withdraw of the accepted proposal:

```http agent-refusal
DELETE /proposals/<proposal>
```

```json agent-result
{ "error": { "code": "E-API-PROPOSAL-CLOSED" } }
```

## 12 The limits

- **No preview.** The window shows the commands of a proposal, not their result. A check before the accept is not possible;
  check after the accept. Row P15 adds a preview on a fork of the Document.
- **Curved faces.** `GetDistance` measures a face of a cylinder, a cone, a sphere or an arc whose body has only `Translate`
  and `Rotate` after its command. It refuses with `E-MEASURE-CURVED` a body with a curved face as a side, an edge of a
  curved face, a face of a body with a `Subtract` (the side of a hole), a torus, a slanted face or edge near the side of a
  cone or past an end of a cylinder, the side of an arc whose nearest point of the whole turn is not on the arc, and two
  curved faces other than two spheres, a sphere and a cylinder or a cone, and two cylinders with parallel axes. Measure from
  a plane face or an edge of the other body in place of a body. `GetAngle` gives the angle of the axes for two curved faces,
  from 0 to 90 degrees, and refuses a sphere, a torus and a face of a body with a `Subtract`. `GetMassProperties` does not
  refuse: a plate with holes across it gives the volume of its curve, and a body with no closed form, as a hole at an angle
  or a notch, gives `facets` true. No query names a vertex, so no measure starts at a vertex (ADR-0180 item 18, row K15d).
- **The cost of a check.** `CheckInterference` holds the session for its whole work, and each command waits for it. The
  work is the sum, over each pair, of the product of the counts of the facets of the two bodies. Above 100,000 the check is
  refused with `E-CHECK-WORK-LIMIT` before its first pair: the 19 boxes of the bookcase need 6,156, a sphere and a plate
  23,808, and two spheres 15,745,024. Below the limit a check can still hold the session for some seconds, and a cancel
  stops it inside a pair (ADR-0182 item 8). Check the bodies near your change, not each body.
- **Curved and mesh bodies in a check.** A body with a curved face, or a body of an STL file, is checked on its facets or its
  triangles, with `meshOnly` true. A closed mesh with two shells that share a volume is refused with `E-MESH-UNSUPPORTED`
  (ADR-0182 item 7).
- **Connectors.** `GetConnectors` gives the axis of the side of a cylinder only when the body has no step after the
  cylinder or the extrude of a circle other than `Translate` and `Rotate`. The side of a hole that a `Subtract` made is
  `Curved`, with no axis. A datum keeps the frame of the place where it was made: a later `Translate` or `Rotate` of its
  body does not move it, so compare its frame with the face of its `reference` (ADR-0183 item 3).
- **No list of the sheets.** No query and no field of the snapshot gives the sheets of the Document. The event
  `sheet.created` gives the identifier of a new sheet while you listen; for an earlier sheet, ask the person.
- **No list of the bodies by a query.** The snapshot of `/events` gives each live body with its name. `GetFeatures` gives
  the identifiers but not the names; then `GetBodyInfo` gives one name for each request.
- **No person on `engine-api-http`.** That program has no window, so its proposals stay open.
- **No phone.** The phone has no HTTP surface, so it shows no proposal.
- **No author in the log.** An accepted command is a command of the person in the log; the log does not record that an agent
  proposed it.
