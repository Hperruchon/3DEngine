using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;

// Finding T186 of the codebase review of 2026-11-21 (TASK-0431, Q7 A of the owner): the comparison of the builder of the
// bookcase (row K10a, TASK-0429) with the document file of the owner. The file is on the computer of the owner and not in the
// repository, so the owner runs this program there:
//
//   dotnet run --project eng/bookcase-compare [-- <file.3de>]
//
// With no argument the file is 3DProjects/Bibliotheque120/Bibliotheque120.3de in the documents folder of the person. The
// program replays the file and runs the builder, each in a new session with the native backend, and compares the name and the
// box of each live body. The exit code is 0 when each body agrees, 1 when a body differs, and 2 when the file is absent.
// Until TASK-0431 a test of Engine.Tests made this comparison, and it passed with no comparison where the file was absent.

var file = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "3DProjects", "Bibliotheque120", "Bibliotheque120" + DocumentFile.Extension);
if (!File.Exists(file))
{
    Console.Error.WriteLine($"The file {file} is absent, so the program compares nothing.");
    return 2;
}

SortedDictionary<string, Aabb> replayed;
using (var backend = new ManifoldGeometryBackend())
{
    var reader = new DocumentSession(EngineHosting.CreateDefault(backend));
    await DocumentFile.Read(reader, file);
    replayed = await Boxes(reader);
    reader.Close();
}

SortedDictionary<string, Aabb> built;
using (var backend = new ManifoldGeometryBackend())
{
    var session = new DocumentSession(EngineHosting.CreateDefault(backend));
    await BookcaseCase.Build(session);
    built = await Boxes(session);
    session.Close();
}

Console.WriteLine($"The replay of {file} gives {replayed.Count} bodies; the builder gives {built.Count}.");
var differences = 0;
foreach (var name in replayed.Keys.Union(built.Keys).Order(StringComparer.Ordinal))
{
    var inFile = replayed.TryGetValue(name, out var a) ? a.ToString() : "no body";
    var fromBuilder = built.TryGetValue(name, out var b) ? b.ToString() : "no body";
    if (!replayed.ContainsKey(name) || !built.ContainsKey(name) || a != b)
    {
        differences++;
        Console.WriteLine($"{name}: the file gives {inFile}, and the builder gives {fromBuilder}.");
    }
}

Console.WriteLine(differences == 0 ? "Each body agrees." : $"{differences} bodies differ.");
return differences == 0 ? 0 : 1;

// The name and the box of each live body, by name. The names of the bookcase are unique.
static async Task<SortedDictionary<string, Aabb>> Boxes(DocumentSession session)
{
    var bodies = await session.Read((document, _) => document.Bodies.Select(body => (body.Name ?? "", body.Handle.Id)).ToList());
    var boxes = new SortedDictionary<string, Aabb>(StringComparer.Ordinal);
    foreach (var (name, id) in bodies)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = id });
        if (result.Error is { } error)
            throw new InvalidOperationException($"{name}: {error.Code} {error.Message}");
        boxes.Add(name, result.Result);
    }

    return boxes;
}
