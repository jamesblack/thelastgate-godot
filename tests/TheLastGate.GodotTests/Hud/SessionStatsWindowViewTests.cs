using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Session;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class SessionStatsWindowViewTests
{
    private static readonly CurrencyTally[] Tallies =
    [
        new(Currency.Experience, 1200, 84000, 60000),
        new(Currency.Gold, 52000, 9100, 2500),
    ];

    private SessionStatsWindowView view = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        view = await HudScene.Show<SessionStatsWindowView>("res://Hud/session_stats_window.tscn");
        view.ShowWindow(true, Tallies, TimeSpan.FromMinutes(90));
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => view.Free();

    [TestCase]
    public void Window_HasNoClassicNodes()
    {
        foreach (var type in (string[])[nameof(NinePatchRect), nameof(Button), nameof(PanelContainer)])
        {
            AssertThat(view.FindChildren("*", type, true, false).Count).IsEqual(0);
        }

        foreach (var label in view.FindChildren("*", nameof(Label), true, false))
        {
            AssertThat(label is HudLabel).IsTrue();
        }

        AssertThat(view.Theme).IsNull();
    }

    [TestCase]
    public void Window_KeepsClicksOffTheWorld()
    {
        var frame = view.GetNode<Control>("Window/Frame").GetGlobalRect();

        AssertThat(ClickThrough.ReachesTheWorldAt(frame.Position + new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.GetCenter())).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(new Vector2(frame.End.X + 0.5f, frame.Position.Y + 5))).IsFalse();
    }

    [TestCase]
    public void TheWindow_ShowsEachCurrencyAndTheTime()
    {
        var rows = view.GetNode("%Table/Body").GetChildren().Cast<SettingsRow>().Where(row => row.Visible).ToList();

        AssertThat(rows.Count).IsEqual(2);
        AssertThat(rows[1].FindChildren("*", nameof(Label), true, false).Cast<Label>().First().Text).IsEqual("Gold");
        AssertThat(view.GetNode<Label>("%Elapsed").Text).IsEqual(SessionStatsText.Elapsed(TimeSpan.FromMinutes(90)));
    }

    [TestCase]
    public void ResetAndClose_RaiseTheirEvents()
    {
        var resets = 0;
        var closes = 0;
        view.ResetPressed += () => resets++;
        view.ClosePressed += () => closes++;

        view.GetNode<ClickArea>("%Reset/Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
        view.GetNode<ClickArea>("%Close").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat((resets, closes)).IsEqual((1, 1));
    }
}
