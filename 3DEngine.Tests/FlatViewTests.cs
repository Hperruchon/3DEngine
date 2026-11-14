using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Tests;

// ADR-0137 (phase R21a, TASK-0369): the flat views of the camera: exact axes along the world, a projection with no
// perspective at the scale of the target, the return to the view with perspective, and a pick along parallel rays. With no
// GPU. ADR-0137 names this file in its enforced-by field.
public sealed class FlatViewTests
{
    private static Camera Camera() => new() { ZUp = true, Target = (10, 20, 30), Distance = 200, YawDegrees = 30, PitchDegrees = 25, FarPlane = 5000f };

    // The point of the picture in normalized coordinates, as the renderer gives it: relative to the eye, through the view and
    // the projection, divided by w.
    private static Vector2 Normalized(Camera camera, (double X, double Y, double Z) point, float aspect = 1.5f)
    {
        var eye = camera.Eye();
        var clip = Vector4.Transform(new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1f), camera.View() * camera.Projection(aspect));
        return new Vector2(clip.X / clip.W, clip.Y / clip.W);
    }

    // Item 1: each flat view looks along an axis of the world, with z up; Top looks down with the y axis up on the screen, and
    // the eye stays at the distance from the target.
    [Theory]
    [InlineData(FlatView.Front, 0, 1, 0, 0, 0, 1)]
    [InlineData(FlatView.Back, 0, -1, 0, 0, 0, 1)]
    [InlineData(FlatView.Right, -1, 0, 0, 0, 0, 1)]
    [InlineData(FlatView.Left, 1, 0, 0, 0, 0, 1)]
    [InlineData(FlatView.Top, 0, 0, -1, 0, 1, 0)]
    [InlineData(FlatView.Bottom, 0, 0, 1, 0, -1, 0)]
    public void A_Flat_View_Looks_Along_An_Axis(FlatView view, double fx, double fy, double fz, double ux, double uy, double uz)
    {
        var camera = Camera();
        camera.ShowFlat(view);

        var (_, up, forward) = camera.Axes();
        Assert.Equal(view, camera.Flat);
        Assert.Equal((fx, fy, fz), (Math.Round(forward.X, 12), Math.Round(forward.Y, 12), Math.Round(forward.Z, 12)));
        Assert.Equal((ux, uy, uz), (Math.Round(up.X, 12), Math.Round(up.Y, 12), Math.Round(up.Z, 12)));
        var eye = camera.Eye();
        Assert.Equal(200, Math.Sqrt(Math.Pow(eye.X - 10, 2) + Math.Pow(eye.Y - 20, 2) + Math.Pow(eye.Z - 30, 2)), 9);
        Assert.True(Matrix4x4.Invert(camera.View(), out _), $"The view of {view} has no inverse.");
    }

    // Item 3: a flat view has no perspective: a point and the same point 150 units farther along the view are at one place
    // of the picture, and a point in the plane of the target is where the view with perspective puts it.
    [Fact]
    public void A_Flat_View_Has_No_Perspective_And_Keeps_The_Scale_At_The_Target()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Front);
        var near = Normalized(camera, (40, 20, 50));
        var far = Normalized(camera, (40, 170, 50));
        Assert.True(Vector2.Distance(near, far) < 1e-5f, $"{near} and {far} differ.");

        var flat = Normalized(camera, (40, 20, 50));
        camera.ShowPerspective();
        camera.YawDegrees = 0;
        camera.PitchDegrees = 0;
        var perspective = Normalized(camera, (40, 20, 50));
        Assert.True(Vector2.Distance(flat, perspective) < 1e-5f, $"The flat view puts the point at {flat}, the perspective at {perspective}.");
    }

    // Item 3: the depth of a flat view is reversed, as with perspective: nearer is larger, from 1 at the near plane to 0 at the
    // far plane.
    [Fact]
    public void The_Depth_Of_A_Flat_View_Is_Reversed()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        var eye = camera.Eye();
        float Depth(double below)
        {
            var clip = Vector4.Transform(new Vector4(0f, 0f, (float)-below, 1f), camera.View() * camera.Projection(1f));
            return clip.Z / clip.W;
        }

        Assert.Equal(1f, Depth(camera.NearPlane), 4);
        Assert.Equal(0f, Depth(camera.FarPlane), 4);
        Assert.True(Depth(100) > Depth(300), "A nearer point is not deeper in the reversed depth.");
        Assert.Equal(230, eye.Z, 9);
    }

    // Item 2: the return gives back the yaw and the pitch before the first flat view, and an orbit leaves a flat view.
    [Fact]
    public void The_Return_Gives_Back_The_View_Before_And_An_Orbit_Leaves_A_Flat_View()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        camera.ShowFlat(FlatView.Right);
        camera.ShowPerspective();
        Assert.Null(camera.Flat);
        Assert.Equal((30.0, 25.0), (camera.YawDegrees, camera.PitchDegrees));

        camera.ShowFlat(FlatView.Front);
        camera.Orbit(10, 5);
        Assert.Null(camera.Flat);
        Assert.Equal((10.0, 5.0), (camera.YawDegrees, camera.PitchDegrees));
    }

    // Item 3: in a flat view each ray of a pick goes along the view; the ray of the centre goes through the target, and a ray
    // of another pixel is parallel to it, offset at the scale of the target.
    [Fact]
    public void In_A_Flat_View_The_Rays_Of_A_Pick_Are_Parallel()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        var centre = Picker.RayThrough(camera, 300, 200, 600, 400);
        var corner = Picker.RayThrough(camera, 0, 0, 600, 400);
        Assert.Equal((0.0, 0.0, -1.0), (Math.Round(centre.Direction.X, 12), Math.Round(centre.Direction.Y, 12), Math.Round(centre.Direction.Z, 12)));
        Assert.Equal(centre.Direction, corner.Direction);
        Assert.Equal((10.0, 20.0), (Math.Round(centre.Origin.X, 9), Math.Round(centre.Origin.Y, 9)));
        var half = camera.HalfHeight;
        Assert.Equal(10 - (half * 1.5), corner.Origin.X, 9);
        Assert.Equal(20 + half, corner.Origin.Y, 9);
    }

    // Item 3: a pan in the view from the top moves the target in the plane of x and y only.
    [Fact]
    public void A_Pan_In_The_View_From_The_Top_Keeps_The_Height()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        camera.Pan(40, -25, 400);
        Assert.Equal(30, camera.Target.Z, 9);
        Assert.NotEqual(10, camera.Target.X);
        Assert.NotEqual(20, camera.Target.Y);
    }

    // Item 3: in a flat view the gizmo has the same length in the world at each depth, the length at the target.
    [Fact]
    public void In_A_Flat_View_The_Gizmo_Has_One_Length_At_Each_Depth()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        var high = ThreeDEngine.Core.Gizmo.TranslateGizmo.WorldLength(camera, 600, 400, (10, 20, 180)) ?? throw new InvalidOperationException("No length.");
        var low = ThreeDEngine.Core.Gizmo.TranslateGizmo.WorldLength(camera, 600, 400, (10, 20, -100)) ?? throw new InvalidOperationException("No length.");
        Assert.Equal(high, low, 9);
    }

    // Finding V185 of the codebase review of 2026-11-14 (d) (TASK-0373): a sketch that starts in a flat view leaves it, so the view
    // of the sketch looks at its plane: from the top view, a sketch on the plane of x and z takes a point at the centre of the
    // picture; from the front view, a sketch on the plane of x and y gives no flat view "Front" that looks down.
    [Fact]
    public void A_Sketch_Leaves_A_Flat_View()
    {
        var camera = Camera();
        camera.ShowFlat(FlatView.Top);
        ThreeDEngine.Core.Sketching.SketchCamera.Face(camera, (0, 1, 0), (0, 0, 0));
        Assert.Null(camera.Flat);
        var ray = Picker.RayThrough(camera, 300, 200, 600, 400);
        Assert.NotNull(Picker.OnPlane(ray, (0, 0, 0), (1, 0, 0), (0, 0, 1)));

        camera.ShowFlat(FlatView.Front);
        ThreeDEngine.Core.Sketching.SketchCamera.Face(camera, (0, 0, 1), (0, 0, 0));
        Assert.Null(camera.Flat);
        Assert.True(camera.PitchDegrees > 80, $"The view of the sketch has a pitch of {camera.PitchDegrees}.");
    }
}
