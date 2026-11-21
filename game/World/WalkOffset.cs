using TheLastGate.State;

namespace TheLastGate.World;

public static class WalkOffset
{
    // The share of a tile walked so far: the ticks since the step began over all the ticks the step will take,
    // counted from the speed's pattern of ticks that move a frame.
    public static int Distance(
        int framesDone,
        int framesInStep,
        int speed,
        int moveSpeed,
        bool advancing,
        int serverTick)
    {
        if (!advancing)
        {
            return IsometricProjection.TileSize * framesDone / framesInStep;
        }

        var effectiveSpeed = StatusSpeed.MoveSpeed(speed, moveSpeed);
        var tick = serverTick;
        var ticksDone = 0;
        for (var framesLeft = framesDone; framesLeft > 0;)
        {
            tick = Previous(tick);
            ticksDone++;
            if (StatusSpeed.StepsThisTick(effectiveSpeed, tick) != 0)
            {
                framesLeft--;
            }
        }

        while (true)
        {
            tick = Previous(tick);
            if (StatusSpeed.StepsThisTick(effectiveSpeed, tick) != 0)
            {
                break;
            }

            ticksDone++;
        }

        tick = serverTick;
        var ticksInStep = ticksDone;
        var framesToGo = framesInStep - framesDone;
        while (true)
        {
            if (StatusSpeed.StepsThisTick(effectiveSpeed, tick) != 0)
            {
                framesToGo--;
            }

            if (framesToGo < 1)
            {
                break;
            }

            tick = Next(tick);
            ticksInStep++;
        }

        return IsometricProjection.TileSize * ticksDone / (ticksInStep + 1);
    }

    private static int Previous(int tick) => tick == 0 ? StatusSpeed.CycleLength - 1 : tick - 1;

    private static int Next(int tick) => tick == StatusSpeed.CycleLength - 1 ? 0 : tick + 1;
}
