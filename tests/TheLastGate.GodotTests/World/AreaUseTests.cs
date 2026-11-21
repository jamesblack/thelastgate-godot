using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class AreaUseTests
{
    private static PlayerTarget Busy => new(0, 0, 0, 4, 50, 50);

    [TestCase(10, 10, 10, 10)]
    [TestCase(14, 14, 14, 14)]
    [TestCase(15, 15, 14, 14)]
    [TestCase(19, 12, 17, 12)]
    [TestCase(5, 5, 6, 6)]
    [TestCase(10, 40, 10, 34)]
    [TestCase(12, 20, 12, 17)]
    public void FitEnd_KeepsTheAreaToTwentyFiveTiles(int endX, int endY, int fittedX, int fittedY)
    {
        AssertThat(AreaUse.FitEnd((10, 10), (endX, endY))).IsEqual((fittedX, fittedY));
    }

    [TestCase]
    public void QueueArea_ReplacesTheQueuePastTheClickLimit()
    {
        var sent = new List<byte[]>();
        var queue = new UseQueue(new TextLog()) { Enabled = true, Send = sent.Add };
        queue.Add(1, 1, Busy);
        var area = Enumerable.Range(0, 8).Select(x => new AreaTarget(0, x, 5, ClientCommand.PickUp));

        AssertThat(queue.QueueArea(area, Busy)).IsEqual(8);

        AssertThat((queue.PositionOf(1, 1), queue.PositionOf(0, 5), queue.Count)).IsEqual((0, 1, 8));
        AssertThat(queue.Add(9, 9, Busy)).IsEqual(UseQueueAdd.Full);
        queue.TargetChanged(Busy, new PlayerTarget());
        AssertThat(sent).HasSize(1);
        AssertThat(sent[0]).ContainsExactly(MapCommandPackets.ForTile(ClientCommand.PickUp, 0, 5));
    }
}
