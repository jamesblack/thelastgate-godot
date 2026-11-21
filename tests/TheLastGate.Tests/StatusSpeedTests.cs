using TheLastGate.State;

namespace TheLastGate.Tests;

public class StatusSpeedTests
{
    private const int Attack = 0;
    private const int Pickup = 1;
    private const int SecondAttack = 5;
    private const int Cast = 9;
    private const int PlayerTextColour = 5;
    private const int OneStepEveryTick = 100;
    private const int OneStepEveryOtherTick = 200;

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(299)]
    public void StepsThisTick_SpreadsTheStepsOfASpeedEvenlyOverTheCycle(int speed)
    {
        var total = 0;
        for (var serverTick = 0; serverTick < StatusSpeed.CycleLength; serverTick++)
        {
            total += StatusSpeed.StepsThisTick(speed, serverTick);
        }

        Assert.Equal(StatusSpeed.SpeedMax - speed, total);
    }

    [Theory]
    [InlineData(OneStepEveryTick, 0, 1)]
    [InlineData(OneStepEveryTick, 117, 1)]
    [InlineData(OneStepEveryOtherTick, 0, 0)]
    [InlineData(OneStepEveryOtherTick, 1, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 2)]
    public void StepsThisTick_RoundsLikeTheCurrentClient(int speed, int serverTick, int steps)
    {
        Assert.Equal(steps, StatusSpeed.StepsThisTick(speed, serverTick));
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(33, 4)]
    [InlineData(34, 2)]
    [InlineData(66, 2)]
    [InlineData(67, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 0)]
    public void StepsThisTick_AddsTheInstantCastBonusToFastCasts(int speed, int bonus)
    {
        Assert.Equal(
            StatusSpeed.StepsThisTick(speed, 3) + bonus,
            StatusSpeed.StepsThisTick(speed, 3, true));
    }

    [Theory]
    [InlineData(150, 50, 100)]
    [InlineData(50, 80, 0)]
    [InlineData(400, 0, 299)]
    public void MoveSteps_UsesTheSpeedLessTheMoveSpeedKeptInRange(int speed, int moveSpeed, int effectiveSpeed)
    {
        for (var serverTick = 0; serverTick < 4; serverTick++)
        {
            Assert.Equal(
                StatusSpeed.StepsThisTick(effectiveSpeed, serverTick),
                StatusSpeed.MoveSteps(speed, moveSpeed, serverTick));
        }
    }

    [Theory]
    [InlineData(Attack)]
    [InlineData(SecondAttack)]
    public void ActionSteps_AttacksUseTheSpeedLessTheAttackSpeed(int action)
    {
        Assert.Equal(
            StatusSpeed.StepsThisTick(OneStepEveryOtherTick, 1),
            StatusSpeed.ActionSteps(action, 250, 50, 999, 0, 1));
    }

    [Fact]
    public void ActionSteps_CastsUseTwiceTheSpeedLessTwiceTheCastSpeed()
    {
        Assert.Equal(
            StatusSpeed.StepsThisTick(OneStepEveryTick, 1),
            StatusSpeed.ActionSteps(Cast, 100, 999, 50, 0, 1));
    }

    [Fact]
    public void ActionSteps_GivesOnlyPlayersTheInstantCastBonus()
    {
        Assert.Equal(
            StatusSpeed.StepsThisTick(0, 1, true),
            StatusSpeed.ActionSteps(Cast, 50, 0, 50, PlayerTextColour, 1));
        Assert.Equal(StatusSpeed.StepsThisTick(0, 1), StatusSpeed.ActionSteps(Cast, 50, 0, 50, 0, 1));
    }

    [Fact]
    public void ActionSteps_OtherActionsUseTheSpeedAlone()
    {
        Assert.Equal(
            StatusSpeed.StepsThisTick(OneStepEveryOtherTick, 1),
            StatusSpeed.ActionSteps(Pickup, 200, 150, 150, PlayerTextColour, 1));
    }
}
