using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The arm of the button Face (ADR-0053 item 6, finding V77 of the second codebase review of 2026-11-03, TASK-0177): the
// button arms a tap when no sketch is active, a tap to select takes it, and each start of a sketch and of a calibration
// ends it.
public class FaceArmTests
{
    private static readonly SelectIntent Tap = new(10, 20);

    [Fact]
    public void The_Button_Arms_A_Tap_And_The_Tap_Takes_It_One_Time()
    {
        var arm = new FaceArm();
        Assert.False(arm.Take(Tap, out _));

        Assert.Equal(FaceArm.ArmedMessage, arm.Panel(new SketchOnFaceAction(), sketchActive: false));
        Assert.False(arm.Take(new OrbitIntent(1, 1), out _));
        Assert.True(arm.Take(Tap, out var taken));

        Assert.Equal(Tap, taken);
        Assert.False(arm.Armed);
        Assert.False(arm.Take(Tap, out _));
    }

    [Fact]
    public void An_Active_Sketch_Does_Not_Arm_The_Button()
    {
        var arm = new FaceArm();

        Assert.Equal(FaceArm.ActiveMessage, arm.Panel(new SketchOnFaceAction(), sketchActive: true));
        Assert.False(arm.Armed);
    }

    // V77: Face, then a new sketch, an edit, an accept of a contour or a calibration, and Finish: the next tap selects.
    private static readonly PanelAction[] Starts =
    [
        new NewSketchAction(Guid.NewGuid()),
        new EditSketchAction(Guid.NewGuid()),
        new AcceptContourAction(),
        new StartCalibrationAction(Guid.NewGuid()),
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Each_Start_Of_A_Sketch_Or_A_Calibration_Ends_The_Arm(int start)
    {
        var arm = new FaceArm();
        arm.Panel(new SketchOnFaceAction(), sketchActive: false);

        Assert.Null(arm.Panel(Starts[start], sketchActive: false));
        arm.Panel(new FinishSketchAction(), sketchActive: true);

        Assert.False(arm.Armed);
        Assert.False(arm.Take(Tap, out _));
    }

    // An action that starts nothing keeps the arm, for example a rename of a body.
    [Fact]
    public void Another_Action_Keeps_The_Arm()
    {
        var arm = new FaceArm();
        arm.Panel(new SketchOnFaceAction(), sketchActive: false);

        Assert.Null(arm.Panel(new RenameBodyAction(Guid.NewGuid(), "board"), sketchActive: false));

        Assert.True(arm.Armed);
    }
}
