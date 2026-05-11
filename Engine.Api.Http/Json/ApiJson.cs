using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Contracts;

namespace Engine.Api.Http.Json;

// JSON wire format for the HTTP API.
// Shape matches Engine.Cli/JsonRenderer.cs so HTTP and CLI are byte-equivalent
// on the structured-result surface (ADR-0002 parity, ADR-0008 §2/§3/§6).
// The OutputsConverter duplicates the CLI's converter; lifting it into a
// shared types library is its own refactor (out of TASK-0007 scope).
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
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

    private sealed class OutputsConverter : JsonConverter<Outputs>
    {
        public override Outputs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Outputs deserialization is out of scope for the HTTP API.");

        public override void Write(Utf8JsonWriter writer, Outputs value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.Values, options);
    }
}
