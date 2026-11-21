using System;

namespace TheLastGate.Game;

public sealed class TickPacer
{
    public const int NominalTickMilliseconds = 20;

    private const int FixedQueueSize = 8;

    private const int ArrivalWindow = 256;
    private const int GapPercentile = 95;
    private const double GapTolerancePeriods = 0.25;
    private const long LongestCountedGapMilliseconds = 200;
    private const int RecalculateEveryArrivals = 16;
    private const int TargetStart = 4;
    private const int TargetMinimum = 1;
    private const int TargetMaximum = 8;
    private const int TargetLowerAfterArrivals = 96;
    private const double RatePerTickOfError = 0.08;
    private const double RateFastest = 0.5;
    private const double RateSlowest = 1.25;

    private readonly int[] gaps = new int[ArrivalWindow];
    private readonly int[] sortedGaps = new int[ArrivalWindow];
    private bool adaptive;
    private int arrivalsSinceRecalculate;
    private int arrivalsSinceTargetChange;
    private double carriedMilliseconds;
    private int gapCount;
    private int gapHead;
    private long? lastArrival;
    private long? nextTickAt;
    private int windowSpanMilliseconds;

    public bool Adaptive
    {
        get => adaptive;
        set
        {
            if (value && !adaptive)
            {
                ResetMeasurement();
            }

            adaptive = value;
        }
    }

    public int BufferTarget { get; private set; } = TargetStart;

    public double MeasuredPeriodMilliseconds { get; private set; }

    public void OnArrival(long nowMilliseconds)
    {
        if (lastArrival is { } previousArrival)
        {
            // A stall on this side, such as dragging the window, would otherwise inflate the measured
            // period for a whole window. The cap is beyond the largest target, so the target still
            // sees it as a worst-case gap.
            var gap = (int)Math.Min(nowMilliseconds - previousArrival, LongestCountedGapMilliseconds);
            if (gapCount == ArrivalWindow)
            {
                windowSpanMilliseconds -= gaps[gapHead];
            }
            else
            {
                gapCount++;
            }

            gaps[gapHead] = gap;
            windowSpanMilliseconds += gap;
            gapHead = (gapHead + 1) % ArrivalWindow;
        }

        lastArrival = nowMilliseconds;
        arrivalsSinceTargetChange++;
        if (++arrivalsSinceRecalculate >= RecalculateEveryArrivals && gapCount >= RecalculateEveryArrivals)
        {
            arrivalsSinceRecalculate = 0;
            RecalculateTarget();
        }
    }

    public int NextTickMilliseconds(int queuedTicks)
    {
        if (!Adaptive)
        {
            return queuedTicks > 0
                ? NominalTickMilliseconds * FixedQueueSize / queuedTicks
                : NominalTickMilliseconds;
        }

        var baseMilliseconds = MeasuredPeriodMilliseconds >= 1.0
            ? MeasuredPeriodMilliseconds
            : NominalTickMilliseconds;
        var rate = Math.Clamp(1.0 - RatePerTickOfError * (queuedTicks - BufferTarget), RateFastest, RateSlowest);
        var tickMilliseconds = baseMilliseconds * rate + carriedMilliseconds;
        var wholeMilliseconds = (int)tickMilliseconds;
        carriedMilliseconds = tickMilliseconds - wholeMilliseconds;
        return wholeMilliseconds;
    }

    public bool IsTickDue(long nowMilliseconds)
    {
        nextTickAt ??= nowMilliseconds;
        return nowMilliseconds >= nextTickAt;
    }

    public void TickDone(int ticksStillQueued)
    {
        nextTickAt = ScheduledTick() + NextTickMilliseconds(ticksStillQueued);
    }

    public void TickSkipped() => nextTickAt = ScheduledTick() + NextTickMilliseconds(0);

    private long ScheduledTick() =>
        nextTickAt ?? throw new InvalidOperationException("Check IsTickDue before moving on to the next tick.");

    private void RecalculateTarget()
    {
        MeasuredPeriodMilliseconds = (double)windowSpanMilliseconds / gapCount;
        if (MeasuredPeriodMilliseconds < 1.0)
        {
            return;
        }

        Array.Copy(gaps, sortedGaps, gapCount);
        Array.Sort(sortedGaps, 0, gapCount);
        var slowGap = sortedGaps[(gapCount - 1) * GapPercentile / 100];

        // A gap of N periods drains N-1 ticks from the queue before the next one lands. The tolerance
        // stops a gap of exactly N periods rounding up a whole tick on measurement noise.
        var neededTicks = (int)Math.Ceiling(slowGap / MeasuredPeriodMilliseconds - GapTolerancePeriods) - 1;
        neededTicks = Math.Clamp(neededTicks, TargetMinimum, TargetMaximum);

        // Grow at once so the next hiccup is covered; shrink one step at a time so a calm spell can't
        // strip the cushion.
        if (neededTicks > BufferTarget)
        {
            BufferTarget = neededTicks;
            arrivalsSinceTargetChange = 0;
        }
        else if (neededTicks < BufferTarget && arrivalsSinceTargetChange >= TargetLowerAfterArrivals)
        {
            BufferTarget--;
            arrivalsSinceTargetChange = 0;
        }
    }

    private void ResetMeasurement()
    {
        Array.Clear(gaps);
        arrivalsSinceRecalculate = 0;
        arrivalsSinceTargetChange = 0;
        carriedMilliseconds = 0;
        gapCount = 0;
        gapHead = 0;
        lastArrival = null;
        windowSpanMilliseconds = 0;
        BufferTarget = TargetStart;
        MeasuredPeriodMilliseconds = 0;
    }
}
