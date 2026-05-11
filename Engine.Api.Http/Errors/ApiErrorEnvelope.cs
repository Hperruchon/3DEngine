using Engine.Api.Http.Json;
using Engine.Core;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Errors;

// Wire shape for transport-level errors. Engine-level errors (E-CMD-UNKNOWN
// etc.) flow through CommandResult.Error; this envelope is only for problems
// the API rejects before reaching the bus (malformed JSON, missing required
// fields, etc.). See TASK-0007 §5.
internal sealed record ApiErrorEnvelope(ApiErrorBody Error)
{
    public static IResult BadRequest(string message)
        => Results.Json(
            new ApiErrorEnvelope(new ApiErrorBody(DiagnosticCodes.ApiBadRequest, message)),
            ApiJson.Options,
            statusCode: StatusCodes.Status400BadRequest);
}

internal sealed record ApiErrorBody(string Code, string Message);
