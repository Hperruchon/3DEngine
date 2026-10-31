# Diagnostic code registry

All `E-`/`W-`/`I-` codes used in `Engine.*` code MUST appear here. A code is never removed and never changes meaning. A row may move between the two tables, and its text may change to say where the code is raised. Per ADR-0008 §4.

## Conventions

- **Severity prefix:** `E-` error · `W-` warning · `I-` info
- **Subsystem token:** `CMD` (command bus), `QRY` (query bus), `API` (HTTP transport), `GEOM` (geometry), `IO` (persistence/transport), `VAL` (validation), `SKETCH` (the sketch model, ADR-0033), `ASSET` (the store of the assets, ADR-0044), `IMAGE` (an image on a datum, ADR-0045). Add new tokens as subsystems land.
- **Format:** `<severity>-<subsystem>-<short-tag>` — short, kebab-style, stable.

## Active codes

| Code | Severity | Meaning | Where raised |
|---|---|---|---|
| `E-CMD-UNKNOWN` | Error | Command name + schema version is not registered in `CommandRegistry`. | `CommandBus.Apply` when no handler matches. |
| `E-CMD-VERSION-STALE` | Error | Submitted command's `ExpectedDocumentVersion` does not match the Document's current `Version`. | `CommandBus.Apply` optimistic check. |
| `E-CMD-BUS-BUSY` | Error | Inbound command queue is full; submission rejected without enqueueing. | Reserved for the transport task; not raised in P0 (no inbound queue yet). |
| `E-CMD-NOTHING-TO-UNDO` | Error | An `Undo` found no step to reverse: the document has no step, or each step is undone. The log and the version do not change. | `CommandBus` (ADR-0031 item 8). |
| `E-CMD-NOTHING-TO-REDO` | Error | A `Redo` found no undone step: no `Undo` came before it, or a new step cleared the redo list. The log and the version do not change. | `CommandBus` (ADR-0031 item 8). |
| `E-QRY-UNKNOWN` | Error | Query name + schema version is not registered in `QueryRegistry`. | `QueryBus.Query` when no handler matches. |
| `E-API-BAD-REQUEST` | Error | HTTP request body was malformed, missing a required field, or wrong content-type. Never reaches the bus. | `Engine.Api.Http` endpoints, returned with HTTP `400` or `415`. |
| `E-API-WS-INVALID-SUBSCRIBE` | Error | WebSocket subscribe frame is malformed, missing, or wrong shape. The connection is closed before any subscription is established. | `Engine.Api.Http` `/events`, WebSocket close status `1007` (invalid payload data) with this reason. The owner decided 1007 on 2026-09-30; TASK-0010 said 1003, and the code sent 1007 from the start. |
| `W-API-WS-LAGGED` | Warning | A WebSocket subscriber's outbound queue overflowed (per ADR-0005 §6). The engine disconnects that subscriber; other subscribers are unaffected. | `Engine.Api.Http` `/events`, WebSocket close status `1008` with reason `subscriber.lagged`. |
| `W-API-SUBSCRIPTION-REMOVED` | Warning | The action of a subscription in the host process (`EngineHost.Subscribe`, ADR-0024 item 1) threw. It is removed and receives no later event; the commit and each other subscriber continue. | `Engine.Api.Http` `EventBroadcaster`, one line on the error output with the event and the exception. Raised since TASK-0074 (finding E62 of the codebase review of 2026-10-13). |
| `E-API-NOT-SUPPORTED` | Error | The host does not serve this route: `engine-api-http` does not open a file over HTTP, because a script starts it with `--file` (ADR-0030 item 4). HTTP 501. | `Engine.Api.Http` `DocumentEndpoint`, `POST /document/open` on a host with no function to start again. Raised since TASK-0092. Since TASK-0099 also `POST /document/save` on a host with no document file (ADR-0030 item 4). |
| `E-API-OPEN-PENDING` | Error | The host accepted an open of a document file and starts again with it, so it refuses each command, save and open until the new program serves (ADR-0030 item 4). HTTP 503. | `Engine.Api.Http` `CommandsEndpoint` and `DocumentEndpoint`, after `DocumentFolder.Open` accepted an open. Raised since TASK-0099. |
| `E-GEOM-CAP-MISSING` | Error | The active geometry backend does not implement a capability interface (`IMeshOps`, `IGeometryQuery`, etc.) that the handler requested. No silent fallbacks (ADR-0001 §4). | `Engine.Core` command/query handlers when `backend.TryGet<T>()` returns null. |
| `E-GEOM-INVALID-PARAM` | Error | Geometry command rejected because a parameter is out of range (e.g. zero or negative box size). | `Engine.Core` geometry command handlers on parameter validation. |
| `E-GEOM-BODY-NOT-FOUND` | Error | A geometry query referenced a `bodyId` that is not present in `Document.Bodies`. | `Engine.Core` geometry query handlers (e.g. `GetBoundingBox`). |
| `E-GEOM-NATIVE-OP` | Error | A geometry backend operation failed or returned a degenerate result (e.g. a native Manifold FFI failure). | `Engine.Core` geometry command handlers, wrapping the backend call (P7b); the queries `GetBoundingBox` and `GetTessellation` since TASK-0135 (E32). |
| `E-GEOM-BACKEND-INIT` | Error | The native geometry backend failed to initialise (native lib not found / load / version mismatch). | `Engine.Cli` before a command, and `Engine.Api.Http` at its start, when the native library does not load. Each one writes the code and the reason on the error output and stops with exit code 3. Raised since TASK-0036; until then the host took the managed backend with no message. |
| `E-VAL-BODY-NAME-INVALID` | Error | The name of a `RenameBody` command breaks the rules of a name: more than 128 UTF-16 units, a control character, or text that is not well-formed UTF-16 (ADR-0028 item 3; the last rule since TASK-0090). An empty name clears the name and is valid. | `Engine.Core` `RenameBodyCommandHandler`, before the commit. |
| `E-IO-BACKEND-MISMATCH` | Error | A log entry records another backend than the backend of the replay or of the open (ADR-0015 item 6, anti-objective 10). | `Engine.Core` `Replay.ReplayLog` of log entries, as `BackendMismatchException`, and the open of a document file. Raised since TASK-0091. |
| `E-IO-NOT-ENCODABLE` | Error | A command of a session with a document file has a value that the file cannot hold exactly: a number that is not finite, or text that is not well-formed UTF-16 (ADR-0015 item 4). The command is rejected before the write, and the Document does not change. | `Engine.Core` `CommandBus`, in the commit, after the handler. Raised since TASK-0091. |
| `E-IO-WRITE-FAILED` | Error | The write or the flush to disk of an entry of the document file failed (ADR-0015 item 4). The command is rejected, the file is cut back to its last complete line, and the session refuses each later command with this code. | `Engine.Core` `CommandBus`, in the commit, after the handler; and before the handler of each later command. Raised since TASK-0091. |
| `E-IO-LOAD-FAILED` | Error | A document file does not open: no header, another format, version or unit, a bad line before the last line, an entry whose version is not the next one, a command that this engine does not have, or an entry that is not applied (ADR-0015 items 2 and 8). | `Engine.Core` `DocumentFile`, as `DocumentFileException`. `engine-api-http` writes it and stops with exit code 6. Raised since TASK-0091. Since TASK-0099 also a file of `--file` that cannot be made, a file that another session holds, a number that is not finite, and an exception of a handler during an open. |
| `E-IO-SAVE-FAILED` | Error | A save of a document file to a new name failed: the session has no file, an earlier write failed, the target exists, or the move failed. The file stays where it was (ADR-0015 item 9). | `Engine.Core` `DocumentSession.SaveAs`. Raised since TASK-0091. |
| `W-IO-TRUNCATED` | Warning | The last line of a document file was not complete or did not parse, and the open cut it off (ADR-0015 item 8). The work of that last command is lost, and each earlier command is kept. | `Engine.Core` `DocumentFile.Open`, in its list of warnings. Raised since TASK-0091. Since TASK-0099 only for a tail with no line feed or a last line that is not JSON; a complete last line that is JSON and does not bind gives `E-IO-LOAD-FAILED`. |
| `W-IO-BACKEND-VERSION` | Warning | An entry of a document file records another version of the backend of the host; the open continues (ADR-0015 item 6). One warning for each open. | `Engine.Core` `DocumentFile.Open`, in its list of warnings. Raised since TASK-0091. |
| `E-SKETCH-NOT-FOUND` | Error | A sketch command or `GetSketch` names a sketch that is not live in the Document (ADR-0033 item 6). | `Engine.Core` sketch command handlers and `GetSketchQueryHandler`. Raised since TASK-0100. |
| `E-SKETCH-DATUM-NOT-FOUND` | Error | `CreateSketch` names a datum that does not exist; the datums are the origin planes XY, XZ and YZ (ADR-0033 item 1). | `Engine.Core` `CreateSketchCommandHandler`. Raised since TASK-0100. |
| `E-SKETCH-ITEM-NOT-FOUND` | Error | A sketch command names an item that its sketch does not have, an item of another kind than the command needs, or the origin of the frame, which no command moves or deletes (ADR-0033 items 2 and 6). | `Engine.Core` sketch command handlers. Raised since TASK-0100. |
| `E-SKETCH-LINE-INVALID` | Error | `AddSketchLine` gives one point as both ends of the line (ADR-0033 item 2). | `Engine.Core` `AddSketchLineCommandHandler`. Raised since TASK-0100. |
| `E-SKETCH-CURVE-INVALID` | Error | `AddSketchCircle` gives a radius that is not a positive finite number, or `AddSketchArc` gives a centre, a start and an end that are not three different points (ADR-0033 item 2). | `Engine.Core` `AddSketchCircleCommandHandler` and `AddSketchArcCommandHandler`. Raised since TASK-0117. |
| `E-ASSET-TOO-LARGE` | Error | An asset holds more than 16 MiB (ADR-0044 item 3). The store keeps nothing of it. | `Engine.Core` `AssetStore.Put`; HTTP 413 on `PUT /assets`; exit code 1 of `engine asset put`. Raised since TASK-0147. |
| `E-ASSET-BAD-NAME` | Error | A name of an asset is not 64 lower-case hexadecimal digits (ADR-0044 item 1). | `Engine.Core` `AssetStore.Read`; HTTP 400 on `GET /assets/{name}`; exit code 1 of `engine asset get`. Raised since TASK-0147. |
| `E-ASSET-NOT-FOUND` | Error | The store holds no asset of the name. | `Engine.Core` `AssetStore.Read`; HTTP 404 on `GET /assets/{name}`; exit code 1 of `engine asset get`. Raised since TASK-0147. |
| `E-ASSET-CORRUPT` | Error | The file of an asset holds bytes whose SHA-256 is not its name, so the store does not give them (ADR-0044 item 2). | `Engine.Core` `AssetStore.Read`; HTTP 500 on `GET /assets/{name}`; exit code 1 of `engine asset get`. Raised since TASK-0147. |
| `E-ASSET-WRITE-FAILED` | Error | The store could not write an asset to its folder, or the command line could not write the file of `engine asset get`. Nothing of the asset stays under its name. | `Engine.Core` `AssetStore.Put`; HTTP 500 on `PUT /assets`; exit code 1 of `engine asset put` and `engine asset get`. Raised since TASK-0147. |
| `E-IMAGE-NOT-FOUND` | Error | `CalibrateImage` names an image that is not live in the Document (ADR-0045 item 6). | `Engine.Core` `CalibrateImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-ID-IN-USE` | Error | A command of an image has an identifier that the log holds; an image takes the identifier of its command (ADR-0045 item 6). | `Engine.Core` `PlaceImageCommandHandler` and `CalibrateImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-ASSET-INVALID` | Error | `PlaceImage` gives an asset that is not a name of the store: 64 lower-case hexadecimal digits (ADR-0044 item 1, ADR-0045 item 6). | `Engine.Core` `PlaceImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-DATUM-NOT-FOUND` | Error | `PlaceImage` names a datum that does not exist; the datums are the origin planes XY, XZ and YZ (ADR-0045 item 6). | `Engine.Core` `PlaceImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-SIZE-INVALID` | Error | `PlaceImage` gives a width or a height that is not a whole number of pixels from 1 to 2147483647 (ADR-0045 item 6). | `Engine.Core` `PlaceImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-SCALE-INVALID` | Error | A command gives an image a scale that is not finite or not greater than 0 (ADR-0045 item 6). | `Engine.Core` `PlaceImageCommandHandler` and `CalibrateImageCommandHandler`. Raised since TASK-0152. |
| `E-IMAGE-CALIBRATION-INVALID` | Error | `CalibrateImage` gives two equal points, a point that is not finite, or a distance that is not finite or not greater than 0 (ADR-0045 item 6). | `Engine.Core` `CalibrateImageCommandHandler`. Raised since TASK-0152. |
| `E-SKETCH-CONSTRAINT-INVALID` | Error | A constraint has an unknown kind, a kind that this phase does not give, items of the wrong kind or count, or a value that is absent, not finite or negative (ADR-0033 items 3 and 6). | `Engine.Core` `AddSketchConstraintCommandHandler` and `SetSketchConstraintValueCommandHandler`. Raised since TASK-0100. |
| `E-SKETCH-NAME-INVALID` | Error | The name of a `CreateSketch` command breaks the rules of a name of ADR-0028 item 3, as ADR-0033 item 4 requires. An empty name is no name and is valid. | `Engine.Core` `CreateSketchCommandHandler`. Raised since TASK-0100. |
| `E-SKETCH-PROFILE-INVALID` | Error | `Extrude` names a sketch whose profile breaks the rules of an extrude, or a sketch that is not solved (ADR-0036 items 2 and 4). The profile is one closed loop of lines and arcs, or one circle alone (ADR-0040 item 1, since TASK-0126). A loop is closed when each point that a line or an arc uses as an end is the end of exactly two edges, the edges form one cycle, no two segments of its polygon that share no vertex cross or touch, no arc has the radius zero, and the area is not zero. The message names the reason. | `Engine.Core` `ExtrudeCommandHandler` and `SketchProfile`. Raised since TASK-0103. |
| `W-SKETCH-REBUILD-FAILED` | Warning | A sketch command applied, and an extrude of the sketch, or a body of its chain, did not follow it: the sketch is not solved, its lines are not one closed loop, a step of the chain does not resolve, or the backend failed (ADR-0037 item 6, ADR-0039 item 5). The body keeps its last solid and gets the same code as its fault; a later edit that mends the profile clears it. The owner widened the meaning to the chain on 2026-10-30 (Q2 A of the codebase review of that date). | `Engine.Core` `SketchRebuild`, in the diagnostics of each sketch command. Raised since TASK-0108; for a chain since TASK-0116. |
| `W-SKETCH-EXTRUDE-CONSUMED` | Warning | A sketch command applied, and an extrude of the sketch does not follow it, because a later command that the feature graph does not know consumed its body (ADR-0037 item 7, ADR-0039 item 7). The data gives `bodyId`. Since TASK-0116 a `Translate` and a `Subtract` are steps of the chain, so no command of today gives it. | `Engine.Core` `SketchRebuild`, in the diagnostics of each sketch command. Raised since TASK-0108. |
| `E-SKETCH-ID-IN-USE` | Error | A live sketch command has the identifier of a command that the log holds. The new item takes the identifier of its command (ADR-0033 item 5), so a second apply would give two items with one identifier (finding E86 of the codebase review of 2026-10-29). An open of a file and a replay of a log raise it only for a command that creates an item, whose second entry would give two items with one identifier (E104, TASK-0132); for a move, a change of a value and a delete they apply each entry as the session that wrote it did (E100, answer Q1 A of 2026-10-30). | `Engine.Core` through `SketchResults.InUse`: `CreateSketch`, `AddSketchPoint`, `AddSketchLine` and `AddSketchConstraint` since TASK-0106; `MoveSketchPoint`, `SetSketchConstraintValue` and `DeleteSketchItem` since TASK-0114; `AddSketchCircle` and `AddSketchArc` since TASK-0117; `Extrude` since TASK-0123. |
| `W-SKETCH-HOLD-DROPPED` | Warning | `MoveSketchPoint` applied, and the solver did not keep the point at the position of the command: the solve with the held point did not converge, and the solve with no hold did (ADR-0035 item 7, finding E87 of the codebase review of 2026-10-29). The data gives `pointId`. | `Engine.Core` `SketchResults.Changed`. Raised since TASK-0106. |

## Reserved namespaces

- `X-…` — plugin-defined codes (reserved, not used).

## Adding a code

1. Add the row above. Pick a stable, namespaced code.
2. Reference the code from `Engine.Core/DiagnosticCodes.cs` constants.
3. Same change as the code that raises it. `Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs` reads each `Engine.*` project and fails when a code in the source has no row here.

Removing a code is forbidden. A code that no source raises stays in the table above and gets a row in the table below, with its reason.

## Permanently reserved codes

A reserved code is a code that the registry holds and that no source raises. Register entry R-0011
recorded the risk: an unlimited quantity of reserved codes makes the registry unreliable, because a
reader cannot tell a live code from a placeholder.

`Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` reads this table. The quantity of reserved
codes must not grow past one. A new reserved code needs a decision that lowers or raises that number.
When a task raises a reserved code, the task removes its row from this table and corrects the column
"Where raised" above.

| Code | The reason that no source raises it |
|---|---|
| `E-CMD-BUS-BUSY` | No inbound queue exists. ADR-0006 gives a serial bus with no queue, therefore nothing can be full. The code stays because it is shipped and codes are stable. |

TASK-0025 decided this on 2026-09-22 and closed R-0011. Each code above is reserved permanently, and
not until a later phase. A later phase that raises one of them must state that it does so.

TASK-0036 raises `E-GEOM-BACKEND-INIT` (ledger entry v0.44): each host requires the native backend
and stops with this code when its library does not load. The reserve holds one code, and the budget
in the gate is one.
