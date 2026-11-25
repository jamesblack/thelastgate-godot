using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

// The procedural HUD replaced art that was measured pixel by pixel, so each element and each slot must stay where the
// art drew it.
[TestSuite]
[RequireGodotRuntime]
public class HudLayoutTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;
    private const int BackgroundSprite = 1;
    private const int DarkBackgroundSprite = 18001;

    private HudView hud = null!;
    private SpriteLoader sprites = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheHud()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        var state = new GameState();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.Show(state, new GameSession(state), new ClassicFonts(), _ => { });
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void CloseTheHud()
    {
        hud.Free();
        sprites.Dispose();
    }

    [TestCase("Inventory", 258, 3, 357, 106)]
    [TestCase("Currencies", 260, 109, 170, 60)]
    [TestCase("MoneyButtons", 431, 109, 134, 32)]
    [TestCase("Attributes", 3, 3, 232, 102)]
    [TestCase("Skills", 3, 115, 245, 158)]
    [TestCase("Pools", 3, 283, 159, 46)]
    [TestCase("StatModeButtons", 258, 179, 71, 49)]
    [TestCase("Equipment", 668, 3, 174, 140)]
    [TestCase("Character", 842, 3, 130, 189)]
    [TestCase("Chat", 972, 3, 305, 243)]
    [TestCase("QuickStats", 1106, 256, 171, 81)]
    [TestCase("Hotbar", 1033, 594, 244, 64)]
    [TestCase("OptionButtons", 1081, 664, 196, 49)]
    [TestCase("MinimapPanel", 3, 565, 152, 148)]
    [TestCase("TreeButton", 339, 179, 32, 32)]
    public void EachElement_CoversTheRectTheArtDrewItIn(string element, int left, int top, int width, int height)
    {
        var control = hud.GetNode<Control>("%" + element);

        AssertThat(control.Position).IsEqual(new Vector2(left, top));
        AssertThat(control.Size).IsEqual(new Vector2(width, height));
    }

    [TestCase]
    public void InventorySlots_SitWhereTheArtsCellsWere()
    {
        var inventory = hud.GetNode<InventoryGrid>("%Inventory");

        AssertThat(inventory.Slot(0).GlobalPosition).IsEqual(new Vector2(261, 6));
        AssertThat(inventory.Slot(9).GlobalPosition).IsEqual(new Vector2(567, 6));
        AssertThat(inventory.Slot(29).GlobalPosition).IsEqual(new Vector2(567, 74));
    }

    [TestCase(WornSlot.Head, 739, 6)]
    [TestCase(WornSlot.Neck, 701, 19)]
    [TestCase(WornSlot.Body, 739, 40)]
    [TestCase(WornSlot.Arms, 705, 57)]
    [TestCase(WornSlot.Belt, 739, 74)]
    [TestCase(WornSlot.Charm, 778, 18)]
    [TestCase(WornSlot.Feet, 739, 108)]
    [TestCase(WornSlot.OffHand, 807, 57)]
    [TestCase(WornSlot.MainHand, 671, 57)]
    [TestCase(WornSlot.Cloak, 773, 57)]
    [TestCase(WornSlot.LeftRing, 777, 95)]
    [TestCase(WornSlot.RightRing, 701, 95)]
    [TestCase(WornSlot.SecondCharm, 802, 18)]
    public void EquipmentSlots_SitWhereTheArtsBoxesWere(WornSlot slot, int left, int top)
    {
        var view = hud.GetNode<EquipmentView>("%Equipment").Slot(slot);

        AssertThat(view.GlobalPosition).IsEqual(new Vector2(left, top));
        AssertThat(view.Size).IsEqual(new Vector2(32, 32));
    }

    [TestCase]
    public void TheMinimapAndItsZoomButtons_KeepTheirPlaces()
    {
        var minimap = hud.GetNode<MinimapPanelView>("%MinimapPanel");

        AssertThat(minimap.Map.GlobalPosition).IsEqual(new Vector2(6, 582));
        AssertThat(minimap.Map.Size).IsEqual(new Vector2(128, 128));
        AssertThat(minimap.ZoomIn.GlobalPosition).IsEqual(new Vector2(135, 581));
        AssertThat(minimap.ZoomOut.GlobalPosition).IsEqual(new Vector2(135, 599));
    }

    [TestCase]
    public void TheChatLogAndLine_KeepTheirPlaces()
    {
        var chat = hud.GetNode<ChatView>("%Chat");

        AssertThat(chat.Log.GlobalPosition).IsEqual(new Vector2(974, 7));
        AssertThat(chat.Line.GlobalPosition).IsEqual(new Vector2(977, 233));
    }

    [TestCase]
    public void ThePortraitBars_KeepTheirPlaces()
    {
        var character = hud.GetNode<CharacterView>("%Character");

        AssertThat(character.HitPointBar.GlobalPosition).IsEqual(new Vector2(845, 131));
        AssertThat(character.EnduranceBar.GlobalPosition).IsEqual(new Vector2(845, 138));
        AssertThat(character.ManaBar.GlobalPosition).IsEqual(new Vector2(845, 145));
    }

    [TestCase]
    public void HotbarAndOptionButtons_StartInsideTheirFrames()
    {
        var hotbar = hud.GetNode<HotbarView>("%Hotbar").Buttons.ToList();
        var options = hud.GetNode<QuickOptionsView>("%OptionButtons").Buttons.ToList();

        AssertThat(hotbar[0].GlobalPosition).IsEqual(new Vector2(1035, 596));
        AssertThat(hotbar[19].GlobalPosition).IsEqual(new Vector2(1227, 641));
        AssertThat(options[0].GlobalPosition).IsEqual(new Vector2(1083, 666));
    }

    [TestCase("DiamondTopLeft", 271, 264)]
    [TestCase("DiamondTopRight", 965, 264)]
    [TestCase("DiamondBottomLeft", 271, 610)]
    [TestCase("DiamondBottomRight", 965, 610)]
    public void TheAutoWalkSquares_SitWhereTheArtDrewThem(string square, int left, int top)
    {
        var tile = hud.GetNode<DiamondTile>(square);

        AssertThat(tile.Position).IsEqual(new Vector2(left, top));
        AssertThat(tile.Size).IsEqual(new Vector2(44, 21));
    }

    [TestCase]
    public void NothingDrawsTheOldBackgroundSprite()
    {
        var sprites = hud.FindChildren("*", "", true, false).OfType<ArchiveSprite>();

        AssertThat(sprites.Any(sprite => sprite.SpriteNumber is BackgroundSprite or DarkBackgroundSprite)).IsFalse();
    }

    [TestCase]
    public void TheBackground_CutsOutTheWorldsWindow()
    {
        var background = hud.GetNode<Control>("Background");

        AssertThat(background.Size).IsEqual(new Vector2(1280, 720));
        AssertThat(((ProceduralPanel)background).Hole)
            .IsEqual(new Rect2(0, 104, 1280, 687));
    }
}
