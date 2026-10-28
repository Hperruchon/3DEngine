using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Tests;

// The state machine of the pad (ADR-0032 item 3, TASK-0094): the fields, the active field, the
// edits, and an apply that gives three numbers or the error of the first field that fails.
public class NumericPadTests
{
    private static NumericPad Box() => new("New box", "mm", new("x", "10"), new("y", "10"), new("z", "10"));

    [Fact]
    public void A_New_Pad_Has_Its_Fields_And_The_First_One_Is_Active()
    {
        var pad = Box();

        Assert.Equal([new PadField("x", "10"), new PadField("y", "10"), new PadField("z", "10")], pad.Fields);
        Assert.Equal(0, pad.Active);
        Assert.Equal(10.0, pad.Preview.Value);
        Assert.Equal("mm", pad.Unit);
        Assert.Null(pad.Message);
    }

    [Fact]
    public void The_Edits_Change_The_Active_Field_Only()
    {
        var pad = Box();

        pad.Append("0*2");
        pad.Next();
        pad.Clear();
        pad.Append("sqrt(9)");
        pad.Next();
        pad.Delete();

        Assert.Equal(["100*2", "sqrt(9)", "1"], pad.Fields.Select(field => field.Text));
        Assert.Equal(2, pad.Active);
        pad.Next();
        Assert.Equal(0, pad.Active);
    }

    // ADR-0032 item 3: the pad accepts only the characters of the grammar.
    [Fact]
    public void The_Pad_Drops_Each_Character_That_The_Grammar_Does_Not_Have()
    {
        var pad = Box();
        pad.Clear();

        pad.Append("1,5 €x+pi\t");

        Assert.Equal("15 +pi", pad.Fields[0].Text);
    }

    [Fact]
    public void A_Field_Takes_64_Characters_At_Most()
    {
        var pad = Box();

        pad.Append(new string('1', 100));

        Assert.Equal(Expression.MaximumLength, pad.Fields[0].Text.Length);
    }

    [Fact]
    public void An_Apply_Gives_The_Three_Numbers()
    {
        var pad = Box();
        pad.Next();
        pad.Clear();
        pad.Append("2*pi");

        Assert.Equal([10.0, 2 * Math.PI, 10.0], pad.Apply()!);
        Assert.Null(pad.Message);
    }

    // The first field that fails becomes the active field, with its name in the message.
    [Fact]
    public void An_Apply_With_A_Bad_Field_Gives_Its_Error_And_Makes_It_Active()
    {
        var pad = Box();
        pad.Next();
        pad.Next();
        pad.Append("/0");
        pad.Select(1);
        pad.Clear();
        pad.Select(0);

        Assert.Null(pad.Apply());
        Assert.Equal(1, pad.Active);
        Assert.StartsWith("y: ", pad.Message, StringComparison.Ordinal);
        Assert.Contains("empty", pad.Message, StringComparison.Ordinal);
    }

    // ADR-0032 item 8: the host gives the rejection of the command, and the next edit clears it.
    [Fact]
    public void A_Rejection_Stays_Until_The_Next_Edit()
    {
        var pad = Box();

        pad.Fail("E-GEOM-INVALID-PARAM: CreateBox requires sizeX, sizeY, sizeZ all strictly greater than zero.");
        Assert.StartsWith("E-GEOM-INVALID-PARAM", pad.Message, StringComparison.Ordinal);
        pad.Next();
        Assert.NotNull(pad.Message);
        pad.Append("1");
        Assert.Null(pad.Message);
    }

    [Fact]
    public void A_Pad_Has_Three_Fields_And_Selects_Only_One_Of_Them()
    {
        Assert.Throws<ArgumentException>(() => new NumericPad("Move", "mm", new PadField("dx", "0")));
        var pad = Box();
        Assert.Throws<ArgumentOutOfRangeException>(() => pad.Select(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => pad.Select(-1));
    }
}
