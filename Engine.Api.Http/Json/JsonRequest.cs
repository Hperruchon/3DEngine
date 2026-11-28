using System.Text;
using System.Text.Json;
using Engine.Api.Http.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Engine.Api.Http.Json;

// Finding E17 of the codebase review of 2026-09-30, part 3 (register entry R-0028, TASK-0481): the read of a JSON body of
// /commands, /queries and /proposals. Until TASK-0481 an unknown charset of the content type gave HTTP 500 at /commands and
// /queries and "text that is not well-formed UTF-16" at /proposals, and the text of a refused body was the text of the
// exception of System.Text.Json, which names an internal type of the surface, for example
// "Engine.Api.Http.Endpoints.CommandsEndpoint+CommandRequest" for an unknown member.
internal static class JsonRequest
{
    // The refusal of a body that is not JSON or whose charset is not known, with HTTP 415; null for a body that the read takes.
    // ReadFromJsonAsync takes each charset that Encoding.GetEncoding knows, and throws for another one. Finding E252 of the second
    // codebase review of 2026-11-28 (TASK-0488): the check reads the charset as the read takes it, with its quotes, and refuses
    // each exception of GetEncoding. Until TASK-0488 it removed the quotes, so charset="utf-8" passed and the read gave HTTP 500,
    // and it caught ArgumentException only, so charset=utf-7, which .NET refuses with NotSupportedException, gave HTTP 500.
    public static IResult? Refusal(HttpRequest request)
    {
        if (!request.HasJsonContentType())
            return ApiErrorEnvelope.UnsupportedMediaType();
        if (MediaTypeHeaderValue.TryParse(request.ContentType, out var type) && type.Charset is { Length: > 0 } charset)
        {
            try
            {
                _ = Encoding.GetEncoding(charset.ToString());
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
            {
                return ApiErrorEnvelope.UnsupportedCharset(charset.ToString());
            }
        }

        return null;
    }

    // The text of a refused body: the place of the fault, and the reason of the reader for a text that is not JSON. The other
    // faults of a read (a member that the request does not name, a member two times, a value of another type) name the type
    // of the surface in their text, so the text names the place and the three causes.
    public static string Malformed(string what, JsonException exception)
    {
        var at = $"at {exception.Path ?? "$"}";
        if (exception.LineNumber is { } line && exception.BytePositionInLine is { } position)
            at += $", line {line + 1}, byte {position + 1}";

        // The reader throws a type that derives from JsonException; a read of an object wraps it in a JsonException.
        var reader = exception.GetType() != typeof(JsonException) ? exception : exception.InnerException as JsonException;
        if (reader is not null)
        {
            var reason = reader.Message;
            var end = reason.IndexOf(" LineNumber:", StringComparison.Ordinal);
            return $"{what} {at}: {(end >= 0 ? reason[..end] : reason).TrimEnd()}";
        }

        return $"{what} {at}: a member that the request does not name, a member two times, or a value of another type.";
    }
}
