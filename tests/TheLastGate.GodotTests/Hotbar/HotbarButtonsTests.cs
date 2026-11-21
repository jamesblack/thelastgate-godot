using GdUnit4;
using TheLastGate.Characters;
using TheLastGate.Hotbar;
using TheLastGate.Net;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hotbar;

[TestSuite]
public class HotbarButtonsTests
{
    [TestCase]
    public void Name_ShowsADashOnAnUnassignedButton()
    {
        AssertThat(HotbarButtons.Name(CharacterSettings.Unassigned, "Light")).IsEqual("-");
    }

    [TestCase]
    public void Name_ShowsTheStoredNameOfAnAssignedButton()
    {
        AssertThat(HotbarButtons.Name(50, "Light")).IsEqual("Light");
    }

    [TestCase(100, "Item 0")]
    [TestCase(105, "Item 5")]
    [TestCase(159, "Item 59")]
    public void ItemName_NamesInventorySlotsByNumber(int action, string name)
    {
        AssertThat(HotbarButtons.ItemName(action)).IsEqual(name);
    }

    [TestCase(200, "*Helmet")]
    [TestCase(207, "*Offhan")]
    [TestCase(208, "*Weapon")]
    [TestCase(212, "*Tarot2")]
    public void ItemName_NamesEquipmentSlotsByGear(int action, string name)
    {
        AssertThat(HotbarButtons.ItemName(action)).IsEqual(name);
    }

    [TestCase]
    public void GearKeyButton_FindsTheButtonThatUsesTheGear()
    {
        var actions = new CharacterSettings().HotbarActions;
        actions[3] = 208;

        AssertThat(HotbarButtons.GearKeyButton(actions, WornSlot.MainHand)).IsEqual(3);
        AssertThat(HotbarButtons.GearKeyButton(actions, WornSlot.Head)).IsNull();
    }

    [TestCase]
    public void GearKeyButton_FindsTheLastButtonWhenSeveralUseTheGear()
    {
        var actions = new CharacterSettings().HotbarActions;
        actions[3] = 208;
        actions[12] = 208;

        AssertThat(HotbarButtons.GearKeyButton(actions, WornSlot.MainHand)).IsEqual(12);
    }

    [TestCase]
    public void InventoryKeyButton_FindsTheButtonThatUsesTheInventorySlot()
    {
        var actions = new CharacterSettings().HotbarActions;
        actions[5] = 105;

        AssertThat(HotbarButtons.InventoryKeyButton(actions, 5)).IsEqual(5);
        AssertThat(HotbarButtons.InventoryKeyButton(actions, 6)).IsNull();
    }

    [TestCase]
    public void UsePacket_UsesASkillOnTheSelectedCharacter()
    {
        AssertThat(HotbarButtons.UsePacket(50, 345)).IsEqual(SkillPackets.Use(50, 345));
    }

    [TestCase]
    public void UsePacket_UsesAnInventoryItemWhereItIs()
    {
        AssertThat(HotbarButtons.UsePacket(103, 345)).IsEqual(InventoryPackets.For(InventoryAction.UseItem, 3, 345));
    }

    [TestCase]
    public void UsePacket_UsesAnEquippedItemWhereItIs()
    {
        AssertThat(HotbarButtons.UsePacket(208, 345)).IsEqual(InventoryPackets.For(InventoryAction.UseGear, 8, 345));
    }

    [TestCase(CharacterSettings.Unassigned)]
    [TestCase(60)]
    [TestCase(99)]
    public void UsePacket_SendsNothingForAnEmptyOrUnknownButton(int action)
    {
        AssertThat(HotbarButtons.UsePacket(action, 345)).IsNull();
    }
}
