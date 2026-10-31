---
id: 0044
title: The store of the assets of an account
status: Accepted
topic: Vision, persistence
date: 2026-10-31
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Assets/**
  - Engine.Api.Http/Endpoints/AssetsEndpoint.cs
enforced-by: Engine.Tests/Assets/AssetStoreTests.cs
---

# ADR-0044 — The store of the assets of an account

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row V1 is "A content-addressed asset store. A textured quad." V1a drew an image on a quad (ADR-0043). V2
places an image on a datum, so a command must name an image, and the bytes of a photo are too large for an entry of the
log (ADR-0015 item 3).

On 2026-10-31 the owner answered the question of the place of the bytes: one store for each account, and not a folder
beside each document or a line in the document file. The owner told the session on the same day to use its
recommendation for each other question, so this record is Accepted.

## Decision

1. **The name.** The name of an asset is the SHA-256 of its bytes, in 64 lower-case hexadecimal digits. One name holds
   one content, so a command that records a name (V2) records the content too. SHA-256 uses integer operations only,
   so a name is the same on each platform.
2. **The store.** `AssetStore` in `Engine.Core/Assets` keeps each asset in one file of its folder, under its name. A put
   writes a scratch file of the folder, flushes it to the disk, and moves it to the name, so a reader never sees a part
   of an asset; a name that the store holds is not written again. A read computes the name of the bytes again and
   refuses a file whose bytes changed. The codes are `E-ASSET-BAD-NAME`, `E-ASSET-NOT-FOUND`, `E-ASSET-CORRUPT` and
   `E-ASSET-WRITE-FAILED`, and since TASK-0149 `E-ASSET-READ-FAILED` for a read that fails for another reason.
3. **The limit.** An asset holds at most 16 MiB, under the request limit of Kestrel (30 MB). A larger one gives
   `E-ASSET-TOO-LARGE`, and the store keeps nothing of it.
4. **The folder and HTTP.** The folder is `<local application data>/3DEngine/assets`, or the setting
   `Engine:AssetFolder`; a host can register its own store. The store is outside each document session: a command, a
   save, a Save as and an open do not change it, and it is not in the log. The surface gains `PUT /assets`, whose body
   is the bytes and whose reply is `{ asset, size }`, and `GET /assets/{name}`, which gives the bytes. An error gives
   the envelope of the transport with the status 400, 404, 413 or 500. The two routes are outside the triad of
   ADR-0008: they are not a command and not a query, and the two registries do not change.
5. **The command line.** `engine asset put <file>` and `engine asset get <name> <file>`, each with `--folder <path>`,
   print `{ asset, size }` or `{ error }`, with the exit code 0, 1 or 2. The command line keeps no state of a session
   between two runs; the store is a folder of the account, as a document file is.
6. **Not in V1b.** No command names an asset, no event tells of one, and nothing removes one. A document file that a
   person copies to another computer does not carry its assets; a later phase can add a pack of a document with its
   assets.

## Consequences

- No change of `Engine.Contracts` and no change of the document file.
- The store grows until a later phase removes the assets that no document names.

## History

- 2026-10-31 · TASK-0147 · The record, after the answer of the owner on the place of the bytes.
- 2026-10-31 · TASK-0149 · Item 2 gains `E-ASSET-READ-FAILED` (finding E114), a put repairs a damaged file (E115), and a route with no length lets the store apply its limit (E118).
