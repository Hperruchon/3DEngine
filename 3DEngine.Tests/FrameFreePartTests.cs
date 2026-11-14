using System.Numerics;
using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Tests;

// ADR-0140 (phase R21c, TASK-0372): a frame in a part of the picture: the target shows at the centre of the part, the box fits
// it, and the projection, the ray of a pick and the gizmo agree on the shift of the centre. With no GPU. ADR-0140 names this
// file in its enforced-by field.
public sealed class FrameFreePartTests
{
    private static readonly ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Box = ((0, 0, 0), (60, 40, 10));

    // A part of the picture right of a panel of about a third of the width, under a top bar and above the anchors.
    private static readonly (double X0, double Y0, double X1, double Y1) Part = (-0.3, -0.8, 0.95, 0.75);

    private const float Aspect = 1280f / 720f;

    private static Camera Framed(FlatView? flat)
    {
        // FrameFill 1: the sphere touches the edge of the part, so a fit to the whole picture puts corners out of it.
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 25, FarPlane = 5000f, FrameFill = 1f };
        if (flat is { } view)
            camera.ShowFlat(view);
        camera.Frame(Box.Min, Box.Max, Aspect, Part);
        return camera;
    }

    private static Vector2 Normalized(Camera camera, (double X, double Y, double Z) point)
    {
        var eye = camera.Eye();
        var clip = Vector4.Transform(new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1f), camera.View() * camera.Projection(Aspect));
        return new Vector2(clip.X / clip.W, clip.Y / clip.W);
    }

    private static IEnumerable<(double X, double Y, double Z)> Corners()
        => from x in new[] { Box.Min.X, Box.Max.X } from y in new[] { Box.Min.Y, Box.Max.Y } from z in new[] { Box.Min.Z, Box.Max.Z } select (x, y, z);

    // Item 1: the target shows at the centre of the part, and each corner of the box is inside the part, with and without
    // perspective.
    [Theory]
    [InlineData(null)]
    [InlineData(FlatView.Front)]
    [InlineData(FlatView.Top)]
    public void A_Frame_In_A_Part_Puts_The_Box_In_It(FlatView? flat)
    {
        var camera = Framed(flat);
        var centre = Normalized(camera, camera.Target);
        Assert.Equal((float)((Part.X0 + Part.X1) / 2), centre.X, 4);
        Assert.Equal((float)((Part.Y0 + Part.Y1) / 2), centre.Y, 4);
        // The sphere around the box, along the right and the up of the screen, is inside the part too.
        var (right, up, _) = camera.Axes();
        var radius = Math.Sqrt((60 * 60) + (40 * 40) + (10 * 10)) / 2;
        var target = camera.Target;
        var sphere = new[] { right, up }.SelectMany(axis => new[] { 1.0, -1.0 }.Select(sign => (target.X + (sign * radius * axis.X), target.Y + (sign * radius * axis.Y), target.Z + (sign * radius * axis.Z))));
        foreach (var corner in Corners().Concat(sphere))
        {
            var at = Normalized(camera, corner);
            Assert.True(at.X >= Part.X0 && at.X <= Part.X1 && at.Y >= Part.Y0 && at.Y <= Part.Y1, $"The corner {corner} is at {at}, out of the part.");
        }
    }

    // Item 1: a frame of the whole picture has no shift and keeps the distance of Frame, as before the part.
    [Fact]
    public void A_Frame_Of_The_Whole_Picture_Has_No_Shift()
    {
        var whole = new Camera { ZUp = true, FarPlane = 5000f, FrameFill = 0.45f };
        whole.Frame(Box.Min, Box.Max, Aspect, Part);
        whole.Frame(Box.Min, Box.Max, Aspect);
        Assert.Equal((0.0, 0.0), whole.Shift);
        var radius = Math.Sqrt((60 * 60) + (40 * 40) + (10 * 10)) / 2;
        Assert.Equal(radius / Math.Sin(Math.Atan(Math.Tan(Math.PI / 6) * whole.FrameFill)), whole.Distance, 9);
    }

    // Item 1: the ray of a pick through the pixel of the target goes through the target, with and without perspective.
    [Theory]
    [InlineData(null)]
    [InlineData(FlatView.Right)]
    public void The_Ray_Through_The_Pixel_Of_The_Target_Goes_Through_It(FlatView? flat)
    {
        var camera = Framed(flat);
        var centre = Normalized(camera, camera.Target);
        var (width, height) = (1280.0, 720.0);
        var ray = Picker.RayThrough(camera, (centre.X + 1) / 2 * width, (1 - centre.Y) / 2 * height, width, height);
        var to = (camera.Target.X - ray.Origin.X, camera.Target.Y - ray.Origin.Y, camera.Target.Z - ray.Origin.Z);
        var along = (to.Item1 * ray.Direction.X) + (to.Item2 * ray.Direction.Y) + (to.Item3 * ray.Direction.Z);
        var off = Math.Sqrt((to.Item1 * to.Item1) + (to.Item2 * to.Item2) + (to.Item3 * to.Item3) - (along * along));
        Assert.True(off < 1e-3, $"The ray passes {off} from the target.");
    }

    // Item 1: the gizmo puts the pivot at the pixel of the projection.
    [Fact]
    public void The_Gizmo_Puts_The_Pivot_Where_The_Projection_Does()
    {
        var camera = Framed(null);
        var (width, height) = (1280.0, 720.0);
        var layout = TranslateGizmo.Layout(camera, width, height, camera.Target) ?? throw new InvalidOperationException("No gizmo.");
        var centre = Normalized(camera, camera.Target);
        Assert.Equal((centre.X + 1) / 2 * width, layout.Origin.X, 2);
        Assert.Equal((1 - centre.Y) / 2 * height, layout.Origin.Y, 2);
    }
}
