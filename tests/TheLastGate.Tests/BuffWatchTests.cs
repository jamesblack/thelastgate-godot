using TheLastGate.Buffs;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class BuffWatchTests
{
    private static readonly BuffSlot Empty = new(0, 0);
    private static readonly BuffSlot Bless16 = new(BuffList.BlessSprite, 16);

    private readonly PlayerState player = new();
    private readonly List<string> said = [];
    private readonly BuffWatch watch;

    public BuffWatchTests() => watch = new BuffWatch(player) { Say = said.Add };

    [Fact]
    public void AChange_SendsOneRequest()
    {
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        watch.EndOfTick();

        Assert.Equal([BuffWatch.Command], said);
    }

    [Fact]
    public void TheReply_IsCapturedAndKept()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 142 for 4m 10s");

        Assert.Equal(142, watch.Reading(BuffKind.Bless)?.Power);
        Assert.False(watch.Waiting);
        Assert.True(watch.IsActive(BuffKind.Bless));
        Assert.False(watch.IsActive(BuffKind.Enhance));
    }

    [Fact]
    public void AReplyThePlayerAskedFor_IsLeftForTheChat()
    {
        Assert.False(watch.Capture(Line(BuffList.Header)));
        Assert.False(watch.Capture(Line("Bless power of 1 for 0m 1s")));
    }

    [Fact]
    public void ABuffGoing_ClearsItsReadingWithoutARequest()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 142 for 4m 10s");
        RunTicks(120);

        player.Buffs[0] = Empty;
        watch.SpellPacket(0, Bless16, Empty);
        RunTicks(200);

        Assert.Null(watch.Reading(BuffKind.Bless));
        Assert.Single(said);
    }

    // The server writes the cast message straight away but sends the slot on its next character update, a few ticks on.
    [Fact]
    public void ACastMessageThenItsSlotPacket_SendsOneRequest()
    {
        Assert.False(watch.Capture(Line("You have been blessed.")));
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        RunTicks(5);
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        RunTicks(200);

        Assert.Single(said);
        Assert.Equal(60, watch.Reading(BuffKind.Bless)?.Power);
    }

    [Fact]
    public void ARecastLongAfterItsCastMessage_FetchesAgain()
    {
        Assert.False(watch.Capture(Line("You have been blessed.")));
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        RunTicks(120);

        watch.SpellPacket(0, new BuffSlot(BuffList.BlessSprite, 10), Bless16);
        RunTicks(2);

        Assert.Equal(2, said.Count);
    }

    [Fact]
    public void ACastMessageForABuffThatHadGone_HidesItsOldReading()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 142 for 4m 10s");
        player.Buffs[0] = Empty;
        watch.SpellPacket(0, Bless16, Empty);
        RunTicks(120);

        Assert.False(watch.Capture(Line("You have been blessed.")));
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);

        Assert.True(watch.Waiting);
        Assert.Null(watch.Reading(BuffKind.Bless));
    }

    [Fact]
    public void AChangeWhileARequestIsOut_FetchesAgain()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);

        // The recast lands a few ticks later, while the first request is still unanswered.
        RunTicks(5);
        Assert.False(watch.Capture(Line("You have been blessed.")));
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        RunTicks(105);

        Assert.Equal(2, said.Count);

        // A recast keeps the last reading on show while the new one is fetched.
        Assert.False(watch.Waiting);
        Assert.Equal(60, watch.Reading(BuffKind.Bless)?.Power);
    }

    [Fact]
    public void ATimeout_StopsCapturing()
    {
        watch.SpellPacket(0, Empty, Bless16);
        RunTicks(32);

        Assert.False(watch.Capture(Line(BuffList.Header)));
        Assert.True(watch.Waiting);
        Assert.Single(said);
    }

    [Fact]
    public void ACapture_EndsWithTheTick()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        Assert.True(watch.Capture(Line(BuffList.Header)));
        Assert.True(watch.Capture(Line(" ")));
        Assert.True(watch.Capture(Line("Bless power of 77 for 4m 10s")));
        watch.EndOfTick();

        Assert.False(watch.Capture(Line("You hit the goblin.")));
        Assert.NotNull(watch.Reading(BuffKind.Bless));
    }

    // The server resends a slot unchanged every 16 ticks when its buff number is 32768 or higher.
    [Fact]
    public void AnUnchangedResend_NeverFetches()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");

        for (var tick = 0; tick < 2000; tick++)
        {
            if (tick % 16 == 0)
            {
                watch.SpellPacket(0, Bless16, Bless16);
            }

            watch.EndOfTick();
        }

        Assert.Single(said);
    }

    [Fact]
    public void ACastMessage_FetchesAndStaysInTheChat()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        RunTicks(120);

        Assert.False(watch.Capture(Line("Ishtar cast bless on you.")));
        RunTicks(2);

        Assert.Equal(2, said.Count);
        Assert.False(watch.Waiting);
    }

    [Fact]
    public void ACastMessageWithItsSlotPacket_SendsOneRequest()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        Assert.False(watch.Capture(Line("You have been blessed.")));
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        RunTicks(200);

        Assert.Single(said);
    }

    [Fact]
    public void AFloodReply_IsSwallowedAndRetried()
    {
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();

        Assert.True(watch.Capture(new LogLine(BuffWatch.FloodReply, 0)));
        RunTicks(45);

        Assert.Equal(2, said.Count);
    }

    [Fact]
    public void Timeouts_RetryTwiceThenGiveUp()
    {
        watch.SpellPacket(0, Empty, Bless16);
        RunTicks(1000);

        Assert.Equal(3, said.Count);
        Assert.True(watch.Waiting);
    }

    [Fact]
    public void Requests_AreSpacedApart()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 60 for 4m 10s");
        RunTicks(5);

        Assert.False(watch.Capture(Line("You have been blessed.")));
        RunTicks(10);
        Assert.Single(said);

        RunTicks(100);
        Assert.Equal(2, said.Count);
    }

    [Fact]
    public void AFloodReplyToTheirOwnTyping_IsLeftForTheChat()
    {
        Assert.False(watch.Capture(new LogLine(BuffWatch.FloodReply, 0)));
    }

    // A refresh, such as an aura or a group member recasting, mustn't blank the numbers.
    [Fact]
    public void ARefresh_KeepsTheReadingShown()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 142 for 4m 10s");
        RunTicks(5);

        watch.SpellPacket(0, new BuffSlot(BuffList.BlessSprite, 10), Bless16);

        Assert.False(watch.Waiting);
        Assert.Equal(142, watch.Reading(BuffKind.Bless)?.Power);
        RunTicks(120);
        Assert.Equal(2, said.Count);
    }

    [Fact]
    public void ANewBuff_WaitsForItsReading()
    {
        player.Buffs[0] = Bless16;
        watch.SpellPacket(0, Empty, Bless16);
        watch.EndOfTick();
        DeliverBlessReply("Bless power of 142 for 4m 10s");
        RunTicks(5);

        var enhance = new BuffSlot(BuffList.EnhanceSprite, 16);
        player.Buffs[1] = enhance;
        watch.SpellPacket(1, Empty, enhance);

        Assert.True(watch.Waiting);
        Assert.Null(watch.Reading(BuffKind.Enhance));
    }

    private static LogLine Line(string text) => new(text, 1);

    private void DeliverBlessReply(string powerLine)
    {
        Assert.True(watch.Capture(Line(BuffList.Header)));
        Assert.True(watch.Capture(Line(" ")));
        Assert.True(watch.Capture(Line(powerLine)));
        Assert.True(watch.Capture(Line(" ")));
    }

    private void RunTicks(int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            watch.EndOfTick();
        }
    }
}
