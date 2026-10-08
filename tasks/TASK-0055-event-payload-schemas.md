---
id: 0055
title: /schema/events gives the payload fields of each event kind
status: Ready
phase: P0.20
opened: 2026-10-08
depends-on: [0054]
governed-by: [0011, 0013, 0016, 0019, 0021]
writes:
  create:
    - tasks/TASK-0055-event-payload-schemas.md
  modify:
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Schema/SchemaTypes.cs
    - Engine.Tests/Http/**
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0055 — /schema/events gives the payload fields of each event kind

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0008 §9 says that `/schema/events` gives the "index of event kinds + payload schemas", and that the
pipeline fails when an event lacks a schema entry. The endpoint gives the name of each kind only
(`Engine.Api.Http/Schema/SchemaTypes.cs:28`), so the code disagrees with the ADR. TASK-0037 found the
gap, and register entry R-0037 recorded it as a question. On 2026-10-08 the owner confirmed the payload
fields, and R-0037 closed with the outcome "decided", ADR-0008.

An agent reads the schema endpoints to learn the surface. Today it cannot learn what an event carries
without the ADRs.

## Goal

Each entry of `/schema/events` gives its kind and its payload fields with their types.

## Scope (in)

1. Each entry gains the payload fields of its kind, in the form of the fields of `/schema/commands`:
   a name and a type.
2. The payload of each kind comes from the ADR that defines it: ADR-0005, ADR-0006, ADR-0010, ADR-0012
   and ADR-0021.
3. A test compares the payload of each event that the bus emits with the entry of its kind, so that a
   new payload field without a schema entry fails, as ADR-0008 §9 asks.

## Scope (out)

- No event registry in `Engine.Core`. The list stays in the endpoint until a registry exists.
- No change to an event payload.

## Acceptance criteria

- [ ] `/schema/events` gives `body.consumed` with the field `bodyId` of the type `guid`, and
      `body.created` with `bodyId` and `kind`.
- [ ] The test of scope item 3 fails on an injected payload field that the schema does not list.
- [ ] The added fields change no existing field of the reply, so a client that reads the kind only
      continues to work.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.
