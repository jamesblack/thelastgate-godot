using TheLastGate.State;

namespace TheLastGate.Tests;

public class SpectateWatchTests
{
    private const int Own = 11;
    private const int Target = 22;
    private const int OtherTarget = 33;
    private const string StartOnBob = "Now spectating Bob. Use /spectate self to return.";
    private const string StartOnCarl = "Now spectating Carl. Use /spectate self to return.";

    private readonly SpectateWatch watch = new();

    [Fact]
    public void OrdinaryLines_ChangeNothing()
    {
        watch.LogLine("Bob says: \"Now spectating nothing\"", Own);
        watch.ViewCentre(Target);

        Assert.False(watch.Active);
    }

    [Fact]
    public void TheStartMessage_PausesBeforeTheViewMoves()
    {
        watch.LogLine(StartOnBob, Own);
        Assert.True(watch.Active);

        watch.ViewCentre(Own);
        Assert.True(watch.Active);

        watch.ViewCentre(Target);
        Assert.True(watch.Active);
    }

    [Fact]
    public void TheViewReturningToTheOwnCharacter_EndsSpectating()
    {
        watch.LogLine(StartOnBob, Own);
        watch.ViewCentre(Target);
        watch.ViewCentre(0);
        Assert.True(watch.Active);

        watch.ViewCentre(Own);
        Assert.False(watch.Active);
    }

    [Fact]
    public void TheStopMessage_EndsSpectating()
    {
        watch.LogLine(StartOnBob, Own);
        watch.ViewCentre(Target);

        watch.LogLine("No longer spectating.", Target);

        Assert.False(watch.Active);
    }

    [Fact]
    public void SwitchingTarget_KeepsTheOwnCharacter()
    {
        watch.LogLine(StartOnBob, Own);
        watch.ViewCentre(Target);
        watch.LogLine(StartOnCarl, Target);
        watch.ViewCentre(Target);
        watch.ViewCentre(OtherTarget);
        Assert.True(watch.Active);

        watch.ViewCentre(Own);
        Assert.False(watch.Active);
    }

    [Fact]
    public void AnUnknownOwnCharacter_WaitsForTheStopMessage()
    {
        watch.LogLine(StartOnBob, 0);
        watch.ViewCentre(Target);
        watch.ViewCentre(Own);
        Assert.True(watch.Active);

        watch.LogLine("No longer spectating.", Own);
        Assert.False(watch.Active);
    }

    [Fact]
    public void TheTargetLeavingTheGame_EndsSpectating()
    {
        watch.LogLine(StartOnBob, Own);
        watch.ViewCentre(Target);

        watch.LogLine("Bobby left the game.", Target);
        Assert.True(watch.Active);
        watch.LogLine("Carl left the game.", Target);
        Assert.True(watch.Active);
        watch.LogLine("Bob left the game.", Target);
        Assert.False(watch.Active);
    }

    [Fact]
    public void TheTargetLeavingWithoutGoodbye_EndsSpectating()
    {
        watch.LogLine(StartOnBob, Own);
        watch.ViewCentre(Target);

        watch.LogLine("Bob left the game without saying goodbye.", Target);

        Assert.False(watch.Active);
    }

    [Fact]
    public void SwitchingTarget_WatchesTheNewName()
    {
        watch.LogLine(StartOnBob, Own);
        watch.LogLine(StartOnCarl, Target);

        watch.LogLine("Bob left the game.", OtherTarget);
        Assert.True(watch.Active);
        watch.LogLine("Carl left the game.", OtherTarget);
        Assert.False(watch.Active);
    }

    [Fact]
    public void LeavingTheGame_IsIgnoredWhenNotSpectating()
    {
        watch.LogLine(" left the game.", Own);

        Assert.False(watch.Active);
    }

    [Fact]
    public void Reset_StopsSpectating()
    {
        watch.LogLine(StartOnBob, Own);

        watch.Reset();

        Assert.False(watch.Active);
    }
}
