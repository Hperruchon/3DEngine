using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Finding T67 of the codebase review of 2026-10-28 (TASK-0089): a gate file must not
// discard the result of a call, and must not start a task as a statement, because the
// check then ends before the work that can fail. The runner reads the source of each
// gate file that it compiles.
internal static class DiscardedTasks
{
    // The gate files that RepoGates.csproj compiles.
    private static IEnumerable<string> GateFiles(string root)
        => Directory.EnumerateFiles(Path.Combine(root, "Engine.Tests", "Governance"), "*GateTests.cs")
            .Append(Path.Combine(root, "Engine.Tests", "Diagnostics", "DiagnosticsRegistryGateTests.cs"))
            .Where(File.Exists)
            .OrderBy(file => file, StringComparer.Ordinal);

    public static IEnumerable<string> Problems(string root)
        => GateFiles(root).SelectMany(file => Problems(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file)));

    public static IEnumerable<string> Problems(string relative, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: relative);
        int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            if (node is AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "_" }, Right: var right }
                && right.DescendantNodesAndSelf().Any(part => part is InvocationExpressionSyntax))
                yield return $"{relative}:{Line(node)}: a check discards the result of a call, so it does not wait for a task that the call starts";

            if (node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } }
                && access.Name.Identifier.ValueText is "Run" or "StartNew"
                && access.Expression.ToString() is "Task" or "Task.Factory" or "System.Threading.Tasks.Task")
                yield return $"{relative}:{Line(node)}: a check starts a task and does not wait for it";
        }
    }
}
