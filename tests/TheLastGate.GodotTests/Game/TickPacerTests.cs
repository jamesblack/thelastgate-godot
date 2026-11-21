using GdUnit4;
using TheLastGate.Game;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Game;

[TestSuite]
public class TickPacerTests
{
    [TestCase]
    public void Adaptive_IsOffByDefault()
    {
        AssertThat(new TickPacer().Adaptive).IsFalse();
    }

    [TestCase(8, 20)]
    [TestCase(4, 40)]
    [TestCase(3, 53)]
    [TestCase(1, 160)]
    [TestCase(16, 10)]
    public void NextTickMilliseconds_WhenFixed_SpreadsEightNominalTicksOverTheQueue(int queuedTicks, int expected)
    {
        AssertThat(new TickPacer().NextTickMilliseconds(queuedTicks)).IsEqual(expected);
    }

    [TestCase]
    public void NextTickMilliseconds_WhenFixed_ChecksAgainAfterANominalTickWhenNothingIsQueued()
    {
        AssertThat(new TickPacer().NextTickMilliseconds(0)).IsEqual(TickPacer.NominalTickMilliseconds);
    }

    [TestCase]
    public void Adaptive_StartsWithATargetOfFourTicks()
    {
        AssertThat(AdaptivePacer().BufferTarget).IsEqual(4);
    }

    [TestCase]
    public void Adaptive_SettlesAtTheMinimumTargetOnASteadyConnection()
    {
        var pacer = AdaptivePacer();

        FeedSteady(pacer, 1000, 1000, 21);

        AssertThat(pacer.BufferTarget).IsEqual(1);
        AssertThat(pacer.MeasuredPeriodMilliseconds).IsBetween(20.5, 21.5);
    }

    [TestCase]
    public void Adaptive_RaisesTheTargetOnABurstyConnection()
    {
        var pacer = AdaptivePacer();
        var now = FeedSteady(pacer, 1000, 1000, 21);

        FeedBursts(pacer, now, 200, 3, 63);

        AssertThat(pacer.BufferTarget).IsEqual(2);
    }

    [TestCase]
    public void Adaptive_LowersTheTargetSlowlyAfterCalmReturns()
    {
        var pacer = AdaptivePacer();
        var now = FeedBursts(pacer, 1000, 200, 5, 105);
        AssertThat(pacer.BufferTarget).IsEqual(4);

        now = FeedSteady(pacer, now, 300, 21);
        AssertThat(pacer.BufferTarget).IsBetween(2, 3);

        FeedSteady(pacer, now, 1000, 21);
        AssertThat(pacer.BufferTarget).IsEqual(1);
    }

    [TestCase]
    public void Adaptive_PlaysAtTheMeasuredPeriodWhenTheQueueIsOnTarget()
    {
        var pacer = AdaptivePacer();
        FeedSteady(pacer, 1000, 1000, 21);

        var totalMilliseconds = Enumerable.Range(0, 100).Sum(_ => pacer.NextTickMilliseconds(1));

        AssertThat(totalMilliseconds).IsBetween(2095, 2105);
    }

    [TestCase]
    public void Adaptive_NudgesTheRateTowardTheTarget()
    {
        var pacer = AdaptivePacer();
        FeedSteady(pacer, 1000, 1000, 20);

        AssertThat(pacer.NextTickMilliseconds(3) < 20).IsTrue();
        AssertThat(pacer.NextTickMilliseconds(0) > 20).IsTrue();
    }

    [TestCase]
    public void Adaptive_ClampsTheRate()
    {
        var pacer = AdaptivePacer();
        FeedSteady(pacer, 1000, 1000, 20);

        AssertThat(pacer.NextTickMilliseconds(100)).IsEqual(10);
        AssertThat(pacer.NextTickMilliseconds(0) <= 25).IsTrue();
    }

    [TestCase]
    public void Adaptive_BarelyMovesThePeriodAfterALongStall()
    {
        var pacer = AdaptivePacer();
        var now = FeedSteady(pacer, 1000, 1000, 20);

        FeedSteady(pacer, now + 2000, 32, 20);

        AssertThat(pacer.MeasuredPeriodMilliseconds < 21.0).IsTrue();
    }

    [TestCase]
    public void Adaptive_UsesTheNominalTickBeforeAnyMeasurement()
    {
        AssertThat(AdaptivePacer().NextTickMilliseconds(4)).IsEqual(TickPacer.NominalTickMilliseconds);
    }

    [TestCase]
    public void Adaptive_StartsMeasuringAfreshWhenTurnedOn()
    {
        var pacer = AdaptivePacer();
        FeedSteady(pacer, 1000, 1000, 21);

        pacer.Adaptive = false;
        pacer.Adaptive = true;

        AssertThat(pacer.BufferTarget).IsEqual(4);
        AssertThat(pacer.MeasuredPeriodMilliseconds).IsEqual(0.0);
    }

    [TestCase]
    public void IsTickDue_IsTrueAtTheFirstCheck()
    {
        AssertThat(new TickPacer().IsTickDue(5000)).IsTrue();
    }

    [TestCase]
    public void TickDone_SchedulesTheNextTickAtTheCurrentPace()
    {
        var pacer = new TickPacer();
        pacer.IsTickDue(1000);

        pacer.TickDone(4);

        AssertThat(pacer.IsTickDue(1039)).IsFalse();
        AssertThat(pacer.IsTickDue(1040)).IsTrue();
    }

    [TestCase]
    public void TickSkipped_MovesTheScheduleOn()
    {
        var pacer = new TickPacer();
        pacer.IsTickDue(1000);

        pacer.TickSkipped();

        AssertThat(pacer.IsTickDue(1019)).IsFalse();
        AssertThat(pacer.IsTickDue(1020)).IsTrue();
    }

    private static TickPacer AdaptivePacer() => new() { Adaptive = true };

    private static long FeedSteady(TickPacer pacer, long now, int arrivals, int gapMilliseconds)
    {
        for (var arrivalIndex = 0; arrivalIndex < arrivals; arrivalIndex++)
        {
            pacer.OnArrival(now);
            now += gapMilliseconds;
        }

        return now;
    }

    private static long FeedBursts(TickPacer pacer, long now, int bursts, int ticksPerBurst, int gapMilliseconds)
    {
        for (var burstIndex = 0; burstIndex < bursts; burstIndex++)
        {
            for (var tickIndex = 0; tickIndex < ticksPerBurst; tickIndex++)
            {
                pacer.OnArrival(now);
            }

            now += gapMilliseconds;
        }

        return now;
    }
}
