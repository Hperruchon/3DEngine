using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Hosting;

namespace Engine.Core.Persistence;

// The text of one line of a document file (ADR-0015 items 2, 3 and 12). The writer
// uses Utf8JsonWriter, which writes a double in the shortest form that reads back to
// the same value; the reader reads each number of type "number" as a double, so -0
// keeps its sign. No code here calls a transcendental function (CLAUDE.md, rule 1).
public static class EntryCodec
{
    public const string Format = "3dengine-document";
    public const int FormatVersion = 1;

    public static string Header(string lengthUnit)
        => Write(writer =>
        {
            writer.WriteString("format", Format);
            writer.WriteNumber("formatVersion", FormatVersion);
            writer.WriteString("lengthUnit", lengthUnit);
        });

    // The line of one applied command, or null with the reason when a value cannot
    // be held exactly: a number that is not finite, or a string that is not
    // well-formed UTF-16 (ADR-0015 item 4, E-IO-NOT-ENCODABLE).
    public static string? Entry(
        long version,
        BackendIdentity backend,
        Command command,
        IReadOnlyDictionary<string, object?> parameters,
        out string? refusal)
    {
        foreach (var (name, value) in parameters)
        {
            refusal = value switch
            {
                double number when !double.IsFinite(number) => $"The parameter '{name}' is {number.ToString(CultureInfo.InvariantCulture)}, and a document file holds only a finite number.",
                string text when !IsWellFormed(text) => $"The parameter '{name}' is not well-formed UTF-16 text, and a document file holds only well-formed text.",
                null or double or long or string or bool or Guid => null,
                _ => $"The parameter '{name}' has the type {value.GetType().Name}, which a document file does not hold.",
            };
            if (refusal is not null)
                return null;
        }

        refusal = null;
        return Write(writer =>
        {
            writer.WriteNumber("version", version);
            writer.WriteStartObject("backend");
            writer.WriteString("name", backend.Name);
            writer.WriteString("version", backend.Version);
            writer.WriteEndObject();
            writer.WriteStartObject("command");
            writer.WriteString("name", command.Name);
            writer.WriteNumber("schemaVersion", command.SchemaVersion);
            writer.WriteString("commandId", command.CommandId);
            if (command.ExpectedDocumentVersion is { } expected)
                writer.WriteNumber("expectedDocumentVersion", expected);
            else
                writer.WriteNull("expectedDocumentVersion");
            writer.WriteStartObject("parameters");
            foreach (var (name, value) in parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                switch (value)
                {
                    case null: break;
                    case double number: writer.WriteNumber(name, number); break;
                    case long whole: writer.WriteNumber(name, whole); break;
                    case string text: writer.WriteString(name, text); break;
                    case bool flag: writer.WriteBoolean(name, flag); break;
                    case Guid id: writer.WriteString(name, id); break;
                }
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
    }

    public static bool IsWellFormed(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                index++;
            else if (char.IsSurrogate(text[index]))
                return false;
        }

        return true;
    }

    // True when the line is one JSON value; an open cuts only a last line that is not
    // (ADR-0015 item 8).
    public static bool IsJson(string line)
    {
        try
        {
            using var parsed = JsonDocument.Parse(line);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // Finding E109 of the codebase review of 2026-10-31 (b) (TASK-0142): the reader took the last value
    // of a repeated member, and the writer never repeats one (the rule of E78). The two readers refuse
    // it. IsJson keeps the default options, so a last line with a repeated member is refused, not cut.
    private static readonly JsonDocumentOptions Strict = new() { AllowDuplicateProperties = false };

    // The length unit of a header line, or a DocumentFileException.
    public static string ReadHeader(string line)
    {
        try
        {
            using var parsed = JsonDocument.Parse(line, Strict);
            var root = parsed.RootElement;
            var format = root.GetProperty("format").GetString();
            var formatVersion = root.GetProperty("formatVersion").GetInt32();
            var unit = root.GetProperty("lengthUnit").GetString();
            if (format != Format || formatVersion != FormatVersion)
                throw DocumentFileException.Load($"The header gives the format {format} {formatVersion}, and this engine reads {Format} {FormatVersion}.");
            if (unit != Document.Millimetre)
                throw DocumentFileException.Load($"The header gives the length unit {unit}, and the millimetre ({Document.Millimetre}) is the only permitted unit (ADR-0015 item 7).");
            return unit;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw DocumentFileException.Load($"The header is not a header of a document file: {ex.Message}");
        }
    }

    // One entry of a file, read back into a command through the declaration and the
    // Create method of its handler, the same path as a command of a host (ADR-0016).
    public static DecodedEntry ReadEntry(string line, CommandRegistry registry)
    {
        long version;
        BackendIdentity backend;
        string name;
        int schemaVersion;
        Guid commandId;
        long? expected;
        Dictionary<string, object?> raw = new(StringComparer.Ordinal);
        ICommandHandler handler;
        try
        {
            using var parsed = JsonDocument.Parse(line, Strict);
            var root = parsed.RootElement;
            version = root.GetProperty("version").GetInt64();
            var backendElement = root.GetProperty("backend");
            backend = new BackendIdentity(backendElement.GetProperty("name").GetString()!, backendElement.GetProperty("version").GetString()!);
            var command = root.GetProperty("command");
            name = command.GetProperty("name").GetString()!;
            schemaVersion = command.GetProperty("schemaVersion").GetInt32();
            commandId = command.GetProperty("commandId").GetGuid();
            var expectedElement = command.GetProperty("expectedDocumentVersion");
            expected = expectedElement.ValueKind == JsonValueKind.Null ? null : expectedElement.GetInt64();

            if (!registry.TryFind(name, schemaVersion, out handler))
                throw DocumentFileException.Load($"The entry of version {version} names the command {name}@{schemaVersion}, which this engine does not have.");

            foreach (var parameter in command.GetProperty("parameters").EnumerateObject())
            {
                // The writer gives each declared type one kind of JSON value, so the
                // reader accepts that kind only; a null is an absent value. A field
                // that the handler does not declare reaches the binder, which refuses it.
                var declared = handler.Parameters.TryGetValue(parameter.Name, out FieldSchema? field) ? field.Type : null;
                var value = parameter.Value;
                raw[parameter.Name] = (declared, value.ValueKind) switch
                {
                    (_, JsonValueKind.Null) => null,
                    // The writer refuses a number that is not finite (ADR-0015 item 4), so the
                    // reader does too. Until TASK-0099 1e400 read as an infinity (E78).
                    ("number", JsonValueKind.Number) => value.GetDouble() is var number && double.IsFinite(number)
                        ? number
                        : throw DocumentFileException.Load($"The parameter '{parameter.Name}' of {name} is {value.GetRawText()}, which is not a finite double."),
                    ("integer", JsonValueKind.Number) => value.GetInt64(),
                    ("boolean", JsonValueKind.True or JsonValueKind.False) => value.GetBoolean(),
                    ("string" or "guid", JsonValueKind.String) => value.GetString(),
                    (null, _) => value.GetRawText(),
                    _ => throw DocumentFileException.Load($"The parameter '{parameter.Name}' of {name} is a JSON {value.ValueKind}, and its declared type is {declared}."),
                };
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw DocumentFileException.Load($"An entry is not an entry of a document file: {ex.Message}");
        }

        var bound = ParameterBinder.Bind(handler.Parameters, raw);
        if (!bound.IsSuccess)
            throw DocumentFileException.Load($"The entry of version {version} ({name}) does not bind: {bound.FirstMessage}");

        return new DecodedEntry(version, backend, handler.Create(new CommandInput(bound.Values!, commandId, expected)));
    }

    private static string Write(Action<Utf8JsonWriter> body)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            body(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

public sealed record DecodedEntry(long Version, BackendIdentity Backend, Command Command);
