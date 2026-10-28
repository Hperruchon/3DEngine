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
        Assert.Equal("guid", entries["body.removed"].GetProperty("bodyId").GetProperty("type").GetString());
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
        // A name, so that body.created of the cut carries a string, and a cleared name, so
        // that body.changed carries a null (ADR-0028, TASK-0085).
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new RenameBodyCommand { BodyId = minuend, NewName = "A" })).Status);
        var cut = await host.Session.Apply(new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend });
        Assert.Equal(CommandStatus.Applied, cut.Status);
        Assert.True(cut.Outputs.TryGet<Guid>("bodyId", out var result));
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new SetBodyVisibilityCommand { BodyId = result, Visible = false })).Status);
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new RenameBodyCommand { BodyId = result, NewName = "" })).Status);
        await host.Session.Apply(new CreateBoxCommand { SizeX = 0, SizeY = 1, SizeZ = 1 });

        // Three undos reverse the rename, the hide and the cut, so body.removed comes too
        // (ADR-0031 item 6, TASK-0093), and a redo cuts again.
        foreach (var move in new Command[] { new UndoCommand(), new UndoCommand(), new UndoCommand(), new RedoCommand() })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(move)).Status);

        // A sketch with a name and one with none, a point, then two undos and a redo, so that
        // each sketch event comes (ADR-0033 item 9, TASK-0100).
        var named = await host.Session.Apply(new CreateSketchCommand { DatumId = Engine.Contracts.Sketch.Datums.XY.Id, SketchName = "S" });
        Assert.True(named.Outputs.TryGet<Guid>("sketchId", out var sketch));
        foreach (var step in new Command[]
        {
            new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 2 },
            new CreateSketchCommand { DatumId = Engine.Contracts.Sketch.Datums.YZ.Id },
            new UndoCommand(), new UndoCommand(), new RedoCommand(),
        })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(step)).Status);

        Assert.Equal(
            ["body.changed", "body.consumed", "body.created", "body.removed", "command.applied", "command.rejected",
                "sketch.changed", "sketch.created", "sketch.removed"],
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
            else if (TypeOf(value) != field.Type && !(value is null && !field.Required))
                yield return $"{record.Kind} carries '{name}' as {TypeOf(value)}, and its entry gives {field.Type}";
        }

        foreach (var name in entry.Payload.Keys.Where(name => !payload.ContainsKey(name)))
            yield return $"{record.Kind} lacks the field '{name}' of its entry";
    }

    // A null passes only a field whose entry is not required (ADR-0028 item 7).
    [Fact]
    public void A_Null_In_A_Required_Field_Is_A_Problem()
    {
        var record = new EventRecord(1, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "body.changed",
            new Dictionary<string, object?> { ["bodyId"] = Guid.NewGuid(), ["name"] = null, ["visible"] = null });

        var problem = Assert.Single(PayloadProblems(record));
        Assert.Contains("'visible' as null", problem, StringComparison.Ordinal);
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
