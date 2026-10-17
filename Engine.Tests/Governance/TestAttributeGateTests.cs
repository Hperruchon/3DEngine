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
public class TestAttributeGateTests
{
    private static readonly string[] TestProjects = ["Engine.Tests", "3DEngine.Tests", "3DEngine.ImageTests"];

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
