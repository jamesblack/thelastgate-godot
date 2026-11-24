using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class StubbornUseTests
{
    private const int UseAction = 4;
    private const int Pentagram = 1000;
    private const int ActivePentagram = 1001;

    private static readonly PlayerTarget Idle = new();
    private static readonly PlayerTarget Fighting = new(77, 0, 0, 0, 0, 0);

    private TextLog chat = new();
    private List<byte[]> sent = [];
    private List<(int X, int Y)> settled = [];
    private StubbornUse stubborn = null!;

    [BeforeTest]
    public void StartAUse()
    {
        chat = new TextLog();
        sent = [];
        settled = [];
        stubborn = new StubbornUse(chat) { Enabled = true, Send = sent.Add };
        stubborn.Settled += (x, y) => settled.Add((x, y));
        stubborn.Sent(Use(10, 20));
    }

    [TestCase]
    public void AUseThatPlaysOutOnAClearTile_Counts()
    {
        Tick(Using(10, 20), UseSight.None);
        Tick(Idle, Animating(10, 20), 2);
        Tick(Idle, After(10, 20));
        AssertThat(settled).IsEmpty();

        Tick(Idle, After(10, 20));

        AssertThat(settled).ContainsExactly((10, 20));
        AssertThat(sent).IsEmpty();
        AssertThat(stubborn.IsPending).IsFalse();
    }

    [TestCase]
    public void ACharacterOnTheTile_FailsTheUse()
    {
        Tick(Idle, Animating(10, 20, true));

        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1) - 1);
        AssertThat(sent).IsEmpty();
        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
        AssertThat(sent[0]).ContainsExactly(Use(10, 20));
        AssertThat(settled).IsEmpty();
    }

    [TestCase]
    public void OneAnimationOnAnOccupiedTile_IsOneFailure()
    {
        Tick(Idle, Animating(10, 20, true), 4);
        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1) - 3);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void OnlyAFailedUse_WaitsToRetry()
    {
        AssertThat(stubborn.IsWaitingToRetry).IsFalse();

        Tick(Idle, Animating(10, 20, true));
        AssertThat(stubborn.IsWaitingToRetry).IsTrue();

        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1));
        AssertThat(sent).HasSize(1);
        AssertThat(stubborn.IsWaitingToRetry).IsFalse();
    }

    [TestCase]
    public void ACharacterSteppingOnAfterTheAnimation_FailsTheUse()
    {
        Tick(Idle, Animating(10, 20));
        Tick(Idle, After(10, 20));
        Tick(Idle, After(10, 20, true));

        AssertThat(settled).IsEmpty();
        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1));
        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void ACreatureSpawningOnTheChangedTile_Counts()
    {
        Tick(Idle, Animating(10, 20));
        Tick(Idle, After(10, 20, true, ActivePentagram));

        AssertThat(settled).ContainsExactly((10, 20));
        Tick(Idle, UseSight.None, 200);
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void ATileThatChangesDuringTheAnimation_Counts()
    {
        Tick(Idle, Animating(10, 20));
        Tick(Idle, Animating(10, 20, true, ActivePentagram));

        AssertThat(settled).ContainsExactly((10, 20));
        AssertThat(stubborn.IsWaitingToRetry).IsFalse();
    }

    [TestCase]
    public void ATileSeenWhileTheServerWalksToIt_IsWhatTheChangeIsMeasuredFrom()
    {
        Tick(Using(10, 20), After(10, 20, false, ActivePentagram), 5);
        Tick(Idle, Animating(10, 20, false, ActivePentagram));
        Tick(Idle, After(10, 20, true, ActivePentagram));

        AssertThat(settled).IsEmpty();
        AssertThat(stubborn.IsWaitingToRetry).IsTrue();
    }

    [TestCase]
    public void AnAttack_ClearsAPlannedRetry()
    {
        Tick(Idle, Animating(10, 20, true));
        AssertThat(stubborn.IsWaitingToRetry).IsTrue();

        stubborn.Sent(MapCommandPackets.ForCharacter(ClientCommand.Attack, 5));

        AssertThat(stubborn.IsWaitingToRetry).IsFalse();
        AssertThat(stubborn.IsPending).IsFalse();
        Tick(Idle, UseSight.None, 200);
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void AUseFacingAnotherTile_DoesNotCount()
    {
        Tick(Idle, Animating(11, 20), StubbornUse.StartGraceTicks + 1);

        AssertThat(settled).IsEmpty();
        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1));
        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void AUseTheServerShowsWithinTheGrace_IsNotAFailure()
    {
        Tick(Idle, UseSight.None, StubbornUse.StartGraceTicks);
        Tick(Using(10, 20), UseSight.None, StubbornUse.RetryDelayTicks(1) + 5);

        AssertThat(sent).IsEmpty();
        AssertThat(stubborn.IsPending).IsTrue();
    }

    [TestCase]
    public void AUseTheServerNeverShows_FailsAfterTheGrace()
    {
        Tick(Idle, UseSight.None, StubbornUse.StartGraceTicks + 1);
        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1));

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void AUseTheServerIsStillWalkingTo_IsNotAFailure()
    {
        Tick(Using(10, 20), UseSight.None, 200);

        AssertThat(sent).IsEmpty();
        AssertThat(stubborn.IsPending).IsTrue();
    }

    [TestCase]
    public void AUseTheServerKeepsThroughAFight_IsNotAFailure()
    {
        Tick(new PlayerTarget(77, 0, 0, UseAction, 10, 20), UseSight.None, 100);

        AssertThat(stubborn.IsWaitingToRetry).IsFalse();
        AssertThat(stubborn.IsPending).IsTrue();
    }

    [TestCase]
    public void AUseDroppedDuringAFight_FailsWithoutWaitingForTheFightToEnd()
    {
        Tick(Fighting, UseSight.None, StubbornUse.StartGraceTicks + 1);

        AssertThat(stubborn.IsWaitingToRetry).IsTrue();
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void RetriesWaitOutAFight()
    {
        Tick(Idle, Animating(10, 20, true));
        Tick(Fighting, UseSight.None, 200);
        Tick(Idle, UseSight.None, StubbornUse.AfterFightTicks);
        AssertThat(sent).IsEmpty();

        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void AShortFight_KeepsTheRetryDelayRunning()
    {
        Tick(Idle, Animating(10, 20, true));
        Tick(Fighting, UseSight.None, 2);
        Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(1) - 3);
        AssertThat(sent).IsEmpty();

        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void ALullBetweenTargets_IsStillTheFight()
    {
        Tick(Idle, Animating(10, 20, true));
        Tick(Fighting, UseSight.None, StubbornUse.RetryDelayTicks(1));
        Tick(Idle, UseSight.None, StubbornUse.AfterFightTicks);
        Tick(Fighting, UseSight.None, 5);
        Tick(Idle, UseSight.None, StubbornUse.AfterFightTicks);
        AssertThat(sent).IsEmpty();

        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void FightsEightTicksApart_HoldTheRetryUntilTheLastCoolsOff()
    {
        Tick(Idle, Animating(10, 20, true));
        for (var fight = 0; fight < 4; fight++)
        {
            Tick(Fighting, UseSight.None, 15);
            Tick(Idle, UseSight.None, 8);
        }

        AssertThat(sent).IsEmpty();
        Tick(Idle, UseSight.None, StubbornUse.AfterFightTicks - 8);
        AssertThat(sent).IsEmpty();

        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void AUseDroppedDuringAFight_RetriesOnlyAfterTheQuietPeriod()
    {
        Tick(Fighting, UseSight.None, 100);
        Tick(Idle, UseSight.None, StubbornUse.AfterFightTicks);
        AssertThat(sent).IsEmpty();

        Tick(Idle, UseSight.None);

        AssertThat(sent).HasSize(1);
    }

    [TestCase]
    public void EightFailures_GiveUp()
    {
        for (var failures = 1; failures <= StubbornUse.MostFailures; failures++)
        {
            Tick(Idle, Animating(10, 20, true));
            if (failures < StubbornUse.MostFailures)
            {
                Tick(Idle, UseSight.None, StubbornUse.RetryDelayTicks(failures));
            }
        }

        AssertThat(sent).HasSize(StubbornUse.MostFailures - 1);
        AssertThat(settled).ContainsExactly((10, 20));
        AssertThat(stubborn.IsPending).IsFalse();
        AssertThat(chat.Lines[^1]).IsEqual(new LogLine("Gave up on that use after 8 tries.", (int)TextColour.Grey));
    }

    [TestCase]
    public void AnotherAction_CancelsWithoutSettling()
    {
        stubborn.Sent(MapCommandPackets.ForTile(ClientCommand.Move, 1, 1));

        AssertThat(stubborn.IsPending).IsFalse();
        Tick(Idle, UseSight.None, 200);
        AssertThat(settled).IsEmpty();
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void ANewUse_ReplacesThePendingOne()
    {
        stubborn.Sent(Use(11, 20));

        Tick(Idle, Animating(11, 20));
        Tick(Idle, After(11, 20), 2);

        AssertThat(settled).ContainsExactly((11, 20));
    }

    [TestCase]
    public void TurningItOff_ReleasesThePendingUse()
    {
        stubborn.Enabled = false;

        AssertThat(stubborn.IsPending).IsFalse();
        AssertThat(settled).ContainsExactly((10, 20));
        Tick(Idle, UseSight.None, 200);
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void AUseThatShowsUpAfterTheGrace_SettlesWithoutARetry()
    {
        Tick(Idle, UseSight.None, StubbornUse.StartGraceTicks + 1);

        Tick(Idle, Animating(10, 20));
        Tick(Idle, After(10, 20), 2);

        AssertThat(settled).ContainsExactly((10, 20));
        AssertThat(sent).IsEmpty();
    }

    [TestCase]
    public void TheServerTakingTheUseAfterTheGrace_StopsTheRetry()
    {
        Tick(Idle, UseSight.None, StubbornUse.StartGraceTicks + 1);

        Tick(Using(10, 20), UseSight.None, 200);

        AssertThat(sent).IsEmpty();
        AssertThat(stubborn.IsPending).IsTrue();
    }

    [TestCase]
    public void TheStartGrace_CoversASecondOfLatency()
    {
        Tick(Idle, UseSight.None, 20);

        Tick(Idle, Animating(10, 20));
        Tick(Idle, After(10, 20), 2);

        AssertThat(settled).ContainsExactly((10, 20));
    }

    [TestCase]
    public void AUseRightAfterAFight_Counts()
    {
        Tick(Fighting, UseSight.None, 5);

        Tick(Idle, Animating(10, 20));
        Tick(Idle, After(10, 20), 2);

        AssertThat(settled).ContainsExactly((10, 20));
        AssertThat(sent).IsEmpty();
    }

    [TestCase(ClientCommand.Attack)]
    [TestCase(ClientCommand.Give)]
    public void AnAttackOrAGive_CancelsThePendingUse(ClientCommand command)
    {
        stubborn.Sent(MapCommandPackets.ForCharacter(command, 5));

        AssertThat(stubborn.IsPending).IsFalse();
    }

    [TestCase]
    public void AnInventoryClick_KeepsThePendingUse()
    {
        stubborn.Sent(MapCommandPackets.ForCharacter(ClientCommand.Inventory, 5));

        AssertThat(stubborn.IsPending).IsTrue();
    }

    [TestCase]
    public void WhileOff_UsesAreNotWatched()
    {
        var off = new StubbornUse(chat);

        off.Sent(Use(10, 20));

        AssertThat(off.IsPending).IsFalse();
    }

    [TestCase]
    public void RetryDelay_StartsAtOneSecondAndNeverShrinks()
    {
        AssertThat(StubbornUse.RetryDelayTicks(1)).IsGreaterEqual(20);
        for (var failures = 2; failures < StubbornUse.MostFailures; failures++)
        {
            AssertThat(StubbornUse.RetryDelayTicks(failures))
                .IsGreaterEqual(StubbornUse.RetryDelayTicks(failures - 1));
        }
    }

    private void Tick(PlayerTarget target, UseSight sight, int ticks = 1)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            stubborn.EndOfTick(target, sight);
        }
    }

    private static PlayerTarget Using(int x, int y) => new(0, 0, 0, UseAction, x, y);

    private static UseSight Animating(int x, int y, bool occupied = false, int itemSprite = Pentagram) =>
        new(true, x, y, occupied, itemSprite);

    private static UseSight After(int x, int y, bool occupied = false, int itemSprite = Pentagram) =>
        new(false, x, y, occupied, itemSprite);

    private static byte[] Use(int x, int y) => MapCommandPackets.ForTile(ClientCommand.Use, x, y);
}
