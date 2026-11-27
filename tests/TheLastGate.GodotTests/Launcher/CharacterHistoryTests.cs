using GdUnit4;
using TheLastGate.Launcher;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Launcher;

[TestSuite]
public class CharacterHistoryTests
{
    [TestCase]
    public void Use_PutsTheNewestFileFirst()
    {
        var history = new CharacterHistory();

        history.Use("Ishtar", "C:/saves/ishtar.moa");
        history.Use("Bran", "C:/saves/bran.moa");

        AssertThat(history.Entries.Select(entry => entry.FilePath))
            .ContainsExactly("C:/saves/bran.moa", "C:/saves/ishtar.moa");
    }

    [TestCase]
    public void Use_MovesAKnownFileToTheFrontWithItsNewName()
    {
        var history = new CharacterHistory();
        history.Use("Ishtar", "C:/saves/ishtar.moa");
        history.Use("Bran", "C:/saves/bran.moa");

        history.Use("Ishtar the Bold", "C:/Saves/ISHTAR.moa");

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
            history.Use($"Character {fileNumber}", $"C:/saves/{fileNumber}.moa");
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
        history.Use("Ishtar", Path.Combine(older.FolderPath, "ishtar.moa"));
        history.Use("Bran", Path.Combine(newer.FolderPath, "bran.moa"));

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
        history.Use("Gone", Path.Combine(folder.FolderPath, "gone.moa"));
        history.Use("Kept", kept);

        history.RemoveMissingFiles();

        AssertThat(history.Entries).ContainsExactly(new PreviousCharacter("Kept", kept));
    }
}
