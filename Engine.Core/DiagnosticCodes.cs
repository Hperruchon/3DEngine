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

    // ADR-0030 item 4: the host accepted an open, and its next session holds the file,
    // so it refuses each command, save and open until then (TASK-0099).
    public const string ApiOpenPending = "E-API-OPEN-PENDING";
    public const string GeomCapMissing = "E-GEOM-CAP-MISSING";
    public const string GeomInvalidParam = "E-GEOM-INVALID-PARAM";
    public const string GeomBodyNotFound = "E-GEOM-BODY-NOT-FOUND";
    public const string GeomNativeOp = "E-GEOM-NATIVE-OP";
    public const string GeomBackendInit = "E-GEOM-BACKEND-INIT";

    // ADR-0058 item 3: GetFaces of a body with a fault, and of a body that the owned kernel cannot build.
    public const string GeomBodyFaulted = "E-GEOM-BODY-FAULTED";
    public const string GeomFacesFailed = "E-GEOM-FACES-FAILED";

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

    // Finding E114 of the codebase review of 2026-10-31 (c) (TASK-0149).
    public const string AssetReadFailed = "E-ASSET-READ-FAILED";

    // ADR-0045 (phase V2a, TASK-0152): an image on a datum and its calibration.
    public const string ImageNotFound = "E-IMAGE-NOT-FOUND";
    public const string ImageIdInUse = "E-IMAGE-ID-IN-USE";
    public const string ImageAssetInvalid = "E-IMAGE-ASSET-INVALID";
    public const string ImageDatumNotFound = "E-IMAGE-DATUM-NOT-FOUND";
    public const string ImageSizeInvalid = "E-IMAGE-SIZE-INVALID";
    public const string ImageScaleInvalid = "E-IMAGE-SCALE-INVALID";
    public const string ImageCalibrationInvalid = "E-IMAGE-CALIBRATION-INVALID";

    // ADR-0049 (phase K0f, TASK-0163): an edit of the distance of an extrude.
    public const string FeatureIdInUse = "E-FEATURE-ID-IN-USE";
    public const string FeatureNotFound = "E-FEATURE-NOT-FOUND";
    public const string FeatureRebuildFailed = "W-FEATURE-REBUILD-FAILED";

    // ADR-0051 (phase K0g, TASK-0166): a datum on a face of a feature.
    public const string DatumIdInUse = "E-DATUM-ID-IN-USE";
    public const string DatumReferenceInvalid = "E-DATUM-REFERENCE-INVALID";
    public const string DatumNameInvalid = "E-DATUM-NAME-INVALID";
    public const string DatumNotFound = "E-DATUM-NOT-FOUND";
    public const string DatumReferenceFailed = "W-DATUM-REFERENCE-FAILED";

    // ADR-0053 (phase K0i, TASK-0171): the face under a point of a hit.
    public const string DatumFaceNotFound = "E-DATUM-FACE-NOT-FOUND";

    // ADR-0052 (phase K0h, TASK-0167): an edge or a vertex of a feature as a reference of a sketch.
    public const string SketchReferenceInvalid = "E-SKETCH-REFERENCE-INVALID";
    public const string SketchReferenceCycle = "E-SKETCH-REFERENCE-CYCLE";
    public const string SketchReferenceFailed = "W-SKETCH-REFERENCE-FAILED";

    // Findings E121 and E122 of the codebase review of 2026-11-01 (TASK-0160): the STL export of ADR-0048.
    public const string ExportRange = "E-EXPORT-RANGE";
    public const string ExportTooLarge = "E-EXPORT-TOO-LARGE";
    public const string ExportWriteFailed = "E-EXPORT-WRITE-FAILED";

    // ADR-0070 (phase K6a, TASK-0200): a body that the STEP export does not write.
    public const string ExportBodyFailed = "E-EXPORT-BODY-FAILED";

    // Finding E155 of the codebase review of 2026-11-06 (b) (TASK-0224): an export of the command line read an assembly.
    // An assembly holds references and no body, and the engine does not resolve a reference (ADR-0071 item 7).
    public const string ExportKindRefused = "E-EXPORT-KIND-REFUSED";

    // ADR-0071 (phase P8b1, TASK-0202): the kind of a Document, and a reference of an assembly.
    public const string CommandKindRefused = "E-CMD-KIND-REFUSED";

    // Finding E163 of the codebase review of 2026-11-06 (d) (TASK-0242): a command with no value for a required field.
    public const string CommandFieldMissing = "E-CMD-FIELD-MISSING";
    public const string AssemblyReferenceInvalid = "E-ASSEMBLY-REFERENCE-INVALID";
    public const string AssemblyIdInUse = "E-ASSEMBLY-ID-IN-USE";
}
