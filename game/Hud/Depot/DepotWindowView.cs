using Godot;
using TheLastGate.Assets;
using TheLastGate.Items;
using TheLastGate.Net;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Depot;

// One page of the depot's 512 slots, with tabs for the eight pages.
// The scene is saved visible so it can be laid out in the editor; the game hides it until a depot opens.
[Tool]
public partial class DepotWindowView : Control
{
    private const int PreviewPage = 2;
    private const int NoPage = -1;

    private static readonly int[] PreviewSprites =
        [5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 16971, 16972, 16973];

    public ClickArea CloseButton => GetNode<ClickArea>("%CloseButton");

    public Control WheelArea => GetNode<Control>("%Frame");

    public ItemSlotView Cell(int cell) => GetNode<ItemSlotView>($"%Grid/Cell{cell}/Slot");

    public ClickArea PageButton(int page) => GetNode<ClickArea>($"%Tabs/Page{page}/Click");

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        Visible = false;
    }

    // The preview's current tab is set on this scene's own nodes, so it is cleared around editor saves.
    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            ShowPage(NoPage);
        }
        else if (what == NotificationEditorPostSave)
        {
            ShowPage(PreviewPage);
        }
    }

    public void ShowItems(GameState state, DepotPage page)
    {
        Visible = state.OpenShop == ShopKind.Depot;
        if (!Visible)
        {
            return;
        }

        var items = state.Shop.Depot[page.Current];
        for (var cell = 0; cell < DepotClicks.Cells; cell++)
        {
            var item = items[cell];
            Cell(cell).Show(new ItemSlot(item.Sprite, 0, 0, 0), ItemOverlays.Stored(item), false);
        }

        ShowPage(page.Current);
    }

    // The tab's click area is not a tool script, so the editor preview reaches the tab and its label past it.
    public void ShowPage(int current)
    {
        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            var selected = page == current;
            var tab = GetNode<ProceduralPanel>($"%Tabs/Page{page}");
            tab.Fill = selected ? ProceduralFill.Selected : ProceduralFill.Red;
            tab.GetNode<HudLabel>("Click/Label").Colour = selected ? TextColour.Yellow : TextColour.Grey;
        }
    }

    // The slots belong to the item cell scene, so the editor would not save sample sprites set on them anyway.
    private void PreviewInEditor()
    {
        for (var cell = 0; cell < PreviewSprites.Length; cell++)
        {
            Cell(cell).PreviewSprite = PreviewSprites[cell];
        }

        ShowPage(PreviewPage);
    }
}
