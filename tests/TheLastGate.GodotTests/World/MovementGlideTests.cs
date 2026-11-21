using System.Numerics;
using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MovementGlideTests
{
    private const long Start = 1_000;
    private const double TickLength = 50;
    private const int Goblin = 77;

    private static readonly Dictionary<int, Vector2> NoCharacters = [];

    [TestCase]
    public void WorldPoint_LaysTilesOnTheDiamondGridPlusTheirOffset()
    {
        AssertThat(MovementGlide.WorldPoint(513, 512, 14, 7)).IsEqual(new Vector2(16 * 1025 + 14, 8 * 1 + 7));
    }

    [TestCase]
    public void WorldPoint_IsSeamlessWhenAStepCrossesIntoTheNextTile()
    {
        var lastStepInTile = MovementGlide.WorldPoint(513, 512, 14, 7);
        var firstStepInNextTile = MovementGlide.WorldPoint(514, 512, 0, 0);

        AssertThat(firstStepInNextTile - lastStepInTile).IsEqual(new Vector2(2, 1));
    }

    [TestCase]
    public void Player_SnapsOnTheFirstRecord()
    {
        var glide = new MovementGlide();

        glide.Record(new Vector2(100, 50), NoCharacters, Start, 1, TickLength);

        AssertThat(glide.Player(Start)).IsEqual(new Vector2(100, 50));
    }

    [TestCase]
    public void Player_GlidesOverOneTickLength()
    {
        var glide = new MovementGlide();
        glide.Record(new Vector2(100, 50), NoCharacters, Start, 1, TickLength);

        glide.Record(new Vector2(104, 52), NoCharacters, Start + 50, 1, TickLength);

        AssertThat(glide.Player(Start + 75)).IsEqual(new Vector2(102, 51));
        AssertThat(glide.Player(Start + 100)).IsEqual(new Vector2(104, 52));
    }

    [TestCase]
    public void Player_TakesTwiceAsLongWhenTwoTicksArriveTogether()
    {
        var glide = new MovementGlide();
        glide.Record(new Vector2(100, 50), NoCharacters, Start, 1, TickLength);

        glide.Record(new Vector2(104, 52), NoCharacters, Start + 50, 2, TickLength);

        AssertThat(glide.Player(Start + 100)).IsEqual(new Vector2(102, 51));
        AssertThat(glide.Player(Start + 150)).IsEqual(new Vector2(104, 52));
    }

    [TestCase]
    public void Character_IsUnknownUntilRecorded()
    {
        AssertThat(new MovementGlide().Character(Goblin, Start)).IsNull();
    }

    [TestCase]
    public void Character_SnapsWhenItFirstAppearsAndGlidesAfterwards()
    {
        var glide = new MovementGlide();
        glide.Record(Vector2.Zero, new Dictionary<int, Vector2> { [Goblin] = new(10, 10) }, Start, 1, TickLength);
        AssertThat(glide.Character(Goblin, Start)).IsEqual(new Vector2(10, 10));

        glide.Record(Vector2.Zero, new Dictionary<int, Vector2> { [Goblin] = new(14, 10) }, Start + 50, 1, TickLength);

        AssertThat(glide.Character(Goblin, Start + 75)).IsEqual(new Vector2(12, 10));
    }

    [TestCase]
    public void Character_IsForgottenOnceItLeavesTheScreen()
    {
        var glide = new MovementGlide();
        glide.Record(Vector2.Zero, new Dictionary<int, Vector2> { [Goblin] = new(10, 10) }, Start, 1, TickLength);
        glide.Record(Vector2.Zero, NoCharacters, Start + 50, 1, TickLength);
        AssertThat(glide.Character(Goblin, Start + 50)).IsNull();

        glide.Record(Vector2.Zero, new Dictionary<int, Vector2> { [Goblin] = new(14, 10) }, Start + 100, 1, TickLength);

        AssertThat(glide.Character(Goblin, Start + 100)).IsEqual(new Vector2(14, 10));
    }
}
