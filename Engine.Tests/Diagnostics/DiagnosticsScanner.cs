using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tests.Diagnostics;

internal static class DiagnosticsScanner
{
    // <severity>-<subsystem>-<short-tag> per docs/diagnostics.md.
    // Tag is 2+ chars to keep the shape descriptive; severity must be E/W/I.
    public static readonly Regex CodePattern = new(
        @"\b[EWI]-[A-Z][A-Z0-9]*-[A-Z][A-Z0-9-]*[A-Z0-9]\b",
        RegexOptions.Compiled);

    private static readonly Regex BacktickedCodePattern = new(
        @"`(?<code>[EWI]-[A-Z][A-Z0-9]*-[A-Z][A-Z0-9-]*[A-Z0-9])`",
        RegexOptions.Compiled);

    // The test project holds sample codes in its own tests and is not a source
    // of diagnostics. Each other Engine.* project is. Until TASK-0039 the list
    // named three projects, and Engine.Api.Http and Engine.Geometry.Manifold,
    // which both raise codes, were not read (codebase review finding T2).
    private static readonly string[] ExcludedProjects = ["Engine.Tests"];

    // The program repo-gates sets the root from its argument --root, because it
    // runs outside the checkout (TASK-0064). In the test process it stays null.
    public static string? FixedRoot { get; set; }

    public static string FindRepoRoot(string startDirectory)
    {
        if (FixedRoot is not null)
            return FixedRoot;

        var current = new DirectoryInfo(startDirectory);
        for (var depth = 0; depth < 10 && current is not null; depth++)
        {
            if (File.Exists(Path.Combine(current.FullName, "3DEngine.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate 3DEngine.sln above '{startDirectory}'.");
    }

    public static IEnumerable<string> ExtractCodes(string text)
    {
        foreach (Match match in CodePattern.Matches(text))
            yield return match.Value;
    }

    // Finding T81 of the codebase review of 2026-10-29 (TASK-0107): the compiler joins
    // "E-SKETCH-" + "HIDDEN" into a code that the text does not hold, so the scanner also reads
    // the value of each constant expression.
    public static IEnumerable<(string Path, string Code)> ConstantCodes(IEnumerable<(string Path, string Text)> files)
        => Engine.Tests.Governance.ConstantStrings.Values(files)
            .SelectMany(constant => ExtractCodes(constant.Value).Select(code => (constant.Path, code)));

    // Finding T91 of the codebase review of 2026-10-30 (TASK-0114): a code built at run time, as
    // "E-SKETCH-" + a field, or string.Concat("W-SKETCH-", "JOINED"), passed, because only a
    // constant value is read. A constant string with the start of a code that is not a full code
    // is a part of a code, and each code is one constant.
    private static readonly Regex CodeStart = new(@"^[EWI]-[A-Z0-9-]*$", RegexOptions.Compiled);

    //
    // Finding T99 of the codebase review of 2026-10-30 (b) (TASK-0123): four more forms passed. A
    // part of a code is also:
    // - the middle of a code, as "-SKETCH-" in "E" + ("-SKETCH-" + Tag);
    // - the start of a code or a code in lower case, as "e-sketch-hidden".ToUpperInvariant();
    // - a text part of an interpolated string that is one of these, as $"E-SKETCH-{Tag}";
    // - a severity after the separator "-" of string.Join, as string.Join("-", "I", "SKETCH", Tag).
    // Another spelling of a call stays a rule in text: each code is one constant.
    // Finding T109 of the codebase review of 2026-10-31 (TASK-0132): the middle of a code with no dash
    // after it, as "-SKETCH" in $"{Sev}-SKETCH{"-"}{Tag}", a severity alone as a constant, as
    // string.Join("-", new[] { "E", "SKETCH", Tag }) or a constant Sev = "E", and a severity as a
    // character before a string, as 'E' + "-SKETCH", are parts of a code too.
    private static readonly Regex CodeMiddle = new(@"^-[A-Z][A-Z0-9]*(?:-[A-Z0-9-]*)?$", RegexOptions.Compiled);

    private static bool IsSeverity(string value) => value is "E" or "W" or "I";

    private static readonly Regex CodeAnyCase = new(@"^[EWIewi]-[A-Za-z0-9-]*$", RegexOptions.Compiled);

    private static bool IsPart(string value)
        => (CodeAnyCase.IsMatch(value) || CodeMiddle.IsMatch(value))
            && !(CodePattern.Match(value) is { Success: true } code && code.Value == value);

    public static IEnumerable<(string Path, int Line, string Value)> PartsOfCodes(IEnumerable<(string Path, string Text)> files)
    {
        var list = files.ToList();
        var constants = Engine.Tests.Governance.ConstantStrings.Constants(list).ToList();
        return constants
            .Where(constant => constant.Value is string value && (IsPart(value) || IsSeverity(value)))
            .Select(constant => (constant.Path, constant.Line, (string)constant.Value))
            .Concat(constants.Where(IsSeverityBeforeAdd).Select(constant => (constant.Path, constant.Line, constant.Value + "-")))
            .Concat(list.SelectMany(BuiltParts))
            .Distinct();
    }

    // Finding T116 of the codebase review of 2026-10-31 (b) (TASK-0143): the syntax read a char literal
    // on the left of an add only, and const char S = 'E'; S + "-x" + Tag gave no part. The value of the
    // left operand comes from the compiler now.
    private static bool IsSeverityBeforeAdd((string Path, int Line, object Value, ExpressionSyntax Expression) constant)
        => constant.Value is char severity && IsSeverity(severity.ToString())
            && constant.Expression.Parent is BinaryExpressionSyntax add && add.IsKind(SyntaxKind.AddExpression) && add.Left == constant.Expression;

    // The parts of codes that only the syntax shows: the text parts of an interpolated string, and
    // a severity that string.Join puts after "-".
    private static IEnumerable<(string Path, int Line, string Value)> BuiltParts((string Path, string Text) file)
    {
        var root = CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(LanguageVersion.Preview), path: file.Path).GetRoot();
        static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        foreach (var part in root.DescendantNodes().OfType<InterpolatedStringTextSyntax>())
        {
            if (IsPart(part.TextToken.ValueText))
                yield return (file.Path, LineOf(part), part.TextToken.ValueText);
        }

        foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Join" }
                && call.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax separator }, { Expression: LiteralExpressionSyntax severity }, ..]
                && separator.Token.ValueText == "-" && severity.Token.ValueText is "E" or "W" or "I")
                yield return (file.Path, LineOf(call), severity.Token.ValueText + "-");
        }
    }

    public static HashSet<string> ParseRegistry(string registryMarkdown)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in BacktickedCodePattern.Matches(registryMarkdown))
            codes.Add(match.Groups["code"].Value);
        return codes;
    }

    public static IEnumerable<string> EngineSourceProjects(string repoRoot)
        => Directory.EnumerateDirectories(repoRoot, "Engine.*")
            .Select(Path.GetFileName)
            .Where(name => name is not null && !ExcludedProjects.Contains(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal);

    public static IEnumerable<string> EnumerateEngineSources(string repoRoot)
    {
        foreach (var project in EngineSourceProjects(repoRoot))
        {
            var projectDir = Path.Combine(repoRoot, project);

            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (HasExcludedSegment(repoRoot, file))
                    continue;
                yield return file;
            }
        }
    }

    // Finding T81 of the codebase review of 2026-10-29 (TASK-0107): MSBuild leaves out bin/ and
    // obj/ at the root of a project only, and the scanner left them out at each depth, so a code
    // in Engine.Core/Sketch/bin/ compiled and the registry gate did not name it. The scanner now
    // uses the rule of the dependency gate (finding T68).
    private static bool HasExcludedSegment(string repoRoot, string filePath)
        => Engine.Tests.Governance.DependencyDirectionGateTests.IsOutOfScope(
            Path.GetRelativePath(repoRoot, filePath).Replace('\\', '/'),
            directory => Directory.EnumerateFiles(Path.Combine(repoRoot, directory), "*.csproj").Any());
}
