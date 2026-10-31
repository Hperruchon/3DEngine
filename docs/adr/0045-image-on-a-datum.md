---
id: 0045
title: An image on a datum, and its calibration
status: Accepted
topic: Contracts, vision
date: 2026-10-31
supersedes: []
superseded-by: []
amends: ['0012', '0031', '0033']
amended-by: []
affects:
  - Engine.Contracts/Document.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Contracts/Images/**
  - Engine.Core/CommandBus.cs
  - Engine.Core/StepHistory.cs
  - Engine.Core/Commands/PlaceImageCommand.cs
  - Engine.Core/Commands/PlaceImageCommandHandler.cs
  - Engine.Core/Commands/CalibrateImageCommand.cs
  - Engine.Core/Commands/CalibrateImageCommandHandler.cs
  - Engine.Core/Commands/ImageResults.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
enforced-by: Engine.Tests/Images/ImageCommandTests.cs
---

# ADR-0045 — An image on a datum, and its calibration

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row V2 is "Place an image on a datum. Calibrate it to real units." V1b keeps the bytes of an image in the
store of the account under the SHA-256 of the bytes (ADR-0044), and V1a draws an image on a quad of the scene
(ADR-0043). V3 makes a sketch from a contour of a calibrated image, so the Document must know where the image is and
how large it is.

The codebase review of 2026-10-31 (c) asked three questions. The owner told the session on 2026-10-31 to use its
recommendation for each question, so the standing answer takes Q1 A (the Document holds a record of each placed image),
Q2 A (a calibration records two points of the image and a real distance) and Q3 A (the client decodes the image, and
the command records its width and its height). This record is Accepted.

## Decision

1. **The record.** `ImageRecord` in `Engine.Contracts/Images` holds the identifier of the image, the name of its asset
   (ADR-0044 item 1), the identifier of its datum, the origin `(OriginU, OriginV)` in the frame of the datum, the width
   and the height in pixels, and the scale in millimetres for each pixel. A record never changes: an edit gives a new
   record with the same identifier. The identifier of an image is the identifier of the command that places it.
2. **The frame.** The origin is the lower-left corner of the image. The image covers `OriginU` to
   `OriginU + Width × Scale` along the axis U of the datum, and `OriginV` to `OriginV + Height × Scale` along the axis
   V. Row 0 of the image is at the top. So the pixel `(x, y)`, with x to the right and y down from the upper-left
   corner, is at `U = OriginU + x × Scale` and `V = OriginV + (Height − y) × Scale`. The image has no rotation in the
   plane in V2; a later command records a cosine and a sine (`CLAUDE.md`, determinism rule 2).
3. **The Document.** `Document.Images` holds one record for each live image. Only the commit of the command bus changes
   it, as for a sketch (ADR-0033 item 4).
4. **`PlaceImage`@1.** The parameters are `asset` (a name of ADR-0044 item 1), `datumId` (one of the three origin
   planes), `width` and `height` (integers greater than 0), `originU` and `originV` (finite numbers, 0 by default) and
   `scale` (a finite number greater than 0, 1 by default). The output is `imageId`. The handler does not read the store:
   a document file that a person copies does not carry its assets (ADR-0044 item 6), and a replay must not need the
   bytes. The client reads the width and the height when it decodes the image.
5. **`CalibrateImage`@1.** The parameters are `imageId`, two points of the image in pixels `(x1, y1)` and `(x2, y2)`,
   and `distance`, the real distance between them in millimetres. The new scale is
   `distance / sqrt((x2 − x1)² + (y2 − y1)²)`, so the command uses only `+ − × / sqrt` (`CLAUDE.md`, determinism rule
   1). The origin stays. The log records the six inputs and not the scale (ADR-0033 item 7). The two points must be
   different, and the distance must be finite and greater than 0.
6. **The codes.** `E-IMAGE-NOT-FOUND` (no live image with the identifier), `E-IMAGE-ASSET-INVALID` (the asset is not a
   name), `E-IMAGE-DATUM-NOT-FOUND`, `E-IMAGE-SIZE-INVALID` (a width or a height that is not greater than 0),
   `E-IMAGE-SCALE-INVALID` (a scale that is not finite or not greater than 0) and `E-IMAGE-CALIBRATION-INVALID` (two equal points, a point that is not finite, or a distance that is
   not finite or not greater than 0). An origin that is not finite gives `E-GEOM-INVALID-PARAM`, as a position of a
   sketch does. `E-IMAGE-ID-IN-USE` refuses a `PlaceImage` whose identifier the log holds
   already, as `E-SKETCH-ID-IN-USE` does for an item of a sketch.
7. **The result of a handler.** `CommandHandlerResult` gains `CreatedImages` and `ChangedImages`, two lists of
   `ImageRecord`, empty by default. A handler changes only a live image, one time, and keeps its identifier, its asset
   and its datum; the bus refuses a list that breaks the rule. This amends ADR-0033 item 8.
8. **The events.** Three new kinds: `image.created` and `image.changed` with `imageId`, `asset`, `datumId`, `originU`,
   `originV`, `width`, `height` and `scale`, and `image.removed` with `imageId`. The record is small, so the event
   carries it, and no query is necessary. The events of one commit come after each sketch event, in the order
   `image.created`, `image.removed`, `image.changed`. This amends ADR-0031 item 5 and ADR-0033 item 9.
9. **Undo and redo.** A step records each image that it created, and the record before and after of each image that it
   changed. An Undo removes each image that the step created and gives each changed image its record before; a Redo
   gives the first changes again. This amends ADR-0031 items 3 and 4.
10. **The snapshot.** The snapshot of the WebSocket gives `images`, each live image with all the fields of its record.
    This amends ADR-0012 §6.
11. **Not in V2a.** The desktop (V2b): the decode of a PNG file, the put of its bytes, the quad of each live image, and
    the gesture of the calibration. A move, a rotation and a removal of an image are later commands.

## Consequences

- A change of the public shape of `Engine.Contracts`: a new record, a new collection of the Document, two new lists of
  `CommandHandlerResult` and three new event kinds. No field changes its meaning.
- The document file does not change its form: it holds the two new commands as entries.
- A record holds the name of an asset, so a later phase can remove each asset that no document names.

## History

- 2026-10-31 · TASK-0152 · The record, with the standing answers Q1 A, Q2 A and Q3 A of the codebase review of
  2026-10-31 (c).
