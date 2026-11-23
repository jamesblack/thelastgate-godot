using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Hotkeys;
using TheLastGate.State;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hotkeys;

[TestSuite]
public class KeybindCaptureTests
{
    private static readonly KeyChord FunctionOne = new("F1", MapKeys.None);

    private static readonly IReadOnlyDictionary<string, KeyChord?> Current = new Dictionary<string, KeyChord?>
    {
        ["speed_fast_hotkey"] = FunctionOne,
        ["speed_slow_hotkey"] = new KeyChord("F3", MapKeys.None),
        ["toggle_worldmap_hotkey"] = null,
        ["item_tooltip_hotkey"] = new KeyChord(KeyNames.Alt, MapKeys.None),
    };

    private OptionsWindow window = new();

    [BeforeTest]
    public void OpenTheKeybindsTab()
    {
        window = new OptionsWindow();
        window.Toggle();
        window.Show(OptionsTab.Keybinds);
    }

    [TestCase]
    public void StartCapture_WaitsForAKeyOnThatRow()
    {
        window.StartCapture("speed_fast_hotkey");

        AssertThat(window.Capturing).IsEqual("speed_fast_hotkey");
    }

    [TestCase]
    public void StartCapture_OnAnotherRow_MovesTheWait()
    {
        window.StartCapture("speed_fast_hotkey");

        window.StartCapture("speed_slow_hotkey");

        AssertThat(window.Capturing).IsEqual("speed_slow_hotkey");
    }

    [TestCase]
    public void Capture_BindsTheKeyWithItsModifiersAndStopsWaiting()
    {
        window.StartCapture("toggle_worldmap_hotkey");
        var ctrlAltM = new KeyChord("M", MapKeys.Ctrl | MapKeys.Alt);

        var change = window.Capture(ctrlAltM, Current);

        AssertThat(change?.Changes).ContainsExactly(new BindingChange("toggle_worldmap_hotkey", ctrlAltM));
        AssertThat(change?.Line).IsNull();
        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void Capture_TakesTheKeyFromTheBindingThatHadItAndSaysSoInRed()
    {
        window.StartCapture("toggle_worldmap_hotkey");

        var change = window.Capture(FunctionOne, Current);

        AssertThat(change?.Changes)
            .ContainsExactly(
                new BindingChange("speed_fast_hotkey", null),
                new BindingChange("toggle_worldmap_hotkey", FunctionOne));
        AssertThat(change?.Line).IsEqual(new LogLine("Unbound Fast Mode (F1) - key reassigned.", (int)TextColour.Red));
    }

    [TestCase(MapKeys.None)]
    [TestCase(MapKeys.Ctrl)]
    public void Capture_LeavesTheSlashForChatAndKeepsWaiting(MapKeys modifiers)
    {
        window.StartCapture("toggle_worldmap_hotkey");

        AssertThat(window.Capture(new KeyChord("/", modifiers), Current)).IsNull();
        AssertThat(window.Capturing).IsEqual("toggle_worldmap_hotkey");
    }

    [TestCase]
    public void Capture_AcceptsTheKeypadSlash()
    {
        window.StartCapture("toggle_worldmap_hotkey");
        var keypadSlash = new KeyChord("Keypad /", MapKeys.None);

        var change = window.Capture(keypadSlash, Current);

        AssertThat(change?.Changes).ContainsExactly(new BindingChange("toggle_worldmap_hotkey", keypadSlash));
    }

    [TestCase(KeyNames.Shift)]
    [TestCase(KeyNames.Ctrl)]
    [TestCase(KeyNames.Alt)]
    public void Capture_KeepsWaitingWhenAModifierIsPressedForAnOrdinaryBinding(string modifier)
    {
        window.StartCapture("toggle_worldmap_hotkey");

        AssertThat(window.Capture(new KeyChord(modifier, MapKeys.None), Current)).IsNull();
        AssertThat(window.Capturing).IsEqual("toggle_worldmap_hotkey");
    }

    [TestCase]
    public void Capture_BindsAModifierOnItsOwnToAHoldBinding()
    {
        window.StartCapture("item_tooltip_hotkey");

        var change = window.Capture(new KeyChord(KeyNames.Shift, MapKeys.Ctrl), Current);

        AssertThat(change?.Changes)
            .ContainsExactly(new BindingChange("item_tooltip_hotkey", new KeyChord(KeyNames.Shift, MapKeys.None)));
        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void Capture_DoesNothingWhenNoRowIsWaiting()
    {
        AssertThat(window.Capture(new KeyChord("F1", MapKeys.Ctrl), Current)).IsNull();
    }

    [TestCase]
    public void CancelCapture_StopsWaiting()
    {
        window.StartCapture("speed_fast_hotkey");

        window.CancelCapture();

        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void UnbindCaptured_ClearsTheBindingAndStopsWaiting()
    {
        window.StartCapture("speed_fast_hotkey");

        AssertThat(window.UnbindCaptured()).IsEqual(new BindingChange("speed_fast_hotkey", null));
        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void UnbindCaptured_DoesNothingWhenNoRowIsWaiting()
    {
        AssertThat(window.UnbindCaptured()).IsNull();
    }

    [TestCase]
    public void ClosingTheWindow_StopsWaiting()
    {
        window.StartCapture("speed_fast_hotkey");

        window.Close();

        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void TogglingTheWindow_StopsWaiting()
    {
        window.StartCapture("speed_fast_hotkey");

        window.Toggle();

        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void ChangingTab_StopsWaiting()
    {
        window.StartCapture("speed_fast_hotkey");

        window.Show(OptionsTab.General);

        AssertThat(window.Capturing).IsNull();
    }

    [TestCase]
    public void ChooseReset_FirstAsksForConfirmation()
    {
        AssertThat(window.ChooseReset()).IsNull();
        AssertThat(window.ConfirmingReset).IsTrue();
    }

    [TestCase(OptionsTab.General, "General options have been set to their defaults.")]
    [TestCase(OptionsTab.Audio, "Audio settings have been set to their defaults.")]
    [TestCase(OptionsTab.Keybinds, "Keybinds have been set to their defaults.")]
    [TestCase(OptionsTab.Display, "Display settings have been set to their defaults.")]
    [TestCase(OptionsTab.Experimental, "Experimental options have been set to their defaults.")]
    public void ChooseReset_Twice_ResetsTheTabAndSaysSoInGreen(OptionsTab tab, string said)
    {
        window.Show(tab);
        window.ChooseReset();

        var line = window.ChooseReset();

        AssertThat(line).IsEqual(new LogLine(said, (int)TextColour.Green));
        AssertThat(window.ConfirmingReset).IsFalse();
    }

    [TestCase(OptionsTab.Keybinds)]
    [TestCase(OptionsTab.General)]
    public void ChoosingATab_DropsTheConfirmation(OptionsTab tab)
    {
        window.ChooseReset();

        window.Show(tab);

        AssertThat(window.ConfirmingReset).IsFalse();
    }

    [TestCase]
    public void TogglingTheWindow_DropsTheConfirmation()
    {
        window.ChooseReset();

        window.Toggle();
        window.Toggle();

        AssertThat(window.ConfirmingReset).IsFalse();
    }

    [TestCase]
    public void ClosingTheWindow_DropsTheConfirmation()
    {
        window.ChooseReset();

        window.Close();

        AssertThat(window.ConfirmingReset).IsFalse();
    }
}
