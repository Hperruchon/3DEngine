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

    // The actions of the panel that change nothing of the Document. ADR-0114 item 3 (finding V165 of the codebase review of
    // 2026-11-10, TASK-0339): a tap on the path line selects a level, and a focus shows one body; neither writes to the log.
    public static bool Holds(PanelAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action is not (SelectBodyAction or PathAction or FocusAction or EndFocusAction or FrameAction or HomeAction
            or EditDistanceAction or SketchToolAction or ToggleSketchItemAction or SketchOnFaceAction or CancelCalibrationAction
            or AcceptProposalAction or RejectProposalAction);
    }

    // ADR-0114 item 4 (finding T181, TASK-0339): the decision of the host, out of NativeThreeDEngine, so a test reads it. An
    // accept runs while the task of the accept is not null; the host sets it to null when it reads the outcome.
    public static bool Held(PanelAction action, Task? accept) => accept is not null && Holds(action);

    // The intent that the host applies: the intent itself, nothing for a held intent, or a cancel for a held end of a drag of
    // the gizmo, so the drag stops. Held is true when the host must show Text.
    public static (Intent? Intent, bool Held) Pass(Intent intent, Task? accept)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (accept is null || !Holds(intent))
            return (intent, false);
        return (intent is GizmoEndIntent ? new GizmoCancelIntent() : null, true);
    }

    // The intents that change the Document: a hide, a show of each body, an undo, a redo, a point of a sketch and the end of a
    // drag of the gizmo, which sends a Translate. The host turns a held end of a drag into a cancel, so the drag stops.
    public static bool Holds(Intent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return intent is HideIntent or ShowAllIntent or UndoIntent or RedoIntent or SketchPointIntent or GizmoEndIntent;
    }
}
