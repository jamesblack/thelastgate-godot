using System;
using System.Diagnostics;
using TheLastGate.State;

namespace TheLastGate.World;

// No offset means the character keeps the one it was last drawn with.
public readonly record struct CharacterFrame(int Sprite, (int X, int Y)? Offset);

public static class CharacterFrames
{
    private const int IdleUp = 0;
    private const int IdleFrames = 8;
    private const int SpecialIdleSprite = 22480;
    private const int WalkFrames = 8;
    private const int TurnAndActionFrameShift = 32;
    private const int ActionRowShift = 5;

    private static readonly int[] ActionRows = [0, 1, 1, 6, 6, 2, 3, 4, 5, 7, 4];

    private static readonly WalkLook[] StraightWalks =
    [
        new(64, -1, 2, 1, 4),
        new(72, 1, 2, -1, 4),
        new(80, -1, 2, -1, 4),
        new(88, 1, 2, 1, 4),
    ];

    private static readonly WalkLook[] DiagonalWalks =
    [
        new(96, -1, 1, 0, 1),
        new(104, 0, 1, -1, 2),
        new(112, 0, 1, 1, 2),
        new(120, 1, 1, 0, 1),
    ];

    public static bool CyclesIdleFrame(in MapTile tile, int serverTick) =>
        tile.CharacterSprite != 0
        && CharacterStatuses.BlockOf(tile.CharacterStatus).Kind == CharacterStatusKind.Idle
        && (tile.CharacterStatus == IdleUp
            || (StatusSpeed.MoveSteps(tile.CharacterSpeed, tile.CharacterMoveSpeed, serverTick) != 0
                && IsAdvancing(tile)));

    public static int NextIdleFrame(int idleFrame) => (idleFrame + 1) % IdleFrames;

    public static CharacterFrame For(in MapTile tile, int serverTick, int idleFrame, Action<string> reportUnusualData)
    {
        if (tile.CharacterSprite == 0)
        {
            return new CharacterFrame(0, null);
        }

        var status = tile.CharacterStatus;
        var block = CharacterStatuses.BlockOf(status);
        return block.Kind switch
        {
            CharacterStatusKind.Idle => Still(
                tile,
                status * IdleFrames + (tile.CharacterSprite == SpecialIdleSprite ? idleFrame : 0)),
            CharacterStatusKind.StraightWalk => Walk(tile, block, StraightWalks[block.Direction], serverTick),
            CharacterStatusKind.DiagonalWalk => Walk(tile, block, DiagonalWalks[block.Direction], serverTick),
            CharacterStatusKind.Turn => Still(tile, status + TurnAndActionFrameShift),
            CharacterStatusKind.Action => Still(
                tile,
                status + TurnAndActionFrameShift + (ActionRow(tile, reportUnusualData) << ActionRowShift)),
            CharacterStatusKind.Unknown => new CharacterFrame(tile.CharacterSprite, null),
            _ => throw new UnreachableException(),
        };
    }

    private static CharacterFrame Still(in MapTile tile, int frame) => new(tile.CharacterSprite + frame, (0, 0));

    private static CharacterFrame Walk(in MapTile tile, CharacterStatusBlock block, WalkLook look, int serverTick)
    {
        var stepsDone = tile.CharacterStatus - block.FirstStatus;
        var distance = WalkOffset.Distance(
            stepsDone,
            block.Length,
            tile.CharacterSpeed,
            tile.CharacterMoveSpeed,
            IsAdvancing(tile),
            serverTick);
        return new CharacterFrame(
            tile.CharacterSprite + stepsDone * WalkFrames / block.Length + look.FirstFrame,
            (look.XSign * (distance / look.XDivisor), look.YSign * (distance / look.YDivisor)));
    }

    private static int ActionRow(in MapTile tile, Action<string> reportUnusualData)
    {
        var action = tile.CharacterStatusOffset;
        if (action < ActionRows.Length)
        {
            return ActionRows[action];
        }

        reportUnusualData(
            $"Character {tile.CharacterNumber} does action {action}, past the end of the current client's action "
            + "table; showing its first row of frames.");
        return 0;
    }

    private static bool IsAdvancing(in MapTile tile) => !((MapFlags)tile.Flags).HasFlag(MapFlags.Stunned);

    private readonly record struct WalkLook(int FirstFrame, int XSign, int XDivisor, int YSign, int YDivisor);
}
