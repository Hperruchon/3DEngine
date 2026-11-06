using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0053 item 6: the arm of the button Face, a state of the client (ADR-0007). The button arms it when no sketch is
// active, and the next tap to select in the mode Scene takes it and starts a sketch on the face under it.
//
// Finding V77 of the second codebase review of 2026-11-03 (TASK-0177): the start of a new sketch, the edit of a sketch
// and the accept of a contour did not end the arm, so after Face, a new sketch and Finish, the next tap that selected a
// body made a datum and a sketch. Each start of the mode Sketch and the start of a calibration now end it.
// Finding 2 of the review of Codex of v0.221 (TASK-0237): the button "Sketch on face" of ADR-0075 also starts a sketch,
// so it ends the arm too.
internal sealed class FaceArm
{
    public const string ArmedMessage = "Tap a face of a body for the new sketch.";
    public const string ActiveMessage = "Finish the active sketch first.";

    public bool Armed { get; private set; }

    // The change of the arm for an action of the panel, and the message of the line of the document, or null when the
    // action does not touch the arm.
    public string? Panel(PanelAction action, bool sketchActive)
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case SketchOnFaceAction:
                Armed = !sketchActive;
                return Armed ? ArmedMessage : ActiveMessage;
            case NewSketchAction or EditSketchAction or AcceptContourAction or StartCalibrationAction or SketchOnSelectedFaceAction:
                Armed = false;
                return null;
            default:
                return null;
        }
    }

    // True when the arm takes the intent: a tap to select while armed. The arm then ends.
    public bool Take(Intent intent, out SelectIntent tap)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (Armed && intent is SelectIntent select)
        {
            Armed = false;
            tap = select;
            return true;
        }

        tap = null!;
        return false;
    }
}
