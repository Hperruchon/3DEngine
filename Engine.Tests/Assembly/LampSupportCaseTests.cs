using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Assembly;

// Row K10b (TASK-0466): the support of the lamp as the second case of K10. The builder is LampSupportCase.
// docs/proposals/2026-11-25-the-lamp-support-case-of-k10.md gives the table of the bodies; this file holds the same table as
// the expected boxes and the expected classes, apart from the builder, so a wrong size or place in the builder fails a test.
//
// The tests use the native backend, because only that backend turns a body (ADR-0112 item 4). In continuous integration
// they cannot skip (NativeManifoldFact).
public sealed class LampSupportCaseTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-lamp-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // The box of each body, (minimum x, y, z) and (maximum x, y, z) in mm, from the table of the proposal, and true for a body
    // with a curved outside face. A curved body gives the box of its facets, so its bounds agree to Facets; the bounds of a
    // body of plane outside faces are exact. Each hole is inside its body, so it does not change the box.
    private static readonly IReadOnlyDictionary<string, (Aabb Box, bool Curved)> Expected = new Dictionary<string, (Aabb, bool)>(StringComparer.Ordinal)
    {
        [LampSupportCase.Crossbar] = (new(-15, -300, 0, 15, 300, 30), false),
        [LampSupportCase.Bracket] = (new(-15, -30, -30, 25, 30, 0), false),
        [LampSupportCase.Arm] = (new(5, -9, -30, 85, -3, -10), false),
        [LampSupportCase.Knob] = (new(3, -15, -32, 27, 9, -8), true),
        [LampSupportCase.Lamp] = (new(85, -41, -55, 185, 29, 15), true),
        [LampSupportCase.Housing] = (new(-20, -40, 30, 20, 40, 55), false),
        [LampSupportCase.Cable] = (new(20, -8.5, 30, 110, -3.5, 35), true),
    };

    // The largest distance of a bound of a curved body from the bound of its true surface, in mm.
    private const double Facets = 0.5;

    // The envelope of the case (ADR-0126, section 1 of the proposal of 2026-11-12): the mount stays within 60 mm under the lower
    // face of the crossbar, z = 0.
    private const double Envelope = 60;

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<Aabb> Box(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result;
    }

    private static async Task<SortedDictionary<string, Aabb>> Boxes(DocumentSession session)
    {
        var bodies = await session.Read((document, _) => document.Bodies.Select(body => (body.Name ?? "", body.Handle.Id)).ToList());
        var boxes = new SortedDictionary<string, Aabb>(StringComparer.Ordinal);
        foreach (var (name, id) in bodies)
            boxes.Add(name, await Box(session, id));
        return boxes;
    }

    private static bool Near(Aabb a, Aabb b, double tolerance)
        => Math.Abs(a.MinX - b.MinX) <= tolerance && Math.Abs(a.MinY - b.MinY) <= tolerance && Math.Abs(a.MinZ - b.MinZ) <= tolerance
           && Math.Abs(a.MaxX - b.MaxX) <= tolerance && Math.Abs(a.MaxY - b.MaxY) <= tolerance && Math.Abs(a.MaxZ - b.MaxZ) <= tolerance;

    // Each of the seven bodies exists with its name, and its box is the box of the table: exactly for a body of plane outside
    // faces, and to the facets for a curved body. No other body exists. Each part of the mount is within 60 mm under the crossbar.
    [NativeManifoldFact]
    public async Task Each_Body_Of_The_Lamp_Support_Has_Its_Name_And_The_Box_Of_The_Table()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var built = await LampSupportCase.Build(session);

        var boxes = await Boxes(session);
        session.Close();

        Assert.Equal(7, built.Count);
        Assert.Equal(Expected.Keys.Order(StringComparer.Ordinal), boxes.Keys);
        foreach (var (name, (box, curved)) in Expected)
            Assert.True(curved ? Near(box, boxes[name], Facets) : box == boxes[name], $"{name}: the box is {boxes[name]}, and the table gives {box}.");
        foreach (var name in LampSupportCase.Mount)
            Assert.True(boxes[name].MinZ >= -Envelope, $"{name} at {boxes[name]} goes more than {Envelope} mm under the crossbar.");
    }

    // The class of each pair at 0 degrees, by the pair check of K11 (ADR-0182), one query for each pair: the six intended
    // contacts touch, and each other pair is apart, so no pair overlaps and no pair is a fault. The shaft of the knob is 0.25 mm
    // from the wall of the hole of the pivot. A curved body gives the answer of its facets (MeshOnly).
    //
    // Null: the bound of work of ADR-0182 item 8 refuses the pair with E-CHECK-WORK-LIMIT. A body with a hole has many pieces,
    // so 8 of the 21 pairs need from 126,968 to 2,801,550 products, above the limit of 100,000, and the check of the whole case
    // needs 6,133,070. Row K24 removes this gap; then this table gets the class of each pair, and this test fails until it does.
    private static readonly IReadOnlyDictionary<string, PairClass?> Classes = new Dictionary<string, PairClass?>(StringComparer.Ordinal)
    {
        [Key(LampSupportCase.Crossbar, LampSupportCase.Bracket)] = null,
        [Key(LampSupportCase.Crossbar, LampSupportCase.Arm)] = null,
        [Key(LampSupportCase.Crossbar, LampSupportCase.Knob)] = null,
        [Key(LampSupportCase.Crossbar, LampSupportCase.Lamp)] = null,
        [Key(LampSupportCase.Crossbar, LampSupportCase.Housing)] = PairClass.Touching,
        [Key(LampSupportCase.Crossbar, LampSupportCase.Cable)] = null,
        [Key(LampSupportCase.Bracket, LampSupportCase.Arm)] = null,
        [Key(LampSupportCase.Bracket, LampSupportCase.Knob)] = null,
        [Key(LampSupportCase.Bracket, LampSupportCase.Lamp)] = PairClass.Apart,
        [Key(LampSupportCase.Bracket, LampSupportCase.Housing)] = PairClass.Apart,
        [Key(LampSupportCase.Bracket, LampSupportCase.Cable)] = PairClass.Apart,
        [Key(LampSupportCase.Arm, LampSupportCase.Knob)] = null,
        [Key(LampSupportCase.Arm, LampSupportCase.Lamp)] = PairClass.Touching,
        [Key(LampSupportCase.Arm, LampSupportCase.Housing)] = PairClass.Apart,
        [Key(LampSupportCase.Arm, LampSupportCase.Cable)] = PairClass.Apart,
        [Key(LampSupportCase.Knob, LampSupportCase.Lamp)] = PairClass.Apart,
        [Key(LampSupportCase.Knob, LampSupportCase.Housing)] = PairClass.Apart,
        [Key(LampSupportCase.Knob, LampSupportCase.Cable)] = PairClass.Apart,
        [Key(LampSupportCase.Lamp, LampSupportCase.Housing)] = PairClass.Apart,
        [Key(LampSupportCase.Lamp, LampSupportCase.Cable)] = PairClass.Apart,
        [Key(LampSupportCase.Housing, LampSupportCase.Cable)] = PairClass.Touching,
    };

    [NativeManifoldFact]
    public async Task At_Zero_Degrees_Each_Pair_Has_The_Class_Of_The_Table()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var built = await LampSupportCase.Build(session);
        var contacts = LampSupportCase.IntendedContacts.Select(pair => Key(pair.A, pair.B)).ToHashSet(StringComparer.Ordinal);
        var names = built.Keys.ToList();
        var checkedPairs = 0;
        for (var i = 0; i < names.Count; i++)
        {
            for (var j = i + 1; j < names.Count; j++)
            {
                var (a, b) = (built[names[i]], built[names[j]]);
                var key = Key(names[i], names[j]);
                var intended = contacts.Contains(key);
                var result = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = $"{a},{b}", IntendedPairs = intended ? $"{a}:{b}" : null });
                if (Classes[key] is not { } expected)
                {
                    Assert.True(result.Error?.Code == DiagnosticCodes.CheckWorkLimit, $"{key}: the table gives the refusal of the bound of work, and the check gave {result.Error?.Code ?? "an answer"}.");
                    continue;
                }

                Assert.True(result.Error is null, $"{key}: {result.Error?.Code} {result.Error?.Message}");
                var pair = Assert.Single(result.Result!.Pairs);
                Assert.True(pair.Class == expected, $"{key}: the class is {pair.Class} with a gap of {pair.Gap} mm, and the table gives {expected}.");
                Assert.Equal(intended, pair.Intended);
                Assert.False(pair.Fault, $"{key} is a fault.");
                checkedPairs++;
            }
        }

        session.Close();
        Assert.Equal(21, Classes.Count);
        Assert.Equal(13, checkedPairs);
        // Each intended contact that the check answers touches; the table names each of the six.
        Assert.All(contacts, key => Assert.True(Classes.ContainsKey(key), $"{key} is not in the table."));
    }

    // TASK-0473 (finding T200 of the codebase review of 2026-11-25): the bound of work refuses the check of 8 pairs, so the test
    // above checks them by their boxes only. Here each of the 8 pairs shares no volume: the intersection of its two bodies is
    // refused with "share no volume", or it gives a body of no volume (Manifold gives a flat solid for two bodies that touch on
    // a face, ADR-0181 item 3). A builder that moves the shaft of the knob into the arm or into the lug gives a volume, and this
    // test fails. An Undo after each intersection gives the two bodies back for the next pair.
    [NativeManifoldFact]
    public async Task Each_Pair_That_The_Bound_Refuses_Shares_No_Volume()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var built = await LampSupportCase.Build(session);
        var refused = Classes.Where(entry => entry.Value is null).Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        var names = built.Keys.ToList();
        var checkedPairs = 0;
        for (var i = 0; i < names.Count; i++)
        {
            for (var j = i + 1; j < names.Count; j++)
            {
                var key = Key(names[i], names[j]);
                if (!refused.Contains(key))
                    continue;
                var command = new Engine.Core.Commands.IntersectCommand { FirstBodyId = built[names[i]], SecondBodyId = built[names[j]] };
                var result = await session.Apply(command);
                checkedPairs++;
                if (result.Status != CommandStatus.Applied)
                {
                    Assert.True(result.Error?.Code == DiagnosticCodes.GeomNativeOp && result.Error.Message.Contains("share no volume", StringComparison.Ordinal),
                        $"{key}: the intersection gave {result.Error?.Code} {result.Error?.Message}");
                    continue;
                }

                var mass = await session.Query<MassProperties>(new GetMassPropertiesQuery { BodyId = command.CommandId });
                Assert.True(mass.Error is null, $"{key}: {mass.Error?.Code} {mass.Error?.Message}");
                Assert.True(Math.Abs(mass.Result!.Volume) < 1e-6, $"{key}: the two bodies share {mass.Result.Volume} mm³.");
                Assert.Equal(CommandStatus.Applied, (await session.Apply(new Engine.Core.Commands.UndoCommand())).Status);
            }
        }

        session.Close();
        Assert.Equal(8, checkedPairs);
    }

    // A key of a pair that does not depend on the order of the two names.
    private static string Key(string a, string b) => string.CompareOrdinal(a, b) < 0 ? $"{a} · {b}" : $"{b} · {a}";

    // A save and an open give the same bodies: the same records (handle, kind, name, visibility) and the same boxes.
    [NativeManifoldFact]
    public async Task A_Save_And_An_Open_Give_The_Same_Bodies()
    {
        var path = Path.Combine(_folder, "lamp-support" + DocumentFile.Extension);
        List<BodyRecord> records;
        SortedDictionary<string, Aabb> boxes;
        long version;
        using (var backend = new ManifoldGeometryBackend())
        {
            var live = NewSession(backend);
            DocumentFile.Create(live, path);
            await LampSupportCase.Build(live);
            records = await live.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            boxes = await Boxes(live);
            version = await live.Read((document, _) => document.Version);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = NewSession(backend);
            var warnings = await DocumentFile.Open(opened, path);
            var reopened = await opened.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            var reboxes = await Boxes(opened);
            var reversion = await opened.Read((document, _) => document.Version);
            opened.Close();

            Assert.Empty(warnings);
            Assert.Equal(version, reversion);
            Assert.Equal(7, reopened.Count);
            Assert.Equal(records, reopened);
            Assert.Equal(boxes, reboxes);
        }
    }
}
