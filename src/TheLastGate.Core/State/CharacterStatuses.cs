using System;
using System.Diagnostics;

namespace TheLastGate.State;

public enum CharacterStatusKind
{
    Idle,
    StraightWalk,
    DiagonalWalk,
    Turn,
    Action,
    Unknown,
}

public readonly record struct CharacterStatusBlock(
    CharacterStatusKind Kind,
    int FirstStatus,
    int Length,
    int Direction);

// The server resends a character's status only when it moves into another block, so within a block the client has to
// step it on its own, as fast as the character's speed allows.
public static class CharacterStatuses
{
    private const int LastIdle = 7;
    private const int IdleStatuses = 8;
    private const int FirstStraightWalk = 16;
    private const int StraightWalkSteps = 8;
    private const int FirstDiagonalWalk = 48;
    private const int DiagonalWalkSteps = 12;
    private const int FirstTurn = 96;
    private const int TurnSteps = 4;
    private const int FirstAction = 160;
    private const int LastAction = 191;
    private const int ActionSteps = 8;

    public static CharacterStatusBlock BlockOf(int status) => status switch
    {
        >= 0 and <= LastIdle => new CharacterStatusBlock(CharacterStatusKind.Idle, 0, IdleStatuses, status),
        >= FirstStraightWalk and < FirstDiagonalWalk =>
            Block(CharacterStatusKind.StraightWalk, status, FirstStraightWalk, StraightWalkSteps),
        >= FirstDiagonalWalk and < FirstTurn =>
            Block(CharacterStatusKind.DiagonalWalk, status, FirstDiagonalWalk, DiagonalWalkSteps),
        >= FirstTurn and < FirstAction => Block(CharacterStatusKind.Turn, status, FirstTurn, TurnSteps),
        >= FirstAction and <= LastAction => Block(CharacterStatusKind.Action, status, FirstAction, ActionSteps),
        _ => new CharacterStatusBlock(CharacterStatusKind.Unknown, status, 1, 0),
    };

    internal static void Advance(ref MapTile tile, int serverTick, Action<string> reportUnusualData)
    {
        if (tile.CharacterSprite == 0)
        {
            return;
        }

        var block = BlockOf(tile.CharacterStatus);
        switch (block.Kind)
        {
            case CharacterStatusKind.Idle:
                return;
            case CharacterStatusKind.StraightWalk or CharacterStatusKind.DiagonalWalk or CharacterStatusKind.Turn:
                MoveOn(
                    ref tile,
                    block,
                    StatusSpeed.MoveSteps(tile.CharacterSpeed, tile.CharacterMoveSpeed, serverTick),
                    reportUnusualData);
                return;
            case CharacterStatusKind.Action:
                MoveOn(
                    ref tile,
                    block,
                    StatusSpeed.ActionSteps(
                        tile.CharacterStatusOffset,
                        tile.CharacterSpeed,
                        tile.CharacterAttackSpeed,
                        tile.CharacterCastSpeed,
                        tile.CharacterTextColour,
                        serverTick),
                    reportUnusualData);
                return;
            case CharacterStatusKind.Unknown:
                reportUnusualData(
                    $"Character {tile.CharacterNumber} has status {tile.CharacterStatus}, which the current client "
                    + "doesn't animate.");
                return;
            default:
                throw new UnreachableException();
        }
    }

    private static CharacterStatusBlock Block(CharacterStatusKind kind, int status, int firstOfKind, int length)
    {
        var direction = (status - firstOfKind) / length;
        return new CharacterStatusBlock(kind, firstOfKind + direction * length, length, direction);
    }

    // Wraps once, as the current client does, so a jump of more than a whole block lands outside it.
    private static void MoveOn(
        ref MapTile tile,
        CharacterStatusBlock block,
        int steps,
        Action<string> reportUnusualData)
    {
        if (steps == 0 || ((MapFlags)tile.Flags).HasFlag(MapFlags.Stunned))
        {
            return;
        }

        var lastStatus = block.FirstStatus + block.Length - 1;
        var next = tile.CharacterStatus + steps;
        if (next > lastStatus)
        {
            next = block.FirstStatus + (next - lastStatus - 1);
            if (next > lastStatus)
            {
                reportUnusualData(
                    $"Character {tile.CharacterNumber}'s animation jumped past its last frame into status {next}, "
                    + "as the current client's does.");
            }
        }

        tile.CharacterStatus = next;
    }
}
