using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;

namespace Engine.Core.Preview;

// ADR-0185 items 8 to 10 (phase P15b, TASK-0459): the state of the solver, the measures and the checks of the preview of a
// proposal. The fork reads each value with the query that gives it to a client (GetSketch, GetMassProperties and
// CheckInterference), on its own session, so the preview and a query of the accepted Document give the same values. No code
// here computes a measure or a check.
//
// "Before" is the fork before the first command of the proposal. It equals the first Document at its version by the replay
// rule (ADR-0012). The fork reads "after" in its own session after the last command that ran; it reads "before" in a second
// replay of the same copy of the log (the base), only when a value before is necessary (item 10).

// Item 8: the states of the solver of a sketch. Solved: each equation is within its tolerance and the sketch has no free
// degree. Under: each equation is within its tolerance and the sketch has a free degree. Over: an equation is not within its
// tolerance, because the constraints conflict or the iteration did not converge (ADR-0035 items 5 and 6).
public static class SketchStates
{
    public const string Solved = "solved";
    public const string Under = "under";
    public const string Over = "over";
}

// Item 8: the state of the solver of one sketch, from GetSketch. Freedom is the count of the free degrees (ADR-0035 item 8).
// Residual is the largest absolute residual of the constraints of the sketch, 0 with no constraint, or null when a residual
// is not finite (finding E88).
public sealed record SketchState(string State, int Freedom, double? Residual)
{
    public static SketchState Of(SketchInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        double? largest = 0;
        foreach (var constraint in info.Constraints)
        {
            if (constraint.Residual is not { } residual)
            {
                largest = null;
                break;
            }

            largest = Math.Max(largest.Value, Math.Abs(residual));
        }

        var state = !info.Solved ? SketchStates.Over : info.Freedom > 0 ? SketchStates.Under : SketchStates.Solved;
        return new SketchState(state, info.Freedom, largest);
    }
}

// Item 8: one sketch that the proposal adds or changes. Before is null for a sketch that the proposal adds.
public sealed record SketchChange(Guid SketchId, SketchState? Before, SketchState After);

// Item 9: the measures of one body, as the query GetMassProperties gives them, or the refusal of the query, for
// example E-MESH-UNSUPPORTED for a mesh body.
public sealed record BodyMeasure(MassProperties? Mass, ErrorDetail? Error);

// Item 9: one body that the proposal adds or changes, with its measures after the commands and, for a changed body, before
// them. Before is null for a body that the proposal adds.
public sealed record MeasureChange(Guid BodyId, BodyMeasure? Before, BodyMeasure After);

// Item 10: the states of a pair. Apart, touching and overlapping are the classes of the query CheckInterference. Limit:
// the check of the pair is above the bound of work of the check, E-CHECK-WORK-LIMIT. Refused: the check refused the pair
// with another code, for example E-GEOM-BODY-FAULTED.
public static class PairStates
{
    public const string Apart = "apart";
    public const string Touching = "touching";
    public const string Overlapping = "overlapping";
    public const string Limit = "limit";
    public const string Refused = "refused";
}

// Item 10: the state of one pair, with the answer of CheckInterference for a class, or its refusal for limit and refused.
public sealed record PairState(string State, PairCheck? Check, ErrorDetail? Error)
{
    public static PairState Of(QueryResult<InterferenceInfo> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Error is { } error)
            return new PairState(error.Code == DiagnosticCodes.CheckWorkLimit ? PairStates.Limit : PairStates.Refused, null, error);

        var check = result.Result!.Pairs.Single();
        var state = check.Class switch
        {
            PairClass.Apart => PairStates.Apart,
            PairClass.Touching => PairStates.Touching,
            PairClass.Overlapping => PairStates.Overlapping,
            _ => throw new InvalidOperationException($"The class {check.Class} of a pair has no state of the preview."),
        };
        return new PairState(state, check, null);
    }
}

// Item 10: one pair whose state changes, BodyIdA before BodyIdB in the order of the identifiers. Before is null when a body
// of the pair does not exist before the commands, and After is null when a body does not exist after them; a body that does
// not exist counts as apart.
public sealed record PairChange(Guid BodyIdA, Guid BodyIdB, PairState? Before, PairState? After)
{
    // Item 10: a pair is a difference when its state changes, or when the code of its refusal changes. Internal, so the JSON of
    // the preview does not give it.
    internal bool Differs => Key(Before) != Key(After);

    private static (string State, string? Code) Key(PairState? state) => (state?.State ?? PairStates.Apart, state?.Error?.Code);
}

// Items 8 to 10: what the fork reads after the commands and what it reads before them, from the records of the bodies and of
// the sketches before the first command and after the last command that ran.
internal sealed class PreviewPlan
{
    private PreviewPlan()
    {
    }

    // Item 8: each sketch that the proposal adds or changes, in the order of the identifiers, and those of them that exist before.
    public IReadOnlyList<Guid> Sketches { get; private init; } = [];

    public IReadOnlySet<Guid> SketchesBefore { get; private init; } = new HashSet<Guid>();

    // Item 9: each body that the proposal adds or changes, in the order of the identifiers; the changed bodies with a new
    // solid, whose measures before the base gives; and the changed bodies with the same solid.
    public IReadOnlyList<Guid> Measured { get; private init; } = [];

    public IReadOnlySet<Guid> MeasuredBefore { get; private init; } = new HashSet<Guid>();

    public IReadOnlySet<Guid> SameSolid { get; private init; } = new HashSet<Guid>();

    // Item 10: each pair to check, A before B in the order of the identifiers, and true for each side that has both bodies.
    public IReadOnlyList<(Guid A, Guid B, bool Before, bool After)> Pairs { get; private init; } = [];

    // True when a value before the commands is necessary, so the fork replays the base.
    public bool NeedsBase => SketchesBefore.Count > 0 || MeasuredBefore.Count > 0 || Pairs.Any(pair => pair.Before);

    public static PreviewPlan Of(
        IReadOnlyCollection<BodyRecord> bodiesBefore,
        IReadOnlyCollection<BodyRecord> bodiesAfter,
        IReadOnlyCollection<SketchRecord> sketchesBefore,
        IReadOnlyCollection<SketchRecord> sketchesAfter)
    {
        var was = bodiesBefore.ToDictionary(body => body.Handle.Id);
        var now = bodiesAfter.ToDictionary(body => body.Handle.Id);

        // A committed geometry key names one solid (ADR-0059 item 2, ADR-0037 item 2), and a body with a fault has no solid.
        // So a body whose key and fault do not change keeps its solid: its measures and the state of each of its pairs do not
        // change. A rename or a change of the visibility gives such a body.
        var added = now.Keys.Where(id => !was.ContainsKey(id)).ToHashSet();
        var removed = was.Keys.Where(id => !now.ContainsKey(id)).ToHashSet();
        var changed = now.Values.Where(body => was.TryGetValue(body.Handle.Id, out var old) && !old.Equals(body)).ToList();
        var same = changed.Where(body => Same(was[body.Handle.Id], body)).Select(body => body.Handle.Id).ToHashSet();
        var newSolid = changed.Select(body => body.Handle.Id).Where(id => !same.Contains(id)).ToHashSet();

        // Item 10: the pairs of a body with a new solid after the commands, and the pairs of a changed or removed body before
        // them. A removed body counts: Translate and each boolean make a new body and remove the old one (ADR-0021), so a move
        // that ends an overlap shows only in the pair of the removed body.
        var pairs = new SortedSet<(Guid A, Guid B)>();
        foreach (var x in added.Concat(newSolid))
        {
            foreach (var y in now.Keys.Where(y => y != x))
                pairs.Add(Order(x, y));
        }

        foreach (var x in newSolid.Concat(removed))
        {
            foreach (var y in was.Keys.Where(y => y != x))
                pairs.Add(Order(x, y));
        }

        var sketchWas = sketchesBefore.ToDictionary(sketch => sketch.Id);
        var sketches = sketchesAfter
            .Where(sketch => !sketchWas.TryGetValue(sketch.Id, out var old) || !SketchContent.Same(old, sketch))
            .Select(sketch => sketch.Id)
            .OrderBy(id => id)
            .ToList();

        return new PreviewPlan
        {
            Sketches = sketches,
            SketchesBefore = sketches.Where(sketchWas.ContainsKey).ToHashSet(),
            Measured = [.. added.Concat(changed.Select(body => body.Handle.Id)).OrderBy(id => id)],
            MeasuredBefore = newSolid,
            SameSolid = same,
            Pairs = [.. pairs.Select(pair => (pair.A, pair.B, was.ContainsKey(pair.A) && was.ContainsKey(pair.B), now.ContainsKey(pair.A) && now.ContainsKey(pair.B)))],
        };
    }

    private static bool Same(BodyRecord before, BodyRecord after) => before.GeometryKey == after.GeometryKey && Equals(before.Fault, after.Fault);

    private static (Guid A, Guid B) Order(Guid x, Guid y) => x.CompareTo(y) < 0 ? (x, y) : (y, x);
}

// Items 8 to 10: the values that the fork reads in one session, on one side of the commands.
internal sealed class PreviewValues
{
    public Dictionary<Guid, SketchState> Sketches { get; } = [];

    public Dictionary<Guid, BodyMeasure> Measures { get; } = [];

    public Dictionary<(Guid A, Guid B), PairState> Pairs { get; } = [];

    // The values after the commands (after: true) or before them (after: false), each with its query on the session. ADR-0188
    // item 8 (phase P16, TASK-0463): the checks of the two sides take their work from one bound of the preview.
    public static async Task<PreviewValues> Read(DocumentSession session, PreviewPlan plan, bool after, PreviewWork work, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(work);
        var values = new PreviewValues();
        foreach (var id in after ? plan.Sketches : plan.Sketches.Where(plan.SketchesBefore.Contains))
        {
            var sketch = await session.Query<SketchInfo>(new GetSketchQuery { SketchId = id }, ct).ConfigureAwait(false);
            if (sketch.Error is { } error)
                throw new InvalidOperationException($"GetSketch refused the sketch {id} of the fork: {error.Code} {error.Message}");
            values.Sketches[id] = SketchState.Of(sketch.Result!);
        }

        foreach (var id in after ? plan.Measured : plan.Measured.Where(plan.MeasuredBefore.Contains))
        {
            var mass = await session.Query<MassProperties>(new GetMassPropertiesQuery { BodyId = id }, ct).ConfigureAwait(false);
            values.Measures[id] = new BodyMeasure(mass.Error is null ? mass.Result : null, mass.Error);
        }

        // Item 10 (Q4 A): one query for each pair, so the bound of work of the check applies to each pair. ADR-0188 item 8 (Q6 A):
        // the check of a pair may do the work that the bound of the preview leaves, and gives the work that it did. The first
        // pair that the check refuses for its work, and each pair after it, has the state limit, and the fork does not check it.
        foreach (var (a, b, before, isAfter) in plan.Pairs)
        {
            if (after ? !isAfter : !before)
                continue;
            if (work.Spent)
            {
                values.Pairs[(a, b)] = PreviewWork.Bounded;
                continue;
            }

            var check = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = $"{a},{b}", WorkLimit = work.Left }, ct).ConfigureAwait(false);
            values.Pairs[(a, b)] = PairState.Of(check);
            work.Take(check);
        }

        return values;
    }

    // The three differences from the values after the commands and, when the plan needs the base, before them.
    public static (IReadOnlyList<SketchChange> Sketches, IReadOnlyList<MeasureChange> Measures, IReadOnlyList<PairChange> Checks) Differences(
        PreviewPlan plan,
        PreviewValues after,
        PreviewValues? before)
    {
        if (plan.NeedsBase && before is null)
            throw new InvalidOperationException("The plan of the preview needs the values before the commands.");

        var sketches = plan.Sketches
            .Select(id => new SketchChange(id, plan.SketchesBefore.Contains(id) ? before!.Sketches[id] : null, after.Sketches[id]))
            .ToList();
        var measures = plan.Measured
            .Select(id => new MeasureChange(
                id,
                plan.MeasuredBefore.Contains(id) ? before!.Measures[id] : plan.SameSolid.Contains(id) ? after.Measures[id] : null,
                after.Measures[id]))
            .ToList();
        var checks = plan.Pairs
            .Select(pair => new PairChange(pair.A, pair.B, pair.Before ? before!.Pairs[(pair.A, pair.B)] : null, pair.After ? after.Pairs[(pair.A, pair.B)] : null))
            .Where(change => change.Differs)
            .ToList();
        return (sketches, measures, checks);
    }
}

// ADR-0188 item 8 (phase P16, TASK-0463, Q6 A of the codebase review of 2026-11-23): the bound of the sum of the work of the
// checks of one preview, after the commands and before them. Each check gives its work before it splits (CheckInterferenceQueryHandler), and
// the fork adds it here. The bound is the limit of one check, so one preview costs at most the work of one check at its limit,
// about 13 s for an intersection on the computer of the owner. The first pair that its check refuses for the work, and each
// pair after it, has the state limit.
internal sealed class PreviewWork
{
    public const long Most = CheckInterferenceQueryHandler.WorkLimit;

    // The state of a pair after the bound: limit, with the code of the bound of work and no check.
    public static PairState Bounded { get; } = new(PairStates.Limit, null, new ErrorDetail(
        DiagnosticCodes.CheckWorkLimit,
        $"The checks of this preview reached the bound of {Most} products of the pieces before this pair (ADR-0188 item 8)."));

    public PreviewWork(long most = Most) => Left = most;

    // The work that the bound leaves, and true after the first pair that the check refused for its work.
    public long Left { get; private set; }

    public bool Spent { get; private set; }

    // The work of one check: its count when the check ran, or the end of the bound when the check refused the pair for its work.
    public void Take(QueryResult<InterferenceInfo> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (check.Error?.Code == DiagnosticCodes.CheckWorkLimit)
            Spent = true;
        else if (check.Result is { } info)
            Left -= info.Work;
    }
}

// Item 8 (Q5 A): two sketch records compare by content. The equality of SketchRecord compares each list by reference, so this
// comparison reads each member. ProposalPreviewTests fails when a sketch record or one of its items gets a member that this
// comparison does not read.
internal static class SketchContent
{
    public static bool Same(SketchRecord a, SketchRecord b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a.Id == b.Id
            && a.DatumId == b.DatumId
            && a.Name == b.Name
            && a.Points.SequenceEqual(b.Points)
            && a.Lines.SequenceEqual(b.Lines)
            && a.Circles.SequenceEqual(b.Circles)
            && a.Arcs.SequenceEqual(b.Arcs)
            && Same(a.Constraints, b.Constraints, (x, y) => x.Id == y.Id && x.Kind == y.Kind && x.Items.SequenceEqual(y.Items) && x.Values.SequenceEqual(y.Values))
            && Same(a.References, b.References, (x, y) => x.Id == y.Id && Equals(x.Target, y.Target) && x.Points.SequenceEqual(y.Points) && x.Line == y.Line && Equals(x.Fault, y.Fault));
    }

    private static bool Same<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, T, bool> same)
    {
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!same(a[i], b[i]))
                return false;
        }

        return true;
    }
}
