using System.Diagnostics;
using GdUnit4;
using TheLastGate.Game;
using TheLastGate.Net;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Game;

[TestSuite]
public class TickPlaybackTests
{
    [TestCase]
    public void PlayDueTicks_PlaysTheFirstTickAtOnce()
    {
        var playback = new TickPlayback(new TickPacer());
        playback.Receive(Tick(1, 1000));

        var played = PlayDue(playback, 1000);

        AssertThat(Markers(played)).ContainsExactly(1);
        AssertThat(playback.QueuedTicks).IsEqual(0);
    }

    [TestCase]
    public void PlayDueTicks_SpacesTicksByThePacersPace()
    {
        var playback = new TickPlayback(new TickPacer());
        ReceiveTicks(playback, 4, 1000);

        AssertThat(Markers(PlayDue(playback, 1000))).ContainsExactly(0);
        AssertThat(PlayDue(playback, 1052)).IsEmpty();
        AssertThat(Markers(PlayDue(playback, 1053))).ContainsExactly(1);
    }

    [TestCase]
    public void PlayDueTicks_CatchesUpAfterAStall()
    {
        var playback = new TickPlayback(new TickPacer());
        ReceiveTicks(playback, 4, 1000);
        PlayDue(playback, 1000);

        var played = PlayDue(playback, 2000);

        AssertThat(Markers(played)).ContainsExactly(1, 2, 3);
    }

    [TestCase]
    public void PlayDueTicks_KeepsTheScheduleMovingWhileNothingIsQueued()
    {
        var playback = new TickPlayback(new TickPacer());
        playback.Receive(Tick(0, 1000));
        PlayDue(playback, 1000);
        PlayDue(playback, 1500);

        ReceiveTicks(playback, 2, 1500);

        AssertThat(PlayDue(playback, 1519)).IsEmpty();
        AssertThat(PlayDue(playback, 1520)).HasSize(1);
    }

    [TestCase]
    public void Receive_GivesThePacerEachTicksArrivalTime()
    {
        var pacer = new TickPacer { Adaptive = true };
        var playback = new TickPlayback(pacer);

        for (var tickIndex = 0; tickIndex < 1000; tickIndex++)
        {
            playback.Receive(Tick(0, 1000 + tickIndex * 21));
        }

        AssertThat(pacer.MeasuredPeriodMilliseconds).IsBetween(20.5, 21.5);
    }

    private static ReceivedTick Tick(byte marker, long arrivedAtMilliseconds) =>
        new([marker], arrivedAtMilliseconds * Stopwatch.Frequency / 1000);

    private static void ReceiveTicks(TickPlayback playback, int count, long arrivedAtMilliseconds)
    {
        for (var tickIndex = 0; tickIndex < count; tickIndex++)
        {
            playback.Receive(Tick((byte)tickIndex, arrivedAtMilliseconds));
        }
    }

    private static int[] Markers(List<byte[]> played) => played.Select(tick => (int)tick[0]).ToArray();

    private static List<byte[]> PlayDue(TickPlayback playback, long nowMilliseconds)
    {
        var played = new List<byte[]>();
        playback.PlayDueTicks(nowMilliseconds, played.Add);
        return played;
    }
}
