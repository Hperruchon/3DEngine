using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// The fixed state of the pipelines of R3 (TASK-0069), with no GPU. The viewport,
// the front face and the culled face are one choice: the engine gives each
// triangle counter-clockwise when seen from outside (ADR-0018 item 9) in a space
// with y up, the viewport turns y up, and the pipeline culls the back face. A
// change of one part alone shows the inside of each body.
public class PipelineStateTests
{
    [Fact]
    public void The_Viewport_Turns_Y_Up_Over_The_Whole_Extent()
    {
        var viewport = RenderPipeline.Viewport(new VkExtent2D(1280, 720));

        Assert.Equal(0f, viewport.x);
        Assert.Equal(720f, viewport.y);
        Assert.Equal(1280f, viewport.width);
        Assert.Equal(-720f, viewport.height);
        Assert.Equal(0f, viewport.minDepth);
        Assert.Equal(1f, viewport.maxDepth);
    }

    [Fact]
    public void A_Counter_Clockwise_Triangle_Is_The_Front_Face_And_The_Back_Face_Is_Culled()
    {
        Assert.Equal(VkFrontFace.CounterClockwise, RenderPipeline.FrontFace);
        Assert.Equal(VkCullModeFlags.Back, RenderPipeline.CullMode);
        Assert.Equal(VkPrimitiveTopology.TriangleList, RenderPipeline.Topology);
    }
}
