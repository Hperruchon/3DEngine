using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Tests;

// The sketch tools of the desktop with no window (ADR-0034, TASK-0101). ADR-0034 names this file in
// enforced-by: the ray to the plane of a sketch, the step of a tap, the turn of the camera, and
// the chain and the selection of the tools Line and Select. Phase K2e (TASK-0122): the tools Circle
// and Arc, and a pick of a curve.
public class SketchToolTests
{
    private const double Width = 800;
    private const double Height = 600;

    // The three origin planes of ADR-0033 item 1: u, v and the normal u × v.
    public static TheoryData<double[], double[]> Planes => new()
    {
        { [1, 0, 0], [0, 1, 0] },
        { [1, 0, 0], [0, 0, 1] },
        { [0, 1, 0], [0, 0, 1] },
    };

    private static (double X, double Y, double Z) V(double[] a) => (a[0], a[1], a[2]);

    // ADR-0034 item 2: a point (x, y) of a plane, seen through its pixel, gives (x, y) back.
    [Theory]
    [MemberData(nameof(Planes))]
    public void The_Ray_Of_A_Pixel_Gives_The_Position_In_The_Frame_Of_The_Plane(double[] u, double[] v)
    {
        var camera = new Camera { Target = (3, 2, 1), Distance = 50, YawDegrees = 35, PitchDegrees = 30 };
        var (pu, pv) = (V(u), V(v));
        var world = (4 * pu.X + -7 * pv.X, 4 * pu.Y + -7 * pv.Y, 4 * pu.Z + -7 * pv.Z);

        // The pixel of the world point: the ray from the eye towards it goes through that pixel.
        var eye = camera.Eye();
        var direction = Normal((world.Item1 - eye.X, world.Item2 - eye.Y, world.Item3 - eye.Z));
        var position = Picker.OnPlane(new Ray(eye, direction), (0, 0, 0), pu, pv);

        Assert.NotNull(position);
        Assert.Equal(4, position.Value.X, 9);
        Assert.Equal(-7, position.Value.Y, 9);
    }

    [Fact]
    public void A_Ray_Parallel_To_The_Plane_Or_Away_From_It_Gives_No_Position()
    {
        var along = new Ray((0, 5, 0), (1, 0, 0));
        var away = new Ray((0, 0, 5), (0, 0, 1));
        var towards = new Ray((0, 0, 5), (0, 0, -1));

        Assert.Null(Picker.OnPlane(along, (0, 0, 0), (1, 0, 0), (0, 1, 0)));
        Assert.Null(Picker.OnPlane(away, (0, 0, 0), (1, 0, 0), (0, 1, 0)));
        Assert.Equal((0.0, 0.0), Picker.OnPlane(towards, (0, 0, 0), (1, 0, 0), (0, 1, 0)));
    }

    // The ray of the centre of the picture meets the plane at the projection of the target.
    [Fact]
    public void The_Centre_Of_The_Picture_Gives_The_Target_On_The_Plane()
    {
        var camera = new Camera { Target = (6, -2, 0), Distance = 40, YawDegrees = 10, PitchDegrees = 15 };

        var position = Picker.OnPlane(Picker.RayThrough(camera, Width / 2, Height / 2, Width, Height), (0, 0, 0), (1, 0, 0), (0, 1, 0));

        Assert.Equal(6, position!.Value.X, 9);
        Assert.Equal(-2, position.Value.Y, 9);
    }

    // ADR-0034 item 3: the smallest step of 1, 2 and 5 × 10^k mm that is at least 8 pixels.
    [Theory]
    [InlineData(10.0, 1.0)]
    [InlineData(8.0, 1.0)]
    [InlineData(7.9, 2.0)]
    [InlineData(1.0, 10.0)]
    [InlineData(2.0, 5.0)]
    [InlineData(100.0, 0.1)]
    [InlineData(1e9, 0.001)]
    [InlineData(1e-9, 5e6)]
    [InlineData(0.0, 5e6)]
    [InlineData(double.NaN, 5e6)]
    public void The_Step_Is_The_Smallest_Of_At_Least_Eight_Pixels(double pixelsPerMillimetre, double millimetres)
    {
        Assert.Equal(millimetres, SketchGrid.Step(pixelsPerMillimetre).Millimetres);
    }

    // A rounded position is the double nearest to the decimal number, and a half goes away from zero.
    [Theory]
    [InlineData(0.1, 0.30000000001, 0.3)]
    [InlineData(0.001, 12.3456, 12.346)]
    [InlineData(2.0, 1.0, 2.0)]
    [InlineData(2.0, -1.0, -2.0)]
    [InlineData(0.001, 0.0025, 0.003)]
    [InlineData(5.0, 12.4, 10.0)]
    [InlineData(1.0, -0.4, 0.0)]
    public void A_Position_Rounds_To_A_Multiple_Of_The_Step(double step, double value, double rounded)
    {
        var grid = SketchGrid.Series.Single(candidate => candidate.Millimetres == step);

        Assert.Equal(rounded, grid.Round(value));
    }

    // ADR-0034 item 6 (Q4 A): the eye looks along the normal, from its side of the plane, and the
    // target and the distance stay.
    [Theory]
    [InlineData(0.0, 0.0, 1.0, 20.0, 10.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 1.0, 200.0, 10.0, 180.0, 0.0)]
    [InlineData(1.0, 0.0, 0.0, 20.0, 10.0, 90.0, 0.0)]
    [InlineData(1.0, 0.0, 0.0, -70.0, 10.0, -90.0, 0.0)]
    [InlineData(0.0, -1.0, 0.0, 20.0, 30.0, 20.0, 89.0)]
    [InlineData(0.0, -1.0, 0.0, 20.0, -30.0, 20.0, -89.0)]
    public void The_Camera_Faces_The_Plane_From_The_Side_Of_The_Eye(double nx, double ny, double nz, double yaw, double pitch, double faceYaw, double facePitch)
    {
        var camera = new Camera { Target = (1, 2, 3), Distance = 25, YawDegrees = yaw, PitchDegrees = pitch };

        SketchCamera.Face(camera, (nx, ny, nz));

        Assert.Equal(faceYaw, camera.YawDegrees, 9);
        Assert.Equal(facePitch, camera.PitchDegrees, 9);
        Assert.Equal((1.0, 2.0, 3.0), camera.Target);
        Assert.Equal(25, camera.Distance);
    }

    // An editor that records each command and gives a new identifier, or refuses each line.
    private sealed class Editor(bool refuseLines = false) : ISketchEditor
    {
        public List<string> Log { get; } = new();
        public Dictionary<Guid, (double X, double Y)> Points { get; } = new();
        public List<(Guid Start, Guid End)> Lines { get; } = new();
        public Dictionary<Guid, (Guid Centre, double Radius)> Circles { get; } = new();

        public Guid? AddPoint(double x, double y)
        {
            var id = Guid.NewGuid();
            Points[id] = (x, y);
            Log.Add($"point {x} {y}");
            return id;
        }

        public Guid? AddLine(Guid start, Guid end)
        {
            if (refuseLines)
                return null;
            Lines.Add((start, end));
            Log.Add($"line {Name(start)} {Name(end)}");
            return Guid.NewGuid();
        }

        public Guid? AddCircle(Guid centre, double radius)
        {
            var id = Guid.NewGuid();
            Circles[id] = (centre, radius);
            Log.Add($"circle {Name(centre)} {radius}");
            return id;
        }

        public Guid? AddArc(Guid centre, Guid start, Guid end)
        {
            Log.Add($"arc {Name(centre)} {Name(start)} {Name(end)}");
            return Guid.NewGuid();
        }

        private string Name(Guid point) => Points.TryGetValue(point, out var at) ? $"({at.X},{at.Y})" : "origin";

        // A circle in pixels as a polygon of 32 sides, with the pixel (10 x, 10 y) for a point (x, y).
        private ToolCurve Curve(Guid id, (Guid Centre, double Radius) circle)
        {
            var c = Points[circle.Centre];
            var corners = Enumerable.Range(0, 33).Select(i => 2 * Math.PI * i / 32)
                .Select(angle => (X: 10 * (c.X + (circle.Radius * Math.Cos(angle))), Y: 10 * (c.Y + (circle.Radius * Math.Sin(angle))))).ToArray();
            return new ToolCurve(id, [.. corners.Skip(1).Select((corner, i) => (corners[i], corner))], c, circle.Radius);
        }

        // The picture of the points, with the pixel (10 x, 10 y) for a point (x, y).
        public SketchPicture Picture(params Guid[] extra)
            => new(
                [.. Points.Select(point => new ToolPoint(point.Key, point.Value.X, point.Value.Y, 10 * point.Value.X, 10 * point.Value.Y)),
                 .. extra.Select(id => new ToolPoint(id, 0, 0, 0, 0))],
                [.. Lines.Select((line, index) => new ToolLine(new Guid(index + 1, 0, 0, new byte[8]), line.Start, line.End))],
                [],
                [.. Circles.Select(circle => Curve(circle.Key, circle.Value))]);
    }

    // A tap at the pixel of the position (x, y), as the picture of the editor places it.
    private static void Tap(SketchTool tool, Editor editor, double x, double y, Guid? origin = null)
        => tool.Tap(10 * x, 10 * y, (x, y), origin is { } id ? editor.Picture(id) : editor.Picture(), editor);

    // ADR-0034 item 4 (Q2 A): four taps and a tap on the first point make a closed rectangle of four
    // points and four lines, and the chain ends.
    [Fact]
    public void A_Chain_Of_Taps_Closes_On_Its_First_Point()
    {
        var (tool, editor) = (new SketchTool(), new Editor());

        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 40, 0);
        Tap(tool, editor, 40, 20);
        Tap(tool, editor, 0, 20);
        tool.Tap(1.5, -1, (0.15, -0.1), editor.Picture(), editor);

        Assert.Equal(
            ["point 0 0", "point 40 0", "line (0,0) (40,0)", "point 40 20", "line (40,0) (40,20)", "point 0 20", "line (40,20) (0,20)", "line (0,20) (0,0)"],
            editor.Log);
        Assert.Null(tool.Last);
    }

    // A tap within 22 pixels of a point uses it; a tap on the last point ends the chain; a tap that
    // meets no plane does nothing.
    [Fact]
    public void A_Tap_Near_A_Point_Uses_It_And_A_Tap_On_The_Last_Point_Ends_The_Chain()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        var origin = Guid.NewGuid();

        tool.Tap(15, 15, (1.5, 1.5), editor.Picture(origin), editor);
        tool.Tap(300, 0, null, editor.Picture(origin), editor);
        Tap(tool, editor, 30, 0, origin);
        tool.Tap(300 + 20, 5, (32, 0.5), editor.Picture(origin), editor);

        Assert.Equal(["point 30 0", "line origin (30,0)"], editor.Log);
        Assert.Null(tool.Last);
    }

    // A tap on the first point of a chain of one line ends the chain and does not draw that line
    // again; Close needs two lines too.
    [Fact]
    public void A_Chain_Of_One_Line_Does_Not_Close_On_Itself()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 10, 0);
        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 0, 10);
        Tap(tool, editor, 10, 10);
        tool.Close(editor);

        Assert.Equal(["point 0 0", "point 10 0", "line (0,0) (10,0)", "point 0 10", "point 10 10", "line (0,10) (10,10)"], editor.Log);
        Assert.Null(tool.Last);
    }

    // ADR-0034 item 7 (Q5 A): the pad gives the first point as a position and each next point as an
    // offset; Close joins the last point to the first one.
    [Fact]
    public void The_Pad_Gives_Offsets_From_The_Last_Point_And_Close_Ends_The_Chain()
    {
        var (tool, editor) = (new SketchTool(), new Editor());

        tool.Next(0, 0, editor.Picture(), editor);
        tool.Next(40, 0, editor.Picture(), editor);
        tool.Next(0, 20, editor.Picture(), editor);
        tool.Next(-40, 0, editor.Picture(), editor);
        tool.Close(editor);

        Assert.Equal(
            ["point 0 0", "point 40 0", "line (0,0) (40,0)", "point 40 20", "line (40,0) (40,20)", "point 0 20", "line (40,20) (0,20)", "line (0,20) (0,0)"],
            editor.Log);
        Assert.Null(tool.Last);
    }

    // The pad uses a point only at its exact position: an offset that ends 2^-10 mm from the first
    // point adds a point, and the offset that ends on it closes the chain. The numbers are exact in
    // binary, so the sums are exact.
    [Fact]
    public void The_Pad_Uses_A_Point_Only_At_Its_Exact_Position()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        tool.Next(0, 0, editor.Picture(), editor);
        tool.Next(1, 0, editor.Picture(), editor);
        tool.Next(0, 1, editor.Picture(), editor);
        tool.Next(-1, -0.9990234375, editor.Picture(), editor);
        tool.Next(0, -0.0009765625, editor.Picture(), editor);

        Assert.Equal(4, editor.Points.Count);
        Assert.Equal(4, editor.Lines.Count);
        Assert.Null(tool.Last);
    }

    // A line that the session refuses leaves the chain at its last point.
    [Fact]
    public void A_Refused_Line_Leaves_The_Chain_Where_It_Was()
    {
        var (tool, editor) = (new SketchTool(), new Editor(refuseLines: true));
        Tap(tool, editor, 0, 0);
        var first = tool.Last;
        Tap(tool, editor, 10, 0);

        Assert.Equal(first, tool.Last);
    }

    // ADR-0034 item 4 (Q6 A): the tool Select toggles the nearest point, or else the nearest line,
    // and holds three items at most since K2g (TASK-0127); a fourth item replaces the first.
    [Fact]
    public void The_Tool_Select_Toggles_Three_Items_At_Most()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 40, 0);
        Tap(tool, editor, 40, 20);
        tool.Kind = SketchToolKind.Select;
        var picture = editor.Picture();
        var (a, b) = (picture.Points[0].Id, picture.Points[1].Id);
        var bottom = picture.Lines[0].Id;

        tool.Tap(2, 3, null, picture, editor);
        tool.Tap(200, 15, null, picture, editor);
        Assert.Equal([a, bottom], tool.Selection);

        tool.Tap(398, 1, null, picture, editor);
        Assert.Equal([a, bottom, b], tool.Selection);
        var right = picture.Lines[1].Id;
        tool.Tap(402, 100, null, picture, editor);
        Assert.Equal([bottom, b, right], tool.Selection);
        tool.Tap(200, 15, null, picture, editor);
        Assert.Equal([b, right], tool.Selection);
        tool.Tap(200, 100, null, picture, editor);
        Assert.Equal([b, right], tool.Selection);
        Assert.Equal(3, editor.Points.Count);
    }

    // ADR-0034 item 5: an undo that removes the last point ends the chain, and the selection keeps
    // only the items of the record.
    [Fact]
    public void Forget_Drops_The_Items_That_Left_The_Record()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 10, 0);
        var picture = editor.Picture();
        tool.Toggle(picture.Points[0].Id);
        tool.Toggle(picture.Lines[0].Id);

        var undone = new SketchPicture([picture.Points[0]], [], []);
        tool.Forget(undone);

        Assert.Null(tool.Last);
        Assert.Equal([picture.Points[0].Id], tool.Selection);
    }

    // Phase K2e: the tool Circle takes the centre and a point on the circle, and adds no point
    // on the circle. The radius is the distance in the plane.
    [Fact]
    public void The_Tool_Circle_Adds_A_Centre_And_A_Circle_Of_The_Distance()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Circle }, new Editor());

        Tap(tool, editor, 1, 1);
        Assert.Equal(tool.First, tool.Last);
        Assert.NotNull(tool.First);
        Tap(tool, editor, 4, 5);

        Assert.Equal(["point 1 1", "circle (1,1) 5"], editor.Log);
        Assert.Null(tool.First);
    }

    // A tap on a point uses its position, and a tap on the centre again ends the circle.
    [Fact]
    public void The_Tool_Circle_Uses_A_Point_And_Ends_On_Its_Centre()
    {
        var (tool, editor) = (new SketchTool(), new Editor());
        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 6, 8);
        tool.Kind = SketchToolKind.Circle;
        editor.Log.Clear();

        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 0, 0);
        Assert.Null(tool.First);
        Tap(tool, editor, 0.5, 0.5);
        Tap(tool, editor, 6.5, 8.5);

        Assert.Equal(["circle (0,0) 10"], editor.Log);
    }

    // The tool Arc takes the centre, the start and the end; a new end goes on the circle of the
    // start in the direction of the tap, so the implicit equation of the arc holds at the add.
    [Fact]
    public void The_Tool_Arc_Puts_A_New_End_On_The_Circle_Of_The_Start()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Arc }, new Editor());

        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 5, 0);
        Assert.Equal(editor.Points.Keys.Last(), tool.Last);
        Tap(tool, editor, 0, 3);

        Assert.Equal(["point 0 0", "point 5 0", "point 0 5", "arc (0,0) (5,0) (0,5)"], editor.Log);
        Assert.Null(tool.First);
    }

    // The pad gives a position to a tool of a curve, never an offset.
    [Fact]
    public void The_Pad_Gives_Positions_To_The_Tool_Circle()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Circle }, new Editor());

        tool.Next(2, 2, editor.Picture(), editor);
        tool.Next(2, 5, editor.Picture(), editor);

        Assert.Equal(["point 2 2", "circle (2,2) 3"], editor.Log);
    }

    // The tool Select picks a curve near its polyline, and Forget drops a curve that left the
    // record and ends a curve whose centre left it.
    [Fact]
    public void The_Tool_Select_Picks_A_Curve_And_Forget_Drops_It()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Circle }, new Editor());
        Tap(tool, editor, 20, 20);
        Tap(tool, editor, 25, 20);
        var circle = editor.Circles.Keys.Single();
        tool.Kind = SketchToolKind.Select;

        tool.Tap(250, 205, null, editor.Picture(), editor);
        Assert.Equal([circle], tool.Selection);
        tool.Forget(editor.Picture());
        Assert.Equal([circle], tool.Selection);

        tool.Kind = SketchToolKind.Circle;
        Tap(tool, editor, 50, 50);
        Assert.NotNull(tool.First);
        tool.Forget(new SketchPicture([], [], []));
        Assert.Null(tool.First);
        Assert.Empty(tool.Selection);
    }

    // Phase R11a (TASK-0131, ADR-0041): a tap with no point within 22 pixels takes the midpoint of a
    // line, else the nearest point on a line or a curve, else the position of the grid. The line goes
    // from (0, 0) to (10, 0), at the pixels (0, 0) to (100, 0), and the circle has the centre (30, 30)
    // and the radius 5.
    [Theory]
    [InlineData(52, 3, 5.0, 0.0)]
    [InlineData(75, 4, 7.5, 0.0)]
    [InlineData(200, 200, 20.0, 20.0)]
    public void A_Tap_Snaps_To_A_Midpoint_A_Line_Or_The_Grid(double pixelX, double pixelY, double x, double y)
    {
        var editor = new Editor();
        editor.AddLine(editor.AddPoint(0, 0)!.Value, editor.AddPoint(10, 0)!.Value);
        var tool = new SketchTool();
        editor.Log.Clear();

        tool.Tap(pixelX, pixelY, (pixelX / 10, pixelY / 10), editor.Picture(), editor);

        Assert.Equal([$"point {x} {y}"], editor.Log);
    }

    [Fact]
    public void A_Tap_Near_A_Circle_Snaps_Onto_It_And_A_Point_Comes_First()
    {
        var editor = new Editor();
        var centre = editor.AddPoint(30, 30)!.Value;
        editor.AddCircle(centre, 5);
        var tool = new SketchTool();

        tool.Tap(352, 301, (35.2, 30.1), editor.Picture(), editor);
        var added = editor.Points.Last().Value;
        Assert.Equal(5, Math.Sqrt(((added.X - 30) * (added.X - 30)) + ((added.Y - 30) * (added.Y - 30))), 12);

        tool.End();
        var count = editor.Points.Count;
        tool.Tap(302, 301, (30.2, 30.1), editor.Picture(), editor);
        Assert.Equal(count, editor.Points.Count);
        Assert.Equal(centre, tool.First);
    }

    // Finding V57 of the codebase review of 2026-10-31 (TASK-0133): a centre that moved between the two
    // taps gives the radius from its current position.
    [Fact]
    public void A_Circle_Takes_The_Current_Position_Of_Its_Centre()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Circle }, new Editor());
        Tap(tool, editor, 0, 0);
        var centre = tool.First!.Value;
        editor.Points[centre] = (10, 0);

        Tap(tool, editor, 13, 4);

        Assert.Equal("circle (10,0) 5", editor.Log[^1]);
    }

    // Finding V61 (TASK-0133): a new end in the direction of the start ends the curve, with no second
    // point at the position of the start and no arc.
    [Fact]
    public void An_End_In_The_Direction_Of_The_Start_Ends_The_Arc()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Arc }, new Editor());

        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 5, 0);
        Tap(tool, editor, 10, 0);

        Assert.Equal(["point 0 0", "point 5 0"], editor.Log);
        Assert.Null(tool.First);
    }

    // Finding V63 (TASK-0144): at 10 pixels for each unit, a tap 30 pixels from the start puts the new
    // end 0.03 units from it, a third of a pixel; it ends the curve, with no second point and no arc.
    [Fact]
    public void An_End_Within_A_Pixel_Of_The_Start_Ends_The_Arc()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Arc }, new Editor());

        Tap(tool, editor, 0, 0);
        Tap(tool, editor, 100, 1);
        Tap(tool, editor, 103, 1);

        Assert.Equal(["point 0 0", "point 100 1"], editor.Log);
        Assert.Null(tool.First);
    }

    // V63 (TASK-0144): the pad gives no pixel, so the rule in pixels does not act, and the pad still
    // makes an arc. The first form of the fix ended each arc of the pad, and only the GPU test saw it.
    [Fact]
    public void An_Arc_From_The_Pad_Still_Makes_An_Arc()
    {
        var (tool, editor) = (new SketchTool { Kind = SketchToolKind.Arc }, new Editor());

        tool.Next(8, 2, editor.Picture(), editor);
        tool.Next(14, 2, editor.Picture(), editor);
        tool.Next(8, 8, editor.Picture(), editor);

        Assert.Equal(["point 8 2", "point 14 2", "point 8 8", "arc (8,2) (14,2) (8,8)"], editor.Log);
    }

    // Finding V62 (TASK-0133): a tap on a circle that a line touches selects the circle, the nearer
    // item; the line within 22 pixels took the pick.
    [Fact]
    public void The_Tool_Select_Picks_The_Nearer_Of_A_Line_And_A_Curve()
    {
        var editor = new Editor();
        editor.AddCircle(editor.AddPoint(30, 30)!.Value, 5);
        editor.AddLine(editor.AddPoint(20, 25)!.Value, editor.AddPoint(40, 25)!.Value);
        var tool = new SketchTool { Kind = SketchToolKind.Select };

        tool.Tap(330, 260, null, editor.Picture(), editor);

        Assert.Equal([editor.Circles.Keys.Single()], tool.Selection);
    }

    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) v)
    {
        var length = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return (v.X / length, v.Y / length, v.Z / length);
    }
}
