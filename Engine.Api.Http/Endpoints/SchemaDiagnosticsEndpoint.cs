using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Engine.Core;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/diagnostics
//
// Per ADR-0008 §9 and TASK-0009 §7. Hand-mirrors Engine.Core/DiagnosticCodes.cs.
// The P2 gate (TASK-0003) keeps DiagnosticCodes in sync with docs/diagnostics.md;
// the gate test in Engine.Tests/Http/SchemaEndpointGateTests adds a third
// consumer that fails if a constant is missing from this mirror.
internal static class SchemaDiagnosticsEndpoint
{
    public static readonly IReadOnlyList<DiagnosticCodeEntry> Codes = new DiagnosticCodeEntry[]
    {
        new(DiagnosticCodes.CommandUnknown,      "Error", "CMD"),
        new(DiagnosticCodes.CommandVersionStale, "Error", "CMD"),
        new(DiagnosticCodes.CommandBusBusy,      "Error", "CMD"),
        new(DiagnosticCodes.QueryUnknown,        "Error", "QRY"),
        new(DiagnosticCodes.ApiBadRequest,       "Error", "API"),
    };

    public static IResult Handle() => Results.Json(Codes, ApiJson.Options);
}
