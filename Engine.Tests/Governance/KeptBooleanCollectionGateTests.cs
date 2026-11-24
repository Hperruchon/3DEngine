using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tests.Governance;

// TASK-0457 (phase P0.186). The projection keeps the result of each boolean step in one static store of the process, at most
// KernelProjection.BooleanLimit results, and the result of the oldest use goes first (ADR-0059 item 2, TASK-0454). xUnit runs
// the test classes in parallel. Run 45021528072 on main failed: two tests of KeptBooleanTests filled the store past its limit
// while A_Sphere_Cuts_A_Hole_Of_One_Name ran, the kept result of its cut left, and the query of the faces made the boolean
// again. The test counted two booleans and not one.
//
// The rule: each type of Engine.Tests whose own code names BooleansOnThisThread, BooleansKept, IsBooleanKept, BooleanLimit or
// ForksOpen declares a test and carries [Collection("kept booleans")], and Engine.Tests defines that collection with parallel
// runs off. xUnit then runs those classes after the parallel collections and alone. The gate reads the names in the code, not
// in a comment or in a text. Another test project runs in its own process, so it shares no store with Engine.Tests.
//
// TASK-0461 (finding T194 of the codebase review of 2026-11-23): the gate judges the innermost type that holds each name, not
// the outermost one. xUnit reads the attribute on the test class itself, so a nested test class in an outer class of the
// collection ran in parallel and passed the gate. A helper type with no test (static class Counts { ... }) let another class
// count with no name of the list, so a type that names a shared member must declare a test. The gate does not follow a call:
// a public member of a test class of the collection can still give a count to another class.
public class KeptBooleanCollectionGateTests
{
    private const string CollectionName = "kept booleans";

    // The members of the kept results of the projection and of the count of the booleans of the kernel.
    private static readonly string[] SharedNames = ["BooleansOnThisThread", "BooleansKept", "IsBooleanKept", "BooleanLimit", "ForksOpen"];

    [Fact]
    public void Each_Test_Class_That_Reads_The_Kept_Booleans_Is_In_The_Collection_Kept_Booleans()
    {
        var offences = Offences(Sources());

        Assert.True(
            offences.Count == 0,
            "A test that fills the kept booleans past their limit, or that counts the booleans or the kept results, depends on "
            + "the tests that run beside it (run 45021528072). Put [Collection(\"kept booleans\")] on the class "
            + "(Engine.Tests/Features/KeptBooleanCollection.cs). Offences:\n  " + string.Join("\n  ", offences));
    }

    [Fact]
    public void Engine_Tests_Defines_The_Collection_Kept_Booleans_With_Parallel_Runs_Off()
    {
        var definitions = Sources()
            .SelectMany(source => Parse(source.Path, source.Source).GetRoot().DescendantNodes().OfType<AttributeSyntax>()
                .Where(attribute => Name(attribute) == "CollectionDefinition" && FirstText(attribute) == CollectionName)
                .Select(attribute => (source.Path, Off: attribute.ArgumentList!.Arguments.Any(argument =>
                    argument.NameEquals?.Name.Identifier.Text == "DisableParallelization"
                    && argument.Expression.IsKind(SyntaxKind.TrueLiteralExpression)))))
            .ToList();

        Assert.True(definitions.Count == 1, $"Engine.Tests defines the collection \"{CollectionName}\" {definitions.Count} times, and not one time.");
        Assert.True(definitions[0].Off, $"{definitions[0].Path} defines the collection \"{CollectionName}\" with parallel runs on. Set DisableParallelization = true.");
    }

    // The forms that the scan must see, and the forms that it must not take as a use.
    [Theory]
    [InlineData("class A { void M() { var n = PlanarBoolean.BooleansOnThisThread; } }", "Probe.cs:1 class A")]
    [InlineData("[Collection(\"real host\")]\nclass A { int N => KernelProjection.BooleanLimit; }", "Probe.cs:2 class A")]
    [InlineData("class A { class B { bool K() => KernelProjection.IsBooleanKept(default, default); } }", "Probe.cs:1 class B")]
    [InlineData("using static Engine.Core.Features.KernelProjection;\nclass A { int N => BooleansKept; }", "Probe.cs:2 class A")]
    [InlineData("[Collection(\"kept booleans\")]\nclass A { [Fact] void T() { var n = KernelProjection.BooleanLimit; } }", "")]
    [InlineData("[Xunit.CollectionAttribute(\"kept booleans\")]\nclass A { [Theory] void T(int i) { var n = KernelProjection.BooleanLimit + i; } }", "")]
    [InlineData("[Collection(\"kept booleans\")]\nclass A { [NativeManifoldFact] void T() { var n = KernelProjection.ForksOpen; } }", "")]
    [InlineData("[Collection(\"kept booleans\")]\nclass A { public class B { [Fact] public void T() { var n = PlanarBoolean.BooleansOnThisThread; } } }", "Probe.cs:2 class B")]
    [InlineData("[Collection(\"kept booleans\")]\nstatic class Counts { public static long N => PlanarBoolean.BooleansOnThisThread; }", "Probe.cs:2 class Counts")]
    [InlineData("class A { [Collection(\"kept booleans\")] public class B { [Fact] public void T() { var n = KernelProjection.BooleansKept; } } }", "")]
    [InlineData("// BooleanLimit\nclass A { string S => \"BooleansKept\"; }", "")]
    public void The_Scan_Finds_Each_Class_That_Names_A_Shared_Member(string source, string expected)
    {
        var offences = Offences([("Probe.cs", source)]);

        if (expected.Length == 0)
            Assert.Empty(offences);
        else
            Assert.Contains(offences, offence => offence.StartsWith(expected, StringComparison.Ordinal));
    }

    // Each source file of Engine.Tests that the build compiles: MSBuild leaves out the folders obj/ and bin/ at the root of the
    // project only.
    private static List<(string Path, string Source)> Sources()
    {
        var directory = RepositoryFiles.Path("Engine.Tests");
        Assert.True(Directory.Exists(directory), "Engine.Tests was not found.");

        var sources = Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(RepositoryFiles.Relative)
            .Where(relative => !relative.StartsWith("Engine.Tests/obj/", StringComparison.Ordinal)
                && !relative.StartsWith("Engine.Tests/bin/", StringComparison.Ordinal))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .Select(relative => (relative, File.ReadAllText(RepositoryFiles.Path(relative))))
            .ToList();

        // An empty result is not evidence.
        Assert.True(sources.Count > 100, $"The scan found {sources.Count} source files in Engine.Tests.");
        return sources;
    }

    private static SyntaxTree Parse(string path, string source)
        => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), path: path);

    // Each innermost type whose code names a shared member and that declares no test or is not in the collection "kept booleans"
    // (TASK-0461).
    private static List<string> Offences(IEnumerable<(string Path, string Source)> sources)
    {
        var offences = new List<string>();
        foreach (var (path, source) in sources)
        {
            var tree = Parse(path, source);
            var types = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(name => SharedNames.Contains(name.Identifier.ValueText, StringComparer.Ordinal))
                .Select(name => name.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault())
                .Distinct()
                .ToList();

            foreach (var type in types)
            {
                if (type is null)
                {
                    offences.Add($"{path} names a shared member outside a class");
                    continue;
                }

                var inCollection = type.AttributeLists.SelectMany(list => list.Attributes)
                    .Any(attribute => Name(attribute) == "Collection" && FirstText(attribute) == CollectionName);
                var declaresTest = type.Members.OfType<MethodDeclarationSyntax>()
                    .Any(method => method.AttributeLists.SelectMany(list => list.Attributes).Any(attribute => Name(attribute).EndsWith("Fact", StringComparison.Ordinal) || Name(attribute).EndsWith("Theory", StringComparison.Ordinal)));
                var where = $"{path}:{tree.GetLineSpan(type.Identifier.Span).StartLinePosition.Line + 1} class {type.Identifier.Text}";
                if (!declaresTest)
                    offences.Add($"{where} names a shared member and declares no test, so another class can count through it; count in the test class");
                else if (!inCollection)
                    offences.Add($"{where} names a shared member and is not in the collection \"{CollectionName}\"");
            }
        }

        return offences;
    }

    // The name of an attribute with no namespace and no suffix Attribute.
    private static string Name(AttributeSyntax attribute)
    {
        var name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => attribute.Name.ToString(),
        };
        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
    }

    // The text of the first argument of an attribute when it is a string literal, else null.
    private static string? FirstText(AttributeSyntax attribute)
        => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
}
