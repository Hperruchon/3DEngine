using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Export;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0177 (phase R30f, TASK-0422): the sheet page shows a proposal of the agent: an amber box of each proposed view at the place
// where GetSheet puts it after the accept, and an amber value at the place of each proposed dimension. ADR-0177 names this file in
// its enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class ProposedSheetTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly LiveBody Body = new(Guid.Parse("7e000000-0000-0000-0000-000000000001"), "Plate", "Extrude", true);

    // The commands of the proposal: a new sheet, the front, top and right views at 2:1, and two dimensions on the front view.
    private static readonly Guid Sheet = Guid.Parse("7e000000-0000-0000-0001-000000000001");
    private static readonly Guid Front = Guid.Parse("7e000000-0000-0000-0001-000000000002");
    private static readonly Guid Top = Guid.Parse("7e000000-0000-0000-0001-000000000003");
    private static readonly Guid Right = Guid.Parse("7e000000-0000-0000-0001-000000000004");
    private static readonly Guid LengthValue = Guid.Parse("7e000000-0000-0000-0001-000000000005");
    private static readonly Guid DistanceValue = Guid.Parse("7e000000-0000-0000-0001-000000000006");

    private static Dictionary<string, JsonElement> Parameters(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? throw new InvalidOperationException("No parameters.");

    private static string Frame(FlatView view)
    {
        var d = SheetPlacement.FrameOf(view);
        string[] names = ["rightX", "rightY", "rightZ", "upX", "upY", "upZ", "eyeX", "eyeY", "eyeZ"];
        return string.Join(",", names.Select((name, i) => $"\"{name}\":{d[i].ToString("R", CultureInfo.InvariantCulture)}"));
    }

    private static (string Name, Guid Id, string Parameters) View(Guid id, FlatView view, double x, double y)
        => ("AddSheetView", id, $"{{\"sheetId\":\"{Sheet}\",{Frame(view)},\"scaleNumerator\":2,\"scaleDenominator\":1,\"x\":{x},\"y\":{y}}}");

    private static (string Name, Guid Id, string Parameters) Dimension(Guid id, Guid source, string kind, double x, double y)
        => ("PlaceSheetDimension", id, $"{{\"sheetId\":\"{Sheet}\",\"viewId\":\"{Front}\",\"source\":\"{source}\",\"sourceKind\":\"{kind}\",\"x\":{x},\"y\":{y}}}");

    // The proposal of section 4.2 of the design file: a sheet, three views and two values of the plate.
    private static (string Name, Guid Id, string Parameters)[] Commands(Guid length, Guid body) =>
    [
        ("CreateSheet", Sheet, "{\"name\":\"Proposed\",\"paper\":\"A4\",\"landscape\":true}"),
        View(Front, FlatView.Front, 100, 150),
        View(Top, FlatView.Top, 100, 80),
        View(Right, FlatView.Right, 200, 150),
        Dimension(LengthValue, length, "constraint", 100, 170),
        Dimension(DistanceValue, body, "distance", 130, 150),
    ];

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    // A rectangle of 10 by 6 on the plane XY with a length of 10 on its first line, extruded by 3: a constraint and a distance.
    private static async Task<(Guid Body, Guid Length)> Plate(DocumentSession session)
    {
        var sketch = Out(await session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var length = Out(await session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }), "constraintId");
        var body = Out(await session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        return (body, length);
    }

    private static async Task<IReadOnlyList<SketchInfo>> Sketches(DocumentSession session)
    {
        var sketches = new List<SketchInfo>();
        foreach (var id in await session.Read((document, _) => document.Sketches.Select(sketch => sketch.Id).ToArray()))
            sketches.Add((await session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!);
        return sketches;
    }

    private static Task<SheetProposal> ReadOf(DocumentSession session, Guid proposal, ProposedSheet sheet, IReadOnlyList<SketchInfo> sketches)
        => SheetProposal.Read(proposal, sheet, sketches,
            async query => (await session.Query<ViewLinesInfo>(query)).Result,
            async body => (await session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = body })).Result?.Distance);

    // Item 1: each of the three commands gives its item with the identifier of its command; a parameter that does not bind, a
    // paper that is not of ISO 216, a scale of zero and each other command give no item.
    [Fact]
    public void Each_Sheet_Command_Gives_Its_Item()
    {
        var commands = Commands(Guid.NewGuid(), Guid.NewGuid());
        var items = commands.Select(command => ProposedSheet.ItemOf(command.Name, Parameters(command.Parameters), command.Id)).ToList();

        Assert.Equal(new ProposedNewSheet(Sheet, "Proposed", "A4", true, false), items[0]);
        var front = Assert.IsType<ProposedView>(items[1]);
        Assert.Equal((Front, Sheet, 2L, 1L, 100.0, 150.0), (front.Id, front.Sheet, front.ScaleNumerator, front.ScaleDenominator, front.X, front.Y));
        Assert.Equal(SheetPlacement.FrameOf(FlatView.Front), front.Direction);
        var length = Assert.IsType<ProposedDimension>(items[4]);
        Assert.Equal((Sheet, Front, "constraint", 100.0, 170.0), (length.Sheet, length.View, length.SourceKind, length.X, length.Y));

        Assert.Equal(new ProposedNewSheet(Sheet, "S", "A4", true, false), ProposedSheet.ItemOf("CreateSheet", Parameters("{\"name\":\"S\"}"), Sheet));
        Assert.Equal(new ProposedNewSheet(Sheet, "S", "A2", false, true), ProposedSheet.ItemOf("CreateSheet", Parameters("{\"name\":\"S\",\"paper\":\"A2\",\"landscape\":\"false\",\"thirdAngle\":true}"), Sheet));
        Assert.Null(ProposedSheet.ItemOf("CreateSheet", Parameters("{\"name\":\"S\",\"paper\":\"B5\"}"), Sheet));
        Assert.Null(ProposedSheet.ItemOf("CreateSheet", Parameters("{\"paper\":\"A4\"}"), Sheet));
        Assert.Null(ProposedSheet.ItemOf("AddSheetView", Parameters(commands[1].Parameters.Replace("\"scaleNumerator\":2", "\"scaleNumerator\":0", StringComparison.Ordinal)), Front));
        Assert.Null(ProposedSheet.ItemOf("AddSheetView", Parameters(commands[1].Parameters.Replace("\"rightX\":", "\"rightW\":", StringComparison.Ordinal)), Front));
        Assert.Null(ProposedSheet.ItemOf("PlaceSheetDimension", Parameters(commands[4].Parameters.Replace("\"viewId\"", "\"view\"", StringComparison.Ordinal)), LengthValue));
        Assert.Null(ProposedSheet.ItemOf("RemoveSheet", Parameters($"{{\"sheetId\":\"{Sheet}\"}}"), Guid.NewGuid()));
    }

    // Item 1: the sheet of the first sheet item, with the items of that sheet only, in their order; no sheet item gives null.
    [Fact]
    public void A_Proposal_Shows_The_Items_Of_Its_First_Sheet()
    {
        var other = Guid.NewGuid();
        ProposedSheetItem?[] items =
        [
            null,
            new ProposedView(Front, Sheet, SheetPlacement.FrameOf(FlatView.Front), 1, 1, 10, 10),
            new ProposedView(Top, other, SheetPlacement.FrameOf(FlatView.Top), 1, 1, 10, 10),
            new ProposedDimension(LengthValue, Sheet, Front, Guid.NewGuid(), "constraint", 5, 5),
            new ProposedView(Right, Sheet, SheetPlacement.FrameOf(FlatView.Right), 1, 1, 10, 10),
        ];

        var sheet = ProposedSheet.Of(items) ?? throw new InvalidOperationException("No sheet.");
        Assert.Equal(Sheet, sheet.SheetId);
        Assert.Null(sheet.New);
        Assert.Equal([Front, Right], sheet.Views.Select(view => view.Id));
        Assert.Equal([LengthValue], sheet.Dimensions.Select(dimension => dimension.Id));
        Assert.Null(ProposedSheet.Of([null, null]));
        Assert.Null(ProposedSheet.Of(null));
    }

    // Item 2: the box comes from the edges and the outlines, not from a centre line, around its centre, times the scale; a view
    // with no line is a square of 10 mm. The text of a value is that of the page: a diameter for a radius on a whole circle.
    [Fact]
    public void The_Box_And_The_Text_Follow_The_Sheet()
    {
        var view = new ProposedView(Front, Sheet, SheetPlacement.FrameOf(FlatView.Front), 2, 1, 100, 50);
        var lines = new ViewLinesInfo(view.Direction,
        [
            new ViewLineInfo(Body.Id, 0, 0, 10, 0, true, "edge"),
            new ViewLineInfo(Body.Id, 10, 0, 10, 3, false, "edge"),
            new ViewLineInfo(Body.Id, 4, -20, 4, 20, true, "centre"),
            new ViewLineInfo(Body.Id, 6, 1, 6, 1, true, "outline") { Circle = new ViewCircleInfo(6, 1, 2) },
        ], []);
        // The circle at (6, 1) with the radius 2 takes the box down to v = -1, so the centre is (5, 1).
        Assert.Equal((90.0, 46.0, 110.0, 54.0), ProposedSheet.Box(view, lines));
        Assert.Equal((95.0, 45.0, 105.0, 55.0), ProposedSheet.Box(view, null));
        Assert.Equal("Front 2:1", ProposedSheet.Name(view));
        Assert.Equal("View 1:1", ProposedSheet.Name(view with { Direction = [2, 2, -1, -1, 2, 2, 2, -1, 2], ScaleNumerator = 1 }));

        var (circle, arc, sketch) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (diameter, radius, length, angle) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketches = new[]
        {
            new SketchInfo(sketch, Guid.Empty, "S", [], [], [new SketchCircle(circle, Guid.Empty, 4)], [],
            [
                new SketchConstraintInfo(diameter, "radius", [circle], [4], 0),
                new SketchConstraintInfo(radius, "radius", [arc], [2.5], 0),
                new SketchConstraintInfo(length, "distance", [Guid.NewGuid()], [12], 0),
                new SketchConstraintInfo(angle, "angle", [Guid.NewGuid(), Guid.NewGuid()], [0, 1], 0),
            ], true, 0),
        };
        ProposedDimension Of(Guid source, string kind) => new(Guid.NewGuid(), Sheet, Front, source, kind, 0, 0);
        Assert.Equal("Ø8", ProposedSheet.Text(Of(diameter, "constraint"), sketches, null));
        Assert.Equal("R2.5", ProposedSheet.Text(Of(radius, "constraint"), sketches, null));
        Assert.Equal("12", ProposedSheet.Text(Of(length, "constraint"), sketches, null));
        Assert.Equal("?", ProposedSheet.Text(Of(angle, "constraint"), sketches, null));
        Assert.Equal("?", ProposedSheet.Text(Of(Guid.NewGuid(), "constraint"), sketches, null));
        Assert.Equal("30", ProposedSheet.Text(Of(Body.Id, "distance"), sketches, 30));
        Assert.Equal("?", ProposedSheet.Text(Of(Body.Id, "distance"), sketches, null));
    }

    // Items 1, 2 and 6, and C7 of the design file: an agent posts a sheet, three views and two values to the desktop. Before the
    // accept nothing enters the log, and the read gives the paper, a box for each view and the text of each value. After the
    // accept each box is the box of the edges and the outlines of GetSheet for that view, and each value is the value of GetSheet
    // at the same place.
    [Fact]
    public async Task A_Proposed_View_Has_The_Box_Of_GetSheet_After_The_Accept()
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-proposed-sheet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "documents"));
        try
        {
            string[] args =
            [
                "--port", "0",
                "--file", Path.Combine(folder, "documents", "plate.3de"),
                $"--{DesktopSurface.DocumentFolderSetting}={Path.Combine(folder, "documents")}",
                $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(folder, "unsaved")}",
                $"--Engine:AssetFolder={Path.Combine(folder, "assets")}",
            ];
            var (surface, code, message) = DesktopEngineHost.Start(args, null);
            Assert.True(surface is not null, $"{code}: {message}");
            using var session = surface.Session;
            using (surface)
            {
                var (body, length) = await Plate(surface.Host.Session);
                using var client = new HttpClient { BaseAddress = new Uri(surface.Address) };
                var commands = string.Join(",", Commands(length, body).Select(command =>
                    $"{{\"name\":\"{command.Name}\",\"schemaVersion\":1,\"commandId\":\"{command.Id}\",\"parameters\":{command.Parameters}}}"));
                using var post = new StringContent($"{{\"summary\":\"3 views at 2:1 on A4, 2 dimensions\",\"commands\":[{commands}]}}", System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/proposals", post);
                Assert.True(response.StatusCode == System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

                var desk = new HostedSession(surface.Host);
                var card = Assert.Single(desk.Waiting());
                var sheet = ProposedSheet.Of(card.Sheet) ?? throw new InvalidOperationException("No proposed sheet.");
                var logBefore = await surface.Host.Session.Read((document, _) => document.Log.Count);
                var read = await ReadOf(surface.Host.Session, card.Id, sheet, await Sketches(surface.Host.Session));
                Assert.Equal(logBefore, await surface.Host.Session.Read((document, _) => document.Log.Count));
                Assert.Empty(await surface.Host.Session.Read((document, _) => document.Sheets));

                Assert.Equal((Sheet, "Proposed", 297.0, 210.0), (read.NewSheet!.SheetId, read.NewSheet.Name, read.NewSheet.Width, read.NewSheet.Height));
                Assert.Equal(["Front 2:1", "Top 2:1", "Right 2:1"], read.Boxes.Select(box => box.Name));
                Assert.Equal(["10", "3"], read.Values.Select(value => value.Text));

                var outcome = await desk.Accept(card.Id);
                Assert.Equal("accepted", outcome.State);
                var after = (await surface.Host.Session.Query<SheetInfo>(new GetSheetQuery { SheetId = Sheet })).Result ?? throw new InvalidOperationException("No sheet after the accept.");
                foreach (var box in read.Boxes)
                {
                    var view = after.Views.Single(candidate => candidate.ViewId == box.View);
                    var (x0, y0, x1, y1) = SheetPage.Box(view with { Lines = [.. view.Lines.Where(line => line.Kind != "centre")] });
                    Assert.True(Math.Abs(box.X0 - x0) <= 0.001 && Math.Abs(box.Y0 - y0) <= 0.001 && Math.Abs(box.X1 - x1) <= 0.001 && Math.Abs(box.Y1 - y1) <= 0.001,
                        $"The box of {box.Name} is ({box.X0}, {box.Y0}, {box.X1}, {box.Y1}), and GetSheet gives ({x0}, {y0}, {x1}, {y1}).");
                }

                Assert.Equal(read.Values.Select(value => (value.Dimension, value.Text, value.X, value.Y)),
                    after.Dimensions.Select(dimension => (dimension.DimensionId, SvgExport.Value(dimension, "Ø"), dimension.X, dimension.Y)));
            }
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // A proposal of a new A4 sheet with two boxes and one value, as the host reads it.
    private static SheetProposal NewSheetProposal(Guid proposal) => new(proposal, Sheet, new SheetInfo(Sheet, "Proposed", "A4", true, false, 297, 210, [], []),
        [new SheetProposal.Box(Front, "Front 2:1", 90, 140, 110, 160), new SheetProposal.Box(Top, "Top 2:1", 90, 60, 110, 100)],
        [new SheetProposal.Value(LengthValue, "10", 100, 170)]);

    private static void Step(DesktopUi ui, IReadOnlyList<SheetRecord> sheets, SheetProposal? proposal, SheetInfo? read, params InputSample[] samples)
    {
        // The order of the host: the proposals, then the live sheets.
        ui.ShowSheetProposal(proposal);
        ui.ShowSheets(sheets);
        ui.ShowPage(read, false);
        ui.Route(samples);
        ui.Frame(1f / 60f, Display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    private static PointerSample Pointer(Vector2 at, PointerButtons buttons, PointerPhase phase, ulong time)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, time);

    private static void Tap(DesktopUi ui, IReadOnlyList<SheetRecord> sheets, SheetProposal? proposal, SheetInfo? read, Vector2 at)
    {
        Step(ui, sheets, proposal, read, Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
        Step(ui, sheets, proposal, read, Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
        Step(ui, sheets, proposal, read, Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
        Step(ui, sheets, proposal, read);
        Step(ui, sheets, proposal, read);
    }

    private static Vector2 Middle(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    // Items 3 and 4: with no live sheet, the button "Sheet" shows for a proposal of a new sheet, and the page shows its paper with
    // an amber box at the place of each view and the amber value at its place. The paper takes no tap, and the page has no
    // "Export". After a reject the page closes; after an accept it shows the sheet, now live.
    [Fact]
    public void The_Page_Shows_The_New_Sheet_Of_A_Proposal_And_Takes_No_Tap()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var proposal = NewSheetProposal(Guid.NewGuid());
        Step(ui, [], null, null);
        Step(ui, [], null, null);
        Assert.Null(ui.ItemRectangle(DesktopUi.SheetLabel));

        Step(ui, [], proposal, null);
        Tap(ui, [], proposal, null, Middle(ui.ItemRectangle(DesktopUi.SheetLabel) ?? throw new InvalidOperationException("No button Sheet.")));
        Assert.True(ui.PageOpen);
        Assert.True(ui.PageProposedNew);
        Assert.Equal(Sheet, ui.PageSheetId);
        var paper = ui.PagePaper ?? throw new InvalidOperationException("No paper.");
        Assert.Equal(297, paper.Width);
        Assert.Null(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.ExportSheetLabel)));

        Assert.Equal([Front, Top], ui.ProposedBoxes.Select(box => box.View));
        foreach (var (view, rectangle) in ui.ProposedBoxes)
        {
            var box = proposal.Boxes.Single(candidate => candidate.View == view);
            var (from, to) = (paper.Pixel(box.X0, box.Y1), paper.Pixel(box.X1, box.Y0));
            Assert.Equal(new Vector4(from.X, from.Y, to.X, to.Y), rectangle);
        }

        var text = Assert.Single(ui.ProposedTexts);
        Assert.Equal((LengthValue, "10", paper.Pixel(100, 170)), text);

        ui.TakeActions();
        Tap(ui, [], proposal, null, paper.Pixel(40, 40));
        Tap(ui, [], proposal, null, Middle(ui.ProposedBoxes[0].Rectangle));
        Assert.Empty(ui.TakeActions());
        Assert.Null(ui.PagePlace);
        Assert.Null(ui.PageView);

        // The accept: the sheet is live with the identifier of the proposal, and the page reads it.
        var record = new SheetRecord(Sheet, "Proposed", "A4", true, false, [], []);
        var live = new SheetInfo(Sheet, "Proposed", "A4", true, false, 297, 210, [], []);
        Step(ui, [record], null, live);
        Step(ui, [record], null, live);
        Assert.True(ui.PageOpen);
        Assert.False(ui.PageProposedNew);
        Assert.Equal(Sheet, ui.PageSheetId);
        Assert.Empty(ui.ProposedBoxes);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.ExportSheetLabel)));

        // A reject of a second proposal of a new sheet: the page goes back to the live sheet; with no live sheet it closes.
        var second = NewSheetProposal(Guid.NewGuid()) with { SheetId = Right, NewSheet = live with { SheetId = Right, Name = "Second" } };
        Step(ui, [record], second, live);
        Assert.Equal(Right, ui.PageSheetId);
        Assert.True(ui.PageProposedNew);
        Step(ui, [record], null, live);
        Step(ui, [record], null, live);
        Assert.Equal(Sheet, ui.PageSheetId);
        Step(ui, [], second, null);
        Step(ui, [], null, null);
        Step(ui, [], null, null);
        Assert.False(ui.PageOpen);
    }

    // Item 5: while the page shows the sheet of the proposal, the arc has no command line, and a tap on Accept or on Reject gives its
    // action and no tap on the page. Until TASK-0422 the window of the page took each tap on the arc.
    [Fact]
    public void Accept_And_Reject_Take_A_Tap_Over_The_Page()
    {
        foreach (var accept in new[] { true, false })
        {
            using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
            var proposal = NewSheetProposal(Guid.NewGuid());
            // Finding V222 (TASK-0426): the two commands are sheet items of the sheet of the page, so the arc has no line of them.
            var card = new ProposalCard(proposal.Proposal, "3 views at 2:1 on A4", ["CreateSheet Proposed A4 true", "AddSheetView 7e000000 1 0 0"],
                Sheet: [new ProposedNewSheet(Sheet, "Proposed", "A4", true, false), new ProposedView(Front, Sheet, SheetPlacement.FrameOf(FlatView.Front), 2, 1, 100, 150)]);
            void Frame(params InputSample[] samples)
            {
                ui.ShowSheetProposal(proposal);
                ui.ShowSheets([]);
                ui.ShowPage(null, false);
                ui.Route(samples);
                ui.Frame(1f / 60f, Display, Vector2.One, [Body], null, new DocumentLine("plate", null), proposals: [card]);
            }

            Frame();
            Frame();
            Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalLineItem(0)));
            ui.OpenPage();
            Frame();
            Frame();
            Assert.True(ui.PageProposedNew);
            Assert.Null(ui.ItemRectangle(DesktopUi.ProposalLineItem(0)));
            Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalCountItem));

            ui.TakeActions();
            var at = Middle(ui.ItemRectangle(accept ? DesktopUi.ProposalAcceptItem : DesktopUi.ProposalRejectItem) ?? throw new InvalidOperationException("No end of the arc."));
            Assert.NotNull(ui.PagePaper);
            Frame(Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
            Frame(Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
            Frame(Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
            Frame();
            Assert.Equal(accept ? [new AcceptProposalAction(proposal.Proposal)] : [new RejectProposalAction(proposal.Proposal)], ui.TakeActions());
            Assert.True(ui.PageOpen);
        }
    }

    // Finding V222 of the codebase review of 2026-11-20 (TASK-0426, Q3 A of the owner): on the page the arc keeps the line of each
    // command that the page does not show, and only the lines of the sheet items of the sheet of the page go. Until TASK-0426 the
    // arc showed no command line on the page, so a SetExtrudeDistance of the same proposal showed nowhere.
    [Fact]
    public void On_The_Page_The_Arc_Keeps_The_Line_Of_Each_Command_That_The_Page_Does_Not_Show()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var proposal = NewSheetProposal(Guid.NewGuid());
        var card = new ProposalCard(proposal.Proposal, "A sheet and a wider plate", ["CreateSheet Proposed A4 true", "AddSheetView 7e000000 1 0 0", "SetExtrudeDistance 7d000000 12"],
            Sheet: [new ProposedNewSheet(Sheet, "Proposed", "A4", true, false), new ProposedView(Front, Sheet, SheetPlacement.FrameOf(FlatView.Front), 2, 1, 100, 150), null]);
        void Frame()
        {
            ui.ShowSheetProposal(proposal);
            ui.ShowSheets([]);
            ui.ShowPage(null, false);
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [Body], null, new DocumentLine("plate", null), proposals: [card]);
        }

        Frame();
        Frame();
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalLineItem(0)));
        ui.OpenPage();
        Frame();
        Frame();
        Assert.True(ui.ProposalOnPage(card));
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalLineItem(0)));
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalLineItem(1)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalLineItem(2)));
        Assert.Equal("SetExtrudeDistance 7d000000 12", ui.ProposalTexts[DesktopUi.ProposalLineItem(2)]);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalCountItem));
    }

    // Finding V221 of the codebase review of 2026-11-20 (TASK-0426): on the page the arc of a proposal draws after the paper, so the
    // paper does not cover it, and a tap on Accept gives the accept and no tap on the paper, at each size of ADR-0113 item 1 and
    // ADR-0172, and in the largest window of a resize of ADR-0163 item 4 (2560 by 1440), where Accept lies on the paper. Until
    // TASK-0426 the arc drew under the paper, and a tap on Accept over the paper also set the place of a tap there.
    [Theory]
    [MemberData(nameof(ResponsiveLayoutTests.Sizes), MemberType = typeof(ResponsiveLayoutTests))]
    [InlineData(2560, 1440)]
    public void On_The_Page_The_Arc_Is_Over_The_Paper_And_Accept_Takes_The_Tap(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var record = new SheetRecord(Sheet, "Sheet 1", "A4", true, false, [], []);
        var live = new SheetInfo(Sheet, "Sheet 1", "A4", true, false, 297, 210, [], []);
        var proposal = NewSheetProposal(Guid.NewGuid()) with { NewSheet = null };
        var card = new ProposalCard(proposal.Proposal, "One view at 2:1", ["AddSheetView 7e000000 1 0 0"],
            Sheet: [new ProposedView(Front, Sheet, SheetPlacement.FrameOf(FlatView.Front), 2, 1, 100, 150)]);
        UiDrawData drawn = UiDrawData.Empty;
        void Frame(params InputSample[] samples)
        {
            ui.ShowSheetProposal(proposal);
            ui.ShowSheets([record]);
            ui.ShowPage(live, false);
            ui.Route(samples);
            drawn = ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null), proposals: [card]);
        }

        Frame();
        ui.OpenPage();
        Frame();
        Frame();
        Frame();
        Assert.NotNull(ui.PagePaper);
        var vertices = drawn.Vertices;
        var band = (DesktopUi.DimensionColour & 0x00FFFFFFu) | (0x48u << 24);
        var lastPaper = Array.FindLastIndex(vertices, vertex => vertex.Colour == Theme.Paper);
        var firstBand = Array.FindIndex(vertices, vertex => vertex.Colour == band);
        Assert.True(lastPaper >= 0 && firstBand >= 0, $"{width}x{height}: no paper ({lastPaper}) or no band ({firstBand}).");
        Assert.True(firstBand > lastPaper, $"{width}x{height}: the band of the arc draws at {firstBand}, under the paper at {lastPaper}.");

        ui.TakeActions();
        var at = Middle(ui.ItemRectangle(DesktopUi.ProposalAcceptItem) ?? throw new InvalidOperationException($"{width}x{height}: no Accept."));
        Frame(Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
        Frame(Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
        Frame(Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
        Frame();
        Assert.Equal([new AcceptProposalAction(proposal.Proposal)], ui.TakeActions());
        Assert.Null(ui.PagePlace);
        Assert.Null(ui.PageView);
    }

    // Item 3: a proposal for a live sheet shows its boxes over that sheet, and the page still takes a tap on the free paper.
    [Fact]
    public void A_Proposal_For_A_Live_Sheet_Shows_Over_That_Sheet()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var record = new SheetRecord(Sheet, "Sheet 1", "A4", true, false, [], []);
        var live = new SheetInfo(Sheet, "Sheet 1", "A4", true, false, 297, 210, [], []);
        var proposal = NewSheetProposal(Guid.NewGuid()) with { NewSheet = null };
        Step(ui, [record], proposal, live);
        ui.OpenPage();
        Step(ui, [record], proposal, live);
        Step(ui, [record], proposal, live);

        Assert.False(ui.PageProposedNew);
        Assert.Equal(2, ui.ProposedBoxes.Count);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.ExportSheetLabel)));
        var paper = ui.PagePaper ?? throw new InvalidOperationException("No paper.");
        Tap(ui, [record], proposal, live, paper.Pixel(40, 40));
        Assert.NotNull(ui.PagePlace);
    }

    // Item 3: a new proposal takes the page to its sheet once, also from a sheet that the person chose; the person can then choose
    // another sheet, and the page keeps it while the same proposal waits.
    [Fact]
    public void A_New_Proposal_Takes_The_Page_To_Its_Sheet_Once()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());
        SheetRecord[] sheets = [new(first, "Sheet 1", "A4", true, false, [], []), new(second, "Sheet 2", "A4", true, false, [], [])];
        var read = new SheetInfo(second, "Sheet 2", "A4", true, false, 297, 210, [], []);
        Step(ui, sheets, null, read);
        ui.OpenPage();
        Step(ui, sheets, null, read);
        Step(ui, sheets, null, read);
        Tap(ui, sheets, null, read, Middle(ui.ItemRectangle(DesktopUi.PageItem($"Sheet {second:N}")) ?? throw new InvalidOperationException("No button of Sheet 2.")));
        Assert.Equal(second, ui.PageSheetId);

        var proposal = NewSheetProposal(Guid.NewGuid());
        Step(ui, sheets, proposal, read);
        Assert.Equal(Sheet, ui.PageSheetId);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PageItem($"Sheet {Sheet:N}")));
        Tap(ui, sheets, proposal, read, Middle(ui.ItemRectangle(DesktopUi.PageItem($"Sheet {second:N}")) ?? throw new InvalidOperationException("No button of Sheet 2.")));
        Assert.Equal(second, ui.PageSheetId);
        Step(ui, sheets, proposal, read);
        Assert.Equal(second, ui.PageSheetId);
        Assert.Empty(ui.ProposedBoxes);

        // The person chooses the new sheet again, and accepts: the sheet is live after the two others, and the page stays on it.
        Tap(ui, sheets, proposal, read, Middle(ui.ItemRectangle(DesktopUi.PageItem($"Sheet {Sheet:N}")) ?? throw new InvalidOperationException("No button of the new sheet.")));
        Assert.Equal(Sheet, ui.PageSheetId);
        SheetRecord[] after = [.. sheets, new(Sheet, "Proposed", "A4", true, false, [], [])];
        Step(ui, after, null, null);
        Step(ui, after, null, null);
        Assert.Equal(Sheet, ui.PageSheetId);
    }

    // Item 2: one read at a time, for a new proposal or a new version; the last read of the same proposal stays until the new one
    // comes, and another proposal shows nothing until its read ends.
    [Fact]
    public void The_Read_Of_A_Proposal_Runs_Once_For_Each_Version()
    {
        var cache = new SheetProposalCache();
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var started = new List<TaskCompletionSource<SheetProposal>>();
        Task<SheetProposal> Read() { var next = new TaskCompletionSource<SheetProposal>(); started.Add(next); return next.Task; }

        Assert.Null(cache.Read(1, a, Read));
        Assert.Null(cache.Read(1, a, Read));
        Assert.Equal(1, cache.Computed);
        var first = NewSheetProposal(a);
        started[0].SetResult(first);
        Assert.Same(first, cache.Read(1, a, Read));
        Assert.Equal(1, cache.Computed);
        Assert.Same(first, cache.Read(2, a, Read));
        Assert.Equal(2, cache.Computed);
        started[1].SetResult(NewSheetProposal(a));
        Assert.Null(cache.Read(2, b, Read));
        Assert.Equal(3, cache.Computed);
    }
}
