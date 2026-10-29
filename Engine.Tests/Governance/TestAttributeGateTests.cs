using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Engine.Tests.Governance;

// Finding T54 of the codebase review of 2026-10-17 (TASK-0078). C# looks for the name
// of an attribute in the enclosing namespaces and types before a global using, so a
// type FactAttribute in the namespace Engine.Tests takes the place of the attribute of
// xUnit, and each [Fact] below that namespace finds no test. A review agent did it with
// one file that is not a gate file: the build gave no warning, and Engine.Tests ran no
// test. The same holds for an alias, for a nested type, and for a type named Assert,
// which can make each assertion pass.
//
// This gate refuses each declaration of a name of xUnit in a test project. It runs from
// the gate program, which compiles the gate files only, so a shadow type of the test
// assembly does not reach it there.
//
// Finding T62 of the codebase review of 2026-10-28 (TASK-0088). A support file with
// [assembly: TestFramework] and a framework that derives from XunitTestFramework gave
// each failed test as passed, with the exact count of tests. The gate therefore also
// refuses each point where a test project can extend the runner of xUnit, and a type
// with a name of xUnit in a product project, in a namespace that a test namespace sees.
public class TestAttributeGateTests
{
    private static readonly string[] TestProjects = ["Engine.Tests", "3DEngine.Tests", "3DEngine.ImageTests"];

    // The namespaces of the test sources. A type in one of them, or in a namespace that
    // encloses one of them, is found before the global using of Xunit.
    private static readonly string[] TestNamespaces = ["Engine.Tests", "ThreeDEngine.Tests", "ThreeDEngine.ImageTests"];

    // The attributes that give xUnit a framework, an order or a discoverer of its own,
    // with no suffix Attribute.
    private static readonly HashSet<string> ExtensionAttributes = new(StringComparer.Ordinal)
    {
        "TestFramework", "TestFrameworkDiscoverer", "TestCaseOrderer", "TestCollectionOrderer",
        "XunitTestCaseDiscoverer", "TraitDiscoverer", "DataDiscoverer",
    };

    // The types of xUnit that a framework, a runner, a sink or a discoverer derives from
    // or implements. Each other type of Xunit.Sdk needs that namespace, which the gate
    // also refuses in a test project.
    private static readonly HashSet<string> ExtensionTypes = new(StringComparer.Ordinal)
    {
        "XunitTestFramework", "XunitTestFrameworkExecutor", "XunitTestFrameworkDiscoverer",
        "TestFramework", "TestFrameworkExecutor", "TestFrameworkDiscoverer", "LongLivedMarshalByRefObject",
        "ITestFramework", "ITestFrameworkExecutor", "ITestFrameworkDiscoverer", "ITestFrameworkAttribute",
        "ITestFrameworkTypeDiscoverer", "IMessageSink", "IMessageSinkWithTypes", "IMessageBus",
        "IXunitTestCase", "IXunitTestCaseDiscoverer", "ITestCase", "XunitTestCase", "ITestCaseOrderer",
        "ITestCollectionOrderer", "ITraitDiscoverer", "IDataDiscoverer", "IRunnerReporter",
        "IXunitTestCollectionFactory",
    };

    // The names that a test source finds through the global using of Xunit, each with
    // and without the suffix Attribute, and the namespace itself.
    private static readonly HashSet<string> XunitNames = new(StringComparer.Ordinal)
    {
        "Fact", "FactAttribute", "Theory", "TheoryAttribute", "InlineData", "InlineDataAttribute",
        "MemberData", "MemberDataAttribute", "ClassData", "ClassDataAttribute", "Trait", "TraitAttribute",
        "Collection", "CollectionAttribute", "CollectionDefinition", "CollectionDefinitionAttribute",
        "Assert", "Record", "Xunit",
    };

    [Fact]
    public void No_Test_Source_Declares_A_Name_Of_Xunit()
    {
        var offences = new List<string>();

        foreach (var project in TestProjects)
        {
            var directory = RepositoryFiles.Path(project);
            Assert.True(Directory.Exists(directory), $"{project} was not found.");

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepositoryFiles.Relative(file);
                if (relative.StartsWith($"{project}/obj/", StringComparison.Ordinal) || relative.StartsWith($"{project}/bin/", StringComparison.Ordinal))
                    continue;

                offences.AddRange(Offences(relative, File.ReadAllText(file)));
            }
        }

        Assert.True(
            offences.Count == 0,
            "A test source must not declare a name that a test finds through Xunit: such a type or alias "
            + "takes the place of the attribute or of Assert, and the tests below it do not run or always "
            + "pass (finding T54 of the codebase review of 2026-10-17). Offences:\n  " + string.Join("\n  ", offences));
    }

    // The forms of finding T54 and its neighbours; each one must be an offence.
    [Theory]
    [InlineData("namespace Engine.Tests;\npublic class FactAttribute : System.Attribute { }", "declares the type FactAttribute")]
    [InlineData("namespace Engine.Tests.Http;\npublic sealed class TheoryAttribute : System.Attribute { }", "declares the type TheoryAttribute")]
    [InlineData("namespace Engine.Tests;\npublic class Outer { public class InlineDataAttribute : System.Attribute { } }", "declares the type InlineDataAttribute")]
    [InlineData("namespace Engine.Tests;\npublic static class Assert { public static void True(bool value) { } }", "declares the type Assert")]
    [InlineData("global using FactAttribute = System.ObsoleteAttribute;", "declares the alias FactAttribute")]
    [InlineData("using Fact = System.ObsoleteAttribute;\nnamespace Engine.Tests;", "declares the alias Fact")]
    [InlineData("namespace Engine.Tests.Xunit { public class Probe { } }", "declares the namespace Engine.Tests.Xunit")]
    [InlineData("namespace Engine.Tests;\npublic delegate void Fact();", "declares the type Fact")]
    public void Each_Form_Of_Finding_T54_Is_An_Offence(string source, string expected)
    {
        var offences = Offences("Probe.cs", source).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // A name that only contains a name of xUnit is permitted.
    [Fact]
    public void A_Derived_Attribute_With_Its_Own_Name_Is_Permitted()
    {
        Assert.Empty(Offences("Probe.cs", "namespace Engine.Tests;\npublic sealed class NativeManifoldFactAttribute : Xunit.FactAttribute { }"));
    }

    [Fact]
    public void No_Test_Source_Extends_The_Runner_Of_Xunit()
    {
        var offences = new List<string>();

        foreach (var project in TestProjects)
        {
            foreach (var file in Sources(RepositoryFiles.Path(project)))
                offences.AddRange(ExtensionOffences(RepositoryFiles.Relative(file), File.ReadAllText(file)));
        }

        Assert.True(
            offences.Count == 0,
            "A test project must not give xUnit a framework, a runner, a message sink, an order or a "
            + "discoverer of its own: such a type can give each failed test as passed, and the count of "
            + "tests stays (finding T62 of the codebase review of 2026-10-28). Offences:\n  " + string.Join("\n  ", offences));
    }

    [Fact]
    public void No_Product_Source_Declares_A_Name_Of_Xunit_That_A_Test_Sees()
    {
        var offences = new List<string>();

        foreach (var project in ProductProjects())
        {
            foreach (var file in Sources(project))
                offences.AddRange(ShadowOffences(RepositoryFiles.Relative(file), File.ReadAllText(file)));
        }

        Assert.True(
            offences.Count == 0,
            "A project that a test project can reference must not declare a name of xUnit in the global "
            + "namespace, in a test namespace or in a namespace that encloses one: a test finds that type "
            + "before Xunit (finding T62 of the codebase review of 2026-10-28). Offences:\n  " + string.Join("\n  ", offences));
    }

    // The file of finding T62, as the review gives it.
    private const string FrameworkOfT62 = """
        using System.Reflection;
        using Xunit.Abstractions;
        using Xunit.Sdk;

        [assembly: Xunit.TestFramework("Engine.Tests.Http.SupportFramework", "Engine.Tests")]

        namespace Engine.Tests.Http;

        public sealed class SupportFramework(IMessageSink sink) : XunitTestFramework(sink)
        {
            protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
                => new SupportExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }

        public sealed class SupportExecutor(AssemblyName name, ISourceInformationProvider source, IMessageSink diagnostics)
            : XunitTestFrameworkExecutor(name, source, diagnostics)
        {
        }

        public sealed class SupportSink(IMessageSink inner) : LongLivedMarshalByRefObject, IMessageSink
        {
            public bool OnMessage(IMessageSinkMessage message) => inner.OnMessage(message);
        }
        """;

    // The forms of finding T62 and its neighbours; each one must be an offence.
    [Theory]
    [InlineData(FrameworkOfT62, "applies the attribute TestFramework")]
    [InlineData(FrameworkOfT62, "imports Xunit.Sdk")]
    [InlineData(FrameworkOfT62, "derives SupportFramework from XunitTestFramework")]
    [InlineData(FrameworkOfT62, "derives SupportSink from IMessageSink")]
    [InlineData("[assembly: global::Xunit.TestFrameworkAttribute(\"A\", \"B\")]", "applies the attribute TestFramework")]
    [InlineData("namespace Engine.Tests;\n[Xunit.TestCaseOrderer(\"A\", \"B\")]\npublic class Probe { }", "applies the attribute TestCaseOrderer")]
    [InlineData("[assembly: Xunit.TestCollectionOrderer(\"A\", \"B\")]", "applies the attribute TestCollectionOrderer")]
    [InlineData("namespace Engine.Tests;\n[Xunit.Sdk.XunitTestCaseDiscoverer(\"A\", \"B\")]\npublic sealed class ProbeAttribute : Xunit.FactAttribute { }", "applies the attribute XunitTestCaseDiscoverer")]
    [InlineData("namespace Engine.Tests;\npublic sealed class Probe : Xunit.Abstractions.IMessageSink { }", "derives Probe from IMessageSink")]
    [InlineData("namespace Engine.Tests;\npublic sealed class Probe : Xunit.Sdk.XunitTestFramework { }", "names Xunit.Sdk")]
    [InlineData("global using Xunit.Sdk;", "imports Xunit.Sdk")]
    [InlineData("using static Xunit.Sdk.ExceptionUtility;", "imports Xunit.Sdk")]
    [InlineData("using X = Xunit;", "aliases Xunit")]
    [InlineData("using Order = Xunit.TestCaseOrdererAttribute;", "aliases Xunit.TestCaseOrdererAttribute")]
    [InlineData("extern alias Runner;", "declares the extern alias Runner")]
    [InlineData("namespace Engine.Tests;\npublic class Probe { object o = typeof(Xun\\u0069t.Sdk.XunitTestFramework); }", "names Xunit.Sdk")]
    public void Each_Form_Of_Finding_T62_Is_An_Offence(string source, string expected)
    {
        var offences = ExtensionOffences("Probe.cs", source).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // The output helper of xUnit lives in Xunit.Abstractions, and a test may use it.
    [Fact]
    public void The_Output_Helper_Of_Xunit_Is_Permitted()
    {
        Assert.Empty(ExtensionOffences("Probe.cs", "using Xunit.Abstractions;\nnamespace Engine.Tests;\npublic class Probe(ITestOutputHelper output) { }"));
    }

    // A product type with a name of xUnit that a test namespace sees is an offence; the
    // same name in a namespace that no test namespace sees is permitted.
    [Theory]
    [InlineData("namespace Engine;\npublic static class Assert { }", true)]
    [InlineData("public static class Assert { }", true)]
    [InlineData("namespace Engine.Tests.Http { public class FactAttribute : System.Attribute { } }", true)]
    [InlineData("namespace ThreeDEngine;\npublic class Record { }", true)]
    [InlineData("namespace Engine.Core.Xunit;\npublic class Probe { }", true)]
    [InlineData("namespace Engine.Core;\npublic static class Assert { }", false)]
    [InlineData("namespace ThreeDEngine.Desktop;\npublic class Record { }", false)]
    public void A_Product_Type_With_A_Name_Of_Xunit_Is_An_Offence_Where_A_Test_Sees_It(string source, bool offence)
    {
        Assert.Equal(offence, ShadowOffences("Probe.cs", source).Any());
    }

    // Finding T68 of the second codebase review of 2026-10-28 (TASK-0097): the gate
    // skipped a folder bin or obj at each depth, and MSBuild compiles each .cs file
    // below the folder of a project except bin/ and obj/ at its root. A framework of
    // T62 in Engine.Tests/Http/bin/ passed the gate, and each failed test passed. The
    // gate now reads the files that MSBuild compiles, as DeterminismCallGateTests does.
    private static IEnumerable<string> Sources(string directory)
    {
        Assert.True(Directory.Exists(directory), $"{directory} was not found.");
        var project = RepositoryFiles.Relative(directory);
        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsCompiled(project, RepositoryFiles.Relative(file)));
    }

    private static bool IsCompiled(string project, string relative)
        => !relative.StartsWith($"{project}/obj/", StringComparison.Ordinal)
        && !relative.StartsWith($"{project}/bin/", StringComparison.Ordinal);

    [Theory]
    [InlineData("Engine.Tests", "Engine.Tests/Http/bin/SupportFramework.cs", true)]
    [InlineData("Engine.Tests", "Engine.Tests/Http/obj/SupportFramework.cs", true)]
    [InlineData("3DEngine.Tests", "3DEngine.Tests/bin/x/SupportFramework.cs", false)]
    [InlineData("Engine.Tests", "Engine.Tests/obj/Debug/Engine.Tests.AssemblyInfo.cs", false)]
    public void The_Gate_Reads_Each_Source_That_MSBuild_Compiles(string project, string relative, bool compiled)
    {
        Assert.Equal(compiled, IsCompiled(project, relative));
    }

    // Finding T69 of the second codebase review of 2026-10-28 (TASK-0097): a Using item
    // with an alias in the project file of 3DEngine.Tests named each type of the
    // framework of T62, and the source that used only the aliases passed the gate. The
    // project file of a test project may import the namespace Xunit, as each one does,
    // and nothing else of xUnit; it adds no source and imports no file.
    //
    // Finding T79 of the codebase review of 2026-10-29 (TASK-0105): MSBuild also reads
    // the file <project>.csproj.user and each Directory.Build file above the project,
    // and the gate read the project file only. Twelve aliases in 3DEngine.Tests.csproj.user
    // hid a framework of T62, and each failed test passed. The gate now reads each file
    // that DependencyDirectionGateTests.FilesThatMsBuildReads gives for a test project.
    [Fact]
    public void No_Test_Project_File_Brings_A_Name_Or_A_Source_Past_The_Gate()
    {
        var offences = new List<string>();

        foreach (var project in TestProjects)
        {
            var file = RepositoryFiles.Path(project, project + ".csproj");
            Assert.True(File.Exists(file), $"{file} was not found.");
            offences.AddRange(TestProjectOffences(file));
        }

        Assert.True(
            offences.Count == 0,
            "An MSBuild file of a test project may hold <Using Include=\"Xunit\" /> and no other import of a "
            + "name of xUnit, no alias with a name of xUnit, no Compile item, no Import and no property that "
            + "moves the output folders or changes the default items: each one can bring a test framework or a "
            + "shadow type past this gate (findings T69 and T79 of the codebase reviews of 2026-10-28 and "
            + "2026-10-29). Offences:\n  " + string.Join("\n  ", offences));
    }

    private static IEnumerable<string> TestProjectOffences(string projectFile)
        => DependencyDirectionGateTests.FilesThatMsBuildReads(projectFile)
            .SelectMany(file => ProjectFileOffences(RepositoryFiles.Relative(file), File.ReadAllText(file)));

    // The .user file of T79: the gate reads it beside the project file.
    [Fact]
    public void The_User_File_Of_A_Test_Project_Is_Read()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "3de-t79-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var project = System.IO.Path.Combine(folder, "Probe.csproj");
            File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Using Include=\"Xunit\" /></ItemGroup></Project>");
            File.WriteAllText(project + ".user", "<Project><ItemGroup><Using Include=\"Xunit.Sdk.XunitTestFramework\" Alias=\"FwBase\" /></ItemGroup></Project>");

            var offences = TestProjectOffences(project).ToList();

            Assert.True(
                offences.Any(o => o.Contains("Probe.csproj.user", StringComparison.Ordinal) && o.Contains("imports Xunit.Sdk.XunitTestFramework", StringComparison.Ordinal)),
                "Expected an offence of Probe.csproj.user. Offences:\n  " + string.Join("\n  ", offences));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // Finding T79: the gate leaves out <project>/bin/ and <project>/obj/ by name, and
    // MSBuild leaves out $(BaseOutputPath) and $(BaseIntermediateOutputPath) with
    // $(DefaultItemExcludes). A test project with <BaseOutputPath>out\</BaseOutputPath>
    // compiled 3DEngine.Tests/bin/, and a framework there passed. A test project keeps
    // the default folders and the default items, so the rule of IsCompiled holds.
    private static readonly Regex OutputProperty = new(
        @"^(BaseOutputPath|OutputPath|BaseIntermediateOutputPath|IntermediateOutputPath|MSBuildProjectExtensionsPath|DefaultItemExcludes|DefaultExcludesInProjectFolder|EnableDefaultItems|EnableDefaultCompileItems)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Theory]
    [InlineData("<BaseOutputPath>out\\</BaseOutputPath>", "sets the property BaseOutputPath")]
    [InlineData("<OutputPath>out\\</OutputPath>", "sets the property OutputPath")]
    [InlineData("<baseintermediateoutputpath>o\\</baseintermediateoutputpath>", "sets the property baseintermediateoutputpath")]
    [InlineData("<IntermediateOutputPath>o\\</IntermediateOutputPath>", "sets the property IntermediateOutputPath")]
    [InlineData("<DefaultItemExcludes>x</DefaultItemExcludes>", "sets the property DefaultItemExcludes")]
    [InlineData("<EnableDefaultCompileItems>false</EnableDefaultCompileItems>", "sets the property EnableDefaultCompileItems")]
    public void Each_Form_Of_Finding_T79_Is_An_Offence(string element, string expected)
    {
        var xml = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{element}</PropertyGroup></Project>";
        var offences = ProjectFileOffences("Probe.csproj", xml).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // The forms of finding T69 and its neighbours; each one must be an offence.
    [Theory]
    [InlineData("<Using Include=\"Xunit.TestFrameworkAttribute\" Alias=\"Tf\" />", "imports Xunit.TestFrameworkAttribute")]
    [InlineData("<Using Include=\"Xunit.Sdk.XunitTestFramework\" Alias=\"FwBase\" />", "imports Xunit.Sdk.XunitTestFramework")]
    [InlineData("<Using Include=\"Xunit.Sdk\" />", "imports Xunit.Sdk")]
    [InlineData("<Using Include=\"Xunit\" Static=\"true\" />", "imports Xunit")]
    [InlineData("<Using Include=\"Xunit\" Alias=\"X\" />", "imports Xunit")]
    [InlineData("<using include=\"xunit.sdk\" />", "imports xunit.sdk")]
    [InlineData("<Using Include=\"Xunit%2ESdk\" />", "imports Xunit.Sdk")]
    [InlineData("<Using Include=\"Xunit.Sdk\"><Alias>FwBase</Alias></Using>", "imports Xunit.Sdk")]
    [InlineData("<Using Include=\"System.ObsoleteAttribute\" Alias=\"FactAttribute\" />", "aliases FactAttribute")]
    [InlineData("<Compile Include=\"..\\shared\\Framework.cs\" />", "adds the Compile item ..\\shared\\Framework.cs")]
    [InlineData("<Import Project=\"..\\shared\\Usings.props\" />", "imports the file ..\\shared\\Usings.props")]
    public void Each_Form_Of_Finding_T69_Is_An_Offence(string element, string expected)
    {
        var xml = $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{element}</ItemGroup></Project>";
        var offences = ProjectFileOffences("Probe.csproj", xml).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    [Fact]
    public void The_Import_Of_The_Namespace_Xunit_Is_Permitted()
    {
        Assert.Empty(ProjectFileOffences("Probe.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Using Include=\"Xunit\" /></ItemGroup></Project>"));
    }

    // MSBuild reads the names of elements and attributes with no regard to case, and
    // unescapes %XX in a value.
    private static IEnumerable<string> ProjectFileOffences(string relative, string xml)
    {
        var document = System.Xml.Linq.XDocument.Parse(xml);
        foreach (var element in document.Descendants())
        {
            var name = element.Name.LocalName;
            string? Value(string key) => element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value
                ?? element.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
            string Unescaped(string? value) => Uri.UnescapeDataString((value ?? string.Empty).Trim());

            if (OutputProperty.IsMatch(name))
                yield return $"{relative}: sets the property {name}";
            else if (name.Equals("Compile", StringComparison.OrdinalIgnoreCase))
                yield return $"{relative}: adds the Compile item {Unescaped(Value("Include") ?? Value("Update"))}";
            else if (name.Equals("Import", StringComparison.OrdinalIgnoreCase))
                yield return $"{relative}: imports the file {Unescaped(Value("Project"))}";
            else if (name.Equals("Using", StringComparison.OrdinalIgnoreCase))
            {
                var include = Unescaped(Value("Include") ?? Value("Update"));
                var alias = Unescaped(Value("Alias"));
                var isStatic = Unescaped(Value("Static"));
                if (include.StartsWith("Xunit", StringComparison.OrdinalIgnoreCase)
                    && (include != "Xunit" || alias.Length > 0 || isStatic.Length > 0))
                    yield return $"{relative}: imports {include}" + (alias.Length > 0 ? $" as {alias}" : string.Empty);
                if (XunitNames.Contains(alias))
                    yield return $"{relative}: aliases {alias}";
            }
        }
    }

    // Each folder of a project file outside the test projects, the build output, the git
    // folder and the worktrees of an agent.
    private static IEnumerable<string> ProductProjects()
        => Directory.EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(file =>
            {
                var parts = RepositoryFiles.Relative(file).Split('/');
                return !parts.Any(part => part is "bin" or "obj" or ".git" or ".claude") && !TestProjects.Contains(parts[0]);
            })
            .Select(file => System.IO.Path.GetDirectoryName(file)!)
            .Distinct();

    // The name of each part of a dotted name, with each escape of the source resolved.
    private static string Dotted(SyntaxNode node) => node switch
    {
        QualifiedNameSyntax qualified => $"{Dotted(qualified.Left)}.{Dotted(qualified.Right)}",
        AliasQualifiedNameSyntax alias => Dotted(alias.Name),
        MemberAccessExpressionSyntax access => $"{Dotted(access.Expression)}.{Dotted(access.Name)}",
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => node.ToString(),
    };

    private static string Last(string dotted) => dotted[(dotted.LastIndexOf('.') + 1)..];

    private static string WithoutSuffix(string name)
        => name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;

    private static bool IsXunitSdk(string dotted)
        => dotted == "Xunit.Sdk" || dotted.StartsWith("Xunit.Sdk.", StringComparison.Ordinal);

    private static IEnumerable<string> ExtensionOffences(string relative, string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: relative).GetRoot();
        int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = WithoutSuffix(Last(Dotted(attribute.Name)));
            if (ExtensionAttributes.Contains(name))
                yield return $"{relative}:{Line(attribute)} applies the attribute {name}";
        }

        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(t => t.BaseList is not null))
        {
            foreach (var baseType in type.BaseList!.Types)
            {
                var name = Last(Dotted(baseType.Type));
                if (ExtensionTypes.Contains(name) || ExtensionAttributes.Contains(WithoutSuffix(name)))
                    yield return $"{relative}:{Line(baseType)} derives {type.Identifier.ValueText} from {name}";
            }
        }

        foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.NamespaceOrType is not { } target)
                continue;

            var dotted = Dotted(target);
            if (directive.Alias is null && IsXunitSdk(dotted))
                yield return $"{relative}:{Line(directive)} imports Xunit.Sdk";
            else if (directive.Alias is not null && (dotted == "Xunit" || IsXunitSdk(dotted)
                || ExtensionAttributes.Contains(WithoutSuffix(Last(dotted))) || ExtensionTypes.Contains(Last(dotted))))
                yield return $"{relative}:{Line(directive)} aliases {dotted}";
        }

        foreach (var alias in root.DescendantNodes().OfType<ExternAliasDirectiveSyntax>())
            yield return $"{relative}:{Line(alias)} declares the extern alias {alias.Identifier.ValueText}";

        // A dotted name outside a using directive, at its outermost node only.
        foreach (var name in root.DescendantNodes().Where(node =>
            node is QualifiedNameSyntax or AliasQualifiedNameSyntax or MemberAccessExpressionSyntax
            && node.Parent is not (QualifiedNameSyntax or AliasQualifiedNameSyntax or MemberAccessExpressionSyntax or UsingDirectiveSyntax)))
        {
            var dotted = Dotted(name);
            if (dotted.Split('.').Zip(dotted.Split('.').Skip(1)).Any(pair => pair is ("Xunit", "Sdk")))
                yield return $"{relative}:{Line(name)} names Xunit.Sdk";
        }
    }

    private static IEnumerable<string> ShadowOffences(string relative, string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: relative).GetRoot();
        int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        // A test in the namespace N.M finds a type of N.M, of N and of the global
        // namespace before a type of Xunit, and each type of a namespace under N.M too
        // when the test is in it.
        static bool SeenByATest(string space)
            => space.Length == 0 || TestNamespaces.Any(test =>
                test == space || test.StartsWith(space + ".", StringComparison.Ordinal) || space.StartsWith(test + ".", StringComparison.Ordinal));

        static string Namespace(SyntaxNode node)
            => string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(space => Dotted(space.Name)));

        foreach (var type in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            var name = type switch
            {
                BaseTypeDeclarationSyntax declared => declared.Identifier.ValueText,
                DelegateDeclarationSyntax declared => declared.Identifier.ValueText,
                _ => null,
            };
            if (name is not null && XunitNames.Contains(name) && SeenByATest(Namespace(type)))
                yield return $"{relative}:{Line(type)} declares the type {name} in a namespace that a test sees";
        }

        foreach (var space in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            var name = Dotted(space.Name);
            if (name.Split('.').Contains("Xunit"))
                yield return $"{relative}:{Line(space)} declares the namespace {name}";
        }
    }

    private static IEnumerable<string> Offences(string relative, string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: relative).GetRoot();
        int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t => XunitNames.Contains(t.Identifier.ValueText)))
            yield return $"{relative}:{Line(type)} declares the type {type.Identifier.ValueText}";

        foreach (var type in root.DescendantNodes().OfType<DelegateDeclarationSyntax>().Where(d => XunitNames.Contains(d.Identifier.ValueText)))
            yield return $"{relative}:{Line(type)} declares the type {type.Identifier.ValueText}";

        foreach (var alias in root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(u => u.Alias is not null && XunitNames.Contains(u.Alias.Name.Identifier.ValueText)))
            yield return $"{relative}:{Line(alias)} declares the alias {alias.Alias!.Name.Identifier.ValueText}";

        foreach (var space in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            var name = space.Name.ToString();
            if (name.Split('.').Any(part => part == "Xunit"))
                yield return $"{relative}:{Line(space)} declares the namespace {name}";
        }
    }
}
