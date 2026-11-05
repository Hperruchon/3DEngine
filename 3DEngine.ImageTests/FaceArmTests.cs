using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The arm of the button Face (ADR-0053 item 6, finding V77 of the second codebase review of 2026-11-03, TASK-0177): the
// button arms a tap when no sketch is active, a tap to select takes it, and each start of a sketch and of a calibration
// ends it. Finding V84 of the codebase review of 2026-11-05 (TASK-0181): the panel applies each of its actions to the
// arm, so a press of the real buttons drives it.
[Collection(DearImGui.Collection)]
public class FaceArmTests
{
    private static readonly SelectIntent Tap = new(10, 20);

    private static readonly System.Numerics.Vector2 Display = new(1280, 720);

    private static void Step(DesktopUi ui, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, System.Numerics.Vector2.One, []);
    }

    private static void Press(DesktopUi ui, string label)
    {
        var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
        var (x, y) = ((rectangle.X + rectangle.Z) / 2.0, (rectangle.Y + rectangle.W) / 2.0);
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Step(ui, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, phase, 0));
        Step(ui);
        Step(ui);
    }

    // V84: a press of Face arms the arm of the panel and gives its message, and a press of "Sketch XY" ends it.
    [Fact]
    public void A_Press_Of_The_Buttons_Of_The_Panel_Drives_The_Arm()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui);
        Step(ui);

        Press(ui, DesktopUi.SketchOnFaceLabel);
        Assert.Contains(ui.TakeActions(), action => action is SketchOnFaceAction);
        Assert.True(ui.FaceArm.Armed);
        Assert.Equal(FaceArm.ArmedMessage, ui.ArmMessage);

        Press(ui, DesktopUi.NewSketchButtons[0].Label);
        Assert.Contains(ui.TakeActions(), action => action is NewSketchAction);
        Assert.False(ui.FaceArm.Armed);
        Assert.Null(ui.ArmMessage);
    }

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
