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
            // Finding T120 of the codebase review of 2026-10-31 (c) (TASK-0150): a const char E, W or I in
            // an interpolation or a call gave no part. Finding T126 of the codebase review of 2026-11-01
            // (TASK-0161): each constant char of a severity was a part, so c == 'E' in a parser of numbers
            // was a false part. A constant char of a severity is a part where it joins text only.
            .Concat(constants.Where(constant => constant.Value is char severity && IsSeverity(severity.ToString()) && JoinsText(constant.Expression))
                .Select(constant => (constant.Path, constant.Line, constant.Value.ToString()!)))
            .Concat(list.SelectMany(BuiltParts))
            .Distinct();
    }

    // Finding T116 of the codebase review of 2026-10-31 (b) (TASK-0143): the syntax read a char literal
    // on the left of an add only, and const char S = 'E'; S + "-x" + Tag gave no part. The value of the
    // left operand comes from the compiler now.
    private static bool IsSeverityBeforeAdd((string Path, int Line, object Value, ExpressionSyntax Expression) constant)
        => constant.Value is char severity && IsSeverity(severity.ToString())
            && constant.Expression.Parent is BinaryExpressionSyntax add && add.IsKind(SyntaxKind.AddExpression) && add.Left == constant.Expression;

    // T126: an expression joins text when it is an operand of an add, the expression of an interpolation, or an
    // argument of a call that joins text, through parentheses and casts. Another spelling of a call stays a rule in
    // text: each code is one constant.
    // Finding T128 of the codebase review of 2026-11-03 (TASK-0170): the fix of T126 lost three forms that T120 caught:
    // S.ToString(), an element of an array, and an argument of new string(...). The walk goes through each one too.
    // Finding T134 of the codebase review of 2026-11-05 (TASK-0182): a string call after ToString, a StringBuilder of the
    // node, a pad, a repeat and string.Create hid a severity. A call that inserts into the text of the node, or appends
    // to it, joins text; a call that changes the case or the spaces keeps the text, as ToString does.
    private static bool JoinsText(ExpressionSyntax expression)
    {
        SyntaxNode node = expression;
        while (true)
        {
            if (node.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } member && member.Expression == node
                && JoinCalls.Contains(member.Name.Identifier.ValueText))
                return true;
            if (Outer(node) is not { } outer)
                break;
            node = outer;
        }

        return node.Parent switch
        {
            BinaryExpressionSyntax add => add.IsKind(SyntaxKind.AddExpression),
            InterpolationSyntax => true,
            ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } } => JoinName(call.Expression),
            _ => false,
        };
    }

    // The expression that holds the node and keeps its text: a parenthesis, a cast, a call of ToString, an array of
    // the node, or a new string of the node (T128). Null for each other parent.
    // Finding T131 of the second codebase review of 2026-11-03 (TASK-0177): a ToString with an argument, a static
    // ToString of the node (char.ToString(S), Convert.ToString(S)) and a collection expression gave no part. The walk
    // goes through each one, and an insert into a StringBuilder joins text (JoinName).
    private static SyntaxNode? Outer(SyntaxNode node) => node.Parent switch
    {
        ParenthesizedExpressionSyntax or CastExpressionSyntax => node.Parent,
        MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax call } member
            when member.Expression == node && TextCalls.Contains(member.Name.Identifier.ValueText) => call,
        ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ToString" or "Repeat" } } call } } => call,
        InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax } initializer => initializer.Parent,
        ExpressionElementSyntax { Parent: CollectionExpressionSyntax collection } => collection,
        ArgumentSyntax { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax creation } } when IsString(creation.Type) => creation,
        _ => null,
    };

    // T134: the calls that keep the text of the node, and the calls that join text to it.
    private static readonly HashSet<string> TextCalls =
        ["ToString", "ToUpperInvariant", "ToLowerInvariant", "ToUpper", "ToLower", "Trim", "TrimStart", "TrimEnd", "Normalize"];

    private static readonly HashSet<string> JoinCalls = ["Insert", "Append", "AppendLine", "Replace", "PadLeft", "PadRight"];

    // A string or, since T134, a StringBuilder, whose constructor takes the text of the node.
    private static bool IsString(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
        IdentifierNameSyntax name => name.Identifier.ValueText is "String" or "StringBuilder",
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText is "String" or "StringBuilder",
        _ => false,
    };

    private static bool JoinName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Create" } member => IsStringName(member.Expression),
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText is "Join" or "Concat" or "Format" or "Append" or "Insert" or "PadLeft" or "PadRight",
        IdentifierNameSyntax name => name.Identifier.ValueText is "Join" or "Concat" or "Format" or "Append" or "Insert",
        _ => false,
    };

    // T134: string.Create and String.Create build a string from the state that they take.
    private static bool IsStringName(ExpressionSyntax expression) => expression switch
    {
        PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
        IdentifierNameSyntax name => name.Identifier.ValueText == "String",
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText == "String",
        _ => false,
    };

    // T126: the char of a severity that the compiler does not fold, where it joins text: char.ToUpperInvariant('e')
    // or char.ToUpper('e'), and a constant index into a string literal, as "EWI"[0].
    private static string? BuiltSeverity(ExpressionSyntax expression)
    {
        var value = expression switch
        {
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ToUpperInvariant" or "ToUpper" }, ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax literal }] }
                when literal.IsKind(SyntaxKind.CharacterLiteralExpression) => char.ToUpperInvariant((char)literal.Token.Value!).ToString(),
            ElementAccessExpressionSyntax { Expression: LiteralExpressionSyntax text, ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax index }] }
                when text.IsKind(SyntaxKind.StringLiteralExpression) && index.Token.Value is int at && at >= 0 && at < text.Token.ValueText.Length => text.Token.ValueText[at].ToString(),
            _ => null,
        };
        return value is not null && IsSeverity(value) && JoinsText(expression) ? value : null;
    }

    // The parts of codes that only the syntax shows: the text parts of an interpolated string, a
    // severity that string.Join puts after "-", and the char of a severity that the compiler does not fold (T126).
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

        foreach (var expression in root.DescendantNodes().OfType<ExpressionSyntax>())
        {
            if (BuiltSeverity(expression) is { } severity)
                yield return (file.Path, LineOf(expression), severity);
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
