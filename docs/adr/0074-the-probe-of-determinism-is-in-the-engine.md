---
id: 0074
title: The probe of the determinism hash is a part of the engine, and the phone runs it
status: Accepted
topic: Determinism, platform
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Determinism/**
  - eng/compare-determinism.js
  - eng/android/smoke-emulator.sh
enforced-by: Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
---

# ADR-0074 — The probe of the determinism hash is a part of the engine, and the phone runs it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Anti-objective 16 says that a replay gives the same result on Windows, Linux and macOS, on x64 and arm64, and on Android
on arm64. ADR-0061 item 8 says that the hash of Android must be equal to the hashes of the three desktop runners, and
that a hash that differs stops the merge of the phase that shows it. Since TASK-0156 the test
`CrossPlatformHashTests` of `Engine.Tests` replays one log with geometry and solved sketches and writes the SHA-256 of
the exact bits of its state; the job "Compare the determinism hashes" compares the three files. The program of the phone
cannot reference a test project. Since R14f2 the pipeline starts the APK on the emulator of Android for x86-64 up to its
first frame.

The owner ordered R14f under the standing order of 2026-11-05, which answers each question with the recommended option.
This record is Accepted under that order; the owner checks it on 2026-11-08.

The options:

- **A.** The log, the state and one call of the replay move into `Engine.Core` as `CrossPlatformProbe`; the test and the
  program of the phone call it.
- **B.** A new library of the probe, which the test and the program reference.
- **C.** The program of the phone holds a copy of the log and of the state.

A keeps one copy and adds no project; the probe uses the replay and the commands of `Engine.Core` only. B adds a project
and a class of the gates for 140 lines. C gives two copies that can drift apart, so two equal hashes would prove less.
**Recommended: A.**

## Decision

1. **The probe.** `Engine.Core/Determinism/CrossPlatformProbe.cs` holds the log (`Log`), the text and the SHA-256 of a
   state (`State`), and `Run`, which replays the log on a backend that the caller gives and returns the hash and the
   lines. It writes nothing to a document of a person: the replay has its own Document. The bytes of the hash do not
   change with the move.
2. **The desktop.** `CrossPlatformHashTests` checks the replay as before and writes the file of `DETERMINISM_HASH_FILE`.
3. **The phone.** The activity has the fixed name `org.threedengine.phone.MainActivity`. With the extra `probe`,
   `PhoneHost` runs the probe on its own backend before the session, and writes the hash and each line to the log.
4. **The pipeline.** `eng/android/smoke-emulator.sh` starts the program with the extra, writes the hash file of the phone
   in the form of the files of the runners, and fails with no hash. The job "Compare the determinism hashes" waits for
   the job of the emulator and compares four platforms: the three runners and `android-x64`.

## Consequences

**Good:** each push compares the bits of one replay on four platforms, and a difference stops the merge, as ADR-0061
item 8 asks. One copy of the probe serves each platform.

**Bad:** `Engine.Core` holds a fixed log that only the probe uses. The comparison waits for the emulator, about twenty
minutes. The emulator is x86-64; the hash of arm64, the ABI of the phone, needs a device or an emulator of arm64.

**Next:** the hash of the S23 Ultra in the test of the owner; an emulator of arm64 if a runner gives one.

## History

- 2026-11-06 · TASK-0233 · The record, under the standing order of 2026-11-05.
- 2026-11-08 · TASK-0299 · The log of the probe holds the cone, the sphere and the torus of ADR-0089 (phase K5f).
