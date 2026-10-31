using Engine.Contracts.Images;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop.Ui;

// What a control of the panel asks for (ADR-0029 item 7, TASK-0086). The panel changes nothing
// itself; the host applies each action as a command of the session or as a change of the
// selection, as it applies an intent.
internal abstract record PanelAction;

internal sealed record SelectBodyAction(Guid Body) : PanelAction;

internal sealed record RenameBodyAction(Guid Body, string Name) : PanelAction;

internal sealed record SetVisibleAction(Guid Body, bool Visible) : PanelAction;

internal sealed record ShowAllAction : PanelAction;

// ADR-0030 item 3: a save to a name and an open of a name in the document folder.
internal sealed record SaveDocumentAction(string Name) : PanelAction;

internal sealed record OpenDocumentAction(string Name) : PanelAction;

// ADR-0031 item 11: an Undo or a Redo of the session.
internal sealed record UndoAction : PanelAction;

internal sealed record RedoAction : PanelAction;

// ADR-0032 item 4: the numbers of an apply of the pad, for a new box or a move of a body.
internal sealed record CreateBoxAction(double X, double Y, double Z) : PanelAction;

internal sealed record MoveBodyAction(Guid Body, double Dx, double Dy, double Dz) : PanelAction;

// ADR-0034 item 6: a new sketch on a datum, the edit of a live sketch, and the end of the mode.
internal sealed record NewSketchAction(Guid Datum) : PanelAction;

internal sealed record EditSketchAction(Guid Sketch) : PanelAction;

internal sealed record FinishSketchAction : PanelAction;

// ADR-0034 items 4, 7 and 8: the tool, the end and the close of a chain, the next point from the
// pad, and a row that selects an item.
internal sealed record SketchToolAction(SketchToolKind Kind) : PanelAction;

internal sealed record EndChainAction : PanelAction;

internal sealed record CloseChainAction : PanelAction;

internal sealed record NextPointAction(double A, double B) : PanelAction;

internal sealed record ToggleSketchItemAction(Guid Item) : PanelAction;

// ADR-0034 item 8: the commands of the window of the sketch. The items of a constraint are in the
// order of its kind (ADR-0033 item 3).
internal sealed record AddConstraintAction(string Kind, IReadOnlyList<Guid> Items, IReadOnlyList<double> Values) : PanelAction;

internal sealed record SetConstraintValueAction(Guid Constraint, IReadOnlyList<double> Values) : PanelAction;

internal sealed record MoveSketchPointAction(Guid Point, double X, double Y) : PanelAction;

internal sealed record DeleteSketchItemsAction(IReadOnlyList<Guid> Items) : PanelAction;

// ADR-0036 item 9: an extrude of the active sketch by the signed distance of the pad.
internal sealed record ExtrudeAction(double Distance) : PanelAction;

// The line of the document file in the panel: the name of the file, or null for an
// unsaved document, and the result of the last save or open.
internal sealed record DocumentLine(string? FileName, string? Message);

// ADR-0034 item 8: what the window of the sketch shows. Live holds each live sketch; Active is the
// record of the active sketch, or null in the mode Scene.
internal sealed record SketchPanel(
    IReadOnlyCollection<Engine.Core.Queries.SketchInfo> Live,
    Engine.Core.Queries.SketchInfo? Active,
    SketchToolKind Tool,
    bool ChainOpen,
    IReadOnlyList<Guid> Selection,
    string? Message);

// ADR-0046 items 3 and 4: the place of a PNG file on an origin plane, and the calibration of a live
// image by two taps on it and a real distance.
internal sealed record PlaceImageAction(string Path, Guid Datum) : PanelAction;

internal sealed record StartCalibrationAction(Guid Image) : PanelAction;

internal sealed record CalibrateImageAction(double Distance) : PanelAction;

internal sealed record CancelCalibrationAction : PanelAction;

// ADR-0046 items 3 and 4: what the window of the images shows. Calibrating is the image of the
// calibration, or null, and Points the number of its taps.
internal sealed record ImagePanel(IReadOnlyCollection<ImageRecord> Live, Guid? Calibrating, int Points, string? Message);
