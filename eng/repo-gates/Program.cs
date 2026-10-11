using System.Reflection;
using Engine.Tests.Diagnostics;

// The program repo-gates (TASK-0064). It runs each check of the gates that read
// only files of the repository: each public method with [Fact], and each row of
// [InlineData] of a method with [Theory], in the gate classes that the project
// file compiles. The gates read the checkout that --root gives.
//
// Until TASK-0064 the gates ran only inside the test process of the judged
// commit. A file of that commit, for example a module initializer, could then
// change what a gate read: two forms moved AppContext.BaseDirectory (finding T31
// of the codebase review of 2026-10-11). Here only the code of the build runs,
// and the pipeline builds it from main.
//
// Exit codes: 0 when each check passes, 1 when a check fails, 2 for a wrong
// argument or when no check is found.

if (args is not ["--root", var rootArgument])
{
    Console.Error.WriteLine("Usage: repo-gates --root <path of the checkout>");
    return 2;
}

var root = Path.GetFullPath(rootArgument);
if (!File.Exists(Path.Combine(root, "3DEngine.sln")))
{
    Console.Error.WriteLine($"'{root}' holds no 3DEngine.sln, so it is not a checkout of the repository.");
    return 2;
}

DiagnosticsScanner.FixedRoot = root;

var checks = typeof(DiagnosticsScanner).Assembly.GetTypes()
    .Where(type => type.IsPublic && type.IsClass && !type.IsAbstract)
    .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(method => method.GetCustomAttribute<FactAttribute>() is not null)
        .Select(method => (Type: type, Method: method)))
    .OrderBy(check => check.Type.Name, StringComparer.Ordinal)
    .ThenBy(check => check.Method.Name, StringComparer.Ordinal)
    .ToList();

// An empty result is not evidence: a build with no gate passes nothing.
if (checks.Count == 0)
{
    Console.Error.WriteLine("The build holds no check.");
    return 2;
}

var failures = 0;
var runs = 0;

foreach (var (type, method) in checks)
{
    var fact = method.GetCustomAttribute<FactAttribute>()!;
    var name = $"{type.Name}.{method.Name}";

    if (fact.Skip is not null)
    {
        Console.WriteLine($"skip  {name}: {fact.Skip}");
        continue;
    }

    // A theory with no row of [InlineData] fails, so that a row source that
    // this runner does not read never passes with no run.
    var rows = fact is TheoryAttribute
        ? method.GetCustomAttributes<InlineDataAttribute>().SelectMany(data => data.GetData(method)).ToList()
        : [Array.Empty<object?>()];
    if (rows.Count == 0)
    {
        Console.WriteLine($"FAIL  {name}: the theory has no row of [InlineData]");
        failures++;
        continue;
    }

    foreach (var row in rows)
    {
        var label = row.Length == 0 ? name : $"{name}({string.Join(", ", row)})";
        runs++;
        try
        {
            var result = method.Invoke(Activator.CreateInstance(type), row);
            if (result is Task task)
                await task;
            Console.WriteLine($"pass  {label}");
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            Console.WriteLine($"FAIL  {label}");
            Console.WriteLine("      " + cause.Message.Replace("\n", "\n      ", StringComparison.Ordinal));
            failures++;
        }
    }
}

Console.WriteLine($"{runs} runs of {checks.Count} checks, {failures} failed, on {root}");
return failures == 0 ? 0 : 1;
