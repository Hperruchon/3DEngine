using System.Net;
using System.Reflection;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts.Schema;
using Engine.Core;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Per ADR-0008 §9: "CI fails if a registered command/query/event lacks a
// schema entry." Per ADR-0013 §4: the gate is tighter — the endpoint's
// schema JSON must equal the handler's declared schema, structurally.
//
// The command and query gates iterate the live host's registries and
// compare endpoint output against handler declarations. The diagnostics
// gate uses reflection over Engine.Core/DiagnosticCodes.cs to assert every
// constant appears in /schema/diagnostics. The source-content gate
// asserts SchemaCommandsEndpoint contains no per-command branching.
public class SchemaEndpointGateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SchemaEndpointGateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Every_Registered_Command_Endpoint_Matches_Handler_Declared_Schema()
    {
        var client = _factory.CreateClient();
        var host = (EngineHost)_factory.Services.GetService(typeof(EngineHost))!;

        foreach (var handler in host.CommandRegistry.Handlers)
        {
            var response = await client.GetAsync(
                $"/schema/commands/{handler.CommandName}@{handler.SchemaVersion}");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"/schema/commands/{handler.CommandName}@{handler.SchemaVersion} returned {(int)response.StatusCode}.");

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(handler.CommandName, json.GetProperty("name").GetString());
            Assert.Equal(handler.SchemaVersion, json.GetProperty("schemaVersion").GetInt32());

            AssertFieldDictEquals(handler.Parameters, json.GetProperty("parameters"), "parameters");
            AssertFieldDictEquals(handler.Outputs, json.GetProperty("outputs"), "outputs");
        }
    }

    [Fact]
    public async Task Every_Registered_Query_Endpoint_Matches_Handler_Declared_Schema()
    {
        var client = _factory.CreateClient();
        var host = (EngineHost)_factory.Services.GetService(typeof(EngineHost))!;

        foreach (var handler in host.QueryRegistry.Handlers)
        {
            var response = await client.GetAsync(
                $"/schema/queries/{handler.QueryName}@{handler.SchemaVersion}");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"/schema/queries/{handler.QueryName}@{handler.SchemaVersion} returned {(int)response.StatusCode}.");

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(handler.QueryName, json.GetProperty("name").GetString());
            Assert.Equal(handler.SchemaVersion, json.GetProperty("schemaVersion").GetInt32());

            AssertFieldDictEquals(handler.Parameters, json.GetProperty("parameters"), "parameters");
            AssertFieldDictEquals(handler.Result, json.GetProperty("result"), "result");
        }
    }

    [Fact]
    public async Task Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/diagnostics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;
        var publishedCodes = body.EnumerateArray()
            .Select(e => e.GetProperty("code").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var declaredCodes = typeof(DiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(declaredCodes);
        foreach (var code in declaredCodes)
        {
            Assert.True(
                publishedCodes.Contains(code),
                $"DiagnosticCodes constant '{code}' is missing from /schema/diagnostics.");
        }
    }

    // Per ADR-0013 §Validation 3: the schema endpoint must contain no
    // per-command branching. Specifically, no name-literal of any registered
    // command may appear in the source file.
    [Fact]
    public void SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching()
    {
        var source = FindSource("Engine.Api.Http", "Endpoints", "SchemaCommandsEndpoint.cs");
        var text = File.ReadAllText(source);

        var registeredNames = new[] { "NoOp", "CreateBox" };
        foreach (var name in registeredNames)
        {
            var literal = $"\"{name}\"";
            Assert.False(
                text.Contains(literal, StringComparison.Ordinal),
                $"SchemaCommandsEndpoint.cs must not contain the string literal {literal} " +
                "(per ADR-0013 §3 the endpoint is a pure projection from the handler).");
        }
    }

    [Fact]
    public void SchemaQueriesEndpoint_Source_Contains_No_Per_Query_Branching()
    {
        var source = FindSource("Engine.Api.Http", "Endpoints", "SchemaQueriesEndpoint.cs");
        var text = File.ReadAllText(source);

        var registeredNames = new[] { "GetBoundingBox" };
        foreach (var name in registeredNames)
        {
            var literal = $"\"{name}\"";
            Assert.False(
                text.Contains(literal, StringComparison.Ordinal),
                $"SchemaQueriesEndpoint.cs must not contain the string literal {literal} " +
                "(per ADR-0013 §3 the endpoint is a pure projection from the handler).");
        }
    }

    private static void AssertFieldDictEquals(
        IReadOnlyDictionary<string, FieldSchema> expected,
        JsonElement actual,
        string label)
    {
        Assert.Equal(JsonValueKind.Object, actual.ValueKind);
        var actualKeys = actual.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expected.Count, actualKeys.Count);

        foreach (var (key, schema) in expected)
        {
            Assert.True(
                actual.TryGetProperty(key, out var fieldJson),
                $"{label}.{key} missing from endpoint output.");
            Assert.Equal(schema.Type, fieldJson.GetProperty("type").GetString());
            Assert.Equal(schema.Required, fieldJson.GetProperty("required").GetBoolean());
        }
    }

    private static string FindSource(params string[] segments)
    {
        // Walk up from the test binary until we find the repo root that
        // contains the expected source file. The test binary lives at
        // .../Engine.Tests/bin/Debug/net10.0/, so the repo root is several
        // directories above.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            $"Could not locate source file by walking up from '{AppContext.BaseDirectory}': " +
            string.Join('/', segments));
    }
}
