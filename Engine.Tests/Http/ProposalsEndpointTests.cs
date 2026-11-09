using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0092 (TASK-0326): the proposals of an agent. An agent posts a proposal of command envelopes; the queue holds it
// outside the log; only the person accepts or rejects it, through Proposals.Accept and Proposals.Reject, which no route
// calls. ADR-0092 names this file in its enforced-by field.
public sealed class ProposalsEndpointTests : IAsyncLifetime
{
    private readonly EngineHost _host = new(new InProcessMeshBackend());
    private readonly DocumentFolder _folder = new(Path.Combine(Path.GetTempPath(), "3de-proposals-" + Guid.NewGuid().ToString("N")));
    private WebApplication? _app;
    private HttpClient? _http;

    private HttpClient Http => _http!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(_folder);
        builder.AddEngineSurface(_host);
        _app = builder.Build();
        _app.MapEngineSurface();
        await _app.StartAsync();
        _http = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _http?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        _host.Dispose();
    }

    private static string Box(double x, double y = 10, double z = 10)
        => $"{{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{{\"sizeX\":{x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"sizeY\":{y},\"sizeZ\":{z}}}}}";

    private const string NoOp = "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"x\"}}";

    private static string Body(string summary, params string[] envelopes)
        => $"{{\"summary\":{JsonSerializer.Serialize(summary)},\"commands\":[{string.Join(",", envelopes)}]}}";

    private async Task<(HttpStatusCode Status, JsonElement Body)> Send(HttpMethod method, string route, string? json = null, string media = "application/json")
    {
        using var request = new HttpRequestMessage(method, route);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, media);
        using var response = await Http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private Task<(HttpStatusCode Status, JsonElement Body)> Post(string json) => Send(HttpMethod.Post, "/proposals", json);

    private async Task<Guid> Posted(string summary, params string[] envelopes)
    {
        var (status, body) = await Post(Body(summary, envelopes));
        Assert.True(status == HttpStatusCode.Created, $"The post gave {status}: {body}");
        return body.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> Read(Guid id)
    {
        var (status, body) = await Send(HttpMethod.Get, $"/proposals/{id}");
        Assert.Equal(HttpStatusCode.OK, status);
        return body;
    }

    private async Task<long> Version()
        => (await Send(HttpMethod.Get, "/document")).Body.GetProperty("version").GetInt64();

    private static string Code(JsonElement body) => body.GetProperty("error").GetProperty("code").GetString()!;

    // Item 2: a post gives HTTP 201 with the proposal, its address, and the envelopes as the agent wrote them; nothing enters
    // the log before an accept.
    [Fact]
    public async Task A_Post_Gives_201_With_The_Proposal_And_Nothing_Enters_The_Log()
    {
        var commandId = Guid.NewGuid();
        var envelope = $"{{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{{\"sizeX\":75,\"sizeY\":40,\"sizeZ\":10}},\"commandId\":\"{commandId}\",\"expectedDocumentVersion\":0}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/proposals") { Content = new StringContent(Body("Widen the bracket to 75 mm", envelope, NoOp), Encoding.UTF8, "application/json") };
        using var response = await Http.SendAsync(request);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/proposals/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Widen the bracket to 75 mm", body.GetProperty("summary").GetString());
        Assert.Equal(ProposalStates.Open, body.GetProperty("state").GetString());
        Assert.Equal(0, body.GetProperty("documentVersion").GetInt64());
        Assert.False(body.GetProperty("stale").GetBoolean());
        Assert.Equal(0, body.GetProperty("results").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("failedIndex").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("failedCode").ValueKind);
        var commands = body.GetProperty("commands");
        Assert.Equal(2, commands.GetArrayLength());
        Assert.Equal("CreateBox", commands[0].GetProperty("name").GetString());
        Assert.Equal(1, commands[0].GetProperty("schemaVersion").GetInt32());
        Assert.Equal(75, commands[0].GetProperty("parameters").GetProperty("sizeX").GetDouble());
        Assert.Equal(commandId, commands[0].GetProperty("commandId").GetGuid());
        Assert.Equal(0, commands[0].GetProperty("expectedDocumentVersion").GetInt64());
        Assert.NotEqual(Guid.Empty, commands[1].GetProperty("commandId").GetGuid());
        Assert.Equal(JsonValueKind.Null, commands[1].GetProperty("expectedDocumentVersion").ValueKind);

        Assert.Equal(0, await Version());
        Assert.Equal(0, await _host.Session.Read((document, events) => document.Bodies.Count + events.Count));
        Assert.Equal(ProposalStates.Open, (await Read(id)).GetProperty("state").GetString());
    }

    // Item 2: the post checks each envelope as POST /commands does, and a refusal is HTTP 400 with E-API-BAD-REQUEST and
    // the index of the envelope. The queue does not change.
    [Theory]
    [InlineData("{\"name\":\"NoSuchCommand\",\"schemaVersion\":1,\"parameters\":{}}")]
    [InlineData("{\"name\":\"CreateBox\",\"schemaVersion\":99,\"parameters\":{\"sizeX\":1,\"sizeY\":1,\"sizeZ\":1}}")]
    [InlineData("{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":1,\"sizeY\":1}}")]
    [InlineData("{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":\"wide\",\"sizeY\":1,\"sizeZ\":1}}")]
    [InlineData("{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":1,\"sizeY\":1,\"sizeZ\":1,\"colour\":1}}")]
    [InlineData("{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"x\"},\"expectedDocumentVersoin\":3}")]
    [InlineData("{\"schemaVersion\":1,\"parameters\":{\"echo\":\"x\"}}")]
    [InlineData("{\"name\":\"NoOp\",\"parameters\":{\"echo\":\"x\"}}")]
    [InlineData("[1,2]")]
    public async Task A_Bad_Envelope_Gives_400_With_Its_Index(string bad)
    {
        var (status, body) = await Post(Body("Two steps", NoOp, bad, NoOp));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, Code(body));
        Assert.Equal(1, body.GetProperty("error").GetProperty("index").GetInt32());
        Assert.StartsWith("commands[1]: ", body.GetProperty("error").GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Empty(_host.Proposals.Waiting());

        // The control: the same proposal with a good envelope is accepted.
        Assert.Equal(HttpStatusCode.Created, (await Post(Body("Two steps", NoOp, NoOp, NoOp))).Status);
    }

    // Item 2: the summary has 1 to 120 characters, and the list 1 to 16 envelopes; an unknown member, a body that is not
    // JSON and a body that is not well-formed are refused.
    [Fact]
    public async Task The_Summary_And_The_Count_Have_Limits()
    {
        var sixteen = Enumerable.Repeat(NoOp, Proposals.MostCommands).ToArray();
        Assert.Equal(HttpStatusCode.Created, (await Post(Body(new string('a', Proposals.MostSummary), NoOp))).Status);
        Assert.Equal(HttpStatusCode.Created, (await Post(Body("Sixteen", sixteen))).Status);

        string[] refused =
        [
            Body(new string('a', Proposals.MostSummary + 1), NoOp),
            Body(string.Empty, NoOp),
            Body("Two\nlines", NoOp),
            "{\"commands\":[" + NoOp + "]}",
            "{\"summary\":\"No list\"}",
            Body("None"),
            Body("Seventeen", [.. sixteen, NoOp]),
            "{\"summary\":\"Extra\",\"commands\":[" + NoOp + "],\"author\":\"agent\"}",
            "{\"summary\":\"Broken\",\"commands\":[",
            "{\"summary\":\"\\uD800\",\"commands\":[" + NoOp + "]}",

            // A repeated member is a malformed body (finding E101), so the refusal names no envelope.
            Body("Repeated", "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"x\",\"echo\":\"y\"}}"),
        ];
        foreach (var json in refused)
        {
            var (status, body) = await Post(json);
            Assert.True(status == HttpStatusCode.BadRequest, $"The body {json} gave {status}.");
            Assert.Equal(DiagnosticCodes.ApiBadRequest, Code(body));
        }

        var (media, mediaBody) = await Send(HttpMethod.Post, "/proposals", Body("Text", NoOp), "text/plain");
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, media);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, Code(mediaBody));
        Assert.Equal(2, _host.Proposals.Waiting().Count);
    }

    // Items 2 and 3: GET /proposals gives the open proposals, the oldest first; GET /proposals/{id} gives one, and an
    // unknown identifier gives HTTP 404 with E-API-PROPOSAL-NOT-FOUND.
    [Fact]
    public async Task The_Queue_Gives_The_Oldest_First()
    {
        var ids = new[] { await Posted("First", NoOp), await Posted("Second", NoOp), await Posted("Third", NoOp) };

        var (status, list) = await Send(HttpMethod.Get, "/proposals");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(ids, list.EnumerateArray().Select(proposal => proposal.GetProperty("id").GetGuid()));
        Assert.Equal(["First", "Second", "Third"], list.EnumerateArray().Select(proposal => proposal.GetProperty("summary").GetString()));
        Assert.Equal("Second", (await Read(ids[1])).GetProperty("summary").GetString());

        // A closed proposal leaves the list, and GET still gives it.
        Assert.NotNull(_host.Proposals.Reject(ids[0]));
        Assert.Equal(ids[1..], (await Send(HttpMethod.Get, "/proposals")).Body.EnumerateArray().Select(proposal => proposal.GetProperty("id").GetGuid()));
        Assert.Equal(ProposalStates.Rejected, (await Read(ids[0])).GetProperty("state").GetString());

        foreach (var unknown in new[] { Guid.NewGuid().ToString(), "not-a-guid" })
        {
            var (missing, body) = await Send(HttpMethod.Get, $"/proposals/{unknown}");
            Assert.Equal(HttpStatusCode.NotFound, missing);
            Assert.Equal(DiagnosticCodes.ApiProposalNotFound, Code(body));
        }
    }

    // Item 3: a ninth open proposal gives HTTP 409 with E-API-PROPOSAL-QUEUE-FULL; a closed proposal makes room.
    [Fact]
    public async Task A_Ninth_Open_Proposal_Gives_409()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < Proposals.MostOpen; i++)
            ids.Add(await Posted($"Step {i}", NoOp));

        var (status, body) = await Post(Body("Ninth", NoOp));
        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(DiagnosticCodes.ApiProposalQueueFull, Code(body));
        Assert.Equal(Proposals.MostOpen, _host.Proposals.Waiting().Count);

        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Delete, $"/proposals/{ids[3]}")).Status);
        Assert.Equal(HttpStatusCode.Created, (await Post(Body("Ninth", NoOp))).Status);
    }

    // Item 3: the queue keeps the last 32 closed proposals, so an agent reads the outcome; an older one is gone.
    [Fact]
    public async Task The_Queue_Keeps_The_Last_32_Closed_Proposals()
    {
        var ids = new List<Guid>();
        for (var i = 0; i <= Proposals.MostKept; i++)
        {
            ids.Add(await Posted($"Step {i}", NoOp));
            Assert.NotNull(_host.Proposals.Reject(ids[^1]));
        }

        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"/proposals/{ids[0]}")).Status);
        Assert.Equal(ProposalStates.Rejected, (await Read(ids[1])).GetProperty("state").GetString());
        Assert.Equal(ProposalStates.Rejected, (await Read(ids[^1])).GetProperty("state").GetString());
    }

    // Items 2 and 3: DELETE withdraws an open proposal; a closed proposal gives HTTP 409 with E-API-PROPOSAL-CLOSED, and an
    // unknown one HTTP 404.
    [Fact]
    public async Task A_Withdraw_Of_A_Closed_Proposal_Gives_409()
    {
        var open = await Posted("Open", NoOp);
        var (withdrawn, body) = await Send(HttpMethod.Delete, $"/proposals/{open}");
        Assert.Equal(HttpStatusCode.OK, withdrawn);
        Assert.Equal(ProposalStates.Withdrawn, body.GetProperty("state").GetString());
        Assert.Empty(_host.Proposals.Waiting());
        Assert.Null(await _host.Proposals.Accept(open));

        var rejected = await Posted("Rejected", NoOp);
        _host.Proposals.Reject(rejected);
        foreach (var closed in new[] { open, rejected })
        {
            var (status, refusal) = await Send(HttpMethod.Delete, $"/proposals/{closed}");
            Assert.Equal(HttpStatusCode.Conflict, status);
            Assert.Equal(DiagnosticCodes.ApiProposalClosed, Code(refusal));
        }

        var (missing, missingBody) = await Send(HttpMethod.Delete, $"/proposals/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing);
        Assert.Equal(DiagnosticCodes.ApiProposalNotFound, Code(missingBody));
        Assert.Equal(0, await Version());
    }

    // Item 2: no route accepts or rejects. The surface maps four routes under /proposals, and each other method or path
    // leaves the proposal open.
    [Fact]
    public async Task No_Route_Accepts_Or_Rejects()
    {
        var routes = _app!.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/').StartsWith("proposals", StringComparison.OrdinalIgnoreCase))
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["*"])
                .Select(method => $"{method} /{endpoint.RoutePattern.RawText!.TrimStart('/')}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["DELETE /proposals/{id}", "GET /proposals", "GET /proposals/{id}", "POST /proposals"], routes);

        var id = await Posted("Wait for the person", Box(20));
        foreach (var (method, route) in new[]
        {
            (HttpMethod.Post, $"/proposals/{id}"),
            (HttpMethod.Put, $"/proposals/{id}"),
            (HttpMethod.Patch, $"/proposals/{id}"),
            (HttpMethod.Post, $"/proposals/{id}/accept"),
            (HttpMethod.Post, $"/proposals/{id}/reject"),
            (HttpMethod.Post, "/proposals/accept"),
        })
        {
            var (status, _) = await Send(method, route, $"{{\"id\":\"{id}\",\"state\":\"accepted\"}}");
            Assert.True(status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed, $"{method} {route} gave {status}.");
        }

        Assert.Equal(ProposalStates.Open, (await Read(id)).GetProperty("state").GetString());
        Assert.Equal(0, await Version());
    }

    // Item 6: an accept applies the envelopes in their order, as the commands of a person, and gives one step of undo for
    // each command.
    [Fact]
    public async Task An_Accept_Applies_In_Order_And_Gives_N_Steps_Of_Undo()
    {
        var id = await Posted("Two boxes", Box(10), Box(20));
        var posted = (await Read(id)).GetProperty("commands").EnumerateArray().Select(command => command.GetProperty("commandId").GetGuid()).ToArray();

        var accepted = await _host.Proposals.Accept(id);

        Assert.NotNull(accepted);
        Assert.Equal(ProposalStates.Accepted, accepted.State);
        Assert.Equal(posted, accepted.Results.Select(result => result.CommandId));
        Assert.All(accepted.Results, result => Assert.Equal(CommandStatus.Applied, result.Status));
        Assert.True(accepted.Results[0].AppliedAtSeq < accepted.Results[1].AppliedAtSeq);
        Assert.Equal([1L, 2L], accepted.Results.Select(result => result.DocumentVersion));
        Assert.Equal(2, await Version());
        Assert.Empty(_host.Proposals.Waiting());

        var read = await Read(id);
        Assert.Equal(ProposalStates.Accepted, read.GetProperty("state").GetString());
        Assert.Equal(2, read.GetProperty("results").GetArrayLength());
        Assert.Equal("Applied", read.GetProperty("results")[1].GetProperty("status").GetString());

        // Two steps of undo, and no third one.
        string[] undo = ["{\"name\":\"Undo\",\"schemaVersion\":1,\"parameters\":{}}"];
        for (var i = 0; i < 2; i++)
            Assert.Equal("Applied", (await Send(HttpMethod.Post, "/commands", undo[0])).Body.GetProperty("status").GetString());
        var third = (await Send(HttpMethod.Post, "/commands", undo[0])).Body;
        Assert.Equal(DiagnosticCodes.CommandNothingToUndo, third.GetProperty("error").GetProperty("code").GetString());

        // A second accept of a closed proposal does nothing.
        Assert.Null(await _host.Proposals.Accept(id));
    }

    // Item 6: the apply stops at the first command that the engine does not apply; the commands before it stay applied,
    // and the proposal is failed with the index and the code of that command.
    [Fact]
    public async Task An_Accept_Stops_At_The_First_Rejected_Command()
    {
        var id = await Posted("A bad box in the middle", Box(10), Box(-1), Box(30));

        var failed = await _host.Proposals.Accept(id);

        Assert.NotNull(failed);
        Assert.Equal(ProposalStates.Failed, failed.State);
        Assert.Equal(1, failed.FailedIndex);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, failed.FailedCode);
        Assert.Equal([CommandStatus.Applied, CommandStatus.Rejected], failed.Results.Select(result => result.Status));
        Assert.Equal(1, await Version());
        Assert.Equal(1, await _host.Session.Read((document, _) => document.Bodies.Count));

        var read = await Read(id);
        Assert.Equal(ProposalStates.Failed, read.GetProperty("state").GetString());
        Assert.Equal(1, read.GetProperty("failedIndex").GetInt32());
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, read.GetProperty("failedCode").GetString());
    }

    // Items 4 and 6: an envelope with expectedDocumentVersion guards itself, so a stale envelope stops the accept there.
    [Fact]
    public async Task An_Envelope_With_An_Expected_Version_Guards_Itself()
    {
        var guarded = "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"x\"},\"expectedDocumentVersion\":0}";
        var id = await Posted("Guarded", guarded);
        Assert.Equal("Applied", (await Send(HttpMethod.Post, "/commands", NoOp)).Body.GetProperty("status").GetString());

        var failed = await _host.Proposals.Accept(id);

        Assert.Equal((ProposalStates.Failed, 0, DiagnosticCodes.CommandVersionStale), (failed!.State, failed.FailedIndex, failed.FailedCode));
        Assert.Equal(1, await Version());
    }

    // Item 6: a reject closes the proposal with no command.
    [Fact]
    public async Task A_Reject_Applies_Nothing()
    {
        var id = await Posted("Two boxes", Box(10), Box(20));

        var rejected = _host.Proposals.Reject(id);

        Assert.NotNull(rejected);
        Assert.Equal(ProposalStates.Rejected, rejected.State);
        Assert.Empty(rejected.Results);
        Assert.Equal(0, await Version());
        Assert.Equal(0, await _host.Session.Read((document, events) => document.Bodies.Count + events.Count));
        Assert.Null(_host.Proposals.Reject(id));
        Assert.Null(await _host.Proposals.Accept(id));
    }

    // Item 4: a command that applies after the post marks each open proposal as stale; a later proposal is not stale, and a
    // rejected command changes no version and marks nothing.
    [Fact]
    public async Task A_Command_After_The_Post_Marks_The_Proposal_Stale()
    {
        var first = await Posted("First", NoOp);
        Assert.False((await Read(first)).GetProperty("stale").GetBoolean());

        Assert.Equal("Rejected", (await Send(HttpMethod.Post, "/commands", Box(-1))).Body.GetProperty("status").GetString());
        Assert.False((await Read(first)).GetProperty("stale").GetBoolean());

        Assert.Equal("Applied", (await Send(HttpMethod.Post, "/commands", NoOp)).Body.GetProperty("status").GetString());
        var second = await Posted("Second", NoOp);

        var read = await Read(first);
        Assert.True(read.GetProperty("stale").GetBoolean());
        Assert.Equal(0, read.GetProperty("documentVersion").GetInt64());
        var later = await Read(second);
        Assert.False(later.GetProperty("stale").GetBoolean());
        Assert.Equal(1, later.GetProperty("documentVersion").GetInt64());

        // The accept of the first one changes the Document after the second one.
        Assert.Equal(ProposalStates.Accepted, (await _host.Proposals.Accept(first))!.State);
        Assert.True(_host.Proposals.Waiting().Single().Stale);
    }

    // Item 6: while the commands of an accept apply, the state is applying; a second accept, a reject and a withdraw are
    // refused, and GET still lists the proposal.
    [Fact]
    public async Task A_Proposal_That_Applies_Refuses_A_Second_Accept()
    {
        var id = await Posted("Held", Box(10));
        using var gate = new ManualResetEventSlim();
        using var entered = new ManualResetEventSlim();
        var hold = Task.Run(() => _host.Session.Read((_, _) =>
        {
            entered.Set();
            gate.Wait();
            return 0;
        }));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));

        var accept = _host.Proposals.Accept(id);
        try
        {
            Assert.Equal(ProposalStates.Applying, _host.Proposals.Find(id)!.State);
            Assert.Null(await _host.Proposals.Accept(id));
            Assert.Null(_host.Proposals.Reject(id));
            var (status, body) = await Send(HttpMethod.Delete, $"/proposals/{id}");
            Assert.Equal(HttpStatusCode.Conflict, status);
            Assert.Equal(DiagnosticCodes.ApiProposalClosed, Code(body));
            Assert.Equal(ProposalStates.Applying, (await Send(HttpMethod.Get, "/proposals")).Body[0].GetProperty("state").GetString());
        }
        finally
        {
            gate.Set();
        }

        await hold;
        Assert.Equal(ProposalStates.Accepted, (await accept)!.State);
    }

    // ADR-0030 item 4: after an accepted open, the old document takes no proposal, as it takes no command.
    [Fact]
    public async Task A_Post_After_An_Accepted_Open_Gives_503()
    {
        _folder.AcceptRequest();

        var (status, body) = await Post(Body("Too late", NoOp));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal(DiagnosticCodes.ApiOpenPending, Code(body));
        Assert.Empty(_host.Proposals.Waiting());
    }
}
