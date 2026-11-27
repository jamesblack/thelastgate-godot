using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheLastGate.Net;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.World;

public readonly record struct CardSight(
    bool IsDiviner,
    int HeldSprite,
    IReadOnlyList<int> InventorySprites,
    bool PlayerIsCasting,
    bool PlayerIsMoving,
    bool IsExhausted)
{
    private const int CastAnimation = 9;
    private const int ExhaustionBuffSprite = 97;

    public static CardSight From(PlayerState player, WorldMap map)
    {
        ref readonly var tile = ref map.Tiles[WorldMap.PlayerTile];
        var kind = CharacterStatuses.BlockOf(tile.CharacterStatus).Kind;
        var isCasting = kind == CharacterStatusKind.Action && tile.CharacterStatusOffset == CastAnimation;
        var isMoving = kind is CharacterStatusKind.StraightWalk or CharacterStatusKind.DiagonalWalk
            or CharacterStatusKind.Turn;
        return new CardSight(
            DivinerCard.IsDiviner(player.Kindred, player.TreePoints, player.TreeNodes),
            player.HeldItem.Sprite,
            player.Inventory.Select(slot => slot.Sprite).ToArray(),
            isCasting,
            isMoving,
            player.Buffs.Any(buff => buff.Sprite == ExhaustionBuffSprite));
    }
}

// The server copies the card held on the cursor onto the summon when the spell fires, at the end of the cast
// animation, not when the skill is sent.
public sealed class DivinerCard
{
    public const int GhostCompanion = 27;
    public const int ShadowCopy = 46;
    public const int StartGraceTicks = 20;

    private const int FirstUprightCard = 3800;
    private const int FirstReversedCard = 2800;
    private const int CardsInADeck = 22;
    private const int DivinerNode = 10;
    private const int NoSlot = -1;

    private int cardSlot = NoSlot;
    private int cardSprite;
    private Phase phase;
    private int ticksSinceInventoryClick = StartGraceTicks;
    private int ticksWaited;

    public static IReadOnlySet<int> TarotSprites { get; } = Enumerable.Range(FirstUprightCard, CardsInADeck)
        .Concat(Enumerable.Range(FirstReversedCard, CardsInADeck))
        .ToHashSet();

    public bool Enabled { get; set; }

    public Action<byte[]> Send { get; set; } = _ => { };

    public bool IsPending => phase != Phase.None;

    // Until the put-back shows, the card on the cursor is still the helper's, not something the player is holding.
    public bool HoldsItsCard(int heldSprite) => IsPending && heldSprite == cardSprite;

    // A corruptor in the Diviner node replaces it, as the server's T_SUMM_SK(cn, 10) counts it.
    public static bool IsDiviner(uint kindred, int treePoints, IReadOnlyList<byte> treeNodes) =>
        TreeEffects.ClassFromKindred(kindred) == TreeClass.Summoner
        && TreeEffects.NodeLearned(treePoints, DivinerNode)
        && treeNodes[DivinerNode - 1] == 0;

    public void BeforeSending(byte[] packet, CardSight sight)
    {
        var command = (ClientCommand)packet[0];
        if (command == ClientCommand.Inventory)
        {
            ticksSinceInventoryClick = 0;
            if (MovesTheCursor(packet))
            {
                phase = Phase.None;
            }
        }
        else if (command == ClientCommand.Skill && ShouldHoldTheCard(SkillOf(packet), sight))
        {
            PickUpTheCard(sight);
        }
    }

    public void EndOfTick(CardSight sight)
    {
        ticksSinceInventoryClick = Math.Min(ticksSinceInventoryClick + 1, StartGraceTicks);
        switch (phase)
        {
            case Phase.None:
                return;
            case Phase.WaitingForCast:
                if (sight.PlayerIsCasting)
                {
                    phase = Phase.Casting;
                }
                else if (!sight.PlayerIsMoving && ++ticksWaited > StartGraceTicks)
                {
                    PutTheCardBack(sight);
                }

                break;
            case Phase.Casting:
                if (!sight.PlayerIsCasting)
                {
                    PutTheCardBack(sight);
                }

                break;
            case Phase.PuttingBack:
                if (sight.HeldSprite != cardSprite || ++ticksWaited > StartGraceTicks)
                {
                    phase = Phase.None;
                }

                break;
            default:
                throw new UnreachableException();
        }
    }

    private static int SkillOf(byte[] packet) => BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(1));

    // The server answers these with the cursor's item, so after one the card is wherever the player put it.
    private static bool MovesTheCursor(byte[] packet) =>
        (InventoryAction)BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(1))
        is InventoryAction.SwapWithInventorySlot or InventoryAction.SwapWithGearSlot or InventoryAction.MoveStack;

    // The client's view of the cursor and inventory runs behind the server's, so a recent inventory click or a cast
    // still playing would make what it sees unreliable.
    private bool ShouldHoldTheCard(int skill, CardSight sight) =>
        Enabled
        && skill is GhostCompanion or ShadowCopy
        && !IsPending
        && sight.IsDiviner
        && sight.HeldSprite == 0
        && !sight.PlayerIsCasting
        && !sight.IsExhausted
        && ticksSinceInventoryClick >= StartGraceTicks
        && FirstCardSlot(sight) != NoSlot;

    private static int FirstCardSlot(CardSight sight) => FirstSlot(sight, TarotSprites.Contains);

    private static int FirstSlot(CardSight sight, Func<int, bool> holds)
    {
        for (var slot = 0; slot < sight.InventorySprites.Count; slot++)
        {
            if (holds(sight.InventorySprites[slot]))
            {
                return slot;
            }
        }

        return NoSlot;
    }

    private void PickUpTheCard(CardSight sight)
    {
        cardSlot = FirstCardSlot(sight);
        cardSprite = sight.InventorySprites[cardSlot];
        phase = Phase.WaitingForCast;
        ticksWaited = 0;
        Send(Swap(cardSlot));
    }

    // A pick-up from the ground or the depot fills the first empty slot, which is often the one the card left.
    private void PutTheCardBack(CardSight sight)
    {
        var slot = sight.InventorySprites[cardSlot] == 0 ? cardSlot : FirstSlot(sight, sprite => sprite == 0);
        if (sight.HeldSprite != cardSprite || slot == NoSlot)
        {
            phase = Phase.None;
            return;
        }

        Send(Swap(slot));
        phase = Phase.PuttingBack;
        ticksWaited = 0;
    }

    private static byte[] Swap(int slot) => InventoryPackets.For(InventoryAction.SwapWithInventorySlot, slot, 0);

    private enum Phase
    {
        None,
        WaitingForCast,
        Casting,
        PuttingBack,
    }
}
