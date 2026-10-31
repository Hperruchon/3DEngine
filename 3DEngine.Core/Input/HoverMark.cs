namespace ThreeDEngine.Core.Input;

// ADR-0041 item 6 and finding V67 of the codebase review of 2026-10-31 (c) (TASK-0151): the mark of
// the snap shows under the mouse, and under a finger or a pen while it is down. After the intents of
// a frame, a cancel of a pointer, or a lift of a finger or a pen as the last pointer sample of the
// frame, clears the point of the mark. The mouse still hovers after its button goes up.
public static class HoverMark
{
    public static (double X, double Y)? After((double X, double Y)? hover, IReadOnlyList<InputSample> frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        for (var i = frame.Count - 1; i >= 0; i--)
        {
            if (frame[i] is not PointerSample pointer)
                continue;
            if (pointer.Phase == PointerPhase.Cancel)
                return null;
            return pointer.Kind is DeviceKind.Touch or DeviceKind.Pen && pointer.Phase == PointerPhase.Up ? null : hover;
        }

        return hover;
    }
}
