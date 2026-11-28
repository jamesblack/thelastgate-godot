using System.Collections.Generic;
using System.Linq;
using TheLastGate.Assets;

namespace TheLastGate.Windows;

public enum AccessibilityOption
{
    ObviousHoleTraps,
    PlainInfravision,
}

public sealed record AccessibilityRow(AccessibilityOption Option, string Label, string SettingKey)
{
    public IReadOnlyDictionary<int, int> Sprites { get; init; } = new Dictionary<int, int>();
}

public static class AccessibilityOptions
{
    private const int HiddenHoleTrap = 16859;
    private const int ObviousHoleTrap = SpriteNumbers.FirstClientOnly;

    public static IReadOnlyList<AccessibilityRow> Rows { get; } =
    [
        new(AccessibilityOption.ObviousHoleTraps, "More obvious hole traps.", "obvious_hole_traps")
        {
            Sprites = new Dictionary<int, int> { [HiddenHoleTrap] = ObviousHoleTrap },
        },
        new(
            AccessibilityOption.PlainInfravision,
            "Draw infravision in normal colours instead of red.",
            "plain_infravision"),
    ];

    public static IReadOnlyDictionary<int, int> SpritesFor(IEnumerable<AccessibilityOption> switchedOn)
    {
        var sprites = new Dictionary<int, int>();
        foreach (var option in switchedOn)
        {
            foreach (var (shown, drawn) in Rows.Single(row => row.Option == option).Sprites)
            {
                sprites[shown] = drawn;
            }
        }

        return sprites;
    }
}
