using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Tests;

// Finding V67 of the codebase review of 2026-10-31 (c) (TASK-0151): the mark of the snap stayed after a
// finger lifted. ADR-0041 item 6 shows it under the mouse, and under a finger or a pen while it is down.
public class HoverMarkTests
{
    private static readonly (double X, double Y) Mark = (50, 60);

    private static PointerSample Pointer(DeviceKind kind, PointerPhase phase)
        => new(kind, 1, 0, 50, 60, 1, kind == DeviceKind.Mouse ? PointerButtons.Left : PointerButtons.None, KeyModifiers.None, phase, 0);

    [Theory]
    [InlineData(DeviceKind.Touch, PointerPhase.Up, false)]
    [InlineData(DeviceKind.Pen, PointerPhase.Up, false)]
    [InlineData(DeviceKind.Mouse, PointerPhase.Cancel, false)]
    [InlineData(DeviceKind.Touch, PointerPhase.Cancel, false)]
    [InlineData(DeviceKind.Mouse, PointerPhase.Up, true)]
    [InlineData(DeviceKind.Touch, PointerPhase.Down, true)]
    [InlineData(DeviceKind.Touch, PointerPhase.Move, true)]
    public void The_Mark_Goes_When_A_Finger_Or_A_Pen_Lifts_Or_A_Pointer_Is_Lost(DeviceKind kind, PointerPhase phase, bool stays)
    {
        Assert.Equal(stays ? Mark : null, HoverMark.After(Mark, [Pointer(kind, phase)]));
    }

    // The last pointer sample of the frame decides: a finger that lifts and a new finger that goes down
    // keep the mark, and a frame with no pointer sample keeps it.
    [Fact]
    public void The_Last_Pointer_Sample_Of_The_Frame_Decides()
    {
        Assert.Equal(Mark, HoverMark.After(Mark, [Pointer(DeviceKind.Touch, PointerPhase.Up), Pointer(DeviceKind.Touch, PointerPhase.Down)]));
        Assert.Null(HoverMark.After(Mark, [Pointer(DeviceKind.Touch, PointerPhase.Down), Pointer(DeviceKind.Touch, PointerPhase.Up)]));
        Assert.Equal(Mark, HoverMark.After(Mark, [new KeySample(InputKey.Escape, true, KeyModifiers.None, 0)]));
    }
}
