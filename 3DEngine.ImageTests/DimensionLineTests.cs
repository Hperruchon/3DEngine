using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0115 (phase R18c, TASK-0340): the values as in the render: three decimals and the unit, a dimension line parallel to the
// edge outside the outline of the body, extension lines, and a pill on the line. ADR-0115 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class DimensionLineTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Camera View = new() { YawDegrees = 30, PitchDegrees = 25, Distance = 40, Target = (5, 3, 1.5) };

    // Item 1 (Q6 A): three decimals, more when the value has them, at most 15 significant digits, and no "-0.000".
    [Theory]
    [InlineData(60.0, "60.000")]
    [InlineData(10.0, "10.000")]
    [InlineData(12.5, "12.500")]
    [InlineData(60.125, "60.125")]
    [InlineData(60.1234, "60.1234")]
    [InlineData(-2.0, "-2.000")]
    [InlineData(0.0, "0.000")]
    [InlineData(1234567.5, "1234567.500")]
    public void A_Value_Has_Three_Decimals_And_More_When_It_Has_Them(double value, string expected)
        => Assert.Equal(expected, EdgeValues.Exact(value));

    [Fact]
    public void A_Value_Has_At_Most_Fifteen_Significant_Digits_And_No_Negative_Zero()
    {
        Assert.Equal("0.300", EdgeValues.Exact(0.1 + 0.2));
        Assert.Equal("0.000", EdgeValues.Exact(-0.0));
    }

    // Item 2: the dimension line of an edge is parallel to it, on the side away from the centre, and past each point of the
    // outline by the gap. A radius has one point, and its leader goes straight out from the centre.
    [Fact]
    public void The_Dimension_Line_Is_Parallel_To_Its_Edge_And_Past_The_Outline()
    {
        Vector2[] square = [new(0, 0), new(100, 0), new(100, 100), new(0, 100)];

        // The edge at the bottom of the square is the lowest part, so its line is the gap under it.
        var bottom = EdgeValues.Outside(new(0, 100), new(100, 100), new(50, 50), square);
        Assert.Equal((new Vector2(0, 100), new Vector2(100, 100), new Vector2(0, 1), EdgeValues.Gap), (bottom.A, bottom.B, bottom.Normal, bottom.Offset));
        Assert.False(bottom.Leader);

        // The edge at the top, from the left to the right: its first normal points into the square, so the rule turns it
        // away from the centre, and the line is the gap above the square.
        var top = EdgeValues.Outside(new(0, 0), new(100, 0), new(50, 50), square);
        Assert.Equal((new Vector2(0, -1), EdgeValues.Gap), (top.Normal, top.Offset));

        // An edge through the middle, with the centre above it, has its line under the whole square.
        var middle = EdgeValues.Outside(new(0, 50), new(100, 50), new(50, 40), square);
        Assert.Equal(new Vector2(0, 1), middle.Normal);
        Assert.Equal(50f + EdgeValues.Gap, middle.Offset, 0.001f);

        // A slanted edge: the normal is a unit vector at a right angle to it.
        var slant = EdgeValues.Outside(new(0, 0), new(100, 100), new(80, 20), square);
        Assert.Equal(1f, slant.Normal.Length(), 0.001f);
        Assert.Equal(0f, Vector2.Dot(slant.Normal, new Vector2(100, 100)), 0.001f);
        Assert.True(Vector2.Dot(slant.Normal, new Vector2(80, 20) - new Vector2(50, 50)) < 0f, "The line of the slanted edge is on the side of the centre.");

        // A radius: one point, and a leader out from the centre.
        var radius = EdgeValues.Outside(new(100, 100), new(100, 100), new(50, 50), square);
        Assert.True(radius.Leader);
        Assert.Equal(Vector2.Normalize(new Vector2(1, 1)), radius.Normal);
        Assert.Equal(EdgeValues.Gap, radius.Offset, 0.001f);
    }

    // Item 2: each value of a real extrude has its line, and no point of the outline of the body on the screen is past the
    // line less the gap. The distance and each length of the sketch measure an edge, so the line is parallel to that edge.
    [Fact]
    public async Task Each_Value_Of_An_Extrude_Is_Past_The_Outline_Of_The_Body()
    {
        var (host, body) = await Plate();
        using var _ = host;
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var values = await ValuesOf(host, body, faces);
        var outline = Outline(faces);

        Assert.Equal(["3.000 mm", "10.000 mm", "6.000 mm"], values.Select(value => value.Text));
        foreach (var value in values)
        {
            var line = value.Line ?? throw new InvalidOperationException($"The value {value.Text} has no line.");
            Assert.False(line.Leader, $"The value {value.Text} has a leader.");
            Assert.Equal(0f, Vector2.Dot(line.B - line.A, line.Normal), 0.01f);
            var deepest = outline.Max(point => Vector2.Dot(point - line.A, line.Normal));
            Assert.Equal(deepest + EdgeValues.Gap, line.Offset, 0.01f);
            Assert.Equal(((line.A + line.B) / 2f) + (line.Normal * line.Offset), value.At);
        }
    }

    // Items 2 and 3: in the window each pill is centred on its dimension line, the side of the pill nearest the body is at
    // least at the offset of its line, so the pill is outside the outline, and the line goes from the extension line of one
    // end of the edge to the other. The drawn pill is smaller than 44 pixels high, and its target is at least 44 pixels.
    [Fact]
    public async Task Each_Pill_Is_On_Its_Line_And_Outside_The_Outline()
    {
        var (host, body) = await Plate();
        using var _ = host;
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var values = await ValuesOf(host, body, faces);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: values);
        }

        Assert.Equal(values.Count, ui.DimensionLines.Count);
        foreach (var (key, from, to, pill) in ui.DimensionLines)
        {
            var line = values.Single(value => value.Key == key).Line!;
            var centre = new Vector2((pill.X + pill.Z) / 2f, (pill.Y + pill.W) / 2f);
            Assert.True(Vector2.Distance((from + to) / 2f, centre) < 0.5f, $"The pill of {key} at {centre} is not in the middle of its line {from} {to}.");
            Assert.True(Vector2.Distance(from - line.A, to - line.B) < 0.01f, $"The line of {key} is not parallel to its edge.");
            Vector2[] corners = [new(pill.X, pill.Y), new(pill.Z, pill.Y), new(pill.Z, pill.W), new(pill.X, pill.W)];
            var nearest = corners.Min(corner => Vector2.Dot(corner - line.A, line.Normal));
            Assert.True(nearest >= line.Offset - 0.5f, $"The pill of {key} at {pill} comes {line.Offset - nearest} pixels into the outline and the gap.");

            var target = ui.ItemRectangle(key) ?? throw new InvalidOperationException($"No chip {key}.");
            Assert.True(pill.W - pill.Y < DesktopUi.ValueSize, $"The pill of {key} at {pill} is as high as its target.");
            Assert.True(target.Z - target.X >= DesktopUi.ValueSize && target.W - target.Y >= DesktopUi.ValueSize, $"The target of {key} at {target} is smaller than 44 pixels.");
            Assert.True(target.X <= pill.X && target.Y <= pill.Y && target.Z >= pill.Z && target.W >= pill.W, $"The pill of {key} at {pill} is not inside its target {target}.");
        }
    }

    // The ends of each segment of each edge of the body on the screen.
    private static List<Vector2> Outline(FacesInfo faces)
        => [.. faces.Edges.SelectMany(edge => edge.Segments).SelectMany(segment => new[] { segment.Start, segment.End })
            .Select(point => SketchOverlay.Segment(View, Display.X, Display.Y, (point.X, point.Y, point.Z), (point.X, point.Y, point.Z))?.A)
            .OfType<Vector2>()];

    private static async Task<IReadOnlyList<EdgeValues.Value>> ValuesOf(EngineHost host, Guid body, FacesInfo faces)
    {
        var sketches = new List<SketchInfo>();
        foreach (var id in await host.Session.Read((document, _) => document.Sketches.Select(sketch => sketch.Id).ToArray()))
            sketches.Add((await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!);
        return EdgeValues.Of(body, 3, faces.Edges, sketches, View, Display.X, Display.Y, "mm");
    }

    // A rectangle of 10 by 6 with a length of 10 on its first line and a distance of 6 between the ends of its second line,
    // extruded by 3, as in EdgeValueTests.
    private static async Task<(EngineHost Host, Guid Body)> Plate()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }), "constraintId");
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = points[1], Second = points[2], Value = 6 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        return (host, body);
    }

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }
}
