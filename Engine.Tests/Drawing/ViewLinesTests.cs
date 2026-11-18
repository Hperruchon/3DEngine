using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0166 item 1 (phase R30a, TASK-0406): the query GetViewLines gives the lines of one exact frame with no sheet, in units of
// the design on the plane of the view, from the code of GetSheet. ADR-0166 names this file in enforced-by.
public sealed class ViewLinesTests
{
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];

    private static GetViewLinesQuery Query(double[] d) => new()
    {
        RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
    };

    // The plate of 2026-11-17: 60 x 40 x 10 from a sketch on XY, with a through-hole of radius 8 at (20, 20).
    private static async Task<DocumentSession> Plate(ManifoldGeometryBackend backend)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var points = new List<Guid>();
        foreach (var (x, y) in new[] { (0.0, 0.0), (60.0, 0.0), (60.0, 40.0), (0.0, 40.0) })
            points.Add(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }, "lineId");
        var plate = await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 10 }, "bodyId");
        var tool = await Applied(session, new CreateCylinderCommand { Radius = 8, Height = 20 }, "bodyId");
        tool = await Applied(session, new TranslateCommand { BodyId = tool, Dx = 20, Dy = 20, Dz = 5 }, "bodyId");
        await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }, "bodyId");
        return session;
    }

    // A straight line as text with its ends in order.
    private static string Text(ViewLineInfo line)
    {
        var (a, b) = ((line.FromX, line.FromY), (line.ToX, line.ToY));
        if (a.CompareTo(b) > 0)
            (a, b) = (b, a);
        return string.Create(CultureInfo.InvariantCulture, $"{(line.Visible ? "visible" : "hidden")} {a.Item1},{a.Item2} {b.Item1},{b.Item2}");
    }

    // Criterion C1 of the design file: from the front, the four lines of the plate are visible at the places of the design, and
    // the two walls of the hole are hidden, at x = 12 and x = 28. From the top, the hole is one visible whole circle of radius 8
    // at (20, 20). The read does not change the Document.
    [NativeManifoldFact]
    public async Task The_Plate_Gives_Its_Lines_In_Units_Of_The_Design()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = await Plate(backend);
        var before = await session.Read((document, _) => (document.Version, document.Log.Count));

        var front = await session.Query<ViewLinesInfo>(Query(Front));

        Assert.Null(front.Error);
        Assert.Equal(Front, front.Result!.Direction);
        Assert.Empty(front.Result.Skipped);
        Assert.Equal(
            new[] { "hidden 12,0 12,10", "hidden 28,0 28,10", "visible 0,0 0,10", "visible 0,0 60,0", "visible 0,10 60,10", "visible 60,0 60,10" },
            front.Result.Lines.Select(Text).Order(StringComparer.Ordinal));

        var top = await session.Query<ViewLinesInfo>(Query(Top));

        Assert.Null(top.Error);
        var circle = Assert.Single(top.Result!.Lines, line => line.Circle is not null);
        Assert.True(circle.Visible);
        Assert.Equal(new ViewCircleInfo(20, 20, 8), circle.Circle);
        Assert.Equal((circle.FromX, circle.FromY), (circle.ToX, circle.ToY));
        Assert.Equal(4, top.Result.Lines.Count(line => line.Circle is null && line.Visible));
        Assert.Equal(before, await session.Read((document, _) => (document.Version, document.Log.Count)));
    }

    // Item 1: the lines are those of GetSheet for a view of the same frame at 1 : 1, moved by one shift: the sheet puts the
    // centre of the box of the lines at the place of the view.
    [NativeManifoldFact]
    public async Task The_Lines_Are_The_Lines_Of_The_Sheet_Moved_By_One_Shift()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = await Plate(backend);
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Plate" }, "sheetId");
        await Applied(session, new AddSheetViewCommand
        {
            SheetId = sheet, RightX = 1, RightY = 0, RightZ = 0, UpX = 0, UpY = 0, UpZ = 1, EyeX = 0, EyeY = -1, EyeZ = 0,
            ScaleNumerator = 1, ScaleDenominator = 1, X = 150, Y = 100,
        }, "viewId");

        var view = Assert.Single((await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet })).Result!.Views);
        var lines = (await session.Query<ViewLinesInfo>(Query(Front))).Result!.Lines;

        Assert.Equal(view.Lines.Count, lines.Count);
        var (dx, dy) = (150.0 - 30.0, 100.0 - 5.0);
        foreach (var (paper, line) in view.Lines.Zip(lines))
        {
            Assert.Equal((paper.Body, paper.Visible, paper.Kind), (line.Body, line.Visible, line.Kind));
            Assert.Equal((paper.FromX, paper.FromY, paper.ToX, paper.ToY), (line.FromX + dx, line.FromY + dy, line.ToX + dx, line.ToY + dy));
        }
    }

    // Item 1: a frame of length 2 gives the same lines in units of the design as the frame of length 1, and a circle keeps the
    // radius of the design (ADR-0164 item 1).
    [NativeManifoldFact]
    public async Task A_Frame_Of_Length_Two_Gives_The_Units_Of_The_Design()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = await Plate(backend);

        var one = (await session.Query<ViewLinesInfo>(Query(Top))).Result!;
        var two = await session.Query<ViewLinesInfo>(Query([2, 0, 0, 0, 2, 0, 0, 0, 4]));

        Assert.Null(two.Error);
        Assert.Equal(one.Lines, two.Result!.Lines);
        Assert.Equal(8, Assert.Single(two.Result.Lines, line => line.Circle is not null).Circle!.Radius);
    }

    // Item 1: a direction that is not a valid exact frame gives E-SHEET-FRAME-INVALID: a right and an up of two lengths, an up
    // that is not at a right angle, an eye on the wrong side, and a number that is not finite.
    [Theory]
    [InlineData(new[] { 1.0, 0, 0, 0, 0, 2, 0, -1, 0 })]
    [InlineData(new[] { 1.0, 0, 0, 1, 0, 1, 0, -1, 0 })]
    [InlineData(new[] { 1.0, 0, 0, 0, 0, 1, 0, 1, 0 })]
    [InlineData(new[] { double.NaN, 0, 0, 0, 0, 1, 0, -1, 0 })]
    public async Task A_Direction_That_Is_Not_A_Frame_Is_Refused(double[] direction)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));

        var read = await session.Query<ViewLinesInfo>(Query(direction));

        Assert.Null(read.Result);
        Assert.Equal(DiagnosticCodes.SheetFrameInvalid, read.Error?.Code);
    }

    // Item 1: the query takes its nine numbers from a query input, as a client of the HTTP surface gives them.
    [Fact]
    public void The_Handler_Reads_The_Nine_Numbers_Of_An_Input()
    {
        var handler = new GetViewLinesQueryHandler();
        var names = new[] { "rightX", "rightY", "rightZ", "upX", "upY", "upZ", "eyeX", "eyeY", "eyeZ" };
        Assert.Equal(names.Order(StringComparer.Ordinal), handler.Parameters.Keys.Order(StringComparer.Ordinal));
        var query = (GetViewLinesQuery)handler.Create(new QueryInput(names.Select((name, i) => (name, (object?)Front[i])).ToDictionary(pair => pair.name, pair => pair.Item2), Guid.NewGuid()));
        Assert.Equal(Front, query.Direction());
        Assert.Contains(HandlerCatalog.QueryHandlers(), registered => registered.QueryName == "GetViewLines");
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }
}
