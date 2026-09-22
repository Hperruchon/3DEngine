using System.Text.Encodings.Web;
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
        // A person reads this output in a terminal. The default encoder writes
        // each apostrophe, and each of < > & +, as a Unicode escape, which is
        // correct JSON and which reads as noise. This encoder escapes fewer
        // characters. Register entry R-0015 held the question and TASK-0025
        // decided it.
        //
        // The word Unsafe in the name of this encoder names one risk: a page
        // that writes JSON into HTML with no further encoding. The command line
        // writes to a terminal and to a pipe, therefore the risk does not apply
        // here. Engine.Api.Http keeps the default encoder, because a browser
        // client can put a response into a page.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
