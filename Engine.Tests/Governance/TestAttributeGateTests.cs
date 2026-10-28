using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    private static IEnumerable<string> Sources(string directory)
    {
        Assert.True(Directory.Exists(directory), $"{directory} was not found.");
        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !RepositoryFiles.Relative(file).Split('/').Any(part => part is "bin" or "obj"));
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
