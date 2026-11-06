using System.Xml.Linq;

namespace Engine.Tests.Governance;

// The rule of the dependency graph for the built assemblies (TASK-0192). It reads the assemblies of the test run, not
// files of the repository, so it stays a test only and eng/repo-gates does not compile it, as SchemaEndpointGateTests.
public class BuiltAssemblyReferenceTests
{
    // Finding T136 of the codebase review of 2026-11-05 (b) (TASK-0192): the property AdditionalExplicitAssemblyReferences,
    // and a response file of the compiler, gave Engine.Core a reference to 3DEngine.Core, and each gate passed, because
    // the gates read the items of the project files. The compiler writes each assembly that a built assembly uses into its
    // metadata. So each first-party assembly that a built Engine.* assembly references is one that its project reaches
    // through ProjectReference, directly or through another project. A property, a response file or a target can add a
    // reference, and the metadata shows each one, so the class is closed for each assembly that the test reads; until
    // TASK-0198 it read the Engine assemblies only (T141).
    [Fact]
    public void Each_Built_Engine_Assembly_References_Only_The_Projects_That_Its_Graph_Reaches()
    {
        var graph = DependencyDirectionGateTests.Graph();
        var assemblyOf = DependencyDirectionGateTests.ProjectFiles().ToDictionary(
            file => Path.GetFileNameWithoutExtension(file),
            file => XDocument.Load(file).Descendants().FirstOrDefault(e => DependencyDirectionGateTests.Is(e, "AssemblyName"))?.Value.Trim() ?? Path.GetFileNameWithoutExtension(file),
            StringComparer.Ordinal);
        var firstParty = assemblyOf.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> Reached(string project)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>(graph.GetValueOrDefault(project) ?? []);
            while (stack.Count > 0)
            {
                var next = stack.Pop();
                if (reached.Add(next))
                {
                    foreach (var further in graph.GetValueOrDefault(next) ?? [])
                        stack.Push(further);
                }
            }
            return reached;
        }

        // Finding T141 of the codebase review of 2026-11-06 (TASK-0198): the test read the Engine assemblies only, so the
        // render kernel could reference Engine.Contracts by the same property. The render kernel and the Vulkan layer are
        // not in the output of this project; the pipeline builds the solution before it runs the tests, so their assembly
        // is in their own output folder, with the configuration and the framework of this test run.
        string[] engine = ["Engine.Contracts", "Engine.Core", "Engine.Kernel", "Engine.Geometry.Manifold", "Engine.Api.Http", "Engine.Cli", "Engine.Api.Http.Server"];
        string[] render = ["3DEngine.Core", "3DEngine.Vulkan"];
        var output = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var (framework, configuration) = (output.Name, output.Parent!.Name);

        // Finding T142 (TASK-0198): Engine.Kernel took an assembly of a third party by the same property, and each gate
        // passed. On the path of the log a built assembly references only the assemblies of the shared framework and the
        // first-party assemblies that its graph reaches (CLAUDE.md, determinism rules).
        string[] log = ["Engine.Contracts", "Engine.Core", "Engine.Kernel"];
        var shared = Directory.EnumerateFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")
            .Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var offences = new List<string>();
        foreach (var project in engine.Concat(render))
        {
            var path = render.Contains(project)
                ? RepositoryFiles.Path(project, "bin", configuration, framework, assemblyOf[project] + ".dll")
                : Path.Combine(AppContext.BaseDirectory, assemblyOf[project] + ".dll");
            Assert.True(File.Exists(path), $"The built assembly of {project} is not at {path}. Build the solution before the tests.");
            using var stream = File.OpenRead(path);
            using var pe = new System.Reflection.PortableExecutable.PEReader(stream);
            var metadata = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
            var allowed = Reached(project).Select(reached => assemblyOf.GetValueOrDefault(reached, reached)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in metadata.AssemblyReferences)
            {
                var name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                if (firstParty.Contains(name) && !allowed.Contains(name))
                    offences.Add($"{project} references the assembly {name}, which its graph of ProjectReference does not reach");
                else if (log.Contains(project) && !firstParty.Contains(name) && !shared.Contains(name))
                    offences.Add($"{project} is on the path of the log and references the assembly {name}, which is not of the shared framework");
            }
        }

        Assert.True(offences.Count == 0, "A built assembly must use only the projects of its graph.\n  " + string.Join("\n  ", offences));
    }
}
