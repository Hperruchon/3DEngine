using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// Finding V20 of the codebase review of 2026-10-17 (TASK-0079): the swapchain asked for
// the opaque composite alpha and did not read the modes that the surface gives. Some
// surfaces on Linux do not give opaque (register entry R-0022).
public class CompositeAlphaTests
{
    [Fact]
    public void Opaque_Is_Chosen_When_The_Surface_Gives_It()
        => Assert.Equal(
            VkCompositeAlphaFlagsKHR.Opaque,
            Swapchain.ChooseCompositeAlpha(VkCompositeAlphaFlagsKHR.Opaque | VkCompositeAlphaFlagsKHR.PreMultiplied));

    [Theory]
    [InlineData(VkCompositeAlphaFlagsKHR.Inherit, VkCompositeAlphaFlagsKHR.Inherit)]
    [InlineData(VkCompositeAlphaFlagsKHR.PreMultiplied | VkCompositeAlphaFlagsKHR.PostMultiplied, VkCompositeAlphaFlagsKHR.PreMultiplied)]
    [InlineData(VkCompositeAlphaFlagsKHR.PostMultiplied, VkCompositeAlphaFlagsKHR.PostMultiplied)]
    public void A_Surface_With_No_Opaque_Mode_Gets_A_Mode_That_It_Gives(VkCompositeAlphaFlagsKHR supported, VkCompositeAlphaFlagsKHR expected)
        => Assert.Equal(expected, Swapchain.ChooseCompositeAlpha(supported));

    [Fact]
    public void A_Surface_With_No_Mode_Is_Refused()
    {
        var refusal = Assert.Throws<InvalidOperationException>(() => Swapchain.ChooseCompositeAlpha(VkCompositeAlphaFlagsKHR.None));
        Assert.Contains("no composite alpha mode", refusal.Message, StringComparison.Ordinal);
    }
}
