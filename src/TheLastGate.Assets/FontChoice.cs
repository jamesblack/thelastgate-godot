using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Assets;

public static class FontChoice
{
    public const string ClassicId = "classic";

    public static FontEntry? Resolve(string? savedId, IReadOnlyList<FontEntry> available) =>
        available.FirstOrDefault(font => font.Id == savedId);
}
