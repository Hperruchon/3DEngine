using System.Globalization;
using Engine.Contracts.Schema;

namespace Engine.Core.Hosting;

// Per ADR-0016: one binder serves every host. A host supplies raw values —
// strings from the CLI, already-typed values from a JSON reader — and the
// binder coerces them against the handler's declared FieldSchema.
//
// Every parse uses CultureInfo.InvariantCulture. Per CLAUDE.md "Determinism
// rules": a culture-sensitive parse makes the same input produce different
// numbers on a machine with a comma decimal separator, and the log would then
// not denote the model.
public static class ParameterBinder
{
    public static BindResult Bind(
        IReadOnlyDictionary<string, FieldSchema> schema,
        IReadOnlyDictionary<string, object?> raw)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var failures = new List<BindFailure>();

        foreach (var (field, declared) in schema)
        {
            if (!raw.TryGetValue(field, out var supplied) || supplied is null)
            {
                if (declared.Required)
                    failures.Add(new BindFailure(field, $"Required field missing: {field}."));
                continue;
            }

            if (TryCoerce(supplied, declared.Type, out var coerced, out var reason))
                values[field] = coerced;
            else
                failures.Add(new BindFailure(field, $"Field '{field}': {reason}"));
        }

        // An unknown field is a failure, not a value the binder discards. A
        // silent discard hides a typo in a script and in an agent request.
        foreach (var field in raw.Keys)
        {
            if (!schema.ContainsKey(field))
                failures.Add(new BindFailure(field, $"Unknown field: {field}."));
        }

        return failures.Count == 0
            ? new BindResult(values, Array.Empty<BindFailure>())
            : new BindResult(null, failures);
    }

    private static bool TryCoerce(
        object supplied,
        string declaredType,
        out object? coerced,
        out string reason)
    {
        coerced = null;
        reason = string.Empty;
        var text = supplied as string;

        switch (declaredType)
        {
            case "string":
                coerced = text ?? Convert.ToString(supplied, CultureInfo.InvariantCulture);
                return true;

            case "number":
                if (supplied is double d) { coerced = d; return true; }
                if (text is not null
                    && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var pd))
                {
                    coerced = pd;
                    return true;
                }
                reason = $"expected a number, received '{Describe(supplied)}'.";
                return false;

            case "integer":
                if (supplied is long l) { coerced = l; return true; }
                if (supplied is int i) { coerced = (long)i; return true; }
                if (text is not null
                    && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pl))
                {
                    coerced = pl;
                    return true;
                }
                reason = $"expected an integer, received '{Describe(supplied)}'.";
                return false;

            case "boolean":
                if (supplied is bool b) { coerced = b; return true; }
                if (text is not null && bool.TryParse(text, out var pb)) { coerced = pb; return true; }
                reason = $"expected 'true' or 'false', received '{Describe(supplied)}'.";
                return false;

            case "guid":
                if (supplied is Guid g) { coerced = g; return true; }
                if (text is not null && Guid.TryParse(text, out var pg)) { coerced = pg; return true; }
                reason = $"expected a GUID, received '{Describe(supplied)}'.";
                return false;

            case "datetime":
                if (supplied is DateTime dt) { coerced = dt; return true; }
                if (text is not null
                    && DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var pdt))
                {
                    coerced = pdt;
                    return true;
                }
                reason = $"expected a round-trip date and time, received '{Describe(supplied)}'.";
                return false;

            // ADR-0016 defers "object" and "array". A command that needs a
            // nested value extends the vocabulary through its own ADR, so a
            // silent acceptance here would hide that decision.
            default:
                reason = $"declared type '{declaredType}' is not supported by the binder.";
                return false;
        }
    }

    private static string Describe(object supplied)
        => supplied as string ?? Convert.ToString(supplied, CultureInfo.InvariantCulture) ?? "null";
}

public sealed record BindFailure(string Field, string Message);

public sealed record BindResult(
    IReadOnlyDictionary<string, object?>? Values,
    IReadOnlyList<BindFailure> Failures)
{
    public bool IsSuccess => Failures.Count == 0;

    // The first failure, for a host that reports one message. The full list
    // stays available for a host that reports each one.
    public string FirstMessage => Failures.Count > 0 ? Failures[0].Message : string.Empty;
}
