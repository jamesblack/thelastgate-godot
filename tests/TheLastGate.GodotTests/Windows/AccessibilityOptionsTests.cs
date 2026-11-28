using System.Globalization;
using GdUnit4;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class AccessibilityOptionsTests
{
    [TestCase]
    public void SpritesFor_NothingSwitchedOn_SwapsNothing()
    {
        AssertThat(AccessibilityOptions.SpritesFor([])).IsEmpty();
    }

    [TestCase]
    public void SpritesFor_ObviousHoleTraps_DrawsTheHoleInsteadOfTheHiddenTrap()
    {
        var sprites = AccessibilityOptions.SpritesFor([AccessibilityOption.ObviousHoleTraps]);

        AssertThat(sprites).IsEqual(new Dictionary<int, int> { [16859] = 70000 });
    }

    [TestCase]
    public void SpritesFor_PlainInfravision_SwapsNoSprites()
    {
        AssertThat(AccessibilityOptions.SpritesFor([AccessibilityOption.PlainInfravision])).IsEmpty();
    }

    [TestCase]
    public void SpritesFor_TheSameOptionTwice_IsLikeOnce()
    {
        var sprites = AccessibilityOptions.SpritesFor(
            [AccessibilityOption.ObviousHoleTraps, AccessibilityOption.ObviousHoleTraps]);

        AssertThat(sprites).IsEqual(new Dictionary<int, int> { [16859] = 70000 });
    }

    [TestCase]
    public void Rows_HaveOneRowForEveryOption()
    {
        AssertThat(AccessibilityOptions.Rows.Select(row => row.Option))
            .ContainsExactly(Enum.GetValues<AccessibilityOption>());
    }

    [TestCase]
    public void Rows_HaveDistinctSettingKeys()
    {
        var keys = AccessibilityOptions.Rows.Select(row => row.SettingKey).ToList();

        AssertThat(keys.Distinct().Count()).IsEqual(keys.Count);
    }

    [TestCase]
    public void Rows_NeverSwapASpriteIntoOneAnotherRowSwapsAgain()
    {
        var sources = AccessibilityOptions.Rows.SelectMany(row => row.Sprites.Keys).ToList();
        var targets = AccessibilityOptions.Rows.SelectMany(row => row.Sprites.Values);

        AssertThat(sources.Distinct().Count()).IsEqual(sources.Count);
        AssertThat(targets.Where(sources.Contains)).IsEmpty();
    }

    [TestCase]
    public void Rows_SwapOnlyToSpritesTheGameHas()
    {
        var art = Directory.EnumerateFiles(
                Path.Combine(RepositoryFolder.Find(), "art"),
                "*.png",
                SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToHashSet();

        var missing = AccessibilityOptions.Rows
            .SelectMany(row => row.Sprites.Values)
            .Where(sprite => !art.Contains(sprite.ToString("00000", CultureInfo.InvariantCulture) + ".png"));

        AssertThat(missing).IsEmpty();
    }
}
