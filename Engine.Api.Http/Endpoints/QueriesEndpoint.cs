using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Queries;
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
// Response: HTTP 200 with QueryResult JSON when the engine answers.
// HTTP 4xx only for transport-level problems.
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

        if (body.Name == "GetBoundingBox")
        {
            if (body.Parameters is null
                || !body.Parameters.TryGetValue("bodyId", out var idElement))
            {
                return ApiErrorEnvelope.BadRequest("GetBoundingBox requires parameters.bodyId.");
            }
            if (idElement.ValueKind != JsonValueKind.String
                || !Guid.TryParse(idElement.GetString(), out var bodyId))
            {
                return ApiErrorEnvelope.BadRequest("GetBoundingBox parameter 'bodyId' must be a GUID string.");
            }

            var typed = await host.QueryBus
                .Query<Aabb>(new GetBoundingBoxQuery { BodyId = bodyId }, context.RequestAborted)
                .ConfigureAwait(false);
            return Results.Json(typed, ApiJson.Options);
        }

        // Unknown query — same rationale as CommandsEndpoint's unknown branch:
        // surface the existing E-QRY-UNKNOWN diagnostic directly.
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
