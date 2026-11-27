using GdUnit4;
using TheLastGate.Net;
using TheLastGate.Rules;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class DivinerCardTests
{
    private const int CardSlot = 7;
    private const int OtherItem = 1234;
    private const int Regenerate = 28;
    private const uint Summoner = 1u << 13;
    private const uint Warrior = 1u << 10;
    private const int DivinerNode = 10;

    private static readonly int Card = DivinerCard.TarotSprites.First();
    private DivinerCard helper = null!;

    private List<byte[]> sent = [];

    [BeforeTest]
    public void TurnItOn()
    {
        sent = [];
        helper = new DivinerCard { Enabled = true, Send = sent.Add };
    }

    [TestCase]
    public void IsDiviner_NeedsASummonerWithNodeTenLearnedAndNotCorrupted()
    {
        var learned = TreeValueWithNodeLearned(DivinerNode);
        var plainNodes = new byte[PlayerState.TreeNodeCount];
        var corruptedNodes = new byte[PlayerState.TreeNodeCount];
        corruptedNodes[DivinerNode - 1] = 5;

        AssertThat(DivinerCard.IsDiviner(Summoner, learned, plainNodes)).IsTrue();
        AssertThat(DivinerCard.IsDiviner(Warrior, learned, plainNodes)).IsFalse();
        AssertThat(DivinerCard.IsDiviner(Summoner, 0, plainNodes)).IsFalse();
        AssertThat(DivinerCard.IsDiviner(Summoner, learned, corruptedNodes)).IsFalse();
    }

    [TestCase(DivinerCard.GhostCompanion)]
    [TestCase(DivinerCard.ShadowCopy)]
    public void ASummoningCast_PicksUpTheCardFirst(int skill)
    {
        helper.BeforeSending(Cast(skill), Sight());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
        AssertThat(helper.IsPending).IsTrue();
    }

    [TestCase]
    public void TheFirstCardInTheInventory_IsTheOneTaken()
    {
        var inventory = Inventory();
        inventory[3] = DivinerCard.TarotSprites.Last();

        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { InventorySprites = inventory });

        AssertThat(sent).ContainsExactly(Swap(3));
    }

    [TestCase]
    public void OtherSkills_AreLeftAlone()
    {
        helper.BeforeSending(Cast(Regenerate), Sight());

        AssertThat(sent).IsEmpty();
        AssertThat(helper.IsPending).IsFalse();
    }

    [TestCase]
    public void NotADiviner_IsLeftAlone()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { IsDiviner = false });

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void NoCardInTheInventory_IsLeftAlone()
    {
        var inventory = Inventory();
        inventory[CardSlot] = OtherItem;

        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { InventorySprites = inventory });

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void TurnedOff_IsLeftAlone()
    {
        helper.Enabled = false;

        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void SomethingOnTheCursor_IsLeftAlone()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { HeldSprite = OtherItem });

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void SpellExhaustion_KeepsTheHelperOut()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { IsExhausted = true });

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void ACastAlreadyPlaying_KeepsTheHelperOut()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight() with { PlayerIsCasting = true });

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void AnInventoryClickJustBefore_KeepsTheHelperOut()
    {
        helper.BeforeSending(Swap(2), Sight());
        Ticks(DivinerCard.StartGraceTicks - 1, Sight());
        sent.Clear();

        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());

        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void AnInventoryClickAGraceAgo_NoLongerKeepsTheHelperOut()
    {
        helper.BeforeSending(Swap(2), Sight());
        Ticks(DivinerCard.StartGraceTicks, Sight());

        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
    }

    [TestCase]
    public void TheCardGoesBackAfterTheCastPlaysOut()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        helper.EndOfTick(Holding());
        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        AssertThat(sent).IsEmpty();

        helper.EndOfTick(Holding());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
    }

    [TestCase]
    public void TheCardStaysTheHelpersUntilItLeavesTheCursor()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding());
        AssertThat(helper.HoldsItsCard(Card)).IsTrue();

        helper.EndOfTick(Sight());

        AssertThat(helper.HoldsItsCard(Card)).IsFalse();
        AssertThat(helper.IsPending).IsFalse();
    }

    [TestCase]
    public void ACardTheHelperDidNotPickUp_IsNotItsCard()
    {
        AssertThat(helper.HoldsItsCard(Card)).IsFalse();
    }

    [TestCase]
    public void TheGraceWaitsOutAWalk()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        Ticks(DivinerCard.StartGraceTicks * 2, Holding() with { PlayerIsMoving = true });
        Ticks(DivinerCard.StartGraceTicks, Holding());
        AssertThat(sent).IsEmpty();

        helper.EndOfTick(Holding());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
    }

    [TestCase(InventoryAction.SwapWithInventorySlot)]
    [TestCase(InventoryAction.SwapWithGearSlot)]
    [TestCase(InventoryAction.MoveStack)]
    public void AnInventoryClickWhilePending_HandsTheCardToThePlayer(InventoryAction action)
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        helper.EndOfTick(Holding());
        sent.Clear();

        helper.BeforeSending(InventoryPackets.For(action, 3, 0), Holding());
        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding());

        AssertThat(sent).IsEmpty();
        AssertThat(helper.IsPending).IsFalse();
        AssertThat(helper.HoldsItsCard(Card)).IsFalse();
    }

    [TestCase]
    public void AnItemUseWhilePending_KeepsTheCard()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        helper.BeforeSending(InventoryPackets.For(InventoryAction.UseItem, 0, 0), Holding());
        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
    }

    [TestCase]
    public void ACastThatNeverShows_PutsTheCardBackAfterTheGrace()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        Ticks(DivinerCard.StartGraceTicks, Holding());
        AssertThat(sent).IsEmpty();

        helper.EndOfTick(Holding());

        AssertThat(sent).ContainsExactly(Swap(CardSlot));
    }

    [TestCase]
    public void AMovedCard_IsLeftAlone()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        helper.EndOfTick(Holding() with { HeldSprite = 0, PlayerIsCasting = true });
        helper.EndOfTick(Holding() with { HeldSprite = 0 });

        AssertThat(sent).IsEmpty();
        AssertThat(helper.IsPending).IsFalse();
    }

    [TestCase]
    public void ASlotFilledFromTheGround_PutsTheCardInTheFirstEmptySlot()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();
        var filled = Holding().InventorySprites.ToArray();
        filled[CardSlot] = OtherItem;

        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding() with { InventorySprites = filled });

        AssertThat(sent).ContainsExactly(Swap(1));
    }

    [TestCase]
    public void AFullInventory_LeavesTheCardHeld()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();
        var full = Enumerable.Repeat(OtherItem, PlayerState.InventorySlots).ToArray();

        helper.EndOfTick(Holding() with { PlayerIsCasting = true });
        helper.EndOfTick(Holding() with { InventorySprites = full });

        AssertThat(sent).IsEmpty();
        AssertThat(helper.IsPending).IsFalse();
    }

    [TestCase]
    public void ACastWhilePending_PassesThrough()
    {
        helper.BeforeSending(Cast(DivinerCard.GhostCompanion), Sight());
        sent.Clear();

        helper.BeforeSending(Cast(DivinerCard.ShadowCopy), Sight());

        AssertThat(sent).IsEmpty();
        AssertThat(helper.IsPending).IsTrue();
    }

    private void Ticks(int count, CardSight sight)
    {
        for (var tick = 0; tick < count; tick++)
        {
            helper.EndOfTick(sight);
        }
    }

    private static int TreeValueWithNodeLearned(int node) =>
        Enumerable.Range(0, 31).Select(bit => 1 << bit).First(value => TreeEffects.NodeLearned(value, node));

    private static int[] Inventory()
    {
        var inventory = new int[PlayerState.InventorySlots];
        inventory[0] = OtherItem;
        inventory[CardSlot] = Card;
        return inventory;
    }

    private static CardSight Sight() => new(true, 0, Inventory(), false, false, false);

    private static CardSight Holding()
    {
        var inventory = Inventory();
        inventory[CardSlot] = 0;
        return new CardSight(true, Card, inventory, false, false, false);
    }

    private static byte[] Cast(int skill) => SkillPackets.Use(skill, 0);

    private static byte[] Swap(int slot) => InventoryPackets.For(InventoryAction.SwapWithInventorySlot, slot, 0);
}
