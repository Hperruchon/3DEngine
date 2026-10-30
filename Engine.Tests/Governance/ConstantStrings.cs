using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tests.Governance;

// The string values that the compiler computes in a set of C# files (TASK-0107). A gate that
// reads text misses a value that the compiler joins or resolves: "cim" + "gui", a raw string,
// nameof(cimgui), an escape such as S, and a constant of another file (findings T81 and
// T82 of the codebase review of 2026-10-29).
//
// Finding T90 of the codebase review of 2026-10-30 (TASK-0114): one compilation of all files made
// two internal types of one name in two projects ambiguous, and a joined code passed. Each file now
// binds in the compilation of its own project: the files of the project, of each project that it
// references, and the assemblies of the runtime. The project of a file is the first folder of its
// path, as each project of the repository lies in a folder of the root.
//
// Finding T97 of the codebase review of 2026-10-30 (b) (TASK-0123): the files of the referenced
// projects went into the same compilation, so a type of one name in a project and in a project that
// it references gave error CS0101 there, and a joined code passed. Each project is now its own
// compilation, and a project gets the compilations of the projects that it references, as the
// build does. Each compilation grants its internals to each other project, as the items
// InternalsVisibleTo of the repository do for the projects that read them.
// Finding T98 (TASK-0123): the references came from the project file only, and a reference in a
// Directory.Build.props file passed. They now come from each file that MSBuild reads for the
// project (DependencyDirectionGateTests.FilesThatMsBuildReads).
internal static class ConstantStrings
{
    // Each expression of each file whose constant value is a string, with its line.
    public static IEnumerable<(string Path, int Line, string Value)> Values(IEnumerable<(string Path, string Text)> files)
    {
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(LanguageVersion.Preview), path: file.Path))
            .ToList();
        var byProject = trees.GroupBy(Project).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var grants = CSharpSyntaxTree.ParseText(
            string.Concat(byProject.Keys.Select(name => $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{name}\")]\n")),
            new CSharpParseOptions(LanguageVersion.Preview));
        var compilations = new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal);

        CSharpCompilation CompilationOf(string project)
        {
            if (compilations.TryGetValue(project, out var known))
                return known;
            var referenced = References(project).Where(byProject.ContainsKey).Select(name => CompilationOf(name).ToMetadataReference());
            var compilation = CSharpCompilation.Create(
                project, [.. byProject[project], grants], [.. RuntimeReferences.Value, .. referenced],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            compilations[project] = compilation;
            return compilation;
        }

        foreach (var (project, own) in byProject.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var compilation = CompilationOf(project);
            foreach (var tree in own)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var expression in tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>())
                {
                    if (model.GetConstantValue(expression) is { HasValue: true, Value: string value })
                        yield return (tree.FilePath, expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1, value);
                }
            }
        }
    }

    private static string Project(SyntaxTree tree) => tree.FilePath.Split('/')[0];

    // The projects that a project references, and each project that they reference, from the
    // items ProjectReference of each file that MSBuild reads for its project file in the repository.
    // A folder with no project file references nothing. A test gives another reader of the files.
    internal static IReadOnlyCollection<string> References(string project, Func<string, IEnumerable<string>>? filesOf = null)
    {
        filesOf ??= DependencyDirectionGateTests.FilesThatMsBuildReads;
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>([project]);
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            var projectFile = RepositoryFiles.Path(next, next + ".csproj");
            if (!File.Exists(projectFile))
                continue;
            foreach (var reference in filesOf(projectFile)
                .SelectMany(file => XDocument.Load(file).Descendants().Where(e => DependencyDirectionGateTests.Is(e, "ProjectReference"))))
            {
                var include = DependencyDirectionGateTests.Value(reference, "Include");
                if (include is null)
                    continue;
                var name = Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
                if (name != project && found.Add(name))
                    queue.Enqueue(name);
            }
        }
        return found;
    }

    // The assemblies of the runtime, so that the compiler binds the attributes and the types
    // of the framework. The folder of System.Private.CoreLib only: the test process also loads
    // the engine assemblies, and the program repo-gates does not, so both bind the same files.
    private static readonly Lazy<MetadataReference[]> RuntimeReferences = new(() =>
        ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(typeof(object).Assembly.Location), StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray());
}
