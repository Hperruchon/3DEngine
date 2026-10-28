using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Tests;

// The pick of the desktop (ADR-0029 items 1 and 3, TASK-0086). ADR-0029 names this file in
// enforced-by. The ray of a pixel agrees with the camera that draws, and the nearest visible
// body under the ray is the one that a select intent selects.
public class PickingTests
{
    private const double Width = 800;
    private const double Height = 600;

    // A box of two triangles for each face, from min to max.
    private static PickBody Box(Guid id, (double X, double Y, double Z) min, (double X, double Y, double Z) max, bool visible = true)
    {
        var positions = new List<double>();
        foreach (var x in new[] { min.X, max.X })
        foreach (var y in new[] { min.Y, max.Y })
        foreach (var z in new[] { min.Z, max.Z })
            positions.AddRange([x, y, z]);

        // The corners are numbered by the bits x, y, z (4, 2, 1).
        uint[] faces = [0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5, 0, 4, 5, 0, 5, 1, 2, 3, 7, 2, 7, 6, 0, 2, 6, 0, 6, 4, 1, 5, 7, 1, 7, 3];
        return new PickBody(id, [.. positions], faces, visible);
    }

    private static Camera Looking() => new() { Target = (0, 0, 0), Distance = 10, YawDegrees = 30, PitchDegrees = 20 };

    [Fact]
    public void The_Ray_Of_The_Centre_Points_From_The_Eye_To_The_Target()
    {
        var camera = Looking();
        var ray = Picker.RayThrough(camera, Width / 2, Height / 2, Width, Height);
        var eye = camera.Eye();

        Assert.Equal(eye, ray.Origin);
        var expected = Vector3.Normalize(new Vector3((float)-eye.X, (float)-eye.Y, (float)-eye.Z));
        Assert.Equal(expected.X, ray.Direction.X, 5);
        Assert.Equal(expected.Y, ray.Direction.Y, 5);
        Assert.Equal(expected.Z, ray.Direction.Z, 5);
    }

    // A point that the camera projects to a pixel lies on the ray of that pixel, so the pick
    // agrees with the picture.
    [Theory]
    [InlineData(2.0, 1.0, -1.5)]
    [InlineData(-3.0, 0.5, 2.0)]
    [InlineData(0.0, -2.0, 0.0)]
    public void The_Ray_Of_A_Pixel_Passes_Through_The_Point_That_The_Camera_Draws_There(double x, double y, double z)
    {
        var camera = Looking();
        var aspect = (float)(Width / Height);
        var eye = camera.Eye();
        var clip = Vector4.Transform(new Vector4((float)(x - eye.X), (float)(y - eye.Y), (float)(z - eye.Z), 1f), camera.View() * camera.Projection(aspect));

        // The viewport of the renderer has y up in clip space (ADR-0022), and a sample has y down.
        var pixelX = (clip.X / clip.W + 1) / 2 * Width;
        var pixelY = (1 - clip.Y / clip.W) / 2 * Height;
        var ray = Picker.RayThrough(camera, pixelX, pixelY, Width, Height);

        var toPoint = (x - ray.Origin.X, y - ray.Origin.Y, z - ray.Origin.Z);
        var along = toPoint.Item1 * ray.Direction.X + toPoint.Item2 * ray.Direction.Y + toPoint.Item3 * ray.Direction.Z;
        var closest = (ray.Origin.X + ray.Direction.X * along, ray.Origin.Y + ray.Direction.Y * along, ray.Origin.Z + ray.Direction.Z * along);
        var miss = Math.Sqrt(Math.Pow(closest.Item1 - x, 2) + Math.Pow(closest.Item2 - y, 2) + Math.Pow(closest.Item3 - z, 2));
        Assert.True(miss < 1e-4, $"The ray passes {miss} from the point.");
    }

    [Fact]
    public void The_Nearest_Of_Two_Bodies_On_The_Ray_Is_Picked()
    {
        Guid near = Guid.NewGuid(), far = Guid.NewGuid();
        var ray = new Ray((0, 0, 10), (0, 0, -1));

        var hit = Picker.Nearest(ray, [Box(far, (-1, -1, -6), (1, 1, -4)), Box(near, (-1, -1, -1), (1, 1, 1))]);

        Assert.Equal(near, hit!.Value.Id);
        Assert.Equal(9.0, hit.Value.Distance, 9);
    }

    // ADR-0029 item 3: a hidden body cannot be picked, so the body behind it is.
    [Fact]
    public void A_Hidden_Body_Is_Not_Picked()
    {
        Guid hidden = Guid.NewGuid(), behind = Guid.NewGuid();
        var ray = new Ray((0, 0, 10), (0, 0, -1));

        var hit = Picker.Nearest(ray, [Box(hidden, (-1, -1, -1), (1, 1, 1), visible: false), Box(behind, (-1, -1, -6), (1, 1, -4))]);

        Assert.Equal(behind, hit!.Value.Id);
    }

    [Fact]
    public void A_Ray_That_Misses_Each_Body_Or_Points_Away_Picks_Nothing()
    {
        var box = Box(Guid.NewGuid(), (-1, -1, -1), (1, 1, 1));

        Assert.Null(Picker.Nearest(new Ray((5, 0, 10), (0, 0, -1)), [box]));
        Assert.Null(Picker.Nearest(new Ray((0, 0, 10), (0, 0, 1)), [box]));
    }

    // A pixel over the body that the camera frames picks it, and a corner of the picture does not.
    [Fact]
    public void A_Pixel_Of_The_Picture_Picks_The_Body_Under_It()
    {
        var camera = Looking();
        var id = Guid.NewGuid();
        var box = Box(id, (-1, -1, -1), (1, 1, 1));

        Assert.Equal(id, Picker.Nearest(Picker.RayThrough(camera, Width / 2, Height / 2, Width, Height), [box])!.Value.Id);
        Assert.Null(Picker.Nearest(Picker.RayThrough(camera, 5, 5, Width, Height), [box]));
    }
}
