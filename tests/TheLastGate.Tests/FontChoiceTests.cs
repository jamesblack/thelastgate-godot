using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class FontChoiceTests
{
    private static readonly FontEntry Vt323 = new("vt323", "VT323", "vt323-regular.ttf", 12, 0, false);
    private static readonly FontEntry Syne = new("syne", "Syne", "syne-regular.ttf", 9, 0, true);
    private static readonly FontEntry[] Available = [Vt323, Syne];

    [Fact]
    public void Resolve_Null_IsClassic() => Assert.Null(FontChoice.Resolve(null, Available));

    [Fact]
    public void Resolve_ClassicId_IsClassic() => Assert.Null(FontChoice.Resolve(FontChoice.ClassicId, Available));

    [Fact]
    public void Resolve_UnknownId_IsClassic() => Assert.Null(FontChoice.Resolve("comic-sans", Available));

    [Fact]
    public void Resolve_KnownId_ReturnsThatEntry() => Assert.Same(Syne, FontChoice.Resolve("syne", Available));

    [Fact]
    public void Resolve_IdIsCaseSensitive() => Assert.Null(FontChoice.Resolve("VT323", Available));
}
