using TheLastGate.State;

namespace TheLastGate.World;

public static class MapMarkers
{
    public const int WalkTarget = 31;
    public const int AttackTarget = 34;
    public const int GiveTarget = 45;

    private const int DropMarker = 32;
    private const int PickUpMarker = 33;
    private const int UseMarker = 45;

    public static bool IsWalkTarget(in MapTile tile, PlayerTarget target) =>
        tile.X == target.GotoX && tile.Y == target.GotoY;

    public static bool IsAttackTarget(in MapTile tile, PlayerTarget target) =>
        target.AttackCharacter != 0 && target.AttackCharacter == tile.CharacterNumber;

    public static bool IsGiveTarget(in MapTile tile, PlayerTarget target) =>
        target.MiscAction == MiscActions.Give && target.MiscTarget1 == tile.CharacterId;

    public static int ActionMarker(in MapTile tile, PlayerTarget target)
    {
        if (target.MiscTarget1 != tile.X || target.MiscTarget2 != tile.Y)
        {
            return 0;
        }

        return target.MiscAction switch
        {
            MiscActions.Drop => DropMarker,
            MiscActions.PickUp => PickUpMarker,
            MiscActions.Use => UseMarker,
            _ => 0,
        };
    }
}
