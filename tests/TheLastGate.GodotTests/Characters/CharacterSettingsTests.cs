using GdUnit4;
using TheLastGate.Characters;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Characters;

[TestSuite]
public class CharacterSettingsTests
{
    [TestCase]
    public void New_ShowsWallsNamesAndHealthPercentWithAnEmptyHotbar()
    {
        var settings = new CharacterSettings();

        AssertThat(settings.HideWalls).IsTrue();
        AssertThat(settings.ShowNames).IsTrue();
        AssertThat(settings.ShowHealthPercent).IsTrue();
        AssertThat(settings.ShowHealthBars).IsFalse();
        AssertThat(settings.ShowStatBase).IsFalse();
        AssertThat(settings.HotbarActions.Distinct().ToArray()).ContainsExactly(CharacterSettings.Unassigned);
        AssertThat(settings.HotbarNames.Distinct().ToArray()).ContainsExactly("-");
    }

    [TestCase]
    public void Write_ThenParse_KeepsTogglesAndHotbar()
    {
        var settings = new CharacterSettings { HideWalls = false, ShowHealthBars = true };
        settings.HotbarActions[3] = 17;
        settings.HotbarNames[3] = "Protect";
        var stored = new byte[CharacterRecord.SettingsSize];

        settings.Write(stored);
        var parsed = CharacterSettings.Parse(stored);

        AssertThat(parsed.HideWalls).IsFalse();
        AssertThat(parsed.ShowNames).IsTrue();
        AssertThat(parsed.ShowHealthBars).IsTrue();
        AssertThat(parsed.HotbarActions[3]).IsEqual(17);
        AssertThat(parsed.HotbarNames[3]).IsEqual("Protect");
        AssertThat(parsed.HotbarActions[4]).IsEqual(CharacterSettings.Unassigned);
    }

    [TestCase]
    public void Write_TruncatesHotbarNamesToFitTheirField()
    {
        var settings = new CharacterSettings();
        settings.HotbarNames[0] = "Magic Shield";
        var stored = new byte[CharacterRecord.SettingsSize];

        settings.Write(stored);

        AssertThat(CharacterSettings.Parse(stored).HotbarNames[0]).IsEqual("Magic S");
    }

    [TestCase]
    public void SetStartingHotbar_GivesATemplarCleave()
    {
        AssertStartingHotbar(CharacterClass.Templar, (7, 40, "Cleave"));
    }

    [TestCase]
    public void SetStartingHotbar_GivesAHarakimBlastGhostCompanionMagicShieldAndProtect()
    {
        AssertStartingHotbar(
            CharacterClass.Harakim,
            (3, 17, "Protect"),
            (4, 11, "Magic S"),
            (7, 24, "Blast"),
            (11, 27, "Ghost C"));
    }

    [TestCase(CharacterClass.Mercenary)]
    [TestCase(CharacterClass.Seyan)]
    [TestCase(CharacterClass.Lycanthrope)]
    public void SetStartingHotbar_GivesEveryOtherClassBlindAndProtect(CharacterClass characterClass)
    {
        AssertStartingHotbar(characterClass, (3, 17, "Protect"), (11, 37, "Blind"));
    }

    [TestCase]
    public void SetStartingHotbar_ClearsWhatTheHotbarHeldBefore()
    {
        var settings = new CharacterSettings();
        settings.HotbarActions[10] = 105;
        settings.HotbarNames[10] = "Potion";

        settings.SetStartingHotbar(CharacterClass.Templar);

        AssertThat(settings.HotbarActions[10]).IsEqual(CharacterSettings.Unassigned);
        AssertThat(settings.HotbarNames[10]).IsEqual("-");
    }

    private static void AssertStartingHotbar(
        CharacterClass characterClass,
        params (int Slot, int Skill, string Name)[] classSkills)
    {
        var settings = new CharacterSettings();

        settings.SetStartingHotbar(characterClass);

        var expectedActions = Enumerable.Repeat(CharacterSettings.Unassigned, CharacterSettings.HotbarSlots).ToArray();
        var expectedNames = Enumerable.Repeat("-", CharacterSettings.HotbarSlots).ToArray();
        foreach (var (slot, skill, name) in classSkills.Append((0, 50, "Light")).Append((19, 51, "Recall")))
        {
            expectedActions[slot] = skill;
            expectedNames[slot] = name;
        }

        AssertThat(settings.HotbarActions).IsEqual(expectedActions);
        AssertThat(settings.HotbarNames).IsEqual(expectedNames);
    }
}
