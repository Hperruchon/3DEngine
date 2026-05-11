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
}
