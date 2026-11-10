using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0111 item 3 (TASK-0331): while the commands of an accepted proposal apply, the window holds each action and each intent
// that can change the Document, and says why in the line of the document. The camera, the selection, the frame, the home
// screen and the tools that open a pad or choose a tool still work. An action that is not in the list of free actions is
// held, so a new action is held until a change says that it changes nothing.
//
// Finding V163 of the codebase review of 2026-11-09 (b): the accept applies its commands one at a time on the thread pool.
// Until TASK-0331 an Undo of the person between two of them removed the first one, and the proposal still said accepted.
internal static class AcceptHold
{
    public const string Text = "The commands of the proposal apply now. Do it again when they are done.";

    // The actions of the panel that change nothing of the Document.
    public static bool Holds(PanelAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action is not (SelectBodyAction or FrameAction or HomeAction or EditDistanceAction or SketchToolAction
            or ToggleSketchItemAction or SketchOnFaceAction or CancelCalibrationAction or AcceptProposalAction or RejectProposalAction);
    }

    // The intents that change the Document: a hide, a show of each body, an undo, a redo, a point of a sketch and the end of a
    // drag of the gizmo, which sends a Translate. The host turns a held end of a drag into a cancel, so the drag stops.
    public static bool Holds(Intent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return intent is HideIntent or ShowAllIntent or UndoIntent or RedoIntent or SketchPointIntent or GizmoEndIntent;
    }
}
