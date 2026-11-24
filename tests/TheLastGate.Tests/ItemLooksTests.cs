using TheLastGate.Items;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class ItemLooksTests
{
    private const int Sword = 100;
    private const int Shield = 200;

    private static readonly LogLine Header = new("Steel Sword:", (int)LogFont.Lime);
    private static readonly LogLine OtherHeader = new("Bronze Sword:", (int)LogFont.Lime);
    private static readonly LogLine Description = new("A finely crafted steel sword.", (int)LogFont.Yellow);
    private static readonly LogLine Chatter = new("Bob: hello", (int)LogFont.Blue);

    private readonly TextLog chat = new();
    private readonly ItemLooks looks;
    private readonly List<byte[]> sent = [];

    public ItemLooksTests() => looks = new ItemLooks(chat) { SendLook = sent.Add };

    [Fact]
    public void SlotChanged_FetchesTheSlotQuietlyOnceItsUpdatesSettle()
    {
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);

        Ticks(3);
        Assert.Empty(sent);
        Ticks(1);
        Assert.Equal([ItemLook(5), ItemLook(5)], sent);
    }

    [Fact]
    public void For_KeepsTheAnsweredLookForTheSlot()
    {
        FetchInventorySlot(5, Sword);

        Answer(Header, Description);

        var look = looks.For(ItemSlotKind.Inventory, 5, Sword);
        Assert.NotNull(look);
        Assert.Equal([Header, Description], look.Lines);
        Assert.Equal("Steel Sword", look.Details.Name);
        Assert.Empty(chat.Lines);
    }

    [Fact]
    public void EndOfTick_FetchesWornSlotsBeforeTheInventory()
    {
        looks.SlotChanged(ItemSlotKind.Inventory, 0, Sword);
        looks.SlotChanged(ItemSlotKind.Worn, 8, Shield);

        Ticks(4);

        Assert.Equal([GearLook(8), GearLook(8)], sent);
    }

    [Fact]
    public void EndOfTick_FetchesThePreferredSlotFirst()
    {
        looks.SlotChanged(ItemSlotKind.Inventory, 3, Sword);
        looks.SlotChanged(ItemSlotKind.Inventory, 9, Shield);
        looks.Prefer(ItemSlotKind.Inventory, 9);

        Ticks(4);

        Assert.Equal([ItemLook(9), ItemLook(9)], sent);
    }

    [Fact]
    public void EndOfTick_FetchesOneSlotAtATime()
    {
        looks.SlotChanged(ItemSlotKind.Inventory, 3, Sword);
        looks.SlotChanged(ItemSlotKind.Inventory, 9, Shield);

        Ticks(20);

        Assert.Equal(2, sent.Count);
    }

    [Fact]
    public void EndOfTick_FetchesNothingWhileAnItemIsHeld()
    {
        looks.HeldItemChanged(Shield);
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);

        Ticks(20);
        Assert.Empty(sent);

        looks.HeldItemChanged(0);
        Ticks(1);
        Assert.Equal([ItemLook(5), ItemLook(5)], sent);
    }

    [Fact]
    public void EndOfTick_FetchesNothingWhenTooltipsAreTurnedOff()
    {
        looks.Enabled = false;
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);

        Ticks(20);

        Assert.Empty(sent);
    }

    [Fact]
    public void EndOfTick_FetchesTheSameSlotAgainAtMostEveryFortyTicks()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);
        sent.Clear();

        Ticks(38);
        Assert.Empty(sent);
        Ticks(1);
        Assert.Equal(2, sent.Count);
    }

    [Fact]
    public void For_KeepsTheOldTextWhileTheSameItemIsFetchedAgain()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);

        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);

        Assert.NotNull(looks.For(ItemSlotKind.Inventory, 5, Sword));
    }

    [Fact]
    public void For_ForgetsTheTextWhenADifferentItemArrives()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);

        looks.SlotChanged(ItemSlotKind.Inventory, 5, Shield);

        Assert.Null(looks.For(ItemSlotKind.Inventory, 5, Shield));
    }

    [Fact]
    public void For_ShowsNothingForAnEmptySlot()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);

        looks.SlotChanged(ItemSlotKind.Inventory, 5, 0);

        Assert.Null(looks.For(ItemSlotKind.Inventory, 5, 0));
    }

    [Fact]
    public void EndOfTick_StopsAskingAboutAnItemThatNeverAnswers()
    {
        FetchInventorySlot(5, Sword);

        Ticks(31);
        looks.SlotChanged(ItemSlotKind.Inventory, 8, Sword);
        Ticks(100);

        Assert.Equal(2, sent.Count);
    }

    [Fact]
    public void EndOfTick_RetriesAMismatchedAnswerOnceThenGivesUp()
    {
        FetchInventorySlot(5, Sword);
        AnswerDifferently();

        Ticks(50);
        Assert.Equal(4, sent.Count);

        AnswerDifferently();
        Ticks(100);
        Assert.Equal(4, sent.Count);
    }

    [Fact]
    public void Offer_ReleasesOtherChatCaughtWithTheAnswer()
    {
        FetchInventorySlot(5, Sword);

        looks.Offer(Header);
        looks.Offer(Description);
        looks.Offer(Chatter);
        looks.Offer(Header);
        looks.Offer(Description);
        looks.EndOfTick();

        Assert.Equal([Chatter], chat.Lines);
    }

    [Fact]
    public void EndOfTick_ReleasesWhatWasCaughtWhenItGivesUpWaiting()
    {
        FetchInventorySlot(5, Sword);

        looks.Offer(Header);
        Ticks(31);

        Assert.Equal([Header], chat.Lines);
    }

    [Fact]
    public void For_DiscardsAnAnswerForASlotThatChangedMeanwhile()
    {
        FetchInventorySlot(5, Sword);

        looks.SlotChanged(ItemSlotKind.Inventory, 5, Shield);
        Answer(Header, Description);

        Assert.Null(looks.For(ItemSlotKind.Inventory, 5, Shield));
    }

    [Fact]
    public void ManualLookSent_LetsTheReplyThroughAndPausesFetching()
    {
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);

        looks.ManualLookSent();

        Assert.False(looks.Offer(Header));
        Ticks(19);
        Assert.Empty(sent);
        Ticks(1);
        Assert.Equal(2, sent.Count);
    }

    [Fact]
    public void HeldItemChanged_CarriesTheTextOfAnItemPickedUp()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);

        looks.SlotChanged(ItemSlotKind.Inventory, 5, 0);
        looks.HeldItemChanged(Sword);

        Assert.Equal([Header, Description], looks.Held(Sword)?.Lines);
        Assert.Null(looks.For(ItemSlotKind.Inventory, 5, 0));
    }

    [Fact]
    public void HeldItemChanged_CarriesTheTextOfAnItemPutDown()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);
        looks.SlotChanged(ItemSlotKind.Inventory, 5, 0);
        looks.HeldItemChanged(Sword);
        looks.EndOfTick();

        looks.SlotChanged(ItemSlotKind.Inventory, 7, Sword);
        looks.HeldItemChanged(0);

        Assert.Equal([Header, Description], looks.For(ItemSlotKind.Inventory, 7, Sword)?.Lines);
        Assert.Null(looks.Held(Sword));
    }

    [Fact]
    public void HeldItemChanged_SendsNoLookForAMovedItemWhoseLookIsKnown()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);
        sent.Clear();

        MoveThroughTheHand(5, ItemSlotKind.Worn, 4, Sword);
        Ticks(100);

        Assert.Empty(sent);
        Assert.Equal([Header, Description], looks.For(ItemSlotKind.Worn, 4, Sword)?.Lines);
    }

    [Fact]
    public void HeldItemChanged_StillFetchesAMovedItemWhoseLookWasDueAgain()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);
        looks.SlotChanged(ItemSlotKind.Inventory, 5, Sword);
        Ticks(1);
        sent.Clear();

        MoveThroughTheHand(5, ItemSlotKind.Worn, 4, Sword);
        Ticks(100);

        Assert.Equal([GearLook(4), GearLook(4)], sent);
    }

    [Fact]
    public void For_ShowsALongLookWholeAndKeepsItOutOfTheChat()
    {
        const int rainbowBeltLines = 35;
        var reply = Enumerable.Range(0, rainbowBeltLines)
            .Select(line => line == 0 ? Header : new LogLine($"Row {line}", (int)LogFont.Yellow))
            .ToArray();
        FetchInventorySlot(5, Sword);

        Answer(reply);

        Assert.Equal(reply, looks.For(ItemSlotKind.Inventory, 5, Sword)?.Lines);
        Assert.Empty(chat.Lines);
    }

    [Theory]
    [InlineData(ItemSlotKind.Inventory, 5)]
    [InlineData(ItemSlotKind.Worn, 4)]
    public void For_ShowsNothingAndFetchesAgainWhenTheSlotHoldsAnotherItem(ItemSlotKind kind, int index)
    {
        looks.SlotChanged(kind, index, Sword);
        Ticks(4);
        Answer(Header, Description);
        sent.Clear();

        Assert.Null(looks.For(kind, index, Shield));
        Assert.Null(looks.For(kind, index, Shield));
        Ticks(40);

        var look = kind == ItemSlotKind.Worn ? GearLook(index) : ItemLook(index);
        Assert.Equal([look, look], sent);
    }

    [Fact]
    public void Held_ShowsNothingWhenTheHandHoldsAnotherItem()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);
        looks.SlotChanged(ItemSlotKind.Inventory, 5, 0);
        looks.HeldItemChanged(Sword);

        Assert.Null(looks.Held(Shield));
        Assert.Null(looks.Held(Sword));
    }

    [Fact]
    public void Reset_ForgetsEverything()
    {
        FetchInventorySlot(5, Sword);
        Answer(Header, Description);

        looks.Reset();

        Assert.Null(looks.For(ItemSlotKind.Inventory, 5, Sword));
    }

    [Theory]
    [MemberData(nameof(Packets))]
    public void IsManualLook_SpotsTheLooksAPlayerAsksFor(byte[] packet, bool manualLook)
    {
        Assert.Equal(manualLook, ItemLooks.IsManualLook(packet));
    }

    public static TheoryData<byte[], bool> Packets() => new()
    {
        { ClientPackets.ThreeValues(ClientCommand.InventoryLook, 3, 0, 1), true },
        { InventoryPackets.For(InventoryAction.LookAtGear, 2, 1), true },
        { InventoryPackets.For(InventoryAction.UseItem, 2, 1), false },
        { MapCommandPackets.ForCharacter(ClientCommand.Look, 5), false },
    };

    private static byte[] ItemLook(int slot) => InventoryPackets.For(InventoryAction.LookAtItem, slot, 0);

    private static byte[] GearLook(int slot) => InventoryPackets.For(InventoryAction.LookAtGear, slot, 0);

    private void Ticks(int count)
    {
        for (var tick = 0; tick < count; tick++)
        {
            looks.EndOfTick();
        }
    }

    private void FetchInventorySlot(int slot, int sprite)
    {
        looks.SlotChanged(ItemSlotKind.Inventory, slot, sprite);
        Ticks(4);
    }

    private void MoveThroughTheHand(int fromInventory, ItemSlotKind toKind, int toIndex, int sprite)
    {
        looks.SlotChanged(ItemSlotKind.Inventory, fromInventory, 0);
        looks.HeldItemChanged(sprite);
        Ticks(10);
        looks.SlotChanged(toKind, toIndex, sprite);
        looks.HeldItemChanged(0);
    }

    private void Answer(params LogLine[] reply)
    {
        foreach (var line in reply.Concat(reply))
        {
            looks.Offer(line);
        }

        looks.EndOfTick();
    }

    private void AnswerDifferently()
    {
        looks.Offer(Header);
        looks.Offer(OtherHeader);
        looks.EndOfTick();
    }
}
