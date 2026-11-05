using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Core;
using Engine.Core.Export;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// ADR-0048 item 4 (phase P8g, TASK-0157): the live bodies of the document of the session as one binary STL
// file, for an agent (ADR-0002). The route reads the session and changes nothing, and the log does not
// change. It is outside the triad of ADR-0008: the bytes of a file are not the result of a query.
//
//   GET /export/stl   the bytes of the file, model/stl
internal static class ExportEndpoint
{
    public static async Task<IResult> Stl(EngineHost host, CancellationToken ct)
    {
        var (content, error) = await host.Session.ReadGeometry(StlExport.Write, ct);
        if (error is null)
            return Results.File(content!, "model/stl", "document.stl");

        return Results.Json(new ApiErrorEnvelope(new ApiErrorBody(error.Code, error.Message)), ApiJson.Options, statusCode: error.Code switch
        {
            DiagnosticCodes.GeomCapMissing => StatusCodes.Status501NotImplemented,
            // Finding E121 (TASK-0160): the bodies of the document do not fit the form of an STL file.
            DiagnosticCodes.ExportRange or DiagnosticCodes.ExportTooLarge => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        });
    }

    // ADR-0070 item 4 (phase K6a, TASK-0200): the live bodies as one STEP file of AP214, from the owned kernel.
    //
    //   GET /export/step   the bytes of the file, model/step
    public static async Task<IResult> Step(EngineHost host, CancellationToken ct)
    {
        var (content, error) = await host.Session.ReadGeometry((document, _) => StepExport.Write(document), ct);
        if (error is null)
            return Results.File(content!, "model/step", "document.step");

        return Results.Json(new ApiErrorEnvelope(new ApiErrorBody(error.Code, error.Message)), ApiJson.Options, statusCode: error.Code switch
        {
            DiagnosticCodes.ExportBodyFailed => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        });
    }
}
