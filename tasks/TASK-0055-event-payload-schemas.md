---
id: 0055
title: /schema/events gives the payload fields of each event kind
status: Done
phase: P0.22
opened: 2026-10-08
depends-on: [0064]
governed-by: [0011, 0013, 0016, 0019, 0021]
writes:
  create:
    - tasks/TASK-0055-event-payload-schemas.md
  modify:
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Schema/SchemaTypes.cs
    - Engine.Tests/Http/**
    - eng/test-floor.txt
    - docs/roadmap.md
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

- [x] `/schema/events` gives `body.consumed` with the field `bodyId` of the type `guid`, and
      `body.created` with `bodyId` and `kind`.
- [x] The test of scope item 3 fails on an injected payload field that the schema does not list.
- [x] The added fields change no existing field of the reply, so a client that reads the kind only
      continues to work.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.80 · the commit that carries this block.

Each entry of `/schema/events` gives its kind and the fields that a message of that kind carries, with their
types: for an event record, the fields of its payload; for a message of the transport, its own fields. A test
compares each payload that the bus emits in a scenario of each outcome with its entry.

## Method

**Mechanical.** The comparison test ran a creation, a subtraction and a rejected creation on the native
backend, and each of the four kinds that they emit agreed with its entry. With the field `kind` removed from
the entry of `body.created`, the test named the field. A forged record with an extra field gives one problem.

**Judgement.** The entry type gains one property, `payload`, and keeps `kind`, so a client that reads the kind
only sees no change. The kinds that no source emits yet (`command.progress`, `document.*`,
`validation.report`) have no field, as ADR-0005 defines none. A message of the transport keeps its fields
beside its kind on the wire, and its entry lists them as its payload; the comment of the endpoint says so.

**Weakest.** No test emits `command.cancelled`: only a handler that throws a cancellation gives it, and the
entry comes from reading `Engine.Core/CommandBus.cs`. The write set first named neither `eng/test-floor.txt`,
whose exact count TASK-0078 made each test task change, nor `docs/roadmap.md`; the close adds both.

## Progress

- 2026-10-17: the task is active, after TASK-0078 in the order of the owner of 2026-10-17.
- 2026-10-17: closed, after a clean build and a full local run of 498 tests (452 in `Engine.Tests`, 39 in
  `3DEngine.Tests`, 7 in `3DEngine.ImageTests`).
