namespace Engine.Core;

// Seed diagnostic codes per TASK-0001. Mirrors docs/diagnostics.md.
// Append-only: codes never change meaning once shipped.
public static class DiagnosticCodes
{
    public const string CommandUnknown = "E-CMD-UNKNOWN";
    public const string CommandVersionStale = "E-CMD-VERSION-STALE";
    public const string CommandBusBusy = "E-CMD-BUS-BUSY";

    // ADR-0031 item 8: an Undo with no step to reverse, and a Redo with no undone step.
    public const string CommandNothingToUndo = "E-CMD-NOTHING-TO-UNDO";
    public const string CommandNothingToRedo = "E-CMD-NOTHING-TO-REDO";
    public const string QueryUnknown = "E-QRY-UNKNOWN";
    public const string ApiBadRequest = "E-API-BAD-REQUEST";
    public const string ApiWsInvalidSubscribe = "E-API-WS-INVALID-SUBSCRIBE";
    public const string ApiWsLagged = "W-API-WS-LAGGED";
    public const string ApiSubscriptionRemoved = "W-API-SUBSCRIPTION-REMOVED";

    // ADR-0030 item 4: a route that this host does not serve, for example an open
    // over HTTP in engine-api-http.
    public const string ApiNotSupported = "E-API-NOT-SUPPORTED";

    // ADR-0030 item 4: the host accepted an open and starts again, so it refuses each
    // command, save and open until then (TASK-0099).
    public const string ApiOpenPending = "E-API-OPEN-PENDING";
    public const string GeomCapMissing = "E-GEOM-CAP-MISSING";
    public const string GeomInvalidParam = "E-GEOM-INVALID-PARAM";
    public const string GeomBodyNotFound = "E-GEOM-BODY-NOT-FOUND";
    public const string GeomNativeOp = "E-GEOM-NATIVE-OP";
    public const string GeomBackendInit = "E-GEOM-BACKEND-INIT";

    // ADR-0028 item 3: a name of a body that breaks the rules of a name.
    public const string ValBodyNameInvalid = "E-VAL-BODY-NAME-INVALID";

    // ADR-0015 item 6: a log entry of another backend than the backend of the replay.
    public const string IoBackendMismatch = "E-IO-BACKEND-MISMATCH";

    // ADR-0015 items 4, 6, 8 and 9: the document file.
    public const string IoNotEncodable = "E-IO-NOT-ENCODABLE";
    public const string IoWriteFailed = "E-IO-WRITE-FAILED";
    public const string IoLoadFailed = "E-IO-LOAD-FAILED";
    public const string IoSaveFailed = "E-IO-SAVE-FAILED";
    public const string IoTruncated = "W-IO-TRUNCATED";
    public const string IoBackendVersion = "W-IO-BACKEND-VERSION";

    // ADR-0033 item 6: the refusals of a sketch command.
    public const string SketchNotFound = "E-SKETCH-NOT-FOUND";
    public const string SketchDatumNotFound = "E-SKETCH-DATUM-NOT-FOUND";
    public const string SketchItemNotFound = "E-SKETCH-ITEM-NOT-FOUND";
    public const string SketchLineInvalid = "E-SKETCH-LINE-INVALID";
    public const string SketchConstraintInvalid = "E-SKETCH-CONSTRAINT-INVALID";
    public const string SketchNameInvalid = "E-SKETCH-NAME-INVALID";

    // ADR-0036 item 4: the lines of a sketch do not form one closed loop, or the sketch
    // is not solved, so an extrude has no profile.
    public const string SketchProfileInvalid = "E-SKETCH-PROFILE-INVALID";

    // ADR-0037 items 6 and 7: an edit of a sketch applies, and an extrude of it does not
    // follow: its profile broke, or a later command consumed it.
    public const string SketchRebuildFailed = "W-SKETCH-REBUILD-FAILED";
    public const string SketchExtrudeConsumed = "W-SKETCH-EXTRUDE-CONSUMED";

    // Findings E86 and E87 of the codebase review of 2026-10-29: a command whose identifier
    // the log holds, and a move whose point the solver could not hold.
    public const string SketchIdInUse = "E-SKETCH-ID-IN-USE";
    public const string SketchHoldDropped = "W-SKETCH-HOLD-DROPPED";

    // Phase K2d (TASK-0117): a circle with a radius that is not a positive finite number, or an
    // arc whose centre, start and end are not three different points.
    public const string SketchCurveInvalid = "E-SKETCH-CURVE-INVALID";

    // ADR-0044 (phase V1b, TASK-0147): the store of the assets of an account.
    public const string AssetTooLarge = "E-ASSET-TOO-LARGE";
    public const string AssetBadName = "E-ASSET-BAD-NAME";
    public const string AssetNotFound = "E-ASSET-NOT-FOUND";
    public const string AssetCorrupt = "E-ASSET-CORRUPT";
    public const string AssetWriteFailed = "E-ASSET-WRITE-FAILED";

    // ADR-0045 (phase V2a, TASK-0152): an image on a datum and its calibration.
    public const string ImageNotFound = "E-IMAGE-NOT-FOUND";
    public const string ImageIdInUse = "E-IMAGE-ID-IN-USE";
    public const string ImageAssetInvalid = "E-IMAGE-ASSET-INVALID";
    public const string ImageDatumNotFound = "E-IMAGE-DATUM-NOT-FOUND";
    public const string ImageSizeInvalid = "E-IMAGE-SIZE-INVALID";
    public const string ImageScaleInvalid = "E-IMAGE-SCALE-INVALID";
    public const string ImageCalibrationInvalid = "E-IMAGE-CALIBRATION-INVALID";
}
