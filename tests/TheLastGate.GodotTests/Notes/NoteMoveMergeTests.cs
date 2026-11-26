using GdUnit4;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Notes;

[TestSuite]
public class NoteMoveMergeTests
{
    private NoteSet set = new();

    [BeforeTest]
    public void StartWithNoNotes() => set = new NoteSet();

    [TestCase]
    public void MergeMoved_CombinesNotesAndKeepsTheTargetsSettings()
    {
        var places = set.AddCategory("Places", 0);
        var target = set.AddCategory("Dungeons", 2, places);
        set.AddPoint(target, NoteKind.Pin, 1, 1, "Entrance");
        var moving = set.AddCategory("Dungeons", 5);
        set.AddPoint(moving, NoteKind.Pin, 9, 9, "Boss");

        var result = NoteImport.MergeMoved(set, moving, target);

        AssertThat(result.Status).IsEqual(NoteImportStatus.Ok);
        AssertThat(set.FindCategory(moving)).IsNull();
        AssertThat(Labels(target)).ContainsExactly("Entrance", "Boss");
        AssertThat(set.FindCategory(target)!.ColourIndex).IsEqual(2);
    }

    [TestCase]
    public void MergeMoved_IdenticalNotesAreKeptOnce()
    {
        var target = set.AddCategory("Camps", 0);
        set.AddPoint(target, NoteKind.Pin, 1, 1, "Fire");
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        set.AddPoint(moving, NoteKind.Pin, 1, 1, "Fire");

        NoteImport.MergeMoved(set, moving, target);

        AssertThat(Labels(target)).ContainsExactly("Fire");
    }

    [TestCase]
    public void MergeMoved_ThatAddsNoNotes_StillMarksTheSetChanged()
    {
        var target = set.AddCategory("Camps", 0);
        set.AddPoint(target, NoteKind.Pin, 1, 1, "Fire");
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        set.AddPoint(moving, NoteKind.Pin, 1, 1, "Fire");
        set.Changed = false;

        NoteImport.MergeMoved(set, moving, target);

        AssertThat(set.Changed).IsTrue();
    }

    [TestCase]
    public void MergeMoved_SameLabelDifferentSpot_KeepsBoth()
    {
        var target = set.AddCategory("Camps", 0);
        set.AddPoint(target, NoteKind.Pin, 1, 1, "Boss");
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        set.AddPoint(moving, NoteKind.Pin, 7, 7, "Boss");

        NoteImport.MergeMoved(set, moving, target);

        AssertThat(Labels(target)).ContainsExactly("Boss", "Boss");
    }

    [TestCase]
    public void MergeMoved_MergesMatchingSubcategoriesDownTheTree()
    {
        var target = set.AddCategory("Camps", 0);
        var targetChild = set.AddCategory("North", 0, target);
        set.AddPoint(targetChild, NoteKind.Pin, 1, 1, "Old");
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        var movingChild = set.AddCategory("north", 0, moving);
        set.AddPoint(movingChild, NoteKind.Pin, 2, 2, "New");

        NoteImport.MergeMoved(set, moving, target);

        AssertThat(set.Tree(target).Count).IsEqual(2);
        AssertThat(Labels(targetChild)).ContainsExactly("Old", "New");
    }

    [TestCase]
    public void MergeMoved_IntoItsFormerSameNamedParent_Works()
    {
        var parent = set.AddCategory("Bar", 0);
        var child = set.AddCategory("Bar", 0, parent);
        set.AddPoint(child, NoteKind.Pin, 3, 3, "Inside");

        var result = NoteImport.MergeMoved(set, child, parent);

        AssertThat(result.Status).IsEqual(NoteImportStatus.Ok);
        AssertThat(set.Categories.Count).IsEqual(1);
        AssertThat(Labels(parent)).ContainsExactly("Inside");
    }

    [TestCase]
    public void MergeMoved_OverALimit_LeavesEverythingAsItWas()
    {
        var target = set.AddCategory("Full", 0);
        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            set.AddPoint(target, NoteKind.Pin, 1, count, $"{count}");
        }

        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Full", 0, places);
        var pin = set.AddPoint(moving, NoteKind.Pin, 5, 5, "One too many");
        var before = Shape();

        var result = NoteImport.MergeMoved(set, moving, target);

        AssertThat(result.Status).IsEqual(NoteImportStatus.CategoryFull);
        AssertThat(Shape()).IsEqual(before);
        AssertThat(set.OwnerOf(pin)!.Id).IsEqual(moving);
    }

    [TestCase]
    public void MergeMoved_NearTheCategoryLimit_DoesNotCountTheOriginal()
    {
        var target = set.AddCategory("Camps", 0);
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        set.AddCategory("Inner", 0, moving);
        while (set.Categories.Count < NoteLimits.Categories)
        {
            set.AddCategory($"Filler {set.Categories.Count}", 0);
        }

        var result = NoteImport.MergeMoved(set, moving, target);

        AssertThat(result.Status).IsEqual(NoteImportStatus.Ok);
        AssertThat(set.Categories.Count).IsEqual(NoteLimits.Categories - 1);
    }

    [TestCase]
    public void CheckMergeMoved_ChangesNothing()
    {
        var target = set.AddCategory("Camps", 0);
        var places = set.AddCategory("Places", 0);
        var moving = set.AddCategory("Camps", 0, places);
        var pin = set.AddPoint(moving, NoteKind.Pin, 2, 2, "Fire");
        var before = Shape();

        var result = NoteImport.CheckMergeMoved(set, moving, target);

        AssertThat(result.Status).IsEqual(NoteImportStatus.Ok);
        AssertThat(Shape()).IsEqual(before);
        AssertThat(set.OwnerOf(pin)!.Id).IsEqual(moving);
    }

    private string Shape() => string.Join(";", set.Tree().Select(category => $"{category.Id}<{category.ParentId}"));

    private List<string> Labels(int categoryId) =>
        set.FindCategory(categoryId)!.Notes.Select(note => note.Label).ToList();
}
