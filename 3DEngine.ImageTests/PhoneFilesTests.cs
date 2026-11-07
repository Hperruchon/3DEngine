using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0083 D1 and D6 (phase P9d, TASK-0260): the files of the phone for the run of the program. The phone has no document
// folder yet, so it shows no file controls and no recent file; a new part and its import pass to the next session once.
public sealed class PhoneFilesTests
{
    [Fact]
    public void A_New_Part_And_Its_Import_Pass_To_The_Next_Session_Once()
    {
        var files = new PhoneFiles();
        var import = new StlImport(new string('a', 64), "bracket.stl");
        Assert.True(files.Home);
        Assert.False(files.TakeRequestedNew());

        files.RequestNew(import);

        Assert.True(files.TakeRequestedNew());
        Assert.False(files.TakeRequestedNew());
        Assert.Equal(import, files.TakeImport());
        Assert.Null(files.TakeImport());
    }

    [Fact]
    public async Task The_Phone_Has_No_Document_Folder_Yet()
    {
        var files = new PhoneFiles();

        Assert.False(files.HasFolder);
        Assert.Empty(files.Recent(5));
        Assert.Null(files.Resolve("plate", out var refusal));
        Assert.NotNull(refusal);
        Assert.NotNull(await files.Open("plate.3de", CancellationToken.None));
        Assert.False(NativeThreeDEngine.LineOf(files, null, null).Files);
    }
}
