using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Finding T67 of the codebase review of 2026-10-28 (TASK-0089): a gate file must not
// discard the result of a call, and must not start a task as a statement, because the
// check then ends before the work that can fail. The runner reads the source of each
// gate file that it compiles.
//
// Finding T74 of the second codebase review of 2026-10-28 (TASK-0098): the check read
// the text of three receivers, and a task in a local, a task with ConfigureAwait or
// ContinueWith, a task of global::System.Threading.Tasks.Task, and a call of a Func<Task>
// passed. The check now binds each expression with the compiler, and refuses each
// statement whose value is a task, and each local of a task type that the check never
// reads. The runner checks its own forms at each run (SelfCheck).
internal static class DiscardedTasks
{
    // The gate files that RepoGates.csproj compiles.
    private static IEnumerable<string> GateFiles(string root)
        => Directory.EnumerateFiles(Path.Combine(root, "Engine.Tests", "Governance"), "*GateTests.cs")
            .Append(Path.Combine(root, "Engine.Tests", "Diagnostics", "DiagnosticsRegistryGateTests.cs"))
            .Where(File.Exists)
            .OrderBy(file => file, StringComparer.Ordinal);

    // The helpers of the gates, which the compilation needs to bind their calls. The check
    // does not read them.
    private static IEnumerable<string> HelperFiles(string root)
        => new[] { Path.Combine(root, "Engine.Tests", "Governance", "RepositoryFiles.cs"), Path.Combine(root, "Engine.Tests", "Diagnostics", "DiagnosticsScanner.cs") }
            .Where(File.Exists);

    public static IEnumerable<string> Problems(string root)
    {
        var gates = GateFiles(root).Select(file => Parse(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file))).ToList();
        var helpers = HelperFiles(root).Select(file => Parse(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file)));
        return Problems(gates, [.. gates, .. helpers]);
    }

    public static IEnumerable<string> Problems(string relative, string source)
    {
        var tree = Parse(relative, source);
        return Problems([tree], [tree]);
    }

    private static SyntaxTree Parse(string relative, string source)
        => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: relative);

    private static IEnumerable<string> Problems(IReadOnlyList<SyntaxTree> checkedTrees, IReadOnlyList<SyntaxTree> allTrees)
    {
        var compilation = CSharpCompilation.Create("gates", allTrees, References.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        foreach (var tree in checkedTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var relative = tree.FilePath;
            int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "_" }, Right: var right }
                    && right.DescendantNodesAndSelf().Any(part => part is InvocationExpressionSyntax))
                    yield return $"{relative}:{Line(node)}: a check discards the result of a call, so it does not wait for a task that the call starts";

                else if (node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } }
                    && access.Name.Identifier.ValueText is "Run" or "StartNew"
                    && access.Expression.ToString() is "Task" or "Task.Factory" or "System.Threading.Tasks.Task")
                    yield return $"{relative}:{Line(node)}: a check starts a task and does not wait for it";

                else if (node is ExpressionStatementSyntax { Expression: var expression } and not { Expression: AssignmentExpressionSyntax }
                    && IsTask(model.GetTypeInfo(expression).Type))
                    yield return $"{relative}:{Line(node)}: a check computes a task as a statement and does not wait for it";

                else if (node is VariableDeclaratorSyntax declarator
                    && model.GetDeclaredSymbol(declarator) is ILocalSymbol local
                    && IsTask(local.Type)
                    && !IsRead(local, declarator, model))
                    yield return $"{relative}:{Line(node)}: a check keeps a task in '{local.Name}' and never awaits, returns or passes it";
            }
        }
    }

    // Task, ValueTask, their generic forms and a type that derives from them, and the
    // awaitables of ConfigureAwait.
    private static bool IsTask(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            var name = current.ContainingNamespace?.ToDisplayString();
            if (name == "System.Threading.Tasks" && current.Name is "Task" or "ValueTask")
                return true;
            if (name == "System.Runtime.CompilerServices" && current.Name is "ConfiguredTaskAwaitable" or "ConfiguredValueTaskAwaitable")
                return true;
        }
        return false;
    }

    // True when the body that declares the local names it again.
    private static bool IsRead(ILocalSymbol local, VariableDeclaratorSyntax declarator, SemanticModel model)
    {
        var body = declarator.Ancestors().FirstOrDefault(ancestor => ancestor is BlockSyntax or ArrowExpressionClauseSyntax or GlobalStatementSyntax) ?? declarator.SyntaxTree.GetRoot();
        return body.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Any(name => name.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, local));
    }

    // The assemblies of the runtime and of the runner, so that the compiler binds the types of
    // the framework, of xUnit and of the compiler.
    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray());

    // The forms of findings T67 and T74, and forms that wait. A form that the check misses, or
    // a form that waits and that the check refuses, fails the run.
    private static readonly (string Name, string Body, bool Problem)[] Forms =
    [
        ("a discard of a call (T67)", "_ = Work();", true),
        ("Task.Run as a statement (T67)", "Task.Run(() => { });", true),
        ("a task in a local that nobody reads (T74)", "var task = Task.Run(() => { });", true),
        ("Task.Run with ConfigureAwait (T74)", "Task.Run(() => { }).ConfigureAwait(false);", true),
        ("ContinueWith as a statement (T74)", "Task.CompletedTask.ContinueWith(_ => { });", true),
        ("global::System.Threading.Tasks.Task.Run (T74)", "global::System.Threading.Tasks.Task.Run(() => { });", true),
        ("a call of a Func<Task> (T74)", "Func<Task> start = Work; start();", true),
        ("an await", "await Task.Run(() => { });", false),
        ("a local that the check awaits", "var task = Work(); await task;", false),
        ("a task that the check passes", "await Task.WhenAll(Work(), Work());", false),
        ("a call that returns no task", "Console.WriteLine();", false),
    ];

    public static IEnumerable<string> SelfCheck()
    {
        foreach (var (name, body, problem) in Forms)
        {
            var source = "using System;\nusing System.Threading.Tasks;\npublic class Probe\n{\n    private static Task Work() => Task.CompletedTask;\n\n"
                + $"    public async Task Check()\n    {{\n        {body}\n        await Task.CompletedTask;\n    }}\n}}\n";
            var found = Problems("Probe.cs", source).Any();
            if (found != problem)
                yield return problem ? $"the runner does not refuse {name}" : $"the runner refuses {name}, which waits";
        }
    }
}
