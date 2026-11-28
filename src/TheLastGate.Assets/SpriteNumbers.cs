namespace TheLastGate.Assets;

public static class SpriteNumbers
{
    public const int MaxSprite = 2000 + 128 * 1024;

    // The server sends sprite numbers as 16 bits, so sprites only this client draws sit above that range.
    public const int FirstClientOnly = 70000;
}
