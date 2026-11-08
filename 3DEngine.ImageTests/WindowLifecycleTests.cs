using SDL;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

namespace ThreeDEngine.ImageTests;

// Finding V112 of the codebase review of 2026-11-06 (e) (TASK-0250): a window destroys its SDL window at its Dispose, so a
// later session of the process makes its own. The video driver "offscreen" of SDL needs no display; the window asks for
// Vulkan, so the test runs where the image tests of the GPU run.
// TASK-0292: each new window clears the state of the file dialog (E179), so this class and FileDialogStateTests run in one
// collection; until TASK-0292 they ran at the same time, and the test of the dialog failed when the timing changed.
[Collection(FileDialogStateTests.Collection)]
public sealed unsafe class WindowLifecycleTests
{
    [VulkanImageFact]
    public void A_Disposed_Window_Is_Gone_And_A_Second_Window_Comes()
    {
        fixed (byte* name = SDL_HINT_VIDEO_DRIVER)
        fixed (byte* offscreen = "offscreen"u8)
        {
            _ = SDL_SetHint(name, offscreen);
        }

        Assert.True(SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO), "SDL did not start its video.");
        try
        {
            var first = new Window("first", 64, 64);
            var id = first.Id;
            Assert.True(SDL_GetWindowFromID(id) != null, "SDL does not know the first window.");

            first.Dispose();
            first.Dispose();
            Assert.True(SDL_GetWindowFromID(id) == null, "The first window is still in SDL after its Dispose.");

            using var second = new Window("second", 64, 64);
            Assert.True(SDL_GetWindowFromID(second.Id) != null, "SDL does not know the second window.");
        }
        finally
        {
            SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
        }
    }
}
