using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry; // NativeManifoldFact
using Engine.Tests.Governance;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Guides;

// Row P17 (TASK-0439, ADR-0157 item 13; answer Q3 A of the second codebase review of 2026-11-21): the guide for an agent,
// docs/guides/agent-loop.md, stays true because this test sends each of its examples to a host, in the order of the guide.
//
// The guide marks each example with the info string of a fenced block:
// - "http agent-request": a request that the host must accept. The first line is the method and the route, header lines
//   follow, then an empty line and the body.
// - "http agent-refusal": a request that the host must refuse.
// - "json agent-result" (or "text agent-result"): the shape of the answer of the request above it (or the start of an answer
//   that is not JSON).
// - "text agent-person": an action of the person in the window, "accept <id>" or "reject <id>". No route does it, so the test
//   calls Proposals.Accept or Proposals.Reject, as the window does.
// A text between < and > is a placeholder: the first answer that gives it binds it, a request uses the bound value, and a
// placeholder that no answer gave is a new UUID.
public sealed class AgentGuideExamplesTests
{
    private const string GuidePath = "docs/guides/agent-loop.md";

    private static readonly Regex Fence = new(@"^```(.*)$", RegexOptions.Compiled);
    private static readonly Regex OtherFence = new(@"^(\s+```|\s*~~~)", RegexOptions.Compiled);
    private static readonly Regex WholePlaceholder = new("^<([^<>\"]+)>$", RegexOptions.Compiled);
    private static readonly Regex QuotedPlaceholder = new("\"<([^<>\"]+)>\"", RegexOptions.Compiled);
    private static readonly Regex Placeholder = new("<([^<>\"]+)>", RegexOptions.Compiled);

    // One fenced block of the guide: the line of its fence, its language, its marker, and its text.
    internal sealed record Block(int Line, string Language, string Marker, string Text);

    // One step of the guide: a request, a refusal or an action of the person, with the block of its result.
    internal sealed record Step(Block Block, Block? Result);

    internal static string ReadGuide() => File.ReadAllText(RepositoryFiles.Path(GuidePath)).Replace("\r\n", "\n");

    internal static List<Block> Blocks(string markdown)
    {
        var blocks = new List<Block>();
        var lines = markdown.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            // TASK-0449 (finding T190): Markdown shows an indented fence and a fence of tildes as a block too, so the test
            // refuses them and does not skip a block that it does not send.
            Assert.False(OtherFence.IsMatch(lines[i]), $"{GuidePath}, line {i + 1}: the fence is indented or of tildes. Each fenced block of the guide starts with three backticks at the start of a line, so the test reads it.");
            var open = Fence.Match(lines[i]);
            if (!open.Success)
                continue;
            var info = open.Groups[1].Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var start = i;
            var text = new StringBuilder();
            for (i++; i < lines.Length && !lines[i].StartsWith("```", StringComparison.Ordinal); i++)
                text.Append(lines[i]).Append('\n');
            Assert.True(i < lines.Length, $"{GuidePath}, line {start + 1}: the fenced block has no end.");
            blocks.Add(new Block(start + 1, info.ElementAtOrDefault(0) ?? "", info.ElementAtOrDefault(1) ?? "", text.ToString().TrimEnd('\n')));
        }

        return blocks;
    }

    // The steps of the guide in their order. Each block is marked; a request and a refusal each have a result block directly
    // after them; a result block follows a request or a refusal.
    internal static List<Step> Steps(IReadOnlyList<Block> blocks)
    {
        var steps = new List<Step>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var where = $"{GuidePath}, line {block.Line}";
            switch (block.Marker)
            {
                case "agent-request" or "agent-refusal":
                    Assert.True(block.Language == "http", $"{where}: a request is a block \"http {block.Marker}\", not \"{block.Language}\".");
                    var result = i + 1 < blocks.Count ? blocks[i + 1] : null;
                    Assert.True(result is { Marker: "agent-result" }, $"{where}: the request has no block \"json agent-result\" or \"text agent-result\" directly after it.");
                    Assert.True(result!.Language is "json" or "text", $"{GuidePath}, line {result.Line}: a result block is \"json\" or \"text\", not \"{result.Language}\".");
                    steps.Add(new Step(block, result));
                    i++;
                    break;
                case "agent-person":
                    steps.Add(new Step(block, null));
                    break;
                case "agent-result":
                    Assert.Fail($"{where}: a result block follows no block \"http agent-request\" or \"http agent-refusal\".");
                    break;
                default:
                    Assert.Fail($"{where}: the block \"{block.Language} {block.Marker}\" has no marker of an example. Each fenced block of the guide is a request, a refusal, a result or an action of the person, so the test sends each one.");
                    break;
            }
        }

        return steps;
    }

    [Fact]
    public void Each_Block_Of_The_Guide_Is_A_Marked_Example_And_The_Loop_Has_Each_Route()
    {
        var steps = Steps(Blocks(ReadGuide()));
        var requests = steps.Where(step => step.Block.Marker == "agent-request").Select(step => step.Block.Text.Split('\n')[0]).ToList();

        // The loop of the guide: find the surface, read, measure, propose, the person, check, and the picture.
        string[] routes = ["GET /schema/", "GET /document", "GET /events", "POST /queries", "POST /proposals", "GET /proposals/", "GET /export/svg?sheetId="];
        foreach (var route in routes)
            Assert.True(requests.Any(line => line.StartsWith(route, StringComparison.Ordinal)), $"{GuidePath} has no request that starts with \"{route}\".");
        Assert.Contains(steps, step => step.Block.Marker == "agent-person" && step.Block.Text.StartsWith("accept ", StringComparison.Ordinal));
        Assert.Contains(steps, step => step.Block.Marker == "agent-refusal");
        // Row P15a (TASK-0456): the preview of a proposal is a step of the loop, before the person decides.
        Assert.True(requests.Any(line => line.StartsWith("GET /proposals/", StringComparison.Ordinal) && line.EndsWith("/preview", StringComparison.Ordinal)), $"{GuidePath} has no request of the preview of a proposal.");
        // Row P15b (TASK-0459): the result of the preview shows the state of the solver, the measures and the checks.
        var preview = steps.First(step => step.Block.Marker == "agent-request" && step.Block.Text.Split('\n')[0].StartsWith("GET /proposals/", StringComparison.Ordinal) && step.Block.Text.Split('\n')[0].EndsWith("/preview", StringComparison.Ordinal));
        var shape = JsonDocument.Parse(preview.Result!.Text).RootElement;
        foreach (var member in new[] { "sketches", "measures", "checks" })
            Assert.True(shape.TryGetProperty(member, out _), $"{GuidePath}, line {preview.Result.Line}: the result of the preview has no member \"{member}\".");
        Assert.NotEqual(0, shape.GetProperty("measures").GetArrayLength());
        Assert.NotEqual(0, shape.GetProperty("checks").GetArrayLength());
        // Row K17 (TASK-0443): the connectors of a body are a step of the measure.
        foreach (var query in new[] { "GetBodyInfo", "GetDistance", "GetMassProperties", "GetConnectors", "CheckInterference", "GetViewLines" })
            Assert.Contains(steps, step => step.Block.Marker == "agent-request" && step.Block.Text.Contains($"\"name\": \"{query}\"", StringComparison.Ordinal));
    }

    // TASK-0449 (finding T190 of the third codebase review of 2026-11-21): the test reads a fence of three backticks at the
    // start of a line only. An indented fence or a fence of tildes is a block that Markdown shows and the test would not send,
    // so the read of the guide fails on it.
    [Theory]
    [InlineData("Text.\n\n  ```http agent-request\n  GET /document\n  ```\n")]
    [InlineData("Text.\n\n~~~http agent-request\nGET /document\n~~~\n")]
    public void A_Fence_That_The_Test_Does_Not_Read_Fails_The_Read_Of_The_Guide(string markdown)
    {
        var error = Assert.ThrowsAny<Exception>(() => Blocks(markdown));
        Assert.Contains("line 3", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Shape_Of_A_Result_Refuses_A_Missing_Member_A_Wrong_Value_An_Order_And_A_Changed_Placeholder()
    {
        static JsonElement J(string text) => JsonDocument.Parse(text).RootElement.Clone();
        var bound = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var actual = J("""{ "a": 1, "b": "x", "list": [ { "k": 1 }, { "k": 2 }, { "k": 3 } ], "none": [], "id": "u" }""");

        Assert.Null(Match(J("""{ "a": 1, "list": [ { "k": 1 }, { "k": 3 } ], "none": [], "id": "<id>" }"""), actual, bound, "$"));
        Assert.Equal("u", bound["id"].GetString());
        Assert.NotNull(Match(J("""{ "c": 1 }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "a": 2 }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "b": "y" }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "list": [ { "k": 3 }, { "k": 1 } ] }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "list": [] }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "b": "<id>" }"""), actual, bound, "$"));
        Assert.NotNull(Match(J("""{ "a": null }"""), actual, bound, "$"));

        // A request takes the value of a bound placeholder, with its type in a whole string, and a new UUID for a new one.
        var json = Substitute("""{ "v": "<n>", "ids": "<id>,<id>", "new": "<fresh>" }""", new Dictionary<string, JsonElement>(StringComparer.Ordinal) { ["n"] = J("7"), ["id"] = J("\"u\"") });
        var sent = J(json);
        Assert.Equal(7, sent.GetProperty("v").GetInt32());
        Assert.Equal("u,u", sent.GetProperty("ids").GetString());
        Assert.True(Guid.TryParse(sent.GetProperty("new").GetString(), out _), json);
    }

    [NativeManifoldFact]
    public async Task Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape()
    {
        var steps = Steps(Blocks(ReadGuide()));

        // ADR-0185 item 2 (TASK-0456): the host makes the preview of a proposal on a fork with its own native backend, as the
        // desktop does.
        using var host = new EngineHost(new ManifoldGeometryBackend(), forks: () => new ManifoldGeometryBackend());
        var folder = new DocumentFolder(Path.Combine(Path.GetTempPath(), "3de-guide-" + Guid.NewGuid().ToString("N")));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(folder);
        builder.AddEngineSurface(host);
        await using var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        var address = new Uri(app.Urls.First());
        using var http = new HttpClient { BaseAddress = address };

        // The example document of section 3 of the guide: the bookcase of K10a and one sheet with a front view.
        await BookcaseCase.Build(host.Session);
        var sheet = await BookcaseCase.Applied(host.Session, new CreateSheetCommand { SheetName = "Bookcase" });
        Assert.True(sheet.Outputs.TryGet<Guid>("sheetId", out var sheetId), "CreateSheet gave no output sheetId.");
        await BookcaseCase.Applied(host.Session, new AddSheetViewCommand
        {
            SheetId = sheetId,
            RightX = 1, RightY = 0, RightZ = 0, UpX = 0, UpY = 0, UpZ = 1, EyeX = 0, EyeY = -1, EyeZ = 0,
            ScaleNumerator = 1, ScaleDenominator = 20, X = 148.5, Y = 105,
        });

        var bound = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["sheet"] = JsonDocument.Parse($"\"{sheetId}\"").RootElement.Clone(),
        };

        foreach (var step in steps)
        {
            var where = $"{GuidePath}, line {step.Block.Line}";
            if (step.Block.Marker == "agent-person")
            {
                await Person(host, step.Block.Text, bound, where);
                continue;
            }

            var (status, text, mediaType) = await Send(http, address, step.Block.Text, bound, where);
            var refusal = step.Block.Marker == "agent-refusal";
            var said = $"{where}: {step.Block.Text.Split('\n')[0]} gave HTTP {status}: {Cut(text)}";

            if (step.Result!.Language == "text")
            {
                Assert.True(status is >= 200 and < 300 && !refusal, said);
                Assert.True(text.Replace("\r\n", "\n").StartsWith(step.Result.Text, StringComparison.Ordinal), $"{said}\nThe answer does not start with the text of the result block at line {step.Result.Line}.");
                continue;
            }

            Assert.True(mediaType == "application/json", $"{said}\nThe answer is {mediaType}, not JSON.");
            var answer = JsonDocument.Parse(text).RootElement.Clone();
            var error = answer.ValueKind == JsonValueKind.Object && answer.TryGetProperty("error", out var e) && e.ValueKind != JsonValueKind.Null;
            var rejected = answer.ValueKind == JsonValueKind.Object && answer.TryGetProperty("status", out var s) && s.GetString() != "Applied";
            if (refusal)
                Assert.True(status >= 400 || error || rejected, $"{said}\nThe guide gives a refusal, and the host accepted the request.");
            else
                Assert.True(status is >= 200 and < 300 && !error && !rejected, $"{said}\nThe host refused a request of the guide.");

            var expected = JsonDocument.Parse(step.Result.Text).RootElement.Clone();
            var mismatch = Match(expected, answer, bound, "$");
            Assert.True(mismatch is null, $"{said}\nThe answer does not have the shape of the result block at line {step.Result.Line}: {mismatch}");
        }
    }

    // The person in the window: "accept <id>" or "reject <id>".
    private static async Task Person(EngineHost host, string text, Dictionary<string, JsonElement> bound, string where)
    {
        var parts = text.Trim().Split(' ', 2);
        var name = WholePlaceholder.Match(parts.ElementAtOrDefault(1) ?? "");
        Assert.True(name.Success && bound.ContainsKey(name.Groups[1].Value), $"{where}: an action of the person is \"accept <id>\" or \"reject <id>\" with a bound placeholder, not \"{text}\".");
        var id = Guid.Parse(bound[name.Groups[1].Value].GetString()!);
        var closed = parts[0] switch
        {
            "accept" => await host.Proposals.Accept(id),
            "reject" => host.Proposals.Reject(id),
            _ => throw new InvalidOperationException($"{where}: the action \"{parts[0]}\" is not accept or reject."),
        };
        Assert.True(closed is not null, $"{where}: the person could not {parts[0]} the proposal {id}; it is not open.");
    }

    // Sends one request of the guide. GET /events is the WebSocket: the body is the first frame, and the answer is the first
    // message of the host.
    private static async Task<(int Status, string Text, string? MediaType)> Send(HttpClient http, Uri address, string block, Dictionary<string, JsonElement> bound, string where)
    {
        var lines = Substitute(block, bound).Split('\n');
        var start = lines[0].Split(' ', 2);
        Assert.True(start.Length == 2 && start[1].StartsWith('/'), $"{where}: the first line of a request is \"METHOD /route\", not \"{lines[0]}\".");
        var headers = new List<(string Name, string Value)>();
        var at = 1;
        for (; at < lines.Length && lines[at].Length > 0; at++)
        {
            var colon = lines[at].IndexOf(':', StringComparison.Ordinal);
            Assert.True(colon > 0, $"{where}: \"{lines[at]}\" is not a header line; an empty line comes before the body.");
            headers.Add((lines[at][..colon].Trim(), lines[at][(colon + 1)..].Trim()));
        }

        var body = string.Join('\n', lines.Skip(at + 1)).Trim();
        if (start[0] == "GET" && start[1] == "/events")
        {
            using var socket = new ClientWebSocket();
            using var limit = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await socket.ConnectAsync(new Uri($"ws://{address.Authority}/events"), limit.Token);
            await socket.SendAsync(Encoding.UTF8.GetBytes(body), WebSocketMessageType.Text, endOfMessage: true, limit.Token);
            using var message = new MemoryStream();
            var buffer = new byte[64 * 1024];
            WebSocketReceiveResult received;
            do
            {
                received = await socket.ReceiveAsync(buffer, limit.Token);
                Assert.True(received.MessageType != WebSocketMessageType.Close, $"{where}: the host closed the WebSocket: {received.CloseStatus} {received.CloseStatusDescription}.");
                message.Write(buffer, 0, received.Count);
            }
            while (!received.EndOfMessage);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", limit.Token);
            return (200, Encoding.UTF8.GetString(message.ToArray()), "application/json");
        }

        using var request = new HttpRequestMessage(new HttpMethod(start[0]), start[1]);
        if (body.Length > 0)
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        foreach (var (name, value) in headers)
        {
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(request.Content is not null, $"{where}: a Content-Type with no body.");
                request.Content!.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
            }
            else
            {
                request.Headers.Add(name, value);
            }
        }

        using var response = await http.SendAsync(request);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(), response.Content.Headers.ContentType?.MediaType);
    }

    // The text of a request with each placeholder replaced. A whole JSON string "<name>" takes the bound value with its type;
    // a placeholder inside a text takes the text of the bound value; a placeholder that no answer gave is a new UUID.
    internal static string Substitute(string text, Dictionary<string, JsonElement> bound)
    {
        JsonElement Value(string name)
        {
            if (!bound.TryGetValue(name, out var value))
                bound[name] = value = JsonDocument.Parse($"\"{Guid.NewGuid()}\"").RootElement.Clone();
            return value;
        }

        text = QuotedPlaceholder.Replace(text, match => Value(match.Groups[1].Value).GetRawText());
        return Placeholder.Replace(text, match =>
        {
            var value = Value(match.Groups[1].Value);
            return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
        });
    }

    // Null when the answer has the shape, else the first difference. A member of an object of the shape must be in the answer
    // with a value of the shape; an array of the shape is a subsequence of the answer in order, and an empty array needs an
    // empty array; a whole placeholder binds the value of the answer, or must equal its bound value.
    internal static string? Match(JsonElement expected, JsonElement actual, Dictionary<string, JsonElement> bound, string path)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                if (actual.ValueKind != JsonValueKind.Object)
                    return $"{path} is {actual.ValueKind}, not an object.";
                foreach (var member in expected.EnumerateObject())
                {
                    if (!actual.TryGetProperty(member.Name, out var value))
                        return $"{path} has no member \"{member.Name}\".";
                    var inner = Match(member.Value, value, bound, $"{path}.{member.Name}");
                    if (inner is not null)
                        return inner;
                }

                return null;

            case JsonValueKind.Array:
            {
                if (actual.ValueKind != JsonValueKind.Array)
                    return $"{path} is {actual.ValueKind}, not an array.";
                var items = actual.EnumerateArray().ToList();
                var wanted = expected.EnumerateArray().ToList();
                if (wanted.Count == 0)
                    return items.Count == 0 ? null : $"{path} has {items.Count} items, and the shape gives an empty array.";
                var next = 0;
                for (var w = 0; w < wanted.Count; w++)
                {
                    var found = false;
                    string? last = null;
                    for (; next < items.Count && !found; next++)
                    {
                        var trial = new Dictionary<string, JsonElement>(bound, StringComparer.Ordinal);
                        last = Match(wanted[w], items[next], trial, $"{path}[{next}]");
                        if (last is null)
                        {
                            found = true;
                            foreach (var (name, value) in trial)
                                bound[name] = value;
                        }
                    }

                    if (!found)
                        return $"{path}: no item after the item of the shape {w - 1} has the shape {wanted[w].GetRawText()} (the last difference: {last}).";
                }

                return null;
            }

            case JsonValueKind.String:
            {
                var text = expected.GetString()!;
                var whole = WholePlaceholder.Match(text);
                if (whole.Success)
                {
                    var name = whole.Groups[1].Value;
                    if (actual.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        return $"{path} is null, and the placeholder <{name}> needs a value.";
                    if (!bound.TryGetValue(name, out var earlier))
                    {
                        bound[name] = actual.Clone();
                        return null;
                    }

                    return Same(earlier, actual) ? null : $"{path} is {actual.GetRawText()}, and the placeholder <{name}> is {earlier.GetRawText()}.";
                }

                return actual.ValueKind == JsonValueKind.String && actual.GetString() == text ? null : $"{path} is {actual.GetRawText()}, not \"{text}\".";
            }

            case JsonValueKind.Number:
                return actual.ValueKind == JsonValueKind.Number && actual.GetDouble() == expected.GetDouble() ? null : $"{path} is {actual.GetRawText()}, not {expected.GetRawText()}.";

            default:
                return actual.ValueKind == expected.ValueKind ? null : $"{path} is {actual.GetRawText()}, not {expected.GetRawText()}.";
        }
    }

    private static bool Same(JsonElement a, JsonElement b) => (a.ValueKind, b.ValueKind) switch
    {
        (JsonValueKind.String, JsonValueKind.String) => a.GetString() == b.GetString(),
        (JsonValueKind.Number, JsonValueKind.Number) => a.GetDouble() == b.GetDouble(),
        _ => a.GetRawText() == b.GetRawText(),
    };

    private static string Cut(string text) => text.Length <= 600 ? text : text[..600] + " …";
}
