---
id: 0147
title: The engine keeps each asset under the SHA-256 of its bytes, in one store of the account
status: Active
phase: V1b
opened: 2026-10-31
depends-on: [0145]
governed-by: [0002, 0004, 0013, 0016, 0018, 0019, 0030, 0044]
writes:
  create:
    - tasks/TASK-0147-asset-store.md
    - docs/adr/0044-asset-store.md
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Api.Http/Endpoints/AssetsEndpoint.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
  modify:
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - Engine.Cli/JsonRenderer.cs
    - docs/diagnostics.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0147 — The engine keeps each asset under the SHA-256 of its bytes, in one store of the account

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner told the session on 2026-10-31 to start V1: "A content-addressed asset store. A textured quad." V1a
(TASK-0146) draws an image on a quad. On the same day the owner chose one store for each account for the bytes of an
asset (ADR-0044).

## Goal

An agent, the command line and a host put the bytes of a file into one store and get them back by their SHA-256.

## Scope (in)

1. `AssetStore` in `Engine.Core/Assets`: the name, the put, the read, the limit and five codes (ADR-0044 items 1 to 3).
2. `PUT /assets` and `GET /assets/{name}` on the surface, and the setting `Engine:AssetFolder` (item 4).
3. `engine asset put` and `engine asset get` (item 5).

## Scope (out)

- A command or an event that names an asset, an image in the Document, and the image in the window (V2).
- The removal of an asset, and a pack of a document with its assets.

## Acceptance criteria

- [ ] The name of "abc" is the SHA-256 of FIPS 180-2, and a second put of the same bytes gives one file.
- [ ] A bad name, an absent asset, a changed file, an asset over 16 MiB and a folder that cannot be written each give
      their code.
- [ ] The two routes and the two verbs put and get an asset, and refuse a bad name.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the owner chose one store for each account. The store, the routes, the verbs and five codes in the new
  namespace `ASSET`. The marker gate refuses the word for a short-lived file in live code, so the code says "scratch
  file". 6 new test cases.
- 2026-10-31: 5 injections caught: no hash check on a read, no limit of size, an upper-case name, a missing asset
  with the status 500, and a path as a name. The first form of the injection of the limit was dead code, which the
  compiler refused; the second form compiled and failed the test of the limit.
