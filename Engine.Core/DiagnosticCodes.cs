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
    public const string ApiSubscriptionRemoved = "W-API-SUBSCRIPTION-REMOVED";
    public const string GeomCapMissing = "E-GEOM-CAP-MISSING";
    public const string GeomInvalidParam = "E-GEOM-INVALID-PARAM";
    public const string GeomBodyNotFound = "E-GEOM-BODY-NOT-FOUND";
    public const string GeomNativeOp = "E-GEOM-NATIVE-OP";
    public const string GeomBackendInit = "E-GEOM-BACKEND-INIT";

    // ADR-0028 item 3: a name of a body that breaks the rules of a name.
    public const string ValBodyNameInvalid = "E-VAL-BODY-NAME-INVALID";

    // ADR-0015 item 6: a log entry of another backend than the backend of the replay.
    public const string IoBackendMismatch = "E-IO-BACKEND-MISMATCH";
}
