using System.Text.Json.Nodes;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;

namespace Engine.Tests;

// Finding E22 of the codebase review of 2026-10-04 (R-0033, TASK-0321). The bus turned each
// OperationCanceledException of a handler into Cancelled, also with a live token of the caller, and
// the cache kept the cancellation, so a retry with the same CommandId never ran the handler.
public sealed class CommandBusCancellationTests
{
    [Fact]
    public async Task A_Cancellation_Exception_With_A_Live_Token_Is_Not_Cancelled()
    {
        var document = new Document();
        var registry = new CommandRegistry();
        registry.Register(new LiveCancellingHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(document, registry, sink);

        await Assert.ThrowsAsync<TaskCanceledException>(() => bus.Apply(new LiveCancellingCommand()));

        Assert.Empty(document.Log);
        Assert.Equal(0, document.Version);
        Assert.Empty(sink.Snapshot());
    }

    [Fact]
    public async Task A_Cancelled_Result_Is_Not_Cached_So_A_Retry_Runs_The_Handler()
    {
        var registry = new CommandRegistry();
        var handler = new CancelOnceHandler();
        registry.Register(handler);
        var document = new Document();
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(document, registry, sink);
        var command = new CancelOnceCommand();

        using (var cancel = new CancellationTokenSource())
        {
            handler.Cancel = cancel;
            var first = await bus.Apply(command, cancel.Token);
            Assert.Equal(CommandStatus.Cancelled, first.Status);
        }

        var retry = await bus.Apply(command);

        Assert.Equal(2, handler.Calls);
        Assert.Equal(CommandStatus.Applied, retry.Status);
        Assert.Single(document.Log);
    }

    // The replay and the open caught each exception that is not a cancellation exception. A cancellation
    // exception with a live token of the caller is now an exception of a handler, so each one keeps its
    // own error and does not report a cancellation.
    [Fact]
    public async Task A_Cancellation_Exception_With_A_Live_Token_In_A_Replay_Is_A_Divergence()
    {
        var registry = new CommandRegistry();
        registry.Register(new LiveCancellingHandler());

        var divergence = await Assert.ThrowsAsync<ReplayDivergenceException>(
            () => Replay.ReplayLog(new Command[] { new LiveCancellingCommand() }, registry, NullGeometryBackend.Instance));

        Assert.Equal(0, divergence.Index);
        Assert.IsType<TaskCanceledException>(divergence.InnerException);
    }

    [Fact]
    public async Task A_Cancellation_Exception_With_A_Live_Token_In_An_Open_Is_A_Load_Failure()
    {
        var folder = Path.Combine(Path.GetTempPath(), "engine-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "live" + DocumentFile.Extension);
            var writer = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
            DocumentFile.Create(writer, path);
            await writer.Apply(new NoOpCommand { Echo = "one" });
            writer.Close();
            var lines = File.ReadAllLines(path);
            var entry = JsonNode.Parse(lines[1])!;
            entry["command"]!["name"] = "TestLiveCancelling";
            entry["command"]!["parameters"] = new JsonObject();
            File.WriteAllLines(path, [lines[0], entry.ToJsonString()]);

            var commands = new CommandRegistry();
            var queries = new QueryRegistry();
            HandlerCatalog.RegisterAll(commands, queries);
            commands.Register(new LiveCancellingHandler());
            var session = new DocumentSession(new EngineKit(new Document(), commands, queries, new InMemoryEventSink(), new InProcessMeshBackend()));

            var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(session, path));

            Assert.Contains(nameof(TaskCanceledException), error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private sealed record LiveCancellingCommand : Command
    {
        public override string Name => "TestLiveCancelling";
        public override int SchemaVersion => 1;
    }

    // Throws a cancellation exception while the token of the caller is live, as the review agent of E22 did.
    private sealed class LiveCancellingHandler : ICommandHandler
    {
        public string CommandName => "TestLiveCancelling";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => new LiveCancellingCommand { CommandId = input.CommandId };

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
            => throw new TaskCanceledException("The handler gives up with no cancellation of the caller.");
    }

    private sealed record CancelOnceCommand : Command
    {
        public override string Name => "TestCancelOnce";
        public override int SchemaVersion => 1;
    }

    // The first call cancels the source of the caller and observes it; each later call succeeds.
    private sealed class CancelOnceHandler : ICommandHandler
    {
        public CancellationTokenSource? Cancel { get; set; }
        public int Calls { get; private set; }

        public string CommandName => "TestCancelOnce";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => new CancelOnceCommand { CommandId = input.CommandId };

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
        {
            Calls++;
            if (Calls == 1)
            {
                Cancel!.Cancel();
                ct.ThrowIfCancellationRequested();
            }
            return Task.FromResult(CommandHandlerResult.Success(new Outputs(new Dictionary<string, object?>())));
        }
    }
}
