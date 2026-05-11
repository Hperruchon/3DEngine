using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Core;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// POST /queries handler.
//
// Body shape:
//   {
//     "name": string,           // required
//     "schemaVersion": integer, // required
//     "parameters": object      // required, may be {}
//   }
//
// Query registry is empty (TASK-0001 / TASK-0002 / TASK-0007). Every name
// produces a Rejected QueryResult<object> with E-QRY-UNKNOWN. Same rationale
// as Engine.Cli/Cli.cs §Query: Query is abstract, sentinels are forbidden,
// the API surfaces the existing diagnostic without dispatching.
internal static class QueriesEndpoint
{
    public static async Task<IResult> Handle(HttpContext context, EngineHost host)
    {
        if (!context.Request.HasJsonContentType())
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        QueryRequest? body;
        try
        {
            body = await context.Request
                .ReadFromJsonAsync<QueryRequest>(ApiJson.Options, context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return ApiErrorEnvelope.BadRequest($"Malformed JSON: {ex.Message}");
        }

        if (body is null)
            return ApiErrorEnvelope.BadRequest("Request body is required.");

        if (string.IsNullOrEmpty(body.Name))
            return ApiErrorEnvelope.BadRequest("Required field missing: name.");

        if (body.SchemaVersion is null)
            return ApiErrorEnvelope.BadRequest("Required field missing: schemaVersion.");

        var result = new QueryResult<object>(
            QueryName: body.Name,
            AsOfDocumentVersion: host.Document.Version,
            Result: null,
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: new ErrorDetail(
                DiagnosticCodes.QueryUnknown,
                $"No handler registered for query '{body.Name}'."),
            DurationMs: 0);

        return Results.Json(result, ApiJson.Options);
    }

    internal sealed record QueryRequest(
        string? Name,
        int? SchemaVersion,
        Dictionary<string, JsonElement>? Parameters);
}
