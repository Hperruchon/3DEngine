using Engine.Api.Http;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Rendering;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// Findings V86 and V87 of the codebase review of 2026-11-05 (b) (TASK-0191): the far plane holds the live images, and a
// drag of the gizmo queries the size of the selected body one time.
public class TwentySecondReviewDesktopTests
{
    // An image of 4000 by 3000 units on the plane XY, and a camera that looks at its centre from 20000 units away.
    private static readonly ImageQuad Photo = new("photo", (0, 0, 0), (4000, 0, 0), (0, 3000, 0));

    private static Camera Far() => new() { Target = (2000, 1500, 0), Distance = 20000, YawDegrees = 0, PitchDegrees = 0 };

    private static double Farthest(Camera camera)
    {
        var eye = camera.Eye();
        return new (double X, double Y, double Z)[] { (0, 0, 0), (4000, 0, 0), (0, 3000, 0), (4000, 3000, 0) }
            .Max(corner => Math.Sqrt((corner.X - eye.X) * (corner.X - eye.X) + (corner.Y - eye.Y) * (corner.Y - eye.Y) + (corner.Z - eye.Z) * (corner.Z - eye.Z)));
    }

    // V86: the box of the scene holds the four corners of an image, also with no body, and the far plane of that box holds
    // the farthest corner; the default far plane does not.
    [Fact]
    public void The_Box_Of_The_Scene_Holds_An_Image_With_No_Body()
    {
        var box = SceneRenderer.SceneBox(null, [Photo]);

        Assert.Equal(((0.0, 0.0, 0.0), (4000.0, 3000.0, 0.0)), box);
        var camera = Far();
        Assert.True(Camera.DefaultFarPlane < Farthest(camera));
        Assert.True(camera.FarPlaneFor(box!.Value.Min, box.Value.Max) >= Farthest(camera));
        Assert.Equal(((-5.0, 0.0, 0.0), (4000.0, 3000.0, 7.0)), SceneRenderer.SceneBox(((-5, 1, 2), (3, 4, 7)), [Photo]));
        Assert.Null(SceneRenderer.SceneBox(null, []));
    }

    // V86: Update of the renderer with no body and the image sets the far plane that holds the image.
    [VulkanImageFact]
    public void Update_With_An_Image_And_No_Body_Sets_A_Far_Plane_That_Holds_It()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var scene = SessionScene.Create();
        var camera = Far();
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true);
        using var target = new OffscreenTarget(device, new VkExtent2D(64, 64));
        using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));

        renderer.Update(scene, camera, 1f, [Photo]);

        Assert.True(camera.FarPlane >= Farthest(camera), $"The far plane {camera.FarPlane} is nearer than {Farthest(camera)}.");
    }

    // V87: a drag keeps the box with no offset of the selected body, so three frames of the drag query its size one time.
    // A new solid or another body queries again.
    [Fact]
    public void A_Drag_Queries_The_Size_Of_The_Selected_Body_One_Time()
    {
        var body = Guid.NewGuid();
        var other = Guid.NewGuid();
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) still = ((0, 0, 0), (10, 10, 10));
        var queries = 0;
        double? Query(Guid id) { queries++; return 4; }

        (Guid Body, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Still, double? Distance)? kept = null;
        for (var frame = 0; frame < 3; frame++)
            kept = NativeThreeDEngine.SizeOf(kept, body, still, Query);
        Assert.Equal(1, queries);
        Assert.Equal(4, kept!.Value.Distance);

        kept = NativeThreeDEngine.SizeOf(kept, body, ((0, 0, 0), (10, 10, 20)), Query);
        kept = NativeThreeDEngine.SizeOf(kept, other, ((0, 0, 0), (10, 10, 20)), Query);
        Assert.Equal(3, queries);
    }
}
