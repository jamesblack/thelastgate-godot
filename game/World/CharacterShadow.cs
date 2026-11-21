namespace TheLastGate.World;

public readonly record struct CharacterShadow(int Left, int Top, int Height)
{
    private const int TileSize = IsometricProjection.TileSize;
    private const int ShadowRowsPerTile = 8;

    // The current client lifts the shadow 14 pixels above the sprite's foot after moving it 4 down.
    private const int ShadowTop = 4 - 14;

    private const int FirstCasting = 2000;
    private const int LastCastingBeforeGap = 16335;
    private const int LastInGap = 17360;

    public static bool Casts(int spriteNumber) =>
        spriteNumber is >= FirstCasting and <= LastCastingBeforeGap || spriteNumber > LastInGap;

    public static CharacterShadow For(int width, int height) =>
        new(-(width / TileSize) * (TileSize / 2), ShadowTop, height / TileSize * ShadowRowsPerTile);
}
