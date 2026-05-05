namespace Engine.Cli;

// Hand-rolled --param k=v parser. Per TASK-0002: the wire-format task
// will replace this with JSON input dispatch; until then, generic
// --param keeps the CLI usable for any command we register.
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
