using Godot;
using TheLastGate.State;

namespace TheLastGate.Hud;

public partial class EquipmentView : Control
{
    private const float OneCardWidth = 38;
    private const float TwoCardsWidth = 62;

    public ClickArea SwapSetButton => GetNode<ClickArea>("%SwapSetButton");

    public ItemSlotView Slot(WornSlot slot) => GetNode<ItemSlotView>("%" + slot);

    public void ShowSecondCharm(bool worn)
    {
        var box = GetNode<Control>("CharmBox");
        box.Size = box.Size with { X = worn ? TwoCardsWidth : OneCardWidth };
        GetNode<Label>("%CharmCaption").Text = worn ? "Cards" : "Card";
    }
}
