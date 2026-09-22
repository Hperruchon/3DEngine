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
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Number => AsNumber(element),

        // An object and an array are deferred by ADR-0016. The raw text reaches
        // the binder, which reports the declared type as unsupported. The
        // failure is therefore visible and names the field.
        _ => element.GetRawText(),
    };

    private static object AsNumber(JsonElement element)
        => element.TryGetInt64(out var whole) ? whole : element.GetDouble();
}
