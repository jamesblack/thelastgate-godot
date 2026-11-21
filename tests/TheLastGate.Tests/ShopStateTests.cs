using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class ShopStateTests
{
    private const int ShopNumber = 1201;
    private readonly TickProcessor processor;

    private readonly GameState state = new();

    public ShopStateTests() => processor = new TickProcessor(state);

    [Fact]
    public void CloseShop_ClosesTheShopWithoutSayingTheServerDid()
    {
        OpenShop();
        var serverCloses = 0;
        state.ShopClosedByServer += () => serverCloses++;

        Assert.True(state.CloseShop());

        Assert.Null(state.OpenShop);
        Assert.Equal(0, serverCloses);
    }

    [Fact]
    public void CloseShop_SaysWhenNoShopWasOpen()
    {
        Assert.False(state.CloseShop());
    }

    [Fact]
    public void ShopRefresh_AsksForTheOpenShopEveryNinthTick()
    {
        var refresh = new ShopRefresh();
        var sent = new List<byte[]>();
        refresh.Send = sent.Add;

        for (var tick = 0; tick < 8; tick++)
        {
            refresh.EndOfTick(ShopKind.Merchant, ShopNumber);
        }

        Assert.Empty(sent);
        refresh.EndOfTick(ShopKind.Merchant, ShopNumber);
        Assert.Equal([ClientPackets.OneValue(ClientCommand.Look, ShopNumber)], sent);
    }

    [Fact]
    public void ShopRefresh_AsksNothingWithoutAShop()
    {
        var refresh = new ShopRefresh();
        var sent = new List<byte[]>();
        refresh.Send = sent.Add;

        for (var tick = 0; tick < 30; tick++)
        {
            refresh.EndOfTick(null, ShopNumber);
        }

        Assert.Empty(sent);
    }

    [Fact]
    public void EachTick_RefreshesTheOpenShop()
    {
        var sent = new List<byte[]>();
        state.ShopRefresh.Send = sent.Add;
        OpenShop();

        for (var tick = 0; tick < 9; tick++)
        {
            Process();
        }

        Assert.Contains(ClientPackets.OneValue(ClientCommand.Look, ShopNumber), sent);
    }

    private void OpenShop() => Process(ShopCommands());

    private static TestCommand[] ShopCommands() =>
    [
        TestCommand.Of(ServerCommand.Look1),
        TestCommand.Of(ServerCommand.Look2),
        TestCommand.Of(ServerCommand.Look3).UInt16(0).UInt16(0).UInt16(0).UInt16(ShopNumber).UInt16(7),
        TestCommand.Of(ServerCommand.Look4).UInt16(0).UInt16(0).Byte((byte)LookFlags.Shop).UInt32(250),
        TestCommand.Of(ServerCommand.Look5).Text("Merchant", 15),
        .. Enumerable.Range(0, CharacterLook.ShopSlots / 2)
            .Select(pair => TestCommand.Of(ServerCommand.Look6)
                .Byte((byte)(pair * 2))
                .UInt16(1000)
                .UInt32(10)
                .UInt16(1001)
                .UInt32(20)
                .Bytes(0, 0)),
    ];

    private void Process(params TestCommand[] commands) => processor.Process(TestCommand.Payload(commands));
}
