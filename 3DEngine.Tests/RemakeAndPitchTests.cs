using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// Findings V18 and V30 of the codebase review of 2026-10-28 (TASK-0090), and V39 of the second
// codebase review of that date (TASK-0099), with no GPU.
public class RemakeAndPitchTests
{
    // V39: a constructor that failed after the swapchain existed leaked the handle, the depth
    // image and the views. The release frees each object that exists, and no other.
    [Fact]
    public void The_Release_Of_A_Swapchain_Frees_Each_Object_That_Exists()
    {
        var freed = new List<string>();

        Swapchain.Release(
            [new VkFramebuffer(7), VkFramebuffer.Null],
            [new VkImageView(5), new VkImageView(6), VkImageView.Null],
            new VkSwapchainKHR(42),
            framebuffer => freed.Add($"framebuffer {framebuffer.Handle}"),
            view => freed.Add($"view {view.Handle}"),
            () => freed.Add("depth"),
            swapchain => freed.Add($"swapchain {swapchain.Handle}"));
        Swapchain.Release([], [], VkSwapchainKHR.Null, _ => freed.Add("no framebuffer"), _ => freed.Add("no view"), () => freed.Add("depth of a failed creation"), _ => freed.Add("no swapchain"));

        Assert.Equal(["framebuffer 7", "view 5", "view 6", "depth", "swapchain 42", "depth of a failed creation"], freed);
    }

    // V18: after a failed remake the driver retired the old swapchain, so a retry gives none.
    [Fact]
    public void A_Retry_After_A_Failed_Remake_Gives_No_Old_Swapchain()
    {
        var old = new VkSwapchainKHR(42);

        Assert.Equal(old, Presenter.OldSwapchain(remakeFailed: false, old));
        Assert.Equal(VkSwapchainKHR.Null, Presenter.OldSwapchain(remakeFailed: true, old));
    }

    // V30: a pitch outside the range of Orbit is clamped, so the view is never NaN.
    [Theory]
    [InlineData(90.0, 89.0)]
    [InlineData(-90.0, -89.0)]
    [InlineData(double.NaN, 0.0)]
    [InlineData(45.0, 45.0)]
    public void A_Pitch_Outside_The_Range_Of_Orbit_Is_Clamped(double set, double expected)
    {
        var camera = new Camera { PitchDegrees = set };

        Assert.Equal(expected, camera.PitchDegrees);
        var view = camera.View();
        Assert.True(float.IsFinite(view.M11) && float.IsFinite(view.M22) && float.IsFinite(view.M33) && float.IsFinite(view.M43), $"The view is {view}.");
    }
}
