using System;
using TheLastGate.Characters;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Hotbar;

public static class HotbarButtons
{
    public const int FirstInventoryAction = 100;
    public const int FirstEquipmentAction = 200;

    private const int SkillLimit = 60;

    private static readonly string[] GearNames =
    [
        "*Helmet", "*Neckla", "*Armor", "*Gloves", "*Belt", "*Tarot1", "*Boots", "*Offhan", "*Weapon", "*Cloak",
        "*L-Ring", "*R-Ring", "*Tarot2",
    ];

    public static string Name(int action, string storedName) =>
        action == CharacterSettings.Unassigned ? "-" : storedName;

    public static string ItemName(int action) =>
        action >= FirstEquipmentAction
            ? GearNames[action - FirstEquipmentAction]
            : $"Item {action - FirstInventoryAction}";

    public static int? GearKeyButton(int[] actions, WornSlot slot) =>
        ButtonFor(actions, FirstEquipmentAction + (int)slot);

    public static int? InventoryKeyButton(int[] actions, int slot) => ButtonFor(actions, FirstInventoryAction + slot);

    public static byte[]? UsePacket(int action, int selectedCharacter) => action switch
    {
        >= FirstEquipmentAction =>
            InventoryPackets.For(InventoryAction.UseGear, action - FirstEquipmentAction, selectedCharacter),
        >= FirstInventoryAction =>
            InventoryPackets.For(InventoryAction.UseItem, action - FirstInventoryAction, selectedCharacter),
        >= 0 and < SkillLimit => SkillPackets.Use(action, selectedCharacter),
        _ => null,
    };

    private static int? ButtonFor(int[] actions, int action) =>
        Array.LastIndexOf(actions, action) is var button and >= 0 ? button : null;
}
