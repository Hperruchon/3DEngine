using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Contracts.Schema;

namespace Engine.Api.Http.Schema;

// Wire shapes for the /schema/* discovery endpoints.
// Per ADR-0008 §9, TASK-0009 §1–7, and ADR-0013 §2.
//
// FieldSchema moved to Engine.Contracts/Schema/FieldSchema.cs per ADR-0013 §2
// so handlers can declare schemas. The wire types here project from the
// handler's declarations.

internal sealed record CommandSchemaIndexEntry(string Name, int SchemaVersion);

internal sealed record CommandSchemaItem(
    string Name,
    int SchemaVersion,
    [property: JsonConverter(typeof(FieldMapConverter))] IReadOnlyDictionary<string, FieldSchema> Parameters,
    [property: JsonConverter(typeof(FieldMapConverter))] IReadOnlyDictionary<string, FieldSchema> Outputs);

internal sealed record QuerySchemaIndexEntry(string Name, int SchemaVersion);

internal sealed record QuerySchemaItem(
    string Name,
    int SchemaVersion,
    [property: JsonConverter(typeof(FieldMapConverter))] IReadOnlyDictionary<string, FieldSchema> Parameters,
    [property: JsonConverter(typeof(FieldMapConverter))] IReadOnlyDictionary<string, FieldSchema> Result);

// The kind of an event, and the fields that a message of that kind carries (TASK-0055).
internal sealed record EventKindEntry(
    string Kind,
    [property: JsonConverter(typeof(FieldMapConverter))] IReadOnlyDictionary<string, FieldSchema> Payload);

internal sealed record DiagnosticCodeEntry(string Code, string Severity, string Subsystem);

// ADR-0060 item 5: a field gives "type" and "required" as before, then "unit" or
// "unitBy" only when the handler declares it, so a field with no unit keeps its
// form of ADR-0013. The names of the fields follow the key policy of the options,
// as before. The values of a "unitBy" map are values of a parameter, so the
// converter writes them as they are.
internal sealed class FieldMapConverter : JsonConverter<IReadOnlyDictionary<string, FieldSchema>>
{
    public override IReadOnlyDictionary<string, FieldSchema> Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("The schema endpoints write a map of fields and do not read one.");

    public override void Write(
        Utf8JsonWriter writer, IReadOnlyDictionary<string, FieldSchema> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, field) in value)
        {
            writer.WritePropertyName(options.DictionaryKeyPolicy?.ConvertName(name) ?? name);
            writer.WriteStartObject();
            writer.WriteString("type", field.Type);
            writer.WriteBoolean("required", field.Required);
            if (field.Unit is { } unit)
                writer.WriteString("unit", unit);

            if (field.UnitBy is { } unitBy)
            {
                writer.WriteStartObject("unitBy");
                writer.WriteString("field", unitBy.Field);
                writer.WriteStartObject("units");
                foreach (var (fieldValue, fieldUnit) in unitBy.Units)
                    writer.WriteString(fieldValue, fieldUnit);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
