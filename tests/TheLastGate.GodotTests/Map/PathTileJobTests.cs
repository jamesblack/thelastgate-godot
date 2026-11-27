using GdUnit4;
using TheLastGate.Map;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class PathTileJobTests
{
    private static readonly ShownPath Across = new([new PathSegment(0, 0, 2, 0)], 0xFF0000);
    private static readonly ShownPath Down = new([new PathSegment(5, 5, 5, 6)], 0x00FF00);

    private readonly List<Action> waiting = [];

    [BeforeTest]
    public void ForgetWaitingWork() => waiting.Clear();

    [TestCase]
    public void Rebuild_ThenTakeReady_SwapsInTheColours()
    {
        var job = new PathTileJob(PathTiles.Colours, work => work());

        job.Rebuild([Across]);

        AssertThat(job.TakeReady()).IsTrue();
        AssertThat(job.Current.Keys.ToArray()).ContainsExactlyInAnyOrder((0, 0), (1, 0), (2, 0));
        AssertThat(job.TakeReady()).IsFalse();
    }

    [TestCase]
    public void Current_KeepsTheOldColoursUntilTheRebuildIsTaken()
    {
        var job = new PathTileJob(PathTiles.Colours, waiting.Add);
        job.Rebuild([Across]);
        waiting[0]();
        job.TakeReady();

        job.Rebuild([Down]);
        AssertThat(job.TakeReady()).IsFalse();
        waiting[1]();

        AssertThat(job.Current.ContainsKey((0, 0))).IsTrue();
        AssertThat(job.TakeReady()).IsTrue();
        AssertThat(job.Current.Keys.ToArray()).ContainsExactlyInAnyOrder((5, 5), (5, 6));
    }

    [TestCase]
    public void AnOlderRebuild_FinishingLate_IsDropped()
    {
        var job = new PathTileJob(PathTiles.Colours, waiting.Add);
        job.Rebuild([Across]);
        job.Rebuild([Down]);

        waiting[1]();
        waiting[0]();

        AssertThat(job.TakeReady()).IsTrue();
        AssertThat(job.Current.Keys.ToArray()).ContainsExactlyInAnyOrder((5, 5), (5, 6));
    }

    [TestCase]
    public void Clear_EmptiesAtOnceAndDropsARunningRebuild()
    {
        var job = new PathTileJob(PathTiles.Colours, waiting.Add);
        job.Rebuild([Across]);
        waiting[0]();
        job.TakeReady();
        job.Rebuild([Down]);

        job.Clear();
        waiting[1]();

        AssertThat(job.Current.Count).IsEqual(0);
        AssertThat(job.TakeReady()).IsFalse();
    }

    [TestCase]
    public void AFailedRebuild_ThrowsWhenTaken()
    {
        var job = new PathTileJob(_ => throw new InvalidOperationException("broken"), work => work());

        job.Rebuild([Across]);

        AssertThrown(() => job.TakeReady()).IsInstanceOf<InvalidOperationException>();
    }
}
