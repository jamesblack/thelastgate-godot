using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Items;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class InventoryGridTests
{
    private InventoryGrid grid = null!;

    [BeforeTest]
    public async Task ShowTheGrid() => grid = await HudScene.Show<InventoryGrid>("res://Hud/inventory_grid.tscn");

    [AfterTest]
    public void FreeTheGrid() => grid.Free();

    [TestCase]
    public void EveryVisibleCell_HasItsOwnSlot()
    {
        var slots = Enumerable.Range(0, InventoryScroll.VisibleSlots).Select(grid.Slot).ToList();

        AssertThat(slots.Distinct().Count()).IsEqual(InventoryScroll.VisibleSlots);
    }

    [TestCase]
    public void TheScrollBar_CoversEveryRow()
    {
        var bar = grid.GetNode<ScrollBar>("%ScrollBar");

        AssertThat(bar.MaxValue).IsEqual(InventoryScroll.LastRow + 1);
        AssertThat(bar.Size.X).IsEqual(7f);
    }

    [TestCase]
    public void TheWheel_ScrollsTheCellsAndMovesTheBar()
    {
        grid._GuiInput(Wheel(MouseButton.WheelDown));

        AssertThat(grid.SlotShownIn(0)).IsEqual(10);
        AssertThat(grid.GetNode<ScrollBar>("%ScrollBar").Value).IsEqual(1);
    }

    [TestCase]
    public void MovingTheBar_ScrollsTheCells()
    {
        grid.GetNode<ScrollBar>("%ScrollBar").Value = 2;

        AssertThat(grid.SlotShownIn(0)).IsEqual(20);
    }

    private static InputEventMouseButton Wheel(MouseButton button) => new() { ButtonIndex = button, Pressed = true };
}
