using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Tests;

// The sketch tools of the desktop with no window (ADR-0034, TASK-0101). ADR-0034 names this file in
// enforced-by: the ray to the plane of a sketch, the step of a tap, the turn of the camera, and
// the chain and the selection of the tools Line and Select.
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

        private string Name(Guid point) => Points.TryGetValue(point, out var at) ? $"({at.X},{at.Y})" : "origin";

        // The picture of the points, with the pixel (10 x, 10 y) for a point (x, y).
        public SketchPicture Picture(params Guid[] extra)
            => new(
                [.. Points.Select(point => new ToolPoint(point.Key, point.Value.X, point.Value.Y, 10 * point.Value.X, 10 * point.Value.Y)),
                 .. extra.Select(id => new ToolPoint(id, 0, 0, 0, 0))],
                [.. Lines.Select((line, index) => new ToolLine(new Guid(index + 1, 0, 0, new byte[8]), line.Start, line.End))],
                []);
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
    // and holds two items at most.
    [Fact]
    public void The_Tool_Select_Toggles_Two_Items_At_Most()
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
        Assert.Equal([bottom, b], tool.Selection);
        tool.Tap(200, 15, null, picture, editor);
        Assert.Equal([b], tool.Selection);
        tool.Tap(200, 100, null, picture, editor);
        Assert.Equal([b], tool.Selection);
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

    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) v)
    {
        var length = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return (v.X / length, v.Y / length, v.Z / length);
    }
}
