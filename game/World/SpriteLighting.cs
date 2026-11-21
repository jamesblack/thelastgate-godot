namespace TheLastGate.World;

public static class SpriteLighting
{
    public const int DarkestLight = 15;

    // The current client stored gamma as 5000 for 1.00, and its shader darkened by light against gamma minus 4880.
    private const float StoredGammaScale = 5000;
    private const float ShaderGammaBase = 4880;

    public static float Brightness(int light) => Brightness(light, Gamma.Normal);

    public static float Brightness(int light, float worldGamma)
    {
        var gammaEffect = worldGamma * StoredGammaScale - ShaderGammaBase;
        return light > 0 ? gammaEffect / (light * light + gammaEffect) : 1f;
    }
}
