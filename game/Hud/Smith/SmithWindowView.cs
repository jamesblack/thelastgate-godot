using System.Collections.Generic;
using Godot;
using TheLastGate.Items;
using TheLastGate.State;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Smith;

// The scene is saved visible so it can be laid out in the editor; the game hides it until a smith opens.
[Tool]
public partial class SmithWindowView : Control
{
    private const int MiddleButton = 5;
    private const int SecondMaterial = 2;

    private static readonly string[] WeaponWhetstoneButtons = ["Grind", "Hone", "Sharpen"];
    private static readonly string[] ArmourWhetstoneButtons = ["Smooth", "Polish", "Thicken"];
    private static readonly int[] PreviewSprites = [5000, 0, 0, 5001];

    public ClickArea CloseButton => GetNode<ClickArea>("%CloseButton");

    public ItemSlotView Slot(int slot) => GetNode<ItemSlotView>($"%Slot{slot}/Slot");

    public ClickArea Button(int number) => GetNode<ClickArea>($"%Button{number}/Click");

    public override void _Ready()
    {
        ShowNormalLayout();
        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        Visible = false;
    }

    // The preview's captions and layout are set on this scene's own nodes, so they are reset around editor saves.
    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            ShowNormalLayout();
            ShowCaptions(new int[SmithClicks.Slots]);
        }
        else if (what == NotificationEditorPostSave)
        {
            PreviewInEditor();
        }
    }

    public void ShowItems(GameState state)
    {
        Visible = state.OpenShop is ShopKind.WeaponSmith or ShopKind.ArmourSmith;
        if (!Visible || state.OpenShop is not { } smith)
        {
            return;
        }

        var items = state.Player.SmithItems;
        ShowLayout(SmithClicks.UsesTwoMaterials(items), SmithClicks.ShowsOuterButtons(items), smith);
        ShowSlots(items);
    }

    public void ShowLayout(bool twoMaterials, bool outerButtons, ShopKind smith)
    {
        GetNode<CanvasItem>($"%Slot{SecondMaterial}").Visible = twoMaterials;
        GetNode<CanvasItem>("%And").Visible = twoMaterials;
        var labels = smith == ShopKind.WeaponSmith ? WeaponWhetstoneButtons : ArmourWhetstoneButtons;
        for (var number = SmithClicks.FirstButton; number <= SmithClicks.LastButton; number++)
        {
            var button = GetNode<CanvasItem>($"%Button{number}");
            button.Visible = outerButtons || number == MiddleButton;
            button.GetNode<Label>("Label").Text = outerButtons ? labels[number - SmithClicks.FirstButton] : "Smith!";
        }
    }

    public void ShowSlots(IReadOnlyList<ItemSlot> items)
    {
        var sprites = new int[SmithClicks.Slots];
        for (var slot = 0; slot < SmithClicks.Slots; slot++)
        {
            var item = items[slot];
            var overlays = ItemOverlays.Stored(new DepotItem(item.Sprite, item.Stack, item.Flags, 0));
            Slot(slot).Show(item with { Properties = 0 }, overlays, false);
            sprites[slot] = item.Sprite;
        }

        ShowCaptions(sprites);
    }

    private void ShowNormalLayout() => ShowLayout(false, false, ShopKind.WeaponSmith);

    private void ShowCaptions(IReadOnlyList<int> sprites)
    {
        for (var slot = 0; slot < SmithClicks.Slots; slot++)
        {
            GetNode<CanvasItem>($"%Slot{slot}/Caption").Visible = sprites[slot] == 0;
        }
    }

    // The slots belong to the item cell scene, so the editor would not save sample sprites set on them anyway.
    private void PreviewInEditor()
    {
        for (var slot = 0; slot < SmithClicks.Slots; slot++)
        {
            Slot(slot).PreviewSprite = PreviewSprites[slot];
        }

        ShowCaptions(PreviewSprites);
    }
}
