using Engine.Api.Http;
using Engine.Contracts.Sketch;
using Engine.Core.Geometry;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0085 (the redesign, slice e, TASK-0273): the active sketch under the body that it makes, with no GPU. ADR-0085 names
// this file in its enforced-by field.
public sealed class SketchLiftTests
{
    private const double Width = 1280;
    private const double Height = 720;

    private static readonly DatumFrame Xy = new(new DatumPoint(0, 0, 0), Datums.XY.U, Datums.XY.V, Datums.XY.Normal);

    // An oblique view, as the camera of a session gives it; the edit of a sketch of a body does not turn it.
    private static Camera Oblique() => new() { Target = (0, 0, 5), Distance = 120, YawDegrees = 30, PitchDegrees = 30 };

    // Item 3: the plane goes out of the box of the body, to the side away from the body, a quarter of the diagonal past it.
    [Fact]
    public void The_Plane_Moves_To_The_Side_Away_From_The_Body()
    {
        var sketch = Guid.NewGuid();
        var diagonal = Math.Sqrt((40 * 40) + (24 * 24) + (10 * 10));

        var up = SketchLift.Under(sketch, Xy, (-20, -12, 0), (20, 12, 10)) ?? throw new InvalidOperationException("No move.");
        var down = SketchLift.Under(sketch, Xy, (-20, -12, -10), (20, 12, 0)) ?? throw new InvalidOperationException("No move.");

        Assert.Equal((0d, 0d), (up.Dx, up.Dy));
        Assert.Equal(-SketchLift.Gap * diagonal, up.Dz, 9);
        Assert.Equal(SketchLift.Gap * diagonal, down.Dz, 9);
        Assert.Null(SketchLift.Under(sketch, Xy, (1, 1, 1), (1, 1, 1)));
    }

    // Item 2: the move changes the origin of the plane of its sketch only; the axes, the points and each other sketch stay.
    [Fact]
    public void The_Move_Changes_The_Origin_Of_Its_Sketch_Only()
    {
        var id = Guid.NewGuid();
        var sketch = new Engine.Core.Queries.SketchInfo(id, Datums.XY.Id, null, [new SketchPoint(Guid.NewGuid(), 3, 4)], [], [], [], [], true, 0) { Frame = Xy };
        var other = sketch with { SketchId = Guid.NewGuid() };
        var lift = new SketchLift(id, 0, 0, -12);

        var moved = lift.Apply(sketch);

        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, -12), Xy.U, Xy.V, Xy.Normal), moved.Frame);
        Assert.Equal(sketch.Points, moved.Points);
        Assert.Same(other, lift.Apply(other));
    }

    // Item 4: one line from each moved point to its place on the plane of the record.
    [Fact]
    public void A_Line_Links_Each_Moved_Point_To_Its_Place()
    {
        var id = Guid.NewGuid();
        SketchPoint[] points = [new(Guid.NewGuid(), -20, -12), new(Guid.NewGuid(), 20, -12), new(Guid.NewGuid(), 20, 12)];
        var sketch = new Engine.Core.Queries.SketchInfo(id, Datums.XY.Id, null, points, [], [], [], [], true, 0) { Frame = Xy };
        var lift = new SketchLift(id, 0, 0, -12);

        var links = lift.Links(sketch, Oblique(), Width, Height);

        Assert.Equal(3, links.Count);
        for (var i = 0; i < points.Length; i++)
        {
            Assert.Equal(SketchOverlay.Pixel(lift.Apply(sketch), Oblique(), Width, Height, (points[i].X, points[i].Y)), links[i].A);
            Assert.Equal(SketchOverlay.Pixel(sketch, Oblique(), Width, Height, (points[i].X, points[i].Y)), links[i].B);
        }
    }

    // Item 2: a tap on the moved sketch, in an oblique view, gives the point that the person sees there, with the position of
    // the plane of the record. Until TASK-0273 no sketch moved; the test holds that the picks use the moved plane.
    [Fact]
    public void A_Tap_On_The_Moved_Sketch_Gives_The_Position_That_The_Person_Sees()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var actions = new SketchActions(host.Session);
        var sketch = actions.Create(Datums.XY.Id) ?? throw new InvalidOperationException("No sketch.");
        var lift = SketchLift.Under(sketch, Xy, (-20, -12, 0), (20, 12, 10)) ?? throw new InvalidOperationException("No move.");
        actions.Lift = lift;
        Engine.Core.Queries.SketchInfo Record()
            => host.Session.Query<Engine.Core.Queries.SketchInfo>(new Engine.Core.Queries.GetSketchQuery { SketchId = sketch }).GetAwaiter().GetResult().Result!;
        // The pixel of (12, 8) on the moved plane, from the record of the session and the move, as the overlay draws it.
        var pixel = SketchOverlay.Pixel(lift.Apply(Record()), Oblique(), Width, Height, (12, 8)) ?? throw new InvalidOperationException("No pixel.");
        var before = Record().Points.Select(point => point.Id).ToHashSet();

        actions.Tap(Oblique(), pixel.X, pixel.Y, Width, Height);

        var point = Assert.Single(Record().Points, point => !before.Contains(point.Id));
        Assert.Equal((12d, 8d), (point.X, point.Y));
    }
}
