using Engine.Core.Determinism;
using Engine.Core.Assets;
using Engine.Geometry.Manifold;
using SDL;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 items 4 and 9, ADR-0072, TASK-0226): the host of the phone. It composes a session of its own on
// the native backend, with no surface and no file, and draws it with the application in one window, as the desktop
// draws one session. The person sends each command through the CommandBus of this process.
// Phase R14e3 (ADR-0073 item 3, TASK-0227): the session stays through the background. Each time in the foreground has
// its own engine, with its device and its surface, because Android destroys the surface in the background. The picker,
// the pre-rotation and the test of the owner come later; no device ran this program before the test of the owner.
internal static class PhoneHost
{
    // ADR-0079 item 4 (TASK-0255): turn is the turn of the create of the activity of this session (SessionTurns); 0 for a
    // session that takes no turn.
    public static void Run(bool probe = false, int turn = 0)
    {
        try
        {
            if (!ManifoldGeometryBackend.IsNativeAvailable())
            {
                Console.Error.WriteLine($"3DEngine: {ManifoldGeometryBackend.UnavailableReason()}");
                return;
            }

            // Phase R14f2 (TASK-0229): the smoke test of the emulator reads this line and the line of the first frame.
            Console.Error.WriteLine($"3DEngine: the session of the phone starts, with Manifold {ManifoldGeometryBackend.NativeVersion}.");
            // Phase R14f3 (ADR-0074, TASK-0233): the probe of anti-objective 16, on its own backend and its own Document. The
            // smoke test of the emulator reads these lines, and the pipeline compares the hash with the desktop runners.
            if (probe)
            {
                using var backend = new ManifoldGeometryBackend();
                var (hash, lines, _) = CrossPlatformProbe.Run(backend).GetAwaiter().GetResult();
                Console.Error.WriteLine($"3DEngine: determinism {hash}");
                foreach (var line in lines)
                    Console.Error.WriteLine($"3DEngine: determinism-line {line}");
            }

            // ADR-0083 D1 and D6 (TASK-0260): the store of the phone, in the folder of the application, and the files of the run:
            // the home screen at the start, and a new part as a new session in the same window.
            var assets = new AssetStore(AssetStore.Default);
            var files = new PhoneFiles();
            BlockOnPause();
            // ADR-0079 item 4 (P0.103, TASK-0255): a session of a destroyed activity ends before its window, and a session
            // waits until the window of an earlier session is gone. Run 42308415060: the activity was destroyed during the
            // probe, the new session made its window, and this session then could not make one.
            if (Stale(turn))
                return;
            using var held = SessionTurns.Phone.HoldWindow(TimeSpan.FromSeconds(10))
                ?? throw new TimeoutException("The window of an earlier session of this process stayed for 10 seconds.");
            if (Stale(turn))
                return;
            // Finding V112 (TASK-0250): the session destroys its window at its end, so a later session of this process, after a
            // new start of the activity (ADR-0079), makes its own; SDL on Android permits one window.
            using var window = new Window("3DEngine", 1080, 2316);
            // Finding E176 of the codebase review of 2026-11-07 (b) (TASK-0272): the host comes after the checks of the turn
            // and the window, so each way out of Run disposes it; until TASK-0272 a stale session and a timeout of the window
            // left its session and its native backend.
            var host = new LocalSessionHost(new ManifoldGeometryBackend(), assets);
            try
            {
                while (true)
                {
                    // Finding E172 of the codebase review of 2026-11-07 (b) (TASK-0269): the turn at each pass, so a session
                    // whose activity was destroyed does not make an engine in the window of the new one.
                    if (turn != 0 && !SessionTurns.Phone.IsLast(turn))
                    {
                        Console.Error.WriteLine("3DEngine: the activity of this session was destroyed, so the session ends.");
                        break;
                    }

                    var step = Foreground(window, host, files, assets);
                    if (step == Session.NewPart)
                    {
                        host.Dispose();
                        host = new LocalSessionHost(new ManifoldGeometryBackend(), assets);
                        Console.Error.WriteLine("3DEngine: a new part document.");
                        continue;
                    }

                    if (step == Session.Quit || !WaitForForeground(turn))
                        break;
                    Console.Error.WriteLine("3DEngine: the program is in the foreground again, with the same document.");
                }
            }
            finally
            {
                host.Dispose();
            }
        }
        catch (Exception ex)
        {
            // The log of Android receives the standard error of the program. Each line of the exception has the prefix, so
            // that the smoke test of the emulator gives the stack as public annotations (TASK-0229: the third run of the
            // job ended with PlatformNotSupportedException and showed no stack).
            Console.Error.WriteLine($"3DEngine: the session of the phone ended: {ex.GetType().Name}: {ex.Message}");
            foreach (var line in ex.ToString().Split('\n'))
                Console.Error.WriteLine($"3DEngine: | {line.TrimEnd()}");
        }
    }

    // True, with a line for the smoke test, when a later create of the activity took a turn after this session.
    private static bool Stale(int turn)
    {
        if (turn == 0 || SessionTurns.Phone.IsLast(turn))
            return false;
        Console.Error.WriteLine("3DEngine: the activity of this session was destroyed, so the session ends before its window.");
        return true;
    }

    // Finding V101 of the codebase review of 2026-11-06 (c) (TASK-0232), Q1 B: SDL blocks the thread of the program in its
    // event loop while the program is paused, so the loop sees WILL_ENTER_BACKGROUND only at the return, and the engine
    // ends then, just before the new one starts (ADR-0073 item 3). The hint holds that order, so that it does not rest on
    // a default of SDL. The hint comes before the first window. Finding V110 of the codebase review of 2026-11-06 (d)
    // (TASK-0243): this order comes from the code of SDL, which is not in this repository; no run shows it yet, and step 8
    // of the test sheet of the phone shows it.
    // ADR-0079 item 1 (TASK-0247): a later create of the activity in this process goes on. With the default of SDL, a new
    // start of the activity ends the process (the strings "activity finished" and exit of SDLActivity, run 42289169473).
    public static unsafe void AllowNewStart()
    {
        fixed (byte* name = SDL_HINT_ANDROID_ALLOW_RECREATE_ACTIVITY)
        fixed (byte* on = "1"u8)
        {
            var set = SDL_SetHint(name, on);
            Console.Error.WriteLine($"3DEngine: a new start of the activity {(set ? "goes on" : "ends the process; the hint was refused")}.");
        }
    }

    private static unsafe void BlockOnPause()
    {
        fixed (byte* name = SDL_HINT_ANDROID_BLOCK_ON_PAUSE)
        fixed (byte* on = "1"u8)
        {
            _ = SDL_SetHint(name, on);
        }
    }

    // How one time in the foreground ends (ADR-0083, TASK-0260): a quit, the background, or a new part document.
    private enum Session
    {
        Quit,
        Background,
        NewPart,
    }

    // The loop of one time in the foreground.
    private static unsafe Session Foreground(Window window, LocalSessionHost host, PhoneFiles files, AssetStore assets)
    {
        using var engine = new NativeThreeDEngine(window, host, document: files, assets: assets);
        engine.Initialize();
        engine.LoadScene(SessionScene.Create());
        window.Show();

        var lastFrame = DateTime.UtcNow;
        var first = true;
        while (true)
        {
            // A frame draws nothing while the window has no area, as on the desktop (finding V23).
            if (window.HasNoArea)
                Window.WaitForEvent(100);

            SDL_Event sdlEvent;
            while (SDL_PollEvent(&sdlEvent))
            {
                switch (PhoneLifecycle.Of(sdlEvent.Type))
                {
                    case PhoneStep.Quit:
                        return Session.Quit;
                    case PhoneStep.Background:
                        Console.Error.WriteLine("3DEngine: the program goes into the background; the document stays.");
                        return Session.Background;
                }

                engine.HandleEvent(sdlEvent);
            }

            var now = DateTime.UtcNow;
            engine.Update(now - lastFrame);
            // Finding V124 of the codebase review of 2026-11-07 (TASK-0265): Android destroyed the activity after the first
            // frame (run 42323457861), and the surface was lost before the next event of SDL reached this loop; the session
            // ended with the exception. A lost surface now ends the engine as the background does, and the wait for the
            // foreground ends the session quietly when a later create of the activity took a turn (ADR-0079 item 4).
            try
            {
                engine.Render();
            }
            catch (Vortice.Vulkan.VkException ex) when (ex.Message.Contains(nameof(Vortice.Vulkan.VkResult.ErrorSurfaceLostKHR), StringComparison.Ordinal))
            {
                Console.Error.WriteLine("3DEngine: the surface of the window is lost, so the engine ends as in the background.");
                // Finding E175 of the codebase review of 2026-11-07 (b) (TASK-0272): the frame that took a chosen STL queued
                // its new part before the present, so a lost surface in that frame keeps the request; until TASK-0272 the old
                // session took the import at its return, and the request then replaced it with an empty part.
                return files.TakeRequestedNew() ? Session.NewPart : Session.Background;
            }

            lastFrame = now;
            if (files.TakeRequestedNew())
                return Session.NewPart;
            // Finding V106 of the codebase review of 2026-11-06 (d) (TASK-0243): the line comes after the first frame that the
            // presenter submitted, not after the first call, which draws nothing while the surface has no area.
            if (first && engine.Drew)
            {
                Console.Error.WriteLine("3DEngine: the first frame is drawn.");
                first = false;
            }
        }
    }

    // The wait in the background, until the program is in the foreground again (true) or ends (false). V124 (TASK-0265): the
    // wait ends quietly when a later create of the activity took a turn, because the session of that create holds the window.
    // Finding E172 of the codebase review of 2026-11-07 (b) (TASK-0269): the turn at each return of the wait, also at an
    // event; until TASK-0269 only a quiet wait of 500 ms read it, so the stale session took the foreground event of the new
    // activity and kept the window (PhoneLifecycle.AfterWait).
    private static unsafe bool WaitForForeground(int turn)
    {
        SDL_Event sdlEvent;
        while (true)
        {
            var step = SDL_WaitEventTimeout(&sdlEvent, 500) ? PhoneLifecycle.Of(sdlEvent.Type) : (PhoneStep?)null;
            var stale = turn != 0 && !SessionTurns.Phone.IsLast(turn);
            if (PhoneLifecycle.AfterWait(step, stale) is { } result)
            {
                if (stale)
                    Console.Error.WriteLine("3DEngine: the activity of this session was destroyed, so the session ends.");
                return result;
            }
        }
    }
}
