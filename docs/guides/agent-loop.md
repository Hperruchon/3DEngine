# The loop of an agent: read, measure, propose and check

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This guide is for an agent that works on the Document of a person through the HTTP surface. It gives the loop of four
steps, the exact request of each step, the shape of each answer, the refusals and the limits. Row P17 (TASK-0439) wrote it,
and ADR-0157 item 13 asks for it. The guide covers the HTTP surface only; the command line comes later.

A test keeps the guide true. `Engine.Tests/Guides/AgentGuideExamplesTests.cs` reads this file and sends each example
request to a host in the order of the guide. It compares each answer with the shape under it. When a request of the guide is
refused, or an answer does not have its shape, the test fails.

## 1 The rules

- **The person decides.** An agent proposes a change with `POST /proposals`. Only the person accepts or rejects it, in the
  window of the desktop. No route accepts or rejects a proposal, so an agent cannot accept its own proposal (ADR-0092).
- **Nothing enters the log before the person accepts.** A proposal waits in a queue of the host, outside the log. Each
  query reads the Document and changes nothing.
- **Do not change the Document of a person with `POST /commands`.** That route applies a command at once, with no accept.
  Use it only on a Document that is yours, for example a host that you started for your own work.
- **An accept is one step of undo, and applies each command or none.** The accept runs the commands again on the Document as
  it is. It applies only when the result equals the preview that the person saw, and one `Undo` then reverses the whole
  proposal. When a command stops, nothing stays applied (ADR-0188).
- **Each length is in millimetres.** An answer gives a number with no unit (register entry R-0053).
- **A turn is a quaternion, never an angle.** The command `Rotate` takes `qw`, `qx`, `qy` and `qz`. If you think in
  degrees, compute the sine and the cosine yourself, and send the numbers (determinism rule 2 of `CLAUDE.md`).
- **Move a part with `PlaceBody`.** `PlaceBody` takes `bodyId`, a quaternion `qw`, `qx`, `qy`, `qz` and an offset `dx`, `dy`,
  `dz`. The placement is absolute, and the body keeps its identifier and its shape. `GetBodyInfo` and `GetBodies` give the
  placement of a body, null at the identity, so read it before you compute a new one. `Rotate` and `Translate` make a new
  body with a new identifier (ADR-0191). A `PlaceBody` keeps the key `geometry` of the body, so if you keep the triangles of
  `GetTessellation`, read them again when `geometry` or `placement` changes in `body.changed`.
- **Connect two parts with a joint.** `AddJoint` takes a `kind` (`fixed`, `pivot` or `slider`), a `parentBodyId` and a
  `childBodyId`, and a connector on each: a face (`parentFeatureId` and `parentElement`, as `GetConnectors` names it) or a
  datum (`parentDatumId`), and the same for the child. The child face meets the parent face with the normals opposite. A pivot
  takes `cos` and `sin`, a slider takes `distance`, and the limits are optional. `SetJointValue` moves the joint, and the
  child and each body under it follow; a `PlaceBody` of a child is refused, and a `PlaceBody` of a root moves its tree.
  `GetJoints` gives the joints (ADR-0193).
- **Move the child of a joint away, and snap a part to a part.** `SetJointState` with `state` "loose" keeps the joint and lets
  you move its child with `PlaceBody`; "tight" puts it back at the value of the joint. `PlaceByConnectors` takes the fields of
  the bodies and the connectors of `AddJoint`, places the child on the parent one time, and records no joint. `CheckInterference`
  takes the two bodies of each joint as an intended pair (ADR-0194).
- **Check that a pivot can turn.** `CheckTravel` takes `jointId` and an optional `tolerance` (0.01 mm by default). It turns the
  child and the bodies under it from the lower to the upper limit with no change of the Document, and gives `free`, or the first
  `contact` with the pair and its gap; no contact comes before it (ADR-0195).
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
the answer at that place. After that, each answer must give the same value. A placeholder in a request is that value. A
placeholder that no earlier answer gave is a new UUID that you make. For example `<Bas>` is the identifier of the body
"Bas"; your identifiers are different. A placeholder that is the whole JSON string `"<version>"` stands for the value of
the answer with its type. So in your request the version is a number with no quotes.

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

Then open the WebSocket `GET /events` and send `{}` as the first frame. The first message is a snapshot of the Document. It
gives each live body in the order of the Document, with its identifier `handle`, its kind and its name. After the snapshot, the
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

The query `GetBodies` gives the same bodies with no WebSocket, and `GetSheets` gives the sheets (section 12). The query
`GetBodyInfo` gives the kind, the name and the visibility of one body. Each query has the same envelope: a
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

`GetConnectors` gives the places on a body that a later step can name. It gives each named datum of the body, and one
entry for each face with the name that `GetFaces` gives. A plane face gives `point`, the centre of its area, and `direction`, its
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

Read the preview of your proposal before the person decides. The host runs the commands of the proposal on a fork of the
Document: a copy of its log, replayed in a second session with its own backend (ADR-0185). The fork changes nothing in the
Document, its log or its events. The host makes the preview when the proposal arrives, and again at each new version of the
Document while the proposal is open. `GET /proposals/{id}/preview` waits for the preview of the version of the Document at
your request.

```http agent-request
GET /proposals/<proposal>/preview
```

The answer gives the version that the fork copied, the result of each command, as an accept gives it, and the bodies that the
commands add, remove and change. Each body is a record of the body, as the events give it. Here the box of `CreateBox` is not
in the list, because `Translate` consumes it: the preview compares the bodies before the first command and after the last
command. The new shelf "Milieu" is the one added body.

The answer also gives three more differences (ADR-0185 items 8 to 10):

- `sketches`: each sketch that the commands add or change, with the state of its solver before and after them. The state is
  `solved` (no free degree), `under` (a free degree) or `over` (an equation is not within its tolerance), with `freedom` and
  `residual`, the largest absolute residual of its constraints. Here no sketch changes.
- `measures`: each body that the commands add or change, with the values of `GetMassProperties` after them and, for a changed
  body, before them. `before` is null for an added body. Here the shelf has 1164 × 394 × 18 = 8,255,088 mm³.
- `checks`: each pair with a body that the commands add, change or remove whose state changes, with the answer of
  `CheckInterference` before and after. The state is `apart`, `touching`, `overlapping`, `limit` (the pair is above the bound
  of work of the check, `E-CHECK-WORK-LIMIT`) or `refused`. A body that does not exist counts as apart, so `before` is null
  here. The shelf touches the two sides and the back panel, so three pairs change from apart to touching.

```json agent-result
{
  "proposalId": "<proposal>",
  "state": "ready",
  "documentVersion": "<version>",
  "results": [
    { "commandName": "CreateBox", "status": "Applied", "outputs": { "bodyId": "<new box>" } },
    { "commandName": "Translate", "status": "Applied", "outputs": { "bodyId": "<Milieu>" } },
    { "commandName": "RenameBody", "status": "Applied" }
  ],
  "failedIndex": null,
  "failedCode": null,
  "bodies": {
    "added": [ { "handle": { "id": "<Milieu>" }, "name": "Milieu" } ],
    "removed": [],
    "changed": []
  },
  "sketches": [],
  "measures": [
    {
      "bodyId": "<Milieu>",
      "before": null,
      "after": { "mass": { "volume": 8255088, "centre": { "x": 300, "y": -3, "z": 1000 }, "facets": false }, "error": null }
    }
  ],
  "checks": [
    { "before": null, "after": { "state": "touching", "check": { "class": "Touching", "gap": 0, "fault": false } } },
    { "before": null, "after": { "state": "touching" } },
    { "before": null, "after": { "state": "touching" } }
  ],
  "error": null
}
```

A command that the fork does not apply stops the preview: `failedIndex` and `failedCode` name it, and the commands after it
do not run. The accept of that proposal then applies nothing and gives the same index and code. An `Undo` or a `Redo` in a
proposal stops it with `E-CMD-GROUP-MOVE`, because the commands of a proposal are one group of steps. A preview with the state `failed` could not replay the log, and `error` gives the code
`E-API-PREVIEW-FAILED` and the cause. When the person or another agent changes the Document, read the preview again: the
proposal has `stale` true, and the new preview runs on the new log.

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
`failedCode`, and applied nothing. `results` gives the answer of each command that ran. `held` is null here (see below).

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
`Translate` moved, and `body.changed` for the name. Each entry of the log of the three commands has the member `groupId`,
the identifier of the proposal. So the three commands are one step of undo, and one `Undo` in the window removes the shelf.

### When the Document changes after the preview

The accept compares the result of the commands at the version of the Document now with the preview that the person saw
(ADR-0188). At the version of that preview, the preview is the result. After a new version, the host runs the fork again and
compares the two results by their content: the outcome of each command, the bodies, the sketches, the measures and the checks.
An equal result applies. A different result applies nothing: the proposal stays `open` with the new preview, and its member
`held` gives the code `W-API-PROPOSAL-RESULT-CHANGED`. The window shows the new preview, and the person accepts again.

In this example the agent proposes a new name for the shelf and reads the preview.

```http agent-request
POST /proposals
Content-Type: application/json

{
  "summary": "Name the new shelf Middle shelf",
  "commands": [
    { "name": "RenameBody", "schemaVersion": 1, "commandId": "<second name>", "parameters": { "bodyId": "<Milieu>", "name": "Middle shelf" } }
  ]
}
```

```json agent-result
{ "id": "<second proposal>", "state": "open", "held": null }
```

```http agent-request
GET /proposals/<second proposal>/preview
```

```json agent-result
{
  "proposalId": "<second proposal>",
  "state": "ready",
  "bodies": { "changed": [ { "before": { "name": "Milieu" }, "after": { "name": "Middle shelf" } } ] }
}
```

Before the person accepts, the person renames the shelf "Shelf 3" in the window. Then the person accepts the preview that the
window showed.

```text agent-person
rename <Milieu> Shelf 3
```

```text agent-person
accept <second proposal>
```

The result now changes the name "Shelf 3", not the name "Milieu" of the preview that the person saw. So nothing applied.

```http agent-request
GET /proposals/<second proposal>
```

```json agent-result
{
  "id": "<second proposal>",
  "state": "open",
  "results": [],
  "held": { "code": "W-API-PROPOSAL-RESULT-CHANGED" }
}
```

The new preview gives the new result. The window shows it with the note "The result changed: check it".

```http agent-request
GET /proposals/<second proposal>/preview
```

```json agent-result
{
  "proposalId": "<second proposal>",
  "state": "ready",
  "bodies": { "changed": [ { "before": { "name": "Shelf 3" }, "after": { "name": "Middle shelf" } } ] }
}
```

The person accepts the new preview, and the accept applies.

```text agent-person
accept <second proposal>
```

```http agent-request
GET /proposals/<second proposal>
```

```json agent-result
{
  "id": "<second proposal>",
  "state": "accepted",
  "results": [ { "commandName": "RenameBody", "status": "Applied" } ],
  "held": null
}
```

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

A pair with `fault` true starts the loop again: read, measure, and propose a correction. A gap that is not the gap of
your plan does the same. To remove an accepted change, propose the opposite change; the person can also undo it in the window.

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
| `E-CMD-GROUP-MOVE` | The `failedCode` of a proposal with an `Undo` or a `Redo`: a proposal is one group of steps. | Leave the undo to the person; propose the opposite change. |
| `W-API-PROPOSAL-RESULT-CHANGED` | The `held` of an open proposal: the result changed after the preview that the person saw, and nothing applied. | Read the new preview; the person accepts again. |
| `E-API-OPEN-PENDING` | The host opens another file (HTTP 503). | Connect again after the start. |
| `E-API-HOST-STOPPED` | The host stopped while `GET /proposals/{id}/preview` waited (HTTP 503). | Connect again after the start, and post the proposal again. |

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

- **The preview checks only the changed pairs.** `GET /proposals/{id}/preview` checks each pair with a body that the commands
  add, change or remove, and gives only the pairs whose state changes. A pair whose two bodies keep their solids is not
  checked: a rename or a change of the visibility keeps the solid. `GetDistance` and `GetAngle` are not in the preview; ask
  them after the accept. Each pair is one check with its own bound of work, so a large body gives `limit` for its pair and not
  a refusal of the preview. The checks of one preview also share one bound, the bound of one check: the first pair above the
  work that it leaves, and each pair after it in the order of the identifiers, has `limit` (ADR-0188 item 8). On a host with no
  backend for a fork the route gives HTTP 501 with `E-API-NOT-SUPPORTED`; the desktop and `engine-api-http` make a fork.
- **A preview is compared, not replayed.** The accept applies only a result equal to the preview that the person saw (section
  8), so a change of the Document after the preview gives `held`, not another outcome. A `commandId` that the session knows
  from before is the one exception that the fork cannot see: the fork knows only the identifiers of the log. The accept stops
  at that command, and one `Undo` of the group reverses the commands before it. When the session applied that `commandId`, the
  proposal is failed with `E-API-PROPOSAL-COMMAND-KNOWN`. When the session refused it, the bus gives the refusal again, and the
  proposal is failed with the code of that refusal, for example `E-GEOM-INVALID-PARAM`. Give each command a new `commandId`.
- **Curved faces.** `GetDistance` measures a face of a cylinder, a cone, a sphere or an arc. The body of that face has only
  `Translate` and `Rotate` after its command. It also measures the side of a hole or of a tube that a `Subtract` made. The
  tool of that `Subtract` is a cylinder, a cone or an extrude of one circle. The faces of the body near the hole are across
  its axis or far from it. `GetDistance` refuses some cases with `E-MEASURE-CURVED`:
  - a body with a curved face as a side, an edge of a curved face, and a torus;
  - a slanted face or edge near the side of a cone or near a rim;
  - the side of a notch or of a hole at an angle;
  - the side of an arc whose nearest point of the whole turn is not on the arc;
  - a cone and another side on another axis;
  - two cylinders that are not parallel, when their common perpendicular is past an end or when they cross.

  Measure from a plane face or an edge of the other body in place of a body. For two curved faces, `GetAngle` gives the
  angle of their axes, from 0 to 90 degrees. It gives the angle of the side of a hole from its outward normals, which point
  to the axis. The side of an arc that bites into the profile has outward normals that point to the axis too. `GetAngle`
  refuses a sphere and a torus. `GetMassProperties` does not refuse. A plate with holes, a counterbore, a countersink and a
  tube give the volume of their curve. A body with no closed form, as a hole at an angle or a notch, gives `facets` true.
  No query names a vertex, so no measure starts at a vertex (ADR-0180 item 23, row K15d).
- **The cost of a check.** `CheckInterference` holds the session for its whole work, and each command waits for it. The
  work is the sum, over each pair, of the product of the counts of the facets of the two bodies. Above 100,000 the check is
  refused with `E-CHECK-WORK-LIMIT` before its first pair. The 19 boxes of the bookcase need 6,156, a sphere and a plate
  23,808, and two spheres 15,745,024. The count of the triangles of an STL file comes before its solid, so a large file is
  refused before that work. Below the limit a check can still hold the session for some seconds. A cancel stops it inside
  a pair (ADR-0182 item 8). Check the bodies near your change, not each body.
- **Curved and mesh bodies in a check.** A body with a curved face is checked on its facets, with `meshOnly` true. A body
  of an STL file is checked on its triangles, with `meshOnly` true. A closed mesh is refused with `E-MESH-UNSUPPORTED`
  when two of its shells share a volume outside its voids (ADR-0182 item 7).
- **Connectors.** `GetConnectors` gives the axis of the side of a cylinder or of an extrude of a circle. The body must have
  no step after that command other than `Translate` and `Rotate`. The side of a hole that a `Subtract` made is
  `Curved`, with no axis. A datum keeps the frame of the place where it was made. A later `Translate` or `Rotate` of its
  body does not move it, so compare its frame with the face of its `reference` (ADR-0183 item 3).
- **The lists give no geometry.** `GetSheets` gives no line of a view, and `GetBodies` gives no size and no fault of a body.
  Read one sheet with `GetSheet`, measure one body with the queries of section 6, and read the faults with `GetFeatures`
  (see the examples below).
- **No person on `engine-api-http`.** That program has no window, so its proposals stay open.
- **No phone.** The phone has no HTTP surface, so it shows no proposal.
- **No author in the log.** An accepted command is a command of the person in the log. Its `groupId` is the identifier of the
  proposal, but the log does not record that an agent proposed it.

### The lists of the sheets and of the bodies

Two queries give the sheets and the bodies of the Document with no WebSocket (ADR-0189). Each takes no parameter, changes
nothing and gives no error. Each list is in the order of the log of the command that made each sheet or body. That order
can differ from the order of the snapshot of `/events`.

`GetSheets` gives each live sheet: its identifier, which each command of a sheet and `GetSheet` take, its name, its paper,
its orientation, its projection and the identifier of each view in the order of the commands. A removed sheet is not in the
list.

```http agent-request
POST /queries
Content-Type: application/json

{ "name": "GetSheets", "schemaVersion": 1, "parameters": {} }
```

```json agent-result
{
  "result": {
    "sheets": [
      {
        "sheetId": "<sheet>",
        "name": "Bookcase",
        "paper": "A4",
        "landscape": true,
        "thirdAngle": false,
        "viewIds": [ "<front view>" ]
      }
    ]
  },
  "error": null
}
```

`GetBodies` gives each live body with the fields of `GetBodyInfo`: the identifier, the kind, the name, the visibility and
the distance of an extrude, or null. A hidden body is in the list. A body that a command consumed is not: here the box of
`CreateBox` of section 7 is not in the list, and the middle shelf has the name of the second proposal.

```http agent-request
POST /queries
Content-Type: application/json

{ "name": "GetBodies", "schemaVersion": 1, "parameters": {} }
```

```json agent-result
{
  "result": {
    "bodies": [
      { "bodyId": "<Cote gauche>", "name": "Cote gauche", "visible": true },
      { "bodyId": "<Bas>", "kind": "Solid", "name": "Bas", "visible": true },
      { "bodyId": "<Fond>", "name": "Fond", "visible": true },
      { "bodyId": "<Milieu>", "kind": "Solid", "name": "Middle shelf", "visible": true, "distance": null }
    ]
  },
  "error": null
}
```
