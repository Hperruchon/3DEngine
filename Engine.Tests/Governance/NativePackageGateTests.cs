using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for ADR-0014 section 5, which requires a checksum for the native
// payload. Register entry R-0005 recorded the gap: the repository held a binary
// of 4.4 MB with no licence element and no checksum.
//
// A checksum in a document and nothing that reads it is a decoration. This gate
// reads each value in THIRD-PARTY-NOTICES.md and compares it against the file,
// therefore a change to the binary that does not change the notices fails the
// build. A change to the notices that does not match the binary fails too.
public class NativePackageGateTests
{
    private const string Package = "nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg";

    // A row of the two checksum tables: | `path` | `sha256` |
    private static readonly Regex Row = new(
        @"^\|\s*`([^`]+)`\s*\|\s*`([0-9a-f]{64})`\s*\|",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void The_Notices_File_Exists_And_Names_Each_Component()
    {
        var notices = Notices();

        foreach (var required in new[] { "Manifold", "Clipper2", "Apache License 2.0", "Boost Software License 1.0" })
            Assert.Contains(required, notices, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Package_Matches_Its_Recorded_Checksum()
    {
        var file = RepositoryFiles.Path(Package.Split('/'));
        Assert.True(File.Exists(file), $"{Package} is absent.");

        var expected = Rows().TryGetValue(Package, out var value) ? value : null;
        Assert.True(
            expected is not null,
            $"THIRD-PARTY-NOTICES.md gives no checksum for {Package}.");

        Assert.Equal(expected, Sha256(File.ReadAllBytes(file)));
    }

    [Fact]
    public void Each_Native_Binary_Matches_Its_Recorded_Checksum()
    {
        using var archive = ZipFile.OpenRead(RepositoryFiles.Path(Package.Split('/')));
        var rows = Rows();
        var problems = new List<string>();
        var checked_ = 0;

        foreach (var (path, expected) in rows)
        {
            if (!path.StartsWith("runtimes/", StringComparison.Ordinal))
                continue;

            var entry = archive.GetEntry(path);
            if (entry is null)
            {
                problems.Add($"{path}: the notices name it, and the package does not hold it");
                continue;
            }

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            var actual = Sha256(memory.ToArray());
            if (actual != expected)
                problems.Add($"{path}: the notices give {expected}, the package holds {actual}");

            checked_++;
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
        Assert.True(checked_ > 0, "THIRD-PARTY-NOTICES.md gives no checksum for a file inside the package.");
    }

    [Fact]
    public void Each_Native_Binary_In_The_Package_Has_A_Recorded_Checksum()
    {
        // The other direction. A new binary that nobody recorded must fail, so
        // that a payload cannot enter the repository without a notice.
        //
        // A file with a version suffix and a file with no suffix hold the same
        // bytes, therefore the gate compares by content and not by name.
        using var archive = ZipFile.OpenRead(RepositoryFiles.Path(Package.Split('/')));
        var recorded = Rows().Values.ToHashSet(StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("runtimes/", StringComparison.Ordinal) || entry.Length == 0)
                continue;

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            if (!recorded.Contains(Sha256(memory.ToArray())))
                missing.Add(entry.FullName);
        }

        Assert.True(
            missing.Count == 0,
            "The package holds a native binary whose content no row of THIRD-PARTY-NOTICES.md "
            + "records. Add a row, and add a notice for its licence. Files:\n  "
            + string.Join("\n  ", missing));
    }

    private static string Notices() => RepositoryFiles.Read("THIRD-PARTY-NOTICES.md");

    private static Dictionary<string, string> Rows()
    {
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in Row.Matches(Notices()))
            rows[match.Groups[1].Value] = match.Groups[2].Value;

        return rows;
    }

    private static string Sha256(byte[] bytes)
        => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
