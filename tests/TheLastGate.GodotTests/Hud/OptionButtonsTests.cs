using GdUnit4;
using TheLastGate.Characters;
using TheLastGate.Hud;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
public class OptionButtonsTests
{
    [TestCase(OptionButton.Fast, SpeedMode.Fast)]
    [TestCase(OptionButton.Normal, SpeedMode.Normal)]
    [TestCase(OptionButton.Slow, SpeedMode.Slow)]
    public void Packet_ChangesTheMovementMode(OptionButton button, SpeedMode mode)
    {
        AssertThat(OptionButtons.Packet(button)).IsEqual(ModePackets.For(mode));
    }

    [TestCase(OptionButton.HealthPercent)]
    [TestCase(OptionButton.Names)]
    [TestCase(OptionButton.Options)]
    [TestCase(OptionButton.Exit)]
    public void Packet_SendsNothingForOtherButtons(OptionButton button)
    {
        AssertThat(OptionButtons.Packet(button)).IsNull();
    }

    [TestCase]
    public void Toggle_FlipsEachButtonsSetting()
    {
        var settings = new CharacterSettings
        {
            ShowHealthPercent = false,
            ShowStatBase = false,
            WallHiding = WallHiding.Off,
            ShowNames = false,
            ShowHealthBars = false,
        };

        AssertThat(OptionButtons.Toggle(OptionButton.HealthPercent, settings)).IsTrue();
        AssertThat(OptionButtons.Toggle(OptionButton.StatBase, settings)).IsTrue();
        AssertThat(OptionButtons.Toggle(OptionButton.HideWalls, settings)).IsTrue();
        AssertThat(OptionButtons.Toggle(OptionButton.Names, settings)).IsTrue();
        AssertThat(OptionButtons.Toggle(OptionButton.HealthBars, settings)).IsTrue();

        AssertThat(settings.ShowHealthPercent).IsTrue();
        AssertThat(settings.ShowStatBase).IsTrue();
        AssertThat(settings.WallHiding).IsEqual(WallHiding.Near);
        AssertThat(settings.ShowNames).IsTrue();
        AssertThat(settings.ShowHealthBars).IsTrue();

        OptionButtons.Toggle(OptionButton.Names, settings);
        AssertThat(settings.ShowNames).IsFalse();
    }

    [TestCase]
    public void Toggle_CyclesWallHiding()
    {
        var settings = new CharacterSettings { WallHiding = WallHiding.Off };
        var seen = new List<WallHiding>();

        for (var press = 0; press < 3; press++)
        {
            OptionButtons.Toggle(OptionButton.HideWalls, settings);
            seen.Add(settings.WallHiding);
        }

        AssertThat(seen).ContainsExactly(WallHiding.Near, WallHiding.All, WallHiding.Off);
    }

    [TestCase(WallHiding.Off, false)]
    [TestCase(WallHiding.Near, true)]
    [TestCase(WallHiding.All, true)]
    public void IsMarked_MarksHideForNearAndAll(WallHiding wallHiding, bool marked)
    {
        var settings = new CharacterSettings { WallHiding = wallHiding };

        AssertThat(OptionButtons.IsMarked(OptionButton.HideWalls, settings, 1)).IsEqual(marked);
    }

    [TestCase(OptionButton.HideWalls, WallHiding.All, true)]
    [TestCase(OptionButton.HideWalls, WallHiding.Near, false)]
    [TestCase(OptionButton.HideWalls, WallHiding.Off, false)]
    [TestCase(OptionButton.Names, WallHiding.All, false)]
    [TestCase(OptionButton.StatBase, WallHiding.All, false)]
    [TestCase(OptionButton.Fast, WallHiding.All, false)]
    public void IsDoubleMarked_OnlyMarksHideTwiceWhenHidingAll(
        OptionButton button,
        WallHiding wallHiding,
        bool doubleMarked)
    {
        var settings = new CharacterSettings { WallHiding = wallHiding };

        AssertThat(OptionButtons.IsDoubleMarked(button, settings)).IsEqual(doubleMarked);
    }

    [TestCase(OptionButton.Fast)]
    [TestCase(OptionButton.Options)]
    [TestCase(OptionButton.Exit)]
    public void Toggle_LeavesSettingsAloneForOtherButtons(OptionButton button)
    {
        var settings = new CharacterSettings();

        AssertThat(OptionButtons.Toggle(button, settings)).IsFalse();
        AssertThat(settings.WallHiding).IsEqual(WallHiding.Near);
        AssertThat(settings.ShowNames).IsTrue();
    }

    [TestCase(2, OptionButton.Fast)]
    [TestCase(1, OptionButton.Normal)]
    [TestCase(0, OptionButton.Slow)]
    public void IsMarked_MarksTheCurrentMode(int mode, OptionButton marked)
    {
        var settings = new CharacterSettings();

        foreach (var button in new[] { OptionButton.Fast, OptionButton.Normal, OptionButton.Slow })
        {
            AssertThat(OptionButtons.IsMarked(button, settings, mode)).IsEqual(button == marked);
        }
    }

    [TestCase]
    public void IsMarked_MarksEachToggleThatIsOn()
    {
        var settings = new CharacterSettings
        {
            ShowHealthPercent = true,
            ShowStatBase = false,
            WallHiding = WallHiding.Near,
            ShowNames = false,
            ShowHealthBars = true,
        };

        AssertThat(OptionButtons.IsMarked(OptionButton.HealthPercent, settings, 1)).IsTrue();
        AssertThat(OptionButtons.IsMarked(OptionButton.StatBase, settings, 1)).IsFalse();
        AssertThat(OptionButtons.IsMarked(OptionButton.HideWalls, settings, 1)).IsTrue();
        AssertThat(OptionButtons.IsMarked(OptionButton.Names, settings, 1)).IsFalse();
        AssertThat(OptionButtons.IsMarked(OptionButton.HealthBars, settings, 1)).IsTrue();
        AssertThat(OptionButtons.IsMarked(OptionButton.Options, settings, 1)).IsFalse();
        AssertThat(OptionButtons.IsMarked(OptionButton.Exit, settings, 1)).IsFalse();
    }

    [TestCase(
        OptionButton.Fast,
        "F1/FAST: Makes you move faster, but uses more endurance. You will also be seen more easily.")]
    [TestCase(OptionButton.Normal, "F2/NORMAL: Move at normal speed. Takes no endurance when not fighting.")]
    [TestCase(
        OptionButton.Slow,
        "F3/SLOW: Makes you move slower. You regain endurance while not fighting. Decreases chances of being seen.")]
    [TestCase(
        OptionButton.HealthPercent,
        "F4/HEALTH: Toggle whether the current health of all characters is displayed as a percent.")]
    [TestCase(
        OptionButton.StatBase,
        "F5/STATS: Toggle seeing stat bases on the skill list, displayed as blue numbers.")]
    [TestCase(
        OptionButton.HideWalls,
        "F6/HIDE: Hide walls and other high objects near you; press again to hide them everywhere, and again to show them.")]
    [TestCase(OptionButton.Names, "F7/NAMES: Toggle whether the name of all characters is displayed.")]
    [TestCase(
        OptionButton.HealthBars,
        "F8/BARS: Toggle whether the current health of all characters is displayed as bars.")]
    [TestCase(
        OptionButton.Exit,
        "F12/EXIT: Leave the game immediately. Will cost you 50% health and can result in death.")]
    public void Help_GivesTheCurrentClientsText(OptionButton button, string help)
    {
        AssertThat(OptionButtons.Help(button)).IsEqual(help);
    }

    [TestCase(OptionButton.Options)]
    [TestCase(OptionButton.FirstBlank)]
    [TestCase(OptionButton.SecondBlank)]
    public void Help_HasNothingForTheOtherButtons(OptionButton button)
    {
        AssertThat(OptionButtons.Help(button)).IsNull();
    }
}
