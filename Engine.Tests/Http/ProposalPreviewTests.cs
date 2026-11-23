using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Preview;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0185 (phase P15a, TASK-0456): the preview of a proposal. The host copies the log of the Document under the serial lock,
// replays the copy in a second session with its own backend off the lock, and applies the commands of the proposal there
// (Q2 A of the third codebase review of 2026-11-21). It computes the preview when the proposal arrives and again at each new
// version (Q3 A), and compares the bodies by content (Q5 A). ADR-0185 names this file in its enforced-by field.
public sealed class ProposalPreviewTests
{
    // A new managed backend, and a new native backend. The bookcase needs Translate, which only the native backend has.
    private static Func<IGeometryBackend> Managed => () => new InProcessMeshBackend();

    private static Func<IGeometryBackend> Native => () => new ManifoldGeometryBackend();

    private sealed class Served : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly DocumentFolder _folder;

        private Served(EngineHost host, WebApplication app, DocumentFolder folder, HttpClient http)
        {
            Host = host;
            _app = app;
            _folder = folder;
            Http = http;
        }

        public EngineHost Host { get; }

        public HttpClient Http { get; }

        // A host on the given backend, the managed backend by default, with the given forks.
        public static async Task<Served> Start(Func<IGeometryBackend>? forks, Func<IGeometryBackend>? backend = null)
        {
            var host = new EngineHost((backend ?? Managed)(), forks: forks);
            var folder = new DocumentFolder(Path.Combine(Path.GetTempPath(), "3de-preview-" + Guid.NewGuid().ToString("N")));
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSingleton(folder);
            builder.AddEngineSurface(host);
            var app = builder.Build();
            app.MapEngineSurface();
            await app.StartAsync();
            return new Served(host, app, folder, new HttpClient { BaseAddress = new Uri(app.Urls.First()) });
        }

        public async ValueTask DisposeAsync()
        {
            Http.Dispose();
            await _app.DisposeAsync();
            Host.Dispose();
            _ = _folder;
        }
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Translate(Guid commandId, Guid bodyId, double dx, double dy, double dz)
        => $"{{\"name\":\"Translate\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"bodyId\":\"{bodyId}\",\"dx\":{Number(dx)},\"dy\":{Number(dy)},\"dz\":{Number(dz)}}}}}";

    private static string Rename(Guid commandId, Guid bodyId, string name)
        => $"{{\"name\":\"RenameBody\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"bodyId\":\"{bodyId}\",\"name\":{JsonSerializer.Serialize(name)}}}}}";

    private static string Box(Guid commandId)
        => $"{{\"name\":\"CreateBox\",\"schemaVersion\":1,\"commandId\":\"{commandId}\",\"parameters\":{{\"sizeX\":10,\"sizeY\":10,\"sizeZ\":10}}}}";

    private static async Task<Guid> Post(Served served, string summary, params string[] envelopes)
    {
        var json = $"{{\"summary\":{JsonSerializer.Serialize(summary)},\"commands\":[{string.Join(",", envelopes)}]}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await served.Http.PostAsync("/proposals", content);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"The post gave {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    // The limit of 30 s only ends a test that would wait for ever, for example when no fork computes the preview; a pass does not
    // wait for it.
    private static async Task<ProposalPreview> Preview(Served served, Guid id)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ProposalPreview? preview;
        try
        {
            preview = await served.Host.Proposals.Preview(id, limit.Token);
        }
        catch (OperationCanceledException) when (limit.IsCancellationRequested)
        {
            Assert.Fail($"No preview of the proposal {id} came in 30 s.");
            throw;
        }

        Assert.NotNull(preview);
        return preview;
    }

    // The state of the first Document: its version, the entries of its log, its bodies and the count of its events.
    private sealed record State(long Version, LogEntry[] Log, BodyRecord[] Bodies, int Events);

    private static Task<State> Read(EngineHost host)
        => host.Session.Read((document, events) => new State(document.Version, [.. document.Log], [.. document.Bodies.OrderBy(body => body.Handle.Id)], events.Count));

    private static void Unchanged(State before, State after)
    {
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Log.Length, after.Log.Length);
        for (var i = 0; i < before.Log.Length; i++)
            Assert.Same(before.Log[i], after.Log[i]);
        Assert.Equal(before.Bodies, after.Bodies);
        Assert.Equal(before.Events, after.Events);
    }

    // Items 2 and 4: a proposal of three commands on the bookcase of K10a gives the outcome of each command and the bodies that
    // it adds, removes and changes. The first Document keeps its version, its log, its bodies and its events.
    [NativeManifoldFact]
    public async Task Three_Commands_On_The_Bookcase_Give_Three_Outcomes_And_The_Body_Differences_And_Change_Nothing()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var before = await Read(served.Host);
        Assert.Equal(BookcaseCase.CommandCount, before.Version);

        var module = bodies["Module 1 - volume indicatif"];
        var bas = bodies["Bas"];
        var lifted = Guid.NewGuid();
        var id = await Post(
            served,
            "Lift module 1 and rename Bas",
            Translate(lifted, module, 0, 0, 10),
            Rename(Guid.NewGuid(), lifted, "Module 1 - lifted"),
            Rename(Guid.NewGuid(), bas, "Bas 2"));

        var preview = await Preview(served, id);

        Assert.Equal(id, preview.ProposalId);
        Assert.Equal(PreviewStates.Ready, preview.State);
        Assert.Null(preview.Error);
        Assert.Equal(before.Version, preview.DocumentVersion);
        Assert.Equal(["Translate", "RenameBody", "RenameBody"], preview.Results.Select(result => result.CommandName));
        Assert.All(preview.Results, result => Assert.Equal(CommandStatus.Applied, result.Status));
        Assert.Null(preview.FailedIndex);
        Assert.Null(preview.FailedCode);

        var differences = preview.Bodies!;
        var added = Assert.Single(differences.Added);
        Assert.Equal(lifted, added.Handle.Id);
        Assert.Equal("Module 1 - lifted", added.Name);
        Assert.Equal(module, Assert.Single(differences.Removed).Handle.Id);
        var changed = Assert.Single(differences.Changed);
        Assert.Equal(bas, changed.Before.Handle.Id);
        Assert.Equal(bas, changed.After.Handle.Id);
        Assert.Equal("Bas", changed.Before.Name);
        Assert.Equal("Bas 2", changed.After.Name);

        Unchanged(before, await Read(served.Host));
        var proposal = served.Host.Proposals.Find(id)!;
        Assert.Equal(ProposalStates.Open, proposal.State);
        Assert.Empty(proposal.Results);

        // P15b item 9: the measures of the added module and of the renamed shelf. The rename keeps the solid of Bas, so its
        // measures before are its measures after. No sketch changes (item 8).
        Assert.Empty(preview.Sketches!);
        Assert.Equal(new[] { lifted, bas }.Order(), preview.Measures!.Select(measure => measure.BodyId));
        var liftedMeasure = preview.Measures!.Single(measure => measure.BodyId == lifted);
        Assert.Null(liftedMeasure.Before);
        Assert.Null(liftedMeasure.After.Error);
        Assert.Equal(800.0 * 400 * 1000, liftedMeasure.After.Mass!.Volume);
        Assert.Equal(new DatumPoint(-1300, 0, 510), liftedMeasure.After.Mass.Centre);
        var basMeasure = preview.Measures!.Single(measure => measure.BodyId == bas);
        Assert.Equal(1164.0 * 394 * 18, basMeasure.After.Mass!.Volume);
        Assert.Equal(basMeasure.After, basMeasure.Before);

        // P15b item 10: module 1 touched the modules 7, 8, 9 and 12, and the Translate removes it. The lifted module overlaps
        // module 7 by 10 mm and touches the modules 8, 9 and 12. Bas keeps its solid, so it is in no pair.
        string[] neighbours = ["Module 7 - volume indicatif", "Module 8 - volume indicatif", "Module 9 - volume indicatif", "Module 12 - volume indicatif"];
        Assert.Equal(8, preview.Checks!.Count);
        Assert.All(preview.Checks, check => Assert.True(check.BodyIdA.CompareTo(check.BodyIdB) < 0));
        Assert.DoesNotContain(preview.Checks, check => check.BodyIdA == bas || check.BodyIdB == bas);
        foreach (var name in neighbours)
        {
            var old = Pair(preview, module, bodies[name]);
            Assert.Equal(PairStates.Touching, old.Before!.State);
            Assert.Null(old.After);
            var now = Pair(preview, lifted, bodies[name]);
            Assert.Null(now.Before);
            Assert.Equal(name == neighbours[0] ? PairStates.Overlapping : PairStates.Touching, now.After!.State);
        }

        var overlap = Pair(preview, lifted, bodies[neighbours[0]]).After!.Check!;
        Assert.Equal(800.0 * 400 * 10, overlap.Volume);
        Assert.True(overlap.Fault);
    }

    // The pair of the two bodies in the checks of the preview, in either order.
    private static PairChange Pair(ProposalPreview preview, Guid x, Guid y)
        => Assert.Single(preview.Checks!, check => (check.BodyIdA == x && check.BodyIdB == y) || (check.BodyIdA == y && check.BodyIdB == x));

    private static string Envelope(string name, string parameters)
        => $"{{\"name\":\"{name}\",\"schemaVersion\":1,\"commandId\":\"{Guid.NewGuid()}\",\"parameters\":{parameters}}}";

    private static Guid Out(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"{result.CommandName} gave no output {name}.");

    // A rectangle of 10 by 6 mm on the plane XY with a corner at (0, 0): the sketch, its four points and its four lines.
    private static async Task<(Guid Sketch, Guid[] Points, Guid[] Lines)> Rectangle(DocumentSession session)
    {
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var sketch = Out(await BookcaseCase.Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Out(await BookcaseCase.Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[points.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Out(await BookcaseCase.Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        return (sketch, points, lines);
    }

    // P15b items 9 and 10: a new distance of an extrude changes its body in place. The preview gives the measures of the body
    // before and after the command, to the double, and the one pair whose class changes: the extrude now overlaps the box above
    // it. The far box stays apart, so its pair is not a difference, and the sketch of the extrude does not change.
    [NativeManifoldFact]
    public async Task A_Changed_Body_Gives_Its_Measures_Before_And_After_And_The_Pair_Whose_Class_Changes()
    {
        await using var served = await Served.Start(Native, Native);
        var session = served.Host.Session;
        var (sketch, _, _) = await Rectangle(session);
        var extrude = Out(await BookcaseCase.Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }), "bodyId");
        var above = BookcaseCase.Output(await BookcaseCase.Applied(session, new TranslateCommand
        {
            BodyId = BookcaseCase.Output(await BookcaseCase.Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 })),
            Dx = 5, Dy = 3, Dz = 12,
        }));
        await BookcaseCase.Applied(session, new TranslateCommand
        {
            BodyId = BookcaseCase.Output(await BookcaseCase.Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 })),
            Dx = 100, Dy = 0, Dz = 0,
        });
        var before = await Read(served.Host);

        var id = await Post(served, "Make the extrude 8 mm high", Envelope("SetExtrudeDistance", $"{{\"extrudeId\":\"{extrude}\",\"distance\":8}}"));
        var preview = await Preview(served, id);

        Assert.Equal(PreviewStates.Ready, preview.State);
        Assert.Equal(extrude, Assert.Single(preview.Bodies!.Changed).After.Handle.Id);
        Assert.Empty(preview.Sketches!);

        var measure = Assert.Single(preview.Measures!);
        Assert.Equal(extrude, measure.BodyId);
        Assert.Equal(new MassProperties(300, new DatumPoint(5, 3, 2.5), false), measure.Before!.Mass);
        Assert.Equal(new MassProperties(480, new DatumPoint(5, 3, 4), false), measure.After.Mass);

        var pair = Assert.Single(preview.Checks!);
        Assert.Equal(new[] { extrude, above }.Order(), new[] { pair.BodyIdA, pair.BodyIdB });
        Assert.Equal(PairStates.Apart, pair.Before!.State);
        Assert.Equal(2, pair.Before.Check!.Gap);
        Assert.Equal(PairStates.Overlapping, pair.After!.State);
        Assert.Equal(60, pair.After.Check!.Volume);
        Unchanged(before, await Read(served.Host));
    }

    // P15b item 10 (Q4 A): a pair above the bound of work of CheckInterference (ADR-0182 item 8) has the state limit, with
    // E-CHECK-WORK-LIMIT and no answer of a check. A body that the proposal adds counts as apart before it, so the pair is a
    // difference.
    [NativeManifoldFact]
    public async Task A_Pair_Above_The_Bound_Of_Work_Has_The_State_Limit()
    {
        await using var served = await Served.Start(Native, Native);
        var first = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateSphereCommand { Radius = 10 }));

        var made = Guid.NewGuid();
        var id = await Post(served, "Make a second sphere", $"{{\"name\":\"CreateSphere\",\"schemaVersion\":1,\"commandId\":\"{made}\",\"parameters\":{{\"radius\":10}}}}");
        var preview = await Preview(served, id);

        var pair = Assert.Single(preview.Checks!);
        Assert.Equal(new[] { first, made }.Order(), new[] { pair.BodyIdA, pair.BodyIdB });
        Assert.Null(pair.Before);
        Assert.Equal(PairStates.Limit, pair.After!.State);
        Assert.Equal(DiagnosticCodes.CheckWorkLimit, pair.After.Error?.Code);
        Assert.Null(pair.After.Check);
        var measure = Assert.Single(preview.Measures!);
        Assert.Equal(made, measure.BodyId);
        Assert.True(measure.After.Mass!.Volume > 0);
    }

    // P15b item 8: the state of the solver of each sketch that the proposal changes, before and after its commands. A second
    // distance on the same line conflicts, so the sketch goes from under to over, with the residual of the conflict. Four fixed
    // points at their places give no free degree, so the sketch goes from under to solved.
    [Fact]
    public async Task A_Sketch_That_The_Proposal_Changes_Gives_Its_State_Before_And_After()
    {
        await using var served = await Served.Start(Managed);
        var (sketch, points, lines) = await Rectangle(served.Host.Session);
        await BookcaseCase.Applied(served.Host.Session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 });

        var conflict = await Post(served, "Make the first line 7 mm", Envelope("AddSketchConstraint", $"{{\"sketchId\":\"{sketch}\",\"kind\":\"distance\",\"first\":\"{lines[0]}\",\"value\":7}}"));
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var fix = await Post(
            served,
            "Fix the four corners",
            [.. points.Select((point, i) => Envelope("AddSketchConstraint", $"{{\"sketchId\":\"{sketch}\",\"kind\":\"fixed\",\"first\":\"{point}\",\"value\":{Number(corners[i].X)},\"secondValue\":{Number(corners[i].Y)}}}"))]);

        var over = Assert.Single((await Preview(served, conflict)).Sketches!);
        Assert.Equal(sketch, over.SketchId);
        Assert.Equal(SketchStates.Under, over.Before!.State);
        Assert.Equal(7, over.Before.Freedom);
        Assert.Equal(0, over.Before.Residual);
        Assert.Equal(SketchStates.Over, over.After.State);
        Assert.Equal(3, over.After.Residual);

        var solved = Assert.Single((await Preview(served, fix)).Sketches!);
        Assert.Equal(SketchStates.Under, solved.Before!.State);
        Assert.Equal(SketchStates.Solved, solved.After.State);
        Assert.Equal(0, solved.After.Freedom);
        Assert.Equal(0, solved.After.Residual);
    }

    // P15b items 8 to 10: a proposal that changes no record gives three empty differences, and the fork needs no base. A rename
    // changes the record of the body and keeps its solid: the measures before are the measures after, no pair is checked, and the
    // fork needs no base either. Each preview takes one backend of the factory.
    [Fact]
    public async Task A_Proposal_That_Changes_Nothing_Gives_Empty_Differences_And_No_Base()
    {
        var backends = 0;
        await using var served = await Served.Start(() =>
        {
            Interlocked.Increment(ref backends);
            return new InProcessMeshBackend();
        });
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));

        var nothing = await Preview(served, await Post(served, "Nothing", Envelope("NoOp", "{\"echo\":\"x\"}")));
        Assert.Equal(CommandStatus.Applied, Assert.Single(nothing.Results).Status);
        Assert.Empty(nothing.Bodies!.Added);
        Assert.Empty(nothing.Bodies.Removed);
        Assert.Empty(nothing.Bodies.Changed);
        Assert.Empty(nothing.Sketches!);
        Assert.Empty(nothing.Measures!);
        Assert.Empty(nothing.Checks!);
        Assert.Equal(1, Volatile.Read(ref backends));

        var renamed = await Preview(served, await Post(served, "Rename the box", Rename(Guid.NewGuid(), box, "Box")));
        var measure = Assert.Single(renamed.Measures!);
        Assert.Equal(box, measure.BodyId);
        Assert.Equal(1000, measure.After.Mass!.Volume);
        Assert.Equal(measure.After, measure.Before);
        Assert.Empty(renamed.Checks!);
        Assert.Equal(2, Volatile.Read(ref backends));
    }

    // P15b item 8 (Q5 A): the preview compares two sketch records by content, member by member, because the equality of the
    // record compares each list by reference (SketchContent). A new member of a sketch record or of one of its items fails this
    // test, so the comparison changes with it.
    [Fact]
    public void The_Comparison_Of_Two_Sketches_Reads_Each_Member_Of_A_Sketch_Record()
    {
        var read = new Dictionary<Type, string[]>
        {
            [typeof(SketchRecord)] = ["Id", "DatumId", "Name", "Points", "Lines", "Circles", "Arcs", "Constraints", "References"],
            [typeof(SketchPoint)] = ["Id", "X", "Y"],
            [typeof(SketchLine)] = ["Id", "Start", "End"],
            [typeof(SketchCircle)] = ["Id", "Center", "Radius"],
            [typeof(SketchArc)] = ["Id", "Center", "Start", "End"],
            [typeof(SketchConstraint)] = ["Id", "Kind", "Items", "Values"],
            [typeof(SketchReference)] = ["Id", "Target", "Points", "Line", "Fault"],
            [typeof(DatumReference)] = ["FeatureId", "Element", "ItemId"],
            [typeof(DatumFault)] = ["Code", "Message"],
        };
        foreach (var (type, names) in read)
            Assert.True(
                names.Order().SequenceEqual(type.GetProperties().Select(property => property.Name).Order()),
                $"{type.Name} has the members {string.Join(", ", type.GetProperties().Select(property => property.Name))}; SketchContent reads {string.Join(", ", names)} (ADR-0185 item 8).");
    }

    // Item 2 and ADR-0092 item 6: a refused command gives its code, and the commands after it do not run, as in an accept. The
    // accept of the same proposal then gives the same outcomes.
    [NativeManifoldFact]
    public async Task A_Refused_Command_Gives_Its_Code_And_Stops_The_Rest_As_The_Accept_Does()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var before = await Read(served.Host);

        var id = await Post(
            served,
            "Rename two shelves",
            Rename(Guid.NewGuid(), bodies["Bas"], "Bas 2"),
            Translate(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 10),
            Rename(Guid.NewGuid(), bodies["Haut"], "Haut 2"));

        var preview = await Preview(served, id);

        Assert.Equal(PreviewStates.Ready, preview.State);
        Assert.Equal(2, preview.Results.Count);
        Assert.Equal(CommandStatus.Applied, preview.Results[0].Status);
        Assert.Equal(CommandStatus.Rejected, preview.Results[1].Status);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, preview.Results[1].Error?.Code);
        Assert.Equal(1, preview.FailedIndex);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, preview.FailedCode);
        Assert.Equal("Bas 2", Assert.Single(preview.Bodies!.Changed).After.Name);
        Assert.Empty(preview.Bodies.Added);
        Assert.Empty(preview.Bodies.Removed);
        Unchanged(before, await Read(served.Host));

        var accepted = await served.Host.Proposals.Accept(id);
        Assert.Equal(ProposalStates.Failed, accepted!.State);
        Assert.Equal(preview.FailedIndex, accepted.FailedIndex);
        Assert.Equal(preview.FailedCode, accepted.FailedCode);
        Assert.Equal(preview.Results.Select(result => (result.CommandName, result.Status)), accepted.Results.Select(result => (result.CommandName, result.Status)));
    }

    // Finding E198 and ADR-0092 item 6: a command whose identifier the log holds does not apply again. The preview stops there
    // with E-API-PROPOSAL-COMMAND-KNOWN and no result for that command, as the accept does.
    [NativeManifoldFact]
    public async Task A_Known_Command_Identifier_Stops_The_Preview_As_It_Stops_The_Accept()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var known = await served.Host.Session.Read((document, _) => document.Log[0].Command.CommandId);

        var id = await Post(served, "Rename Bas and make a known box", Rename(Guid.NewGuid(), bodies["Bas"], "Bas 2"), Box(known));
        var preview = await Preview(served, id);

        Assert.Equal(CommandStatus.Applied, Assert.Single(preview.Results).Status);
        Assert.Equal(1, preview.FailedIndex);
        Assert.Equal(DiagnosticCodes.ApiProposalCommandKnown, preview.FailedCode);

        var accepted = await served.Host.Proposals.Accept(id);
        Assert.Equal(preview.FailedIndex, accepted!.FailedIndex);
        Assert.Equal(preview.FailedCode, accepted.FailedCode);
    }

    // Item 3 (Q3 A): a new version of the Document computes the preview again on the new log. Here the person moves the shelf
    // that the proposal renames, so the new preview refuses the rename.
    [NativeManifoldFact]
    public async Task A_New_Version_Of_The_Document_Computes_The_Preview_Again()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var bas = bodies["Bas"];
        var id = await Post(served, "Rename Bas", Rename(Guid.NewGuid(), bas, "Bas 2"));

        var first = await Preview(served, id);
        Assert.Equal(BookcaseCase.CommandCount, first.DocumentVersion);
        Assert.Null(first.FailedIndex);
        Assert.Equal("Bas 2", Assert.Single(first.Bodies!.Changed).After.Name);

        await BookcaseCase.Applied(served.Host.Session, new TranslateCommand { BodyId = bas, Dx = 0, Dy = 0, Dz = 5 });

        var second = await Preview(served, id);
        Assert.Equal(BookcaseCase.CommandCount + 1, second.DocumentVersion);
        Assert.Equal(0, second.FailedIndex);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, second.FailedCode);
        Assert.Equal(CommandStatus.Rejected, Assert.Single(second.Results).Status);
        Assert.Empty(second.Bodies!.Changed);
        Assert.True(served.Host.Proposals.Find(id)!.Stale);
    }

    // Item 6: a fork sends no event. A subscriber of the host gets the events of the command of the person only, and the sink
    // of the first Document holds no event of a fork.
    [NativeManifoldFact]
    public async Task No_Event_Of_A_Fork_Reaches_A_Subscriber()
    {
        await using var served = await Served.Start(Native, Native);
        var bodies = await BookcaseCase.Build(served.Host.Session);
        var received = new List<EventRecord>();
        using var subscription = served.Host.Subscribe(record =>
        {
            lock (received)
                received.Add(record);
        });
        var before = await Read(served.Host);

        var lifted = Guid.NewGuid();
        var first = await Post(served, "Lift module 1", Translate(lifted, bodies["Module 1 - volume indicatif"], 0, 0, 10), Rename(Guid.NewGuid(), lifted, "Lifted"));
        var second = await Post(served, "Rename Haut", Rename(Guid.NewGuid(), bodies["Haut"], "Haut 2"));
        await Preview(served, first);
        await Preview(served, second);
        lock (received)
            Assert.Empty(received);
        Unchanged(before, await Read(served.Host));

        var person = new RenameBodyCommand { BodyId = bodies["Fond"], NewName = "Fond 2" };
        await BookcaseCase.Applied(served.Host.Session, person);
        Assert.Equal(before.Version + 1, (await Preview(served, first)).DocumentVersion);
        Assert.Equal(before.Version + 1, (await Preview(served, second)).DocumentVersion);

        lock (received)
        {
            Assert.NotEmpty(received);
            Assert.All(received, record => Assert.Equal(person.CommandId, record.CauseCommandId));
        }
    }

    // Item 2 (Q2 A): the host copies the log under the serial lock and makes the fork off it. While a fork waits for its
    // backend, a command of the person applies. The limit of 30 s only ends a test that would wait for ever; a pass does not wait.
    [Fact]
    public async Task The_Fork_Runs_Off_The_Serial_Lock()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var gate = new ManualResetEventSlim(false);
        await using var served = await Served.Start(() =>
        {
            started.TrySetResult();
            gate.Wait();
            return new InProcessMeshBackend();
        });
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        var id = await Post(served, "Rename the box", Rename(Guid.NewGuid(), box, "Box"));
        var forked = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        if (forked != started.Task)
            gate.Set();
        Assert.True(forked == started.Task, "The post of a proposal started no fork.");

        var apply = served.Host.Session.Apply(new RenameBodyCommand { BodyId = box, NewName = "Person" });
        var done = await Task.WhenAny(apply, Task.Delay(TimeSpan.FromSeconds(30)));
        gate.Set();
        Assert.True(done == apply, "A command of the person waited for a fork: the fork holds the serial lock.");
        Assert.Equal(CommandStatus.Applied, (await apply).Status);

        var preview = await Preview(served, id);
        Assert.Equal(2, preview.DocumentVersion);
        Assert.Equal("Box", Assert.Single(preview.Bodies!.Changed).After.Name);
    }

    // Item 5: one preview for each proposal of the queue, so at most 8 open and 32 kept. A closed proposal keeps its last
    // preview and gets no new one; a proposal that leaves the queue takes its preview with it.
    [Fact]
    public async Task The_Queue_Keeps_One_Preview_For_Each_Proposal_And_A_Closed_Proposal_Keeps_Its_Last_Preview()
    {
        await using var served = await Served.Start(Managed);
        var proposals = served.Host.Proposals;
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));

        // The witness stays open, so the new versions copy the log again while the first proposal is closed.
        var first = await Post(served, "Rename the box A", Rename(Guid.NewGuid(), box, "A"));
        var witness = await Post(served, "Rename the box W", Rename(Guid.NewGuid(), box, "W"));
        Assert.Equal(1, (await Preview(served, first)).DocumentVersion);
        Assert.Equal(ProposalStates.Accepted, (await proposals.Accept(first))!.State);
        await BookcaseCase.Applied(served.Host.Session, new RenameBodyCommand { BodyId = box, NewName = "B" });
        Assert.Equal(3, (await Preview(served, witness)).DocumentVersion);
        var kept = await Preview(served, first);
        Assert.Equal(1, kept.DocumentVersion);
        Assert.Null(kept.FailedIndex);
        Assert.NotNull(proposals.Reject(witness));

        var open = new List<Guid>();
        for (var i = 0; i < Proposals.MostOpen; i++)
            open.Add(await Post(served, $"Open {i}", Rename(Guid.NewGuid(), box, $"Open {i}")));
        foreach (var id in open)
            Assert.Equal(3, (await Preview(served, id)).DocumentVersion);
        Assert.Equal(Proposals.MostOpen + 2, proposals.KeptPreviews);

        for (var i = 0; i < Proposals.MostKept; i++)
        {
            Assert.NotNull(proposals.Reject(open[0]));
            open.RemoveAt(0);
            var id = await Post(served, $"Next {i}", Rename(Guid.NewGuid(), box, $"Next {i}"));
            open.Add(id);
            await Preview(served, id);
            Assert.True(proposals.KeptPreviews <= Proposals.MostOpen + Proposals.MostKept, $"{proposals.KeptPreviews} previews are kept.");
        }

        Assert.Null(proposals.Find(first));
        Assert.Null(await proposals.Preview(first));
        Assert.Equal(Proposals.MostOpen + Proposals.MostKept, proposals.KeptPreviews);
    }

    // Item 3: GET /proposals/{id}/preview gives the preview as JSON; an identifier that no proposal has gives HTTP 404 with
    // E-API-PROPOSAL-NOT-FOUND, and a host with no backend for a fork gives HTTP 501 with E-API-NOT-SUPPORTED.
    [Fact]
    public async Task The_Route_Gives_The_Preview_And_Its_Refusals()
    {
        await using var served = await Served.Start(Managed);
        var made = Guid.NewGuid();
        var id = await Post(served, "Make a box", Box(made));

        using (var response = await served.Http.GetAsync($"/proposals/{id}/preview"))
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {text}");
            var body = JsonDocument.Parse(text).RootElement;
            Assert.Equal(id, body.GetProperty("proposalId").GetGuid());
            Assert.Equal("ready", body.GetProperty("state").GetString());
            Assert.Equal(0, body.GetProperty("documentVersion").GetInt64());
            Assert.Equal("Applied", body.GetProperty("results")[0].GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("failedIndex").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("error").ValueKind);
            var bodies = body.GetProperty("bodies");
            Assert.Equal(made, bodies.GetProperty("added")[0].GetProperty("handle").GetProperty("id").GetGuid());
            Assert.Equal(0, bodies.GetProperty("removed").GetArrayLength());
            Assert.Equal(0, bodies.GetProperty("changed").GetArrayLength());
            // P15b (TASK-0459): the three other differences. The new box has its measures after the command only.
            Assert.Equal(0, body.GetProperty("sketches").GetArrayLength());
            var measure = body.GetProperty("measures")[0];
            Assert.Equal(made, measure.GetProperty("bodyId").GetGuid());
            Assert.Equal(JsonValueKind.Null, measure.GetProperty("before").ValueKind);
            Assert.Equal(1000, measure.GetProperty("after").GetProperty("mass").GetProperty("volume").GetDouble());
            Assert.Equal(0, body.GetProperty("checks").GetArrayLength());
        }

        foreach (var route in new[] { $"/proposals/{Guid.NewGuid()}/preview", "/proposals/not-a-guid/preview" })
        {
            using var missing = await served.Http.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal(DiagnosticCodes.ApiProposalNotFound, JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
        }

        await using var plain = await Served.Start(forks: null);
        var other = await Post(plain, "Make a box", Box(Guid.NewGuid()));
        using var refused = await plain.Http.GetAsync($"/proposals/{other}/preview");
        Assert.Equal(HttpStatusCode.NotImplemented, refused.StatusCode);
        Assert.Equal(DiagnosticCodes.ApiNotSupported, JsonDocument.Parse(await refused.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 2: a fork that cannot replay the log, here on a backend of another name or with no backend, gives a failed preview
    // with E-API-PREVIEW-FAILED and the cause, and no outcome.
    [Fact]
    public async Task A_Fork_That_Cannot_Replay_The_Log_Gives_A_Failed_Preview()
    {
        Func<IGeometryBackend>[] forks = [() => new NullGeometryBackend(), () => throw new InvalidOperationException("No backend for this fork.")];
        string[] causes = [DiagnosticCodes.IoBackendMismatch, "No backend for this fork."];
        for (var i = 0; i < forks.Length; i++)
        {
            await using var served = await Served.Start(forks[i]);
            var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
            var id = await Post(served, "Rename the box", Rename(Guid.NewGuid(), box, "Box"));

            var preview = await Preview(served, id);

            Assert.Equal(PreviewStates.Failed, preview.State);
            Assert.Equal(DiagnosticCodes.ApiPreviewFailed, preview.Error?.Code);
            Assert.Contains(causes[i], preview.Error!.Message, StringComparison.Ordinal);
            Assert.Equal(1, preview.DocumentVersion);
            Assert.Empty(preview.Results);
            Assert.Null(preview.Bodies);
            Assert.Null(preview.Sketches);
            Assert.Null(preview.Measures);
            Assert.Null(preview.Checks);
        }
    }

    // Item 4 (Q5 A): the preview compares two body records with the equality of the record. That equality compares content only
    // while no member of a body record is a list, because a list compares by reference. A member with a list fails this test, so
    // the comparison of the preview changes with it.
    [Fact]
    public void A_Body_Record_Has_No_List_So_Its_Equality_Compares_Content()
    {
        foreach (var type in new[] { typeof(BodyRecord), typeof(BodyHandle), typeof(BodyFault) })
            foreach (var property in type.GetProperties())
                Assert.False(
                    property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType),
                    $"{type.Name}.{property.Name} is a {property.PropertyType.Name}: the equality of the record compares it by reference, not by content (ADR-0185 item 4).");
    }

    // ADR-0187 items 1 and 2 (phase R25, TASK-0462): a host that draws sets KeepShapes, and the queue keeps the shape of each body
    // with a new solid of the last preview of an open proposal: the new box has a shape, and the renamed box keeps its solid and
    // has none. The shape is the tessellation of the box after the accept. A closed proposal keeps its preview and drops its
    // shapes. The native backend, because only it gives the triangles of a body (ITessellationOps).
    [NativeManifoldFact]
    public async Task With_KeepShapes_The_Queue_Keeps_The_Shape_Of_Each_New_Solid_Until_The_Proposal_Closes()
    {
        await using var served = await Served.Start(Native, Native);
        served.Host.Proposals.KeepShapes = true;
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        var made = Guid.NewGuid();
        var id = await Post(served, "A box and a name", Box(made), Rename(Guid.NewGuid(), box, "Old"));
        await Preview(served, id);

        var kept = served.Host.Proposals.Kept(id) ?? throw new InvalidOperationException("No kept preview.");
        Assert.True(kept.Current);
        Assert.Equal(0, kept.ShapesLeftOut);
        var shape = Assert.Single(kept.Shapes);
        Assert.Equal(made, shape.BodyId);
        Assert.Equal(12, shape.Triangles.TriangleCount);
        Assert.Null(served.Host.Proposals.Kept(Guid.NewGuid()));

        Assert.Equal(ProposalStates.Accepted, (await served.Host.Proposals.Accept(id))!.State);
        var accepted = (await served.Host.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = made })).Result!;
        Assert.Equal(accepted.Positions, shape.Triangles.Positions);
        Assert.Equal(accepted.Indices, shape.Triangles.Indices);
        var closed = served.Host.Proposals.Kept(id) ?? throw new InvalidOperationException("The closed proposal has no preview.");
        Assert.Empty(closed.Shapes);
        Assert.Same(kept.Preview, closed.Preview);
    }

    // ADR-0187 item 1: with no KeepShapes, as engine-api-http, the fork reads no shape, and the answer of the route has no member
    // for the shapes in each case: a shape is presentation, for the window of the host only (ADR-0007).
    [NativeManifoldFact]
    public async Task With_No_KeepShapes_The_Fork_Reads_No_Shape_And_The_Route_Gives_None()
    {
        foreach (var keep in new[] { false, true })
        {
            await using var served = await Served.Start(Native, Native);
            served.Host.Proposals.KeepShapes = keep;
            var id = await Post(served, "Make a box", Box(Guid.NewGuid()));
            await Preview(served, id);
            Assert.Equal(keep ? 1 : 0, served.Host.Proposals.Kept(id)!.Shapes.Count);

            using var response = await served.Http.GetAsync($"/proposals/{id}/preview");
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(
                ["proposalId", "state", "documentVersion", "results", "failedIndex", "failedCode", "bodies", "sketches", "measures", "checks", "error"],
                body.EnumerateObject().Select(member => member.Name));
        }
    }

    // ADR-0187 item 2: the shapes of one preview stay under the bound of the triangles, in the order of the body identifiers; a
    // body whose shape passes the bound has no shape and counts in ShapesLeftOut. With no bound the fork reads no shape.
    [NativeManifoldFact]
    public async Task The_Shapes_Of_One_Preview_Stay_Under_The_Bound_Of_The_Triangles()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(Engine.Core.Hosting.EngineHosting.CreateDefault(backend));
        ForkSource source;
        try
        {
            source = await session.Read((document, _) => ForkSource.Of(document));
        }
        finally
        {
            session.Close();
        }

        Command[] commands = [new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 }];
        var all = await DocumentFork.Run(source, commands, Native, shapes: int.MaxValue);
        Assert.Equal(commands.Select(command => command.CommandId).Order(), all.Shapes!.Select(shape => shape.BodyId));
        Assert.Equal(0, all.ShapesLeftOut);

        var first = all.Shapes![0].Triangles.TriangleCount;
        var bounded = await DocumentFork.Run(source, commands, Native, shapes: first);
        Assert.Equal([all.Shapes[0].BodyId], bounded.Shapes!.Select(shape => shape.BodyId));
        Assert.Equal(1, bounded.ShapesLeftOut);

        var none = await DocumentFork.Run(source, commands, Native);
        Assert.Null(none.Shapes);
        Assert.Equal(0, none.ShapesLeftOut);
    }

    // ADR-0187 item 3: while a later version of the Document waits for its fork, the kept preview is not current, so the window
    // says "updating"; when the fork ends, the new preview is current.
    [NativeManifoldFact]
    public async Task A_Kept_Preview_Is_Not_Current_While_A_Later_Version_Waits_For_Its_Fork()
    {
        var forks = 0;
        using var gate = new ManualResetEventSlim(true);
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var served = await Served.Start(() =>
        {
            if (Interlocked.Increment(ref forks) > 1 && !gate.IsSet)
                blocked.TrySetResult();
            gate.Wait();
            return new ManifoldGeometryBackend();
        }, Native);
        served.Host.Proposals.KeepShapes = true;
        var box = BookcaseCase.Output(await BookcaseCase.Applied(served.Host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        var id = await Post(served, "Make a box", Box(Guid.NewGuid()));
        await Preview(served, id);
        Assert.True(served.Host.Proposals.Kept(id)!.Current);

        gate.Reset();
        await BookcaseCase.Applied(served.Host.Session, new RenameBodyCommand { BodyId = box, NewName = "Later" });
        var waited = await Task.WhenAny(blocked.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        var stale = served.Host.Proposals.Kept(id)!;
        gate.Set();
        Assert.True(waited == blocked.Task, "The new version started no fork.");
        Assert.False(stale.Current);
        Assert.Equal(1, stale.Preview.DocumentVersion);
        Assert.Single(stale.Shapes);

        await Preview(served, id);
        var fresh = served.Host.Proposals.Kept(id)!;
        Assert.True(fresh.Current);
        Assert.Equal(2, fresh.Preview.DocumentVersion);
        Assert.Single(fresh.Shapes);
    }
}
