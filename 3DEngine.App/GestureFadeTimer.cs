using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop;

// ADR-0116 item 2 (phase R20a, TASK-0341): the time of a gesture of the view, for the fade of the ring and of the history arc.
// An orbit, a pan or a zoom starts or continues the gesture, and the gesture ends when no such intent came for Hold seconds.
// A drag gives an intent in each frame that the pointer moves, and a wheel gives one for each step, so the hold joins the
// frames of one gesture and the steps of one turn of the wheel.
internal sealed class GestureFadeTimer
{
    // The time after the last intent of the view until the ring comes back, in seconds.
    public const double Hold = 0.25;

    private double _left;

    // True while a gesture of the view runs.
    public bool Running => _left > 0;

    // Notes the intents of one frame and the time since the last frame, in seconds.
    public void Step(IEnumerable<Intent> intents, double seconds)
    {
        ArgumentNullException.ThrowIfNull(intents);
        _left = intents.Any(intent => intent is OrbitIntent or PanIntent or ZoomIntent) ? Hold : Math.Max(0, _left - seconds);
    }
}
