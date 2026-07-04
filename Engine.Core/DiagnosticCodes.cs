namespace Engine.Core;

// Seed diagnostic codes per TASK-0001. Mirrors docs/diagnostics.md.
// Append-only: codes never change meaning once shipped.
public static class DiagnosticCodes
{
    public const string CommandUnknown = "E-CMD-UNKNOWN";
    public const string CommandVersionStale = "E-CMD-VERSION-STALE";
    public const string CommandBusBusy = "E-CMD-BUS-BUSY";
    public const string QueryUnknown = "E-QRY-UNKNOWN";
    public const string ApiBadRequest = "E-API-BAD-REQUEST";
    public const string ApiWsInvalidSubscribe = "E-API-WS-INVALID-SUBSCRIBE";
    public const string ApiWsLagged = "W-API-WS-LAGGED";
    public const string GeomCapMissing = "E-GEOM-CAP-MISSING";
    public const string GeomInvalidParam = "E-GEOM-INVALID-PARAM";
    public const string GeomBodyNotFound = "E-GEOM-BODY-NOT-FOUND";
    public const string GeomNativeOp = "E-GEOM-NATIVE-OP";
    public const string GeomBackendInit = "E-GEOM-BACKEND-INIT";
}
