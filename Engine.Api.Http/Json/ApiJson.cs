using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Contracts;

namespace Engine.Api.Http.Json;

// JSON wire format for the HTTP API.
// Shape matches Engine.Cli/JsonRenderer.cs: the same names, the same casing and the
// same converters on the structured-result surface (ADR-0002 parity, ADR-0008
// §2/§3/§6). The HTTP API writes compact JSON, and the command line keeps its
// indented output for a person; until TASK-0067 the HTTP API wrote indented JSON,
// and a tessellation was about 2.4 times larger (finding E60 of the codebase review
// of 2026-10-12).
// The OutputsConverter duplicates the CLI's converter; lifting it into a
// shared types library is its own refactor (out of TASK-0007 scope).
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        // Finding E101 of the codebase review of 2026-10-30 (b) (TASK-0123): a repeated member of
        // an envelope or of a parameter kept its last value, so a second "expectedDocumentVersion"
        // of null removed the check of the version. A repeated member is a malformed body.
        AllowDuplicateProperties = false,
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
