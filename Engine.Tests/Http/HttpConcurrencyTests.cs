using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// TASK-0034: the HTTP host sends commands, queries and the WebSocket handshake
// through one document session.
//
// These tests run on the test server and not on Kestrel. The behaviour that they
// test is the order of the engine and of the broadcaster, and it does not depend
// on the server. Each test builds its own host, so that it starts from an empty
// Document.
public class HttpConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly TimeSpan OperationLimit = TimeSpan.FromSeconds(10);

    private readonly WebApplicationFactory<Program> _baseFactory;

    public HttpConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    // Before the session, a query read the backend while a command changed it,
    // and the host answered with HTTP 500.
    [Fact]
    public async Task Queries_And_Commands_In_Parallel_Get_No_Http_500()
    {
        const int commandCount = 1_000;
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        var statuses = new ConcurrentDictionary<int, int>();
        var done = 0;

        // The first body exists before a reader starts. In the first form of the
        // test each reader turned in a loop with no await until that body existed.
        // Four such loops held four threads of the pool, which can starve the
        // server on a runner with few cores (TASK-0045).
        var firstId = Guid.NewGuid();
        using (var first = await PostCreateBox(http, firstId))
            statuses.AddOrUpdate((int)first.StatusCode, 1, (_, n) => n + 1);
        object newest = firstId; // A boxed Guid, because Volatile needs a reference type.

        var writer = Task.Run(async () =>
        {
            try
            {
                for (var i = 1; i < commandCount; i++)
                {
                    var commandId = Guid.NewGuid();
                    using var response = await PostCreateBox(http, commandId);
                    statuses.AddOrUpdate((int)response.StatusCode, 1, (_, n) => n + 1);
                    Volatile.Write(ref newest, (object)commandId);
                }
            }
            finally
            {
                Volatile.Write(ref done, 1);
            }
        });

        var readers = Enumerable.Range(0, 4).Select(reader => Task.Run(async () =>
        {
            while (Volatile.Read(ref done) == 0)
            {
                var bodyId = (Guid)Volatile.Read(ref newest);

                using var cts = new CancellationTokenSource(OperationLimit);
                using var response = await http.PostAsJsonAsync("/queries", new
                {
                    name = "GetBoundingBox",
                    schemaVersion = 1,
                    parameters = new { bodyId },
                }, cts.Token);
                statuses.AddOrUpdate((int)response.StatusCode, 1, (_, n) => n + 1);
            }
        })).ToArray();

        await Task.WhenAll(readers.Append(writer));

        var report = string.Join("; ", statuses.OrderBy(s => s.Key).Select(s => $"HTTP {s.Key}: {s.Value}"));
        Assert.True(statuses.Keys.All(code => code == 200), report);
    }

    // Finding E9 of the codebase review of 2026-09-30. A subscriber that attached
    // during a commit received a snapshot that did not agree with the ring, and
    // then lost an event or received it two times. Each subscriber here asks for
    // a reset while commands run, and its first live event must carry the seq of
    // the snapshot plus one. Until TASK-0035 the snapshot gave no seq, and the
    // version was the last Seq (ADR-0020).
    [Fact]
    public async Task The_First_Live_Seq_After_A_Reset_Is_The_Snapshot_Seq_Plus_One()
    {
        const int subscriberCount = 100;
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        using var stop = new CancellationTokenSource();
        var writer = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
                await PostCreateBox(http, Guid.NewGuid());
        });

        var mismatches = new List<string>();
        try
        {
            for (var i = 0; i < subscriberCount; i++)
            {
                using var cts = new CancellationTokenSource(OperationLimit);
                using var socket = await WebSocketTestClient.ConnectAsync(factory, cts.Token);
                await WebSocketTestClient.SendJsonAsync(socket, new { }, cts.Token);

                var reset = await WebSocketTestClient.ReceiveJsonAsync(socket, cts.Token);
                Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());
                var seq = reset.GetProperty("snapshot").GetProperty("seq").GetInt64();

                var firstSeq = await FirstLiveSeq(socket, cts.Token);
                if (firstSeq != seq + 1)
                    mismatches.Add($"subscriber {i}: snapshot seq {seq}, first live seq {firstSeq}");

                await CloseAfterTheMeasurement(socket);
            }
        }
        finally
        {
            stop.Cancel();
            await writer;
        }

        Assert.True(mismatches.Count == 0, string.Join("; ", mismatches));
    }

    // TASK-0034, the order of the locks. A commit holds the session, and its sink
    // takes the lock of the broadcaster. The handshake must take the session
    // first and the broadcaster second. With the opposite order, two threads can
    // each wait for the other.
    //
    // A thread that waits for a lock does not see a cancellation token, so the
    // test sets one time limit for all the work. With the opposite order injected
    // into the handshake, the test stopped at that limit. On a deadlock the test
    // does not dispose the host, because a host with blocked requests does not stop.
    [Fact]
    public async Task Subscriptions_And_Commands_In_Parallel_For_One_Second_Do_Not_Deadlock()
    {
        var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();
        var clock = Stopwatch.StartNew();
        var commands = 0;
        var subscriptions = 0;

        var writers = Enumerable.Range(0, 2).Select(writer => Task.Run(async () =>
        {
            while (clock.Elapsed < TimeSpan.FromSeconds(1))
            {
                using var response = await PostCreateBox(http, Guid.NewGuid());
                response.EnsureSuccessStatusCode();
                Interlocked.Increment(ref commands);
            }
        }));

        var subscribers = Enumerable.Range(0, 4).Select(subscriber => Task.Run(async () =>
        {
            while (clock.Elapsed < TimeSpan.FromSeconds(1))
            {
                using var cts = new CancellationTokenSource(OperationLimit);
                using var socket = await WebSocketTestClient.ConnectAsync(factory, cts.Token);
                await WebSocketTestClient.SendJsonAsync(socket, new { }, cts.Token);
                var first = await WebSocketTestClient.ReceiveJsonAsync(socket, cts.Token);
                Assert.Equal("subscription.reset", first.GetProperty("kind").GetString());
                await CloseAfterTheMeasurement(socket);
                Interlocked.Increment(ref subscriptions);
            }
        }));

        var all = Task.WhenAll(writers.Concat(subscribers));
        var limit = TimeSpan.FromSeconds(1) + OperationLimit;
        var first = await Task.WhenAny(all, Task.Delay(limit));
        Assert.True(first == all,
            $"The work did not end in {limit.TotalSeconds} s: {commands} commands and " +
            $"{subscriptions} subscriptions completed. Two threads wait for each other.");
        await all;
        factory.Dispose();

        Assert.True(clock.Elapsed >= TimeSpan.FromSeconds(1));
        Assert.True(commands > 0, "No command completed.");
        Assert.True(subscriptions > 0, "No subscription completed.");
    }

    private static async Task<HttpResponseMessage> PostCreateBox(HttpClient http, Guid commandId)
    {
        using var cts = new CancellationTokenSource(OperationLimit);
        return await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            commandId,
            parameters = new { sizeX = 1.0, sizeY = 2.0, sizeZ = 3.0 },
        }, cts.Token);
    }

    // The client stops reading after its measurement while commands continue.
    // On a slow runner its queue of 1,024 events can fill before it closes, and
    // the server then disconnects it as a slow subscriber (ADR-0005 §6), which is
    // correct. A run on one core showed it: the pump ended with lagged=True, and
    // the close of the client found a closed socket. The measurement is complete
    // at this point, so the close is cleanup only (TASK-0045).
    private static async Task CloseAfterTheMeasurement(WebSocket socket)
    {
        try
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or WebSocketException or ObjectDisposedException)
        {
            // The server closed first.
        }
    }

    // The first message with a seq, so that a heartbeat does not count.
    private static async Task<long> FirstLiveSeq(WebSocket socket, CancellationToken ct)
    {
        while (true)
        {
            var message = await WebSocketTestClient.ReceiveJsonAsync(socket, ct);
            if (message.TryGetProperty("seq", out var seq) && seq.ValueKind == JsonValueKind.Number)
                return seq.GetInt64();
        }
    }
}
