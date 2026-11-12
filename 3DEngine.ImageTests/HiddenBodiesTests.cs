using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0124 (phase R20b2, TASK-0351): the selection after a hide, and the hidden bodies in the status line. ADR-0124 names this
// file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class HiddenBodiesTests
{
    private static readonly Vector2 Desktop = new(1280, 720);
    private static readonly Guid A = Guid.Parse("66000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("66000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("66000000-0000-0000-0000-00000000000c");

    private static async Task<IReadOnlyList<LiveBody>> Live(EngineHost host)
        => await host.Session.Read((document, _) => document.Bodies.Select(body => new LiveBody(body.Handle.Id, body.Kind, body.Name, body.Visible)).ToArray());

    private static PickHit? NoPick(double x, double y) => null;

    // Item 1: when the selected body hides, the selection goes to the next visible body in the order of the list, else to the
    // nearest visible body before it, else to none; a hide of a body that is not selected keeps the selection.
    [Fact]
    public async Task The_Selection_Goes_To_A_Neighbour_After_A_Hide()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        foreach (var id in new[] { A, B, C })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = id, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        var actions = new BodyActions(host.Session);

        actions.Select(B);
        actions.Apply(new HideIntent(null, null), NoPick, await Live(host));
        Assert.Equal(C, actions.Selection);

        // C is the last, and B is hidden, so the selection goes back to A.
        actions.SetVisible(C, false, await Live(host));
        Assert.Equal(A, actions.Selection);

        actions.SetVisible(B, true, await Live(host));
        actions.SetVisible(C, true, await Live(host));
        actions.SetVisible(B, false, await Live(host));
        Assert.Equal(A, actions.Selection);

        actions.SetVisible(C, false, await Live(host));
        actions.SetVisible(A, false, await Live(host));
        Assert.Null(actions.Selection);
    }

    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies, null, null);
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, float x, float y)
    {
        ui.TakeActions();
        PointerSample Mouse(PointerPhase phase, ulong ms)
            => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);
        Step(ui, display, bodies, Mouse(PointerPhase.Move, 0));
        Step(ui, display, bodies, Mouse(PointerPhase.Down, 10));
        Step(ui, display, bodies, Mouse(PointerPhase.Up, 20));
        Step(ui, display, bodies);
        return ui.TakeActions();
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, string item)
    {
        var at = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        return Tap(ui, display, bodies, (at.X + at.Z) / 2f, (at.Y + at.W) / 2f);
    }

    // Item 2: the status line ends with "N hidden · Show" while a live body is hidden. The part is right of the count, as high
    // as the anchors, and on the right edge of the status line; with no hidden body it does not show.
    [Fact]
    public void The_Status_Line_Counts_The_Hidden_Bodies()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        LiveBody[] bodies = [new(A, "Box", "Side", true), new(B, "Box", "Back", false), new(C, "Box", null, false)];
        Step(ui, Desktop, bodies);
        Step(ui, Desktop, bodies);

        Assert.Equal("2 hidden · Show", DesktopUi.HiddenText(bodies));
        var part = ui.ItemRectangle(DesktopUi.HiddenItem) ?? throw new InvalidOperationException("No part of the hidden bodies.");
        var status = ui.ItemRectangle(DesktopUi.StatusLabel) ?? throw new InvalidOperationException("No status line.");
        Assert.Equal(DesktopUi.AnchorSize, part.W - part.Y);
        Assert.True(Math.Abs(part.Z - (Desktop.X - 16f)) <= 1f, $"The part at {part} does not end 16 pixels inside the right edge.");
        Assert.True(part.X > status.Z, $"The part at {part} is not right of the count at {status}.");
        Assert.True(status.Y > part.Y && status.W < part.W, $"The count at {status} is not in the height of the part at {part}.");

        LiveBody[] shown = [new(A, "Box", "Side", true)];
        Step(ui, Desktop, shown);
        Assert.Null(ui.ItemRectangle(DesktopUi.HiddenItem));
        Assert.Null(DesktopUi.HiddenText(shown));
    }

    // Item 3: a tap on the part opens the list of the hidden bodies; a tap on a row shows that body, "Show all" shows each one,
    // a second tap on the part closes the list, and a release outside it closes it too.
    [Fact]
    public void A_Tap_On_The_Part_Lists_The_Hidden_Bodies_And_Shows_One_Or_Each()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        LiveBody[] bodies = [new(A, "Box", "Side", true), new(B, "Box", "Back", false), new(C, "Box", null, false)];
        Step(ui, Desktop, bodies);
        Step(ui, Desktop, bodies);
        Assert.False(ui.HiddenListOpen);

        Assert.Empty(Tap(ui, Desktop, bodies, DesktopUi.HiddenItem));
        Assert.True(ui.HiddenListOpen);
        Assert.Null(ui.ItemRectangle(DesktopUi.HiddenRowItem(A)));
        var list = ui.ItemRectangle(DesktopUi.HiddenListItem) ?? throw new InvalidOperationException("No list.");
        var part = ui.ItemRectangle(DesktopUi.HiddenItem)!.Value;
        Assert.True(list.W <= part.Y, $"The list at {list} is not above the part at {part}.");
        Assert.True(Math.Abs(list.Z - part.Z) <= 1f, $"The list at {list} does not end on the right edge of the part at {part}.");
        Assert.True(ui.OverPanel((list.X + list.Z) / 2f, (list.Y + list.W) / 2f), "A tap on the list reaches the scene.");
        foreach (var row in new[] { DesktopUi.HiddenRowItem(B), DesktopUi.HiddenRowItem(C), DesktopUi.HiddenShowAllItem })
        {
            var at = ui.ItemRectangle(row) ?? throw new InvalidOperationException($"No row {row}.");
            Assert.True(at.W - at.Y >= DesktopUi.MinimumControlHeight, $"The row {row} at {at} is lower than 44 pixels.");
        }

        Assert.Equal(new PanelAction[] { new SetVisibleAction(C, true) }, Tap(ui, Desktop, bodies, DesktopUi.HiddenRowItem(C)));
        Assert.True(ui.HiddenListOpen);
        Assert.Equal(new PanelAction[] { new ShowAllAction() }, Tap(ui, Desktop, bodies, DesktopUi.HiddenShowAllItem));

        Assert.Empty(Tap(ui, Desktop, bodies, DesktopUi.HiddenItem));
        Assert.False(ui.HiddenListOpen);

        Tap(ui, Desktop, bodies, DesktopUi.HiddenItem);
        Assert.True(ui.HiddenListOpen);
        Tap(ui, Desktop, bodies, Desktop.X / 2f, Desktop.Y / 2f);
        Assert.False(ui.HiddenListOpen);

        // One hidden body has no "Show all"; the list closes when no body is hidden.
        LiveBody[] one = [new(A, "Box", "Side", true), new(B, "Box", "Back", false)];
        Tap(ui, Desktop, one, DesktopUi.HiddenItem);
        Assert.True(ui.HiddenListOpen);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.HiddenRowItem(B)));
        Assert.Null(ui.ItemRectangle(DesktopUi.HiddenShowAllItem));
        Step(ui, Desktop, [new(A, "Box", "Side", true), new(B, "Box", "Back", true)]);
        Assert.False(ui.HiddenListOpen);
    }

    // Item 2: the status line gives the text that fits right of Home: the whole line with the part, else the part alone, else
    // none. A small portrait window keeps the part alone; a portrait phone of 390 pixels has 38 pixels of room, so neither
    // shows there (R-0064). A tap on the part opens the list inside the display.
    [Theory]
    [InlineData(844, 390, "count and part")]
    [InlineData(540, 900, "part")]
    [InlineData(390, 844, "none")]
    public void The_Part_Of_The_Hidden_Bodies_Fits_Right_Of_Home(int width, int height, string shows)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        LiveBody[] bodies = [new(A, "Extrude", "Left side panel", true), new(B, "Extrude", "Back panel", false), new(C, "Box", "Shelf", true)];
        Step(ui, display, bodies);
        Step(ui, display, bodies);

        var home = ui.ItemRectangle(DesktopUi.HomeLabel) ?? throw new InvalidOperationException("No Home.");
        var (count, part) = (ui.ItemRectangle(DesktopUi.StatusLabel), ui.ItemRectangle(DesktopUi.HiddenItem));
        Assert.Equal(shows, (count, part) switch
        {
            (not null, not null) => "count and part",
            (null, not null) => "part",
            (null, null) => "none",
            _ => "count",
        });
        if (part is not { } shown)
            return;
        Assert.True(shown.X >= home.Z, $"The part at {shown} covers Home at {home} on {display}.");
        Assert.True(shown.Z <= display.X - 16f + 1f, $"The part at {shown} is outside the display {display}.");

        Tap(ui, display, bodies, DesktopUi.HiddenItem);
        var row = ui.ItemRectangle(DesktopUi.HiddenRowItem(B)) ?? throw new InvalidOperationException($"No row on {display}.");
        Assert.True(row.X >= 0f && row.Z <= display.X, $"The row at {row} is outside the display {display}.");
    }
}
