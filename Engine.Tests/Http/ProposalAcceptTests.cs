using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Preview;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0188 (phase P16, TASK-0463): the accept after a preview. The accept compares the result of the commands at the version of
// its apply with the result that the person saw: at the version of that preview no fork runs (Q4 A); a different result applies
// nothing and keeps the proposal open with the new preview and the note W-API-PROPOSAL-RESULT-CHANGED; an equal result applies
// the commands as one group in one serial section, one step of undo; a commit between the comparison and the apply makes the
// accept compare again; a result that stops applies nothing. The checks of one preview share one bound of work (Q6 A).
// ADR-0188 names this file in its enforced-by field.
public sealed class ProposalAcceptTests
{
    // A host on the managed backend whose factory of forks counts its backends, and an optional action before each backend.
    private sealed class Served : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private int _forks;

        private Served(EngineHost host, WebApplication app, HttpClient http)
        {
            Host = host;
            _app = app;
            Http = http;
        }

        public EngineHost Host { get; private set; } = null!;

        public HttpClient Http { get; }

        public int Forks => Volatile.Read(ref _forks);

        // The action runs in the worker of the queue, before the factory gives a backend.
        public Action? BeforeFork { get; set; }

        public static async Task<Served> Start()
        {
            Served? served = null;
            var host = new EngineHost(new InProcessMeshBackend(), forks: () =>
            {
                Interlocked.Increment(ref served!._forks);
                served.BeforeFork?.Invoke();
                return new InProcessMeshBackend();
            });
            var folder = new DocumentFolder(Path.Combine(Path.GetTempPath(), "3de-accept-" + Guid.NewGuid().ToString("N")));
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSingleton(folder);
            builder.AddEngineSurface(host);
            var app = builder.Build();
            app.MapEngineSurface();
            await app.StartAsync();
            served = new Served(host, app, new HttpClient { BaseAddress = new Uri(app.Urls.First()) }) { Host = host };
            return served;
        }

        public async ValueTask DisposeAsync()
        {
            Http.Dispose();
            await _app.DisposeAsync();
            Host.Dispose();
        }
    }

    private static string Box(Guid commandId, double size = 10)
        => $"{{\"name\":\"CreateBox\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"sizeX\":{size},\"sizeY\":10,\"sizeZ\":10}}}}";

    private static string Rename(Guid bodyId, string name)
        => $"{{\"name\":\"RenameBody\",\"schemaVersion\":1,\"commandId\":\"{Guid.NewGuid()}\",\"parameters\":{{\"bodyId\":\"{bodyId}\",\"name\":{JsonSerializer.Serialize(name)}}}}}";

    private static async Task<Guid> Post(Served served, string summary, params string[] envelopes)
    {
        var json = $"{{\"summary\":{JsonSerializer.Serialize(summary)},\"commands\":[{string.Join(",", envelopes)}]}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await served.Http.PostAsync("/proposals", content);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"The post gave {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<ProposalPreview> Preview(Served served, Guid id)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await served.Host.Proposals.Preview(id, limit.Token) ?? throw new InvalidOperationException("No preview.");
    }

    private static async Task<Proposal> Accept(Served served, Guid id, long? seen)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await served.Host.Proposals.Accept(id, seen, limit.Token) ?? throw new InvalidOperationException("The proposal is not open.");
    }

    private static Task<Guid> Made(Served served, double size = 10)
        => Task.Run(async () => BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = size, SizeY = 10, SizeZ = 10 })));

    private static Task<string[]> Names(Served served)
        => served.Host.Session.Read((document, _) => document.Bodies.Select(body => body.Name ?? "-").Order(StringComparer.Ordinal).ToArray());

    private static Task<string[]> Log(Served served)
        => served.Host.Session.Read((document, _) => document.Log.Select(entry => entry.Command.Name).ToArray());

    // Item 5 (Q4 A) and item 3: an accept at the version of the preview that the person saw runs no fork, applies the commands
    // as one group, one entry each, and one Undo reverses the whole proposal. The kept preview stays the preview that applied.
    [Fact]
    public async Task An_Accept_At_The_Version_Of_The_Preview_Runs_No_Fork_And_Is_One_Step_Of_Undo()
    {
        await using var served = await Served.Start();
        var box = await Made(served);
        var made = Guid.NewGuid();
        var id = await Post(served, "A box and two names", Box(made), Rename(made, "New"), Rename(box, "Old"));
        var seen = await Preview(served, id);
        var forks = served.Forks;

        var accepted = await Accept(served, id, seen.DocumentVersion);

        Assert.Equal(ProposalStates.Accepted, accepted.State);
        Assert.Null(accepted.Held);
        Assert.Equal(forks, served.Forks);
        Assert.Equal([2L, 3L, 4L], accepted.Results.Select(result => result.DocumentVersion));
        Assert.All(await served.Host.Session.Read((document, _) => document.Log.Skip(1).Select(entry => entry.Command.GroupId).ToArray()), group => Assert.Equal(id, group));
        Assert.Equal(["New", "Old"], await Names(served));
        Assert.Same(seen, served.Host.Proposals.Kept(id)!.Preview);

        Assert.Equal(CommandStatus.Applied, (await served.Host.Session.Apply(new UndoCommand())).Status);
        Assert.Equal(["-"], await Names(served));
        Assert.Equal(CommandStatus.Applied, (await served.Host.Session.Apply(new RedoCommand())).Status);
        Assert.Equal(["New", "Old"], await Names(served));
    }

    // Item 6: a commit after the preview that changes the result applies nothing. The proposal is open again with the new
    // preview, the answer of GET /proposals/{id} gives the note in held, and the next accept of the new preview applies.
    [Fact]
    public async Task A_Commit_That_Changes_The_Result_Applies_Nothing_And_Shows_The_New_Preview()
    {
        await using var served = await Served.Start();
        var box = await Made(served);
        var id = await Post(served, "Rename the box", Rename(box, "Shelf"));
        var seen = await Preview(served, id);
        await BookcaseCase.Applied(served.Host.Session, new RenameBodyCommand { BodyId = box, NewName = "Plate" });

        var held = await Accept(served, id, seen.DocumentVersion);

        Assert.Equal(ProposalStates.Open, held.State);
        Assert.Equal(DiagnosticCodes.ApiProposalResultChanged, held.Held?.Code);
        Assert.Empty(held.Results);
        Assert.Equal(["CreateBox", "RenameBody"], await Log(served));
        var kept = served.Host.Proposals.Kept(id)!;
        Assert.Equal(seen.DocumentVersion + 1, kept.Preview.DocumentVersion);
        Assert.Equal("Plate", Assert.Single(kept.Preview.Bodies!.Changed).Before.Name);
        Assert.Equal(DiagnosticCodes.ApiProposalResultChanged, kept.Held);
        Assert.True(kept.Current);

        using (var response = await served.Http.GetAsync($"/proposals/{id}"))
        {
            var proposal = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("open", proposal.GetProperty("state").GetString());
            Assert.Equal(DiagnosticCodes.ApiProposalResultChanged, proposal.GetProperty("held").GetProperty("code").GetString());
        }

        var accepted = await Accept(served, id, kept.Preview.DocumentVersion);
        Assert.Equal(ProposalStates.Accepted, accepted.State);
        Assert.Null(accepted.Held);
        Assert.Equal(["Shelf"], await Names(served));
    }

    // Item 6 (Q4 A): a commit that does not change the result runs one new fork, and the accept applies. The fork of the version 2
    // waits until the accept is applying, so the accept reads the preview of the version 1, which the person saw, as the kept
    // preview (finding T197 of the codebase review of 2026-11-25, R-0078). A fork that ends before the accept reads it makes the
    // kept preview the preview of the version 2, and the accept then holds the proposal open, by ADR-0188 "Bad" (Q3 A).
    [Fact]
    public async Task A_Commit_That_Does_Not_Change_The_Result_Runs_One_Fork_And_Applies()
    {
        await using var served = await Served.Start();
        var box = await Made(served);
        var id = await Post(served, "Rename the box", Rename(box, "Shelf"));
        var seen = await Preview(served, id);
        var forks = served.Forks;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var once = 0;
        served.BeforeFork = () =>
        {
            if (Interlocked.Exchange(ref once, 1) == 0)
                release.Task.Wait(TimeSpan.FromSeconds(60));
        };
        await BookcaseCase.Applied(served.Host.Session, new NoOpCommand { Echo = "elsewhere" });

        var accept = Accept(served, id, seen.DocumentVersion);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (served.Host.Proposals.Find(id)?.State != ProposalStates.Applying && clock.Elapsed < TimeSpan.FromSeconds(60))
            await Task.Delay(10);
        release.SetResult();
        var accepted = await accept;

        Assert.Equal(ProposalStates.Accepted, accepted.State);
        Assert.Equal(seen.DocumentVersion + 2, Assert.Single(accepted.Results).DocumentVersion);
        Assert.Equal(forks + 1, served.Forks);
        Assert.Equal(["Shelf"], await Names(served));
    }

    // Item 6: with no preview that the person saw, the accept applies nothing and shows the preview; the overload with no
    // version compares with the last preview of the queue.
    [Fact]
    public async Task With_No_Preview_That_The_Person_Saw_The_Accept_Applies_Nothing()
    {
        await using var served = await Served.Start();
        var box = await Made(served);
        var id = await Post(served, "Rename the box", Rename(box, "Shelf"));
        await Preview(served, id);

        var held = await Accept(served, id, seen: null);
        Assert.Equal((ProposalStates.Open, DiagnosticCodes.ApiProposalResultChanged), (held.State, held.Held?.Code));
        Assert.Equal(["-"], await Names(served));

        var stale = await Accept(served, id, seen: 99);
        Assert.Equal(ProposalStates.Open, stale.State);

        Assert.Equal(ProposalStates.Accepted, (await served.Host.Proposals.Accept(id))!.State);
        Assert.Equal(["Shelf"], await Names(served));
    }

    // Item 7: a result that stops at a command applies nothing; the proposal is failed with the index and the code of the
    // preview, and the log does not change.
    [Fact]
    public async Task A_Result_That_Stops_Applies_Nothing()
    {
        await using var served = await Served.Start();
        await Made(served);
        var id = await Post(served, "A bad box", Box(Guid.NewGuid()), Box(Guid.NewGuid(), -1));
        var seen = await Preview(served, id);
        Assert.Equal(1, seen.FailedIndex);

        var failed = await Accept(served, id, seen.DocumentVersion);

        Assert.Equal((ProposalStates.Failed, 1, DiagnosticCodes.GeomInvalidParam), (failed.State, failed.FailedIndex, failed.FailedCode));
        Assert.Empty(failed.Results);
        Assert.Equal(["CreateBox"], await Log(served));
    }

    // Item 5: a commit that comes after the fork of the comparison and before the apply makes the group stale, so the accept
    // compares again at the new version. Here the fork of the version 2 waits until the accept waits for it; then another client
    // renames the box (version 3) and the fork ends. The result of the version 2 equals the result that the person saw, but the
    // apply at the version 2 is stale, and the result of the version 3 differs, so nothing applies. An apply that ignored the
    // version would rename the box to "Shelf".
    [Fact]
    public async Task A_Commit_Between_The_Comparison_And_The_Apply_Makes_The_Accept_Compare_Again()
    {
        await using var served = await Served.Start();
        var box = await Made(served);
        var id = await Post(served, "Rename the box", Rename(box, "Shelf"));
        var seen = await Preview(served, id);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var once = 0;
        served.BeforeFork = () =>
        {
            if (Interlocked.Exchange(ref once, 1) != 0)
                return;
            release.Task.Wait(TimeSpan.FromSeconds(30));
            BookcaseCase.Applied(served.Host.Session, new RenameBodyCommand { BodyId = box, NewName = "Plate" }).GetAwaiter().GetResult();
        };
        await BookcaseCase.Applied(served.Host.Session, new NoOpCommand { Echo = "version 2" });

        var accept = Accept(served, id, seen.DocumentVersion);
        // The accept reads the version 2 and waits for its fork, which waits for the release. The wait gives the accept the time
        // to start that wait; a later start reads the version 3, and the test still passes, with no stale apply.
        while (served.Host.Proposals.Find(id)?.State != ProposalStates.Applying)
            await Task.Delay(10);
        await Task.Delay(300);
        release.SetResult();
        var held = await accept;

        Assert.Equal((ProposalStates.Open, DiagnosticCodes.ApiProposalResultChanged), (held.State, held.Held?.Code));
        Assert.Equal(["Plate"], await Names(served));
        Assert.Equal(["CreateBox", "NoOp", "RenameBody"], await Log(served));
        Assert.Equal(seen.DocumentVersion + 2, served.Host.Proposals.Kept(id)!.Preview.DocumentVersion);
    }

    // Item 4: a proposal with an Undo stops at it in its preview, so its accept applies nothing.
    [Fact]
    public async Task A_Proposal_With_An_Undo_Stops_At_It_And_Applies_Nothing()
    {
        await using var served = await Served.Start();
        await Made(served);
        var id = await Post(served, "A box and an undo", Box(Guid.NewGuid()), "{\"name\":\"Undo\",\"schemaVersion\":1,\"parameters\":{}}");
        var seen = await Preview(served, id);

        var failed = await Accept(served, id, seen.DocumentVersion);

        Assert.Equal((ProposalStates.Failed, 1, DiagnosticCodes.CommandGroupMove), (failed.State, failed.FailedIndex, failed.FailedCode));
        Assert.Equal(["CreateBox"], await Log(served));
    }

    // Item 6: two previews of the same result at two versions compare equal: the version, the sequence, the time and a message
    // do not count. A record, an output or a code that changes makes them differ.
    [Fact]
    public void Two_Previews_Compare_By_Their_Content()
    {
        var body = Guid.NewGuid();
        CommandResult Result(long version, long seq, long time, string name, string message) => new(
            body, "RenameBody", CommandStatus.Applied, seq, version,
            new Outputs(new Dictionary<string, object?> { ["bodyId"] = body, ["name"] = name }),
            [new Diagnostic(Severity.Warning, "W-TEST", message)], null, time);
        ProposalPreview Of(long version, string name, string message = "a", string after = "Shelf") => new(
            Guid.Empty, PreviewStates.Ready, version, [Result(version + 1, version * 7, version * 3, name, message)], null, null,
            new BodyDifferences([], [], [new BodyChange(new BodyRecord(new BodyHandle(body), "Box"), new BodyRecord(new BodyHandle(body), "Box") { Name = after })]),
            [], [new MeasureChange(body, null, new BodyMeasure(null, new ErrorDetail("E-TEST", message)))],
            [new PairChange(body, Guid.Empty, null, new PairState(PairStates.Limit, null, new ErrorDetail(DiagnosticCodes.CheckWorkLimit, message)))], null);

        Assert.True(PreviewContent.Same(Of(3, "n"), Of(9, "n", "another message")));
        Assert.False(PreviewContent.Same(Of(3, "n"), Of(3, "m")));
        Assert.False(PreviewContent.Same(Of(3, "n"), Of(3, "n", after: "Plate")));
        Assert.False(PreviewContent.Same(Of(3, "n"), Of(3, "n") with { FailedIndex = 0 }));
        Assert.False(PreviewContent.Same(Of(3, "n"), Of(3, "n") with { Checks = [] }));
        Assert.False(PreviewContent.Same(Of(3, "n"), Of(3, "n") with { Sketches = [new SketchChange(body, null, new SketchState(SketchStates.Solved, 0, 0))] }));
    }

    // Item 6: the comparison names each member of the records of a preview. A new member fails this test, so the comparison
    // changes with it. "Not compared" is the list of ADR-0188 item 6: the version, the sequence, the time, and each message.
    [Fact]
    public void The_Comparison_Of_Two_Previews_Names_Each_Member()
    {
        var named = new Dictionary<Type, string[]>
        {
            [typeof(ProposalPreview)] = ["ProposalId", "State", "DocumentVersion", "Results", "FailedIndex", "FailedCode", "Bodies", "Sketches", "Measures", "Checks", "Error"],
            [typeof(CommandResult)] = ["CommandId", "CommandName", "Status", "AppliedAtSeq", "DocumentVersion", "Outputs", "Diagnostics", "Error", "DurationMs"],
            [typeof(Diagnostic)] = ["Severity", "Code", "Message", "Path", "Data"],
            [typeof(ErrorDetail)] = ["Code", "Message", "Cause", "Retriable"],
            [typeof(BodyDifferences)] = ["Added", "Removed", "Changed"],
            [typeof(BodyChange)] = ["Before", "After"],
            [typeof(SketchChange)] = ["SketchId", "Before", "After"],
            [typeof(SketchState)] = ["State", "Freedom", "Residual"],
            [typeof(MeasureChange)] = ["BodyId", "Before", "After"],
            [typeof(BodyMeasure)] = ["Mass", "Error"],
            [typeof(MassProperties)] = ["Volume", "Centre", "Facets"],
            [typeof(DatumPoint)] = ["X", "Y", "Z"],
            [typeof(PairChange)] = ["BodyIdA", "BodyIdB", "Before", "After"],
            [typeof(PairState)] = ["State", "Check", "Error"],
            [typeof(PairCheck)] = ["BodyIdA", "BodyIdB", "Class", "Volume", "Box", "Gap", "PointA", "PointB", "Intended", "Fault", "MeshOnly", "Basis"],
            [typeof(Aabb)] = ["MinX", "MinY", "MinZ", "MaxX", "MaxY", "MaxZ"],
        };
        foreach (var (type, names) in named)
            Assert.True(
                names.Order().SequenceEqual(type.GetProperties().Select(property => property.Name).Order()),
                $"{type.Name} has the members {string.Join(", ", type.GetProperties().Select(property => property.Name))}; PreviewContent names {string.Join(", ", names)} (ADR-0188 item 6).");

        // The records that PreviewContent compares with the equality of the record hold no list, so that equality compares content.
        foreach (var type in new[] { typeof(SketchChange), typeof(SketchState), typeof(MassProperties), typeof(PairCheck), typeof(Aabb), typeof(DatumPoint) })
            foreach (var property in type.GetProperties())
                Assert.False(
                    property.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType),
                    $"{type.Name}.{property.Name} is a {property.PropertyType.Name}: the equality of the record compares it by reference (ADR-0188 item 6).");
    }

    // Item 8 (Q6 A): the check gives the work that it counted before it splits and refuses a pair above the limit that it got;
    // the checks of one preview share one bound, and the first pair above the work that the bound leaves, and each pair after
    // it, has the state limit. The answer of the query gives no work to a client.
    [NativeManifoldFact]
    public async Task The_Checks_Of_One_Preview_Share_One_Bound_Of_Work()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(Engine.Core.Hosting.EngineHosting.CreateDefault(backend));
        try
        {
            var bodies = await BookcaseCase.Build(session);
            var pair = $"{bodies["Bas"]},{bodies["Haut"]}";
            var checkedPair = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = pair });
            var work = checkedPair.Result!.Work;
            Assert.True(work > 0);
            Assert.Null(typeof(InterferenceInfo).GetProperty("Work"));
            Assert.Null((await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = pair, WorkLimit = work })).Error);
            Assert.Equal(DiagnosticCodes.CheckWorkLimit, (await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = pair, WorkLimit = work - 1 })).Error?.Code);

            var source = await session.Read((document, _) => ForkSource.Of(document));
            var box = Guid.NewGuid();
            Command[] shelf =
            [
                new CreateBoxCommand { CommandId = box, SizeX = 1164, SizeY = 394, SizeZ = 18 },
                new TranslateCommand { BodyId = box, Dx = 300, Dy = -3, Dz = 1000 },
            ];
            Func<IGeometryBackend> backends = () => new ManifoldGeometryBackend();

            var whole = await DocumentFork.Run(source, shelf, backends, null, default, 0, PreviewWork.Most);
            Assert.DoesNotContain(whole.Checks, change => change.After?.State == PairStates.Limit);

            // Each pair of the new shelf with one of the 19 bodies costs the same work here, so a bound of five pairs checks five.
            var bounded = await DocumentFork.Run(source, shelf, backends, null, default, 0, 5 * work);
            var limited = bounded.Checks.Where(change => change.After?.State == PairStates.Limit).ToList();
            Assert.Equal(19 - 5, limited.Count);
            Assert.All(limited, change => Assert.Equal(DiagnosticCodes.CheckWorkLimit, change.After!.Error?.Code));
        }
        finally
        {
            session.Close();
        }
    }
}
