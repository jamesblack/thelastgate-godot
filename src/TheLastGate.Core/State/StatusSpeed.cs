using System;

namespace TheLastGate.State;

public static class StatusSpeed
{
    public const int SpeedMax = 300;
    public const int CycleLength = 200;

    private const int FastestSpeed = SpeedMax - 1;
    private const int Attack = 0;
    private const int SecondAttack = 5;
    private const int ThirdAttack = 6;
    private const int Cast = 9;
    private const int PlayerTextColour = 5;

    public static int StepsThisTick(int speed, int serverTick, bool instantCast = false)
    {
        var stepsPerCycle = SpeedMax - speed;
        return stepsPerCycle * (serverTick + 1) / CycleLength
            - stepsPerCycle * serverTick / CycleLength
            + (instantCast ? InstantCastBonus(speed) : 0);
    }

    public static int MoveSpeed(int speed, int moveSpeed) => KeepInRange(speed - moveSpeed);

    public static int MoveSteps(int speed, int moveSpeed, int serverTick) =>
        StepsThisTick(MoveSpeed(speed, moveSpeed), serverTick);

    public static int ActionSteps(
        int action,
        int speed,
        int attackSpeed,
        int castSpeed,
        int textColour,
        int serverTick) =>
        action switch
        {
            Attack or SecondAttack or ThirdAttack => StepsThisTick(KeepInRange(speed - attackSpeed), serverTick),
            Cast => StepsThisTick(
                KeepInRange(2 * speed - 2 * castSpeed),
                serverTick,
                textColour == PlayerTextColour),
            _ => StepsThisTick(KeepInRange(speed), serverTick),
        };

    private static int InstantCastBonus(int speed) => speed switch
    {
        <= 0 => 8,
        <= 33 => 4,
        <= 66 => 2,
        <= 99 => 1,
        _ => 0,
    };

    private static int KeepInRange(int speed) => Math.Clamp(speed, 0, FastestSpeed);
}
