using System.Runtime.InteropServices;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// A [Fact] that needs a Vulkan device (ADR-0025 item 4). xunit v2 has no Assert.Skip,
// so the attribute sets Skip at discovery time:
//
// - In continuous integration (CI=true) on Linux it never skips. The runner installs
//   lavapipe, and a runner that lost it must give a red run and not a green one with
//   a skipped test, as for NativeManifoldFactAttribute (TASK-0036).
// - In continuous integration on Windows and macOS it skips: the images of those
//   runners give no usable Vulkan device. The register holds the reason (R-0040).
// - On the computer of a person it skips when no Vulkan loader exists.
public sealed class VulkanImageFactAttribute : FactAttribute
{
    public VulkanImageFactAttribute()
    {
        Skip = SkipReason(
            Environment.GetEnvironmentVariable("CI"),
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            static () => GraphicsDevice.IsSupported());
    }

    public static bool InContinuousIntegration
        => string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    // The loader is probed only when the answer depends on it.
    internal static string? SkipReason(string? ci, bool linux, Func<bool> vulkanLoaderExists)
    {
        if (string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase))
            return linux ? null : "The pipeline runs the image test on ubuntu-latest only (ADR-0025 item 4, R-0040).";

        return vulkanLoaderExists() ? null : "No Vulkan loader exists on this computer, and CI is not true.";
    }
}

// The rule of the attribute above (TASK-0076).
public class VulkanImageFactAttributeTests
{
    [Theory]
    [InlineData("true", true, false, false)]
    [InlineData("true", false, true, true)]
    [InlineData("TRUE", false, false, true)]
    [InlineData(null, false, true, false)]
    [InlineData(null, true, false, true)]
    [InlineData("false", true, true, false)]
    public void The_Image_Test_Skips_Only_Off_Linux_In_The_Pipeline_Or_With_No_Loader(string? ci, bool linux, bool loader, bool skips)
    {
        Assert.Equal(skips, VulkanImageFactAttribute.SkipReason(ci, linux, () => loader) is not null);
    }
}
