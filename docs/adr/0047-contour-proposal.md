---
id: 0047
title: The proposal of a contour and its accept as a sketch
status: Accepted
topic: Vision, clients
date: 2026-10-31
supersedes: []
superseded-by: []
amends: ['0042']
amended-by: []
affects:
  - 3DEngine.Core/Vision/**
  - 3DEngine/ImageActions.cs
  - 3DEngine/ImagePlacement.cs
enforced-by: 3DEngine.ImageTests/ContourAcceptTests.cs
---

# ADR-0047 — The proposal of a contour and its accept as a sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row V3 is "A contour proposal becomes an editable sketch. Propose and accept." V2 places a calibrated
image on a datum (ADR-0045, ADR-0046). ADR-0042 item 3 says that the client proposes a contour, the person accepts it,
and the commands that exist record its points, so a replay never calls the vision library.

ADR-0042 item 6 says that the phase that first puts the vision library into a distributed program adds its notices.
On 2026-10-31 the session read the runtime packages of OpenCvSharp4 4.13.0.20260627. The build information of the
Windows payload names zlib, libjpeg-turbo, libwebp, libpng, libtiff, OpenJPEG, OpenEXR, Protobuf, Flatbuffers, Intel
IPP ICV under the licence of Intel, and Tesseract, all linked statically, and a separate FFmpeg file under LGPL v2.1.
The README of the packages says that the macOS payload links FFmpeg and Tesseract statically. A distribution of these
licences, above all of LGPL and of the licence of Intel, is a decision of the owner.

The owner told the session on 2026-10-31 to take the recommended answer to each question. The recommendation is the
decision below; register entry R-0048 asks the owner about the licences of the vision library.

## Decision

1. **The place and the library.** `ContourProposal` in `3DEngine.Core/Vision` proposes the contour in managed code,
   with no package. The vision library of ADR-0042 stays in the spike `3DEngine.VisionTests`, and no distributed
   program holds it, so item 6 of ADR-0042 does not act yet. A later phase that needs more than one contour of one
   region puts the library into a program after the answer of R-0048. This amends ADR-0042 item 3: V3 proposes with
   its own code.
2. **The method.** The grey of a pixel is (299 R + 587 G + 114 B + 500) / 1000 in integers. The threshold of Otsu
   separates two classes, and the region is the class that holds fewer pixels of the border of the image. Of the
   regions whose pixels touch by a side, the proposal takes the largest. The trace of the neighbours of Moore, with
   the stop of Jacob, gives its outer boundary; Douglas and Peucker simplify it with a tolerance of the larger of one
   pixel and 0.5 % of the boundary, and the tolerance grows by half until the polygon has at most 200 vertices. A vertex
   is at the centre of a pixel. An image with no two classes, or a region with fewer than three vertices, gives no
   proposal. The arithmetic is integers and `+ − × / sqrt`, so a proposal is the same on each platform; it is outside
   the log in each case.
3. **The proposal in the desktop.** Each live image of the window "Images" has a button "Propose contour". The
   proposal shows over the scene as the lines of a sketch until the person accepts it or rejects it.
4. **The accept.** An accept sends `CreateSketch` on the datum of the image, `AddSketchPoint` at each vertex and
   `AddSketchLine` along each side, with the point (U, V) = (OriginU + x × Scale, OriginV + (Height − y) × Scale) of the
   pixel (x, y) (ADR-0045 item 2). The log holds these commands and their numbers, as for a sketch that a person draws.
   The new sketch becomes the active sketch, so the person edits it with the tools of ADR-0034.
5. **Not in V3.** A hole of a region, more than one region, an arc from a contour, a constraint from a contour, one
   step of undo for one accept, and the vision library in a distributed program.

## Consequences

- No change of `Engine.Contracts`, of the document file and of the log.
- An undo of an accept takes one step for each command: 1 + 2n steps for a polygon of n vertices.
- A photo with a shadow or a background of many greys can give a region that is not the part; the person rejects it.

## History

- 2026-10-31 · TASK-0155 · The record, with the recommended answers under the standing order of the owner of
  2026-10-31.
