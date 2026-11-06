using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// The desktop and a sketch on a face (ADR-0053 items 6 to 8, phase K0i, TASK-0171), with a session and no GPU: a tap on a
// face makes a datum and a sketch and then reuses the datum, the tool puts a point on the plane of the face, the overlay
// draws a point where it is in space, the items of a reference draw dim, and the projection follows a moved datum.
public class SketchOnFaceTests
{
    private const double Width = 1280;
    private const double Height = 720;

    // The camera looks along -z at the origin from 100 mm.
    private static Camera Facing() => new() { Target = (0, 0, 0), Distance = 100, YawDegrees = 0, PitchDegrees = 0 };

    private static async Task<Guid> Box(EngineHost host, double z)
    {
        var box = await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 6, SizeZ = 4 });
        Assert.True(box.Outputs.TryGet<Guid>("bodyId", out var body));
        if (z == 0)
            return body;
        var moved = await host.Session.Apply(new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = z });
        Assert.True(moved.Outputs.TryGet<Guid>("bodyId", out var top));
        return top;
    }

    // Item 6: the first tap on the top of the box makes a datum and a sketch on it, and the camera can face its normal;
    // a second tap on the same face reuses the datum. A tap off each face gives the code and no sketch.
    [Fact]
    public async Task A_Tap_On_A_Face_Makes_A_Datum_And_A_Sketch_Then_Reuses_The_Datum()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new SketchActions(host.Session);
        var box = await Box(host, 30);

        var first = actions.CreateOnFace(box, (1, 1, 32), (0, 0, 1));
        Assert.NotNull(first);
        Assert.Equal(first.Value.Sketch, actions.Active);
        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 32), new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1)), first.Value.Frame);
        actions.Finish();
        var second = actions.CreateOnFace(box, (-2, 2, 32), (0, 0, 1));
        Assert.NotNull(second);
        var miss = actions.CreateOnFace(box, (20, 0, 32), (0, 0, 1));

        Assert.Null(miss);
        Assert.StartsWith("E-DATUM-FACE-NOT-FOUND", actions.Message);
        var (datums, sketches) = await host.Session.Read((document, _) => (document.Datums.Count, document.Sketches.Count));
        Assert.Equal((1, 2), (datums, sketches));
    }

    // Item 7: a tap of the tool Line takes the position on the plane of the face, 32 mm above the origin. A plane through
    // the origin would give about (4.4, 1.5) for the pixel of (3, 1, 32). The overlay draws the point on the ray of its
    // pixel, so a sketch and a pick agree on the face too.
    [Fact]
    public async Task The_Tool_And_The_Overlay_Use_The_Plane_Of_The_Face()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new SketchActions(host.Session);
        var box = await Box(host, 30);
        var (sketch, _) = actions.CreateOnFace(box, (1, 1, 32), (0, 0, 1))!.Value;
        var camera = Facing();
        var pixel = SketchOverlay.Pixel(actions.Current()!, camera, Width, Height, (3, 1))!.Value;

        actions.Tap(camera, pixel.X, pixel.Y, Width, Height);

        var info = actions.Current()!;
        var point = Assert.Single(info.Points, point => point.X != 0 || point.Y != 0);
        Assert.Equal((3.0, 1.0), (point.X, point.Y));
        var spot = Assert.Single(SketchOverlay.Of([info], camera, Width, Height).Points, spot => Math.Abs(spot.At.X - pixel.X) < 0.5 && Math.Abs(spot.At.Y - pixel.Y) < 0.5);
        var ray = Picker.RayThrough(camera, spot.At.X, spot.At.Y, Width, Height);
        var t = (32 - ray.Origin.Z) / ray.Direction.Z;
        Assert.Equal(3, ray.Origin.X + (t * ray.Direction.X), 2);
        Assert.Equal(1, ray.Origin.Y + (t * ray.Direction.Y), 2);
        Assert.Equal(sketch, info.SketchId);
    }

    // Item 8: the point of a reference to a vertex of the box draws dim, and the origin of the sketch does not.
    [Fact]
    public async Task The_Items_Of_A_Reference_Draw_Dim()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new SketchActions(host.Session);
        var box = await Box(host, 0);
        var floor = actions.Create(Datums.XY.Id)!.Value;
        var reference = await host.Session.Apply(new AddSketchReferenceCommand { SketchId = floor, FeatureId = box, Element = "+x+y+z" });
        Assert.True(reference.Outputs.TryGet<Guid>("referenceId", out var point));

        var strokes = SketchOverlay.Of([actions.Current()!], Facing(), Width, Height);
        var at = SketchOverlay.Pixel(actions.Current()!, Facing(), Width, Height, (5, 3))!.Value;

        Assert.Equal(SketchOverlay.Mark.Reference, Assert.Single(strokes.Points, spot => spot.At == at).Mark);
        Assert.All(strokes.Points.Where(spot => spot.At != at), spot => Assert.Equal(SketchOverlay.Mark.Active, spot.Mark));
        Assert.Contains(actions.Current()!.Points, item => item.Id == point);
    }

    // Item 7: a new distance of the base moves the datum of the sketch, and the projection reads the sketch again, so its
    // frame follows.
    [Fact]
    public async Task The_Projection_Follows_A_Moved_Datum()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new SketchActions(host.Session);
        Assert.NotNull(actions.Create(Datums.XY.Id));
        foreach (var (a, b) in new[] { (0.0, 0.0), (10, 0), (0, 6), (-10, 0) })
            Assert.Null(actions.Next(a, b));
        actions.Close();
        var (_, body) = actions.Extrude(5);
        var top = actions.CreateOnFace(body!.Value, (5, 3, 5), (0, 0, 1))!.Value;
        using var live = new SessionSketches(new HostedSession(host));
        live.Apply();
        Assert.Equal(5, live.Live.Single(sketch => sketch.SketchId == top.Sketch).Frame!.Origin.Z);

        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new SetExtrudeDistanceCommand { ExtrudeId = body.Value, Distance = 8 })).Status);
        live.Apply();

        Assert.Equal(8, live.Live.Single(sketch => sketch.SketchId == top.Sketch).Frame!.Origin.Z);
    }
}
