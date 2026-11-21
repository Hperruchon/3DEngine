using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0168 (phase R30c, TASK-0408): the sheet page: the paper in the free part, the view under a tap, the drag with the
// alignment of the projection, the read and the commands off the frame thread, the controls of the page and its keys. ADR-0168
// names this file in its enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetPageTests
{
    private static readonly SheetPlacement.Box Plate = new((0, 0, 0), (60, 40, 10));
    private static readonly LiveBody Body = new(Guid.Parse("7d000000-0000-0000-0000-000000000001"), "Plate", "Extrude", true);

    // A host with the box of the plate and a sheet with the standard views at 2:1 on A3, at the places of R30b.
    private static async Task<(EngineHost Host, SheetInfo Sheet, SheetRecord Record)> Setup()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        await host.Session.Apply(new CreateBoxCommand { SizeX = 60, SizeY = 40, SizeZ = 10 });
        new SheetActions(host.Session).Create("Sheet 1", "A3", true, (2, 1), SheetPlacement.Standard, Plate);
        var (sheet, record) = await Read(host);
        return (host, sheet, record);
    }

    private static async Task<(SheetInfo Sheet, SheetRecord Record)> Read(EngineHost host)
    {
        var record = await host.Session.Read((document, _) => document.Sheets.Single());
        return ((await host.Session.Query<SheetInfo>(new GetSheetQuery { SheetId = record.Id })).Result!, record);
    }

    // Item 1: the paper fits the room less the gap with its aspect, at its centre, and a pixel goes back to its millimetres.
    [Fact]
    public void The_Paper_Fits_The_Room_With_Its_Aspect()
    {
        var paper = SheetPage.Fit(420, 297, new Vector4(100, 50, 900, 650));
        var rect = paper.Rectangle;

        Assert.Equal(420f / 297f, (rect.Z - rect.X) / (rect.W - rect.Y), 3);
        Assert.Equal(900 - 100 - (2 * SheetPage.Gap), rect.Z - rect.X, 1);
        Assert.Equal((100f + 900f) / 2f, (rect.X + rect.Z) / 2f, 1);
        Assert.Equal((50f + 650f) / 2f, (rect.Y + rect.W) / 2f, 1);
        var (x, y) = paper.Millimetres(paper.Pixel(123.5, 45));
        Assert.Equal(123.5, x, 3);
        Assert.Equal(45, y, 3);
        Assert.Equal(new Vector2(rect.X, rect.W), paper.Pixel(0, 0));
    }

    // Item 2: a tap on a view or within 3 mm of its box finds it; a tap on the free paper finds no view.
    [Fact]
    public async Task A_Tap_Finds_The_View_Under_It()
    {
        var (host, sheet, _) = await Setup();
        using var _ = host;
        var (front, top, right) = (sheet.Views[0], sheet.Views[1], sheet.Views[2]);

        Assert.Equal(front.ViewId, SheetPage.Hit(sheet, (255, 215.5)));
        Assert.Equal(front.ViewId, SheetPage.Hit(sheet, (255 + 60 + 2.5, 215.5)));
        Assert.Equal(top.ViewId, SheetPage.Hit(sheet, (255, 155.5)));
        Assert.Equal(right.ViewId, SheetPage.Hit(sheet, (145, 215.5)));
        Assert.Null(SheetPage.Hit(sheet, (60, 60)));
        Assert.Equal((195.0, 205.5, 315.0, 225.5), SheetPage.Box(front));
    }

    // Item 3 (D13 A): the front view, the first view, moves free and takes its aligned views; the top view slides on the vertical
    // line of the front view and the side view on its horizontal line; Shift or "Keep aligned" off moves a view free and alone.
    [Fact]
    public async Task A_Drag_Keeps_The_Alignment_Of_The_Projection()
    {
        var (host, sheet, _) = await Setup();
        using var _ = host;
        var (front, top, right) = (sheet.Views[0].ViewId, sheet.Views[1].ViewId, sheet.Views[2].ViewId);

        Assert.Equal((265.0, 210.5, true), SheetPage.Drag(sheet, front, 10, -5, keepAligned: true, shift: false));
        Assert.Equal((255.0, 150.5, false), SheetPage.Drag(sheet, top, 10, -5, keepAligned: true, shift: false));
        Assert.Equal((155.0, 215.5, false), SheetPage.Drag(sheet, right, 10, -5, keepAligned: true, shift: false));
        Assert.Equal((265.0, 150.5, false), SheetPage.Drag(sheet, top, 10, -5, keepAligned: true, shift: true));
        Assert.Equal((265.0, 210.5, false), SheetPage.Drag(sheet, front, 10, -5, keepAligned: false, shift: false));
    }

    // Item 2: "Go to" finds the flat view of each standard frame, and no flat view for a pictorial frame.
    [Fact]
    public void Go_To_Finds_The_Flat_View_Of_A_Frame()
    {
        foreach (var view in Enum.GetValues<FlatView>())
            Assert.Equal(view, SheetPage.FlatOf(SheetPlacement.FrameOf(view)));
        Assert.Null(SheetPage.FlatOf([2, 2, -1, -1, 2, 2, 2, -1, 2]));
    }

    // Item 1: one read at a time; the last read of the same sheet stays until the new one comes, and another sheet shows nothing
    // until its read ends.
    [Fact]
    public void The_Read_Keeps_The_Last_Sheet_Until_The_New_Read_Comes()
    {
        var cache = new SheetReadCache();
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var reads = new Queue<TaskCompletionSource<SheetInfo?>>([new(), new(), new()]);
        var started = new List<TaskCompletionSource<SheetInfo?>>();
        Task<SheetInfo?> Read() { var next = reads.Dequeue(); started.Add(next); return next.Task; }
        SheetInfo Info(Guid id) => new(id, "Sheet", "A3", true, false, 420, 297, [], []);

        Assert.Equal((null, true), cache.Read(1, a, Read));
        Assert.Equal((null, true), cache.Read(2, a, Read));
        Assert.Equal(1, cache.Computed);
        var first = Info(a);
        started[0].SetResult(first);
        Assert.Equal((first, true), cache.Read(2, a, Read));
        Assert.Equal(2, cache.Computed);
        Assert.Equal((first, true), cache.Read(2, a, Read));
        var second = Info(a);
        started[1].SetResult(second);
        Assert.Equal((second, false), cache.Read(2, a, Read));
        Assert.Equal((null, true), cache.Read(2, b, Read));
    }

    // Item 4 (finding V196): the commands run on the thread pool, one at a time in their order, and each gives its text.
    [Fact]
    public async Task The_Commands_Run_Off_The_Frame_Thread_In_Their_Order()
    {
        var queue = new SheetCommandQueue();
        var order = new List<int>();
        var pool = new List<bool>();
        for (var i = 0; i < 3; i++)
        {
            var n = i;
            queue.Send(() => { lock (order) { order.Add(n); pool.Add(Thread.CurrentThread.IsThreadPoolThread); } return $"done {n}"; });
        }

        Assert.True(queue.Busy);
        Assert.Equal(["done 0", "done 1", "done 2"], await queue.Drain());

        // A work that waits does not stop the frame: Poll starts it and returns. TASK-0435 (phase P0.173): the work waits
        // until the test opens the gate after the Poll, so Poll returns while the work waits, or not at all. Until TASK-0435
        // Poll had 5 s. 60 s only stops a test that waits for ever (ADR-0173 item 4).
        using var gate = new ManualResetEventSlim();
        queue.Send(() => { gate.Wait(); return "late"; });
        var poll = Task.Run(queue.Poll);
        var returned = await Task.WhenAny(poll, Task.Delay(TimeSpan.FromSeconds(60))) == poll;
        gate.Set();
        await poll;
        Assert.True(returned, "Poll waited for the work.");
        Assert.Equal(["late"], await queue.Drain());
        Assert.Equal([0, 1, 2], order);
        Assert.All(pool, Assert.True);
        Assert.False(queue.Busy);
    }

    // Finding V210 of the codebase review of 2026-11-20 (TASK-0426): the frame reads the count of the changes, a command of the
    // queue ends after that and raises the count, and Poll ends its work in the same frame. The read of the old count is done,
    // so the queue gives the pending state: it is busy in the frame of the Poll that ended a work, and the page keeps its sent
    // values until the read of the new count. Until TASK-0426 the page got the old sheet as not pending and cleared them.
    [Fact]
    public void The_Frame_In_Which_A_Command_Ends_Gives_A_Pending_Read()
    {
        var (cache, queue, sheet) = (new SheetReadCache(), new SheetCommandQueue(), Guid.NewGuid());
        var version = 1L;
        Task<SheetInfo?> Read() => Task.FromResult<SheetInfo?>(new SheetInfo(sheet, "Sheet", "A3", true, false, 420, 297, [], []));
        cache.Read(version, sheet, Read);
        Assert.False(cache.Read(version, sheet, Read).Pending);

        using var gate = new ManualResetEventSlim();
        queue.Send(() => { gate.Wait(); Interlocked.Increment(ref version); return "Moved the view."; });
        Assert.Empty(queue.Poll());
        // The frame of the race: the count before the end of the command, then the Poll that ends it.
        var seen = Interlocked.Read(ref version);
        gate.Set();
        IReadOnlyList<string> texts = [];
        Assert.True(SpinWait.SpinUntil(() => (texts = queue.Poll()).Count > 0, TimeSpan.FromSeconds(30)), "The command did not end.");
        Assert.Equal(["Moved the view."], texts);
        var (_, pending) = cache.Read(seen, sheet, Read);
        Assert.False(pending);
        Assert.True(queue.Busy, "The frame that ended the command gives a read of the old count as not pending.");

        // The next frame reads the new count, and its read is pending until it ends.
        Assert.Empty(queue.Poll());
        Assert.False(queue.Busy);
        Assert.True(cache.Read(Interlocked.Read(ref version), sheet, Read).Pending);
    }

    // Items 2, 3 and 5: the commands of the page: a move, the switches of the lines, the scale of one view, a remove, and a view
    // at a tap on the free paper at the scale of the first view; a view that the sheet holds is not placed again.
    [Fact]
    public async Task The_Commands_Of_The_Page_Change_The_Sheet()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var actions = new SheetActions(host.Session);
        var (front, top) = (sheet.Views[0].ViewId, sheet.Views[1].ViewId);

        Assert.Equal("Moved the view and its aligned views.", actions.Move(record.Id, front, 265, 198.5, true));
        Assert.Equal("Hidden lines off, centre lines on.", actions.Lines(record.Id, top, false, true));
        Assert.Equal("The view is at 1:1.", actions.ScaleView(record.Id, top, (1, 1)));
        Assert.Equal("Added the bottom view to Sheet 1 at 2:1.", actions.AddAt(record.Id, FlatView.Bottom, 100, 250, Plate));
        Assert.Equal("Sheet 1 has the top view already.", actions.AddAt(record.Id, FlatView.Top, 100, 100, Plate));
        var (_, after) = await Read(host);
        Assert.Equal(
            [(265.0, 198.5, 2L, true), (265.0, 138.5, 1L, false), (155.0, 198.5, 2L, true), (100.0, 250.0, 2L, true)],
            after.Views.Select(view => (view.X, view.Y, view.ScaleNumerator, view.HiddenLines)));

        Assert.Equal("Removed the view. Undo gives it back.", actions.Remove(record.Id, top));
        Assert.Equal(3, (await Read(host)).Record.Views.Count);
    }

    private static void Frame(DesktopUi ui, Vector2 display, SheetInfo? sheet, SheetRecord? record, params InputSample[] samples)
    {
        ui.ShowSheets(record is null ? [] : [record]);
        ui.ShowPage(sheet, false);
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    // Finding V206 (TASK-0416): the host gives a pending read while a command waits, then the read of its version. The page keeps
    // the values that it sent until then.
    private static void ReadComes(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record)
    {
        ui.ShowSheets([record]);
        ui.ShowPage(sheet, true);
        ui.Route([]);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        Frame(ui, display, sheet, record);
    }

    private static PointerSample Pointer(Vector2 at, PointerButtons buttons, PointerPhase phase, ulong time, KeyModifiers modifiers = KeyModifiers.None)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, buttons, modifiers, phase, time);

    private static void Tap(DesktopUi ui, Vector2 display, SheetInfo? sheet, SheetRecord? record, Vector2 at)
    {
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
    }

    private static void Drag(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record, Vector2 from, Vector2 to, KeyModifiers modifiers = KeyModifiers.None)
    {
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.None, PointerPhase.Move, 0, modifiers));
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.Left, PointerPhase.Down, 10_000_000, modifiers));
        for (var i = 1; i <= 5; i++)
            Frame(ui, display, sheet, record, Pointer(Vector2.Lerp(from, to, i / 5f), PointerButtons.Left, PointerPhase.Move, (ulong)(10_000_000 * (i + 1)), modifiers));
        Frame(ui, display, sheet, record, Pointer(to, PointerButtons.Left, PointerPhase.Up, 100_000_000, modifiers));
        Frame(ui, display, sheet, record);
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static Vector2 Middle(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    // Item 1: the button "Sheet" shows only with a live sheet; a tap opens the page in the free part, and a second tap closes it.
    [Fact]
    public async Task The_Button_Sheet_Opens_And_Closes_The_Page()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, null, null);
        Frame(ui, display, null, null);
        Assert.Null(ui.ItemRectangle(DesktopUi.SheetLabel));

        Frame(ui, display, sheet, record);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SheetLabel)));
        Assert.True(ui.PageOpen);
        var paper = ui.PagePaper ?? throw new InvalidOperationException("No paper.");
        var free = ui.FreePart();
        Assert.True(paper.Rectangle.X >= (free.X0 + 1) / 2 * display.X && paper.Rectangle.Z <= (free.X1 + 1) / 2 * display.X, $"The paper {paper.Rectangle} leaves the free part {free}.");

        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SheetLabel)));
        Assert.False(ui.PageOpen);
        Assert.Null(ui.PagePaper);
    }

    // Item 2: a tap on a view selects it and shows its actions: "Remove" removes it, a switch of its lines sets them, "Scale" opens
    // the pad of its scale, whose Apply scales that view, and "Go to" shows its flat view, frames it and closes the page.
    [Fact]
    public async Task A_Tap_On_A_View_Gives_Its_Actions()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var top = sheet.Views[1];
        ui.TakeActions();

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y));
        Assert.Equal(top.ViewId, ui.PageView);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.HiddenLinesLabel))));
        Assert.Equal([new SheetLinesAction(record.Id, top.ViewId, false, true)], ui.TakeActions());

        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.ScaleViewLabel))));
        Assert.Equal(top.ViewId, ui.SheetPadShown?.View);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SheetPadItem("1:1"))));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SheetPadItem(DesktopUi.SheetPadApply))));
        Assert.Equal([new ScaleSheetAction(1, 1, record.Id, top.ViewId)], ui.TakeActions().OfType<ScaleSheetAction>());

        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.RemoveViewLabel))));
        Assert.Equal([new SheetRemoveAction(record.Id, top.ViewId)], ui.TakeActions());
        Assert.Null(ui.PageView);

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.GoToLabel))));
        Assert.Equal([new FlatViewAction(FlatView.Top), new FrameAction()], ui.TakeActions());
        Assert.False(ui.PageOpen);
    }

    // Item 5 (D5 B): a tap on the free paper marks the place and offers the six views; a view gives the action at the millimetres
    // of the tap.
    [Fact]
    public async Task A_Tap_On_The_Free_Paper_Places_A_View()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();

        // A pointer gives whole pixels, so the tap is on the pixel nearest to the point of 80 by 80 millimetres, and the place
        // is within half a pixel of that point. Until TASK-0420 the test tapped between two pixels and took 0.5 millimetres as
        // its limit; a paper that started 0.68 pixels from a whole pixel put the place 0.6 millimetres away.
        var paper = ui.PagePaper!.Value;
        var point = paper.Pixel(80, 80);
        var at = new Vector2(MathF.Round(point.X), MathF.Round(point.Y));
        Tap(ui, display, sheet, record, at);
        var place = ui.PagePlace ?? throw new InvalidOperationException("No place.");
        var (x, y) = paper.Millimetres(at);
        Assert.Equal(x, place.X, 3);
        Assert.Equal(y, place.Y, 3);
        Assert.True(Math.Abs(place.X - 80) <= 0.5 / paper.Scale && Math.Abs(place.Y - 80) <= 0.5 / paper.Scale, $"The place {place} is more than half a pixel from 80 by 80.");
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem("Place Bottom"))));
        Assert.Equal([new SheetAddAtAction(record.Id, FlatView.Bottom, place.X, place.Y)], ui.TakeActions());
        Assert.Null(ui.PagePlace);
    }

    // Item 3 (finding V202 of the codebase review of 2026-11-18 (b), TASK-0415): a read that comes without the dragged view ends
    // the drag; the next frames do not throw, and the release gives no move, no selection and no place.
    [Fact]
    public async Task A_Read_Without_The_Dragged_View_Ends_The_Drag()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();
        var paper = ui.PagePaper!.Value;
        var top = sheet.Views[1];
        var from = paper.Pixel(top.X, top.Y);
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.Left, PointerPhase.Down, 10_000_000));
        Frame(ui, display, sheet, record, Pointer(from + new Vector2(20f, 0f), PointerButtons.Left, PointerPhase.Move, 20_000_000));
        Assert.True(ui.PageDragging);

        Assert.Equal("Removed the view. Undo gives it back.", new SheetActions(host.Session).Remove(record.Id, top.ViewId));
        var (removed, removedRecord) = await Read(host);
        Assert.DoesNotContain(removed.Views, view => view.ViewId == top.ViewId);
        Frame(ui, display, removed, removedRecord, Pointer(from + new Vector2(40f, 0f), PointerButtons.Left, PointerPhase.Move, 30_000_000));
        Assert.False(ui.PageDragging);
        Frame(ui, display, removed, removedRecord, Pointer(from + new Vector2(60f, 0f), PointerButtons.Left, PointerPhase.Move, 40_000_000));
        Frame(ui, display, removed, removedRecord, Pointer(from + new Vector2(60f, 0f), PointerButtons.Left, PointerPhase.Up, 50_000_000));
        Frame(ui, display, removed, removedRecord);

        Assert.Empty(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Null(ui.PageView);
        Assert.Null(ui.PagePlace);
        Assert.Null(SheetPage.Drag(removed, top.ViewId, 1, 1, true, false));

        // The next tap acts again: it selects the front view.
        var front = removed.Views[0];
        Tap(ui, display, removed, removedRecord, paper.Pixel(front.X, front.Y));
        Assert.Equal(front.ViewId, ui.PageView);
    }

    // Finding V215 of the codebase review of 2026-11-20 (TASK-0426): a frame with no paper ("Reading the sheet") ends the active
    // item of a drag with no release, and then a read comes without the dragged view. The release gives nothing, and the next tap
    // on a view acts. Until TASK-0426 the page kept the mark of the lost drag, and the release of the next tap gave nothing.
    [Fact]
    public async Task A_Tap_After_A_Drag_That_A_Frame_With_No_Paper_Ended_Acts()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();
        var paper = ui.PagePaper!.Value;
        var top = sheet.Views[1];
        var from = paper.Pixel(top.X, top.Y);
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.Left, PointerPhase.Down, 10_000_000));
        Frame(ui, display, sheet, record, Pointer(from + new Vector2(20f, 0f), PointerButtons.Left, PointerPhase.Move, 20_000_000));
        Assert.True(ui.PageDragging);

        // A frame with no read, then the read after a remove of the dragged view.
        Frame(ui, display, null, record, Pointer(from + new Vector2(40f, 0f), PointerButtons.Left, PointerPhase.Move, 30_000_000));
        new SheetActions(host.Session).Remove(record.Id, top.ViewId);
        var (removed, removedRecord) = await Read(host);
        Frame(ui, display, removed, removedRecord, Pointer(from + new Vector2(60f, 0f), PointerButtons.Left, PointerPhase.Move, 40_000_000));
        Frame(ui, display, removed, removedRecord, Pointer(from + new Vector2(60f, 0f), PointerButtons.Left, PointerPhase.Up, 50_000_000));
        Frame(ui, display, removed, removedRecord);
        Assert.False(ui.PageDragging);
        Assert.Empty(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Null(ui.PageView);

        var front = removed.Views[0];
        Tap(ui, display, removed, removedRecord, paper.Pixel(front.X, front.Y));
        Assert.Equal(front.ViewId, ui.PageView);
    }

    // Item 3: a drag of the front view gives one aligned move; a drag of the top view slides on the vertical line; with Shift the
    // top view moves free.
    [Fact]
    public async Task A_Drag_Gives_One_Move()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();
        var paper = ui.PagePaper!.Value;
        var (front, top) = (sheet.Views[0], sheet.Views[1]);
        var step = new Vector2(20f, -10f);

        Drag(ui, display, sheet, record, paper.Pixel(front.X, front.Y), paper.Pixel(front.X, front.Y) + step);
        var move = Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Equal((record.Id, front.ViewId, true), (move.Sheet, move.View, move.Aligned));
        Assert.Equal(front.X + (20 / paper.Scale), move.X, 3);
        Assert.Equal(front.Y + (10 / paper.Scale), move.Y, 3);
        ReadComes(ui, display, sheet, record);

        Drag(ui, display, sheet, record, paper.Pixel(top.X, top.Y), paper.Pixel(top.X, top.Y) + step);
        move = Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Equal((top.ViewId, top.X, false), (move.View, move.X, move.Aligned));
        ReadComes(ui, display, sheet, record);

        Drag(ui, display, sheet, record, paper.Pixel(top.X, top.Y), paper.Pixel(top.X, top.Y) + step, KeyModifiers.Shift);
        move = Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Equal(top.X + (20 / paper.Scale), move.X, 3);
    }

    // Item 6 (findings V195 and V199): the page holds the keys: Escape ends the place, then the selection, then the page, and no
    // key reaches the intents or gives a view; the pad of a sheet holds its keys too, and the home screen closes the page and the
    // pad, so Enter there gives no sheet.
    [Fact]
    public async Task The_Page_And_The_Pad_Hold_Their_Keys()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();
        KeySample Key(InputKey key) => new(key, true, KeyModifiers.None, 0);

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(sheet.Views[0].X, sheet.Views[0].Y));
        ui.ShowSheets([record]);
        ui.ShowPage(sheet, false);
        Assert.DoesNotContain(ui.Route([Key(InputKey.Escape), Key(InputKey.Digit1)]), sample => sample is KeySample);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        Assert.Null(ui.PageView);
        Assert.True(ui.PageOpen);
        Assert.DoesNotContain(ui.TakeActions(), action => action is FlatViewAction);
        Frame(ui, display, sheet, record, Key(InputKey.Escape));
        Assert.False(ui.PageOpen);

        ui.OpenSheetPad([FlatView.Front], Plate);
        Assert.DoesNotContain(ui.Route([Key(InputKey.Escape)]), sample => sample is KeySample);
        Assert.Null(ui.SheetPadShown);
        ui.OpenSheetPad([FlatView.Front], Plate);
        ui.OpenPage();
        ui.ShowHome(["plate"]);
        Assert.Null(ui.SheetPadShown);
        Frame(ui, display, sheet, record, Key(InputKey.Enter));
        Assert.DoesNotContain(ui.TakeActions(), action => action is CreateSheetAction);
        ui.ShowHome(null);
        Assert.False(ui.PageOpen);
    }

    // Finding V203 of the codebase review of 2026-11-18 (b) (TASK-0416): the Escape that closes the pad of a sheet or cancels a
    // numeric pad on the page ends only that pad: the selected view stays, and with no selection the page stays open.
    [Fact]
    public async Task The_Escape_Of_A_Pad_Does_Not_Reach_The_Page()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var top = sheet.Views[1];
        void Escape()
        {
            Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, true, KeyModifiers.None, 1));
            Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, false, KeyModifiers.None, 2));
        }

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.ScaleViewLabel))));
        Assert.NotNull(ui.SheetPadShown);
        Escape();
        Assert.Null(ui.SheetPadShown);
        Assert.Equal(top.ViewId, ui.PageView);

        ui.OpenDistancePad(Body.Id, 10);
        Frame(ui, display, sheet, record);
        Assert.NotNull(ui.Pad);
        Escape();
        Assert.Null(ui.Pad);
        Assert.Equal(top.ViewId, ui.PageView);

        Escape();
        Assert.Null(ui.PageView);
        ui.OpenDistancePad(Body.Id, 10);
        Frame(ui, display, sheet, record);
        Escape();
        Assert.Null(ui.Pad);
        Assert.True(ui.PageOpen);
    }

    // Finding V206 of the codebase review of 2026-11-18 (b) (TASK-0416): two changes of one view before the read keep the first.
    // A tap on "Hidden lines" and then on "Centre lines" sends both changes; two drags of the top view give two places, the
    // second from the first; after the read, the page starts from the read again.
    [Fact]
    public async Task Two_Changes_Of_One_View_Before_The_Read_Keep_The_First()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        ui.TakeActions();
        var paper = ui.PagePaper!.Value;
        var top = sheet.Views[1];
        var (hidden, centre) = (top.HiddenLines, top.CentreLines);

        Tap(ui, display, sheet, record, paper.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.HiddenLinesLabel))));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.CentreLinesLabel))));
        Assert.Equal(
            [new SheetLinesAction(record.Id, top.ViewId, !hidden, centre), new SheetLinesAction(record.Id, top.ViewId, !hidden, !centre)],
            ui.TakeActions().OfType<SheetLinesAction>());

        var step = new Vector2(0f, -20f);
        Drag(ui, display, sheet, record, paper.Pixel(top.X, top.Y), paper.Pixel(top.X, top.Y) + step);
        var first = Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());
        Drag(ui, display, sheet, record, paper.Pixel(first.X, first.Y), paper.Pixel(first.X, first.Y) + step);
        var second = Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());
        Assert.Equal(top.Y + (20 / paper.Scale), first.Y, 3);
        Assert.Equal(first.Y + (20 / paper.Scale), second.Y, 3);
        Assert.Equal((top.X, top.X), (first.X, second.X));

        ReadComes(ui, display, sheet, record);
        Tap(ui, display, sheet, record, paper.Pixel(top.X, top.Y));
        if (ui.PageView != top.ViewId)
            Tap(ui, display, sheet, record, paper.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.HiddenLinesLabel))));
        Assert.Equal([new SheetLinesAction(record.Id, top.ViewId, !hidden, centre)], ui.TakeActions().OfType<SheetLinesAction>());

        // Finding E225 (TASK-0416): a drag that comes back to the place of the view sends no move.
        var at = paper.Pixel(top.X, top.Y);
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Down, 10_000_000));
        Frame(ui, display, sheet, record, Pointer(at + new Vector2(0f, -20f), PointerButtons.Left, PointerPhase.Move, 20_000_000));
        Assert.True(ui.PageDragging);
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Move, 30_000_000));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Up, 40_000_000));
        Frame(ui, display, sheet, record);
        Assert.Empty(ui.TakeActions().OfType<SheetMoveAction>());
    }

    // Finding V207 of the codebase review of 2026-11-18 (b) (TASK-0416): the paper keeps its place while the rows of the header
    // change: with no selection, with a selected view, with a section, with the views to place, with the formats, and with the
    // mark of a pending read. At a width of 476 to 500 pixels that mark put "Export" on a new row when it showed.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(488, 844)]
    [InlineData(1280, 720)]
    public async Task The_Paper_Keeps_Its_Place_While_The_Header_Changes(int width, int height)
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var paper = ui.PagePaper!.Value;
        var front = sheet.Views[0];
        void Escape()
        {
            Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, true, KeyModifiers.None, 1));
            Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, false, KeyModifiers.None, 2));
        }

        void Same(string state)
        {
            var now = ui.PagePaper!.Value;
            Assert.True(now.Rectangle == paper.Rectangle, $"{state}: the paper is at {now.Rectangle}, and it was at {paper.Rectangle}.");
        }

        Tap(ui, display, sheet, record, paper.Pixel(front.X, front.Y));
        Assert.Equal(front.ViewId, ui.PageView);
        Same("a selected view");
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.NotNull(ui.PageSection);
        Same("a section");
        Escape();
        Escape();
        Assert.Null(SheetPage.Hit(sheet, (40, 260)));
        Tap(ui, display, sheet, record, paper.Pixel(40, 260));
        Assert.NotNull(ui.PagePlace);
        Same("the views to place");
        Escape();
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.ExportSheetLabel))));
        Assert.True(ui.ExportOpen);
        Same("the formats");
        ui.ShowSheets([record]);
        ui.ShowPage(sheet, true);
        ui.Route([]);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        Same("a pending read");
    }

    // Item 1: on a phone the page takes the place of the panel, the paper fits the width, and each control of the page is inside
    // the display and 44 pixels high.
    [Fact]
    public async Task On_A_Phone_The_Page_Takes_The_Place_Of_The_Panel()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(390, 844);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SheetLabel)));
        Frame(ui, display, sheet, record);

        Assert.True(ui.PageOpen);
        Assert.Null(ui.ItemRectangle(DesktopUi.OpenProjectLabel));
        var paper = ui.PagePaper!.Value.Rectangle;
        Assert.True(paper.X >= 16 && paper.Z <= 390 - 16 && paper.Z - paper.X > 300, $"The paper {paper} does not fit the width.");
        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(sheet.Views[0].X, sheet.Views[0].Y));
        foreach (var label in new[] { DesktopUi.PageCloseLabel, DesktopUi.GoToLabel, DesktopUi.ScaleViewLabel, DesktopUi.RemoveViewLabel })
        {
            var box = Item(ui, DesktopUi.PageItem(label));
            Assert.True(box.X >= 0 && box.Z <= 390 && box.W <= 844 && box.W - box.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"{label} {box} is out of the display or lower than 44 pixels.");
            Assert.True(box.W <= paper.Y, $"{label} {box} is on the paper {paper}.");
        }
    }
}
