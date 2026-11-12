using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0127 (phase R18g, TASK-0354): the fixed controls and the status as in the render: the step of Undo, the count of the
// log, the state of the agent, "saved" or "not saved", and Frame and Home out of the row of a landscape display. ADR-0127
// names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class FixedStatusTests
{
    private static readonly Vector2 Desktop = new(1280, 720);
    private static readonly Vector2 Phone = new(390, 844);
    private const string Address = "http://127.0.0.1:5180";

    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody>? bodies = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies ?? [], null, null);
    }

    // Items 1 and 2: the count of the log and the step of Undo follow the history of the command bus (ADR-0031 item 2), from
    // the log of a read and the later events, as the host subscribes them; a text that the window expected names its step,
    // and a rejected command leaves no expected text behind.
    [Fact]
    public async Task The_Log_And_The_Step_Of_Undo_Follow_The_History()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        var steps = new StepTexts();
        Assert.Null(steps.LogCount);
        using var subscription = await host.Session.Read((document, _) =>
        {
            steps.Seed([.. document.Log.Select(entry => entry.Command.Name)]);
            return host.Subscribe(steps.Take);
        });
        Assert.Equal((1, "Create box"), (steps.LogCount, steps.Undo));

        async Task Apply(Command command, CommandStatus status = CommandStatus.Applied)
            => Assert.Equal(status, (await host.Session.Apply(command)).Status);

        steps.Expect("CreateBox", "Box 2");
        await Apply(new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        Assert.Equal((2, "Box 2"), (steps.LogCount, steps.Undo));

        await Apply(new UndoCommand());
        Assert.Equal((3, "Create box"), (steps.LogCount, steps.Undo));
        await Apply(new RedoCommand());
        Assert.Equal((4, "Box 2"), (steps.LogCount, steps.Undo));
        await Apply(new UndoCommand());
        await Apply(new UndoCommand());
        Assert.Equal((6, (string?)null), (steps.LogCount, steps.Undo));

        // A new step clears the undone steps, so a Redo is rejected and adds no entry.
        await Apply(new NoOpCommand { Echo = "a" });
        Assert.Equal((7, "No op"), (steps.LogCount, steps.Undo));
        await Apply(new RedoCommand(), CommandStatus.Rejected);
        Assert.Equal(7, steps.LogCount);

        steps.Expect("NoOp", "never");
        await Apply(new NoOpCommand { Echo = "b", ExpectedDocumentVersion = 999 }, CommandStatus.Rejected);
        await Apply(new NoOpCommand { Echo = "c" });
        Assert.Equal((8, "No op"), (steps.LogCount, steps.Undo));

        // A text for another command waits for its command.
        steps.Expect("CreateBox", "Box 3");
        await Apply(new NoOpCommand { Echo = "d" });
        Assert.Equal("No op", steps.Undo);
        await Apply(new CreateBoxCommand { SizeX = 3, SizeY = 3, SizeZ = 3 });
        Assert.Equal((10, "Box 3"), (steps.LogCount, steps.Undo));
        steps.Expect("CreateBox", "gone");
        steps.Forget();
        await Apply(new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 4 });
        Assert.Equal("Create box", steps.Undo);
    }

    // Item 2: the texts of the steps: a value of a constraint with its label, an angle in degrees, a distance, the named values
    // of a proposal, and the name of a command in words.
    [Fact]
    public void The_Text_Of_A_Step_Names_The_Value()
    {
        var (sketchId, a, b, length, angle) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new SketchInfo(sketchId, Guid.NewGuid(), "Plate", [new(sketchId, 0, 0), new(a, 0, 0), new(b, 60, 0)], [], [], [],
            [new(Guid.NewGuid(), "fixed", [a], [], 0), new(Guid.NewGuid(), "horizontal", [a, b], [], 0), new(length, "distance", [a, b], [60], 0), new(angle, "angle", [a, b], [1, 0], 0)],
            true, 0);

        Assert.Equal("C3 = 75", StepTexts.ValueText([sketch], length, [75]));
        Assert.Equal("C4 = 45 deg", StepTexts.ValueText([sketch], angle, [Math.Sqrt(0.5), Math.Sqrt(0.5)]));
        Assert.Null(StepTexts.ValueText([sketch], Guid.NewGuid(), [75]));
        Assert.Equal("distance = 30", StepTexts.DistanceText(30));
        Assert.Equal("Set sketch constraint value", StepTexts.Readable("SetSketchConstraintValue"));
        Assert.Equal("No op", StepTexts.Readable("NoOp"));

        var card = new ProposalCard(Guid.NewGuid(), "Wider plate", ["SetSketchConstraintValue a b 75", "NoOp x", "SetExtrudeDistance e 30"], Values:
        [
            new ProposedValue(sketchId, length, null, 75) { Name = "Plate", Label = "C3" },
            null,
            new ProposedValue(null, null, Guid.NewGuid(), 30) { Name = "Bracket", Label = ProposedValues.DistanceLabel },
        ]);
        Assert.Equal([("SetSketchConstraintValue", "C3 = 75"), ("SetExtrudeDistance", "distance = 30")], StepTexts.ProposalTexts(card).ToList());
    }

    // Items 1 and 3: the status line gives the log, the state, the agent while a client listens, and the address; a narrow room
    // drops the address first, then the agent, then the line.
    [Fact]
    public void The_Status_Line_Gives_The_Log_The_State_And_The_Agent()
    {
        using var ui = new DesktopUi(Address, Document.Millimetre);
        ui.ShowStatus(new SessionStatus(7, true, null, null));
        Step(ui, Desktop);

        Assert.Equal($"log 7 · all correct · agent connected · {Address}", ui.StatusText([]));
        LiveBody[] failed = [new(Guid.NewGuid(), "Box", null, true, new BodyFault("E-TEST", "a fault"))];
        Assert.Equal($"log 7 · 1 failed · agent connected · {Address}", ui.StatusText(failed));
        ui.ShowStatus(new SessionStatus(7, false, null, null));
        Assert.Equal($"log 7 · all correct · {Address}", ui.StatusText([]));

        ui.ShowStatus(new SessionStatus(7, true, null, null));
        var shown = new List<string?>();
        for (var width = 1280; width >= 200; width -= 4)
        {
            Step(ui, new Vector2(width, 200));
            if (shown.Count == 0 || shown[^1] != ui.StatusShown)
                shown.Add(ui.StatusShown);
        }

        Assert.Equal([$"log 7 · all correct · agent connected · {Address}", "log 7 · all correct · agent connected", "log 7 · all correct", null], shown);
    }

    // Item 5: the top bar gives "saved" when the document has its own file, "not saved" in the folder of the unsaved
    // documents, and the unit alone for a host with no folder of documents.
    [Fact]
    public void The_Top_Bar_Says_Saved_Or_Not_Saved()
    {
        using var ui = new DesktopUi(Address, Document.Millimetre);
        Assert.Equal("mm", ui.UnitText);
        ui.ShowStatus(new SessionStatus(1, false, null, true));
        Assert.Equal("mm · saved", ui.UnitText);
        ui.ShowStatus(new SessionStatus(1, false, null, false));
        Assert.Equal("mm · not saved", ui.UnitText);

        // The longer unit takes its room in the top bar of each display, so the path stays left of the command line.
        foreach (var display in new[] { Desktop, Phone })
        {
            Step(ui, display);
            Step(ui, display);
            Assert.NotNull(ui.ItemRectangle(DesktopUi.TitleItem));
        }
    }

    // Items 2 and 4: a landscape display has Undo, Redo and the step of Undo on the row, and no Frame and no Home; the status
    // line follows the step. A portrait display keeps Frame and Home there, and has no room for the step.
    [Fact]
    public void A_Landscape_Row_Holds_The_Step_And_A_Portrait_Row_Holds_Frame_And_Home()
    {
        using var ui = new DesktopUi(Address, Document.Millimetre);
        ui.ShowStatus(new SessionStatus(3, false, "C3 = 75", true));
        Step(ui, Desktop);
        Step(ui, Desktop);

        var (redo, step, status) = (ui.ItemRectangle(DesktopUi.RedoLabel)!.Value, ui.ItemRectangle(DesktopUi.UndoStepItem), ui.ItemRectangle(DesktopUi.StatusLabel)!.Value);
        Assert.Null(ui.ItemRectangle(DesktopUi.FrameLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.HomeLabel));
        var text = step ?? throw new InvalidOperationException("No step of Undo on the landscape row.");
        Assert.True(text.X >= redo.Z + 16f && text.Y >= redo.Y && text.W <= redo.W, $"The step at {text} is not right of Redo at {redo}.");
        Assert.True(status.X >= text.Z + 12f, $"The status line at {status} covers the step at {text}.");

        // A long step keeps its start and a quarter of the width of the display.
        ui.ShowStatus(new SessionStatus(3, false, new string('x', 400), true));
        Step(ui, Desktop);
        var long_ = ui.ItemRectangle(DesktopUi.UndoStepItem)!.Value;
        Assert.True(long_.Z - long_.X <= Desktop.X * DesktopUi.UndoStepShare + 1f, $"The step at {long_} is wider than a quarter of the display.");

        ui.ShowStatus(new SessionStatus(3, false, null, true));
        Step(ui, Desktop);
        Assert.Null(ui.ItemRectangle(DesktopUi.UndoStepItem));

        ui.ShowStatus(new SessionStatus(3, false, "C3 = 75", true));
        Step(ui, Phone);
        Step(ui, Phone);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.FrameLabel));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.HomeLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.UndoStepItem));
    }

    // Item 4: F frames, as the command Frame does; F with Control does not, and a field that takes the keys keeps F.
    [Fact]
    public void The_Key_F_Frames_Unless_A_Field_Takes_The_Keys()
    {
        using var ui = new DesktopUi(Address, Document.Millimetre);
        Step(ui, Desktop);
        ui.TakeActions();

        Step(ui, Desktop, null, new KeySample(InputKey.F, true, KeyModifiers.None, 0), new KeySample(InputKey.F, false, KeyModifiers.None, 0));
        Assert.Equal([new FrameAction()], ui.TakeActions());
        Step(ui, Desktop, null, new KeySample(InputKey.F, true, KeyModifiers.Control, 0));
        Assert.Empty(ui.TakeActions());

        // Control and K give the command line the keys, so F is a letter of the field.
        Step(ui, Desktop, null, new KeySample(InputKey.K, true, KeyModifiers.Control, 0));
        Step(ui, Desktop);
        Step(ui, Desktop);
        Step(ui, Desktop, null, new KeySample(InputKey.F, true, KeyModifiers.None, 0), new TextSample("f", 0));
        Step(ui, Desktop);
        Assert.DoesNotContain(ui.TakeActions(), action => action is FrameAction);
    }
}
