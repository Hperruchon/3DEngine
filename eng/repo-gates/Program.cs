using System.Reflection;
using Engine.Tests.Diagnostics;
using Xunit.Sdk;

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

// Each method with [Fact] or [Theory] in each type of the build, of each form,
// so that a form that this runner cannot run fails and is never left out. Until
// TASK-0066 the runner read the public instance methods of public classes only,
// and a static check, a check in a nested or an internal class, an inherited
// check and a row of [MemberData] did not run (finding T47 of the codebase review
// of 2026-10-12).
const BindingFlags Each = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
    | BindingFlags.Static | BindingFlags.DeclaredOnly;
var checks = typeof(DiagnosticsScanner).Assembly.GetTypes()
    .SelectMany(type => type.GetMethods(Each)
        .Where(method => method.GetCustomAttribute<FactAttribute>() is not null)
        .Select(method => (Type: type, Method: method)))
    .OrderBy(check => check.Type.FullName, StringComparer.Ordinal)
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

// An async lambda or local function that returns void runs on after its call, so a
// check that calls one ends before it and passes. The compiler makes each one a hidden
// method with AsyncStateMachineAttribute, so the runner fails each such method of the
// build. Until TASK-0078 the runner refused an async void check only, and a check that
// called an async lambda passed (finding T59 of the codebase review of 2026-10-17).
foreach (var (type, method) in typeof(DiagnosticsScanner).Assembly.GetTypes()
    .SelectMany(type => type.GetMethods(Each).Select(method => (Type: type, Method: method)))
    .Where(found => found.Method.ReturnType == typeof(void)
        && found.Method.GetCustomAttribute<FactAttribute>() is null
        && found.Method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() is not null)
    .OrderBy(found => $"{found.Type.FullName}.{found.Method.Name}", StringComparer.Ordinal))
{
    Console.WriteLine($"FAIL  {type.FullName}.{method.Name}: an async method or lambda that returns void, whose end no check awaits");
    failures++;
}

// A check that starts a task and does not wait for it ends before the task, so a failure
// in the task never fails the check: `_ = f();` with a function that returns a task, and
// `Task.Run(...)` as a statement. Reflection does not see a discard, so the runner reads
// the source of each gate file. Until TASK-0089 both forms passed (finding T67 of the
// codebase review of 2026-10-28).
foreach (var problem in DiscardedTasks.Problems(root))
{
    Console.WriteLine($"FAIL  {problem}");
    failures++;
}

foreach (var (type, method) in checks)
{
    var fact = method.GetCustomAttribute<FactAttribute>()!;
    var name = $"{type.Name}.{method.Name}";

    // A check that the runner cannot run fails, and a skip fails: the program is
    // the run that decides, so each check runs or the run is red.
    var refusal = fact.Skip is not null ? $"it is skipped ({fact.Skip})"
        : !type.IsPublic || type.IsAbstract || type.ContainsGenericParameters ? "its class is not a public, top-level, concrete class"
        : !method.IsPublic || method.IsStatic || method.ContainsGenericParameters ? "it is not a public instance method"
        : method.ReturnType != typeof(void) && method.ReturnType != typeof(Task) && method.ReturnType != typeof(ValueTask) ? "it returns neither void, Task nor ValueTask"
        : method.ReturnType == typeof(void) && method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() is not null ? "it is async void, so its end is not awaited (finding T52 of the review of 2026-10-13)"
        : fact is TheoryAttribute && method.GetCustomAttributes<DataAttribute>().Any(data => data is not InlineDataAttribute) ? "a row source other than [InlineData] gives its rows"
        : null;
    if (refusal is not null)
    {
        Console.WriteLine($"FAIL  {name}: the runner cannot run it, because {refusal}");
        failures++;
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
            else if (result is ValueTask valueTask)
                await valueTask;
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
