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
        new(DiagnosticCodes.CommandUnknown,        "Error",   "CMD"),
        new(DiagnosticCodes.CommandVersionStale,   "Error",   "CMD"),
        new(DiagnosticCodes.CommandBusBusy,        "Error",   "CMD"),
        new(DiagnosticCodes.CommandNothingToUndo,  "Error",   "CMD"), // ADR-0031 item 8, TASK-0093
        new(DiagnosticCodes.CommandNothingToRedo,  "Error",   "CMD"), // ADR-0031 item 8, TASK-0093
        new(DiagnosticCodes.QueryUnknown,          "Error",   "QRY"),
        new(DiagnosticCodes.ApiBadRequest,         "Error",   "API"),
        new(DiagnosticCodes.ApiWsInvalidSubscribe, "Error",   "API"),
        new(DiagnosticCodes.ApiWsLagged,           "Warning", "API"),
        new(DiagnosticCodes.ApiSubscriptionRemoved, "Warning", "API"),
        new(DiagnosticCodes.GeomCapMissing,        "Error",   "GEOM"),
        new(DiagnosticCodes.GeomInvalidParam,      "Error",   "GEOM"),
        new(DiagnosticCodes.GeomBodyNotFound,      "Error",   "GEOM"),
        new(DiagnosticCodes.GeomNativeOp,          "Error",   "GEOM"),
        new(DiagnosticCodes.GeomBackendInit,       "Error",   "GEOM"),
        new(DiagnosticCodes.ValBodyNameInvalid,    "Error",   "VAL"), // ADR-0028 item 3, TASK-0085
        new(DiagnosticCodes.ApiNotSupported,       "Error",   "API"), // ADR-0030 item 4, TASK-0092
        new(DiagnosticCodes.ApiOpenPending,        "Error",   "API"), // ADR-0030 item 4, TASK-0099
        new(DiagnosticCodes.IoBackendMismatch,     "Error",   "IO"),  // ADR-0015 item 6, TASK-0091
        new(DiagnosticCodes.IoNotEncodable,        "Error",   "IO"),  // ADR-0015 item 4, TASK-0091
        new(DiagnosticCodes.IoWriteFailed,         "Error",   "IO"),  // ADR-0015 item 4, TASK-0091
        new(DiagnosticCodes.IoLoadFailed,          "Error",   "IO"),  // ADR-0015 item 8, TASK-0091
        new(DiagnosticCodes.IoSaveFailed,          "Error",   "IO"),  // ADR-0015 item 9, TASK-0091
        new(DiagnosticCodes.IoTruncated,           "Warning", "IO"),  // ADR-0015 item 8, TASK-0091
        new(DiagnosticCodes.IoBackendVersion,      "Warning", "IO"),  // ADR-0015 item 6, TASK-0091
        new(DiagnosticCodes.SketchNotFound,        "Error",   "SKETCH"), // ADR-0033 item 6, TASK-0100
        new(DiagnosticCodes.SketchDatumNotFound,   "Error",   "SKETCH"), // ADR-0033 item 6, TASK-0100
        new(DiagnosticCodes.SketchItemNotFound,    "Error",   "SKETCH"), // ADR-0033 item 6, TASK-0100
        new(DiagnosticCodes.SketchLineInvalid,     "Error",   "SKETCH"), // ADR-0033 item 6, TASK-0100
        new(DiagnosticCodes.SketchConstraintInvalid, "Error", "SKETCH"), // ADR-0033 item 6, TASK-0100
        new(DiagnosticCodes.SketchNameInvalid,     "Error",   "SKETCH"), // ADR-0033 items 4 and 6, TASK-0100
        new(DiagnosticCodes.SketchProfileInvalid,  "Error",   "SKETCH"), // ADR-0036 item 4, TASK-0103
        new(DiagnosticCodes.SketchRebuildFailed,   "Warning", "SKETCH"), // ADR-0037 item 6, TASK-0108
        new(DiagnosticCodes.SketchExtrudeConsumed, "Warning", "SKETCH"), // ADR-0037 item 7, TASK-0108
        new(DiagnosticCodes.SketchIdInUse,         "Error",   "SKETCH"), // finding E86, TASK-0106
        new(DiagnosticCodes.SketchHoldDropped,     "Warning", "SKETCH"), // finding E87, TASK-0106
        new(DiagnosticCodes.SketchCurveInvalid,    "Error",   "SKETCH"), // phase K2d, TASK-0117
        new(DiagnosticCodes.AssetTooLarge,         "Error",   "ASSET"), // ADR-0044 item 3, TASK-0147
        new(DiagnosticCodes.AssetBadName,          "Error",   "ASSET"), // ADR-0044 item 1, TASK-0147
        new(DiagnosticCodes.AssetNotFound,         "Error",   "ASSET"), // ADR-0044 item 2, TASK-0147
        new(DiagnosticCodes.AssetCorrupt,          "Error",   "ASSET"), // ADR-0044 item 2, TASK-0147
        new(DiagnosticCodes.AssetWriteFailed,      "Error",   "ASSET"), // ADR-0044 item 2, TASK-0147
        new(DiagnosticCodes.AssetReadFailed,       "Error",   "ASSET"), // finding E114, TASK-0149
        new(DiagnosticCodes.ImageNotFound,         "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageIdInUse,          "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageAssetInvalid,     "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageDatumNotFound,    "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageSizeInvalid,      "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageScaleInvalid,     "Error",   "IMAGE"), // ADR-0045 item 6, TASK-0152
        new(DiagnosticCodes.ImageCalibrationInvalid, "Error", "IMAGE"), // ADR-0045 item 6, TASK-0152
    };

    public static IResult Handle() => Results.Json(Codes, ApiJson.Options);
}
