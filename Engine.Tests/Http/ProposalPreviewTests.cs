using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0185 (phase P15a, TASK-0456): the preview of a proposal. The host copies the log of the Document under the serial lock,
// replays the copy in a second session with its own backend off the lock, and applies the commands of the proposal there
// (Q2 A of the third codebase review of 2026-11-21). It computes the preview when the proposal arrives and again at each new
// version (Q3 A), and compares the bodies by content (Q5 A). ADR-0185 names this file in its enforced-by field.
public sealed class ProposalPreviewTests
{
    // A new managed backend, and a new native backend. The bookcase needs Translate, which only the native backend has.
    private static Func<IGeometryBackend> Managed => () => new InProcessMeshBackend();

    private static Func<IGeometryBackend> Native => () => new ManifoldGeometryBackend();

    private sealed class Served : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly DocumentFolder _folder;

        private Served(EngineHost host, WebApplication app, DocumentFolder folder, HttpClient http)
        {
            Host = host;
            _app = app;
            _folder = folder;
            Http = http;
        }

        public EngineHost Host { get; }

        public HttpClient Http { get; }

        // A host on the given backend, the managed backend by default, with the given forks.
        public static async Task<Served> Start(Func<IGeometryBackend>? forks, Func<IGeometryBackend>? backend = null)
        {
            var host = new EngineHost((backend ?? Managed)(), forks: forks);
            var folder = new DocumentFolder(Path.Combine(Path.GetTempPath(), "3de-preview-" + Guid.NewGuid().ToString("N")));
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSingleton(folder);
            builder.AddEngineSurface(host);
            var app = builder.Build();
            app.MapEngineSurface();
            await app.StartAsync();
            return new Served(host, app, folder, new HttpClient { BaseAddress = new Uri(app.Urls.First()) });
        }

        public async ValueTask DisposeAsync()
        {
            Http.Dispose();
            await _app.DisposeAsync();
            Host.Dispose();
            _ = _folder;
        }
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Translate(Guid commandId, Guid bodyId, double dx, double dy, double dz)
        => $"{{\"name\":\"Translate\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"bodyId\":\"{bodyId}\",\"dx\":{Number(dx)},\"dy\":{Number(dy)},\"dz\":{Number(dz)}}}}}";

    private static string Rename(Guid commandId, Guid bodyId, string name)
        => $"{{\"name\":\"RenameBody\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"bodyId\":\"{bodyId}\",\"name\":{JsonSerializer.Serialize(name)}}}}}";

    private static string Box(Guid commandId)
        => $"{{\"name\":\"CreateBox\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"sizeX\":10,\"sizeY\":10,\"sizeZ\":10}}}}";

    private static async Task<Guid> Post(Served served, string summary, params string[] envelopes)
    {
        var json = $"{{\"summary\":{JsonSerializer.Serialize(summary)},\"commands\":[{string.Join(",", envelopes)}]}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await served.Http.PostAsync("/proposals", content);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"The post gave {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    // The limit of 30 s only ends a test that would wait for ever, for example when no fork computes the preview; a pass does not
    // wait for it.
    private static async Task<ProposalPreview> Preview(Served served, Guid id)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ProposalPreview? preview;
        try
        {
            preview = await served.Host.Proposals.Preview(id, limit.Token);
        }
        catch (OperationCanceledException) when (limit.IsCancellationRequested)
        {
            Assert.Fail($"No preview of the proposal {id} came in 30 s.");
            throw;
        }

        Assert.NotNull(preview);
        return preview;
    }

    // The state of the first Document: its version, the entries of its log, its bodies and the count of its events.
    private sealed record State(long Version, LogEntry[] Log, BodyRecord[] Bodies, int Events);

    private static Task<State> Read(EngineHost host)
        => host.Session.Read((document, events) => new State(document.Version, [.. document.Log], [.. document.Bodies.OrderBy(body => body.Handle.Id)], events.Count));

    private static void Unchanged(State before, State after)
    {
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Log.Length, after.Log.Length);
        for (var i = 0; i < before.Log.Length; i++)
            Assert.Same(before.Log[i], after.Log[i]);
        Assert.Equal(before.Bodies, after.Bodies);
        Assert.Equal(before.Events, after.Events);
    }

    // Items 2 and 4: a proposal of three commands on the bookcase of K10a gives the outcome of each command and the bodies that
    // it adds, removes and changes. The first Document keeps its version, its log, its bodies and its events.
    [NativeManifoldFact]
    public async Task Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var before = await Read(served.Host);
        Assert.Equal(BookcaseCase.CommandCount, before.Version);

        var module = bodies["Module 1 - volume indicatif"];
        var bas = bodies["Bas"];
        var lifted = Guid.NewGuid();
        var id = await Post(
            served,
            "Lift module 1 and rename Bas",
            Translate(lifted, module, 0, 0, 10),
            Rename(Guid.NewGuid(), lifted, "Module 1 - lifted"),
            Rename(Guid.NewGuid(), bas, "Bas 2"));

        var preview = await Preview(served, id);

        Assert.Equal(id, preview.ProposalId);
        Assert.Equal(PreviewStates.Ready, preview.State);
        Assert.Null(preview.Error);
        Assert.Equal(before.Version, preview.DocumentVersion);
        Assert.Equal(["Translate", "RenameBody", "RenameBody"], preview.Results.Select(result => result.CommandName));
        Assert.All(preview.Results, result => Assert.Equal(CommandStatus.Applied, result.Status));
        Assert.Null(preview.FailedIndex);
        Assert.Null(preview.FailedCode);

        var differences = preview.Bodies!;
        var added = Assert.Single(differences.Added);
        Assert.Equal(lifted, added.Handle.Id);
        Assert.Equal("Module 1 - lifted", added.Name);
        Assert.Equal(module, Assert.Single(differences.Removed).Handle.Id);
        var changed = Assert.Single(differences.Changed);
        Assert.Equal(bas, changed.Before.Handle.Id);
        Assert.Equal(bas, changed.After.Handle.Id);
        Assert.Equal("Bas", changed.Before.Name);
        Assert.Equal("Bas 2", changed.After.Name);

        Unchanged(before, await Read(served.Host));
        var proposal = served.Host.Proposals.Find(id)!;
        Assert.Equal(ProposalStates.Open, proposal.State);
        Assert.Empty(proposal.Results);
    }

    // Item 2 and ADR-0092 item 6: a refused command gives its code, and the commands after it do not run, as in an accept. The
    // accept of the same proposal then gives the same outcomes.
    [NativeManifoldFact]
    public async Task A_Refused_Command_Gives_Its_Code_And_Stops_The_Rest_As_The_Accept_Does()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var before = await Read(served.Host);

        var id = await Post(
            served,
            "Rename two shelves",
            Rename(Guid.NewGuid(), bodies["Bas"], "Bas 2"),
            Translate(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 10),
            Rename(Guid.NewGuid(), bodies["Haut"], "Haut 2"));

        var preview = await Preview(served, id);

        Assert.Equal(PreviewStates.Ready, preview.State);
        Assert.Equal(2, preview.Results.Count);
        Assert.Equal(CommandStatus.Applied, preview.Results[0].Status);
        Assert.Equal(CommandStatus.Rejected, preview.Results[1].Status);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, preview.Results[1].Error?.Code);
        Assert.Equal(1, preview.FailedIndex);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, preview.FailedCode);
        Assert.Equal("Bas 2", Assert.Single(preview.Bodies!.Changed).After.Name);
        Assert.Empty(preview.Bodies.Added);
        Assert.Empty(preview.Bodies.Removed);
        Unchanged(before, await Read(served.Host));

        var accepted = await served.Host.Proposals.Accept(id);
        Assert.Equal(ProposalStates.Failed, accepted!.State);
        Assert.Equal(preview.FailedIndex, accepted.FailedIndex);
        Assert.Equal(preview.FailedCode, accepted.FailedCode);
        Assert.Equal(preview.Results.Select(result => (result.CommandName, result.Status)), accepted.Results.Select(result => (result.CommandName, result.Status)));
    }

    // Finding E198 and ADR-0092 item 6: a command whose identifier the log holds does not apply again. The preview stops there
    // with E-API-PROPOSAL-COMMAND-KNOWN and no result for that command, as the accept does.
    [NativeManifoldFact]
    public async Task A_Known_Command_Identifier_Stops_The_Preview_As_It_Stops_The_Accept()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var known = await served.Host.Session.Read((document, _) => document.Log[0].Command.CommandId);

        var id = await Post(served, "Rename Bas and make a known box", Rename(Guid.NewGuid(), bodies["Bas"], "Bas 2"), Box(known));
        var preview = await Preview(served, id);

        Assert.Equal(CommandStatus.Applied, Assert.Single(preview.Results).Status);
        Assert.Equal(1, preview.FailedIndex);
        Assert.Equal(DiagnosticCodes.ApiProposalCommandKnown, preview.FailedCode);

        var accepted = await served.Host.Proposals.Accept(id);
        Assert.Equal(preview.FailedIndex, accepted!.FailedIndex);
        Assert.Equal(preview.FailedCode, accepted.FailedCode);
    }

    // Item 3 (Q3 A): a new version of the Document computes the preview again on the new log. Here the person moves the shelf
    // that the proposal renames, so the new preview refuses the rename.
    [NativeManifoldFact]
    public async Task A_New_Version_Of_The_Document_Computes_The_Preview_Again()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var bas = bodies["Bas"];
        var id = await Post(served, "Rename Bas", Rename(Guid.NewGuid(), bas, "Bas 2"));

        var first = await Preview(served, id);
        Assert.Equal(BookcaseCase.CommandCount, first.DocumentVersion);
        Assert.Null(first.FailedIndex);
        Assert.Equal("Bas 2", Assert.Single(first.Bodies!.Changed).After.Name);

        await BookcaseCase.Applied(served.Host.Session, new TranslateCommand { BodyId = bas, Dx = 0, Dy = 0, Dz = 5 });

        var second = await Preview(served, id);
        Assert.Equal(BookcaseCase.CommandCount + 1, second.DocumentVersion);
        Assert.Equal(0, second.FailedIndex);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, second.FailedCode);
        Assert.Equal(CommandStatus.Rejected, Assert.Single(second.Results).Status);
        Assert.Empty(second.Bodies!.Changed);
        Assert.True(served.Host.Proposals.Find(id)!.Stale);
    }

    // Item 6: a fork sends no event. A subscriber of the host gets the events of the command of the person only, and the sink
    // of the first Document holds no event of a fork.
    [NativeManifoldFact]
    public async Task No_Event_Of_A_Fork_Reaches_A_Subscriber()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var received = new List<EventRecord>();
        using var subscription = served.Host.Subscribe(record =>
        {
            lock (received)
                received.Add(record);
        });
        var before = await Read(served.Host);

        var lifted = Guid.NewGuid();
        var first = await Post(served, "Lift module 1", Translate(lifted, bodies["Module 1 - volume indicatif"], 0, 0, 10), Rename(Guid.NewGuid(), lifted, "Lifted"));
        var second = await Post(served, "Rename Haut", Rename(Guid.NewGuid(), bodies["Haut"], "Haut 2"));
        await Preview(served, first);
        await Preview(served, second);
        lock (received)
            Assert.Empty(received);
        Unchanged(before, await Read(served.Host));

        var person = new RenameBodyCommand { BodyId = bodies["Fond"], NewName = "Fond 2" };
        await BookcaseCase.Applied(served.Host.Session, person);
        Assert.Equal(before.Version + 1, (await Preview(served, first)).DocumentVersion);
        Assert.Equal(before.Version + 1, (await Preview(served, second)).DocumentVersion);

        lock (received)
        {
            Assert.NotEmpty(received);
            Assert.All(received, record => Assert.Equal(person.CommandId, record.CauseCommandId));
        }
    }

    // Item 2 (Q2 A): the host copies the log under the serial lock and makes the fork off it. While a fork waits for its
    // backend, a command of the person applies. The limit of 30 s only ends a test that would wait for ever; a pass does not wait.
    [Fact]
    public async Task The_Fork_Runs_Off_The_Serial_Lock()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var gate = new ManualResetEventSlim(false);
        await using var served = await Served.Start(() =>
        {
            started.TrySetResult();
            gate.Wait();
            return new InProcessMeshBackend();
        });
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        var id = await Post(served, "Rename the box", Rename(Guid.NewGuid(), box, "Box"));
        var forked = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        if (forked != started.Task)
            gate.Set();
        Assert.True(forked == started.Task, "The post of a proposal started no fork.");

        var apply = served.Host.Session.Apply(new RenameBodyCommand { BodyId = box, NewName = "Person" });
        var done = await Task.WhenAny(apply, Task.Delay(TimeSpan.FromSeconds(30)));
        gate.Set();
        Assert.True(done == apply, "A command of the person waited for a fork: the fork holds the serial lock.");
        Assert.Equal(CommandStatus.Applied, (await apply).Status);

        var preview = await Preview(served, id);
        Assert.Equal(2, preview.DocumentVersion);
        Assert.Equal("Box", Assert.Single(preview.Bodies!.Changed).After.Name);
    }

    // Item 5: one preview for each proposal of the queue, so at most 8 open and 32 kept. A closed proposal keeps its last
    // preview and gets no new one; a proposal that leaves the queue takes its preview with it.
    [Fact]
    public async Task The_Queue_Keeps_One_Preview_For_Each_Proposal_And_A_Closed_Proposal_Keeps_Its_Last_Preview()
    {
        await using var served = await Served.Start(Managed);
        var proposals = served.Host.Proposals;
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));

        // The witness stays open, so the new versions copy the log again while the first proposal is closed.
        var first = await Post(served, "Rename the box A", Rename(Guid.NewGuid(), box, "A"));
        var witness = await Post(served, "Rename the box W", Rename(Guid.NewGuid(), box, "W"));
        Assert.Equal(1, (await Preview(served, first)).DocumentVersion);
        Assert.Equal(ProposalStates.Accepted, (await proposals.Accept(first))!.State);
        await BookcaseCase.Applied(served.Host.Session, new RenameBodyCommand { BodyId = box, NewName = "B" });
        Assert.Equal(3, (await Preview(served, witness)).DocumentVersion);
        var kept = await Preview(served, first);
        Assert.Equal(1, kept.DocumentVersion);
        Assert.Null(kept.FailedIndex);
        Assert.NotNull(proposals.Reject(witness));

        var open = new List<Guid>();
        for (var i = 0; i < Proposals.MostOpen; i++)
            open.Add(await Post(served, $"Open {i}", Rename(Guid.NewGuid(), box, $"Open {i}")));
        foreach (var id in open)
            Assert.Equal(3, (await Preview(served, id)).DocumentVersion);
        Assert.Equal(Proposals.MostOpen + 2, proposals.KeptPreviews);

        for (var i = 0; i < Proposals.MostKept; i++)
        {
            Assert.NotNull(proposals.Reject(open[0]));
            open.RemoveAt(0);
            var id = await Post(served, $"Next {i}", Rename(Guid.NewGuid(), box, $"Next {i}"));
            open.Add(id);
            await Preview(served, id);
            Assert.True(proposals.KeptPreviews <= Proposals.MostOpen + Proposals.MostKept, $"{proposals.KeptPreviews} previews are kept.");
        }

        Assert.Null(proposals.Find(first));
        Assert.Null(await proposals.Preview(first));
        Assert.Equal(Proposals.MostOpen + Proposals.MostKept, proposals.KeptPreviews);
    }

    // Item 3: GET /proposals/{id}/preview gives the preview as JSON; an identifier that no proposal has gives HTTP 404 with
    // E-API-PROPOSAL-NOT-FOUND, and a host with no backend for a fork gives HTTP 501 with E-API-NOT-SUPPORTED.
    [Fact]
    public async Task The_Route_Gives_The_Preview_And_Its_Refusals()
    {
        await using var served = await Served.Start(Managed);
        var made = Guid.NewGuid();
        var id = await Post(served, "Make a box", Box(made));

        using (var response = await served.Http.GetAsync($"/proposals/{id}/preview"))
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {text}");
            var body = JsonDocument.Parse(text).RootElement;
            Assert.Equal(id, body.GetProperty("proposalId").GetGuid());
            Assert.Equal("ready", body.GetProperty("state").GetString());
            Assert.Equal(0, body.GetProperty("documentVersion").GetInt64());
            Assert.Equal("Applied", body.GetProperty("results")[0].GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("failedIndex").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("error").ValueKind);
            var bodies = body.GetProperty("bodies");
            Assert.Equal(made, bodies.GetProperty("added")[0].GetProperty("handle").GetProperty("id").GetGuid());
            Assert.Equal(0, bodies.GetProperty("removed").GetArrayLength());
            Assert.Equal(0, bodies.GetProperty("changed").GetArrayLength());
        }

        foreach (var route in new[] { $"/proposals/{Guid.NewGuid()}/preview", "/proposals/not-a-guid/preview" })
        {
            using var missing = await served.Http.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal(DiagnosticCodes.ApiProposalNotFound, JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
        }

        await using var plain = await Served.Start(forks: null);
        var other = await Post(plain, "Make a box", Box(Guid.NewGuid()));
        using var refused = await plain.Http.GetAsync($"/proposals/{other}/preview");
        Assert.Equal(HttpStatusCode.NotImplemented, refused.StatusCode);
        Assert.Equal(DiagnosticCodes.ApiNotSupported, JsonDocument.Parse(await refused.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 2: a fork that cannot replay the log, here on a backend of another name or with no backend, gives a failed preview
    // with E-API-PREVIEW-FAILED and the cause, and no outcome.
    [Fact]
    public async Task A_Fork_That_Cannot_Replay_The_Log_Gives_A_Failed_Preview()
    {
        Func<IGeometryBackend>[] forks = [() => new NullGeometryBackend(), () => throw new InvalidOperationException("No backend for this fork.")];
        string[] causes = [DiagnosticCodes.IoBackendMismatch, "No backend for this fork."];
        for (var i = 0; i < forks.Length; i++)
        {
            await using var served = await Served.Start(forks[i]);
            var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
            var id = await Post(served, "Rename the box", Rename(Guid.NewGuid(), box, "Box"));

            var preview = await Preview(served, id);

            Assert.Equal(PreviewStates.Failed, preview.State);
            Assert.Equal(DiagnosticCodes.ApiPreviewFailed, preview.Error?.Code);
            Assert.Contains(causes[i], preview.Error!.Message, StringComparison.Ordinal);
            Assert.Equal(1, preview.DocumentVersion);
            Assert.Empty(preview.Results);
            Assert.Null(preview.Bodies);
        }
    }

    // Item 4 (Q5 A): the preview compares two body records with the equality of the record. That equality compares content only
    // while no member of a body record is a list, because a list compares by reference. A member with a list fails this test, so
    // the comparison of the preview changes with it.
    [Fact]
    public void A_Body_Record_Has_No_List_So_Its_Equality_Compares_Content()
    {
        foreach (var type in new[] { typeof(BodyRecord), typeof(BodyHandle), typeof(BodyFault) })
            foreach (var property in type.GetProperties())
                Assert.False(
                    property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType),
                    $"{type.Name}.{property.Name} is a {property.PropertyType.Name}: the equality of the record compares it by reference, not by content (ADR-0185 item 4).");
    }
}
