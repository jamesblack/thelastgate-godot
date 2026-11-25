using Godot;
using TheLastGate.Hotbar;
using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Hud;

public partial class InventoryGrid : Control
{
    private readonly InventoryScroll scroll = new();

    private ScrollBar ScrollBar => GetNode<ScrollBar>("%ScrollBar");

    public override void _Ready()
    {
        ScrollBar.MaxValue = InventoryScroll.LastRow + 1;
        ScrollBar.ValueChanged += value => scroll.ScrollTo((int)value);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton click)
        {
            return;
        }

        AcceptEvent();
        if (click.Pressed && click.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            scroll.ScrollBy(click.ButtonIndex == MouseButton.WheelUp ? -1 : 1);
            ScrollBar.SetValueNoSignal(scroll.Row);
        }
    }

    public int SlotShownIn(int cell) => scroll.SlotShownIn(cell);

    public ItemSlotView Slot(int cell) => GetNode("%Cells").GetChild(cell).GetNode<ItemSlotView>("Slot");

    public void Show(PlayerState player, int[] hotbarActions)
    {
        for (var cell = 0; cell < InventoryScroll.VisibleSlots; cell++)
        {
            var slot = scroll.SlotShownIn(cell);
            var item = player.Inventory[slot];
            var slotView = Slot(cell);
            slotView.Show(item, ItemOverlays.Carried(item), false);
            slotView.ShowHotbarKey(HotbarButtons.InventoryKeyButton(hotbarActions, slot));
        }
    }
}
