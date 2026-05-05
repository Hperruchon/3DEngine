using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Contracts;

namespace Engine.Cli;

// JSON wire format for CommandResult / QueryResult<T>.
// Per ADR-0008 §2 + §6: shapes are normative; this is just a serializer.
// Per ADR-0008 §3: Outputs surfaces as a bare map at the wire, not the
// record's Values wrapper — the OutputsConverter handles that.
internal static class JsonRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
            new OutputsConverter(),
        },
    };

    public static void WriteCommandResult(CommandResult result, TextWriter writer)
    {
        var json = JsonSerializer.Serialize(result, Options);
        writer.WriteLine(json);
    }

    public static void WriteQueryResult<T>(QueryResult<T> result, TextWriter writer)
    {
        var json = JsonSerializer.Serialize(result, Options);
        writer.WriteLine(json);
    }

    private sealed class OutputsConverter : JsonConverter<Outputs>
    {
        public override Outputs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Outputs deserialization is out of scope for the CLI.");

        public override void Write(Utf8JsonWriter writer, Outputs value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.Values, options);
    }
}
