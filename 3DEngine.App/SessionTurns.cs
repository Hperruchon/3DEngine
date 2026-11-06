namespace ThreeDEngine.Desktop;

// ADR-0079 item 4 (P0.103, TASK-0255): the turns of the sessions of the phone in one process. With the hint of ADR-0079, a
// new create of the activity starts a new session while the session of the destroyed activity can still run, for example
// in the probe of ADR-0074. Run 42308415060: the new session made its window, and the old one then could not make one,
// because SDL on Android permits one window. Each create of the activity takes a turn; a session whose turn is not the
// last one ends before its window, and a session holds the window until its window is gone, so that a later session waits
// for it.
internal sealed class SessionTurns
{
    private int _last;
    private readonly SemaphoreSlim _window = new(1, 1);

    // The turns of the activity of the phone.
    public static SessionTurns Phone { get; } = new();

    // A create of the activity: its turn. Each earlier turn is no longer the last one.
    public int Begin() => Interlocked.Increment(ref _last);

    // True while no later create of the activity took a turn.
    public bool IsLast(int turn) => turn == Volatile.Read(ref _last);

    // The window of one session, until the dispose of the result; null when an earlier session still holds it after the
    // limit.
    public IDisposable? HoldWindow(TimeSpan limit) => _window.Wait(limit) ? new Held(_window) : null;

    private sealed class Held(SemaphoreSlim window) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                window.Release();
        }
    }
}
