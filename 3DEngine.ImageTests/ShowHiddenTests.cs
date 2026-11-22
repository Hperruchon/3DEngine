using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0178 (phase R20e, TASK-0424): "Show hidden" on the ring of the part while a live body is hidden, and the count of the hidden
// bodies on the path line where the status line has no room for it. ADR-0178 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ShowHiddenTests
{
    private static readonly LiveBody Plate = new(Guid.Parse("78000000-0000-0000-0000-000000000001"), "Box", "Plate", true);
    private static readonly LiveBody Post = new(Guid.Parse("78000000-0000-0000-0000-000000000002"), "Cylinder", "Post", true);
    private static readonly LiveBody Rib = new(Guid.Parse("78000000-0000-0000-0000-000000000003"), "Box", "Rib", true);

    // A long file name, so the path line of a phone must cut it.
    private static readonly DocumentLine File = new("entry-hall-shelf-with-two-doors-and-a-long-name.3de", null);

    private static readonly IReadOnlyList<LiveBody> Shown = [Plate, Post, Rib];
    private static readonly IReadOnlyList<LiveBody> TwoHidden = [Plate, Post with { Visible = false }, Rib with { Visible = false }];

    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies, null, File);
    }

    private static PointerSample Pointer(Vector2 at, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    private static Vector2 Mid(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    private static Vector4 Box(DesktopUi ui, string item, Vector2 display)
        => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item} at {display}.");

    // A tap at a point, and a frame after it; the actions of the tap.
    private static IReadOnlyList<PanelAction> TapAt(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, Vector2 at)
    {
        ui.TakeActions();
        Step(ui, display, bodies, Pointer(at, PointerPhase.Down, 0));
        Step(ui, display, bodies, Pointer(at, PointerPhase.Up, 50));
        Step(ui, display, bodies);
        return ui.TakeActions();
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies, string item)
        => TapAt(ui, display, bodies, Mid(Box(ui, item, display)));

    // The first frame sizes the structure view at its whole room, so the ring of the part takes its place from the third frame.
    private static DesktopUi Started(Vector2 display, IReadOnlyList<LiveBody> bodies)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        for (var i = 0; i < 3; i++)
            Step(ui, display, bodies);
        return ui;
    }

    // Item 1: while at least one live body is hidden, the ring of the part has "Show hidden" after its other tools, with its
    // label on the ring; with no hidden body it does not show, also after the last hidden body shows again.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void The_Ring_Of_The_Part_Has_Show_Hidden_Only_While_A_Body_Is_Hidden(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display, Shown);
        // ADR-0184 item 1 (TASK-0447): "Section" after "Bodies".
        Assert.Equal([DesktopUi.NewLabel, DesktopUi.PartSketchLabel, DesktopUi.BodiesLabel, DesktopUi.SectionLabel], ui.RingLabels);
        Assert.Null(ui.ItemRectangle(DesktopUi.RingTextItem(DesktopUi.ShowHiddenLabel)));

        Step(ui, display, TwoHidden);
        Step(ui, display, TwoHidden);
        Assert.Equal([DesktopUi.NewLabel, DesktopUi.PartSketchLabel, DesktopUi.BodiesLabel, DesktopUi.SectionLabel, DesktopUi.ShowHiddenLabel], ui.RingLabels);
        var text = Box(ui, DesktopUi.RingTextItem(DesktopUi.ShowHiddenLabel), display);
        Assert.True(text.X >= 0f && text.Z <= display.X && text.Y >= 0f && text.W <= display.Y, $"The label at {text} is outside the display {display}.");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingIconItem(DesktopUi.ShowHiddenLabel)));

        // One hidden body is enough.
        IReadOnlyList<LiveBody> one = [Plate, Post with { Visible = false }, Rib];
        Step(ui, display, one);
        Assert.Contains(DesktopUi.ShowHiddenLabel, ui.RingLabels);

        Step(ui, display, Shown);
        Assert.DoesNotContain(DesktopUi.ShowHiddenLabel, ui.RingLabels);
    }

    // Item 1: a tap on "Show hidden" gives the action of the button "Show all" of the list of the hidden bodies (ADR-0124 item 3),
    // which the host turns into one SetBodyVisibility for each hidden body; the ring stays at its first level.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void A_Tap_On_Show_Hidden_Gives_The_Action_Of_Show_All(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display, TwoHidden);
        var ring = Tap(ui, display, TwoHidden, DesktopUi.RingTextItem(DesktopUi.ShowHiddenLabel));
        Assert.Equal([new ShowAllAction()], ring);
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);

        // The button "Show all" of the list gives the same actions.
        var part = ui.ItemRectangle(DesktopUi.HiddenItem) is not null ? DesktopUi.HiddenItem : DesktopUi.PathHiddenItem;
        Tap(ui, display, TwoHidden, part);
        Assert.True(ui.HiddenListOpen);
        Assert.Equal(ring, Tap(ui, display, TwoHidden, DesktopUi.HiddenShowAllItem));
    }

    // Item 2: on a portrait phone the status line has no room for the part, so the path line shows "N hidden" right of the file
    // name, which is cut and keeps its chevron: a tap on it still opens the file menu. The part is in the accent colour, 44 pixels
    // high, and left of "Views". A tap on the part opens the list of the hidden bodies under the path line, inside the display;
    // a tap on a row shows that body, and a second tap on the part closes the list.
    [Theory]
    [InlineData(360, 640)]
    [InlineData(390, 844)]
    [InlineData(412, 915)]
    public void On_A_Portrait_Phone_The_Path_Line_Counts_The_Hidden_Bodies(int width, int height)
    {
        var display = new Vector2(width, height);
        var file = DesktopUi.PathSegmentItem(PathLevel.Document);
        using var shown = Started(display, Shown);
        var whole = Box(shown, file, display);
        Assert.Null(shown.ItemRectangle(DesktopUi.PathHiddenItem));

        using var ui = Started(display, TwoHidden);
        Assert.Null(ui.ItemRectangle(DesktopUi.HiddenItem));
        Assert.Equal("2 hidden", DesktopUi.PathHiddenText(TwoHidden));
        var part = Box(ui, DesktopUi.PathHiddenItem, display);
        var path = Box(ui, DesktopUi.PathItem, display);
        var name = Box(ui, file, display);
        var views = Box(ui, DesktopUi.ViewsLabel, display);
        Assert.True(part.X >= name.Z, $"The part at {part} is not right of the file name at {name}.");
        Assert.True(part.Y >= path.Y - 0.5f && part.W <= path.W + 0.5f, $"The part at {part} is not on the path line at {path}.");
        Assert.True(part.W - part.Y >= DesktopUi.MinimumControlHeight, $"The part at {part} is lower than 44 pixels.");
        Assert.True(part.Z <= views.X - 12f + 0.5f, $"The part at {part} is not 12 pixels left of Views at {views}.");
        Assert.True(name.Z - name.X < whole.Z - whole.X, $"The file name at {name} is not cut; without the part it is {whole}.");

        Tap(ui, display, TwoHidden, file);
        Assert.True(ui.FileMenuOpen);
        Tap(ui, display, TwoHidden, file);
        Assert.False(ui.FileMenuOpen);

        Assert.Empty(Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem));
        Assert.True(ui.HiddenListOpen);
        var list = Box(ui, DesktopUi.HiddenListItem, display);
        Assert.True(list.Y >= part.W && list.Y <= part.W + 8f, $"The list at {list} is not just under the path line at {part}.");
        Assert.True(list.X <= part.X + 0.5f, $"The list at {list} does not start at the left edge of the part at {part}.");
        Assert.True(list.X >= 0f && list.Z <= display.X && list.W <= display.Y, $"The list at {list} is outside the display {display}.");
        Assert.True(ui.OverPanel(Mid(list).X, Mid(list).Y), "A tap on the list reaches the scene.");
        foreach (var body in TwoHidden.Where(body => !body.Visible))
        {
            var row = Box(ui, DesktopUi.HiddenRowItem(body.Id), display);
            Assert.True(row.Y >= list.Y - 0.5f && row.W <= list.W + 0.5f, $"The row at {row} is outside the list at {list}.");
        }

        Assert.NotNull(ui.ItemRectangle(DesktopUi.HiddenShowAllItem));
        Assert.Equal([new SetVisibleAction(Rib.Id, true)], Tap(ui, display, TwoHidden, DesktopUi.HiddenRowItem(Rib.Id)));
        Assert.True(ui.HiddenListOpen);
        Assert.Empty(Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem));
        Assert.False(ui.HiddenListOpen);

        // A release on the scene closes the list; with no hidden body the part does not show and the list closes.
        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Assert.True(ui.HiddenListOpen);
        TapAt(ui, display, TwoHidden, new Vector2(width / 2f, height * 0.45f));
        Assert.False(ui.HiddenListOpen);
        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Assert.True(ui.HiddenListOpen);
        Step(ui, display, Shown);
        Assert.False(ui.HiddenListOpen);
        Assert.Null(ui.ItemRectangle(DesktopUi.PathHiddenItem));
    }

    // Finding V225 of the codebase review of 2026-11-21 (TASK-0431): while a live sheet exists, a portrait display has "Sheet" right
    // of "Views" (ADR-0168 item 1), so "Views" is further left. The part, the path line and the top bar end 12 pixels left of
    // "Views" in that state too (ADR-0138). Until TASK-0431 they kept the room of "Views" only, and "2 hidden" ran under it.
    // At 360 pixels the file name at its floor of 48 pixels and the part do not fit in the room; register entry R-0072 asks the
    // owner which rule gives way there.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(412, 915)]
    public void With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        ui.ShowSheets([new SheetRecord(Guid.NewGuid(), "Sheet 1", "A4", true, false, [], [])]);
        // "not saved" makes the text of the top bar long, so it fills its room.
        ui.ShowStatus(new SessionStatus(null, false, null, false));
        for (var i = 0; i < 3; i++)
            Step(ui, display, TwoHidden);

        var views = Box(ui, DesktopUi.ViewsLabel, display);
        var sheet = Box(ui, DesktopUi.SheetLabel, display);
        Assert.True(sheet.X >= views.Z, $"Sheet at {sheet} is not right of Views at {views}.");
        foreach (var item in new[] { DesktopUi.PathHiddenItem, DesktopUi.PathItem, DesktopUi.TitleItem })
        {
            var box = Box(ui, item, display);
            Assert.True(box.Z <= views.X - 12f + 0.5f, $"The item {item} at {box} is not 12 pixels left of Views at {views}.");
        }
    }

    // Finding V226 (TASK-0431, Q5 A of the owner): the path line shows "N hidden" only while no body is selected. With a selected
    // body the part goes, its list closes, and the segments end 12 pixels left of "Views"; with no selection the part comes back.
    // Until TASK-0431 the part stayed with a selection, and with a selected edge it went past the room of the path line.
    [Theory]
    [InlineData(360, 640)]
    [InlineData(390, 844)]
    public void With_A_Selected_Body_The_Path_Line_Does_Not_Count_The_Hidden_Bodies(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display, TwoHidden);
        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Assert.True(ui.HiddenListOpen);

        for (var i = 0; i < 2; i++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, TwoHidden, Plate.Id, File);
        }

        Assert.Null(ui.ItemRectangle(DesktopUi.PathHiddenItem));
        Assert.False(ui.HiddenListOpen);
        var path = Box(ui, DesktopUi.PathItem, display);
        var views = Box(ui, DesktopUi.ViewsLabel, display);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PathSegmentItem(PathLevel.Body)));
        Assert.True(path.Z <= views.X - 12f + 0.5f, $"The path line at {path} is not 12 pixels left of Views at {views}.");

        Step(ui, display, TwoHidden);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PathHiddenItem));
    }

    // Item 2: where the status line has room for the part, it stays there, and the path line does not repeat it: on the desktop,
    // on a landscape phone, and in a portrait window that has room for the part alone. The rule is the same on each display.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(844, 390)]
    [InlineData(540, 900)]
    public void Where_The_Status_Line_Has_Room_The_Path_Line_Does_Not_Repeat_The_Part(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display, TwoHidden);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.HiddenItem));
        Assert.Null(ui.ItemRectangle(DesktopUi.PathHiddenItem));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PathSegmentItem(PathLevel.Document)));
    }

    // Item 2: under the path line the list covers the folded structure view. After a tap on the structure view, the list still
    // opens over it, so its first row takes the tap there. A tap brings a window to the front, so a list that does not take the
    // focus opens behind the view, as the file menu did (finding V211).
    [Fact]
    public void The_List_Under_The_Path_Line_Opens_Over_The_Structure_View()
    {
        var display = new Vector2(390, 844);
        using var ui = Started(display, TwoHidden);
        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Assert.False(ui.HiddenListOpen);
        Tap(ui, display, TwoHidden, DesktopUi.DocumentRowItem);
        Tap(ui, display, TwoHidden, DesktopUi.DocumentRowItem);
        Assert.False(ui.StructureOpen);

        Tap(ui, display, TwoHidden, DesktopUi.PathHiddenItem);
        Assert.True(ui.HiddenListOpen);
        var row = Box(ui, DesktopUi.HiddenRowItem(Post.Id), display);
        var view = Box(ui, DesktopUi.DocumentRowItem, display);
        var (left, top, right, bottom) = (Math.Max(row.X, view.X), Math.Max(row.Y, view.Y), Math.Min(row.Z, view.Z), Math.Min(row.W, view.W));
        Assert.True(right - left > 4f && bottom - top > 4f, $"The row at {row} does not cover the structure view at {view}.");
        Assert.Equal([new SetVisibleAction(Post.Id, true)], TapAt(ui, display, TwoHidden, new Vector2((left + right) / 2f, (top + bottom) / 2f)));
        Assert.False(ui.StructureOpen);
    }
}
