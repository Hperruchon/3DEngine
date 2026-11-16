using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0158 (phase P10.3, TASK-0395): the dimensions of a sheet. ADR-0158 names this file in enforced-by. The two commands, each
// refusal, the place again, undo and redo, the document file, and the value from the design at each read with a lost
// dimension.
public sealed class SheetDimensionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-dimension-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];

    // The corners of a rectangle of 10 by 6 on the plane XY.
    private static readonly (double X, double Y)[] Rectangle = [(0, 0), (10, 0), (10, 6), (0, 6)];

    private sealed record Scene(DocumentSession Session, InMemoryEventSink Events, Guid Sheet, Guid Front, Guid Top, Guid Sketch, Guid[] Lines, Guid Length, Guid Circle, Guid Radius);

    // A sheet with a Front view and a Top view, and a sketch with a closed rectangle, a constraint 'distance' of 10 on its first
    // line, and a circle of radius 2 with a constraint 'radius'.
    private static async Task<Scene> NewScene(IGeometryBackend? backend = null)
    {
        var kit = EngineHosting.CreateDefault(backend ?? new InProcessMeshBackend());
        var session = new DocumentSession(kit);
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, 100, 120), "viewId");
        var top = await Applied(session, View(sheet, Top, 100, 50), "viewId");
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var points = new Guid[Rectangle.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = Rectangle[i].X, Y = Rectangle[i].Y }, "pointId");
        var lines = new Guid[points.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }, "lineId");
        var length = await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }, "constraintId");
        var centre = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 30, Y = 3 }, "pointId");
        var circle = await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 2 }, "circleId");
        var radius = await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = circle, Value = 2 }, "constraintId");
        return new Scene(session, kit.Events, sheet, front, top, sketch, lines, length, circle, radius);
    }

    private static AddSheetViewCommand View(Guid sheet, double[] d, double x, double y) => new()
    {
        SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
        ScaleNumerator = 1, ScaleDenominator = 1, X = x, Y = y,
    };

    private static PlaceSheetDimensionCommand Place(Guid sheet, Guid view, Guid source, string kind = "constraint", double x = 110, double y = 130, Guid? again = null)
        => new() { SheetId = sheet, ViewId = view, Source = source, SourceKind = kind, X = x, Y = y, DimensionId = again };

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    private static async Task Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Task<SheetRecord> Sheet(DocumentSession session)
        => session.Read((document, _) => document.Sheets.Single());

    private static async Task<IReadOnlyList<SheetDimensionInfo>> Read(DocumentSession session, Guid sheet)
    {
        var result = await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!.Dimensions;
    }

    // A dimension as text, to compare two records, with the culture of no country.
    private static string Text(SheetDimension dimension)
        => string.Create(CultureInfo.InvariantCulture, $"{dimension.Id} {dimension.ViewId} {dimension.Source} {dimension.SourceKind} ({dimension.X},{dimension.Y})");

    // A read of a dimension as text: its label and its value, or "lost".
    private static string Text(SheetDimensionInfo read)
        => read.Lost ? $"lost {read.Label ?? "-"} {read.Value?.ToString(CultureInfo.InvariantCulture) ?? "-"}" : string.Create(CultureInfo.InvariantCulture, $"{read.Label} {read.Value}");

    // Item 1: PlaceSheetDimension gives a dimension with the identifier of its command, its view, its source, its kind and its
    // place, and the event sheet.changed carries it.
    [Fact]
    public async Task A_Placed_Dimension_Has_Its_Record_And_The_Event_Sheet_Changed()
    {
        var scene = await NewScene();
        var command = Place(scene.Sheet, scene.Front, scene.Length, x: 148.5, y: 105.25);

        var id = await Applied(scene.Session, command, "dimensionId");

        Assert.Equal(command.CommandId, id);
        var dimension = Assert.Single((await Sheet(scene.Session)).Dimensions);
        Assert.Equal($"{id} {scene.Front} {scene.Length} constraint (148.5,105.25)", Text(dimension));
        var changed = (IReadOnlyDictionary<string, object?>)scene.Events.Snapshot().Last(record => record.Kind == "sheet.changed").Payload!;
        Assert.Equal(Text(dimension), Text(Assert.Single((IReadOnlyList<SheetDimension>)changed["dimensions"]!)));
    }

    // Item 4: the read gives the number of the design at each read. A change of a value changes no sheet record, and the next
    // read gives the new value.
    [Fact]
    public async Task The_Read_Gives_The_Value_Of_A_Constraint_At_Each_Read()
    {
        var scene = await NewScene();
        await Applied(scene.Session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        await Applied(scene.Session, Place(scene.Sheet, scene.Top, scene.Radius, x: 120, y: 60), "dimensionId");
        Assert.Equal(["length 10", "radius 2"], (await Read(scene.Session, scene.Sheet)).Select(Text));
        var record = (await Sheet(scene.Session)).Dimensions.Select(Text).ToArray();
        var count = scene.Events.Snapshot().Count;

        await Applied(scene.Session, new SetSketchConstraintValueCommand { SketchId = scene.Sketch, ConstraintId = scene.Length, Value = 12.5 });
        await Applied(scene.Session, new SetSketchConstraintValueCommand { SketchId = scene.Sketch, ConstraintId = scene.Radius, Value = 3 });

        Assert.Equal(["length 12.5", "radius 3"], (await Read(scene.Session, scene.Sheet)).Select(Text));
        Assert.Equal(record, (await Sheet(scene.Session)).Dimensions.Select(Text));
        Assert.DoesNotContain(scene.Events.Snapshot().Skip(count), recorded => recorded.Kind.StartsWith("sheet.", StringComparison.Ordinal));
    }

    // Items 3 and 4: a dimension whose constraint leaves the design keeps its record, and the read gives it as lost with no
    // label and no value. An undo gives the constraint back and the next read gives the value again. An undo of the sketch
    // also makes the dimension lost.
    [Fact]
    public async Task A_Dimension_Is_Lost_When_Its_Constraint_Leaves_And_Not_Lost_After_Undo()
    {
        var scene = await NewScene();
        await Applied(scene.Session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        var record = Text(Assert.Single((await Sheet(scene.Session)).Dimensions));

        await Applied(scene.Session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Length });
        Assert.Equal(["lost - -"], (await Read(scene.Session, scene.Sheet)).Select(Text));
        Assert.Equal(record, Text(Assert.Single((await Sheet(scene.Session)).Dimensions)));
        await Applied(scene.Session, new UndoCommand());
        Assert.Equal(["length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));

        // A line of the constraint goes, so the constraint goes with it.
        await Applied(scene.Session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Lines[0] });
        Assert.Equal(["lost - -"], (await Read(scene.Session, scene.Sheet)).Select(Text));
        await Applied(scene.Session, new UndoCommand());
        Assert.Equal(["length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));
    }

    // Item 4: the value of an extrude is its distance in effect; a SetExtrudeDistance changes the next read, and a body that a
    // later command consumes keeps the dimension, because the extrude stays in effect.
    [NativeManifoldFact]
    public async Task The_Read_Gives_The_Distance_In_Effect_Of_An_Extrude()
    {
        using var backend = new ManifoldGeometryBackend();
        var scene = await NewScene(backend);
        await Applied(scene.Session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Circle });
        var body = await Applied(scene.Session, new ExtrudeCommand { SketchId = scene.Sketch, Distance = 5 }, "bodyId");
        await Applied(scene.Session, Place(scene.Sheet, scene.Front, body, "distance"), "dimensionId");
        await Applied(scene.Session, Place(scene.Sheet, scene.Top, scene.Length, x: 120, y: 60), "dimensionId");
        Assert.Equal(["distance 5", "length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));

        await Applied(scene.Session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 7.5 });
        Assert.Equal(["distance 7.5", "length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));

        await Applied(scene.Session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 10 }, "bodyId");
        Assert.Equal(["distance 7.5", "length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));
        await Applied(scene.Session, new UndoCommand());
        await Applied(scene.Session, new UndoCommand());
        Assert.Equal(["distance 5", "length 10"], (await Read(scene.Session, scene.Sheet)).Select(Text));
    }

    // Item 4: a source that is not in the design gives a lost dimension, for each kind. Undo is linear, so an undo of the
    // Extrude first removes each later dimension, and no command removes an extrude; the test gives the query a Document
    // with a sheet whose sources are not in it.
    [Fact]
    public async Task A_Read_Gives_A_Dimension_Whose_Source_Is_Not_In_The_Design_As_Lost()
    {
        var view = Guid.NewGuid();
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A4", true, false, [new SheetView(view, Front, 1, 1, 80, 120)],
            [new SheetDimension(Guid.NewGuid(), view, Guid.NewGuid(), "distance", 90, 130), new SheetDimension(Guid.NewGuid(), view, Guid.NewGuid(), "constraint", 90, 60)]);
        var document = new Document();
        document.AddSheet(sheet);

        var result = await new GetSheetQueryHandler().Handle(new GetSheetQuery { SheetId = sheet.Id }, document, new InProcessMeshBackend(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var read = ((SheetInfo)result.Result!).Dimensions;
        Assert.Equal(["lost - -", "lost - -"], read.Select(Text));
        Assert.Equal(sheet.Dimensions.Select(dimension => (dimension.Id, dimension.ViewId, dimension.Source, dimension.SourceKind, dimension.X, dimension.Y)),
            read.Select(info => (info.DimensionId, info.ViewId, info.Source, info.SourceKind, info.X, info.Y)));
    }

    // Items 1, 3 and 5: each wrong command is refused with its code, and the sheet does not change. Another sheet can show the
    // same source.
    [Fact]
    public async Task Each_Wrong_Dimension_Command_Gives_Its_Code()
    {
        var scene = await NewScene();
        var session = scene.Session;
        var (sheet, front) = (scene.Sheet, scene.Front);
        var dimension = await Applied(session, Place(sheet, front, scene.Length), "dimensionId");
        var horizontal = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "horizontal", First = scene.Lines[0] }, "constraintId");
        var angle = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "angle", First = scene.Lines[0], Second = scene.Lines[1], Value = 0, SecondValue = 1 }, "constraintId");
        var before = (await Sheet(session)).Dimensions.Select(Text).ToArray();

        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, Place(Guid.NewGuid(), front, scene.Radius)));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new RemoveSheetDimensionCommand { SheetId = Guid.NewGuid(), DimensionId = dimension }));
        Assert.Equal(DiagnosticCodes.SheetDimensionNotFound, await Refused(session, Place(sheet, front, scene.Radius, again: Guid.NewGuid())));
        Assert.Equal(DiagnosticCodes.SheetDimensionNotFound, await Refused(session, new RemoveSheetDimensionCommand { SheetId = sheet, DimensionId = Guid.NewGuid() }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, Place(sheet, Guid.NewGuid(), scene.Radius)));
        foreach (var kind in new[] { "angle", "", "Constraint", "length" })
            Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, kind)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, horizontal)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, angle)));
        Assert.Equal(DiagnosticCodes.SheetSourceNotFound, await Refused(session, Place(sheet, front, Guid.NewGuid())));
        Assert.Equal(DiagnosticCodes.SheetSourceNotFound, await Refused(session, Place(sheet, front, Guid.NewGuid(), "distance")));
        Assert.Equal(DiagnosticCodes.SheetSourceNotFound, await Refused(session, Place(sheet, front, scene.Radius, "distance")));
        // A4 landscape is 297 mm across and 210 mm up.
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: 297.5, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: 100, y: 210.5)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: -0.5, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: 100, y: -0.5)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: double.NaN, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, front, scene.Radius, x: 100, y: double.PositiveInfinity)));
        Assert.Equal(DiagnosticCodes.SheetDimensionInvalid, await Refused(session, Place(sheet, scene.Top, scene.Length)));
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand()); // refused Redo commands fill the cache of the results of the session
        Assert.Equal(DiagnosticCodes.SheetIdInUse, await Refused(session, Place(sheet, front, scene.Radius) with { CommandId = dimension }));
        Assert.Equal(before, (await Sheet(session)).Dimensions.Select(Text));

        // The corners of the paper are on it, and another sheet can show the same source.
        var corner = await Applied(session, Place(sheet, front, scene.Radius, x: 297, y: 210), "dimensionId");
        await Applied(session, Place(sheet, front, scene.Length, x: 0, y: 0, again: dimension), "dimensionId");
        var other = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 2" }, "sheetId");
        var view = await Applied(session, View(other, Front, 100, 100), "viewId");
        await Applied(session, Place(other, view, scene.Length), "dimensionId");
        Assert.Equal(corner, (await session.Read((document, _) => document.Sheets.Single(record => record.Id == sheet))).Dimensions[1].Id);
    }

    // Items 1 and 3: a place and a remove are each one step of undo; the remove keeps the other dimension in its order.
    [Fact]
    public async Task Place_And_Remove_Have_Undo_And_Redo()
    {
        var scene = await NewScene();
        var session = scene.Session;
        var length = await Applied(session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        var radius = await Applied(session, Place(scene.Sheet, scene.Top, scene.Radius, x: 120, y: 60), "dimensionId");
        async Task<string> Dimensions()
            => string.Join(" ", (await Sheet(session)).Dimensions.Select(dimension => dimension.Id == length ? "length" : dimension.Id == radius ? "radius" : "other"));
        async Task<IReadOnlyList<string>> Kinds(Command command)
        {
            var count = scene.Events.Snapshot().Count;
            await Applied(session, command);
            return [.. scene.Events.Snapshot().Skip(count).Select(record => record.Kind)];
        }

        Assert.Equal("length radius", await Dimensions());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RemoveSheetDimensionCommand { SheetId = scene.Sheet, DimensionId = length }));
        Assert.Equal("radius", await Dimensions());

        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new UndoCommand()));
        Assert.Equal("length radius", await Dimensions());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new UndoCommand()));
        Assert.Equal("length", await Dimensions());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RedoCommand()));
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RedoCommand()));
        Assert.Equal("radius", await Dimensions());
    }

    // Item 2 (Q2 A): a place again gives the dimension a new view, source and place with the same identifier at the same
    // index, in one step of undo.
    [Fact]
    public async Task A_Place_Again_Keeps_The_Identifier_And_The_Order()
    {
        var scene = await NewScene();
        var session = scene.Session;
        var first = await Applied(session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        var second = await Applied(session, Place(scene.Sheet, scene.Front, scene.Radius, x: 140), "dimensionId");
        var before = (await Sheet(session)).Dimensions.Select(Text).ToArray();
        var extra = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "distance", First = scene.Lines[1], Value = 6 }, "constraintId");

        var output = await Applied(session, Place(scene.Sheet, scene.Top, extra, x: 50.5, y: 40, again: first), "dimensionId");

        Assert.Equal(first, output);
        var after = (await Sheet(session)).Dimensions;
        Assert.Equal([$"{first} {scene.Top} {extra} constraint (50.5,40)", before[1]], after.Select(Text));
        Assert.Equal(second, after[1].Id);
        Assert.Equal(["length 6", "radius 2"], (await Read(session, scene.Sheet)).Select(Text));
        await Applied(session, new UndoCommand());
        Assert.Equal(before, (await Sheet(session)).Dimensions.Select(Text));
    }

    // ADR-0155 item 3 (Q6 A): the removal of a view removes its dimensions in the same step, and keeps the other; an undo gives
    // both back.
    [Fact]
    public async Task A_Removed_View_Removes_Its_Dimensions_And_Undo_Gives_Them_Back()
    {
        var scene = await NewScene();
        var session = scene.Session;
        await Applied(session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        var kept = await Applied(session, Place(scene.Sheet, scene.Top, scene.Radius, x: 120, y: 60), "dimensionId");
        var before = (await Sheet(session)).Dimensions.Select(Text).ToArray();

        await Applied(session, new RemoveSheetViewCommand { SheetId = scene.Sheet, ViewId = scene.Front }, "viewId");

        Assert.Equal(kept, Assert.Single((await Sheet(session)).Dimensions).Id);
        await Applied(session, new UndoCommand());
        Assert.Equal(before, (await Sheet(session)).Dimensions.Select(Text));
    }

    // Items 2 and 3: a lost dimension can be removed, and a lost dimension placed again with a new source is not lost.
    [Fact]
    public async Task A_Lost_Dimension_Can_Be_Removed_And_Placed_Again()
    {
        var scene = await NewScene();
        var session = scene.Session;
        var length = await Applied(session, Place(scene.Sheet, scene.Front, scene.Length), "dimensionId");
        var radius = await Applied(session, Place(scene.Sheet, scene.Top, scene.Radius, x: 120, y: 60), "dimensionId");
        await Applied(session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Length });
        await Applied(session, new DeleteSketchItemCommand { SketchId = scene.Sketch, ItemId = scene.Radius });
        Assert.Equal(["lost - -", "lost - -"], (await Read(session, scene.Sheet)).Select(Text));
        var extra = await Applied(session, new AddSketchConstraintCommand { SketchId = scene.Sketch, Kind = "distance", First = scene.Lines[1], Value = 6 }, "constraintId");

        await Applied(session, new RemoveSheetDimensionCommand { SheetId = scene.Sheet, DimensionId = radius }, "dimensionId");
        await Applied(session, Place(scene.Sheet, scene.Front, extra, again: length), "dimensionId");

        var read = Assert.Single(await Read(session, scene.Sheet));
        Assert.Equal((length, "length 6"), (read.DimensionId, Text(read)));
    }

    // ADR-0153 item 1: an open of the document file replays the commands and gives the same dimensions in the same order.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Dimensions()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "dimensions" + DocumentFile.Extension);
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var live = new DocumentSession(kit);
        DocumentFile.Create(live, path);
        var sheet = await Applied(live, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A2" }, "sheetId");
        var view = await Applied(live, View(sheet, Front, 200, 200), "viewId");
        var sketch = await Applied(live, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var a = await Applied(live, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }, "pointId");
        var b = await Applied(live, new AddSketchPointCommand { SketchId = sketch, X = 8, Y = 0 }, "pointId");
        var c = await Applied(live, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 5 }, "pointId");
        var ab = await Applied(live, new AddSketchLineCommand { SketchId = sketch, StartPointId = a, EndPointId = b }, "lineId");
        var ac = await Applied(live, new AddSketchLineCommand { SketchId = sketch, StartPointId = a, EndPointId = c }, "lineId");
        var first = await Applied(live, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = ab, Value = 8 }, "constraintId");
        var second = await Applied(live, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = ac, Value = 5 }, "constraintId");
        var dimension = await Applied(live, Place(sheet, view, first, x: 210.125, y: 190), "dimensionId");
        var removed = await Applied(live, Place(sheet, view, second, x: 220, y: 180), "dimensionId");
        await Applied(live, new RemoveSheetDimensionCommand { SheetId = sheet, DimensionId = removed }, "dimensionId");
        await Applied(live, Place(sheet, view, second, x: 10.75, y: 400, again: dimension), "dimensionId");
        await Applied(live, Place(sheet, view, first, x: 230, y: 170), "dimensionId");
        var expected = (await Sheet(live)).Dimensions.Select(Text).ToArray();
        live.Close();

        var opened = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        await DocumentFile.Open(opened, path);
        var actual = (await Sheet(opened)).Dimensions.Select(Text).ToArray();
        var read = (await Read(opened, sheet)).Select(Text).ToArray();
        opened.Close();

        Assert.Equal(2, actual.Length);
        Assert.Equal(expected, actual);
        Assert.Equal(["length 5", "length 8"], read);
    }
}
