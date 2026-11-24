using GdUnit4;
using TheLastGate.Cursors;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class ShopClicksTests
{
    private const int ShopNumber = 1201;

    [TestCase(MapButton.Left, MapKeys.None, 5)]
    [TestCase(MapButton.Left, MapKeys.Ctrl, 129)]
    [TestCase(MapButton.Left, MapKeys.Alt, 129)]
    [TestCase(MapButton.Left, MapKeys.Shift, 5)]
    [TestCase(MapButton.Right, MapKeys.None, 67)]
    [TestCase(MapButton.Right, MapKeys.Ctrl, 67)]
    public void Packet_BuysTenWithControlOrAltAndDescribesOnARightClick(MapButton button, MapKeys keys, int value)
    {
        AssertThat(ShopClicks.Packet(button, keys, ShopNumber, 5))
            .IsEqual(ClientPackets.ShortAndInt(ClientCommand.Shop, ShopNumber, value));
    }

    [TestCase]
    public void Packet_SendsNothingForOtherButtons()
    {
        AssertThat(ShopClicks.Packet(MapButton.None, MapKeys.None, ShopNumber, 5)).IsNull();
    }

    [TestCase(true, false, MouseCursor.Take)]
    [TestCase(true, true, MouseCursor.Take)]
    [TestCase(false, true, MouseCursor.Drop)]
    [TestCase(false, false, MouseCursor.None)]
    public void Cursor_ShowsTakeOverAnItemAndDropWhenHoldingOne(bool slotHasItem, bool holding, MouseCursor cursor)
    {
        AssertThat(ShopClicks.Cursor(slotHasItem, holding)).IsEqual(cursor);
    }

    [TestCase(ShopKind.Merchant, 12345, false, "   Buy for:       123G 45S")]
    [TestCase(ShopKind.Merchant, 12345, true, "Buy 10 for:      1234G 50S")]
    [TestCase(ShopKind.BlackStronghold, 500, false, "    Take reward for:       500 Stronghold Pts")]
    [TestCase(ShopKind.Casino, 75, false, "Take reward for:        75 Tokens")]
    [TestCase(ShopKind.Contract, 1200, false, "  Take reward for:      1200 Contract Pts")]
    [TestCase(ShopKind.Experience, 999999, false, "         Buy for:    999999 Exp")]
    public void Price_WritesWhatTheHoveredItemCosts(ShopKind kind, int price, bool buyingTen, string text)
    {
        AssertThat(ShopText.Price(kind, price, buyingTen)).IsEqual(text);
    }

    [TestCase(ShopKind.BlackStronghold)]
    [TestCase(ShopKind.Experience)]
    public void Price_BuysOnlyOneOutsideGoldShops(ShopKind kind)
    {
        AssertThat(ShopText.Price(kind, 500, true) == ShopText.Price(kind, 500, false)).IsTrue();
    }

    [TestCase]
    public void Price_ShowsNothingForAFreeItem()
    {
        AssertThat(ShopText.Price(ShopKind.Merchant, 0, false)).IsNull();
    }

    [TestCase]
    public void Sell_WritesWhatTheMerchantPaysForTheHeldItem()
    {
        AssertThat(ShopText.Sell(ShopKind.Merchant, 2500)).IsEqual("  Sell for:        25G  0S");
    }

    [TestCase(ShopKind.Merchant, 0)]
    [TestCase(ShopKind.BlackStronghold, 2500)]
    public void Sell_ShowsNothingWithoutAGoldPrice(ShopKind kind, int heldItemPrice)
    {
        AssertThat(ShopText.Sell(kind, heldItemPrice)).IsNull();
    }

    [TestCase(ShopKind.Merchant, "Your Money:     12345G 67S")]
    [TestCase(ShopKind.BlackStronghold, "Your Stronghold Pts:       176")]
    [TestCase(ShopKind.Casino, "    Your Tokens:      4896")]
    [TestCase(ShopKind.Contract, "Your Contract Pts:     20018")]
    [TestCase(ShopKind.Experience, "Your Unspent Exp:       187")]
    public void Balance_ShowsWhatTheShopTakes(ShopKind kind, string text)
    {
        var balances = new ShopBalances(1234567, 176, 4896, 20018, 187);

        AssertThat(ShopText.Balance(kind, [new ShopItem(1000, 10, false, false, 0)], balances)).IsEqual(text);
    }

    [TestCase]
    public void Balance_ShowsNothingWhenNothingHasAPrice()
    {
        var balances = new ShopBalances(1234567, 176, 4896, 20018, 187);

        AssertThat(ShopText.Balance(ShopKind.Merchant, [new ShopItem(1000, 0, false, false, 0)], balances)).IsNull();
    }
}
