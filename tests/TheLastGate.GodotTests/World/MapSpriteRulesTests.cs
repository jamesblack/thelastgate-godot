using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MapSpriteRulesTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FloorFlagsObjectAndCharacter = 0b0010_1011;
    private const int Centre = WorldMap.Size / 2;
    private const int FacingEast = 1;
    private const int Wall = 1234;
    private const int Waypoint = 598;
    private const int LitWaypoint = 599;
    private static readonly (int Column, int Row) BehindThePlayer = (Centre + 3, Centre - 7);
    private static readonly (int Column, int Row) InFrontOfThePlayer = (Centre - 17, Centre + 13);
    private static readonly (int Column, int Row) HiddenCorner = (WorldMap.Size - 1, 0);

    [TestCase]
    public void FloorSprite_IsTheTilesFloor()
    {
        var (tile, _) = Tile(BehindThePlayer, 1002);

        AssertThat(MapSpriteRules.FloorSprite(tile, 0)).IsEqual(1002);
    }

    [TestCase]
    public void FloorSprite_ShowsNothingOnATileOutOfSight()
    {
        var (tile, _) = Tile(BehindThePlayer, 1002, flags: MapFlags.Invisible);

        AssertThat(MapSpriteRules.FloorSprite(tile, 0)).IsEqual(0);
    }

    [TestCase(MapFlags.None, SpriteTint.None)]
    [TestCase(MapFlags.Infrared, SpriteTint.Infrared)]
    [TestCase(MapFlags.Underwater, SpriteTint.Underwater)]
    [TestCase(MapFlags.Infrared | MapFlags.Underwater, SpriteTint.Infrared | SpriteTint.Underwater)]
    [TestCase(MapFlags.Stoned | MapFlags.Bloody, SpriteTint.None)]
    public void EnvironmentTint_TintsFloorsAndObjectsForInfraredAndUnderwater(MapFlags flags, SpriteTint tint)
    {
        var (tile, _) = Tile(BehindThePlayer, flags: flags);

        AssertThat(MapSpriteRules.EnvironmentTint(tile, false)).IsEqual(tint);
    }

    [TestCase(MapFlags.Infrared, SpriteTint.None)]
    [TestCase(MapFlags.Infrared | MapFlags.Underwater, SpriteTint.Underwater)]
    public void EnvironmentTint_WithPlainInfravisionLeavesInfraredTilesUntinted(MapFlags flags, SpriteTint tint)
    {
        var (tile, _) = Tile(BehindThePlayer, flags: flags);

        AssertThat(MapSpriteRules.EnvironmentTint(tile, true)).IsEqual(tint);
    }

    [TestCase(MapFlags.None, SpriteTint.None)]
    [TestCase(MapFlags.Stoned, SpriteTint.Grey)]
    [TestCase(MapFlags.Bloody, SpriteTint.Bloody)]
    [TestCase(MapFlags.Infrared, SpriteTint.Infrared)]
    [TestCase(MapFlags.Underwater, SpriteTint.Underwater)]
    [TestCase(MapFlags.Stoned | MapFlags.Underwater, SpriteTint.Grey | SpriteTint.Underwater)]
    [TestCase(MapFlags.Infrared | MapFlags.Bloody, SpriteTint.Infrared | SpriteTint.Bloody)]
    public void CharacterTint_AlsoGreysStonedAndReddensBloodyCharacters(MapFlags flags, SpriteTint tint)
    {
        var (tile, _) = Tile(BehindThePlayer, flags: flags, character: 3021);

        AssertThat(MapSpriteRules.CharacterTint(tile, false)).IsEqual(tint);
    }

    [TestCase(MapFlags.Infrared, SpriteTint.None)]
    [TestCase(MapFlags.Bloody, SpriteTint.Bloody)]
    [TestCase(MapFlags.Infrared | MapFlags.Bloody, SpriteTint.Bloody)]
    public void CharacterTint_WithPlainInfravisionStillReddensBloodyCharacters(MapFlags flags, SpriteTint tint)
    {
        var (tile, _) = Tile(BehindThePlayer, flags: flags, character: 3021);

        AssertThat(MapSpriteRules.CharacterTint(tile, true)).IsEqual(tint);
    }

    [TestCase]
    public void ObjectSprite_ShowsTheLowWallBehindThePlayer()
    {
        AssertThat(ObjectAt(BehindThePlayer, Wall)).IsEqual(Wall + 1);
    }

    [TestCase]
    public void ObjectSprite_ShowsTheWholeWallWhenWallsArentHidden()
    {
        AssertThat(ObjectAt(BehindThePlayer, Wall, wallHiding: WallHiding.Off)).IsEqual(Wall);
    }

    [TestCase]
    public void ObjectSprite_ShowsAWholeItem()
    {
        AssertThat(ObjectAt(BehindThePlayer, Wall, MapFlags.IsItem)).IsEqual(Wall);
    }

    [TestCase]
    public void ObjectSprite_ShowsTheWholeWallInFrontOfThePlayer()
    {
        AssertThat(ObjectAt(InFrontOfThePlayer, Wall)).IsEqual(Wall);
    }

    [TestCase]
    public void ObjectSprite_ShortensTheWallInFrontOfThePlayerWhenHidingAll()
    {
        AssertThat(ObjectAt(InFrontOfThePlayer, Wall, wallHiding: WallHiding.All)).IsEqual(Wall + 1);
    }

    [TestCase]
    public void ObjectSprite_ShowsAWholeItemWhenHidingAll()
    {
        AssertThat(ObjectAt(InFrontOfThePlayer, Wall, MapFlags.IsItem, WallHiding.All)).IsEqual(Wall);
    }

    [TestCase]
    public void ObjectSprite_LowersTheMineWallThePlayerFacesWhenHidingAll()
    {
        AssertThat(ObjectAt((Centre + 1, Centre), 16350, MapFlags.IsItem, WallHiding.All, FacingEast)).IsEqual(457);
    }

    [TestCase]
    public void ObjectSprite_LowersAMineWallInFrontOfThePlayerWhenHidingAll()
    {
        AssertThat(ObjectAt(InFrontOfThePlayer, 16350, MapFlags.IsItem, WallHiding.All)).IsEqual(457);
    }

    [TestCase]
    public void ObjectSprite_LightsAFoundWaypointInFrontOfThePlayerWhenHidingAll()
    {
        var (tile, player) = Tile(InFrontOfThePlayer, 1002, Waypoint, MapFlags.IsItem, waypoints: 1u);

        AssertThat(
                MapSpriteRules.ObjectSprite(
                    tile,
                    tile.ItemSprite,
                    InFrontOfThePlayer.Column,
                    InFrontOfThePlayer.Row,
                    player,
                    WallHiding.All))
            .IsEqual(LitWaypoint);
    }

    [TestCase(330, 70100)]
    [TestCase(5784, 70117)]
    [TestCase(320, 70118)]
    [TestCase(2872, 70125)]
    [TestCase(1096, 70135)]
    public void ObjectSprite_ShowsATreesTrunkWhenHidingAll(int tree, int trunk)
    {
        AssertThat(ObjectAt(BehindThePlayer, tree, wallHiding: WallHiding.All)).IsEqual(trunk);
        AssertThat(ObjectAt(InFrontOfThePlayer, tree, wallHiding: WallHiding.All)).IsEqual(trunk);
    }

    [TestCase]
    public void ObjectSprite_ShowsATreesDitherWhenHidingNear()
    {
        AssertThat(ObjectAt(BehindThePlayer, 330)).IsEqual(331);
    }

    [TestCase]
    public void ObjectSprite_ShowsAWholeLookableTreeWhenHidingAll()
    {
        AssertThat(ObjectAt(BehindThePlayer, 330, MapFlags.IsItem, WallHiding.All)).IsEqual(330);
    }

    [TestCase]
    public void TreeTrunks_HasThirtySixTrees()
    {
        AssertThat(MapSpriteRules.TreeTrunks.Count).IsEqual(36);
    }

    [TestCase]
    public void ObjectSprite_ShowsNothingWhereThereIsNoObject()
    {
        AssertThat(ObjectAt(BehindThePlayer, 0)).IsEqual(0);
    }

    [TestCase]
    public void ObjectSprite_ShowsNothingOnAnInvisibleTileOrInTheHiddenCorner()
    {
        AssertThat(ObjectAt(BehindThePlayer, Wall, MapFlags.IsItem | MapFlags.Invisible)).IsEqual(0);
        AssertThat(ObjectAt(HiddenCorner, Wall, MapFlags.IsItem)).IsEqual(0);
    }

    [TestCase(16350, 457)]
    [TestCase(16358, 456)]
    [TestCase(16366, 455)]
    [TestCase(16374, 466)]
    [TestCase(16382, 459)]
    [TestCase(16390, 458)]
    [TestCase(16398, 468)]
    [TestCase(16406, 467)]
    [TestCase(16414, 467)]
    public void ObjectSprite_ShowsALowMineWallBehindThePlayer(int mineWall, int lowWall)
    {
        AssertThat(ObjectAt(BehindThePlayer, mineWall, MapFlags.IsItem)).IsEqual(lowWall);
    }

    [TestCase(16357)]
    [TestCase(16365)]
    [TestCase(16421)]
    [TestCase(16422)]
    public void ObjectSprite_KeepsAMineWallThatHasNoLowVersion(int mineWall)
    {
        AssertThat(ObjectAt(BehindThePlayer, mineWall, MapFlags.IsItem)).IsEqual(mineWall);
    }

    [TestCase]
    public void ObjectSprite_KeepsTheWholeMineWallThePlayerFaces()
    {
        AssertThat(ObjectAt((Centre + 1, Centre), 16350, MapFlags.IsItem, direction: FacingEast)).IsEqual(16350);
    }

    [TestCase]
    public void ObjectSprite_KeepsTheWholeMineWallWhenWallsArentHidden()
    {
        AssertThat(ObjectAt(BehindThePlayer, 16350, MapFlags.IsItem, WallHiding.Off)).IsEqual(16350);
    }

    [TestCase(0, 1002)]
    [TestCase(31, 549)]
    public void ObjectSprite_LightsAWaypointTheCharacterHasFound(int waypointBit, int floor)
    {
        var (tile, player) = Tile(BehindThePlayer, floor, Waypoint, MapFlags.IsItem, waypoints: 1u << waypointBit);

        AssertThat(
                MapSpriteRules.ObjectSprite(
                    tile,
                    tile.ItemSprite,
                    BehindThePlayer.Column,
                    BehindThePlayer.Row,
                    player,
                    WallHiding.Near))
            .IsEqual(LitWaypoint);
    }

    [TestCase]
    public void ObjectSprite_LeavesAWaypointDarkUntilItsFound()
    {
        var (unfound, player) = Tile(BehindThePlayer, 1002, Waypoint, MapFlags.IsItem, waypoints: 1u << 1);
        var (otherFloor, playerWithIt) = Tile(BehindThePlayer, 1008, Waypoint, MapFlags.IsItem, waypoints: 1u << 0);

        AssertThat(
                MapSpriteRules.ObjectSprite(
                    unfound,
                    unfound.ItemSprite,
                    BehindThePlayer.Column,
                    BehindThePlayer.Row,
                    player,
                    WallHiding.Near))
            .IsEqual(Waypoint);
        AssertThat(
                MapSpriteRules.ObjectSprite(
                    otherFloor,
                    otherFloor.ItemSprite,
                    BehindThePlayer.Column,
                    BehindThePlayer.Row,
                    playerWithIt,
                    WallHiding.Near))
            .IsEqual(Waypoint);
    }

    [TestCase]
    public void CharacterSprite_ShowsTheCharacter()
    {
        var (tile, _) = Tile(BehindThePlayer, character: 3021);

        AssertThat(
                MapSpriteRules.CharacterSprite(tile, tile.CharacterSprite, BehindThePlayer.Column, BehindThePlayer.Row))
            .IsEqual(3021);
    }

    [TestCase]
    public void CharacterSprite_ShowsNothingOnAnInvisibleTileOrInTheHiddenCorner()
    {
        var (invisible, _) = Tile(BehindThePlayer, flags: MapFlags.Invisible, character: 3021);
        var (cornered, _) = Tile(HiddenCorner, character: 3021);

        AssertThat(
                MapSpriteRules.CharacterSprite(
                    invisible,
                    invisible.CharacterSprite,
                    BehindThePlayer.Column,
                    BehindThePlayer.Row))
            .IsEqual(0);
        AssertThat(
                MapSpriteRules.CharacterSprite(
                    cornered,
                    cornered.CharacterSprite,
                    HiddenCorner.Column,
                    HiddenCorner.Row))
            .IsEqual(0);
    }

    private static int ObjectAt(
        (int Column, int Row) position,
        int objectSprite,
        MapFlags flags = MapFlags.None,
        WallHiding wallHiding = WallHiding.Near,
        int direction = 0)
    {
        var (tile, player) = Tile(position, objectSprite: objectSprite, flags: flags, direction: direction);
        return MapSpriteRules.ObjectSprite(tile, tile.ItemSprite, position.Column, position.Row, player, wallHiding);
    }

    private static (MapTile Tile, PlayerState Player) Tile(
        (int Column, int Row) position,
        int floor = 0,
        int objectSprite = 0,
        MapFlags flags = MapFlags.None,
        int character = 0,
        int direction = 0,
        uint waypoints = 0)
    {
        var tileIndex = position.Column + position.Row * WorldMap.Size;
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                TestCommand.Of(AbsoluteSetMap)
                    .Byte(FloorFlagsObjectAndCharacter)
                    .UInt16((ushort)tileIndex)
                    .UInt16((ushort)floor)
                    .UInt32((uint)flags)
                    .UInt16((ushort)objectSprite)
                    .UInt16((ushort)character)
                    .Bytes(0, 0),
                TestCommand.Of(ServerCommand.SetCharacterDirection).Byte((byte)direction),
                TestCommand.Of(ServerCommand.SetCharacterWaypoints).UInt32(waypoints)));
        return (state.Map.Tiles[tileIndex], state.Player);
    }
}
