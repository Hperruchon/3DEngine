namespace ThreeDEngine.Core.Input;

// The intent layer (ADR-0026 item 2): a state machine that turns the input frames of a
// window into intents with no kind of device. It reads no clock and no device of its own,
// so a test gives it any sequence of samples; the time of a tap comes from the samples.
//
// Touch and pen: one contact drags, two contacts drag their centre and pinch, and a short
// contact with no motion taps. Each contact has its own samples, so the machine places
// each moved contact of a pair first, and computes the centre and the spread once, at the
// end of the frame or before a sample of another kind; two contacts that move together in
// one frame then pan and do not pinch. Mouse: a left drag, a middle drag, and a left drag with
// Shift are three gestures, a short press with no motion clicks, and the wheel turns.
// Keyboard: the arrow keys move, with or without Shift, and plus and minus scale; H hides, and
// Control with Z or Y undoes or redoes. A still tap of two, three or four fingers is a gesture
// with no point. The
// settings give the action of each gesture. A press moves first by more than the slop, and
// the first motion then gives the whole distance from the press, so a touch and a mouse on
// one path give the same intents. In the mode Sketch, a tap or a click gives a point of the
// sketch, and the gestures of a hide and of a show all give nothing (ADR-0034 item 1). In the
// mode Scene, a press that starts on a handle of the gizmo, as the hit test of the host says, is a
// drag of the gizmo: a start, motions and an end, or a cancel (ADR-0038 item 5).
public sealed class IntentMachine
{
    private readonly IntentSettings _settings;
    private IntentMode _mode;

    // The touch and pen contacts that are down, in the order of their press.
    private readonly List<Contact> _contacts = new();
    private bool _twoContactsSeen;

    // The touch gesture since its first contact went down: the time of that press, whether no
    // contact moved beyond the slop, and the most contacts that were down at one time
    // (ADR-0029 item 4). Until TASK-0099 it counted each press, so two fingers with one bounce
    // gave a tap of three (finding V36 of the second codebase review of 2026-10-28).
    private ulong _gestureDownNs;
    private bool _gestureStill;
    private int _gestureContacts;
    private (double X, double Y) _lastCentre;
    private double _lastSpread;
    private bool _pairMoved;

    private MousePress? _mouse;

    // ADR-0038 item 5: the drag of a handle of the gizmo, from the press on the handle to the
    // release. Started is true after the press moved beyond the slop, and Cancelled after a
    // cancel, which makes the rest of the press give nothing.
    private sealed class GizmoPress(GizmoHandle handle)
    {
        public GizmoHandle Handle { get; } = handle;
        public bool Started { get; set; }
        public bool Cancelled { get; set; }
    }

    private GizmoPress? _gizmo;

    public IntentMachine(IntentSettings? settings = null)
    {
        _settings = (settings ?? IntentSettings.Default).Validated();
    }

    // The mode, which the host sets. A change of the mode ends the gesture in progress, as a
    // lost pointer does, so no gesture starts in one mode and ends in the other.
    public IntentMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode)
                return;
            _mode = value;
            Lose([]);
        }
    }

    // The hit test of the host (ADR-0038 item 5): the handle of the gizmo under a point of the
    // window, or null. Null when the host shows no gizmo.
    public Func<double, double, GizmoHandle?>? HitTest { get; set; }

    public IReadOnlyList<Intent> Step(IEnumerable<InputSample> frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var intents = new List<Intent>();

        foreach (var sample in frame)
        {
            if (sample is not PointerSample { Kind: DeviceKind.Touch or DeviceKind.Pen, Phase: PointerPhase.Move })
                MovePair(intents);

            switch (sample)
            {
                case PointerSample { Kind: DeviceKind.Touch or DeviceKind.Pen } contact:
                    Touch(contact, intents);
                    break;
                case PointerSample mouse:
                    Mouse(mouse, intents);
                    break;
                // Finding V53 of the codebase review of 2026-10-30 (b) (TASK-0124, answer Q2 A): a drag
                // of the gizmo keeps the view and the document still, so the wheel, the camera keys,
                // H, undo and redo give nothing while it runs. Escape still cancels it.
                case WheelSample when Dragging:
                    break;
                case WheelSample wheel:
                    Scale(Gesture.Wheel, Math.Pow(_settings.ZoomPerStep, wheel.Steps), intents);
                    break;
                case KeySample { Down: true } key:
                    Key(key, intents);
                    break;
                case FocusLostSample:
                    Lose(intents);
                    break;
            }
        }

        MovePair(intents);
        return intents;
    }

    // The motion of a pair of contacts since its last motion: the drag of its centre, and
    // the pinch of its spread.
    private void MovePair(List<Intent> intents)
    {
        if (!_pairMoved || _contacts.Count < 2)
            return;

        _pairMoved = false;
        var (centre, spread) = (Centre(), Spread());
        Motion(Gesture.TwoFingerDrag, centre.X - _lastCentre.X, centre.Y - _lastCentre.Y, intents);
        if (spread > 0 && _lastSpread > 0 && spread != _lastSpread)
            Scale(Gesture.Pinch, _lastSpread / spread, intents);
        (_lastCentre, _lastSpread) = (centre, spread);
    }

    private void Touch(PointerSample sample, List<Intent> intents)
    {
        var contact = _contacts.Find(c => c.Kind == sample.Kind && c.Pointer == sample.PointerId);
        switch (sample.Phase)
        {
            case PointerPhase.Down when contact is null:
                if (_contacts.Count == 0)
                {
                    (_gestureDownNs, _gestureStill, _gestureContacts) = (sample.TimestampNs, true, 0);
                    _gizmo = Handle(sample);
                    Hover(sample, intents);
                }
                else
                {
                    // A second contact stops a drag of the gizmo, and the pair pans and pinches.
                    CancelGizmo(intents);
                    _gizmo = null;
                }

                _contacts.Add(new Contact(sample));
                _gestureContacts = Math.Max(_gestureContacts, _contacts.Count);
                if (_contacts.Count == 2)
                    StartTwo();
                break;

            case PointerPhase.Move when contact is not null:
                if (_contacts.Count == 1 && _gizmo is not null)
                {
                    if (contact.MoveFrom(sample, _settings.DragSlopPixels) is { } delta)
                        DragGizmo(contact, sample, delta, intents);
                }
                else if (_contacts.Count == 1)
                {
                    if (contact.MoveFrom(sample, _settings.DragSlopPixels) is { } delta)
                        Motion(Gesture.OneFingerDrag, delta.Dx, delta.Dy, intents);
                }
                else if (_contacts.IndexOf(contact) < 2)
                {
                    contact.Place(sample);
                    _pairMoved = true;
                }

                if (contact.Displacement > _settings.DragSlopPixels)
                    _gestureStill = false;
                break;

            case PointerPhase.Up when contact is not null:
                _contacts.Remove(contact);
                if (_contacts.Count == 0)
                {
                    var cancelled = EndGizmo(sample, intents);
                    var held = sample.TimestampNs - contact.DownNs;
                    if (cancelled)
                    {
                        // Finding V65 of the codebase review of 2026-10-31 (c) (TASK-0151): the rest of
                        // a press that Escape cancelled gives nothing, also no tap.
                    }
                    else if (!_twoContactsSeen && !contact.Moved && held <= _settings.TapMaxNanoseconds)
                        Point(Gesture.Tap, contact.DownX, contact.DownY, intents);
                    else if (!_twoContactsSeen && !contact.Moved && held >= _settings.LongPressNanoseconds)
                        Point(Gesture.LongPress, contact.DownX, contact.DownY, intents);
                    else if (_twoContactsSeen && _gestureStill && sample.TimestampNs - _gestureDownNs <= _settings.TapMaxNanoseconds
                        && _gestureContacts switch { 2 => Gesture.TwoFingerTap, 3 => Gesture.ThreeFingerTap, 4 => Gesture.FourFingerTap, _ => (Gesture?)null } is { } tap)
                        NoPoint(tap, intents);
                    _twoContactsSeen = false;
                }
                else
                {
                    // The contacts that stay must not jump: each one drags from where it is.
                    foreach (var other in _contacts)
                        other.Hold();
                    if (_contacts.Count >= 2)
                        StartTwo();
                }

                break;

            case PointerPhase.Cancel:
                Lose(intents);
                break;
        }
    }

    private void StartTwo()
    {
        _twoContactsSeen = true;
        foreach (var contact in _contacts)
            contact.Hold();
        (_lastCentre, _lastSpread) = (Centre(), Spread());
    }

    private (double X, double Y) Centre()
        => ((_contacts[0].X + _contacts[1].X) / 2, (_contacts[0].Y + _contacts[1].Y) / 2);

    private double Spread()
    {
        var (dx, dy) = (_contacts[0].X - _contacts[1].X, _contacts[0].Y - _contacts[1].Y);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void Mouse(PointerSample sample, List<Intent> intents)
    {
        switch (sample.Phase)
        {
            case PointerPhase.Down when _mouse is null && (sample.Buttons & (PointerButtons.Left | PointerButtons.Middle | PointerButtons.Right)) != 0:
                _mouse = new MousePress(sample);
                _gizmo = _mouse.Button == PointerButtons.Left ? Handle(sample) : null;
                if (_mouse.Button == PointerButtons.Left)
                    Hover(sample, intents);
                break;

            case PointerPhase.Move when _mouse is not null && _gizmo is not null:
                if (_mouse.MoveFrom(sample, _settings.DragSlopPixels) is { } drag)
                    DragGizmo(_mouse, sample, drag, intents);
                break;

            case PointerPhase.Move when _mouse is null:
                Hover(sample, intents);
                break;

            case PointerPhase.Move when _mouse is not null:
                // A drag of the right button gives no gesture; it only stops a right click.
                if (_mouse.MoveFrom(sample, _settings.DragSlopPixels) is { } delta && _mouse.Button != PointerButtons.Right)
                {
                    var gesture = _mouse.Button == PointerButtons.Middle ? Gesture.MiddleDrag
                        : (_mouse.Modifiers & KeyModifiers.Shift) != 0 ? Gesture.ShiftLeftDrag
                        : Gesture.LeftDrag;
                    Motion(gesture, delta.Dx, delta.Dy, intents);
                }

                break;

            case PointerPhase.Up when _mouse is not null && (sample.Buttons & _mouse.Button) != 0:
                // Finding V65: and no click.
                if (!EndGizmo(sample, intents) && !_mouse.Moved && sample.TimestampNs - _mouse.DownNs <= _settings.TapMaxNanoseconds)
                    Point(_mouse.Button == PointerButtons.Right ? Gesture.RightClick : Gesture.Click, _mouse.DownX, _mouse.DownY, intents);
                _mouse = null;
                break;

            case PointerPhase.Cancel:
                Lose(intents);
                break;
        }
    }

    private void Key(KeySample key, List<Intent> intents)
    {
        if (Dragging && key.Key != InputKey.Escape)
            return;
        var step = _settings.PixelsPerKey;
        var motion = (key.Modifiers & KeyModifiers.Shift) != 0 ? Gesture.ShiftArrowKeys : Gesture.ArrowKeys;
        switch (key.Key)
        {
            case InputKey.ArrowLeft: Motion(motion, -step, 0, intents); break;
            case InputKey.ArrowRight: Motion(motion, step, 0, intents); break;
            case InputKey.ArrowUp: Motion(motion, 0, -step, intents); break;
            case InputKey.ArrowDown: Motion(motion, 0, step, intents); break;
            case InputKey.Plus: Scale(Gesture.PlusMinusKeys, _settings.ZoomPerStep, intents); break;
            case InputKey.Minus: Scale(Gesture.PlusMinusKeys, 1 / _settings.ZoomPerStep, intents); break;
            case InputKey.H: NoPoint((key.Modifiers & KeyModifiers.Shift) != 0 ? Gesture.ShiftHKey : Gesture.HKey, intents); break;
            case InputKey.Z when (key.Modifiers & KeyModifiers.Control) != 0:
                NoPoint((key.Modifiers & KeyModifiers.Shift) != 0 ? Gesture.ControlShiftZKey : Gesture.ControlZKey, intents);
                break;
            case InputKey.Y when (key.Modifiers & KeyModifiers.Control) != 0: NoPoint(Gesture.ControlYKey, intents); break;
            case InputKey.Escape: CancelGizmo(intents); break;
        }
    }

    // ADR-0041 item 6 (phase R11b): the point of the mark of a snap in the mode Sketch: the mouse with
    // no button, the press of the left button, and the first contact of a touch or a pen, so each
    // method shows the mark before the tap (objective 9).
    private void Hover(PointerSample sample, List<Intent> intents)
    {
        if (_mode == IntentMode.Sketch)
            intents.Add(new SketchHoverIntent(sample.X, sample.Y));
    }

    // A press on a handle of the gizmo holds, and no cancel ended it. Finding V58 of the codebase review
    // of 2026-10-31 (TASK-0133): a wheel and an undo before the slop still acted, and the drag then
    // started from a view or a body that had changed.
    private bool Dragging => _gizmo is { Cancelled: false };

    // A pointer was lost: the gesture in progress ends, and nothing stays down.
    private void Lose(List<Intent> intents)
    {
        CancelGizmo(intents);
        _gizmo = null;
        if (_contacts.Count > 0 || _mouse is not null)
            intents.Add(new CancelIntent());
        _contacts.Clear();
        _twoContactsSeen = false;
        _pairMoved = false;
        _mouse = null;
    }

    // The handle under a press in the mode Scene, as the hit test of the host gives it.
    private GizmoPress? Handle(PointerSample sample)
        => _mode == IntentMode.Scene && HitTest?.Invoke(sample.X, sample.Y) is { } handle ? new GizmoPress(handle) : null;

    // A motion of the press on a handle beyond the slop: the start with the point of the press,
    // then the point of the pointer.
    private void DragGizmo(Press press, PointerSample sample, (double Dx, double Dy) delta, List<Intent> intents)
    {
        if (_gizmo is not { Cancelled: false } gizmo)
            return;
        if (!gizmo.Started)
        {
            gizmo.Started = true;
            intents.Add(new GizmoStartIntent(gizmo.Handle, press.DownX, press.DownY));
        }
        if (delta.Dx != 0 || delta.Dy != 0)
            intents.Add(new GizmoMoveIntent(sample.X, sample.Y));
    }

    // The release of a press on a handle: the end of its drag. A drag starts only after the press
    // moved beyond the slop, and such a press never taps or clicks, so a press that did not move
    // gives its tap or its click and no end. True when a cancel ended the press, so the release
    // gives no tap and no click (finding V65).
    private bool EndGizmo(PointerSample sample, List<Intent> intents)
    {
        var gizmo = _gizmo;
        _gizmo = null;
        if (gizmo is { Started: true, Cancelled: false })
            intents.Add(new GizmoEndIntent(sample.X, sample.Y));
        return gizmo is { Cancelled: true };
    }

    // A cancel of the drag in progress: the rest of the press gives nothing.
    // Finding V64 of the codebase review of 2026-10-31 (b) (TASK-0144): a press that had not moved
    // beyond the slop stayed open, and a motion after Escape started the drag. Such a press is
    // cancelled too, with no intent, because no drag started.
    private void CancelGizmo(List<Intent> intents)
    {
        if (_gizmo is not { Cancelled: false } gizmo)
            return;
        gizmo.Cancelled = true;
        if (gizmo.Started)
            intents.Add(new GizmoCancelIntent());
    }

    private void Motion(Gesture gesture, double dx, double dy, List<Intent> intents)
    {
        if (dx == 0 && dy == 0)
            return;

        switch (_settings.ActionOf(gesture, _mode))
        {
            case IntentAction.Orbit:
                intents.Add(new OrbitIntent(-dx * _settings.OrbitDegreesPerPixel, dy * _settings.OrbitDegreesPerPixel));
                break;
            case IntentAction.Pan:
                intents.Add(new PanIntent(dx, dy));
                break;
            case IntentAction.Zoom when dy != 0:
                // A drag up comes closer.
                intents.Add(new ZoomIntent(Math.Pow(_settings.ZoomPerStep, -dy / _settings.PixelsPerZoomStep)));
                break;
        }
    }

    private void Scale(Gesture gesture, double factor, List<Intent> intents)
    {
        if (factor > 0 && factor != 1 && _settings.ActionOf(gesture, _mode) == IntentAction.Zoom)
            intents.Add(new ZoomIntent(factor));
    }

    private void Point(Gesture gesture, double x, double y, List<Intent> intents)
    {
        switch (_settings.ActionOf(gesture, _mode))
        {
            case IntentAction.Select:
                intents.Add(new SelectIntent(x, y));
                break;
            case IntentAction.Hide:
                intents.Add(new HideIntent(x, y));
                break;
            case IntentAction.SketchPoint:
                intents.Add(new SketchPointIntent(x, y));
                break;
        }
    }

    // A gesture with no point: hide the selection, show each hidden body (ADR-0029 item 4), undo
    // or redo (ADR-0032 item 9).
    private void NoPoint(Gesture gesture, List<Intent> intents)
    {
        switch (_settings.ActionOf(gesture, _mode))
        {
            case IntentAction.Hide:
                intents.Add(new HideIntent(null, null));
                break;
            case IntentAction.ShowAll:
                intents.Add(new ShowAllIntent());
                break;
            case IntentAction.Undo:
                intents.Add(new UndoIntent());
                break;
            case IntentAction.Redo:
                intents.Add(new RedoIntent());
                break;
        }
    }

    // A press of a pointer: where it went down, where its last motion ended, and whether
    // it moved by more than the slop.
    private class Press
    {
        protected Press(PointerSample sample)
        {
            (DownX, DownY, X, Y, DownNs) = (sample.X, sample.Y, sample.X, sample.Y, sample.TimestampNs);
        }

        public double DownX { get; }
        public double DownY { get; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public ulong DownNs { get; }
        public bool Moved { get; private set; }

        // The largest distance from the point of the press so far; Hold does not change it, so a
        // pair of fingers that never moves still taps (ADR-0029 item 4).
        public double Displacement { get; private set; }

        // The motion since the last one, or null while the press stays inside the slop.
        public (double Dx, double Dy)? MoveFrom(PointerSample sample, double slop)
        {
            Measure(sample);
            if (!Moved)
            {
                var (fromX, fromY) = (sample.X - DownX, sample.Y - DownY);
                if (Math.Sqrt(fromX * fromX + fromY * fromY) <= slop)
                    return null;
                Moved = true;
            }

            var delta = (sample.X - X, sample.Y - Y);
            (X, Y) = (sample.X, sample.Y);
            return delta;
        }

        public void Place(PointerSample sample)
        {
            Measure(sample);
            (X, Y) = (sample.X, sample.Y);
            Moved = true;
        }

        private void Measure(PointerSample sample)
        {
            var (dx, dy) = (sample.X - DownX, sample.Y - DownY);
            Displacement = Math.Max(Displacement, Math.Sqrt(dx * dx + dy * dy));
        }

        // The press no longer taps, and its next motion starts where it is.
        public void Hold() => Moved = true;
    }

    private sealed class Contact(PointerSample sample) : Press(sample)
    {
        public DeviceKind Kind { get; } = sample.Kind;
        public ulong Pointer { get; } = sample.PointerId;
    }

    private sealed class MousePress(PointerSample sample) : Press(sample)
    {
        public PointerButtons Button { get; } = (sample.Buttons & PointerButtons.Left) != 0 ? PointerButtons.Left
            : (sample.Buttons & PointerButtons.Middle) != 0 ? PointerButtons.Middle
            : PointerButtons.Right;
        public KeyModifiers Modifiers { get; } = sample.Modifiers;
    }
}
