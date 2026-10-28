using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// The selection and the commands of the desktop (ADR-0029 items 1, 5, 6 and 8, TASK-0086), with
// no GPU. A pick function stands in for the pick of the picture, which PickingTests holds. The
// central test is the comparison of logs of anti-objective 7: a touch script and a mouse script
// give the same commands.
public class BodyActionsTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private const ulong Ms = 1_000_000;

    // Body A is under (100, 100) and body B under (300, 100).
    private static Guid? Pick(double x, double y) => (x, y) switch
    {
        (100, 100) => A,
        (300, 100) => B,
        _ => null,
    };

    private static async Task<EngineHost> TwoBoxes()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        foreach (var id in new[] { A, B })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = id, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        return host;
    }

    private static async Task<IReadOnlyList<LiveBody>> Live(EngineHost host)
        => await host.Session.Read((document, _) => document.Bodies.Select(body => new LiveBody(body.Handle.Id, body.Kind, body.Name, body.Visible)).ToArray());

    // Runs a script through the state machine and the actions, and gives the commands after the
    // two boxes, with their body and their value.
    private static async Task<List<string>> Log(InputSample[][] script)
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var machine = new IntentMachine();
        foreach (var frame in script)
        {
            foreach (var intent in machine.Step(frame))
                actions.Apply(intent, Pick, await Live(host));
        }

        return await host.Session.Read((document, _) => document.Log.Skip(2).Select(entry => entry.Command).Select(command => command switch
        {
            SetBodyVisibilityCommand visibility => $"SetBodyVisibility {visibility.BodyId} {visibility.Visible}",
            RenameBodyCommand rename => $"RenameBody {rename.BodyId} {rename.NewName}",
            _ => command.Name,
        }).ToList());
    }

    private static PointerSample Touch(ulong finger, double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Touch, 1, finger, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

    private static PointerSample Mouse(double x, double y, PointerButtons buttons, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, buttons, KeyModifiers.None, phase, ms * Ms);

    // Anti-objective 7 and objective 9 of docs/CHARTER.md: select A, hide B at its point, hide the
    // selection, then show all; by touch and by mouse and keyboard.
    [Fact]
    public async Task A_Touch_Script_And_A_Mouse_Script_Give_The_Same_Command_Log()
    {
        InputSample[][] touch =
        [
            [Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 100)],
            [Touch(1, 300, 100, PointerPhase.Down, 1000)], [Touch(1, 300, 100, PointerPhase.Up, 1700)],
            [new KeySample(InputKey.H, true, KeyModifiers.None, 2000 * Ms)],
            [Touch(1, 500, 500, PointerPhase.Down, 3000), Touch(2, 600, 500, PointerPhase.Down, 3000)],
            [Touch(1, 500, 500, PointerPhase.Up, 3100), Touch(2, 600, 500, PointerPhase.Up, 3100)],
        ];
        InputSample[][] mouse =
        [
            [Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(100, 100, PointerButtons.Left, PointerPhase.Up, 100)],
            [Mouse(300, 100, PointerButtons.Right, PointerPhase.Down, 1000)], [Mouse(300, 100, PointerButtons.Right, PointerPhase.Up, 1100)],
            [new KeySample(InputKey.H, true, KeyModifiers.None, 2000 * Ms)],
            [new KeySample(InputKey.H, true, KeyModifiers.Shift, 3000 * Ms)],
        ];

        var fromTouch = await Log(touch);
        var fromMouse = await Log(mouse);

        Assert.Equal(
            [$"SetBodyVisibility {B} False", $"SetBodyVisibility {A} False", $"SetBodyVisibility {A} True", $"SetBodyVisibility {B} True"],
            fromTouch);
        Assert.Equal(fromTouch, fromMouse);
    }

    [Fact]
    public async Task A_Select_Picks_A_Body_And_A_Select_Of_Nothing_Clears_The_Selection()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        Assert.True(actions.Apply(new SelectIntent(300, 100), Pick, await Live(host)));
        Assert.Equal(B, actions.Selection);
        actions.Apply(new SelectIntent(5, 5), Pick, await Live(host));
        Assert.Null(actions.Selection);
        Assert.False(actions.Apply(new OrbitIntent(1, 0), Pick, await Live(host)));
    }

    // A hidden body cannot stay selected, and a selected body that leaves the session is forgotten.
    [Fact]
    public async Task A_Hidden_Or_Consumed_Body_Is_No_Longer_Selected()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Select(A);
        actions.Apply(new HideIntent(null, null), Pick, await Live(host));
        Assert.Null(actions.Selection);

        actions.Select(B);
        actions.Forget((await Live(host)).Where(body => body.Id != B).ToArray());
        Assert.Null(actions.Selection);
    }

    [Fact]
    public async Task A_Rename_Is_A_Command_Of_The_Session()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Rename(A, "Plate");

        Assert.Equal("Plate", (await Live(host)).Single(body => body.Id == A).Name);
    }

    // A hide with no selection and no body under the point sends nothing.
    [Fact]
    public async Task A_Hide_Of_Nothing_Sends_No_Command()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Apply(new HideIntent(null, null), Pick, await Live(host));
        actions.Apply(new HideIntent(5, 5), Pick, await Live(host));

        Assert.Equal(2, await host.Session.Read((document, _) => document.Version));
    }

    // ADR-0029 item 8 (decision KD6): the camera frames only when the set of visible bodies goes
    // from empty to not empty.
    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 3, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 2, false)]
    [InlineData(2, 1, false)]
    [InlineData(1, 0, false)]
    public void The_Camera_Frames_Only_When_The_First_Visible_Body_Comes(int before, int after, bool frames)
        => Assert.Equal(frames, SceneRenderer.ShouldFrame(before, after));
}
