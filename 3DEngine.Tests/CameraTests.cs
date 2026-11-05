using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Tests;

// The orbit camera and its reversed projection (ADR-0023 items 1 and 2,
// TASK-0070), with no GPU. ADR-0023 names this file in enforced-by.
public class CameraTests
{
    [Fact]
    public void With_No_Turn_The_Eye_Is_On_The_Positive_Z_Side_Of_The_Target()
    {
        var camera = new Camera { Target = (1, 2, 3), Distance = 10 };

        var eye = camera.Eye();

        Assert.Equal(1, eye.X, 12);
        Assert.Equal(2, eye.Y, 12);
        Assert.Equal(13, eye.Z, 12);
    }

    [Fact]
    public void A_Yaw_Of_90_Degrees_Puts_The_Eye_On_The_Positive_X_Side()
    {
        var camera = new Camera { Distance = 10 };

        camera.Orbit(90, 0);
        var eye = camera.Eye();

        Assert.Equal(10, eye.X, 9);
        Assert.Equal(0, eye.Z, 9);
    }

    [Fact]
    public void The_Pitch_Stays_Between_Minus_89_And_89_Degrees()
    {
        var camera = new Camera();

        camera.Orbit(0, 200);
        Assert.Equal(Camera.MaximumPitchDegrees, camera.PitchDegrees);
        camera.Orbit(0, -500);
        Assert.Equal(Camera.MinimumPitchDegrees, camera.PitchDegrees);
    }

    [Fact]
    public void A_Zoom_Keeps_The_Eye_Between_The_Planes_And_Refuses_A_Factor_That_Is_Not_Positive()
    {
        var camera = new Camera { NearPlane = 0.1f, FarPlane = 1000f, Distance = 10 };

        camera.Zoom(1e-6);
        Assert.Equal(0.2, camera.Distance, 6);
        camera.Zoom(1e9);
        Assert.Equal(500, camera.Distance, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom(0));
    }

    [Fact]
    public void The_View_Puts_The_Target_On_The_Negative_Z_Axis_At_The_Distance()
    {
        var camera = new Camera { Target = (5, -3, 2), Distance = 12 };
        camera.Orbit(37, 21);
        var eye = camera.Eye();
        var target = new Vector3((float)(5 - eye.X), (float)(-3 - eye.Y), (float)(2 - eye.Z));

        var inView = Vector3.Transform(target, camera.View());

        Assert.Equal(0, inView.X, 4);
        Assert.Equal(0, inView.Y, 4);
        Assert.Equal(-12, inView.Z, 4);
    }

    // Reversed depth: the near plane gives 1, the far plane gives 0, and a nearer
    // point gives a greater depth, which the pipeline keeps.
    [Fact]
    public void The_Projection_Gives_Depth_1_At_The_Near_Plane_And_0_At_The_Far_Plane()
    {
        var camera = new Camera { NearPlane = 0.1f, FarPlane = 1000f };
        var projection = camera.Projection(16f / 9f);

        float Depth(float z)
        {
            var clip = Vector4.Transform(new Vector4(0, 0, z, 1), projection);
            return clip.Z / clip.W;
        }

        Assert.Equal(1f, Depth(-0.1f), 5);
        Assert.Equal(0f, Depth(-1000f), 5);
        Assert.True(Depth(-1f) > Depth(-10f));
    }

    [Fact]
    public void The_Projection_Narrows_X_By_The_Aspect()
    {
        var camera = new Camera { FieldOfViewDegrees = 90f };

        var projection = camera.Projection(2f);

        Assert.Equal(1f, projection.M22, 5);
        Assert.Equal(0.5f, projection.M11, 5);
    }

    // ADR-0025 item 6 (TASK-0076): each corner of a framed box is in the picture, for a
    // wide picture and for a tall one, and the yaw and the pitch do not change.
    [Theory]
    [InlineData(16f / 9f)]
    [InlineData(9f / 16f)]
    public void A_Framed_Box_Is_Inside_The_Picture(float aspect)
    {
        var camera = new Camera { YawDegrees = 30, PitchDegrees = 20, Distance = 500 };
        (double X, double Y, double Z) min = (95, -5, -5), max = (105, 5, 5);

        Assert.True(camera.Frame(min, max, aspect));

        Assert.Equal((100.0, 0.0, 0.0), camera.Target);
        Assert.Equal(30, camera.YawDegrees);
        Assert.Equal(20, camera.PitchDegrees);

        var eye = camera.Eye();
        var viewProjection = camera.View() * camera.Projection(aspect);
        var largest = 0f;
        foreach (var x in new[] { min.X, max.X })
        foreach (var y in new[] { min.Y, max.Y })
        foreach (var z in new[] { min.Z, max.Z })
        {
            var clip = Vector4.Transform(new Vector4((float)(x - eye.X), (float)(y - eye.Y), (float)(z - eye.Z), 1f), viewProjection);
            largest = Math.Max(largest, Math.Max(Math.Abs(clip.X / clip.W), Math.Abs(clip.Y / clip.W)));
        }

        // Inside the picture, and not much smaller than it: the sphere around the box
        // touches the narrower edge.
        Assert.InRange(largest, 0.5f, 1f);
    }

    // Finding V11 of the codebase review of 2026-10-13 (TASK-0074): each parameter of the
    // camera is checked, and a zoom between close planes does not throw.
    [Fact]
    public void The_Camera_Refuses_A_Parameter_That_Draws_Nothing()
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.NearPlane = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FarPlane = float.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FieldOfViewDegrees = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FieldOfViewDegrees = 180f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Projection(0f));

        camera.NearPlane = 10f;
        camera.FarPlane = 5f;
        Assert.Throws<InvalidOperationException>(() => camera.Projection(1f));
    }

    // Finding V22 of the codebase review of 2026-10-17 (TASK-0079): a distance of 0 gave a
    // view of NaN, and an aspect of NaN passed the check of Frame and gave a distance of NaN.
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void The_Camera_Refuses_A_Distance_That_Draws_Nothing(double distance)
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Distance = distance);
        Assert.Equal(10.0, camera.Distance);
    }

    [Fact]
    public void The_Camera_Refuses_A_Zoom_Or_An_Aspect_Of_NaN()
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Frame((0, 0, 0), (1, 1, 1), float.NaN));
        Assert.True(double.IsFinite(camera.Distance));
    }

    // V38 of the second codebase review of 2026-10-28: the yaw and the target accepted NaN, and
    // each view was NaN.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void The_Camera_Keeps_A_Finite_Yaw_And_Target(double value)
    {
        var set = new Camera { YawDegrees = value };
        var orbited = new Camera { YawDegrees = 30 };
        orbited.Orbit(value, 0);
        var targeted = new Camera { Target = (1, 2, 3) };

        Assert.Throws<ArgumentOutOfRangeException>(() => targeted.Target = (value, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => targeted.Target = (0, 0, value));
        Assert.Equal((1.0, 2.0, 3.0), targeted.Target);
        foreach (var camera in new[] { set, orbited, targeted })
        {
            var view = camera.View();
            Assert.True(float.IsFinite(view.M11) && float.IsFinite(view.M13) && float.IsFinite(view.M41) && float.IsFinite(view.M43), $"The view is {view}.");
        }
    }

    // V22: with a near plane of 1 and a far plane of 1.5 the distance was 2, beyond the far
    // plane; a box of 3,000 units put the eye inside the body. Frame now says so.
    [Theory]
    [InlineData(1f, 1.5f, 1.0)]
    [InlineData(0.1f, 1000f, 3000.0)]
    public void Frame_Reports_A_Box_That_The_Planes_Cannot_Hold(float near, float far, double size)
    {
        var camera = new Camera { NearPlane = near, FarPlane = far };

        Assert.False(camera.Frame((0, 0, 0), (size, size, size), 16f / 9f));
    }

    // Register entry R-0051 (TASK-0179): the default far plane cannot hold an L-shaped shelf of 1900 mm, and the far
    // plane for its box frames it whole, for a wide, a square and a narrow window, and also far from the origin: each
    // corner is in the picture and in front of the far plane. The shelf lies on its side, 1900 mm along X, and the camera
    // looks along −Z, so the long side is across the narrow way of a window of the aspect 0.3.
    [Theory]
    [InlineData(16f / 9f, 0.0)]
    [InlineData(1f, 0.0)]
    [InlineData(0.3f, 0.0)]
    [InlineData(16f / 9f, 25000.0)]
    public void The_Far_Plane_For_A_Box_Frames_A_Shelf_Of_1900_Mm_Whole(float aspect, double offset)
    {
        (double X, double Y, double Z) min = (offset - 950, -325, -609);
        (double X, double Y, double Z) max = (offset + 950, 325, 609);
        Assert.False(new Camera().Frame(min, max, aspect));

        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0 };
        camera.FarPlane = camera.FarPlaneFor(min, max);

        Assert.True(camera.Frame(min, max, aspect), $"The far plane {camera.FarPlane} does not hold the box at the distance {camera.Distance}.");
        var eye = camera.Eye();
        var viewProjection = camera.View() * camera.Projection(aspect);
        foreach (var x in new[] { min.X, max.X })
        foreach (var y in new[] { min.Y, max.Y })
        foreach (var z in new[] { min.Z, max.Z })
        {
            var clip = Vector4.Transform(new Vector4((float)(x - eye.X), (float)(y - eye.Y), (float)(z - eye.Z), 1f), viewProjection);
            var (nx, ny, depth) = (clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
            Assert.True(Math.Abs(nx) <= 1 && Math.Abs(ny) <= 1 && depth > 0 && depth < 1, $"The corner ({x}, {y}, {z}) is at ({nx}, {ny}, {depth}).");
        }
    }

    // R-0051: the far plane of a small box is the default, and it is never less than two times the distance of the eye.
    // V78 (TASK-0181): a small box at the target gives two times the distance from the eye to its far side.
    [Fact]
    public void The_Far_Plane_For_A_Box_Keeps_The_Default_And_The_View()
    {
        var camera = new Camera { Target = (0, 0, 0), Distance = 900 };
        var radius = Math.Sqrt(10 * 10 + 6 * 6 + 4 * 4) / 2.0;

        Assert.Equal(Camera.DefaultFarPlane, new Camera().FarPlaneFor((0, 0, 0), (10, 6, 4)));
        Assert.Equal(2 * (900 + radius), camera.FarPlaneFor((-5, -3, -2), (5, 3, 2)), 2);
        Assert.True(camera.FarPlaneFor((-5, -3, -2), (5, 3, 2)) >= 1800f);
        Assert.Equal(float.MaxValue, camera.FarPlaneFor((0, 0, 0), (double.MaxValue, 1, 1)));
    }

    // Finding V78 of the codebase review of 2026-11-05 (TASK-0181): a box A of 100 at the origin and a box B of 100 at
    // x = 5000. The camera frames A and turns to B, and then A is hidden, so the far plane comes from the box of B alone,
    // with the target outside it. Up to v0.182 the far plane was 1385.6 and B lay beyond it; now the far side of B is in
    // front of the far plane.
    [Fact]
    public void The_Far_Plane_Holds_A_Box_Far_From_The_Target()
    {
        (double X, double Y, double Z) aMin = (-50, -50, -50), aMax = (50, 50, 50);
        (double X, double Y, double Z) bMin = (4950, -50, -50), bMax = (5050, 50, 50);
        var camera = new Camera();
        camera.FarPlane = camera.FarPlaneFor((-50, -50, -50), (5050, 50, 50));
        Assert.True(camera.Frame(aMin, aMax, 16f / 9f));
        camera.YawDegrees = -90;
        camera.PitchDegrees = 0;

        camera.FarPlane = camera.FarPlaneFor(bMin, bMax);

        var eye = camera.Eye();
        var farSide = Math.Sqrt(Math.Pow(5000 - eye.X, 2) + eye.Y * eye.Y + eye.Z * eye.Z) + Math.Sqrt(3 * 100 * 100) / 2;
        Assert.True(farSide < camera.FarPlane, $"The far side of B is {farSide} from the eye, and the far plane is {camera.FarPlane}.");
        var viewProjection = camera.View() * camera.Projection(16f / 9f);
        var clip = Vector4.Transform(new Vector4((float)(5000 - eye.X), (float)-eye.Y, (float)-eye.Z, 1f), viewProjection);
        Assert.True(clip.Z / clip.W is > 0 and < 1, $"The centre of B has the depth {clip.Z / clip.W}.");
    }

    [Fact]
    public void A_Zoom_Between_Close_Planes_Keeps_The_Eye_At_Two_Times_The_Near_Plane()
    {
        var camera = new Camera { NearPlane = 1f, FarPlane = 3f, Distance = 2 };

        camera.Zoom(0.5);

        Assert.Equal(2.0, camera.Distance);
    }

    // ADR-0026 (TASK-0082): a pan moves the target and the eye together, in the plane of
    // the view. A drag to the right moves the scene to the right, so the target moves to the
    // left; the distance, the yaw and the pitch do not change.
    [Fact]
    public void A_Pan_Moves_The_Target_In_The_Plane_Of_The_View()
    {
        var camera = new Camera { Target = (1, 2, 3), Distance = 10, YawDegrees = 0, PitchDegrees = 0, FieldOfViewDegrees = 90f };
        var eyeBefore = camera.Eye();

        camera.Pan(50, 0, 100);

        // One pixel is 2 * 10 * tan(45 degrees) / 100 = 0.2 at the depth of the target.
        Assert.Equal(1 - 10, camera.Target.X, 9);
        Assert.Equal(2, camera.Target.Y, 9);
        Assert.Equal(3, camera.Target.Z, 9);
        var eyeAfter = camera.Eye();
        Assert.Equal(eyeBefore.X - 10, eyeAfter.X, 9);
        Assert.Equal(eyeBefore.Z, eyeAfter.Z, 9);
        Assert.Equal((10.0, 0.0, 0.0), (camera.Distance, camera.YawDegrees, camera.PitchDegrees));

        camera.Pan(0, 50, 100);
        Assert.Equal(2 + 10, camera.Target.Y, 9);
    }
}
