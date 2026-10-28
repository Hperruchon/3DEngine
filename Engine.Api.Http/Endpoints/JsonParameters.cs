using System.Text.Json;

namespace Engine.Api.Http.Endpoints;

// Per ADR-0016: the transport converts its own wire form into neutral values,
// and ParameterBinder in Engine.Core coerces them against the declared schema.
//
// The conversion keeps a number as a double and does not pass it through a
// string. A string round-trip loses precision, and CLAUDE.md "Determinism
// rules" forbids a path that can change a logged number.
//
// Engine.Core therefore needs no reference to System.Text.Json, and the
// kernel stays free of a transport concern per ADR-0011.
internal static class JsonParameters
{
    public static IReadOnlyDictionary<string, object?> AsRaw(
        Dictionary<string, JsonElement>? parameters)
    {
        var raw = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (parameters is null)
            return raw;

        foreach (var (field, element) in parameters)
            raw[field] = Convert(element);

        return raw;
    }

    private static object? Convert(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => Text(element),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Number => AsNumber(element),

        // An object and an array are deferred by ADR-0016. They reach the binder as a
        // value that is not a string, so a field of each declared type refuses them and
        // names the field. Until TASK-0090 the raw text reached the binder as a string,
        // and a string field took it (finding E72 of the codebase review of 2026-10-28).
        _ => new NotAString($"the JSON value {element.GetRawText()}"),
    };

    // A JSON string that is not well-formed UTF-16 is refused as a value, and not with
    // HTTP 500 (finding E73).
    private static object Text(JsonElement element)
    {
        try
        {
            return element.GetString()!;
        }
        catch (InvalidOperationException)
        {
            return new NotAString("a string that is not well-formed UTF-16");
        }
    }

    // A value that the binder must not read as text; its text names it in a refusal.
    private sealed record NotAString(string Description)
    {
        public override string ToString() => Description;
    }

    private static object AsNumber(JsonElement element)
        => element.TryGetInt64(out var whole) ? whole : element.GetDouble();
}
