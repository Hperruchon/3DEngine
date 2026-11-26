using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry; // NativeManifoldFact

namespace Engine.Tests.Queries;

// Row P21 (TASK-0471, R-0075): the query GetSheets gives the live sheets of the Document, and the query GetBodies gives the
// live bodies with their names (ADR-0189). ADR-0189 names this file in enforced-by. Each expected value comes from the
// commands of the test, not from the code under test. The tests of the bodies move a body, so they use the native backend.
public sealed class DocumentListQueryTests
{
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static async Task<T> Read<T>(DocumentSession session, Query query)
    {
        var result = await session.Query<T>(query);
        Assert.True(result.Error is null, $"{query.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static async Task<IReadOnlyList<SheetEntry>> Sheets(DocumentSession session)
        => (await Read<SheetsInfo>(session, new GetSheetsQuery())).Sheets;

    private static async Task<IReadOnlyList<BodyInfo>> Bodies(DocumentSession session)
        => (await Read<BodiesInfo>(session, new GetBodiesQuery())).Bodies;

    // A front view of the sheet at (x, 100) on the paper.
    private static async Task<Guid> AddView(DocumentSession session, Guid sheet, double x) => Output(await Applied(session, new AddSheetViewCommand
    {
        SheetId = sheet,
        RightX = 1, RightY = 0, RightZ = 0, UpX = 0, UpY = 0, UpZ = 1, EyeX = 0, EyeY = -1, EyeZ = 0,
        ScaleNumerator = 1, ScaleDenominator = 1, X = x, Y = 100,
    }), "viewId");

    // Items 1, 2 and 4: a Document with no sheet and no body gives two empty lists, and a query changes no version.
    [Fact]
    public async Task An_Empty_Document_Gives_No_Sheet_And_No_Body()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));

        Assert.Empty(await Sheets(session));
        Assert.Empty(await Bodies(session));
        Assert.Equal(0L, await session.Read((document, _) => document.Version));
    }

    // Items 2 and 3: each created sheet is listed with the fields of its record and the identifier of each view in the order of
    // its commands; the order is the order of the CreateSheet commands in the log, so a rename keeps the place of the sheet; a
    // removed sheet is not listed, and an undo of the remove gives it back at its place. A sheet made after a remove comes last.
    [Fact]
    public async Task A_Created_Sheet_Is_Listed_With_Its_Record_And_Its_Views_And_A_Removed_Sheet_Is_Not()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var first = Output(await Applied(session, new CreateSheetCommand { SheetName = "Front" }), "sheetId");
        var second = Output(await Applied(session, new CreateSheetCommand { SheetName = "Detail", Paper = "A3", Landscape = false, ThirdAngle = true }), "sheetId");
        var left = await AddView(session, first, 80);
        var right = await AddView(session, first, 200);

        Assert.Equal(
            new[] { new SheetEntry(first, "Front", "A4", true, false, [left, right]), new SheetEntry(second, "Detail", "A3", false, true, []) },
            await Sheets(session),
            Same);

        await Applied(session, new RenameSheetCommand { SheetId = first, SheetName = "Front 2" });
        Assert.Equal(new[] { first, second }, (await Sheets(session)).Select(sheet => sheet.SheetId));
        Assert.Equal("Front 2", (await Sheets(session))[0].Name);

        var version = await session.Read((document, _) => document.Version);
        await Applied(session, new RemoveSheetCommand { SheetId = second });
        Assert.Equal(new[] { first }, (await Sheets(session)).Select(sheet => sheet.SheetId));
        await Applied(session, new UndoCommand());
        Assert.Equal(new[] { first, second }, (await Sheets(session)).Select(sheet => sheet.SheetId));
        Assert.Equal(version + 2, await session.Read((document, _) => document.Version));

        // A sheet made after a remove takes the free place in the collection of the Document, but it comes last in the list.
        await Applied(session, new RemoveSheetCommand { SheetId = first });
        var third = Output(await Applied(session, new CreateSheetCommand { SheetName = "Third" }), "sheetId");
        Assert.Equal(new[] { third, second }, await session.Read((document, _) => document.Sheets.Select(sheet => sheet.Id).ToList()));
        Assert.Equal(new[] { second, third }, (await Sheets(session)).Select(sheet => sheet.SheetId));
    }

    // Items 4 and 5: each live body with its kind, its name, its visibility and the distance of an extrude, as GetBodyInfo gives
    // them; a hidden body is live. A Translate consumes its source, so the source is not listed, and the moved body has the
    // identifier of the Translate. An undo of a CreateBox removes its body. Item 3: the order is the order of the log of the
    // command that made each body, not the order of the collection of the Document: there the extrude takes the place that the
    // consumed box left, and comes second.
    [NativeManifoldFact]
    public async Task Each_Live_Body_Is_Listed_With_Its_Name_And_A_Consumed_Body_Is_Not()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var a = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 }), "bodyId");
        var b = Output(await Applied(session, new CreateBoxCommand { SizeX = 4, SizeY = 5, SizeZ = 6 }), "bodyId");
        var c = Output(await Applied(session, new CreateCylinderCommand { Radius = 2, Height = 7 }), "bodyId");
        await Applied(session, new RenameBodyCommand { BodyId = a, NewName = "Base" });
        await Applied(session, new SetBodyVisibilityCommand { BodyId = c, Visible = false });
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = b, Dx = 10, Dy = 0, Dz = 0 }), "bodyId");
        await Applied(session, new RenameBodyCommand { BodyId = moved, NewName = "Moved" });

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (4, 0), (4, 3), (0, 3)];
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < points.Length; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] });
        var extrude = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 2.5 }), "bodyId");

        var bodies = await Bodies(session);
        Assert.Equal(new[] { a, c, moved, extrude }, bodies.Select(body => body.BodyId));
        Assert.Equal(new[] { a, extrude, c, moved }, await session.Read((document, _) => document.Bodies.Select(body => body.Handle.Id).ToList()));
        Assert.Equal(new BodyInfo(a, "Box", "Base", true), Assert.Single(bodies, body => body.BodyId == a));
        Assert.Equal(new BodyInfo(c, "Cylinder", null, false), Assert.Single(bodies, body => body.BodyId == c));
        Assert.Equal(new BodyInfo(moved, "Solid", "Moved", true), Assert.Single(bodies, body => body.BodyId == moved));
        Assert.Equal(2.5, Assert.Single(bodies, body => body.BodyId == extrude).Distance);
        foreach (var body in bodies)
            Assert.Equal(await Read<BodyInfo>(session, new GetBodyInfoQuery { BodyId = body.BodyId }), body);

        var version = await session.Read((document, _) => document.Version);
        var last = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }), "bodyId");
        Assert.Equal(last, (await Bodies(session))[^1].BodyId);
        await Applied(session, new UndoCommand());
        Assert.Equal(bodies.Select(body => body.BodyId), (await Bodies(session)).Select(body => body.BodyId));
        Assert.Equal(version + 2, await session.Read((document, _) => document.Version));
    }

    // A record with a list compares the list by reference, so the test compares the items of the list.
    private static readonly IEqualityComparer<SheetEntry> Same = EqualityComparer<SheetEntry>.Create(
        (x, y) => x is not null && y is not null && x with { ViewIds = Array.Empty<Guid>() } == y with { ViewIds = Array.Empty<Guid>() } && x.ViewIds.SequenceEqual(y.ViewIds),
        entry => entry.SheetId.GetHashCode());
}
