---
id: 0046
title: The image of a datum in the desktop
status: Accepted
topic: Rendering, vision, clients
date: 2026-10-31
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.Core/Imaging/**
  - 3DEngine/SessionImages.cs
  - 3DEngine/ImagePlacement.cs
  - 3DEngine/ImageActions.cs
enforced-by: 3DEngine.ImageTests/ImageActionsTests.cs
---

# ADR-0046 — The image of a datum in the desktop

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

V2a put the record of an image on a datum in the Document, with `PlaceImage` and `CalibrateImage` (ADR-0045). The
standing answer Q3 A of the codebase review of 2026-10-31 (c) says that the client decodes the image and gives its
width and its height. V2b is that client in the desktop: it must read a PNG file, put its bytes in the store, place
the image, draw it on its datum, and take the two points of a calibration.

The repository writes PNG files with its own writer (`3DEngine.ImageTests/Png.cs`) on `System.IO.Compression`, and
each third-party package needs a pin, a gate and a licence notice. The owner told the session on 2026-10-31 to use its
recommendation for each question, so this record is Accepted.

## Decision

1. **The reader.** `PngReader` in `3DEngine.Core/Imaging` reads a PNG file of 8 bits for each channel with no
   interlace, in each of the five colour types, with the five filters and the CRC of each chunk, on
   `System.IO.Compression` only. It gives 8-bit RGBA with the top row first, as `ImageRenderer` takes them. Each side
   is at most 4096 pixels, the least `maxImageDimension2D` that Vulkan gives a device, so each image draws on each
   device. A file of another form throws `InvalidDataException` with the reason. No new package.
2. **The projection.** `SessionImages` reads the live images and subscribes to the image events in one read of the
   session, as `SessionSketches` does (ADR-0033 item 13). The event carries the record (ADR-0045 item 8), so the
   subscription only queues it. On the render loop, `Apply` reads the bytes of each new asset from the store of the
   surface, decodes them, and gives them to the renderer one time for each asset. An asset that the store does not
   give, or that is not a PNG file that the reader takes, gives one warning, and its image is not drawn.
   `ImagePlacement` gives the quad of ADR-0043 from the record and the datum (ADR-0045 item 2); the texture of a quad is
   the texture of its asset.
3. **The place.** The window "Images" has a field for the path of a PNG file and one button for each origin plane. A
   place reads the file (at most 16 MiB, ADR-0044 item 3), decodes it, puts the bytes in the store, and sends
   `PlaceImage` with the name, the width and the height, the origin 0 and the scale 1. The window shows the result.
4. **The calibration.** Each live image has a button "Calibrate". After it, the next two taps of the scene are points
   of the image: each tap goes through `Picker.RayThrough` and `Picker.OnPlane` to the plane of the datum, and
   `ImagePlacement.Pixel` gives the pixel there. The taps do not select a body. A third tap is kept out. The person
   gives the real distance in the window, and Apply sends `CalibrateImage`. Cancel ends the calibration.
5. **Not in V2b.** A mark of the two taps on the scene, a move, a rotation and a removal of an image, and a format
   other than PNG.

## Consequences

- The desktop and an agent read the same store: the surface registers it (ADR-0044 item 4), and the desktop takes it
  from the surface.
- A picture of a camera is often larger than 4096 pixels on one side, so a person must make it smaller first.

## History

- 2026-10-31 · TASK-0153 · The record, with the standing answer Q3 A of the codebase review of 2026-10-31 (c).
