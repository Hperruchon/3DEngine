using System.Net;
using System.Reflection;
using System.Text.Json;
using Engine.Core;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Per ADR-0008 §9: "CI fails if a registered command/query/event lacks a
// schema entry." This pair of tests enforces that rule for V1.x's surface.
//
// The command gate iterates CommandRegistry and asserts each entry resolves
// to a 200 on /schema/commands/{name}@{version}. The diagnostics gate uses
// reflection over Engine.Core/DiagnosticCodes.cs to assert every constant
// appears in /schema/diagnostics.
public class SchemaEndpointGateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SchemaEndpointGateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Every_Registered_Command_Has_A_Schema_Entry()
    {
        var client = _factory.CreateClient();
        var registry = new CommandRegistry();
        registry.Register(new Engine.Core.Commands.NoOpCommandHandler());

        foreach (var (name, version) in registry.Registered)
        {
            var response = await client.GetAsync($"/schema/commands/{name}@{version}");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"/schema/commands/{name}@{version} returned {(int)response.StatusCode}.");
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
}
