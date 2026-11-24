using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.Hotkeys;
using TheLastGate.Hud;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class HudKeyboardTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;
    private const int LogLines = 60;

    private static readonly byte[] ResetPacket = ClientPackets.ShortAndInt(ClientCommand.Reset, 0, 0);

    private readonly NavigationKeys navigationKeys = new();
    private readonly List<string> sentLines = [];
    private readonly List<byte[]> sentPackets = [];
    private HudView hud = null!;
    private GameSession session = null!;
    private SpriteLoader sprites = null!;
    private GameState state = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private ChatLine Line => hud.GetNode<ChatLine>("%ChatLine");

    private bool ResetSent => sentPackets.Any(packet => packet.SequenceEqual(ResetPacket));

    [BeforeTest]
    public async Task ShowTheHud()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        state = new GameState();
        session = new GameSession(state);
        navigationKeys.TurnOff();
        sentPackets.Clear();
        sentLines.Clear();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.Show(state, session, new ClassicFonts(), sentPackets.Add);
        Line.LineSent += sentLines.Add;
        await Frames(2);
    }

    [AfterTest]
    public void CloseTheHud()
    {
        hud.Free();
        sprites.Dispose();
        BindingActions.ResetAll();
        navigationKeys.TurnOn();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_AHotkeyRunsWhileTypingInTheChatLine()
    {
        Line.EnterToTalk = false;
        Keyboard.Type("hello");

        Keyboard.Press(Key.F9);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsTrue();
        AssertThat(Line.Text).IsEqual("hello");
    }

    [TestCase]
    public async Task WithEnterToTalkOff_ACtrlChordTheChatLineWouldUseRunsItsHotkey()
    {
        Rebind("toggle_options_hotkey", new KeyChord("A", MapKeys.Ctrl));
        Line.EnterToTalk = false;
        Keyboard.Type("hello");

        Keyboard.Press(Key.A, true);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_APlainCharacterHotkeyTypesInsteadOfRunning()
    {
        Rebind("toggle_options_hotkey", new KeyChord("Q", MapKeys.None));
        Line.EnterToTalk = false;

        Keyboard.Type("q");
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsFalse();
        AssertThat(Line.Text).IsEqual("q");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_AHotkeyRunsWhileTheLineIsClosed()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.F9);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_NoHotkeyRunsWhileTheLineIsOpen()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);

        Keyboard.Press(Key.F9);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsFalse();
        AssertThat(Line.TakesTyping).IsTrue();
    }

    [TestCase]
    public async Task AHeldHotkeyRunsAgainOnEachRepeat()
    {
        Line.EnterToTalk = true;

        Keyboard.HoldUntilItRepeats(Key.F1);
        await Frames(1);

        AssertThat(sentPackets.Count).IsEqual(2);
    }

    [TestCase]
    public async Task AHotkeyNeedsItsExactModifiers()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.F9, shift: true);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_TypingGoesIntoTheLine()
    {
        Line.EnterToTalk = false;

        Keyboard.Type("ab");
        await Frames(1);

        AssertThat(Line.Text).IsEqual("ab");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_TypingIsDroppedWhileTheLineIsClosed()
    {
        Line.EnterToTalk = true;

        Keyboard.Type("ab");
        await Frames(1);

        AssertThat(Line.Text).IsEqual("");
        AssertThat(Line.TakesTyping).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_TypingGoesIntoTheOpenLine()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);

        Keyboard.Type("ab");
        await Frames(1);

        AssertThat(Line.Text).IsEqual("ab");
    }

    [TestCase]
    public async Task WithEnterToTalkOff_EnterSendsAndLeavesTheLineOpen()
    {
        Line.EnterToTalk = false;
        Keyboard.Type("hello");

        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(string.Join(" | ", sentLines)).IsEqual("hello");
        AssertThat(Line.Text).IsEqual("");
        AssertThat(Line.TakesTyping).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_EnterOnAnEmptyLineSendsNothing()
    {
        Line.EnterToTalk = false;

        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(sentLines).IsEmpty();
        AssertThat(Line.TakesTyping).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_EnterOpensAClosedLine()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsTrue();
        AssertThat(sentLines).IsEmpty();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_KeypadEnterOpensAClosedLine()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.KpEnter);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_EnterSendsAndCloses()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);
        Keyboard.Type("hello");

        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(string.Join(" | ", sentLines)).IsEqual("hello");
        AssertThat(Line.TakesTyping).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_EnterOnAnEmptyLineCloses()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);

        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(sentLines).IsEmpty();
        AssertThat(Line.TakesTyping).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_SlashTypes()
    {
        Line.EnterToTalk = false;

        Keyboard.Press(Key.Slash, typed: '/');
        await Frames(1);

        AssertThat(Line.Text).IsEqual("/");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_SlashOpensAClosedLineWithASlash()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.Slash, typed: '/');
        await Frames(1);

        AssertThat(Line.TakesTyping).IsTrue();
        AssertThat(Line.Text).IsEqual("/");
    }

    [TestCase]
    public async Task WithEnterToTalkOff_UpAndDownWalkTheHistory()
    {
        Line.EnterToTalk = false;
        Send("first");
        Send("second");

        Keyboard.Press(Key.Up);
        Keyboard.Press(Key.Up);
        var older = Line.Text;
        Keyboard.Press(Key.Down);
        await Frames(1);

        AssertThat(older).IsEqual("first");
        AssertThat(Line.Text).IsEqual("second");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_UpOpensAClosedLineWithTheLastLine()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);
        Send("hello");

        Keyboard.Press(Key.Up);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsTrue();
        AssertThat(Line.Text).IsEqual("hello");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_UpThenEnterSendsTheRecalledLine()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);
        Send("hello");

        Keyboard.Press(Key.Up);
        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(string.Join(" | ", sentLines)).IsEqual("hello | hello");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_DownLeavesAClosedLineClosed()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.Down);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_TabCompletesAWordSentBefore()
    {
        Line.EnterToTalk = false;
        Send("hello");

        Keyboard.Type("he");
        Keyboard.Press(Key.Tab);
        await Frames(1);

        AssertThat(Line.Text.TrimEnd()).IsEqual("hello");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_TabLeavesAClosedLineClosed()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.Tab);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsFalse();
        AssertThat(Line.Text).IsEqual("");
    }

    [TestCase]
    public async Task WithEnterToTalkOff_PageUpScrollsTheChatLog()
    {
        Line.EnterToTalk = false;

        AssertThat(await ScrollAfterPageUp()).IsLess(await NewestScroll());
    }

    [TestCase]
    public async Task WithEnterToTalkOn_PageUpScrollsTheChatLog()
    {
        Line.EnterToTalk = true;

        AssertThat(await ScrollAfterPageUp()).IsLess(await NewestScroll());
    }

    [TestCase]
    public async Task WithEnterToTalkOn_PageUpScrollsTheChatLogWhileTheLineIsOpen()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);

        AssertThat(await ScrollAfterPageUp()).IsLess(await NewestScroll());
    }

    [TestCase]
    public async Task WithEnterToTalkOff_EscapeClosesAWindowAndKeepsTheText()
    {
        Line.EnterToTalk = false;
        Keyboard.Type("hi");
        Keyboard.Press(Key.F9);

        Keyboard.Press(Key.Escape);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsFalse();
        AssertThat(Line.Text).IsEqual("hi");
        AssertThat(ResetSent).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_EscapeWithNoWindowOpenResets()
    {
        Line.EnterToTalk = false;

        Keyboard.Press(Key.Escape);
        await Frames(1);

        AssertThat(ResetSent).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_EscapeResetsWhileTheLineIsClosed()
    {
        Line.EnterToTalk = true;

        Keyboard.Press(Key.Escape);
        await Frames(1);

        AssertThat(ResetSent).IsTrue();
    }

    [TestCase]
    public async Task WithEnterToTalkOn_EscapeOnTheOpenLineOnlyClearsAndClosesIt()
    {
        Line.EnterToTalk = true;
        Keyboard.Press(Key.Enter);
        Keyboard.Type("hi");

        Keyboard.Press(Key.Escape);
        await Frames(1);

        AssertThat(Line.TakesTyping).IsFalse();
        AssertThat(Line.Text).IsEqual("");
        AssertThat(ResetSent).IsFalse();
    }

    [TestCase]
    public async Task WithEnterToTalkOff_EditingKeysEditTheLine()
    {
        Line.EnterToTalk = false;
        Keyboard.Type("ab");

        Keyboard.Press(Key.Backspace);
        await Frames(1);

        AssertThat(Line.Text).IsEqual("a");
    }

    [TestCase]
    public async Task AWindowFieldThatLetsGoGivesFocusBackToTheChatLine()
    {
        var field = AddWindowField();
        field.GrabFocus();
        await Frames(1);

        field.ReleaseFocus();
        await Frames(1);

        AssertThat(Line.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task AWindowFieldThatIsHiddenGivesFocusBackToTheChatLine()
    {
        var field = AddWindowField();
        field.GrabFocus();
        await Frames(1);

        field.Hide();
        await Frames(1);

        AssertThat(Line.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task AClickedButtonWorksAndLeavesFocusOnTheChatLine()
    {
        var clicks = 0;
        var button = new Button { Position = new Vector2(600, 300), Size = new Vector2(80, 30) };
        button.Pressed += () => clicks++;
        hud.AddChild(button);
        await Frames(1);

        Mouse.Click(new Vector2(640, 315));
        await Frames(1);

        AssertThat(clicks).IsEqual(1);
        AssertThat(Line.HasFocus()).IsTrue();
        AssertThat(Line.IsEditing()).IsTrue();
    }

    [TestCase]
    public async Task TabAndTheArrowKeysNeverMoveFocus()
    {
        Line.EnterToTalk = true;
        var focusChanges = 0;
        hud.GetViewport().GuiFocusChanged += _ => focusChanges++;

        foreach (var key in new[] { Key.Tab, Key.Down, Key.Left, Key.Right, Key.Tab })
        {
            Keyboard.Press(key);
        }

        await Frames(1);

        AssertThat(focusChanges).IsEqual(0);
        AssertThat(Line.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task WhileAWindowFieldIsTypedIn_ItsTypingStaysInIt()
    {
        var field = await TypeInAWindowField();

        Keyboard.Type("ab");
        await Frames(1);

        AssertThat(field.Text).IsEqual("ab");
        AssertThat(Line.Text).IsEqual("");
    }

    [TestCase]
    public async Task WhileAWindowFieldIsTypedIn_HotkeysWait()
    {
        await TypeInAWindowField();

        Keyboard.Press(Key.F9);
        await Frames(1);

        AssertThat(session.OptionsWindow.IsOpen).IsFalse();
    }

    [TestCase]
    public async Task WhileAWindowFieldIsTypedIn_EscapeEndsTheTypingAndNothingElse()
    {
        var field = await TypeInAWindowField();

        Keyboard.Press(Key.Escape);
        await Frames(1);

        AssertThat(field.HasFocus()).IsFalse();
        AssertThat(Line.HasFocus()).IsTrue();
        AssertThat(ResetSent).IsFalse();
    }

    private async Task<LineEdit> TypeInAWindowField()
    {
        Line.EnterToTalk = false;
        var field = AddWindowField();
        field.GrabFocus();
        await Frames(1);
        return field;
    }

    private LineEdit AddWindowField()
    {
        var field = new LineEdit { Position = new Vector2(600, 300), Size = new Vector2(120, 30) };
        hud.AddChild(field);
        return field;
    }

    private static void Rebind(string id, KeyChord chord) =>
        BindingActions.Apply(BindingRules.Assign(BindingActions.Current(), id, chord).Changes);

    private static void Send(string line)
    {
        Keyboard.Type(line);
        Keyboard.Press(Key.Enter);
    }

    private async Task<double> NewestScroll()
    {
        Keyboard.Press(Key.Pagedown);
        Keyboard.Press(Key.Pagedown);
        Keyboard.Press(Key.Pagedown);
        await Frames(2);
        return LogScrollBar().Value;
    }

    private async Task<double> ScrollAfterPageUp()
    {
        for (var lineNumber = 0; lineNumber < LogLines; lineNumber++)
        {
            state.Chat.Add(new LogLine($"Line {lineNumber}", (int)TextColour.Yellow));
        }

        await Frames(2);
        Keyboard.Press(Key.Pageup);
        await Frames(2);
        return LogScrollBar().Value;
    }

    private static async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private VScrollBar LogScrollBar() =>
        hud.GetNode<ChatLog>("%ChatLog").GetNode<RichTextLabel>("Text").GetVScrollBar();
}
