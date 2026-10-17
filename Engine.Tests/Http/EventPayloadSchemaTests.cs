using System.Text.Json;
using Engine.Api.Http;
using Engine.Api.Http.Endpoints;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// The payload fields of /schema/events (ADR-0008 §9, ADR-0005 validation rule 5,
// TASK-0055). Until TASK-0055 each entry gave its kind only, and an agent could not
// learn what an event carries without the ADRs.
public class EventPayloadSchemaTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task The_Schema_Gives_The_Payload_Fields_Of_Each_Body_Event()
    {
        var http = factory.CreateClient();

        var entries = JsonDocument.Parse(await http.GetStringAsync("/schema/events")).RootElement.EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("kind").GetString()!, entry => entry.GetProperty("payload"));

        Assert.Equal("guid", entries["body.consumed"].GetProperty("bodyId").GetProperty("type").GetString());
        Assert.Equal("guid", entries["body.created"].GetProperty("bodyId").GetProperty("type").GetString());
        Assert.Equal("string", entries["body.created"].GetProperty("kind").GetProperty("type").GetString());
        Assert.Empty(entries["heartbeat"].EnumerateObject());
    }

    // Each event that the bus emits in a scenario of each outcome has the fields of its
    // entry, with their types, and no other field. A new field of a payload with no
    // entry fails here (ADR-0008 §9).
    [NativeManifoldFact]
    public async Task Each_Payload_That_The_Bus_Emits_Agrees_With_Its_Entry()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);
        var records = new List<EventRecord>();
        using var subscription = host.Subscribe(records.Add);

        var first = await host.Session.Apply(new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        var second = await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        Assert.True(first.Outputs.TryGet<Guid>("bodyId", out var minuend));
        Assert.True(second.Outputs.TryGet<Guid>("bodyId", out var subtrahend));
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend })).Status);
        await host.Session.Apply(new CreateBoxCommand { SizeX = 0, SizeY = 1, SizeZ = 1 });

        Assert.Equal(
            ["body.consumed", "body.created", "command.applied", "command.rejected"],
            records.Select(record => record.Kind).Distinct().Order(StringComparer.Ordinal));
        var problems = records.SelectMany(PayloadProblems).ToList();
        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    // The check of the test above finds a field that the schema does not list.
    [Fact]
    public void A_Payload_Field_That_The_Schema_Does_Not_List_Is_A_Problem()
    {
        var record = new EventRecord(1, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "body.consumed",
            new Dictionary<string, object?> { ["bodyId"] = Guid.NewGuid(), ["extra"] = 1 });

        var problem = Assert.Single(PayloadProblems(record));
        Assert.Contains("extra", problem, StringComparison.Ordinal);
    }

    private static IEnumerable<string> PayloadProblems(EventRecord record)
    {
        var entry = SchemaEventsEndpoint.Kinds.SingleOrDefault(kind => kind.Kind == record.Kind);
        if (entry is null)
        {
            yield return $"{record.Kind} has no entry in /schema/events";
            yield break;
        }

        var payload = record.Payload as IReadOnlyDictionary<string, object?> ?? new Dictionary<string, object?>();
        foreach (var (name, value) in payload)
        {
            if (!entry.Payload.TryGetValue(name, out var field))
                yield return $"{record.Kind} carries the field '{name}', which its entry does not list";
            else if (TypeOf(value) != field.Type)
                yield return $"{record.Kind} carries '{name}' as {TypeOf(value)}, and its entry gives {field.Type}";
        }

        foreach (var name in entry.Payload.Keys.Where(name => !payload.ContainsKey(name)))
            yield return $"{record.Kind} lacks the field '{name}' of its entry";
    }

    private static string TypeOf(object? value) => value switch
    {
        Guid => "guid",
        string => "string",
        int or long => "integer",
        double or float => "number",
        bool => "boolean",
        null => "null",
        _ => value.GetType().Name,
    };
}
