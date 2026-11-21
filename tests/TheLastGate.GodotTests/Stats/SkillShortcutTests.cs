using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class SkillShortcutTests
{
    private const int Dagger = 3;
    private const int Sword = 4;

    private const string ShortcutKeysHint =
        " Right-click on one of the shortcut keys in the bottom right to set a shortcut.";

    [TestCase]
    public void Toggle_SelectsTheSkillForAShortcut()
    {
        var shortcut = new SkillShortcut();

        var line = shortcut.Toggle(Dagger, "Dagger", 0);

        AssertThat(shortcut.Selected).IsEqual(Dagger);
        AssertThat(line).IsEqual(new LogLine("Dagger selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase]
    public void Toggle_OnTheSelectedSkillUnselectsIt()
    {
        var shortcut = new SkillShortcut();
        shortcut.Toggle(Dagger, "Dagger", 0);

        var line = shortcut.Toggle(Dagger, "Dagger", 0);

        AssertThat(shortcut.Selected).IsNull();
        AssertThat(line).IsEqual(new LogLine("Dagger no longer selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase]
    public void Toggle_OnAnotherSkillSelectsThatOne()
    {
        var shortcut = new SkillShortcut();
        shortcut.Toggle(Dagger, "Dagger", 0);

        var line = shortcut.Toggle(Sword, "Sword", 0);

        AssertThat(shortcut.Selected).IsEqual(Sword);
        AssertThat(line).IsEqual(new LogLine("Sword selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase(Kindred.Mercenary)]
    [TestCase(Kindred.Templar)]
    [TestCase(Kindred.Harakim)]
    public void Toggle_ExplainsTheShortcutKeysToMercenariesTemplarsAndHarakimTheFirstTime(uint kindred)
    {
        var shortcut = new SkillShortcut();

        var first = shortcut.Toggle(Dagger, "Dagger", kindred);
        shortcut.Toggle(Dagger, "Dagger", kindred);
        var second = shortcut.Toggle(Dagger, "Dagger", kindred);

        AssertThat(first)
            .IsEqual(new LogLine("Dagger selected for shortcut." + ShortcutKeysHint, (int)TextColour.Lime));
        AssertThat(second).IsEqual(new LogLine("Dagger selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase]
    public void Toggle_DoesNotExplainTheShortcutKeysToOtherClasses()
    {
        var line = new SkillShortcut().Toggle(Dagger, "Dagger", Kindred.SeyanDu);

        AssertThat(line).IsEqual(new LogLine("Dagger selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase(MapButton.Right, MapKeys.Ctrl, true)]
    [TestCase(MapButton.Right, MapKeys.Alt, true)]
    [TestCase(MapButton.Right, MapKeys.Ctrl | MapKeys.Shift, true)]
    [TestCase(MapButton.Right, MapKeys.None, false)]
    [TestCase(MapButton.Right, MapKeys.Shift, false)]
    [TestCase(MapButton.Left, MapKeys.Ctrl, false)]
    public void SelectsItem_IsARightClickWithControlOrAlt(MapButton button, MapKeys keys, bool selects)
    {
        AssertThat(SkillShortcut.SelectsItem(button, keys)).IsEqual(selects);
    }

    [TestCase]
    public void ToggleInventory_SelectsTheSlotAndExplainsTheShortcutKeysTheFirstTime()
    {
        var shortcut = new SkillShortcut();

        var first = shortcut.ToggleInventory(5);

        AssertThat(shortcut.Selected).IsEqual(105);
        var text = "Inventory slot 5 selected for shortcut." + ShortcutKeysHint;
        AssertThat(first).IsEqual(new LogLine(text, (int)TextColour.Lime));
    }

    [TestCase]
    public void ToggleInventory_OnTheSelectedSlotUnselectsItAndThenSelectsItWithoutTheHint()
    {
        var shortcut = new SkillShortcut();
        shortcut.ToggleInventory(5);

        var unselected = shortcut.ToggleInventory(5);
        var selected = shortcut.ToggleInventory(5);

        var unselectedText = "Inventory slot 5 no longer selected for shortcut.";
        AssertThat(unselected).IsEqual(new LogLine(unselectedText, (int)TextColour.Lime));
        AssertThat(selected).IsEqual(new LogLine("Inventory slot 5 selected for shortcut.", (int)TextColour.Lime));
    }

    [TestCase]
    public void ToggleEquipment_SelectsTheGearSlotAndExplainsTheShortcutKeysTheFirstTime()
    {
        var shortcut = new SkillShortcut();

        var first = shortcut.ToggleEquipment(WornSlot.Head);

        AssertThat(shortcut.Selected).IsEqual(200);
        var text = "*Helmet equipment slot selected for shortcut." + ShortcutKeysHint;
        AssertThat(first).IsEqual(new LogLine(text, (int)TextColour.Lime));
    }

    [TestCase]
    public void ToggleEquipment_OnTheSelectedSlotUnselectsIt()
    {
        var shortcut = new SkillShortcut();
        shortcut.ToggleEquipment(WornSlot.MainHand);

        var line = shortcut.ToggleEquipment(WornSlot.MainHand);

        AssertThat(shortcut.Selected).IsNull();
        var text = "*Weapon equipment slot no longer selected for shortcut.";
        AssertThat(line).IsEqual(new LogLine(text, (int)TextColour.Lime));
    }

    [TestCase]
    public void Toggles_ExplainTheShortcutKeysOnceForSkillsInventoryAndEquipmentEach()
    {
        var shortcut = new SkillShortcut();
        shortcut.Toggle(Dagger, "Dagger", Kindred.Mercenary);
        shortcut.ToggleInventory(5);

        var line = shortcut.ToggleEquipment(WornSlot.Head);

        AssertThat(line.Text).EndsWith(ShortcutKeysHint);
    }

    [TestCase]
    public void Toggle_OnASkillReplacesASelectedItem()
    {
        var shortcut = new SkillShortcut();
        shortcut.ToggleInventory(5);

        shortcut.Toggle(Dagger, "Dagger", 0);

        AssertThat(shortcut.Selected).IsEqual(Dagger);
    }
}
