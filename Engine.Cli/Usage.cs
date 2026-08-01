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

Registered commands: NoOp, CreateBox, Translate, Subtract
Registered queries:  GetBoundingBox

Examples:
  engine apply NoOp --param echo=hello
  engine apply CreateBox --param sizeX=10 --param sizeY=20 --param sizeZ=30
  engine apply Translate --param bodyId=<guid> --param dx=5 --param dy=0 --param dz=0
  engine apply Subtract --param minuendBodyId=<guid> --param subtrahendBodyId=<guid>
  engine query GetBoundingBox --param bodyId=<guid>

""";
}
