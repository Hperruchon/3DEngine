using SDL;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0073 item 3 (phase R14e3, TASK-0227): the steps of the life of the program on the phone, from the events of SDL.
// The first sign of the background ends the engine, only the end of the return to the foreground makes a new one, and
// each other event goes to the engine.
public sealed class PhoneLifecycleTests
{
    [Theory]
    [InlineData(SDL_EventType.SDL_EVENT_QUIT, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_TERMINATING, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND, PhoneStep.Background)]
    [InlineData(SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND, PhoneStep.Background)]
    [InlineData(SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND, PhoneStep.None)]
    [InlineData(SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND, PhoneStep.Foreground)]
    [InlineData(SDL_EventType.SDL_EVENT_FINGER_DOWN, PhoneStep.None)]
    [InlineData(SDL_EventType.SDL_EVENT_LOW_MEMORY, PhoneStep.None)]
    public void Each_Event_Gives_Its_Step(SDL_EventType type, PhoneStep step)
        => Assert.Equal(step, PhoneLifecycle.Of(type));

    // Finding E172 of the codebase review of 2026-11-07 (b) (TASK-0269), ADR-0079 item 4: the wait in the background ends a
    // session whose turn is stale at each return, also at the foreground event of the new activity. Until TASK-0269 the wait
    // checked the turn only after 500 ms with no event, so the stale session took that event and kept the window.
    [Theory]
    [InlineData(null, false, null)]
    [InlineData(PhoneStep.None, false, null)]
    [InlineData(PhoneStep.Background, false, null)]
    [InlineData(PhoneStep.Foreground, false, true)]
    [InlineData(PhoneStep.Quit, false, false)]
    [InlineData(null, true, false)]
    [InlineData(PhoneStep.None, true, false)]
    [InlineData(PhoneStep.Foreground, true, false)]
    public void A_Stale_Turn_Ends_The_Wait_At_Each_Return(PhoneStep? step, bool stale, bool? result)
        => Assert.Equal(result, PhoneLifecycle.AfterWait(step, stale));

    // Finding E184 of the codebase review of 2026-11-07 (c) (TASK-0281): a lost surface always waits for the foreground before
    // the next engine, and a new part that waits gets its host first. Until TASK-0281 a lost surface with a new part waiting
    // made the next engine at once, on the window whose surface was just lost.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Lost_Surface_Waits_For_The_Foreground(bool newPartWaiting)
        => Assert.Equal((newPartWaiting, true), PhoneLifecycle.AfterLostSurface(newPartWaiting));

    // The audit of Codex of the phone host at d5209a2 (TASK-0286): a lost surface is known by the result of Vulkan; another
    // fault is not a lost surface.
    [Fact]
    public void A_Lost_Surface_Is_Known_By_Its_Result()
    {
        Assert.True(PhoneLifecycle.IsSurfaceLost(new Vortice.Vulkan.VkException(Vortice.Vulkan.VkResult.ErrorSurfaceLostKHR, "vkGetPhysicalDeviceSurfaceCapabilitiesKHR")));
        Assert.False(PhoneLifecycle.IsSurfaceLost(new Vortice.Vulkan.VkException(Vortice.Vulkan.VkResult.ErrorDeviceLost, "vkQueueSubmit")));
        Assert.False(PhoneLifecycle.IsSurfaceLost(new InvalidOperationException("ErrorSurfaceLostKHR")));
    }

    // The audit of Codex of the phone host at d5209a2 (TASK-0286): the start of the engine and each frame on the phone have
    // the guard of a lost surface. The phone host runs on no computer here, so the test reads its source. Until TASK-0286 only
    // the frame had the guard, and a surface lost in the start ended the session with its document.
    [Fact]
    public void The_Start_And_The_Frames_Of_The_Phone_Have_The_Guard_Of_A_Lost_Surface()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "3DEngine.sln")))
            directory = directory.Parent;
        var host = File.ReadAllText(Path.Combine(directory?.FullName ?? throw new InvalidOperationException("No 3DEngine.sln."), "3DEngine.Android", "PhoneHost.cs"));
        foreach (var call in new[] { "engine.Initialize();", "engine.Render();" })
        {
            var guarded = new System.Text.RegularExpressions.Regex(@"try\s*\{\s*" + System.Text.RegularExpressions.Regex.Escape(call) + @"\s*\}\s*catch \(Exception ex\) when \(PhoneLifecycle\.IsSurfaceLost\(ex\)\)");
            Assert.True(guarded.IsMatch(host), $"{call} of the phone host has no guard of a lost surface.");
        }
    }
}
