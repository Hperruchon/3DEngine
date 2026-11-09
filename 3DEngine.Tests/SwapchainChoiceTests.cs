using ThreeDEngine.Vulkan;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Tests;

// TASK-0319: the choices of the device and of the swapchain for the old findings V2 and V3 of the codebase review of
// 2026-09-23 (R-0022) and V7 of the codebase review of 2026-09-30 (R-0030), with no GPU.
public class SwapchainChoiceTests
{
    private static VkSurfaceCapabilitiesKHR Capabilities(uint width, uint height) => new()
    {
        currentExtent = new VkExtent2D(width, height),
        minImageExtent = new VkExtent2D(1, 1),
        maxImageExtent = new VkExtent2D(4096, 4096),
    };

    // V2: the extent of the surface applies; 0xFFFFFFFF leaves it to the swapchain, which takes the size of the window in
    // pixels inside the limits of the surface. Until TASK-0319 0xFFFFFFFF passed the check "greater than 0".
    [Fact]
    public void The_Swapchain_Takes_The_Pixels_Of_The_Window_Only_For_The_Special_Extent()
    {
        VkExtent2D Pixels() => new(2560, 1600);

        Assert.Equal(new VkExtent2D(1280, 800), Swapchain.ChooseSwapExtent(Capabilities(1280, 800), Pixels));
        Assert.Equal(new VkExtent2D(2560, 1600), Swapchain.ChooseSwapExtent(Capabilities(uint.MaxValue, uint.MaxValue), Pixels));
        Assert.Equal(new VkExtent2D(4096, 4096), Swapchain.ChooseSwapExtent(Capabilities(uint.MaxValue, uint.MaxValue), () => new VkExtent2D(9000, 9000)));
    }

    // V7 part 2: the format B8G8R8A8 in the colour space of sRGB; the same format in another space is not chosen first.
    [Fact]
    public void The_Format_Needs_The_Colour_Space_Of_Srgb()
    {
        VkSurfaceFormatKHR[] formats =
        [
            new(VkFormat.B8G8R8A8Unorm, VkColorSpaceKHR.ExtendedSrgbLinearEXT),
            new(VkFormat.R8G8B8A8Unorm, VkColorSpaceKHR.SrgbNonLinear),
            new(VkFormat.B8G8R8A8Unorm, VkColorSpaceKHR.SrgbNonLinear),
        ];

        Assert.Equal(formats[2], Swapchain.ChooseSwapSurfaceFormat(formats));
    }

    // V7 part 1: one family that draws and presents comes first, also after a family that only draws; else one family of
    // each kind, and then the swapchain shares its images.
    [Fact]
    public void One_Family_That_Draws_And_Presents_Comes_First()
    {
        Assert.Equal((2u, 2u), GraphicsDevice.ChooseQueueFamilies([true, false, true], [false, true, true], surface: true));
        Assert.Equal((0u, 1u), GraphicsDevice.ChooseQueueFamilies([true, false], [false, true], surface: true));
        Assert.Equal((0u, VK_QUEUE_FAMILY_IGNORED), GraphicsDevice.ChooseQueueFamilies([true, false], [false, false], surface: false));
        Assert.Equal((VK_QUEUE_FAMILY_IGNORED, 1u), GraphicsDevice.ChooseQueueFamilies([false, false], [false, true], surface: true));

        Assert.Equal((VkSharingMode.Exclusive, 0u), Swapchain.Sharing(2, 2));
        Assert.Equal((VkSharingMode.Concurrent, 2u), Swapchain.Sharing(0, 1));
    }

    // V3: a device that gives the portability subset enables it, with a window or with none.
    [Fact]
    public void A_Device_That_Gives_The_Portability_Subset_Enables_It()
    {
        Assert.Equal(["VK_KHR_swapchain"], GraphicsDevice.DeviceExtensions(window: true, ["VK_KHR_swapchain"]));
        Assert.Equal(["VK_KHR_swapchain", GraphicsDevice.PortabilitySubsetExtension], GraphicsDevice.DeviceExtensions(window: true, ["VK_KHR_swapchain", GraphicsDevice.PortabilitySubsetExtension]));
        Assert.Equal([GraphicsDevice.PortabilitySubsetExtension], GraphicsDevice.DeviceExtensions(window: false, [GraphicsDevice.PortabilitySubsetExtension]));
    }
}
