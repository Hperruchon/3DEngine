using System.Numerics;
using System.Text.Json;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0092 item 7 (the agent arc, phase R16r, TASK-0326): a proposal of an agent in the window, with no GPU. On a landscape
// display it is an amber arc on the band of a ring from 235 to 305 degrees, with Reject and Accept at its ends; on a
// portrait display a sheet above the anchors; nothing on the home screen.
[Collection(DearImGui.Collection)]
public sealed class ProposalArcTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly Vector2 Low = new(720, 520);

    private static readonly Guid Bracket = Guid.Parse("73000000-0000-0000-0000-000000000001");
    private static readonly LiveBody BracketBody = new(Bracket, "Extrude", "Bracket", true);

    private static ProposalCard Card(string summary = "Widen the bracket to 75 mm", int commands = 1, bool stale = false, bool applying = false)
        => new(Guid.NewGuid(), summary, [.. Enumerable.Range(1, commands).Select(i => $"NoOp {i}")], stale, applying);

    private static UiDrawData Frame(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Guid? selection = null, Vector2? display = null, Vector2? centre = null, params InputSample[] samples)
    {
        var size = display ?? Display;
        ui.Route(samples);
        return ui.Frame(1f / 60f, size, Vector2.One, [BracketBody], selection, new DocumentLine("bracket", null),
            ringCentre: selection is null ? null : centre ?? Low, proposals: cards);
    }

    // Two frames, so that the windows and the items of the last frame are in their places.
    private static UiDrawData Frames(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Guid? selection = null, Vector2? display = null, Vector2? centre = null)
    {
        Frame(ui, cards, selection, display, centre);
        return Frame(ui, cards, selection, display, centre);
    }

    private static Vector2 Middle(Vector4 rectangle) => new((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // The angle of a point around a centre, in degrees from 0 to 360, y down.
    private static double Degrees(Vector2 point, Vector2 centre)
    {
        var angle = Math.Atan2(point.Y - centre.Y, point.X - centre.X) * 180 / Math.PI;
        return angle < 0 ? angle + 360 : angle;
    }

    private static void Tap(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Vector2 from, Vector2 to, DeviceKind kind, Vector2? display = null)
    {
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        var size = display ?? Display;
        Assert.Empty(ui.Route([new PointerSample(kind, 1, pointer, from.X, from.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0)]));
        ui.Frame(1f / 60f, size, Vector2.One, [BracketBody], Bracket, new DocumentLine("bracket", null), ringCentre: Low, proposals: cards);
        Frame(ui, cards, Bracket, size, null, new PointerSample(kind, 1, pointer, to.X, to.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Frame(ui, cards, Bracket, size);
    }

    // Item 7: the arc is on the band of the ring of the selected body, above its centre, with Reject at 235 degrees and Accept
    // at 305 degrees, each a circle of at least 44 pixels that is a part of the panels for a tap; the caption is above the arc.
    [Fact]
    public void The_Arc_Is_On_The_Band_Of_The_Ring_From_235_To_305_Degrees()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card()], Bracket);

        // ADR-0101 item 3 (TASK-0315): an extrude has Move, Distance, Hide and Focus; ADR-0119 item 1 (TASK-0346): and Rename.
        // ADR-0122 item 1 (TASK-0348): and Dimensions.
        var ring = DesktopUi.Layout(Display, Low, 6);
        Assert.Equal(ring, ui.ProposalRing);
        var band = ring.Inner + (DesktopUi.RingBand / 2f);
        foreach (var (item, degrees) in new[] { (DesktopUi.ProposalRejectItem, 235.0), (DesktopUi.ProposalAcceptItem, 305.0) })
        {
            var target = Item(ui, item);
            var middle = Middle(target);
            Assert.True(target.Z - target.X >= DesktopUi.MinimumControlHeight && target.W - target.Y >= DesktopUi.MinimumControlHeight, $"{item} {target} is less than 44 pixels.");
            Assert.True(Math.Abs(Vector2.Distance(middle, ring.Centre) - band) < 0.5f, $"{item} at {middle} is not on the middle of the band ({band} from {ring.Centre}).");
            Assert.True(Math.Abs(Degrees(middle, ring.Centre) - degrees) < 0.5, $"{item} at {middle} is at {Degrees(middle, ring.Centre)} degrees, not {degrees}.");
            Assert.True(ui.OverPanel(middle.X, middle.Y), $"A tap on {item} reaches the scene.");
            Assert.True(ui.OverPanel(target.X + 4, middle.Y), $"A tap on the edge of {item} reaches the scene.");
        }

        Assert.True(Middle(Item(ui, DesktopUi.ProposalRejectItem)).X < Middle(Item(ui, DesktopUi.ProposalAcceptItem)).X, "Reject is not at the left end.");

        // The caption is above the outer edge of the arc and says AGENT and the summary; the lines are inside the ring.
        var caption = Item(ui, DesktopUi.ProposalCaptionItem);
        Assert.Equal("AGENT · Widen the bracket to 75 mm", ui.ProposalTexts[DesktopUi.ProposalCaptionItem]);
        Assert.True(caption.W <= ring.Centre.Y - ring.Outer, $"The caption {caption} is not above the arc of {ring}.");
        var line = Item(ui, DesktopUi.ProposalLineItem(0));
        Assert.True(Vector2.Distance(new Vector2(line.X, line.Y), ring.Centre) < ring.Inner && Vector2.Distance(new Vector2(line.Z, line.Y), ring.Centre) < ring.Inner,
            $"The command line {line} is not inside the ring.");
        Assert.True(line.Y > Item(ui, DesktopUi.ProposalAcceptItem).W, "The command line is not under the arc.");

        // The middle of the arc is a part of the band but no target: a tap there gives no action.
        var top = ring.Centre - new Vector2(0, band);
        Assert.False(ui.OverPanel(top.X, top.Y));
    }

    // Item 7: when the room above the arc is under the top bar, the caption is the first line inside the ring.
    [Fact]
    public void A_Ring_At_The_Top_Has_Its_Caption_Inside()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card()], Bracket, centre: new Vector2(720, 0));

        var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
        var caption = Item(ui, DesktopUi.ProposalCaptionItem);
        Assert.True(caption.Y > Item(ui, DesktopUi.ProposalAcceptItem).W && caption.W < Item(ui, DesktopUi.ProposalLineItem(0)).Y + 0.5f,
            $"The caption {caption} is not the first line inside the ring {ring}.");
    }

    // ADR-0107 (phase P0.138, TASK-0330): with no selection the ring of the arc is at the middle of the room right of the
    // panel, with the inner radius of a ring of a body where the room is wide. Until TASK-0330 it was at the centre of the
    // display (ADR-0092 item 7).
    [Fact]
    public void With_No_Selection_The_Arc_Is_In_The_Room_Right_Of_The_Panel()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card()]);

        var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
        var panel = ui.PanelRectangles.Where(rectangle => rectangle.X < Display.X / 2f).Max(rectangle => rectangle.Z);
        Assert.Equal((panel + 16f + Display.X - 16f) / 2f, ring.Centre.X, 0.01f);
        Assert.Equal(DesktopUi.Layout(Display, Display / 2f, 3).Inner, ring.Inner);
        var accept = Middle(Item(ui, DesktopUi.ProposalAcceptItem));
        Assert.True(Math.Abs(Degrees(accept, ring.Centre) - 305) < 0.5);
    }

    private static readonly Guid Plate = Guid.Parse("73000000-0000-0000-0000-0000000000a1");

    // ADR-0107: a live sketch and no selection, the state of finding V159 of the codebase review of 2026-11-09 (b).
    private static void LiveSketchFrame(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Vector2 display, params InputSample[] samples)
    {
        var sketch = new Engine.Core.Queries.SketchInfo(Plate, Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [BracketBody], null, new DocumentLine("bracket", null),
            sketchPanel: new SketchPanel([sketch], null, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null), proposals: cards);
    }

    // ADR-0107, finding V159: at each display size, with a live sketch and no selection, the window of the live sketches does
    // not show, each end and its word are out of each panel, and a tap on each end gives its action. Until TASK-0330, at 1280
    // by 720, the window covered Accept and a tap on it gave nothing, and the panel covered the word "Reject".
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1440, 900)]
    [InlineData(1920, 1080)]
    [InlineData(1024, 640)]
    [InlineData(900, 600)]
    public void With_A_Live_Sketch_Each_End_Takes_Its_Tap(int width, int height)
    {
        var display = new Vector2(width, height);
        var card = Card();
        foreach (var (item, expected) in new[] { (DesktopUi.ProposalAcceptItem, (PanelAction)new AcceptProposalAction(card.Id)), (DesktopUi.ProposalRejectItem, new RejectProposalAction(card.Id)) })
        {
            using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
            LiveSketchFrame(ui, [card], display);
            LiveSketchFrame(ui, [card], display);

            Assert.DoesNotContain(ui.PanelRectangles, panel => panel.X >= display.X / 2f);
            var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
            Assert.InRange(ring.Inner, DesktopUi.ProposalInnerMinimum, DesktopUi.Layout(display, display / 2f, 3).Inner);
            foreach (var part in ui.ProposalWords.Append(Item(ui, DesktopUi.ProposalRejectItem)).Append(Item(ui, DesktopUi.ProposalAcceptItem)))
            {
                foreach (var panel in ui.PanelRectangles)
                    Assert.False(part.X < panel.Z && part.Z > panel.X && part.Y < panel.W && part.W > panel.Y, $"{width}x{height}: {part} is under the panel {panel}.");
                Assert.True(part.X >= 0 && part.Z <= display.X, $"{width}x{height}: {part} is out of the display.");
            }

            var at = Middle(Item(ui, item));
            LiveSketchFrame(ui, [card], display, new PointerSample(DeviceKind.Touch, 1, 3UL, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
            LiveSketchFrame(ui, [card], display, new PointerSample(DeviceKind.Touch, 1, 3UL, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
            LiveSketchFrame(ui, [card], display);
            Assert.Equal([expected], ui.TakeActions());
        }
    }

    // ADR-0107: with no proposal, the window of the live sketches shows as before.
    [Fact]
    public void With_No_Proposal_The_Window_Of_The_Live_Sketches_Shows()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var display = new Vector2(1280, 720);
        LiveSketchFrame(ui, [], display);
        LiveSketchFrame(ui, [], display);
        Assert.Contains(ui.PanelRectangles, panel => panel.X >= display.X / 2f);
    }

    // Item 6: a press and a release on Reject give the reject, on Accept the accept, by touch and by mouse; the tap does not
    // reach the scene. A press on one end and a release on the other gives nothing, and a proposal that applies takes no tap.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_Reject_Or_Accept_Gives_Its_Action(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var card = Card();
        Frames(ui, [card], Bracket);
        var reject = Middle(Item(ui, DesktopUi.ProposalRejectItem));
        var accept = Middle(Item(ui, DesktopUi.ProposalAcceptItem));

        Tap(ui, [card], reject, reject, kind);
        Assert.Equal([new RejectProposalAction(card.Id)], ui.TakeActions());
        Tap(ui, [card], accept, accept, kind);
        Assert.Equal([new AcceptProposalAction(card.Id)], ui.TakeActions());

        // The word beside a circle is a part of its target.
        var word = Item(ui, DesktopUi.ProposalAcceptItem);
        var right = new Vector2(word.Z + 20, Middle(word).Y);
        Tap(ui, [card], right, right, kind);
        Assert.Equal([new AcceptProposalAction(card.Id)], ui.TakeActions());

        Tap(ui, [card], reject, accept, kind);
        Assert.Empty(ui.TakeActions());

        var applying = card with { Applying = true };
        Frames(ui, [applying], Bracket);
        Tap(ui, [applying], accept, accept, kind);
        Assert.Empty(ui.TakeActions());
    }

    // Finding V160 of the codebase review of 2026-11-09 (b) (TASK-0331): the arc is on the band of the ring of the selected
    // body, over its tools. A tap on each end and around its middle gives only the action of the proposal: no tool of the
    // ring and no pad. Until TASK-0331 a tap on Accept also gave the tool of the ring under it, and the pad of Move opened.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_An_End_Uses_No_Tool_Of_The_Ring(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var card = Card();
        // A selected face that takes a sketch gives the ring a fourth tool, so its band reaches the end of Accept, as in the
        // probe of the review.
        var face = new FaceLine("Face +z of Bracket", true, []);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [BracketBody], Bracket, new DocumentLine("bracket", null), face: face, ringCentre: Low, proposals: [card]);
        }

        Step();
        Step();
        var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
        var onTools = 0;
        var words = ui.ProposalWords;
        foreach (var (item, word, expected) in new[] { (DesktopUi.ProposalRejectItem, words[0], (PanelAction)new RejectProposalAction(card.Id)), (DesktopUi.ProposalAcceptItem, words[1], new AcceptProposalAction(card.Id)) })
        {
            var middle = Middle(Item(ui, item));
            var points = new[] { Vector2.Zero, new Vector2(16, 0), new Vector2(-16, 0), new Vector2(0, 16), new Vector2(0, -16) }.Select(offset => middle + offset)
                .Concat([Middle(word), new Vector2(word.X + 4, Middle(word).Y), new Vector2(word.Z - 4, Middle(word).Y)]);
            foreach (var at in points)
            {
                if (ring.At([DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.SketchOnFaceButtonLabel], at) is not null)
                    onTools++;
                Step(new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
                Step(new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
                Step();
                Assert.Equal([expected], ui.TakeActions());
                Assert.Null(ui.Pad);
            }
        }

        // The control: some points are on a tool of the ring.
        Assert.True(onTools >= 1, "No point is on a tool of the ring.");
    }

    // Items 3 to 6: the four command lines and "and N more", the count and the steps of undo, "1 of 3" when more proposals
    // wait, and the line of a stale proposal.
    [Fact]
    public void The_Lines_Show_Four_Commands_The_Count_And_The_Place()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var six = Card(commands: 6);
        Frames(ui, [six, Card("Second"), Card("Third")], Bracket);

        var texts = ui.ProposalTexts;
        Assert.Equal(["NoOp 1", "NoOp 2", "NoOp 3", "NoOp 4"], Enumerable.Range(0, DesktopUi.ProposalLines).Select(i => texts[DesktopUi.ProposalLineItem(i)]));
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalLineItem(4)));
        Assert.Equal("and 2 more", texts[DesktopUi.ProposalMoreItem]);
        Assert.Equal("6 commands · 6 steps of undo", texts[DesktopUi.ProposalCountItem]);
        Assert.Equal("1 of 3", texts[DesktopUi.ProposalPlaceItem]);
        Assert.False(texts.ContainsKey(DesktopUi.ProposalStaleItem));
        Assert.Equal("AGENT · Widen the bracket to 75 mm", texts[DesktopUi.ProposalCaptionItem]);
        var lines = new[] { DesktopUi.ProposalLineItem(3), DesktopUi.ProposalMoreItem, DesktopUi.ProposalCountItem, DesktopUi.ProposalPlaceItem }.Select(item => Item(ui, item).Y).ToArray();
        Assert.Equal(lines.Order(), lines);

        Frames(ui, [Card(stale: true)], Bracket);
        texts = ui.ProposalTexts;
        Assert.Equal("1 command · 1 step of undo", texts[DesktopUi.ProposalCountItem]);
        Assert.False(texts.ContainsKey(DesktopUi.ProposalMoreItem));
        Assert.False(texts.ContainsKey(DesktopUi.ProposalPlaceItem));
        Assert.Equal(DesktopUi.ProposalStaleText, texts[DesktopUi.ProposalStaleItem]);

        // Item 5: the text of a command is its name and its parameters, with an identifier cut to 8 characters.
        var sketch = Guid.Parse("3f2a1b4c-0000-0000-0000-000000000001");
        var constraint = Guid.Parse("9e8d7c6b-0000-0000-0000-000000000002");
        using var parameters = JsonDocument.Parse($"{{\"sketchId\":\"{sketch}\",\"constraintId\":\"{constraint}\",\"value\":75}}");
        Assert.Equal("SetSketchConstraintValue 3f2a1b4c 9e8d7c6b 75",
            ProposalCard.Line("SetSketchConstraintValue", parameters.RootElement.EnumerateObject().Select(member => KeyValuePair.Create(member.Name, member.Value))));
    }

    // Item 7: the top bar says "AGENT IS WAITING" in amber while a proposal waits, and the status line gives the outcome of the
    // last accept or reject above the line of the status.
    [Fact]
    public void The_Top_Bar_Waits_And_The_Status_Line_Gives_The_Outcome()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [], Bracket);
        Assert.Null(ui.ItemRectangle(DesktopUi.ProposalWaitingItem));

        Frames(ui, [Card()], Bracket);
        var waiting = Item(ui, DesktopUi.ProposalWaitingItem);
        var title = Item(ui, DesktopUi.TitleItem);
        Assert.Equal(DesktopUi.AgentWaitingText, ui.ProposalTexts[DesktopUi.ProposalWaitingItem]);
        Assert.True(waiting.X > title.Z && waiting.Y < title.W, $"The word {waiting} is not in the top bar {title}.");
        // ADR-0108 item 6 (phase R18a, TASK-0332): the word ends 12 pixels left of the command line, in the middle of its height.
        var command = Item(ui, DesktopUi.CommandItem);
        Assert.True(Math.Abs(command.X - 12f - waiting.Z) < 1f, $"The word {waiting} does not end 12 pixels left of the command line {command}.");
        Assert.True(Math.Abs(((waiting.Y + waiting.W) / 2f) - ((command.Y + command.W) / 2f)) < 1f, $"The word {waiting} is not in the middle of the height of the command line {command}.");

        var outcome = new ProposalOutcome("failed", 1, 3, 1, "E-GEOM-INVALID-PARAM");
        Assert.Equal("Proposal failed at command 2 of 3 (E-GEOM-INVALID-PARAM): 1 command applied", outcome.Text);
        Assert.Equal("Proposal accepted: 2 commands applied, 2 steps of undo", new ProposalOutcome("accepted", 2, 2).Text);
        Assert.Equal("Proposal rejected: no command applied", new ProposalOutcome("rejected", 0, 2).Text);
        Assert.Equal("The proposal is no longer open", ProposalOutcome.Gone.Text);

        ui.ShowProposalOutcome(outcome.Text);
        Frames(ui, [], Bracket);
        var line = Item(ui, DesktopUi.ProposalOutcomeItem);
        var status = Item(ui, DesktopUi.StatusLabel);
        Assert.Equal(outcome.Text, ui.ProposalTexts[DesktopUi.ProposalOutcomeItem]);
        Assert.True(line.W <= status.Y && Math.Abs(line.Z - status.Z) < 1f, $"The outcome {line} is not above the status line {status}.");
    }

    // Item 7: the tools of the ring stay, but dim, while a proposal waits: the label of each segment has a third of its alpha.
    [Fact]
    public void The_Tools_Of_The_Ring_Dim_While_A_Proposal_Waits()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        byte Alpha(UiDrawData data)
        {
            var label = Item(ui, DesktopUi.RingTextItem(DesktopUi.MoveLabel));
            return data.Vertices.Where(vertex => vertex.Position.X >= label.X && vertex.Position.X <= label.Z && vertex.Position.Y >= label.Y && vertex.Position.Y <= label.W)
                .Select(vertex => (byte)(vertex.Colour >> 24)).DefaultIfEmpty().Max();
        }

        Assert.Equal(255, Alpha(Frames(ui, [], Bracket)));
        var dim = Alpha(Frames(ui, [Card()], Bracket));
        Assert.True(dim is > 0 and < 128, $"The label of Move has the alpha {dim} while a proposal waits.");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.MoveLabel)));
    }

    // Item 7: on a portrait display the proposal is a sheet above the anchors, with the same lines, Reject and Accept.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Portrait_Display_Shows_A_Sheet_Above_The_Anchors(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var portrait = new Vector2(390, 844);
        var card = Card(commands: 5);
        Frames(ui, [card, Card("Second")], Bracket, portrait);

        Assert.Null(ui.ProposalRing);
        var anchors = Item(ui, DesktopUi.UndoLabel);
        foreach (var item in new[] { DesktopUi.ProposalRejectItem, DesktopUi.ProposalAcceptItem })
        {
            var button = Item(ui, item);
            Assert.True(button.W - button.Y >= DesktopUi.MinimumControlHeight && button.Z - button.X >= DesktopUi.MinimumControlHeight, $"{item} {button} is less than 44 pixels.");
            Assert.True(button.W <= anchors.Y, $"{item} {button} is not above the anchors {anchors}.");
            Assert.True(ui.OverPanel(Middle(button).X, Middle(button).Y), $"A tap on {item} reaches the scene.");
        }

        var texts = ui.ProposalTexts;
        Assert.Equal("AGENT · Widen the bracket to 75 mm", texts[DesktopUi.ProposalCaptionItem]);
        Assert.Equal("NoOp 4", texts[DesktopUi.ProposalLineItem(3)]);
        Assert.Equal("and 1 more", texts[DesktopUi.ProposalMoreItem]);
        Assert.Equal("1 of 2", texts[DesktopUi.ProposalPlaceItem]);
        Assert.Equal(DesktopUi.AgentWaitingText, texts[DesktopUi.ProposalWaitingItem]);

        var accept = Middle(Item(ui, DesktopUi.ProposalAcceptItem));
        Tap(ui, [card, Card("Second")], accept, accept, kind, portrait);
        Assert.Equal([new AcceptProposalAction(card.Id)], ui.TakeActions());
        var reject = Middle(Item(ui, DesktopUi.ProposalRejectItem));
        Tap(ui, [card, Card("Second")], reject, reject, kind, portrait);
        Assert.Equal([new RejectProposalAction(card.Id)], ui.TakeActions());
    }

    // ADR-0111 item 4 (TASK-0331): on a portrait display the sheet ends above the ring of the selected body, so each tool of the
    // ring is out of the sheet and takes its tap, while the sheet stays under the top bar. Finding V162 of the codebase review
    // of 2026-11-09 (b): until TASK-0331 the sheet ended above the anchors and covered Move, Set distance and Hide at 390 by 844.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(412, 915)]
    [InlineData(393, 873)]
    public void A_Portrait_Sheet_Leaves_The_Tools_Of_The_Ring_Free(int width, int height)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var portrait = new Vector2(width, height);
        var card = Card(commands: 2);
        Frames(ui, [card], Bracket, portrait);
        Frames(ui, [card], Bracket, portrait);

        // The sheet is the smallest panel that holds Accept; the panel of the bodies holds it too on a phone.
        var accept = Item(ui, DesktopUi.ProposalAcceptItem);
        var sheet = ui.PanelRectangles.Where(panel => panel.Y < accept.Y && panel.W > accept.W).MinBy(panel => panel.W - panel.Y);
        var top = ui.RingTop() ?? throw new InvalidOperationException("No ring.");
        Assert.True(sheet.W <= top, $"{width}x{height}: the sheet {sheet} is not above the ring, whose top is {top}.");
        Assert.True(sheet.Y >= Item(ui, DesktopUi.TitleItem).W, $"{width}x{height}: the sheet {sheet} is under the top bar.");
        foreach (var label in new[] { DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel })
        {
            var tool = Middle(Item(ui, DesktopUi.RingItem(label)));
            Assert.False(tool.X >= sheet.X && tool.X < sheet.Z && tool.Y >= sheet.Y && tool.Y < sheet.W, $"{width}x{height}: {label} at {tool} is under the sheet {sheet}.");
        }

        // A tap on Move opens its pad, as with no proposal; the proposal stays.
        var move = Middle(Item(ui, DesktopUi.RingItem(DesktopUi.MoveLabel)));
        Tap(ui, [card], move, move, DeviceKind.Touch, portrait);
        Assert.DoesNotContain(ui.TakeActions(), action => action is AcceptProposalAction or RejectProposalAction);
        Assert.NotNull(ui.Pad);
    }

    // ADR-0111 item 4: when the room between the top bar and the ring is too small for the sheet, the sheet ends above the
    // anchors and covers the ring, as before; its Reject and Accept still take their taps.
    [Fact]
    public void A_Sheet_With_No_Room_Above_The_Ring_Ends_Above_The_Anchors()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var portrait = new Vector2(390, 560);
        var card = Card(commands: 5);
        Frames(ui, [card], Bracket, portrait);
        Frames(ui, [card], Bracket, portrait);

        var anchors = Item(ui, DesktopUi.UndoLabel);
        var accept = Item(ui, DesktopUi.ProposalAcceptItem);
        Assert.True(accept.W <= anchors.Y && accept.W > (ui.RingTop() ?? 0f), $"Accept {accept} is not between the ring and the anchors {anchors}.");
        var at = Middle(accept);
        Tap(ui, [card], at, at, DeviceKind.Touch, portrait);
        Assert.Equal([new AcceptProposalAction(card.Id)], ui.TakeActions());
    }

    // ADR-0111 item 3 (TASK-0331): while an accept runs, the host holds each action and each intent that can change the
    // Document; the selection, the camera, the frame and the tools that open a pad stay free. An action that the list does not
    // name is held. Finding V163 of the codebase review of 2026-11-09 (b): until TASK-0331 an Undo ran between two commands of
    // an accept.
    [Fact]
    public void The_Actions_That_Change_The_Document_Wait_For_An_Accept()
    {
        var body = Guid.NewGuid();
        PanelAction[] held =
        [
            new UndoAction(), new RedoAction(), new CreateBoxAction(10, 10, 10), new MoveBodyAction(body, 1, 0, 0),
            new SetDistanceAction(body, 20), new RenameBodyAction(body, "Plate"), new SetVisibleAction(body, false),
            new ShowAllAction(), new OpenProjectAction(), new NewPartAction(), new ImportStlAction(), new FinishSketchAction(),
        ];
        foreach (var action in held)
            Assert.True(AcceptHold.Holds(action), $"{action.GetType().Name} is not held.");
        PanelAction[] free =
        [
            new SelectBodyAction(body), new FrameAction(), new HomeAction(), new EditDistanceAction(body),
            new AcceptProposalAction(body), new RejectProposalAction(body),
        ];
        foreach (var action in free)
            Assert.False(AcceptHold.Holds(action), $"{action.GetType().Name} is held.");

        foreach (var intent in new Intent[] { new UndoIntent(), new RedoIntent(), new HideIntent(1, 1), new ShowAllIntent(), new SketchPointIntent(1, 1), new GizmoEndIntent(1, 1) })
            Assert.True(AcceptHold.Holds(intent), $"{intent.GetType().Name} is not held.");
        foreach (var intent in new Intent[] { new OrbitIntent(1, 1), new PanIntent(1, 1), new ZoomIntent(2), new SelectIntent(1, 1), new GizmoMoveIntent(1, 1) })
            Assert.False(AcceptHold.Holds(intent), $"{intent.GetType().Name} is held.");
    }

    // ADR-0114 item 3 (finding V165 of the codebase review of 2026-11-10, TASK-0339): a tap on the path line and a focus on one
    // body write nothing to the log, so they stay free while an accept runs. Escape, which goes up one level too, was free.
    [Fact]
    public void The_Path_Line_And_The_Focus_Are_Free_During_An_Accept()
    {
        foreach (var action in new PanelAction[] { new PathAction(PathLevel.Document), new FocusAction(), new EndFocusAction() })
            Assert.False(AcceptHold.Holds(action), $"{action.GetType().Name} is held.");
    }

    // ADR-0114 item 4 (finding T181, TASK-0339): the decision of the host. With no accept each action and each intent passes;
    // while the task of an accept is not null, an action or an intent that changes the Document is held and the host shows the
    // text, the end of a drag of the gizmo becomes a cancel, and the camera passes.
    [Fact]
    public void The_Host_Holds_Only_While_An_Accept_Runs()
    {
        var running = new TaskCompletionSource<ProposalOutcome>().Task;
        Assert.False(AcceptHold.Held(new UndoAction(), null));
        Assert.True(AcceptHold.Held(new UndoAction(), running));
        Assert.False(AcceptHold.Held(new FrameAction(), running));

        Intent undo = new UndoIntent(), end = new GizmoEndIntent(1, 1), orbit = new OrbitIntent(1, 1);
        Assert.Equal((undo, false), AcceptHold.Pass(undo, null));
        Assert.Equal(((Intent?)null, true), AcceptHold.Pass(undo, running));
        Assert.Equal((end, false), AcceptHold.Pass(end, null));
        Assert.Equal((new GizmoCancelIntent(), true), AcceptHold.Pass(end, running));
        Assert.Equal((orbit, false), AcceptHold.Pass(orbit, running));
    }

    // Item 7: the arc does not show on the home screen; nothing of a proposal is there, and no tap reaches it.
    [Fact]
    public void Nothing_Shows_On_The_Home_Screen()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card()], Bracket);
        var accept = Middle(Item(ui, DesktopUi.ProposalAcceptItem));

        ui.ShowHome(["bracket"]);
        Frames(ui, [Card()], Bracket);

        Assert.Null(ui.ProposalRing);
        Assert.Empty(ui.ProposalTexts);
        foreach (var item in new[] { DesktopUi.ProposalRejectItem, DesktopUi.ProposalAcceptItem, DesktopUi.ProposalCaptionItem, DesktopUi.ProposalWaitingItem })
            Assert.Null(ui.ItemRectangle(item));
        Tap(ui, [Card()], accept, accept, DeviceKind.Touch);
        Assert.DoesNotContain(ui.TakeActions(), action => action is AcceptProposalAction or RejectProposalAction);
    }

    // ADR-0092 items 2, 6 and 7 (TASK-0326): the desktop host gives the window the queue of its surface. A proposal that an
    // agent posts over HTTP to the desktop reaches the window through HostedSession, an accept there applies its command,
    // and a reject applies nothing. Until the review of TASK-0326 the class did not give the queue, so the real window
    // showed no proposal although each test of the window passed.
    [Fact]
    public async Task A_Proposal_Posted_To_The_Desktop_Reaches_The_Window_And_Its_Accept_Applies()
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-proposal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "documents"));
        try
        {
            string[] args =
            [
                "--port", "0",
                "--file", Path.Combine(folder, "documents", "plate.3de"),
                $"--{DesktopSurface.DocumentFolderSetting}={Path.Combine(folder, "documents")}",
                $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(folder, "unsaved")}",
                $"--Engine:AssetFolder={Path.Combine(folder, "assets")}",
            ];
            var (surface, code, message) = DesktopEngineHost.Start(args, null);
            Assert.True(surface is not null, $"{code}: {message}");
            using var session = surface.Session;
            using (surface)
            {
                using var client = new HttpClient { BaseAddress = new Uri(surface.Address) };
                const string box = "{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":75,\"sizeY\":40,\"sizeZ\":10}}";
                async Task Post(string summary)
                {
                    using var body = new StringContent($"{{\"summary\":\"{summary}\",\"commands\":[{box}]}}", System.Text.Encoding.UTF8, "application/json");
                    Assert.Equal(System.Net.HttpStatusCode.Created, (await client.PostAsync("/proposals", body)).StatusCode);
                }

                await Post("A plate");
                await Post("A second plate");

                IProposalDesk desk = new HostedSession(surface.Host);
                var waiting = desk.Waiting();
                Assert.Equal(["A plate", "A second plate"], waiting.Select(card => card.Summary));
                Assert.StartsWith("CreateBox", Assert.Single(waiting[0].Commands));

                var accepted = await desk.Accept(waiting[0].Id);
                Assert.Equal(("accepted", 1, 1), (accepted.State, accepted.Applied, accepted.Count));
                var rejected = desk.Reject(waiting[1].Id);
                Assert.Equal(("rejected", 0), (rejected.State, rejected.Applied));

                Assert.Empty(desk.Waiting());
                Assert.Equal(1, await surface.Host.Session.Read((document, _) => document.Bodies.Count));
            }
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
