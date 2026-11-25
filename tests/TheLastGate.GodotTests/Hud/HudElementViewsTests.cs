using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;
using OptionButton = TheLastGate.Hud.OptionButton;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class HudElementViewsTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public async Task EveryWornSlot_HasItsOwnSlotView()
    {
        var view = await HudScene.Show<EquipmentView>("res://Hud/equipment_view.tscn");

        var slots = Enum.GetValues<WornSlot>().Select(view.Slot).ToList();

        AssertThat(slots.Distinct().Count()).IsEqual(Enum.GetValues<WornSlot>().Length);
        AssertThat(view.SwapSetButton.LightsUp).IsTrue();
        view.Free();
    }

    [TestCase]
    public async Task ASecondCharm_WidensTheCharmBox()
    {
        var view = await HudScene.Show<EquipmentView>("res://Hud/equipment_view.tscn");
        var box = view.GetNode<Control>("CharmBox");

        view.ShowSecondCharm(true);
        AssertThat(box.Size.X).IsEqual(62f);
        AssertThat(view.GetNode<Label>("%CharmCaption").Text).IsEqual("Cards");

        view.ShowSecondCharm(false);
        AssertThat(box.Size.X).IsEqual(36f);
        AssertThat(view.GetNode<Label>("%CharmCaption").Text).IsEqual("Card");
        view.Free();
    }

    [TestCase]
    public async Task ShowCharacter_WritesTheNameRankAndSprites()
    {
        var view = await HudScene.Show<CharacterView>("res://Hud/character_view.tscn");

        view.ShowCharacter("Worldofslaying", "Warlord", 10, 77, SpriteTint.Grey);

        AssertThat(view.GetNode<Label>("%PortraitName").Text).IsEqual("Worldofslaying");
        AssertThat(view.GetNode<Label>("%RankName").Text).IsEqual("Warlord");
        AssertThat(view.GetNode<ArchiveSprite>("%RankBadge").SpriteNumber).IsEqual(10);
        AssertThat(view.GetNode<ArchiveSprite>("%Portrait").SpriteNumber).IsEqual(77);
        var material = (ShaderMaterial)view.GetNode<CanvasItem>("%Portrait").Material;
        AssertThat(material.GetShaderParameter("grey").AsBool()).IsTrue();
        view.Free();
    }

    [TestCase]
    public async Task OwnName_HidesWhileLookingAtSomeoneElse()
    {
        var view = await HudScene.Show<CharacterView>("res://Hud/character_view.tscn");

        view.ShowOwnName("Worldofslaying");
        AssertThat(view.GetNode<CanvasItem>("%OwnName").Visible).IsTrue();

        view.ShowOwnName(null);
        AssertThat(view.GetNode<CanvasItem>("%OwnName").Visible).IsFalse();
        view.Free();
    }

    [TestCase]
    public async Task TheCompactQuickStats_DropHitAndParryAndShrink()
    {
        var view = await HudScene.Show<QuickStatsView>("res://Hud/quick_stats_view.tscn");
        var frame = view.GetNode<Control>("%Frame");

        view.ShowExpanded(false);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        AssertThat(view.GetNode<CanvasItem>("%HitRow").Visible).IsFalse();
        AssertThat(frame.Size).IsEqual(new Vector2(171, 53));

        view.ShowExpanded(true);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        AssertThat(frame.Size).IsEqual(new Vector2(171, 81));
        view.Free();
    }

    [TestCase]
    public async Task QuickStats_ShowTheNumbersAndRankProgress()
    {
        var view = await HudScene.Show<QuickStatsView>("res://Hud/quick_stats_view.tscn");

        view.Show(138, 94, 297, 298, 128913320, 0.25);

        AssertThat(view.GetNode<Label>("%WeaponValue").Text).IsEqual("138");
        AssertThat(view.GetNode<Label>("%ParryScore").Text).IsEqual("298");
        AssertThat(view.GetNode<Label>("%Experience").Text).IsEqual("128913320");
        AssertThat(view.GetNode<ProgressBar>("%ExperienceBar").Value).IsEqual(0.25);
        AssertThat(view.GetNode<Control>("%ExperienceBar").Size).IsEqual(new Vector2(165, 6));
        view.Free();
    }

    [TestCase]
    public async Task TheHotbar_HasTwentyButtonsInSlotOrder()
    {
        var view = await HudScene.Show<HotbarView>("res://Hud/hotbar_view.tscn");

        AssertThat(view.Buttons.Select(button => button.Slot).ToList()).ContainsExactly(Enumerable.Range(0, 20));
        AssertThat(view.Buttons.All(button => button.LightsUp)).IsTrue();
        view.Free();
    }

    [TestCase]
    public async Task TheQuickOptions_HaveEveryButtonButTheBlanks()
    {
        var view = await HudScene.Show<QuickOptionsView>("res://Hud/quick_options_view.tscn");

        var kinds = view.Buttons.Select(button => button.Kind).ToList();

        AssertThat(kinds)
            .ContainsExactly(
                Enum.GetValues<OptionButton>()
                    .Where(kind => kind is not (OptionButton.FirstBlank or OptionButton.SecondBlank)));
        view.Free();
    }

    [TestCase]
    public async Task AnOptionThatIsOn_IsFilledSelected()
    {
        var view = await HudScene.Show<QuickOptionsView>("res://Hud/quick_options_view.tscn");
        var fast = view.Buttons.First();

        fast.ShowMarked(true);
        AssertThat(fast.GetNode<ProceduralPanel>("Cell").Fill).IsEqual(ProceduralFill.Selected);

        fast.ShowMarked(false);
        AssertThat(fast.GetNode<ProceduralPanel>("Cell").Fill).IsEqual(ProceduralFill.None);
        view.Free();
    }

    [TestCase]
    public async Task TheMinimap_ShowsTheLocationAndPicture()
    {
        var view = await HudScene.Show<MinimapPanelView>("res://Hud/minimap_panel_view.tscn");
        var picture = new PlaceholderTexture2D();

        view.ShowLocation("Aston, Temple of Skua");
        view.ShowPicture(picture);

        AssertThat(view.GetNode<Label>("%Location").Text).IsEqual("Aston, Temple of Skua");
        AssertThat(view.GetNode<TextureRect>("%Picture").Texture).IsSame(picture);
        AssertThat(view.ZoomIn.LightsUp && view.ZoomOut.LightsUp).IsTrue();
        view.Free();
    }
}
