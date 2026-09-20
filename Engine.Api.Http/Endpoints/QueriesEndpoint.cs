using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Hosting;
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

        // Per ADR-0016: same three steps as CommandsEndpoint.
        if (!host.QueryRegistry.TryFind(body.Name, body.SchemaVersion.Value, out var handler))
        {
            // Unknown query — surface the existing E-QRY-UNKNOWN diagnostic.
            var unknown = new QueryResult<object>(
                QueryName: body.Name,
                AsOfDocumentVersion: host.Document.Version,
                Result: null,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: new ErrorDetail(
                    DiagnosticCodes.QueryUnknown,
                    $"No handler registered for query '{body.Name}'@{body.SchemaVersion}."),
                DurationMs: 0);
            return Results.Json(unknown, ApiJson.Options);
        }

        var bound = ParameterBinder.Bind(
            handler.Parameters,
            JsonParameters.AsRaw(body.Parameters));

        if (!bound.IsSuccess)
            return ApiErrorEnvelope.BadRequest(bound.FirstMessage);

        var query = handler.Create(new QueryInput(bound.Values!, Guid.NewGuid()));

        // One concrete result type today. GetBoundingBox is the only registered
        // query and its result is an Aabb. See ADR-0016 "Next".
        var result = await host.QueryBus
            .Query<Aabb>(query, context.RequestAborted)
            .ConfigureAwait(false);

        return Results.Json(result, ApiJson.Options);
    }

    internal sealed record QueryRequest(
        string? Name,
        int? SchemaVersion,
        Dictionary<string, JsonElement>? Parameters);
}
