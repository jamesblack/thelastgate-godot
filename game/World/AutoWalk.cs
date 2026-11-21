using System;
using System.Diagnostics;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.World;

public enum WalkDirection
{
    North,
    West,
    South,
    East,
}

public sealed class AutoWalk
{
    private const int StepTicks = 4;
    private const int MostSteps = 1000;
    private const int Reach = 7;
    private const int ButtonRadius = 12;
    private const int Centre = WorldMap.Size / 2;

    private static readonly (WalkDirection Direction, int X, int Y)[] Buttons =
    [
        (WalkDirection.North, 289, 272),
        (WalkDirection.West, 289, 622),
        (WalkDirection.South, 990, 622),
        (WalkDirection.East, 990, 272),
    ];

    private WalkDirection? direction;
    private int steps;
    private int ticksUntilStep;

    public Action<byte[]> Send { get; set; } = _ => { };

    public bool Walking => direction != null;

    public static WalkDirection? ButtonAt(int screenX, int screenY)
    {
        if (MapPicking.PointAt(screenX, screenY) != null)
        {
            return null;
        }

        foreach (var (buttonDirection, x, y) in Buttons)
        {
            var (across, down) = (screenX - x, screenY - y);
            if (across * across + down * down < ButtonRadius * ButtonRadius)
            {
                return buttonDirection;
            }
        }

        return null;
    }

    public void Start(WalkDirection walkDirection)
    {
        direction = walkDirection;
        steps = 0;
    }

    public void Stop()
    {
        direction = null;
        steps = 0;
    }

    public void EndOfTick(WorldMap map)
    {
        if (ticksUntilStep > 0)
        {
            ticksUntilStep--;
        }

        if (direction is not { } walkDirection || ticksUntilStep > 0)
        {
            return;
        }

        var (column, row) = walkDirection switch
        {
            WalkDirection.North => (Centre - Reach, Centre),
            WalkDirection.West => (Centre, Centre - Reach),
            WalkDirection.South => (Centre + Reach, Centre),
            WalkDirection.East => (Centre, Centre + Reach),
            _ => throw new UnreachableException(),
        };
        ref readonly var target = ref map.Tiles[column + row * WorldMap.Size];
        Send(MapCommandPackets.ForTile(ClientCommand.Move, target.X, target.Y));
        ticksUntilStep = StepTicks;
        if (steps++ > MostSteps)
        {
            Stop();
        }
    }
}
