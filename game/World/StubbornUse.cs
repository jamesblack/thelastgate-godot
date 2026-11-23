using System;
using System.Diagnostics;
using TheLastGate.Assets;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.World;

public readonly record struct UseSight(bool PlayerIsUsing, int FacedX, int FacedY, bool TargetOccupied)
{
    private const int UseAnimation = 4;
    private const int FacingUp = 0;
    private const int FacingDown = 1;
    private const int FacingLeft = 2;
    private const int FacingRight = 3;

    public static UseSight None { get; } = new(false, -1, -1, false);

    public static UseSight From(WorldMap map, (int X, int Y) target)
    {
        ref readonly var player = ref map.Tiles[WorldMap.PlayerTile];
        var occupied = HasCharacterAt(map, player, target);
        var block = CharacterStatuses.BlockOf(player.CharacterStatus);
        if (block.Kind is not (CharacterStatusKind.Idle or CharacterStatusKind.Action)
            || FacedTileOffset(block.Direction) is not { } offset)
        {
            return None with { TargetOccupied = occupied };
        }

        ref readonly var faced = ref map.Tiles[WorldMap.PlayerTile + offset];
        var isUsing = block.Kind == CharacterStatusKind.Action && player.CharacterStatusOffset == UseAnimation;
        return new UseSight(isUsing, faced.X, faced.Y, occupied);
    }

    // A target off the map, as after a teleporting use, has nobody on it as far as the client can tell.
    private static bool HasCharacterAt(WorldMap map, in MapTile player, (int X, int Y) target)
    {
        var column = WorldMap.Size / 2 + target.X - player.X;
        var row = WorldMap.Size / 2 + target.Y - player.Y;
        return column is >= 0 and < WorldMap.Size && row is >= 0 and < WorldMap.Size
            && map.Tiles[column + row * WorldMap.Size].CharacterSprite != 0;
    }

    private static int? FacedTileOffset(int direction) => direction switch
    {
        FacingUp => -WorldMap.Size,
        FacingDown => WorldMap.Size,
        FacingLeft => -1,
        FacingRight => 1,
        _ => null,
    };
}

// A use counts once the player's use animation, facing the tile, has played out with nobody on the tile, through the
// tick after it ends: the server applies the use when the animation ends, and a pentagram won't activate under anyone.
public sealed class StubbornUse(TextLog chat)
{
    public const int MostFailures = 8;
    public const int StartGraceTicks = 20;
    public const int AfterFightTicks = 10;

    private const int TicksPerSecond = 20;

    private static readonly string GaveUpMessage = $"Gave up on that use after {MostFailures} tries.";

    private bool enabled;
    private int failures;
    private Phase phase;
    private int quietTicks;
    private int ticksCounted;
    private (int X, int Y) tile;
    private bool wasUsing;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!value && IsPending)
            {
                Settle();
            }
        }
    }

    public Action<byte[]> Send { get; set; } = _ => { };

    public bool IsPending => phase != Phase.None;

    public bool IsWaitingToRetry => phase == Phase.Waiting;

    public (int X, int Y) Tile => tile;

    public event Action<int, int>? Settled;

    public void Sent(byte[] packet)
    {
        if (!enabled)
        {
            return;
        }

        if ((ClientCommand)packet[0] == ClientCommand.Use)
        {
            tile = MapCommandPackets.TileOf(packet);
            failures = 0;
            quietTicks = AfterFightTicks;
            wasUsing = false;
            StartWaitingForTheServer();
        }
        else if (CancelsTheUse(packet))
        {
            phase = Phase.None;
        }
    }

    public void EndOfTick(PlayerTarget target, UseSight sight)
    {
        switch (phase)
        {
            case Phase.None:
                return;
            case Phase.Starting:
                WhileStarting(target, sight);
                break;
            case Phase.Animating:
                WhileAnimating(sight);
                break;
            case Phase.AnimationEnded:
                AfterTheAnimation(sight);
                break;
            case Phase.Waiting:
                WhileWaiting(target, sight);
                break;
            default:
                throw new UnreachableException();
        }

        quietTicks = target.AttackCharacter != 0 ? 0 : Math.Min(quietTicks + 1, AfterFightTicks);
        wasUsing = sight.PlayerIsUsing;
    }

    public static int RetryDelayTicks(int failures) => TicksPerSecond * failures;

    private void WhileStarting(PlayerTarget target, UseSight sight)
    {
        if (!NoticedTheUse(target, sight) && ++ticksCounted > StartGraceTicks)
        {
            Fail();
        }
    }

    private void WhileAnimating(UseSight sight)
    {
        if (sight.TargetOccupied)
        {
            Fail();
        }
        else if (!sight.PlayerIsUsing)
        {
            phase = Phase.AnimationEnded;
        }
    }

    private void AfterTheAnimation(UseSight sight)
    {
        if (sight.TargetOccupied)
        {
            Fail();
        }
        else
        {
            Settle();
        }
    }

    private void WhileWaiting(PlayerTarget target, UseSight sight)
    {
        if (NoticedTheUse(target, sight) || --ticksCounted > 0 || FightingOrJustFought(target))
        {
            return;
        }

        Send(MapCommandPackets.ForTile(ClientCommand.Use, tile.X, tile.Y));
        StartWaitingForTheServer();
    }

    // Ticks are played from a buffer, so a use can show up a second or more after it was sent, even once a retry
    // is planned.
    private bool NoticedTheUse(PlayerTarget target, UseSight sight)
    {
        if (ServerIsUsingTheTile(target))
        {
            StartWaitingForTheServer();
            return true;
        }

        if (!sight.PlayerIsUsing || wasUsing || !Faces(sight))
        {
            return false;
        }

        phase = Phase.Animating;
        WhileAnimating(sight);
        return true;
    }

    private void StartWaitingForTheServer()
    {
        phase = Phase.Starting;
        ticksCounted = 0;
    }

    private void Fail()
    {
        failures++;
        if (failures >= MostFailures)
        {
            chat.Add(new LogLine(GaveUpMessage, (int)TextColour.Grey));
            Settle();
            return;
        }

        phase = Phase.Waiting;
        ticksCounted = RetryDelayTicks(failures);
    }

    private void Settle()
    {
        phase = Phase.None;
        Settled?.Invoke(tile.X, tile.Y);
    }

    // Between one attacker and the next the server can show no fight for several ticks.
    private bool FightingOrJustFought(PlayerTarget target) =>
        target.AttackCharacter != 0 || quietTicks < AfterFightTicks;

    // The server drops a use for a new action on a tile, an attack or a give, but not for an inventory click.
    private static bool CancelsTheUse(byte[] packet) =>
        ActionCommands.CancelsPendingActions(packet)
        || (ClientCommand)packet[0] is ClientCommand.Attack or ClientCommand.Give;

    private bool ServerIsUsingTheTile(PlayerTarget target) =>
        target.MiscAction == MiscActions.Use && (target.MiscTarget1, target.MiscTarget2) == tile;

    private bool Faces(UseSight sight) => (sight.FacedX, sight.FacedY) == tile;

    private enum Phase
    {
        None,
        Starting,
        Animating,
        AnimationEnded,
        Waiting,
    }
}
