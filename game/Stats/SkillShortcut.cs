using TheLastGate.Assets;
using TheLastGate.Hotbar;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Stats;

public sealed class SkillShortcut
{
    private const uint ClassesWithShortcutKeys = Kindred.Mercenary | Kindred.Templar | Kindred.Harakim;

    private const string ShortcutKeysHint =
        " Right-click on one of the shortcut keys in the bottom right to set a shortcut.";

    private bool explainedEquipment;
    private bool explainedInventory;
    private bool explainedSkills;

    public int? Selected { get; private set; }

    public static bool SelectsItem(MapButton button, MapKeys keys) =>
        button == MapButton.Right && (keys & (MapKeys.Ctrl | MapKeys.Alt)) != 0;

    public LogLine Toggle(int skillNumber, string name, uint kindred) =>
        Toggle(skillNumber, name, (kindred & ClassesWithShortcutKeys) != 0, ref explainedSkills);

    public LogLine ToggleInventory(int slot) =>
        Toggle(HotbarButtons.FirstInventoryAction + slot, $"Inventory slot {slot}", true, ref explainedInventory);

    public LogLine ToggleEquipment(WornSlot slot)
    {
        var action = HotbarButtons.FirstEquipmentAction + (int)slot;
        return Toggle(action, $"{HotbarButtons.ItemName(action)} equipment slot", true, ref explainedEquipment);
    }

    private LogLine Toggle(int action, string name, bool explains, ref bool explained)
    {
        if (Selected == action)
        {
            Selected = null;
            return Line($"{name} no longer selected for shortcut.");
        }

        Selected = action;
        var selected = $"{name} selected for shortcut.";
        if (explained || !explains)
        {
            return Line(selected);
        }

        explained = true;
        return Line(selected + ShortcutKeysHint);
    }

    private static LogLine Line(string text) => new(text, (int)TextColour.Lime);
}
