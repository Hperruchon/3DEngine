using System.Numerics;
using SDL3;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// Register entry R-0043 (TASK-0188): the order of the samples of a click whose cursor moved with no
// motion message before the press. A probe of 2026-11-05 posted messages to a hidden window of SDL 3.2.10
// on Windows and moved no cursor: a press posted at (300, 200) came as a press at (0, 299), the last place
// of a motion. SDL_mouse.c fills button.x and button.y from mouse->x and mouse->y. So the press of such a
// click reaches the place of the last motion, and the next motion with the button down reaches the place
// of the click. These tests give that order to SdlInput and to the panel. With the place of the event, the
// panel moves or the camera turns, the two faults of the visual check of K2b. With the place of the
// cursor, the click reaches its place.
[Collection(DearImGui.Collection)]
public class FocusClickTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly LiveBody[] Bodies =
    [
        new(Guid.Parse("11111111-0000-0000-0000-000000000000"), "Box", null, true),
    ];

    private static DesktopUi Opened()
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        ui.Route([]);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        ui.Route([]);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        return ui;
    }

    private static SDL_Event Motion(float x, float y, bool held)
    {
        SDL_Event e = default;
        e.type = SDL_EventType.MouseMotion;
        e.motion.which = (SDL_MouseID)1;
        e.motion.state = held ? SDL_MouseButtonFlags.Left : 0;
        e.motion.x = x;
        e.motion.y = y;
        return e;
    }

    private static SDL_Event Button(bool down, float x, float y)
    {
        SDL_Event e = default;
        e.type = down ? SDL_EventType.MouseButtonDown : SDL_EventType.MouseButtonUp;
        e.button.which = (SDL_MouseID)1;
        e.button.button = 1;
        e.button.down = down;
        e.button.x = x;
        e.button.y = y;
        return e;
    }

    // The events of SDL for a click at the place "click" after a last motion at the place "last": a motion at
    // the last place, the press at the last place, the motion with the button down at the click, a frame with the
    // button down, as a hand gives, and the release at the click. Each frame of events goes through SdlInput, the
    // panel and the intent machine.
    private static (Vector4 PanelBefore, Vector4 PanelAfter, List<Intent> Intents, List<PointerSample> Presses) Click(
        (float X, float Y) last, (float X, float Y) click, Func<(float X, float Y)?>? cursor)
    {
        using var ui = Opened();
        var input = new SdlInput(cursor);
        var machine = new IntentMachine();
        var intents = new List<Intent>();
        var presses = new List<PointerSample>();
        var before = ui.PanelRectangles[0];

        SDL_Event[][] frames =
        [
            [Motion(last.X, last.Y, held: false)],
            [Button(true, last.X, last.Y), Motion(click.X, click.Y, held: true)],
            [],
            [Button(false, click.X, click.Y)],
            [],
            [],
        ];
        foreach (var frame in frames)
        {
            foreach (var e in frame)
                input.Translate(e, Display.X, Display.Y);
            var samples = input.TakeFrame();
            presses.AddRange(samples.OfType<PointerSample>().Where(sample => sample.Phase == PointerPhase.Down));
            intents.AddRange(machine.Step(ui.Route(samples)));
            ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        }

        return (before, ui.PanelRectangles[0], intents, presses);
    }

    private static (float X, float Y) TitleOf(Vector4 panel) => ((panel.X + panel.Z) / 2, panel.Y + 6);

    [Fact]
    public void A_Press_At_The_Last_Place_Over_The_Title_Moves_The_Panel_And_The_Place_Of_The_Cursor_Does_Not()
    {
        Vector4 panel;
        using (var ui = Opened())
            panel = ui.PanelRectangles[0];
        var title = TitleOf(panel);
        (float X, float Y) viewport = (900, 500);
        Assert.False(panel.X <= viewport.X && viewport.X < panel.Z && panel.Y <= viewport.Y && viewport.Y < panel.W);

        var stale = Click(title, viewport, cursor: null);
        Assert.NotEqual(stale.PanelBefore, stale.PanelAfter);

        var fixedClick = Click(title, viewport, cursor: () => viewport);
        Assert.Equal(fixedClick.PanelBefore, fixedClick.PanelAfter);
        var press = Assert.Single(fixedClick.Presses);
        Assert.Equal((viewport.X, viewport.Y), ((float)press.X, (float)press.Y));
        Assert.DoesNotContain(fixedClick.Intents, intent => intent is OrbitIntent);
    }

    [Fact]
    public void A_Press_At_The_Last_Place_In_The_Viewport_Turns_The_Camera_And_The_Place_Of_The_Cursor_Does_Not()
    {
        Vector4 panel;
        using (var ui = Opened())
            panel = ui.PanelRectangles[0];
        (float X, float Y) inside = ((panel.X + panel.Z) / 2, (panel.Y + panel.W) / 2);
        (float X, float Y) viewport = (900, 500);

        var stale = Click(viewport, inside, cursor: null);
        Assert.Contains(stale.Intents, intent => intent is OrbitIntent);

        var fixedClick = Click(viewport, inside, cursor: () => inside);
        Assert.DoesNotContain(fixedClick.Intents, intent => intent is OrbitIntent);
        Assert.Equal(fixedClick.PanelBefore, fixedClick.PanelAfter);
    }
}
