using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// TASK-0461 (finding V234 of the codebase review of 2026-11-23): a start of the ground pipeline (ADR-0186) or of an edge
// pipeline (ADR-0109) that fails at one of its objects frees each object that it made before. The validation layer gives a
// message for each object that a device still holds when it is destroyed, so the count of its messages does not grow. Until
// TASK-0461 a failed module leaked the other module, and a failed pipeline leaked its layout. TASK-0473 (finding V236 of the
// codebase review of 2026-11-25) adds the three pipelines of the scene (`SceneRenderer`) and the overlay, with the same form.
[Collection(DearImGui.Collection)]
public sealed class PipelineStartTests
{
    // Finding T203 of the codebase review of 2026-11-28 (TASK-0477): the device turns the validation on only when the layer is
    // installed, and each image test that counts the messages of the layer passes with no check when it is absent. This test
    // fails on a computer with no layer, so a green run of the image tests means that the layer checked them.
    [VulkanImageFact]
    public void The_Validation_Layer_Is_On_For_The_Image_Tests()
    {
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
        Assert.True(device.ValidationEnabled, "The Vulkan validation layer is not installed, so the image tests that count its messages check nothing.");
    }

    [VulkanImageFact]
    public void A_Failed_Start_Of_A_Pipeline_Frees_Each_Object_That_It_Made()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        foreach (var step in new[] { "vertex", "fragment", "layout", "pipeline" })
        {
            using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
            Assert.True(device.ValidationEnabled, "The test counts the messages of the validation layer, which is not installed.");
            using var target = new OffscreenTarget(device, new VkExtent2D(64, 64));
            void Fail(string at)
            {
                if (at == step)
                    throw new InvalidOperationException($"The test fails the start at {at}.");
            }

            Assert.Throws<InvalidOperationException>(() => new GroundPipeline(device, target.RenderPass, Fail));
            Assert.Throws<InvalidOperationException>(() => new EdgePipeline(device, target.RenderPass, hidden: false, Fail));
            Assert.Throws<InvalidOperationException>(() => new EdgePipeline(device, target.RenderPass, hidden: true, Fail));
            Assert.Throws<InvalidOperationException>(() => new RenderPipeline(device, target.RenderPass, "mesh.vert", "mesh.frag", highlight: false, depthOnly: false, Fail));
            Assert.Throws<InvalidOperationException>(() => new RenderPipeline(device, target.RenderPass, "mesh.vert", "highlight.frag", highlight: true, depthOnly: false, Fail));
            Assert.Throws<InvalidOperationException>(() => new RenderPipeline(device, target.RenderPass, "mesh.vert", "mesh.frag", highlight: false, depthOnly: true, Fail));
            Assert.Throws<InvalidOperationException>(() => new OverlayRenderer(device, target.RenderPass, Fail));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }

    // Register entry R-0077 (TASK-0480): the start of the renderer of the images and of the user interface fails at each object
    // that it makes, and each failure frees the objects before it, so the validation layer gives no message when the device ends.
    // Until TASK-0480 a failure after the set layout left the set layout, the pool, the sampler and the buffer of the square.
    [VulkanImageFact]
    public void A_Failed_Start_Of_A_Renderer_Of_The_Images_Or_Of_The_Interface_Frees_Each_Object_That_It_Made()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        string[] steps = ["set layout", "pool", "sampler", "square", "vertex", "fragment", "layout", "pipeline"];
        foreach (var step in steps)
        {
            using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
            Assert.True(device.ValidationEnabled, "The test counts the messages of the validation layer, which is not installed.");
            using var target = new OffscreenTarget(device, new VkExtent2D(64, 64));
            void Fail(string at)
            {
                if (at == step)
                    throw new InvalidOperationException($"The test fails the start at {at}.");
            }

            Assert.Throws<InvalidOperationException>(() => new ImageRenderer(device, target.RenderPass, Fail));
            if (step != "square")
                Assert.Throws<InvalidOperationException>(() => new UiRenderer(device, target.RenderPass, Fail));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }
}
