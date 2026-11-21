using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class UseQueueTests
{
    private const int Using = 4;
    private const int Idle = 0;

    private TextLog chat = new();
    private UseQueue queue = null!;
    private List<byte[]> sent = [];

    [BeforeTest]
    public void StartAQueue()
    {
        chat = new TextLog();
        sent = [];
        queue = new UseQueue(chat) { Enabled = true, Send = sent.Add };
    }

    [TestCase]
    public void TheFirstUseWhileIdle_IsSentAtOnce()
    {
        AssertThat(queue.Add(10, 20, Target(Idle))).IsEqual(UseQueueAdd.Queued);

        AssertThat(sent).HasSize(1);
        AssertThat(sent[0]).ContainsExactly(Use(10, 20));
        AssertThat(queue.PositionOf(10, 20)).IsEqual(1);
    }

    [TestCase]
    public void UsesQueuedWhileBusy_WaitTheirTurn()
    {
        queue.Add(10, 20, Target(Using, 1, 1));
        queue.Add(11, 20, Target(Using, 1, 1));

        AssertThat(sent).IsEmpty();
        AssertThat((queue.PositionOf(10, 20), queue.PositionOf(11, 20))).IsEqual((1, 2));
    }

    [TestCase]
    public void TheUseInProgress_IsNotQueued()
    {
        AssertThat(queue.Add(10, 20, Target(Using, 10, 20))).IsEqual(UseQueueAdd.Ignored);

        AssertThat(queue.Count).IsEqual(0);
    }

    [TestCase]
    public void AUseAlreadyQueued_IsNotQueuedTwice()
    {
        queue.Add(10, 20, Target(Using, 1, 1));

        AssertThat(queue.Add(10, 20, Target(Using, 1, 1))).IsEqual(UseQueueAdd.Ignored);
        AssertThat(queue.Count).IsEqual(1);
    }

    [TestCase]
    public void ASixthUse_SaysTheQueueIsFull()
    {
        for (var x = 0; x < UseQueue.MostQueued; x++)
        {
            queue.Add(x, 0, Target(Using, 1, 1));
        }

        AssertThat(queue.Add(9, 0, Target(Using, 1, 1))).IsEqual(UseQueueAdd.Full);

        AssertThat(queue.Count).IsEqual(UseQueue.MostQueued);
        AssertThat(chat.Lines).HasSize(1);
        AssertThat(chat.Lines[0]).IsEqual(new LogLine("Your queue is full.", (int)TextColour.Red));
    }

    [TestCase]
    public void WhenTheServerStartsTheFirstUse_ItLeavesTheQueue()
    {
        queue.Add(10, 20, Target(Idle));
        queue.Add(11, 20, Target(Idle));

        queue.TargetChanged(Target(Idle), Target(Using, 10, 20));

        AssertThat((queue.PositionOf(10, 20), queue.PositionOf(11, 20))).IsEqual((0, 1));
    }

    [TestCase]
    public void AnotherUseStarting_LeavesTheQueueAlone()
    {
        queue.Add(10, 20, Target(Using, 1, 1));

        queue.TargetChanged(Target(Idle), Target(Using, 5, 5));

        AssertThat(queue.PositionOf(10, 20)).IsEqual(1);
    }

    [TestCase]
    public void WhenAUseEnds_TheNextIsSent()
    {
        queue.Add(10, 20, Target(Using, 1, 1));
        queue.Add(11, 20, Target(Using, 1, 1));

        queue.TargetChanged(Target(Using, 1, 1), Target(Idle));

        AssertThat(sent).HasSize(1);
        AssertThat(sent[0]).ContainsExactly(Use(10, 20));
    }

    [TestCase]
    public void AnEmptyQueue_SendsNothingWhenAUseEnds()
    {
        queue.TargetChanged(Target(Using, 1, 1), Target(Idle));

        AssertThat(sent).IsEmpty();
    }

    [TestCase(ClientCommand.Move)]
    [TestCase(ClientCommand.Turn)]
    [TestCase(ClientCommand.PickUp)]
    [TestCase(ClientCommand.Drop)]
    [TestCase(ClientCommand.Use)]
    [TestCase(ClientCommand.LookItem)]
    [TestCase(ClientCommand.Shop)]
    [TestCase(ClientCommand.Smith)]
    [TestCase(ClientCommand.Mode)]
    [TestCase(ClientCommand.Reset)]
    [TestCase(ClientCommand.Stat)]
    public void AnOrdinaryCommand_ClearsTheQueue(ClientCommand command)
    {
        queue.Add(10, 20, Target(Using, 1, 1));

        queue.Sent(MapCommandPackets.ForTile(command, 1, 1));

        AssertThat(queue.Count).IsEqual(0);
    }

    [TestCase(ClientCommand.Attack)]
    [TestCase(ClientCommand.Give)]
    [TestCase(ClientCommand.Inventory)]
    [TestCase(ClientCommand.Skill)]
    public void CharacterAndInventoryCommands_KeepTheQueue(ClientCommand command)
    {
        queue.Add(10, 20, Target(Using, 1, 1));

        queue.Sent(MapCommandPackets.ForTile(command, 1, 1));

        AssertThat(queue.Count).IsEqual(1);
    }

    [TestCase]
    public void TurningTheOptionOff_ForgetsTheQueue()
    {
        queue.Add(10, 20, Target(Using, 1, 1));

        queue.Enabled = false;

        AssertThat(queue.Count).IsEqual(0);
    }

    private static PlayerTarget Target(int action, int x = 0, int y = 0) => new(0, 0, 0, action, x, y);

    private static byte[] Use(int x, int y) => MapCommandPackets.ForTile(ClientCommand.Use, x, y);
}
