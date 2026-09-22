using System.Text;
using Engine.Core.Hosting;

namespace Engine.Cli;

// Per ADR-0016: the command list and each example come from the handler
// declarations, not from a list that a person keeps by hand. A new command
// therefore changes no file in Engine.Cli.
internal static class Usage
{
    private const string Header =
"""
Usage:
  engine apply <command-name> [--param k=v ...]   Apply a command, print CommandResult JSON.
  engine query <query-name> [--param k=v ...]     Run a query, print QueryResult JSON.
  engine help                                     Print this usage.

Each invocation builds a fresh in-memory engine. There is no persistence
between invocations.

Exit codes:
  0  Applied
  1  Rejected or Cancelled (commands), or query rejected
  2  Invalid usage

""";

    public static string Text => Build();

    private static string Build()
    {
        var commands = HandlerCatalog.CommandHandlers();
        var queries = HandlerCatalog.QueryHandlers();

        var sb = new StringBuilder(Header);

        sb.Append("Registered commands: ")
          .AppendJoin(", ", commands.Select(h => h.CommandName))
          .AppendLine();
        sb.Append("Registered queries:  ")
          .AppendJoin(", ", queries.Select(h => h.QueryName))
          .AppendLine();

        sb.AppendLine().AppendLine("Examples:");
        foreach (var handler in commands)
            sb.Append("  engine apply ").AppendLine(Example(handler.CommandName, handler.Parameters));
        foreach (var handler in queries)
            sb.Append("  engine query ").AppendLine(Example(handler.QueryName, handler.Parameters));

        sb.AppendLine();
        return sb.ToString();
    }

    private static string Example(
        string name,
        IReadOnlyDictionary<string, Contracts.Schema.FieldSchema> parameters)
    {
        var sb = new StringBuilder(name);
        foreach (var (field, schema) in parameters)
            sb.Append(" --param ").Append(field).Append('=').Append(Placeholder(schema.Type));
        return sb.ToString();
    }

    private static string Placeholder(string type) => type switch
    {
        "number" => "<number>",
        "integer" => "<integer>",
        "boolean" => "<true|false>",
        "guid" => "<guid>",
        "datetime" => "<iso-8601>",
        _ => "<value>",
    };
}
