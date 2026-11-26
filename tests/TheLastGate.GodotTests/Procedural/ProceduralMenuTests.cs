using GdUnit4;
using Godot;
using TheLastGate.GodotTests.Hud;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralMenuTests
{
    private const int Edit = 1;
    private const int Delete = 2;
    private const int ShowOnMap = 3;

    private List<int> chosen = [];
    private ProceduralMenu menu = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void MakeAMenu()
    {
        menu = Load();
        Tree.Root.AddChild(menu);
        chosen = [];
        menu.ItemChosen += chosen.Add;
    }

    [AfterTest]
    public void FreeTheMenu() => menu.Free();

    [TestCase]
    public void ClickItem_RaisesItemChosenAndCloses()
    {
        menu.AddItem("Edit", Edit);
        menu.Popup(new Vector2(10, 10));

        Release(menu, $"Item{Edit}");

        AssertThat(chosen).ContainsExactly(Edit);
        AssertThat(menu.IsOpen).IsFalse();
    }

    [TestCase]
    public void ClickDisabledItem_DoesNothing()
    {
        menu.AddItem("Delete...", Delete, false);
        menu.Popup(new Vector2(10, 10));

        Release(menu, $"Item{Delete}");

        AssertThat(chosen).IsEmpty();
        AssertThat(menu.IsOpen).IsTrue();
    }

    [TestCase]
    public void ClickCheckItem_WithKeepOpen_StaysOpenAndFlipsTheCheck()
    {
        menu.KeepOpenOnCheck = true;
        menu.AddCheckItem("Show on map", ShowOnMap, false);
        menu.Popup(new Vector2(10, 10));

        Release(menu, $"Item{ShowOnMap}");

        AssertThat(chosen).ContainsExactly(ShowOnMap);
        AssertThat(menu.IsOpen).IsTrue();
        AssertThat(menu.GetNode<Control>($"Frame/Items/Item{ShowOnMap}/Cells/Check/Mark").Visible).IsTrue();
    }

    [TestCase]
    public void ACheckItem_IsTheSameSizeCheckedOrNot()
    {
        menu.AddCheckItem("Show on map", ShowOnMap, false);
        menu.AddCheckItem("Show labels", Edit, true);

        AssertThat(Check(Edit).GetCombinedMinimumSize()).IsEqual(Check(ShowOnMap).GetCombinedMinimumSize());
    }

    [TestCase]
    public void Escape_Closes()
    {
        menu.AddItem("Edit", Edit);
        menu.Popup(new Vector2(10, 10));

        Keyboard.Press(Key.Escape);

        AssertThat(menu.IsOpen).IsFalse();
    }

    [TestCase]
    public void ClickOutside_ClosesAndIsConsumed()
    {
        var clicks = 0;
        var underneath = new Control { Position = new Vector2(300, 300), Size = new Vector2(100, 100) };
        underneath.GuiInput += _ => clicks++;
        Tree.Root.AddChild(underneath);
        menu.AddItem("Edit", Edit);
        menu.Popup(new Vector2(10, 10));

        Mouse.Click(new Vector2(350, 350));

        AssertThat(menu.IsOpen).IsFalse();
        AssertThat(clicks).IsEqual(0);
        underneath.Free();
    }

    [TestCase]
    public void ClickOutside_KeepsItsReleaseOffTheWorld()
    {
        menu.AddItem("Edit", Edit);
        menu.Popup(new Vector2(10, 10));

        AssertThat(ClickThrough.ReachesTheWorldAt(new Vector2(350, 350))).IsFalse();
        AssertThat(menu.IsOpen).IsFalse();
    }

    [TestCase]
    public void HoverSubmenuRow_OpensTheSubmenu()
    {
        var submenu = Load();
        menu.AddSubmenu("Colour", submenu);
        menu.Popup(new Vector2(10, 10));

        menu.GetNode<ClickArea>("Frame/Items/Submenu0/Click").EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(submenu.IsOpen).IsTrue();
    }

    [TestCase]
    public void Popup_NearTheEdge_StaysOnScreen()
    {
        var submenu = Load();
        submenu.AddItem("Use category colour", Edit);
        menu.AddItem("Edit", Edit);
        menu.AddSubmenu("Colour", submenu);
        var screen = menu.GetViewportRect();

        menu.Popup(screen.End - new Vector2(10, 10));
        menu.GetNode<ClickArea>("Frame/Items/Submenu0/Click").EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(screen.Encloses(menu.GetGlobalRect())).IsTrue();
        AssertThat(screen.Encloses(submenu.GetGlobalRect())).IsTrue();
    }

    [TestCase]
    public void Menu_DrawsAboveTheWindowPrompts()
    {
        var prompt = GD.Load<PackedScene>("res://Map/merge_prompt.tscn").Instantiate<Control>();

        AssertThat(menu.ZAsRelative).IsFalse();
        AssertThat(menu.ZIndex).IsGreater(prompt.ZIndex);
        AssertThat(prompt.ZAsRelative).IsFalse();
        prompt.Free();
    }

    private static ProceduralMenu Load() =>
        GD.Load<PackedScene>("res://Procedural/procedural_menu.tscn").Instantiate<ProceduralMenu>();

    private Control Check(int id) => menu.GetNode<Control>($"Frame/Items/Item{id}/Cells/Check");

    private static void Release(ProceduralMenu shown, string row) =>
        shown.GetNode<ClickArea>($"Frame/Items/{row}/Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
}
