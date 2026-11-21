namespace TheLastGate.State;

public static class Kindred
{
    public const uint Mercenary = 1u << 0;
    public const uint SeyanDu = 1u << 1;
    public const uint Templar = 1u << 4;
    public const uint Harakim = 1u << 6;
    public const uint Lycanthrope = 1u << 16;
    public const uint KnowsIdentify = 1u << 18;

    private const int FirstClassSkill = 50;
    private const int SecondClassSkill = 51;
    private const int IdentifySkill = 52;
    private const int FirstLycanthropeSkill = 53;
    private const int SecondLycanthropeSkill = 54;

    public static bool IsKnown(int skillNumber, bool learnt, uint kindred) => learnt || skillNumber switch
    {
        FirstClassSkill or SecondClassSkill => true,
        IdentifySkill => (kindred & KnowsIdentify) != 0,
        FirstLycanthropeSkill or SecondLycanthropeSkill => (kindred & Lycanthrope) != 0,
        _ => false,
    };
}
