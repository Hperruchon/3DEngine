using System.Collections.Concurrent;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests;

// TASK-0034, scope item 1. The probe of the codebase review of 2026-09-30, section 5:
// commands on one task, and queries and snapshot copies on four tasks.
//
// On the code before the session, with the buses and Document.Bodies read
// directly, three runs gave these failures. Run 1: 1,021 queries threw
// InvalidOperationException, 839 copies threw ArgumentException and 1 copy threw
// IndexOutOfRangeException. Run 2: 1,152 and 920. Run 3: 950 and 849.
public class DocumentSessionConcurrencyTests
{
    private const int CommandCount = 5_000;
    private const int ReaderCount = 4;

    [Fact]
    public async Task Commands_Queries_And_Snapshot_Copies_In_Parallel_Never_Overlap()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var session = new DocumentSession(kit);

        var failures = new ConcurrentDictionary<string, int>();
        void Count(string what) => failures.AddOrUpdate(what, 1, (_, n) => n + 1);

        object? newest = null; // A boxed Guid, because Volatile needs a reference type.
        var done = 0;

        var writer = Task.Run(async () =>
        {
            try
            {
                for (var i = 0; i < CommandCount; i++)
                {
                    var command = new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 };
                    var result = await session.Apply(command);
                    if (result.Status != CommandStatus.Applied)
                        Count("command not applied");
                    Volatile.Write(ref newest, (object)command.CommandId);
                }
            }
            finally
            {
                Volatile.Write(ref done, 1);
            }
        });

        // Finding T13 of the codebase review of 2026-10-04 (R-0031, TASK-0320): no
        // assertion said that a reader ran, so a test with no query and no copy passed.
        // Each reader now runs until it made one query, and the test counts the reads.
        // Until TASK-0320 a reader could end before its first query.
        var queries = 0;
        var copies = 0;

        var readers = Enumerable.Range(0, ReaderCount).Select(reader => Task.Run(async () =>
        {
            var queried = false;
            while (Volatile.Read(ref done) == 0 || (!queried && Volatile.Read(ref newest) is not null))
            {
                if (Volatile.Read(ref newest) is Guid bodyId)
                {
                    queried = true;
                    Interlocked.Increment(ref queries);
                    try
                    {
                        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = bodyId });
                        if (result.Error is not null)
                            Count($"query error {result.Error.Code} for an applied body");
                    }
                    catch (Exception ex)
                    {
                        Count($"query threw {ex.GetType().Name}");
                    }
                }

                Interlocked.Increment(ref copies);
                try
                {
                    _ = await session.Read((document, _) => document.Bodies.ToArray());
                }
                catch (Exception ex)
                {
                    Count($"snapshot copy threw {ex.GetType().Name}");
                }
            }
        })).ToArray();

        await Task.WhenAll(readers.Append(writer));

        var report = string.Join("; ", failures.OrderBy(f => f.Key).Select(f => $"{f.Key}: {f.Value}"));
        Assert.True(failures.IsEmpty, report);
        Assert.True(
            queries >= ReaderCount && copies >= ReaderCount,
            $"Each of the {ReaderCount} readers must run: the readers made {queries} queries and {copies} snapshot copies.");
        Assert.Equal(CommandCount, kit.Document.Log.Count);
        Assert.Equal(CommandCount, kit.Document.Bodies.Count);
    }
}
