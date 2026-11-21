using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core.Drawing;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0179 items 4 to 6 (phase R30i, TASK-0428): the sheet page draws the frame, the centring marks, the title block with its
// fields and the symbol of the projection from the one list of TitleBlock.Layout, as the three files do. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetTitlePageTests
{
    private static readonly LiveBody Body = new(Guid.Parse("7d000000-0000-0000-0000-000000000003"), "Plate", "Extrude", true);

    // True when a triangle of ink of the frame holds the point: each of its three corners has the colour of the ink.
    private static bool Covered(UiDrawData drawn, Vector2 point)
    {
        static float Side(Vector2 a, Vector2 b, Vector2 p) => ((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X));
        foreach (var command in drawn.Commands)
        {
            for (var i = 0; i + 2 < command.IndexCount; i += 3)
            {
                var corners = new UiVertex[3];
                for (var k = 0; k < 3; k++)
                    corners[k] = drawn.Vertices[command.VertexOffset + drawn.Indices[command.FirstIndex + i + k]];
                if (corners.Any(corner => corner.Colour != Theme.Ink))
                    continue;
                var (a, b, c) = (corners[0].Position, corners[1].Position, corners[2].Position);
                var (s1, s2, s3) = (Side(a, b, point), Side(b, c, point), Side(c, a, point));
                if ((s1 >= -0.01f && s2 >= -0.01f && s3 >= -0.01f) || (s1 <= 0.01f && s2 <= 0.01f && s3 <= 0.01f))
                    return true;
            }
        }

        return false;
    }

    // Items 4 to 6: on A3 in landscape and in portrait, first and third angle, the page has ink a third of the way along each
    // straight line of the layout that is not a centre line, 2 pixels from the start of each centre line, at 45 degrees on each
    // circle of the symbol, and in the box of each text of the block.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void The_Page_Draws_Each_Line_And_Each_Text_Of_The_Layout(bool landscape, bool thirdAngle)
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var id = Guid.NewGuid();
        var (width, height) = landscape ? (420.0, 297.0) : (297.0, 420.0);
        var record = new SheetRecord(id, "Sheet 1", "A3", landscape, thirdAngle, [], []);
        var sheet = new SheetInfo(id, "Sheet 1", "A3", landscape, thirdAngle, width, height, [], [])
        {
            TitleBlock = SheetTitleBlock.Empty with { Title = "Bracket", IdentificationNumber = "DWG-0042", Material = "AlMg3", IssueDate = "2026-11-21" },
        };
        var drawn = UiDrawData.Empty;
        void Frame()
        {
            ui.ShowSheets([record]);
            ui.ShowPage(sheet, false);
            drawn = ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        }

        Frame();
        ui.OpenPage();
        Frame();
        Frame();
        Frame();
        var paper = ui.PagePaper ?? throw new InvalidOperationException("No paper.");
        var layout = TitleBlock.Layout(sheet);

        foreach (var line in layout.Lines)
        {
            Vector2 at;
            if (line.Circle is { } circle)
            {
                var half = circle.Radius * Math.Sqrt(0.5);
                at = paper.Pixel(circle.X + half, circle.Y + half);
            }
            else if (line.Kind == "centre")
            {
                var (from, to) = (paper.Pixel(line.FromX, line.FromY), paper.Pixel(line.ToX, line.ToY));
                at = from + (2f * Vector2.Normalize(to - from));
            }
            else
            {
                at = paper.Pixel(line.FromX + ((line.ToX - line.FromX) / 3), line.FromY + ((line.ToY - line.FromY) / 3));
            }

            Assert.True(Covered(drawn, at), $"No ink at {at} for the line {line.Kind} ({line.FromX}, {line.FromY}) ({line.ToX}, {line.ToY}).");
        }

        var ink = drawn.Vertices.Where(vertex => vertex.Colour == Theme.Ink).Select(vertex => vertex.Position).ToList();
        foreach (var text in layout.Texts)
        {
            var (from, to) = (paper.Pixel(text.X, text.Y + text.Size), paper.Pixel(text.X + TitleBlock.Measure(text.Text, text.Size), text.Y));
            Assert.True(ink.Any(p => p.X >= from.X && p.X <= to.X && p.Y >= from.Y - 2 && p.Y <= to.Y + 2), $"No ink in the box of the text '{text.Text}'.");
        }
    }

    // Finding V224 of the codebase review of 2026-11-21 (TASK-0431): a text of the block has a floor of 6 pixels, so at a small
    // scale of the paper a label is larger than its cell. Each text is clipped to its cell (ADR-0179 item 7): each text has a
    // draw whose clip is inside its cell and that has ink in the cell. Until TASK-0431 the clip was the whole block, so a label
    // went into the next cell.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void Each_Text_Of_The_Block_Is_Clipped_To_Its_Cell(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var id = Guid.NewGuid();
        var record = new SheetRecord(id, "Sheet 1", "A3", true, false, [], []);
        var sheet = new SheetInfo(id, "Sheet 1", "A3", true, false, 420, 297, [], [])
        {
            TitleBlock = SheetTitleBlock.Empty with { Title = "Bracket", IdentificationNumber = "DWG-0042", Material = "AlMg3", Revision = "B" },
        };
        var drawn = UiDrawData.Empty;
        for (var frame = 0; frame < 4; frame++)
        {
            if (frame == 1)
                ui.OpenPage();
            ui.ShowSheets([record]);
            ui.ShowPage(sheet, false);
            drawn = ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        }

        var paper = ui.PagePaper ?? throw new InvalidOperationException("No paper.");
        var (bx, by, _, _) = TitleBlock.Box(sheet.Width, sheet.Height);
        bool Ink(UiDrawCommand command, Vector4 cell) => Enumerable.Range(0, (int)command.IndexCount)
            .Select(i => drawn.Vertices[command.VertexOffset + drawn.Indices[command.FirstIndex + i]])
            .Any(vertex => vertex.Colour == Theme.Ink && vertex.Position.X > cell.X && vertex.Position.X < cell.Z && vertex.Position.Y > cell.Y && vertex.Position.Y < cell.W);
        foreach (var text in TitleBlock.Layout(sheet).Texts)
        {
            var cell = TitleBlock.Cells.Single(cell => bx + cell.X0 + TitleBlock.Padding == text.X && text.Y > by + cell.Y0 && text.Y < by + cell.Y1);
            var (low, high) = (paper.Pixel(bx + cell.X0, by + cell.Y1), paper.Pixel(bx + cell.X1, by + cell.Y0));
            var box = new Vector4(low.X, low.Y, high.X, high.Y);
            Assert.True(
                drawn.Commands.Any(command => command.ClipRectangle.X >= box.X - 0.5f && command.ClipRectangle.Y >= box.Y - 0.5f
                    && command.ClipRectangle.Z <= box.Z + 0.5f && command.ClipRectangle.W <= box.W + 0.5f && Ink(command, box)),
                $"No draw of the text '{text.Text}' has a clip inside its cell {box}.");
        }
    }
}
