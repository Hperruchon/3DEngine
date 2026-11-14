using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0130 (phase R18i, TASK-0358): a proposal in the window as in the render: the value now crossed out and the proposed
// value in amber in the rows, a pending node on the history arc, a glow on the edges of the display, and after an accept
// "Applied · Bracket rebuilt" with Undo. With no GPU. ADR-0130 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ProposalWindowTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly Vector2 Centre = new(720, 470);
    private static readonly Guid Bracket = Guid.Parse("74000000-0000-0000-0000-000000000001");
    private static readonly Guid Sketch = Guid.Parse("74000000-0000-0000-0000-000000000002");
    private static readonly Guid Width = Guid.Parse("74000000-0000-0000-0000-000000000003");
    private static readonly LiveBody Body = new(Bracket, "Extrude", "Bracket", true);

    // The distance of the extrude and its width C1 as two chips, and the history of the bracket: its sketch and its extrude.
    private static readonly IReadOnlyList<EdgeValues.Value> Values =
    [
        new(EdgeValues.DistanceKey(Bracket), "10.000 mm", new Vector2(600, 520), 10, Bracket, null, null, "Distance"),
        new(EdgeValues.ConstraintKey(Width), "60.000 mm", new Vector2(760, 380), 60, Bracket, Sketch, Width, "Width", Name: "C1"),
    ];

    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Bracket, "Extrude", "Bracket", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, true),
    ];

    // A proposal that sets C1 to 75, as the host names it (ProposedValues.Named).
    private static readonly ProposalCard Card = new(Guid.Parse("74000000-0000-0000-0000-000000000009"), "Widen the bracket to 75 mm",
        ["SetSketchConstraintValue 74000000 74000000 75"], Values: [new ProposedValue(Sketch, Width, null, 75) { Name = "Plate", Label = "C1", Now = 60 }],
        CommandIds: [Guid.NewGuid()]);

    private static void Frame(DesktopUi ui, IReadOnlyList<ProposalCard>? cards, Vector2? display = null, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display ?? Display, Vector2.One, [Body], Bracket, null, ringCentre: Centre, values: Values, history: Steps, proposals: cards);
    }

    // Item 1: while the proposal shows, the row of C1 gives its value now and the proposed value; the row of the distance,
    // which the proposal does not change, does not. With no proposal no row does.
    [Fact]
    public void A_Row_Gives_The_Value_Now_And_The_Proposed_Value()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, [Card]);
        Frame(ui, [Card]);

        Assert.Equal(("60.000 mm", "75.000 mm"), ui.RowProposals[EdgeValues.ConstraintKey(Width)]);
        Assert.False(ui.RowProposals.ContainsKey(EdgeValues.DistanceKey(Bracket)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RowItem(EdgeValues.ConstraintKey(Width))));

        Frame(ui, null);
        Assert.Empty(ui.RowProposals);
    }

    // Item 2: the pending node is one step above the newest node of the arc, with the text of the first named value, and it goes
    // with the proposal.
    [Fact]
    public void The_History_Arc_Has_A_Pending_Node()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, [Card]);
        Frame(ui, [Card]);

        Assert.Equal("C1 to 75 ?", ui.ProposalTexts[DesktopUi.HistoryPendingItem]);
        var pending = ui.ItemRectangle(DesktopUi.HistoryPendingItem) ?? throw new InvalidOperationException("No pending node.");
        var newest = ui.ItemRectangle(DesktopUi.HistoryItem(Bracket)) ?? throw new InvalidOperationException("No node of the bracket.");
        var (at, after) = (new Vector2((pending.X + pending.Z) / 2f, (pending.Y + pending.W) / 2f), new Vector2((newest.X + newest.Z) / 2f, (newest.Y + newest.W) / 2f));
        Assert.True(at.Y < after.Y && Vector2.Distance(at, after) >= DesktopUi.HistoryNode, $"The pending node {pending} is not above the newest node {newest}, one node apart.");

        Frame(ui, null);
        Assert.Null(ui.ItemRectangle(DesktopUi.HistoryPendingItem));
    }

    // Item 3: a glow along the edges of the display while a proposal waits, on a landscape and a portrait display.
    [Fact]
    public void A_Glow_Marks_The_Edges_While_A_Proposal_Waits()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        foreach (var display in new[] { Display, new Vector2(390, 844) })
        {
            Frame(ui, [Card], display);
            Frame(ui, [Card], display);
            Assert.Equal(new Vector4(0, 0, display.X, display.Y), ui.ProposalGlowRectangle);
        }

        Frame(ui, null);
        Frame(ui, null);
        Assert.Null(ui.ProposalGlowRectangle);
    }

    // Item 4: after an accept the line says what changed and has Undo, a target of 44 pixels; a tap on it gives Undo and the
    // line goes. The text names the body, or the count of the commands with no body.
    [Fact]
    public void After_An_Accept_The_Line_Has_Undo()
    {
        var outcome = new ProposalOutcome("accepted", 2, 2);
        Assert.Equal("Applied · Bracket rebuilt", outcome.AppliedText("Bracket"));
        Assert.Equal("Applied · 2 commands", outcome.AppliedText(null));

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        ui.ShowProposalOutcome(outcome.AppliedText("Bracket"), undo: true);
        Frame(ui, null);
        Frame(ui, null);
        Assert.Equal("Applied · Bracket rebuilt", ui.ProposalTexts[DesktopUi.ProposalOutcomeItem]);
        var undo = ui.ItemRectangle(DesktopUi.ProposalUndoItem) ?? throw new InvalidOperationException("No Undo.");
        Assert.True(undo.W - undo.Y >= DesktopUi.MinimumControlHeight, $"Undo {undo} is lower than 44 pixels.");

        var (x, y) = ((undo.X + undo.Z) / 2f, (undo.Y + undo.W) / 2f);
        ui.TakeActions();
        Frame(ui, null, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, null, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 50_000_000));
        Frame(ui, null, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 100_000_000));
        Frame(ui, null);
        Assert.Contains(ui.TakeActions(), action => action is UndoAction);
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalUndoItem));

        // A reject keeps the line with no Undo.
        ui.ShowProposalOutcome(new ProposalOutcome("rejected", 0, 2).Text);
        Frame(ui, null);
        Frame(ui, null);
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalUndoItem));
    }
}
