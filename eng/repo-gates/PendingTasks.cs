using System.Diagnostics;

// Finding T83 of the codebase review of 2026-10-29 (TASK-0107): DiscardedTasks refuses each
// form of syntax of findings T67 and T74, and five more forms passed it: a lambda converted to
// Action, List<Task>.Add, a tuple assignment, a local that GC.KeepAlive reads, and a void method
// with an expression body. Each one starts a task that the check does not wait for, so a
// failure in the task never failed the check. The runner now waits after each check until no
// work of the check is left, and fails the check when a task of it failed and nobody read the
// failure. No form of syntax is added.
internal static class PendingTasks
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    private static readonly List<Exception> Unobserved = [];

    // The runtime reports a failed task that nobody read when the collector finalizes it.
    public static void Watch()
        => TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            lock (Unobserved)
                Unobserved.Add(e.Exception);
            e.SetObserved();
        };

    // The count of active timers before a check. Task.Delay holds a timer, so a task that
    // waits for a delay is work that is left.
    public static long Timers() => Timer.ActiveCount;

    // Waits until the thread pool holds no queued item and no busy worker, and no timer of the
    // check is active, and then collects each task that nobody holds, so that the finalizer of
    // a failed task reports it. The problem, or null. The runner runs on the main thread, which
    // is not a thread of the pool.
    public static string? Settle(long timersBefore)
    {
        var clock = Stopwatch.StartNew();
        while (!Idle(timersBefore))
        {
            if (clock.Elapsed > Limit)
                return $"work that the check started still runs after {Limit.TotalSeconds} s";
            Thread.Sleep(5);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        lock (Unobserved)
        {
            if (Unobserved.Count == 0)
                return null;
            var causes = string.Join("; ", Unobserved.Select(failure => failure.InnerException?.Message ?? failure.Message));
            Unobserved.Clear();
            return $"a task that the check started failed, and the check did not wait for it: {causes}";
        }
    }

    private static bool Idle(long timersBefore)
    {
        ThreadPool.GetMaxThreads(out var max, out _);
        ThreadPool.GetAvailableThreads(out var available, out _);
        return ThreadPool.PendingWorkItemCount == 0 && max == available && Timer.ActiveCount <= timersBefore;
    }

    private static void Fail() => throw new InvalidOperationException("the probe task fails");

    private static void StartByExpressionBody() => Task.Run(Fail);

    // The forms of finding T83, a task that fails after a delay, and two forms that wait. A form
    // that the runner misses, or a form that waits and that the runner refuses, fails the run.
    private static readonly (string Name, Func<Task> Check, bool Problem)[] Forms =
    [
        ("a lambda converted to Action (T83)", () => { Action start = () => Task.Run(Fail); start(); return Task.CompletedTask; }, true),
        ("List<Task>.Add (T83)", () => { var tasks = new List<Task>(); tasks.Add(Task.Run(Fail)); return Task.CompletedTask; }, true),
        ("a tuple assignment (T83)", () => { (Task task, int count) = (Task.Run(Fail), 1); return Task.FromResult(count); }, true),
        ("a local that GC.KeepAlive reads (T83)", () => { var task = Task.Run(Fail); GC.KeepAlive(task); return Task.CompletedTask; }, true),
        ("a void method with an expression body (T83)", () => { StartByExpressionBody(); return Task.CompletedTask; }, true),
        ("a task that fails after a delay", () => { Task.Delay(200).ContinueWith(_ => Fail()); return Task.CompletedTask; }, true),
        ("an await", async () => await Task.Run(() => { }), false),
        ("a task that the check reads", async () => await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Fail)), false),
    ];

    public static IEnumerable<string> SelfCheck()
    {
        foreach (var (name, check, problem) in Forms)
        {
            var timers = Timers();
            check().GetAwaiter().GetResult();
            var found = Settle(timers) is not null;
            if (found != problem)
                yield return problem ? $"the runner does not see {name}" : $"the runner refuses {name}, which waits";
        }
    }
}
