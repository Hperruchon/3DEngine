using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// TASK-0461 (finding V234 of the codebase review of 2026-11-23): a start of the ground pipeline (ADR-0186) or of an edge
// pipeline (ADR-0109) that fails at one of its objects frees each object that it made before. The validation layer gives a
// message for each object that a device still holds when it is destroyed, so the count of its messages does not grow. Until
// TASK-0461 a failed module leaked the other module, and a failed pipeline leaked its layout.
[Collection(DearImGui.Collection)]
public sealed class PipelineStartTests
{
    [VulkanImageFact]
    public void A_Failed_Start_Of_A_Pipeline_Frees_Each_Object_That_It_Made()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        foreach (var step in new[] { "vertex", "fragment", "layout", "pipeline" })
        {
            using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null);
            using var target = new OffscreenTarget(device, new VkExtent2D(64, 64));
            void Fail(string at)
            {
                if (at == step)
                    throw new InvalidOperationException($"The test fails the start at {at}.");
            }

            Assert.Throws<InvalidOperationException>(() => new GroundPipeline(device, target.RenderPass, Fail));
            Assert.Throws<InvalidOperationException>(() => new EdgePipeline(device, target.RenderPass, hidden: false, Fail));
            Assert.Throws<InvalidOperationException>(() => new EdgePipeline(device, target.RenderPass, hidden: true, Fail));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }
}
