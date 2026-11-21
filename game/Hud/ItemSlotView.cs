using System;
using System.Collections.Generic;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hotkeys;
using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Hud;

[Tool]
public partial class ItemSlotView : ClickArea, IHudContextUser
{
    private const float HighlightBrightness = 2f;
    private const string TooltipScenePath = "res://Hud/item_tooltip.tscn";

    // Godot wraps a custom tooltip in a TooltipPanel under the control that owns it, so the slot's theme styles that
    // panel. The tooltip draws its own background and border.
    private static readonly Theme TooltipTheme = MakeTooltipTheme();
    private IFontSet? fonts;

    private int previewSprite;

    public ItemSlotView() => AddToGroup(HudContext.Group);

    [Export]
    public int PreviewSprite
    {
        get => previewSprite;
        set
        {
            previewSprite = value;
            ShowPreview();
        }
    }

    public Func<ItemTooltipRequest?>? TooltipRequest { get; set; }

    public void ShowContext(HudContext context) => fonts = context.Fonts;

    public override string _GetTooltip(Vector2 atPosition) => TooltipRequest?.Invoke() != null ? "item" : "";

    public override GodotObject? _MakeCustomTooltip(string forText)
    {
        if (TooltipRequest == null || this.fonts is not { } fonts)
        {
            return null;
        }

        var view = GD.Load<PackedScene>(TooltipScenePath).Instantiate<ItemTooltipView>();
        view.Watch(TooltipRequest, fonts);
        return view;
    }

    public override void _Ready()
    {
        Theme ??= TooltipTheme;
        MouseEntered += () => SetHighlighted(true);
        MouseExited += () => SetHighlighted(false);
        ShowPreview();
    }

    public void Show(ItemSlot item, IReadOnlyList<ItemOverlay> overlays, bool blocked)
    {
        GetNode<ArchiveSprite>("Highlighted/Item").SpriteNumber = item.Sprite;
        var overlayNodes = GetNode<Control>("Highlighted/Overlays");
        while (overlayNodes.GetChildCount() < overlays.Count)
        {
            overlayNodes.AddChild(new ArchiveSprite { MouseFilter = MouseFilterEnum.Ignore });
        }

        for (var index = 0; index < overlayNodes.GetChildCount(); index++)
        {
            var overlay = overlayNodes.GetChild<ArchiveSprite>(index);
            overlay.Visible = index < overlays.Count;
            if (overlay.Visible)
            {
                overlay.SpriteNumber = overlays[index].Sprite;
                overlay.Position = new Vector2(overlays[index].OffsetX, 0);
            }
        }

        GetNode<CanvasItem>("Blocked").Visible = blocked;
    }

    public void ShowHotbarKey(int? button)
    {
        var hotbarKey = GetNode<Label>("HotbarKey");
        hotbarKey.Visible = button != null;
        if (button is { } slot)
        {
            hotbarKey.Text = BindingRules.HotbarKeyName(slot);
        }
    }

    private static Theme MakeTooltipTheme()
    {
        var theme = new Theme();
        theme.SetStylebox("panel", "TooltipPanel", new StyleBoxEmpty());
        return theme;
    }

    public void SetHighlighted(bool highlighted)
    {
        var brightness = highlighted ? HighlightBrightness : 1f;
        GetNode<CanvasItem>("Highlighted").Modulate = new Color(brightness, brightness, brightness);
    }

    private void ShowPreview()
    {
        if (Engine.IsEditorHint() && IsInsideTree())
        {
            Show(new ItemSlot(previewSprite, 0, 0, 0), [], false);
        }
    }
}
