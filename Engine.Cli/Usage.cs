namespace Engine.Cli;

internal static class Usage
{
    public const string Text =
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

Registered commands: NoOp
Registered queries:  (none)

Examples:
  engine apply NoOp --param echo=hello
  engine query GetEntity --param id=42

""";
}
