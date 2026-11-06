using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Engine.Api.Http;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// Per ADR-0008 §9: "CI fails if a registered command/query/event lacks a
// schema entry." Per ADR-0013 §4: the gate is tighter — the endpoint's
// schema JSON must equal the handler's declared schema, structurally.
//
// The command and query gates iterate the live host's registries and
// compare endpoint output against handler declarations. The diagnostics
// gate uses reflection over Engine.Core/DiagnosticCodes.cs to assert every
// constant appears in /schema/diagnostics. The source-content gate
// asserts SchemaCommandsEndpoint contains no per-command branching. Its
// list of names comes from HandlerCatalog; until TASK-0039 it was a literal
// list that a new command did not enter (rules review finding F22).
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

    // Findings T140 and E141 of the codebase review of 2026-11-05 (b) (TASK-0192): the rule read the number parameters of
    // the commands only, so the pixel size of PlaceImage and the point and the direction of GetFaceAt had no unit. It
    // reads each number and each integer parameter of a command and of a query.
    // ADR-0060 item 6: each number parameter of a registered command declares one
    // unit of the vocabulary, in Unit or in UnitBy. Until TASK-0186 a number had no
    // stated meaning, and each client held the meaning in its own code.
    [Fact]
    public void Each_Number_Parameter_Of_A_Command_Declares_One_Unit_Of_The_Vocabulary()
    {
        var host = (EngineHost)_factory.Services.GetService(typeof(EngineHost))!;

        var problems = host.CommandRegistry.Handlers
            .SelectMany(h => UnitProblems(h.CommandName, h.Parameters))
            .Concat(host.QueryRegistry.Handlers.SelectMany(h => UnitProblems(h.QueryName, h.Parameters)))
            .ToArray();

        Assert.True(problems.Length == 0, "ADR-0060 item 6:\n  " + string.Join("\n  ", problems));
    }

    // ADR-0060 item 2. A new unit needs a change of the record, not only of the code.
    [Fact]
    public void The_Unit_Vocabulary_Is_The_List_Of_ADR_0060()
        => Assert.Equal(
            ["length", "pixel", "scale", "cosine", "sine", "sign", "constraint-value", "direction", "count"],
            UnitKinds.All);

    // The acceptance criteria of TASK-0186: the wire gives the unit of a field, the map
    // of a field whose unit follows the kind, and no unit member for a field with no unit.
    [Fact]
    public async Task The_Schema_Of_A_Command_Gives_The_Unit_Of_Each_Number_On_The_Wire()
    {
        var client = _factory.CreateClient();

        var box = JsonDocument.Parse(await client.GetStringAsync("/schema/commands/CreateBox@1")).RootElement;
        Assert.Equal("length", box.GetProperty("parameters").GetProperty("sizeX").GetProperty("unit").GetString());

        var calibrate = JsonDocument.Parse(await client.GetStringAsync("/schema/commands/CalibrateImage@1")).RootElement;
        Assert.Equal("pixel", calibrate.GetProperty("parameters").GetProperty("x1").GetProperty("unit").GetString());
        Assert.False(calibrate.GetProperty("parameters").GetProperty("imageId").TryGetProperty("unit", out _));
        Assert.False(calibrate.GetProperty("parameters").GetProperty("imageId").TryGetProperty("unitBy", out _));
        Assert.False(calibrate.GetProperty("outputs").GetProperty("scale").TryGetProperty("unit", out _));

        var constraint = JsonDocument.Parse(await client.GetStringAsync("/schema/commands/AddSketchConstraint@1")).RootElement;
        var value = constraint.GetProperty("parameters").GetProperty("value");
        Assert.False(value.TryGetProperty("unit", out _));
        Assert.Equal("kind", value.GetProperty("unitBy").GetProperty("field").GetString());
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["distance"] = "length", ["radius"] = "length", ["fixed"] = "length", ["tangent"] = "sign", ["angle"] = "cosine",
            },
            value.GetProperty("unitBy").GetProperty("units").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!));
        Assert.Equal(
            new Dictionary<string, string> { ["fixed"] = "length", ["angle"] = "sine" },
            constraint.GetProperty("parameters").GetProperty("secondValue").GetProperty("unitBy").GetProperty("units")
                .EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!));
    }

    // ADR-0060 items 2, 3 and 6, for the parameters of one command. The function is pure,
    // so a theory gives it each form.
    internal static IEnumerable<string> UnitProblems(string command, IReadOnlyDictionary<string, FieldSchema> parameters)
    {
        foreach (var (name, field) in parameters)
        {
            var where = $"{command}.{name}";
            if (field.Type is not ("number" or "integer"))
            {
                if (field.Unit is not null || field.UnitBy is not null)
                    yield return $"{where} is a {field.Type} and declares a unit; only a number or an integer has a unit";
                continue;
            }

            if (field.Unit is null && field.UnitBy is null)
                yield return $"{where} is a {field.Type} with no unit; declare Unit or UnitBy";
            else if (field.Unit is not null && field.UnitBy is not null)
                yield return $"{where} declares Unit and UnitBy; declare one";
            else if (field.Unit is { } unit && !UnitKinds.All.Contains(unit))
                yield return $"{where} declares the unit '{unit}', which is not in the vocabulary";
            else if (field.UnitBy is { } unitBy)
            {
                if (!parameters.TryGetValue(unitBy.Field, out var by) || by.Type != "string")
                    yield return $"{where} takes its unit from '{unitBy.Field}', which is not a string parameter of {command}";

                foreach (var (value, unitOfValue) in unitBy.Units.Where(u => !UnitKinds.All.Contains(u.Value)))
                    yield return $"{where} declares the unit '{unitOfValue}' for '{value}', which is not in the vocabulary";

                if (unitBy.Units.Count == 0)
                    yield return $"{where} takes its unit from '{unitBy.Field}' with an empty map";
            }
        }
    }

    [Theory]
    [InlineData("no unit", "is a number with no unit; declare Unit or UnitBy")]
    [InlineData("both", "declares Unit and UnitBy; declare one")]
    [InlineData("unknown unit", "declares the unit 'degree', which is not in the vocabulary")]
    [InlineData("unknown unit by kind", "declares the unit 'degree' for 'angle', which is not in the vocabulary")]
    [InlineData("no such field", "takes its unit from 'type', which is not a string parameter of C")]
    [InlineData("field not a string", "takes its unit from 'count', which is not a string parameter of C")]
    [InlineData("empty map", "takes its unit from 'kind' with an empty map")]
    [InlineData("unit on a string", "is a string and declares a unit; only a number or an integer has a unit")]
    [InlineData("integer with no unit", "is a integer with no unit; declare Unit or UnitBy")]
    [InlineData("correct", null)]
    public void Each_Form_Of_ADR_0060_Item_6_Is_A_Problem(string form, string? expected)
    {
        var byKind = new UnitByField("kind", new Dictionary<string, string> { ["angle"] = UnitKinds.Cosine });
        FieldSchema value = form switch
        {
            "no unit" => new("number"),
            "both" => new("number", Unit: UnitKinds.Length, UnitBy: byKind),
            "unknown unit" => new("number", Unit: "degree"),
            "unknown unit by kind" => new("number", UnitBy: new("kind", new Dictionary<string, string> { ["angle"] = "degree" })),
            "no such field" => new("number", UnitBy: byKind with { Field = "type" }),
            "field not a string" => new("number", UnitBy: byKind with { Field = "count" }),
            "empty map" => new("number", UnitBy: byKind with { Units = new Dictionary<string, string>() }),
            "unit on a string" => new("number", Unit: UnitKinds.Length),
            "correct" => new("number", UnitBy: byKind),
            "integer with no unit" => new("integer"),
            _ => throw new ArgumentOutOfRangeException(nameof(form)),
        };
        var parameters = new Dictionary<string, FieldSchema>
        {
            ["kind"] = form == "unit on a string" ? new("string", Unit: UnitKinds.Length) : new("string"),
            ["count"] = new("integer", Unit: UnitKinds.Pixel),
            ["value"] = form == "unit on a string" ? new("number", Unit: UnitKinds.Length) : value,
            ["distance"] = new("number", Unit: UnitKinds.Length),
        };

        var problems = UnitProblems("C", parameters).ToArray();

        if (expected is null)
            Assert.Empty(problems);
        else
            Assert.Contains(problems, p => p.EndsWith(expected, StringComparison.Ordinal));
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

        var registeredNames = HandlerCatalog.CommandHandlers().Select(h => h.CommandName).ToArray();
        Assert.NotEmpty(registeredNames);
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

        var registeredNames = HandlerCatalog.QueryHandlers().Select(h => h.QueryName).ToArray();
        Assert.NotEmpty(registeredNames);
        foreach (var name in registeredNames)
        {
            var literal = $"\"{name}\"";
            Assert.False(
                text.Contains(literal, StringComparison.Ordinal),
                $"SchemaQueriesEndpoint.cs must not contain the string literal {literal} " +
                "(per ADR-0013 §3 the endpoint is a pure projection from the handler).");
        }
    }

    // Finding E35 of the codebase review of 2026-10-08 (TASK-0139): /schema/backend was in no list of
    // the surface. Each route of the surface under /schema is in the list of ADR-0008 §9, and each entry
    // of the list is a route.
    //
    // Finding T111 of the codebase review of 2026-10-31 (b) (TASK-0143): the gate read the form
    // app.MapGet("/schema… of the source only, and MapMethods, MapGroup and "/Schema/…" passed, though
    // the routing does not regard case. The routes come from the endpoints of the test host now, and a
    // route is under /schema in any case.
    [Fact]
    public void The_Schema_Routes_Of_The_Surface_Are_The_List_Of_ADR_0008()
    {
        var routes = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/'))
            .Where(route => route.StartsWith("/schema", StringComparison.OrdinalIgnoreCase))
            .Select(route => route.Replace(":int}", "}", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var adr = File.ReadAllText(FindSource("docs", "adr", "0008-command-query-event-triad.md"));
        var section = adr[adr.IndexOf("### 9. Schema endpoints", StringComparison.Ordinal)..];
        section = section[..section.IndexOf("\n## ", StringComparison.Ordinal)];
        var listed = Regex.Matches(section, "^- `(/schema[^`]*)`", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(routes);
        Assert.Equal(listed, routes);
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

            // ADR-0060 item 5: the wire writes a unit and a map only when the handler declares them.
            Assert.True(
                fieldJson.TryGetProperty("unit", out var unit) == schema.Unit is not null,
                $"{label}.{key}: the wire and the handler disagree on the presence of a unit.");
            if (schema.Unit is not null)
                Assert.Equal(schema.Unit, unit.GetString());

            Assert.True(
                fieldJson.TryGetProperty("unitBy", out var unitBy) == schema.UnitBy is not null,
                $"{label}.{key}: the wire and the handler disagree on the presence of a unit map.");
            if (schema.UnitBy is not null)
            {
                Assert.Equal(schema.UnitBy.Field, unitBy.GetProperty("field").GetString());
                Assert.Equal(
                    schema.UnitBy.Units.OrderBy(u => u.Key, StringComparer.Ordinal).ToArray(),
                    unitBy.GetProperty("units").EnumerateObject()
                        .Select(p => new KeyValuePair<string, string>(p.Name, p.Value.GetString()!))
                        .OrderBy(u => u.Key, StringComparer.Ordinal).ToArray());
            }

            Assert.Equal(
                fieldJson.EnumerateObject().Select(p => p.Name).Where(n => n is not ("type" or "required" or "unit" or "unitBy")),
                Array.Empty<string>());
        }
    }

    // Finding E140 of the codebase review of 2026-11-05 (b) (TASK-0192): FieldMapConverter writes four named members, and
    // the comparison of the wire above lists the same four, so a new member of FieldSchema left the wire with no failure.
    // Each public member of FieldSchema and of UnitByField is a member that the converter writes.
    [Fact]
    public void Each_Member_Of_A_Field_Schema_Is_A_Member_That_The_Converter_Writes()
    {
        static string[] Members(Type type) => [.. type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(p => p.Name).Order(StringComparer.Ordinal)];

        Assert.Equal(["Required", "Type", "Unit", "UnitBy"], Members(typeof(FieldSchema)));
        Assert.Equal(["Field", "Units"], Members(typeof(UnitByField)));
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
