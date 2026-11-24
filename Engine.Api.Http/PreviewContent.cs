using System.Text.Json;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Core.Preview;

namespace Engine.Api.Http;

// ADR-0188 item 6 (phase P16, TASK-0463): the accept applies when the result of the commands at the version of its apply equals
// the result that the person saw. Two previews compare by content, member by member, because the equality of a record compares a
// list by reference (section 5 of the codebase review of 2026-11-23; SketchContent does the same for the sketches).
//
// A member that depends on the version or on the time, and not on the result, is not compared: the version of the fork, the
// version and the sequence of each result, and the time of each command. A message is not compared either, because a message
// can name a version or a sum of work; the code of each error and of each diagnostic is compared. ProposalAcceptTests fails when
// one of these records gets a member that this comparison does not name, so the comparison changes with the record.
internal static class PreviewContent
{
    public static bool Same(ProposalPreview a, ProposalPreview b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a.ProposalId == b.ProposalId
            && a.State == b.State
            && a.Error?.Code == b.Error?.Code
            && a.FailedIndex == b.FailedIndex
            && a.FailedCode == b.FailedCode
            && Same(a.Results, b.Results, SameResult)
            && SameBodies(a.Bodies, b.Bodies)
            // A sketch change holds two sketch states, which hold no list, so the equality of the record compares content.
            && Same(a.Sketches, b.Sketches, (x, y) => x == y)
            && Same(a.Measures, b.Measures, (x, y) => x.BodyId == y.BodyId && SameMeasure(x.Before, y.Before) && SameMeasure(x.After, y.After))
            && Same(a.Checks, b.Checks, (x, y) => x.BodyIdA == y.BodyIdA && x.BodyIdB == y.BodyIdB && SamePair(x.Before, y.Before) && SamePair(x.After, y.After));
    }

    // One result: the command, its outcome, its outputs and the codes of its diagnostics and of its error.
    private static bool SameResult(CommandResult a, CommandResult b)
        => a.CommandId == b.CommandId
            && a.CommandName == b.CommandName
            && a.Status == b.Status
            && a.Error?.Code == b.Error?.Code
            && JsonSerializer.Serialize(a.Outputs.Values, ApiJson.Options) == JsonSerializer.Serialize(b.Outputs.Values, ApiJson.Options)
            && a.Diagnostics.Select(diagnostic => (diagnostic.Severity, diagnostic.Code)).SequenceEqual(b.Diagnostics.Select(diagnostic => (diagnostic.Severity, diagnostic.Code)));

    // A body record and a body change hold no list (ProposalPreviewTests), so the equality of the record compares content.
    private static bool SameBodies(BodyDifferences? a, BodyDifferences? b)
        => a is null || b is null
            ? a is null && b is null
            : a.Added.SequenceEqual(b.Added) && a.Removed.SequenceEqual(b.Removed) && a.Changed.SequenceEqual(b.Changed);

    // The mass properties hold no list, so the equality of the record compares content.
    private static bool SameMeasure(BodyMeasure? a, BodyMeasure? b)
        => a is null || b is null ? a is null && b is null : a.Mass == b.Mass && a.Error?.Code == b.Error?.Code;

    // The answer of the check of one pair holds no list, so the equality of the record compares content.
    private static bool SamePair(PairState? a, PairState? b)
        => a is null || b is null ? a is null && b is null : a.State == b.State && a.Check == b.Check && a.Error?.Code == b.Error?.Code;

    private static bool Same<T>(IReadOnlyList<T>? a, IReadOnlyList<T>? b, Func<T, T, bool> same)
    {
        if (a is null || b is null)
            return a is null && b is null;
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
