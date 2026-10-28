using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop.Ui;

// Where each sample of a frame goes: to the panels of the user interface, or to the state
// machine of intents (ADR-0027 item 4, TASK-0084). A pointer gesture that starts over a
// panel belongs to the panel until it ends; each other gesture goes to the intents. A mouse
// that moves with no button down goes to both, so that the panel shows a hover and the state
// machine sees nothing to do. The wheel follows the last place of the mouse. While a text
// field is active, keys and text go to the panel; else keys go to the intents and text goes
// nowhere. A lost focus goes to both and ends each gesture.
//
// The router needs no library: the host gives it a test of a point against the panels of
// the last frame, and whether a text field is active.
internal sealed class UiInputRouter
{
    private readonly Dictionary<(DeviceKind Kind, ulong Device, ulong Pointer), bool> _owners = new();
    private (double X, double Y)? _mouse;

    public (IReadOnlyList<InputSample> Panel, IReadOnlyList<InputSample> Intents) Route(
        IEnumerable<InputSample> frame, Func<double, double, bool> overPanel, bool textActive)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(overPanel);

        var panel = new List<InputSample>();
        var intents = new List<InputSample>();

        foreach (var sample in frame)
        {
            switch (sample)
            {
                case PointerSample pointer:
                    Pointer(pointer, overPanel, panel, intents);
                    break;

                case WheelSample:
                    (_mouse is { } at && overPanel(at.X, at.Y) ? panel : intents).Add(sample);
                    break;

                case KeySample:
                    (textActive ? panel : intents).Add(sample);
                    break;

                case TextSample when textActive:
                    panel.Add(sample);
                    break;

                case FocusLostSample:
                    _owners.Clear();
                    panel.Add(sample);
                    intents.Add(sample);
                    break;
            }
        }

        return (panel, intents);
    }

    private void Pointer(PointerSample sample, Func<double, double, bool> overPanel, List<InputSample> panel, List<InputSample> intents)
    {
        if (sample.Kind == DeviceKind.Mouse)
            _mouse = (sample.X, sample.Y);

        var key = (sample.Kind, sample.DeviceId, sample.PointerId);
        if (sample.Phase == PointerPhase.Down && !_owners.ContainsKey(key))
            _owners[key] = overPanel(sample.X, sample.Y);

        if (_owners.TryGetValue(key, out var toPanel))
        {
            (toPanel ? panel : intents).Add(sample);
            if (sample.Phase is PointerPhase.Up or PointerPhase.Cancel)
                _owners.Remove(key);
            return;
        }

        // A sample of a pointer that no gesture holds: a hover of the mouse goes to both; a
        // lone move or release of a finger goes to the intents, which ignore it.
        if (sample.Kind == DeviceKind.Mouse)
            panel.Add(sample);
        intents.Add(sample);
    }
}
