using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class AutoWalkTests
{
    [TestCase(289, 272, WalkDirection.North)]
    [TestCase(289, 622, WalkDirection.West)]
    [TestCase(990, 622, WalkDirection.South)]
    [TestCase(990, 272, WalkDirection.East)]
    public void EachCornerButton_WalksItsWay(int screenX, int screenY, WalkDirection direction) =>
        AssertThat(AutoWalk.ButtonAt(screenX, screenY)).IsEqual(direction);

    [TestCase(289 + 11, 272)]
    [TestCase(289 + 8, 272 + 8)]
    public void AClickJustInsideTheButton_Counts(int screenX, int screenY) =>
        AssertThat(AutoWalk.ButtonAt(screenX, screenY)).IsEqual(WalkDirection.North);

    [TestCase(289 + 12, 272)]
    [TestCase(289 + 9, 272 + 8)]
    [TestCase(640, 360)]
    public void AClickAwayFromTheButtons_IsNotOne(int screenX, int screenY) =>
        AssertThat(AutoWalk.ButtonAt(screenX, screenY)).IsNull();
}
