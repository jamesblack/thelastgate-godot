using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class FontChoiceRowsTests
{
    private static readonly FontOption[] Choices =
        [new(FontChoice.ClassicId, "Classic"), new("dogicapixel", "Dogica Pixel")];

    [TestCase]
    public async Task TheFontRows_ShowTheChosenFontAndReportAPick()
    {
        var window = await HudScene.Show<OptionsWindowView>("res://Hud/options_window.tscn");
        var picked = new List<string>();
        window.FontChosen += picked.Add;

        window.ShowFontChoices(Choices, "dogicapixel");
        var rows = window.GetNode("%FontOptions").GetChildren().Cast<Control>().ToList();
        AssertThat(rows.Select(row => row.GetNode<Label>("Line/Text").Text)).ContainsExactly("Classic", "Dogica Pixel");
        AssertThat(Box(rows[1]).Checked).IsTrue();
        AssertThat(picked).IsEmpty();

        Box(rows[0]).Toggle();
        AssertThat(picked).ContainsExactly(FontChoice.ClassicId);
        AssertThat(Box(rows[1]).Checked).IsFalse();

        Box(rows[0]).Toggle();
        AssertThat(picked).ContainsExactly(FontChoice.ClassicId);
        AssertThat(Box(rows[0]).Checked).IsTrue();
        window.Free();
    }

    [TestCase]
    public async Task AChoiceThatIsNoLongerOffered_ShowsClassic()
    {
        var window = await HudScene.Show<OptionsWindowView>("res://Hud/options_window.tscn");

        window.ShowFontChoices(Choices, "vt323");

        AssertThat(Box((Control)window.GetNode("%FontOptions").GetChild(0)).Checked).IsTrue();
        window.Free();
    }

    [TestCase]
    public async Task ANewFontSet_RedrawsTheChatInIt()
    {
        var chat = await HudScene.Show<ChatView>("res://Hud/chat_view.tscn");
        var log = new TextLog();
        chat.Log.Show(log, new ClassicFonts());
        var other = new OtherFonts();

        chat.Log.ShowFonts(other);

        AssertThat(chat.Log.GetNode<RichTextLabel>("Text").GetThemeFont("normal_font")).IsSame(OtherFonts.Font);
        chat.Free();
    }

    private static ProceduralCheckBox Box(Control row) => row.GetNode<ProceduralCheckBox>("Line/Box");

    private sealed class OtherFonts : IFontSet
    {
        public static readonly FontVariation Font = new();

        public bool CentresLikeTheCurrentClient => false;

        public GameFont For(TextColour colour) => new(Font, 9, Colors.White);

        public GameFont Caption(TextColour colour) => ClassicFonts.SavedCaption(colour);
    }
}
