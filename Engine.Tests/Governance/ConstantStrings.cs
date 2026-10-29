using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tests.Governance;

// The string values that the compiler computes in a set of C# files (TASK-0107). A gate that
// reads text misses a value that the compiler joins or resolves: "cim" + "gui", a raw string,
// nameof(cimgui), an escape such as S, and a constant of another file (findings T81 and
// T82 of the codebase review of 2026-10-29). One compilation binds each file with the
// assemblies of the runtime, so a constant of one project that another project names binds too.
internal static class ConstantStrings
{
    // Each expression of each file whose constant value is a string, with its line.
    public static IEnumerable<(string Path, int Line, string Value)> Values(IEnumerable<(string Path, string Text)> files)
    {
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(LanguageVersion.Preview), path: file.Path))
            .ToList();
        var compilation = CSharpCompilation.Create("constants", trees, References.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var expression in tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>())
            {
                if (model.GetConstantValue(expression) is { HasValue: true, Value: string value })
                    yield return (tree.FilePath, expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1, value);
            }
        }
    }

    // The assemblies of the runtime, so that the compiler binds the attributes and the types
    // of the framework. The folder of System.Private.CoreLib only: the test process also loads
    // the engine assemblies, and the program repo-gates does not, so both bind the same files.
    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(typeof(object).Assembly.Location), StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray());
}
