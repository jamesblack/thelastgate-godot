using GdUnit4;
using Godot;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Launcher;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Launcher;

[TestSuite]
[RequireGodotRuntime]
public class CharacterHistoryTests
{
    [TestCase]
    public void Use_PutsTheNewestFileFirst()
    {
        var history = new CharacterHistory();

        history.Use(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa"));
        history.Use(new PreviousCharacter("Bran", "C:/saves/bran.moa"));

        AssertThat(history.Entries.Select(entry => entry.FilePath))
            .ContainsExactly("C:/saves/bran.moa", "C:/saves/ishtar.moa");
    }

    [TestCase]
    public void Use_MovesAKnownFileToTheFrontWithItsNewName()
    {
        var history = new CharacterHistory();
        history.Use(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa"));
        history.Use(new PreviousCharacter("Bran", "C:/saves/bran.moa"));

        history.Use(new PreviousCharacter("Ishtar the Bold", "C:/Saves/ISHTAR.moa"));

        AssertThat(history.Entries)
            .ContainsExactly(
                new PreviousCharacter("Ishtar the Bold", "C:/Saves/ISHTAR.moa"),
                new PreviousCharacter("Bran", "C:/saves/bran.moa"));
    }

    [TestCase]
    public void Use_KeepsOnlyTheMostRecentEntries()
    {
        var history = new CharacterHistory();

        for (var fileNumber = 0; fileNumber < CharacterHistory.MaxEntries + 5; fileNumber++)
        {
            history.Use(new PreviousCharacter($"Character {fileNumber}", $"C:/saves/{fileNumber}.moa"));
        }

        AssertThat(history.Entries.Count).IsEqual(CharacterHistory.MaxEntries);
        AssertThat(history.Entries[0].FilePath).IsEqual("C:/saves/24.moa");
        AssertThat(history.Entries[^1].FilePath).IsEqual("C:/saves/5.moa");
    }

    [TestCase]
    public void LastFolder_IsTheFolderOfTheNewestFile()
    {
        using var older = new TempFolder();
        using var newer = new TempFolder();
        var history = new CharacterHistory();
        history.Use(new PreviousCharacter("Ishtar", Path.Combine(older.FolderPath, "ishtar.moa")));
        history.Use(new PreviousCharacter("Bran", Path.Combine(newer.FolderPath, "bran.moa")));

        AssertThat(history.LastFolder).IsEqual(newer.FolderPath);
    }

    [TestCase]
    public void LastFolder_IsNullWithNoFiles() => AssertThat(new CharacterHistory().LastFolder).IsNull();

    [TestCase]
    public void Constructor_KeepsTheSavedOrderWithoutDuplicates()
    {
        PreviousCharacter[] saved =
        [
            new("Bran", "C:/saves/bran.moa"),
            new("Ishtar", "C:/saves/ishtar.moa"),
            new("Bran again", "C:/saves/BRAN.moa"),
        ];

        var history = new CharacterHistory(saved);

        AssertThat(history.Entries).ContainsExactly(saved[0], saved[1]);
    }

    [TestCase]
    public void RemoveMissingFiles_DropsFilesThatNoLongerExist()
    {
        using var folder = new TempFolder();
        var kept = folder.Write("kept.moa", [1]);
        var history = new CharacterHistory();
        history.Use(new PreviousCharacter("Gone", Path.Combine(folder.FolderPath, "gone.moa")));
        history.Use(new PreviousCharacter("Kept", kept));

        history.RemoveMissingFiles();

        AssertThat(history.Entries).ContainsExactly(new PreviousCharacter("Kept", kept));
    }

    [TestCase]
    public void Use_KeepsTheCharactersFacts()
    {
        var history = new CharacterHistory();
        var ishtar = new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Templar", "Hard Mode", "Current");

        history.Use(ishtar);

        AssertThat(history.Entries).ContainsExactly(ishtar);
    }

    [TestCase]
    public void Use_ReplacesAnOlderEntryForTheSameFile()
    {
        var history = new CharacterHistory();
        history.Use(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Templar", "Normal", "Legacy"));
        var newer = new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Seyan", "Normal", "Legacy");

        history.Use(newer);

        AssertThat(history.Entries).ContainsExactly(newer);
    }

    [TestCase]
    public void AnOldEntry_LoadsWithEmptyFacts()
    {
        var file = new ConfigFile();
        file.SetValue("launcher", "character_names", new[] { "Ishtar" });
        file.SetValue("launcher", "character_files", new[] { "C:/saves/ishtar.moa" });

        AssertThat(ClientSettings.SavedCharacters(file))
            .ContainsExactly(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa"));
    }

    [TestCase]
    public void ANewEntry_LoadsWithItsFacts()
    {
        var file = new ConfigFile();
        file.SetValue("launcher", "character_names", new[] { "Ishtar" });
        file.SetValue("launcher", "character_files", new[] { "C:/saves/ishtar.moa" });
        file.SetValue("launcher", "character_classes", new[] { "Templar" });
        file.SetValue("launcher", "character_game_modes", new[] { "Hard Mode" });
        file.SetValue("launcher", "character_seasons", new[] { "Current" });
        file.SetValue("launcher", "character_fact_files", new[] { "C:/saves/ishtar.moa" });

        AssertThat(ClientSettings.SavedCharacters(file))
            .ContainsExactly(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Templar", "Hard Mode", "Current"));
    }

    // An older client reorders the names and files but leaves the facts as they were.
    [TestCase]
    public void FactsSavedByANewerClient_StayWithTheirFileAfterAnOlderClientReordersTheList()
    {
        var file = new ConfigFile();
        file.SetValue("launcher", "character_names", new[] { "Bran", "Ishtar", "Gone" });
        file.SetValue(
            "launcher",
            "character_files",
            new[] { "C:/saves/bran.moa", "C:/saves/ishtar.moa", "C:/saves/gone.moa" });
        file.SetValue("launcher", "character_classes", new[] { "Templar", "Seyan" });
        file.SetValue("launcher", "character_game_modes", new[] { "Normal", "Hard Mode" });
        file.SetValue("launcher", "character_seasons", new[] { "Legacy", "Current" });
        file.SetValue("launcher", "character_fact_files", new[] { "C:/saves/ishtar.moa", "C:/saves/BRAN.moa" });

        AssertThat(ClientSettings.SavedCharacters(file))
            .ContainsExactly(
                new PreviousCharacter("Bran", "C:/saves/bran.moa", "Seyan", "Hard Mode", "Current"),
                new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Templar", "Normal", "Legacy"),
                new PreviousCharacter("Gone", "C:/saves/gone.moa"));
    }

    [TestCase]
    public void AnExistingCharactersRecord_GivesItsFactsInThePickersWords()
    {
        var record = CharacterRecords.CreateNew();
        var race = new CharacterRace(CharacterClass.ArchTemplar, Body.Masculine, GameMode.Hard, Season.Current);
        record.Key = record.Key with { UserNumber = 7, Name = "Ishtar", Race = race.ToNumber() };

        AssertThat(PreviousCharacter.Of(record, "C:/saves/ishtar.moa"))
            .IsEqual(new PreviousCharacter("Ishtar", "C:/saves/ishtar.moa", "Arch-Templar", "Hard Mode", "Current"));
    }

    [TestCase]
    public void ACharacterNotYetCreated_HasNoFacts()
    {
        var record = CharacterRecords.CreateNew();

        AssertThat(PreviousCharacter.Of(record, "C:/saves/new.moa"))
            .IsEqual(new PreviousCharacter(record.Key.Name, "C:/saves/new.moa"));
    }
}
