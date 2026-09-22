namespace Engine.Cli;

// The --param k=v parser for the command line. It splits each pair and
// nothing else.
//
// TASK-0002 said that a wire-format task would replace this with JSON input.
// TASK-0025 refused that plan and the project keeps this parser. Three
// reasons. The convention --param k=v is normal for a command line. ADR-0016
// gave type coercion and validation to ParameterBinder, therefore this file
// splits text and makes no decision about a value. JSON input on the command
// line would copy the HTTP surface and give nothing that the HTTP surface does
// not already give. See the accepted compromise R-0008 in docs/register.md.
internal static class ArgParser
{
    public static Dictionary<string, string> ParseParams(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg != "--param")
                throw new ArgParseException($"Unexpected argument: '{arg}'. Expected '--param'.");

            if (i + 1 >= args.Length)
                throw new ArgParseException("'--param' requires a key=value argument.");

            var kv = args[++i];
            var eq = kv.IndexOf('=');
            if (eq < 0)
                throw new ArgParseException($"Invalid --param value: '{kv}'. Expected key=value.");
            if (eq == 0)
                throw new ArgParseException($"Invalid --param value: '{kv}'. Empty key.");

            var key = kv[..eq];
            var value = kv[(eq + 1)..];
            if (!result.TryAdd(key, value))
                throw new ArgParseException($"Duplicate --param key: '{key}'.");
        }
        return result;
    }
}

internal sealed class ArgParseException : Exception
{
    public ArgParseException(string message) : base(message) { }
}
