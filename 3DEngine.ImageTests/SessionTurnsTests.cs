using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0079 item 4 (P0.103, TASK-0255): the turns of the sessions of the phone in one process. Run 42308415060 gave the order
// that these tests hold: the activity is created, destroyed during the probe of its session and created again; the new
// session makes its window, and the old session must end before its window.
public sealed class SessionTurnsTests
{
    [Fact]
    public void A_Later_Create_Of_The_Activity_Makes_The_Earlier_Turn_Stale()
    {
        var turns = new SessionTurns();
        var first = turns.Begin();
        Assert.True(turns.IsLast(first));

        var second = turns.Begin();

        Assert.False(turns.IsLast(first));
        Assert.True(turns.IsLast(second));
    }

    // A session holds the window until its window is gone; a later session waits for it, and the hold gives null after
    // the limit.
    [Fact]
    public async Task A_Later_Session_Waits_For_The_Window_Of_The_Earlier_One()
    {
        var turns = new SessionTurns();
        var earlier = turns.HoldWindow(TimeSpan.FromSeconds(1));
        Assert.NotNull(earlier);
        Assert.Null(turns.HoldWindow(TimeSpan.FromMilliseconds(50)));

        // P0.135 (TASK-0322): the later session waits up to 5 minutes, so a late continuation of the delay below on a busy
        // runner does not end its wait. Run 42543751923 on windows-latest failed with a limit of 10 s: the waiter gave up
        // before the assertion ran, although the earlier session held the window.
        var later = Task.Run(() => turns.HoldWindow(TimeSpan.FromMinutes(5)));
        await Task.Delay(100);
        Assert.False(later.IsCompleted);
        earlier.Dispose();
        earlier.Dispose();

        using var held = await later;
        Assert.NotNull(held);
        Assert.Null(turns.HoldWindow(TimeSpan.FromMilliseconds(50)));
    }

    // The order of the run: the first session is in its probe when the second create takes a turn. The second session
    // holds the window; the first one then sees that its turn is stale and does not ask for the window.
    [Fact]
    public async Task The_Session_Of_A_Destroyed_Activity_Ends_Before_Its_Window()
    {
        var turns = new SessionTurns();
        var first = turns.Begin();
        using var probe = new ManualResetEventSlim();
        var windows = new List<int>();
        string? Session(int turn, ManualResetEventSlim? waitFor)
        {
            waitFor?.Wait(TimeSpan.FromSeconds(5));
            if (!turns.IsLast(turn))
                return "stale";
            using var held = turns.HoldWindow(TimeSpan.FromSeconds(5));
            if (held is null)
                return "no window";
            lock (windows)
                windows.Add(turn);
            return null;
        }

        var old = Task.Run(() => Session(first, probe));
        var second = turns.Begin();
        var current = await Task.Run(() => Session(second, null));
        probe.Set();

        Assert.Equal(("stale", (string?)null), (await old, current));
        Assert.Equal([second], windows);
    }
}
