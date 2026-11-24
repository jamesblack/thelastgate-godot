using Godot;
using TheLastGate.Items;
using TheLastGate.State;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Shop;

// A merchant's or a reward shop's items, or a corpse's loot.
// The scene is saved visible so it can be laid out in the editor; the game hides it until a shop opens.
[Tool]
public partial class ShopWindowView : Control
{
    private static readonly int[] PreviewSprites =
        [5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 16971, 16972, 16973];

    public ClickArea CloseButton => GetNode<ClickArea>("%CloseButton");

    public static bool Shows(ShopKind? kind) =>
        kind is ShopKind.Merchant or ShopKind.BlackStronghold or ShopKind.Casino or ShopKind.Contract
            or ShopKind.Experience;

    public ItemSlotView Cell(int cell) => GetNode<ItemSlotView>($"%Grid/Cell{cell}/Slot");

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        Visible = false;
    }

    public void ShowItems(GameState state)
    {
        Visible = Shows(state.OpenShop);
        if (!Visible)
        {
            return;
        }

        var items = state.Shop.ShopItems;
        for (var cell = 0; cell < ShopClicks.Slots; cell++)
        {
            var item = cell < items.Length ? items[cell] : default;
            Cell(cell).Show(new ItemSlot(item.Sprite, 0, 0, 0), ItemOverlays.Shop(item), false);
        }
    }

    // The price follows the mouse and the held keys, so it is updated every frame.
    public void ShowText(GameState state, int? hoveredCell, bool buyingTen)
    {
        if (!Visible || state.OpenShop is not { } kind)
        {
            return;
        }

        var items = state.Shop.ShopItems;
        var player = state.Player;
        string? price = null;
        if (player.HeldItem.Sprite != 0)
        {
            price = ShopText.Sell(kind, state.Shop.HeldItemPrice);
        }
        else if (hoveredCell is { } slot && slot < items.Length)
        {
            price = ShopText.Price(kind, items[slot].Price, buyingTen);
        }

        var balances = new ShopBalances(
            player.Gold,
            player.BlackStrongholdPoints,
            player.Tokens,
            player.OsirisPoints,
            player.Points);
        GetNode<Label>("%Price").Text = price ?? "";
        GetNode<Label>("%Balance").Text = ShopText.Balance(kind, items, balances) ?? "";
    }

    // The slots belong to the item cell scene, so the editor would not save sample sprites set on them anyway.
    private void PreviewInEditor()
    {
        for (var cell = 0; cell < PreviewSprites.Length; cell++)
        {
            Cell(cell).PreviewSprite = PreviewSprites[cell];
        }
    }
}
