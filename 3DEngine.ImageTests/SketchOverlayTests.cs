using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The sketch in the desktop, read only (ADR-0033 item 13, TASK-0100): the projection of
// the sketch events, the overlay in pixels of the window, which agrees with the ray of a
// pick, and the lines on the background list of the panel. No GPU.
[Collection(DearImGui.Collection)]
public class SketchOverlayTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static Camera Camera() => new() { YawDegrees = 30, PitchDegrees = 25, Distance = 120, Target = (10, 5, -3) };

    private static SketchInfo Sketch(Guid datum, params (double X, double Y)[] points)
    {
        var ids = points.Select(_ => Guid.NewGuid()).ToArray();
        var lines = ids.Skip(1).Select((id, index) => new SketchLine(Guid.NewGuid(), ids[index], id)).ToArray();
        return new SketchInfo(Guid.NewGuid(), datum, null,
            points.Select((point, index) => new SketchPoint(ids[index], point.X, point.Y)).ToArray(), lines, [], [], [], true, 2 * (points.Length - 1));
    }

    // The overlay is the inverse of Picker.RayThrough: a point on the ray of a pixel projects
    // back to that pixel.
    [Theory]
    [InlineData(640, 360)]
    [InlineData(17, 700)]
    [InlineData(1200, 40)]
    public void A_Point_On_The_Ray_Of_A_Pixel_Projects_To_That_Pixel(double x, double y)
    {
        var camera = Camera();
        var ray = Picker.RayThrough(camera, x, y, Display.X, Display.Y);
        // A point of the plane XY on the ray: origin.Z + t·direction.Z = 0.
        var t = -ray.Origin.Z / ray.Direction.Z;
        var hit = (ray.Origin.X + (t * ray.Direction.X), ray.Origin.Y + (t * ray.Direction.Y));

        var strokes = SketchOverlay.Of([Sketch(Datums.XY.Id, hit)], camera, Display.X, Display.Y);

        var point = Assert.Single(strokes.Points).At;
        Assert.Equal(x, point.X, 2);
        Assert.Equal(y, point.Y, 2);
    }

    // ADR-0033 item 1: a point (x, y) of a sketch on XZ is at (x, 0, y), and on YZ at (0, x, y).
    [Fact]
    public void Each_Datum_Places_The_Points_Of_A_Sketch_In_Its_Plane()
    {
        var camera = Camera();
        var onXz = SketchOverlay.Of([Sketch(Datums.XZ.Id, (4, 7))], camera, Display.X, Display.Y).Points.Single().At;
        var onYz = SketchOverlay.Of([Sketch(Datums.YZ.Id, (4, 7))], camera, Display.X, Display.Y).Points.Single().At;
        var asXz = SketchOverlay.Of([Sketch(Datums.XY.Id, (4, 0))], camera, Display.X, Display.Y).Points.Single().At;

        var ray = Picker.RayThrough(camera, onXz.X, onXz.Y, Display.X, Display.Y);
        var t = (7 - ray.Origin.Z) / ray.Direction.Z;
        Assert.Equal(4, ray.Origin.X + (t * ray.Direction.X), 3);
        Assert.Equal(0, ray.Origin.Y + (t * ray.Direction.Y), 3);
        Assert.NotEqual(onXz, onYz);
        Assert.NotEqual(onXz, asXz);
    }

    // A line that crosses the plane of the eye is cut at the near distance, and a line behind
    // the eye gives nothing.
    [Fact]
    public void A_Line_Behind_The_Eye_Is_Cut_Or_Left_Out()
    {
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 10, Target = (0, 0, 0) };
        // The eye is at (0, 0, 10) and looks along -z.
        var crossing = SketchOverlay.Of([Sketch(Datums.XZ.Id, (0, 0), (0, 20))], camera, Display.X, Display.Y);
        var behind = SketchOverlay.Of([Sketch(Datums.XZ.Id, (1, 12), (-1, 15))], camera, Display.X, Display.Y);

        var (a, b, _) = Assert.Single(crossing.Lines);
        Assert.True(float.IsFinite(a.X) && float.IsFinite(b.X) && float.IsFinite(a.Y) && float.IsFinite(b.Y));
        Assert.Single(crossing.Points);
        Assert.Empty(behind.Lines);
        Assert.Empty(behind.Points);
    }

    // The projection follows the events of the session: a sketch, its points and its line,
    // an undo of the line, and an undo of each step down to the removal of the sketch.
    [Fact]
    public async Task The_Desktop_Follows_The_Sketch_Events_Of_The_Session()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var sketches = new SessionSketches(host);
        var session = host.Session;

        var sketch = (await session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id })).Outputs.Values["sketchId"];
        var a = (await session.Apply(new AddSketchPointCommand { SketchId = (Guid)sketch!, X = 1, Y = 2 })).Outputs.Values["pointId"];
        var b = (await session.Apply(new AddSketchPointCommand { SketchId = (Guid)sketch, X = 5, Y = 2 })).Outputs.Values["pointId"];
        await session.Apply(new AddSketchLineCommand { SketchId = (Guid)sketch, StartPointId = (Guid)a!, EndPointId = (Guid)b! });
        sketches.Apply();
        var drawn = Assert.Single(sketches.Live);

        await session.Apply(new UndoCommand());
        sketches.Apply();
        var undone = Assert.Single(sketches.Live);
        for (var step = 0; step < 3; step++)
            await session.Apply(new UndoCommand());
        sketches.Apply();

        Assert.Equal((3, 1), (drawn.Points.Count, drawn.Lines.Count));
        Assert.Equal((3, 0), (undone.Points.Count, undone.Lines.Count));
        Assert.Empty(sketches.Live);
    }

    // A sketch that exists before the desktop starts is drawn too.
    [Fact]
    public async Task A_Sketch_Of_An_Open_Document_Is_Drawn()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.YZ.Id, SketchName = "Before" });

        using var sketches = new SessionSketches(host);
        sketches.Apply();

        Assert.Equal("Before", Assert.Single(sketches.Live).Name);
    }

    // ADR-0033 item 13: the panel draws the lines and the points in the colour of a sketch.
    [Fact]
    public void The_Panel_Draws_The_Strokes_Of_A_Sketch()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var strokes = new SketchOverlay.Strokes(
            [new(new Vector2(700, 300), new Vector2(900, 500), SketchOverlay.Mark.Active)],
            [new(new Vector2(700, 300), SketchOverlay.Mark.Active), new(new Vector2(900, 500), SketchOverlay.Mark.Active)],
            [],
            []);

        var without = ui.Frame(1f / 60f, Display, Vector2.One, []);
        var with = ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, strokes);

        Assert.DoesNotContain(without.Vertices, vertex => vertex.Colour == DesktopUi.SketchColour);
        Assert.Contains(with.Vertices, vertex => vertex.Colour == DesktopUi.SketchColour && vertex.Position.X >= 690 && vertex.Position.X <= 910);
    }

    // ADR-0034 item 9: with a focus, the active sketch has its labels, its selected items in white,
    // and the ring of the first point of an open chain; another sketch is grey and has no label.
    [Fact]
    public void The_Active_Sketch_Has_Labels_A_Selection_And_A_Ring()
    {
        var camera = Camera();
        var active = Sketch(Datums.XY.Id, (0, 0), (10, 0), (10, 5));
        var other = Sketch(Datums.XZ.Id, (3, 3), (6, 3));
        var selected = active.Lines[0].Id;
        var start = active.Points[1].Id;

        var strokes = SketchOverlay.Of([active, other], camera, Display.X, Display.Y, new SketchOverlay.Focus(active.SketchId, [selected], start));

        Assert.Equal([SketchOverlay.Mark.Selected, SketchOverlay.Mark.Active, SketchOverlay.Mark.Other], strokes.Lines.Select(line => line.Mark));
        Assert.Equal(3, strokes.Points.Count(point => point.Mark == SketchOverlay.Mark.Active));
        Assert.Equal(2, strokes.Points.Count(point => point.Mark == SketchOverlay.Mark.Other));
        Assert.Equal(["L1", "L2", "P1", "P2", "P3"], strokes.Labels.Select(label => label.Text).Order());
        Assert.Equal(strokes.Points[1].At, Assert.Single(strokes.Rings));
    }

    // ADR-0034 item 4: the picture of the tool gives the pixel of each point that the overlay
    // draws, and no pixel with no camera.
    [Fact]
    public void The_Picture_Of_The_Tool_Agrees_With_The_Overlay()
    {
        var camera = Camera();
        var sketch = Sketch(Datums.YZ.Id, (2, 1), (8, -4));

        var picture = SketchOverlay.Picture(sketch, camera, Display.X, Display.Y);
        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y);

        Assert.Equal(strokes.Points.Select(point => ((double?)point.At.X, (double?)point.At.Y)), picture.Points.Select(point => (point.PixelX, point.PixelY)));
        Assert.Equal(sketch.Lines.Select(line => line.Id), picture.Lines.Select(line => line.Id));
        Assert.All(SketchOverlay.Picture(sketch).Points, point => Assert.Null(point.PixelX));
    }

    // A sketch with a circle of radius 5 about (0, 0) and an arc about (20, 0) from (23, 0) to
    // (20, 3), a quarter turn counter-clockwise (ADR-0033 item 2).
    private static SketchInfo WithCurves()
    {
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        (double X, double Y)[] at = [(0, 0), (20, 0), (23, 0), (20, 3)];
        return new SketchInfo(Guid.NewGuid(), Datums.XY.Id, null,
            [.. at.Select((point, index) => new SketchPoint(ids[index], point.X, point.Y))], [],
            [new SketchCircle(Guid.NewGuid(), ids[0], 5)], [new SketchArc(Guid.NewGuid(), ids[1], ids[2], ids[3])], [], true, 4);
    }

    // Phase K2e (TASK-0122): a circle is a polyline of a full turn, and an arc the part of it that
    // its sweep covers, counter-clockwise from its start. Each vertex is on the curve.
    [Fact]
    public void A_Circle_And_An_Arc_Are_Polylines_On_Their_Curves()
    {
        var sketch = WithCurves();

        var curves = SketchOverlay.Curves(sketch).ToArray();

        var circle = curves[0].Polyline;
        Assert.Equal(SketchOverlay.TurnSegments + 1, circle.Count);
        Assert.All(circle, p => Assert.Equal(5, Math.Sqrt((p.X * p.X) + (p.Y * p.Y)), 9));
        var arc = curves[1].Polyline;
        Assert.Equal((SketchOverlay.TurnSegments / 4) + 1, arc.Count);
        Assert.Equal(23, arc[0].X, 9);
        Assert.Equal(0, arc[0].Y, 9);
        Assert.Equal(20, arc[^1].X, 9);
        Assert.Equal(3, arc[^1].Y, 9);
        Assert.All(arc, p => Assert.True(p.Y >= -1e-9 && p.X >= 20 - 1e-9, $"({p.X}, {p.Y}) is not on the quarter turn"));
    }

    // Phase R11a (TASK-0131, ADR-0041): the picture gives each curve its centre and its radius in the
    // plane, for a snap; the radius of an arc is the distance of its start from its centre.
    [Fact]
    public void The_Picture_Gives_The_Centre_And_The_Radius_Of_Each_Curve()
    {
        var picture = SketchOverlay.Picture(WithCurves(), Camera(), Display.X, Display.Y);

        Assert.Equal(((0.0, 0.0), 5.0), (picture.Curves[0].Centre, picture.Curves[0].Radius));
        Assert.Equal(((20.0, 0.0), 3.0), (picture.Curves[1].Centre, picture.Curves[1].Radius));
    }

    // The overlay draws the segments of each curve with its label, and the picture of the tool has
    // the same segments in pixels.
    [Fact]
    public void The_Overlay_And_The_Picture_Draw_Each_Curve()
    {
        var camera = Camera();
        var sketch = WithCurves();

        var strokes = SketchOverlay.Of([sketch], camera, Display.X, Display.Y, new SketchOverlay.Focus(sketch.SketchId, [sketch.Circles[0].Id], null));
        var picture = SketchOverlay.Picture(sketch, camera, Display.X, Display.Y);

        Assert.Equal(SketchOverlay.TurnSegments, strokes.Lines.Count(line => line.Mark == SketchOverlay.Mark.Selected));
        Assert.Equal(SketchOverlay.TurnSegments / 4, strokes.Lines.Count(line => line.Mark == SketchOverlay.Mark.Active));
        Assert.Equal(["A1", "P1", "P2", "P3", "P4", "R1"], strokes.Labels.Select(label => label.Text).Order());
        Assert.Equal([sketch.Circles[0].Id, sketch.Arcs[0].Id], picture.Curves.Select(curve => curve.Id));
        Assert.Equal(
            strokes.Lines.Select(line => ((double)line.A.X, (double)line.A.Y)),
            picture.Curves.SelectMany(curve => curve.Pixels).Select(segment => segment.A));
    }
}
