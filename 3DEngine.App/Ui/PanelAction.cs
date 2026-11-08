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

// ADR-0102 (phase P9g, TASK-0316): the project path of the window. "Open project..." opens the dialog of the system for a
// project file in each folder; a recent row opens its path; "Save as..." and "Export STL..." open the save dialog of the
// system. Until TASK-0316 a save, an open and an export took the name of a field in the document folder (ADR-0030 item 3).
internal sealed record OpenProjectAction : PanelAction;

internal sealed record OpenRecentAction(string Path) : PanelAction;

internal sealed record SaveAsAction : PanelAction;

internal sealed record ExportStlAction : PanelAction;

// ADR-0102 item 5 (Q3 A): the answer to the question before a change of project while the document has no file.
internal enum ChangeAnswer
{
    SaveAs,
    Discard,
    Cancel,
}

internal sealed record AnswerAction(ChangeAnswer Answer) : PanelAction;

// ADR-0031 item 11: an Undo or a Redo of the session.
internal sealed record UndoAction : PanelAction;

internal sealed record RedoAction : PanelAction;

// ADR-0032 item 4: the numbers of an apply of the pad, for a new box or a move of a body.
internal sealed record CreateBoxAction(double X, double Y, double Z) : PanelAction;

// Phase K5b (ADR-0076, TASK-0244): the radius and the height of an apply of the pad, for a new cylinder.
internal sealed record CreateCylinderAction(double Radius, double Height) : PanelAction;

// Phase K5e (ADR-0089 item 5, TASK-0298): the sizes of an apply of the pad, for a new cone, sphere or torus.
internal sealed record CreateConeAction(double Radius, double Height) : PanelAction;

internal sealed record CreateSphereAction(double Radius) : PanelAction;

internal sealed record CreateTorusAction(double Radius, double TubeRadius) : PanelAction;

internal sealed record MoveBodyAction(Guid Body, double Dx, double Dy, double Dz) : PanelAction;

// ADR-0073 item 1 (phase R14e3, TASK-0227): a cut of the body with the tool, the command Subtract.
internal sealed record CutBodyAction(Guid Body, Guid Tool) : PanelAction;

// ADR-0049 item 8: the button Distance of a selected extrude, which asks the host for the pad with
// the distance in effect, and the new distance of an apply of that pad.
internal sealed record EditDistanceAction(Guid Body) : PanelAction;

// ADR-0083 D1 and D6 (TASK-0259): the file dialog of the system for an STL file; a new part document; the home screen; and
// the frame of the camera on the visible bodies. ADR-0102 item 1 (TASK-0316): the import has its own name, apart from the
// open of a project; until TASK-0316 it was OpenFileAction, "Open file".
internal sealed record ImportStlAction : PanelAction;

internal sealed record NewPartAction : PanelAction;

internal sealed record HomeAction : PanelAction;

internal sealed record FrameAction : PanelAction;

// ADR-0082 item 3 (TASK-0254): the new value of a constraint of the sketch of the selected extrude, from the pad of its chip
// on an edge, with no active sketch.
internal sealed record SetBodyValueAction(Guid Sketch, Guid Constraint, double Value) : PanelAction;

internal sealed record SetDistanceAction(Guid Body, double Distance) : PanelAction;

// ADR-0034 item 6: a new sketch on a datum, the edit of a live sketch, and the end of the mode.
internal sealed record NewSketchAction(Guid Datum) : PanelAction;

// ADR-0053 item 6 (phase K0i, TASK-0171): the button Face arms the next tap of the mode Scene for a sketch on a face.
internal sealed record SketchOnFaceAction : PanelAction;

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

// ADR-0075 item 3 (phase R12a, TASK-0236): a sketch on the selected face, as the armed tap of ADR-0053 item 6 makes it.
internal sealed record SketchOnSelectedFaceAction : PanelAction;

// ADR-0075 item 3: the selected face for the panel and the scene: its line, whether a sketch can start on it (a curved
// face has no one plane), and its outline in pixels of the window.
// ADR-0077 item 4 (phase R12b, TASK-0245): the text and the segments of the selected edge, or null.
internal sealed record FaceLine(string Text, bool CanSketch, IReadOnlyList<(System.Numerics.Vector2 A, System.Numerics.Vector2 B)> Outline,
    string? EdgeText = null, IReadOnlyList<(System.Numerics.Vector2 A, System.Numerics.Vector2 B)>? Edge = null);

// The line of the document file in the panel: the name of the file, or null for an
// unsaved document, and the result of the last save or open. Finding V99 of the codebase review of 2026-11-06 (c)
// (TASK-0232): a host with no files, as the phone, has a line too, with the message only, so that a rejection shows.
// ADR-0102 item 7 (TASK-0316): the folder of the file, which the top bar shows after its name, or null.
internal sealed record DocumentLine(string? FileName, string? Message, bool Files = true, string? Folder = null);

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

// ADR-0047 items 3 and 4: the proposal of a contour of a live image, and its accept or its reject.
internal sealed record ProposeContourAction(Guid Image) : PanelAction;

internal sealed record AcceptContourAction : PanelAction;

internal sealed record RejectContourAction : PanelAction;

// ADR-0046 items 3 and 4: what the window of the images shows. Calibrating is the image of the
// calibration, or null, and Points the number of its taps.
internal sealed record ImagePanel(IReadOnlyCollection<ImageRecord> Live, Guid? Calibrating, int Points, string? Message, int ProposalPoints = 0);
