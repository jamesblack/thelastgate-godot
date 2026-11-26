using GdUnit4;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class WorldMapWordingTests
{
    private NoteSet notes = new();

    [BeforeTest]
    public void StartWithNoNotes() => notes = new NoteSet();

    [TestCase(NoteKind.Pin, "Pin: ")]
    [TestCase(NoteKind.Text, "Text: ")]
    [TestCase(NoteKind.Path, "Path: ")]
    public void KindPrefix_StartsEachNoteRow(NoteKind kind, string prefix)
    {
        AssertThat(WorldMapWording.KindPrefix(kind)).IsEqual(prefix);
    }

    [TestCase]
    public void DeleteNoteQuestion_NamesTheNote()
    {
        var category = notes.AddCategory("Camps", 0);
        var text = notes.FindNote(notes.AddPoint(category, NoteKind.Text, 1, 1, "Danger"))!;

        AssertThat(WorldMapWording.DeleteNoteQuestion(text)).IsEqual("Delete text label \"Danger\"?");
    }

    [TestCase]
    public void DeleteNoteQuestion_ForANoteWithNoName()
    {
        var category = notes.AddCategory("Roads", 0);
        var road = notes.FindNote(notes.AddPath(category, [new PathSegment(1, 1, 2, 2)], ""))!;

        AssertThat(WorldMapWording.DeleteNoteQuestion(road)).IsEqual("Delete this unnamed path?");
    }

    [TestCase]
    public void DeleteCategoryQuestion_SaysHowManyNotesGoWithIt()
    {
        var category = notes.AddCategory("Camps", 0);
        notes.AddPoint(category, NoteKind.Pin, 1, 1, "A");
        notes.AddPoint(category, NoteKind.Pin, 2, 2, "B");

        AssertThat(WorldMapWording.DeleteCategoryQuestion(notes.FindCategory(category)!))
            .IsEqual("Delete category \"Camps\" and its 2 annotations?");
    }

    [TestCase(0, "Remove duplicate pins...")]
    [TestCase(1, "Remove 1 duplicate pin...")]
    [TestCase(3, "Remove 3 duplicate pins...")]
    public void DuplicatePinsItem_CountsTheDuplicates(int count, string item)
    {
        AssertThat(WorldMapWording.DuplicatePinsItem(count)).IsEqual(item);
    }

    [TestCase]
    public void DuplicatePinsQuestion_MentionsSubcategories()
    {
        AssertThat(WorldMapWording.DuplicatePinsQuestion(2, "Camps", true))
            .IsEqual("Remove 2 duplicate pins from \"Camps\" and its subcategories?");
        AssertThat(WorldMapWording.DuplicatePinsQuestion(1, "Camps", false))
            .IsEqual("Remove 1 duplicate pin from \"Camps\"?");
    }

    [TestCase]
    public void MergeQuestion_NamesBothCategories()
    {
        AssertThat(WorldMapWording.MergeQuestion("Dungeons", "Places")).IsEqual("Move \"Dungeons\" into \"Places\"?");
        AssertThat(WorldMapWording.MergeClash("Places", "Dungeons"))
            .IsEqual("\"Places\" already has a category called \"Dungeons\".");
    }

    [TestCase]
    public void MergeQuestion_AtTheTopLevel()
    {
        AssertThat(WorldMapWording.MergeQuestion("Dungeons", null)).IsEqual("Move \"Dungeons\" to the top level?");
        AssertThat(WorldMapWording.MergeClash(null, "Dungeons"))
            .IsEqual("The top level already has a category called \"Dungeons\".");
    }
}
