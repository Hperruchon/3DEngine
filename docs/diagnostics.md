# Diagnostic code registry

All `E-`/`W-`/`I-` codes used in `Engine.*` code MUST appear here. Append-only. Codes are stable: once shipped, a code does not change meaning. Per ADR-0008 §4.

## Conventions

- **Severity prefix:** `E-` error · `W-` warning · `I-` info
- **Subsystem token:** `CMD` (command bus), `QRY` (query bus), `GEOM` (geometry), `IO` (persistence/transport), `VAL` (validation). Add new tokens as subsystems land.
- **Format:** `<severity>-<subsystem>-<short-tag>` — short, kebab-style, stable.

## Active codes

| Code | Severity | Meaning | Where raised |
|---|---|---|---|
| `E-CMD-UNKNOWN` | Error | Command name + schema version is not registered in `CommandRegistry`. | `CommandBus.Apply` when no handler matches. |
| `E-CMD-VERSION-STALE` | Error | Submitted command's `ExpectedDocumentVersion` does not match the Document's current `Version`. | `CommandBus.Apply` optimistic check. |
| `E-CMD-BUS-BUSY` | Error | Inbound command queue is full; submission rejected without enqueueing. | Reserved for the transport task; not raised in P0 (no inbound queue yet). |
| `E-QRY-UNKNOWN` | Error | Query name + schema version is not registered in `QueryRegistry`. | `QueryBus.Query` when no handler matches. |

## Reserved namespaces

- `X-…` — plugin-defined codes (V2, not used yet).

## Adding a code

1. Add the row above. Pick a stable, namespaced code.
2. Reference the code from `Engine.Core/DiagnosticCodes.cs` constants.
3. Same PR as the code that raises it.

Removing a code is forbidden. Mark obsolete in this file if no longer raised.
