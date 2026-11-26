using GdUnit4;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Notes;

[TestSuite]
public class NotesTests
{
    private static readonly PathSegment[] TwoSegments = [new(1, 2, 3, 4), new(3, 4, 5, 6)];

    private NoteSet set = new();

    [BeforeTest]
    public void StartWithNoNotes() => set = new NoteSet();

    [TestCase]
    public void Revision_GoesUpWithEveryChangeButNotWithSaving()
    {
        var before = set.Revision;

        set.AddCategory("A", 0);
        var changed = set.Revision;
        set.Changed = false;

        AssertThat(changed).IsNotEqual(before);
        AssertThat(set.Revision).IsEqual(changed);
    }

    [TestCase]
    public void IsSamePin_ComparesOnlyTheLabelAndTile()
    {
        var category = set.AddCategory("A", 0);
        var gate = set.FindNote(set.AddPoint(category, NoteKind.Pin, 5, 5, "Gate"))!;
        var recoloured = set.AddPoint(category, NoteKind.Pin, 5, 5, "Gate");
        set.SetNoteColour(recoloured, 3);

        AssertThat(gate.IsSamePin(set.FindNote(recoloured)!)).IsTrue();
        AssertThat(gate.IsSamePin(set.FindNote(set.AddPoint(category, NoteKind.Pin, 5, 5, "gate"))!)).IsFalse();
        AssertThat(gate.IsSamePin(set.FindNote(set.AddPoint(category, NoteKind.Pin, 6, 5, "Gate"))!)).IsFalse();
        AssertThat(gate.IsSamePin(set.FindNote(set.AddPoint(category, NoteKind.Text, 5, 5, "Gate"))!)).IsFalse();
    }

    [TestCase]
    public void DuplicatePins_AreRemovedThroughSubcategoriesKeepingTheFirst()
    {
        var root = set.AddCategory("Root", 0);
        var firstGate = set.AddPoint(root, NoteKind.Pin, 5, 5, "Gate");
        var recoloured = set.AddPoint(root, NoteKind.Pin, 5, 5, "Gate");
        set.SetNoteColour(recoloured, 3);
        set.AddPoint(root, NoteKind.Pin, 6, 5, "Gate");
        set.AddPoint(root, NoteKind.Text, 5, 5, "Gate");
        set.AddPoint(root, NoteKind.Pin, 1, 1, "Door");
        var child = set.AddCategory("Child", 0, root);
        set.AddPoint(child, NoteKind.Pin, 5, 5, "Gate");
        set.AddPoint(child, NoteKind.Pin, 2, 2, "Door");
        set.AddPoint(child, NoteKind.Pin, 2, 2, "Door");
        var outside = set.AddCategory("Outside", 0);
        set.AddPoint(outside, NoteKind.Pin, 5, 5, "Gate");
        set.AddPoint(outside, NoteKind.Pin, 5, 5, "Gate");
        set.Changed = false;

        AssertThat(set.CountDuplicatePins(root)).IsEqual(2);
        AssertThat(set.CountDuplicatePins(child)).IsEqual(1);
        AssertThat(set.RemoveDuplicatePins(root)).IsEqual(2);

        AssertThat(set.Changed).IsTrue();
        AssertThat(set.FindCategory(root)!.Notes.Count).IsEqual(4);
        AssertThat(set.FindNote(firstGate)).IsNotNull();
        AssertThat(set.FindNote(recoloured)).IsNull();
        AssertThat(set.FindCategory(child)!.Notes.Count).IsEqual(2);
        AssertThat(set.FindCategory(outside)!.Notes.Count).IsEqual(2);
        AssertThat(set.CountDuplicatePins(root)).IsEqual(0);
        set.Changed = false;
        AssertThat(set.RemoveDuplicatePins(root)).IsEqual(0);
        AssertThat(set.Changed).IsFalse();
    }

    [TestCase]
    public void CategoriesAndNotes_ShareOneIdCounterThatNeverReuses()
    {
        var category = set.AddCategory("Keys", 0);
        var firstPin = set.AddPoint(category, NoteKind.Pin, 10, 20, "a");
        set.RemoveNote(firstPin);

        var secondPin = set.AddPoint(category, NoteKind.Pin, 10, 20, "b");

        AssertThat(category).IsEqual(1);
        AssertThat(firstPin).IsEqual(2);
        AssertThat(secondPin).IsEqual(3);
    }

    [TestCase(999, 1, 1)]
    [TestCase(1, 2048, 1)]
    [TestCase(1, 1, -1)]
    public void AddPoint_RefusesAnUnknownCategoryOrATileOffTheMap(int category, int tileX, int tileY)
    {
        set.AddCategory("Keys", 0);

        AssertThat(set.AddPoint(category, NoteKind.Pin, tileX, tileY, "x")).IsEqual(0);
    }

    [TestCase]
    public void Text_NeedsALabelButAPinDoesNot()
    {
        var category = set.AddCategory("Notes", 0);

        AssertThat(set.AddPoint(category, NoteKind.Text, 1, 1, "")).IsEqual(0);
        AssertThat(set.AddPoint(category, NoteKind.Pin, 1, 1, "")).IsNotEqual(0);

        var text = set.AddPoint(category, NoteKind.Text, 1, 1, "Gate");
        AssertThat(set.ReplaceNote(text, set.FindNote(text)! with { Label = "" })).IsFalse();
        AssertThat(set.FindNote(text)!.Label).IsEqual("Gate");
    }

    [TestCase]
    public void AddPoint_OnlyMakesPinsAndText()
    {
        var category = set.AddCategory("Notes", 0);

        AssertThat(set.AddPoint(category, NoteKind.Path, 1, 1, "x")).IsEqual(0);
    }

    [TestCase]
    public void AddPath_KeepsItsOwnCopyOfTheSegments()
    {
        var category = set.AddCategory("Routes", 1);
        var segments = TwoSegments.ToArray();
        var path = set.AddPath(category, segments, "road");

        segments[0] = new PathSegment(900, 2, 3, 4);

        var found = set.FindNote(path)!;
        AssertThat(found.Kind).IsEqual(NoteKind.Path);
        AssertThat(found.Segments).IsEqual(TwoSegments);
        AssertThat((found.TileX, found.TileY)).IsEqual((1, 2));
    }

    [TestCase]
    public void AddPath_RefusesNoSegmentsTooManyOrOffTheMap()
    {
        var category = set.AddCategory("Routes", 1);

        AssertThat(set.AddPath(category, [], "empty")).IsEqual(0);
        AssertThat(set.AddPath(category, LongPath(NoteLimits.SegmentsPerPath + 1), "long")).IsEqual(0);
        AssertThat(set.AddPath(category, [new PathSegment(1, 1, 2048, 1)], "off")).IsEqual(0);
    }

    [TestCase]
    public void ACategory_HoldsAtMost256Notes()
    {
        var category = set.AddCategory("Many", 0);
        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            AssertThat(set.AddPoint(category, NoteKind.Pin, 1, 1, "p")).IsNotEqual(0);
        }

        AssertThat(set.AddPoint(category, NoteKind.Pin, 1, 1, "p")).IsEqual(0);
    }

    [TestCase]
    public void ALongLabel_IsCutBetweenCharacters()
    {
        var category = set.AddCategory("Text", 0);

        var accented = set.AddPoint(category, NoteKind.Text, 3, 3, new string('a', 46) + "é");
        var exact = set.AddPoint(category, NoteKind.Text, 3, 3, new string('b', 47));

        AssertThat(set.FindNote(accented)!.Label).IsEqual(new string('a', 46));
        AssertThat(set.FindNote(exact)!.Label).IsEqual(new string('b', 47));
    }

    [TestCase]
    public void ReplaceNote_KeepsTheIdAndKind()
    {
        var category = set.AddCategory("Routes", 0);
        var path = set.AddPath(category, TwoSegments, "old");
        var edited = set.FindNote(path)! with { Segments = TwoSegments[..1], Label = "new" };

        AssertThat(set.ReplaceNote(path, edited)).IsTrue();

        var stored = set.FindNote(path)!;
        AssertThat(stored.Id).IsEqual(path);
        AssertThat(stored.Kind).IsEqual(NoteKind.Path);
        AssertThat(stored.Segments).HasSize(1);
        AssertThat(stored.Label).IsEqual("new");
    }

    [TestCase]
    public void ReplaceNote_MovesAPathsAnchorToItsFirstSegment()
    {
        var category = set.AddCategory("Routes", 0);
        var path = set.AddPath(category, TwoSegments, "route");

        set.ReplaceNote(path, set.FindNote(path)! with { Segments = TwoSegments[1..] });

        AssertThat((set.FindNote(path)!.TileX, set.FindNote(path)!.TileY)).IsEqual((3, 4));
    }

    [TestCase]
    public void ReplaceNote_MovesAPinToItsNewTile()
    {
        var category = set.AddCategory("Keys", 0);
        var pin = set.AddPoint(category, NoteKind.Pin, 1, 1, "a");

        AssertThat(set.ReplaceNote(pin, set.FindNote(pin)! with { TileX = 7, TileY = 8 })).IsTrue();
        AssertThat(set.ReplaceNote(pin, set.FindNote(pin)! with { TileX = 2048 })).IsFalse();

        AssertThat((set.FindNote(pin)!.TileX, set.FindNote(pin)!.TileY)).IsEqual((7, 8));
    }

    [TestCase]
    public void RemovingACategory_RemovesItsNotes()
    {
        var keep = set.AddCategory("Keep", 0);
        var doomed = set.AddCategory("Doomed", 1);
        var pin = set.AddPoint(doomed, NoteKind.Pin, 4, 4, "x");

        AssertThat(set.RemoveCategory(doomed)).IsTrue();

        AssertThat(set.FindNote(pin)).IsNull();
        AssertThat(set.FindCategory(keep)).IsNotNull();
    }

    [TestCase]
    public void AddingANote_MarksTheSetChanged()
    {
        var category = set.AddCategory("Keys", 0);
        set.Changed = false;

        set.AddPoint(category, NoteKind.Pin, 1, 1, "x");

        AssertThat(set.Changed).IsTrue();
    }

    [TestCase]
    public void ThePalette_FallsBackToItsFirstColour()
    {
        AssertThat(NotePalette.Rgb(1)).IsEqual(0xFFD34Du);
        AssertThat(NotePalette.Rgb(99)).IsEqual(0x4FC3FFu);
        AssertThat(NotePalette.Rgb(-1)).IsEqual(0x4FC3FFu);
    }

    [TestCase]
    public void Count_CountsEachKind()
    {
        var category = set.AddCategory("Mixed", 0);
        set.AddPoint(category, NoteKind.Pin, 1, 1, "a");
        set.AddPoint(category, NoteKind.Pin, 2, 2, "b");
        set.AddPoint(category, NoteKind.Text, 3, 3, "c");
        set.AddPath(category, TwoSegments[..1], "d");

        var mixed = set.FindCategory(category)!;
        AssertThat(mixed.Count(NoteKind.Pin)).IsEqual(2);
        AssertThat(mixed.Count(NoteKind.Text)).IsEqual(1);
        AssertThat(mixed.Count(NoteKind.Path)).IsEqual(1);
    }

    [TestCase]
    public void ACategory_HoldsAtMost4096PathSegments()
    {
        var flood = set.AddCategory("Flood", 0);
        var other = set.AddCategory("Other", 0);
        for (var path = 0; path < NoteLimits.SegmentsPerCategory / NoteLimits.SegmentsPerPath; path++)
        {
            AssertThat(set.AddPath(flood, LongPath(NoteLimits.SegmentsPerPath), "flood")).IsNotEqual(0);
        }

        AssertThat(set.FindCategory(flood)!.SegmentTotal).IsEqual(NoteLimits.SegmentsPerCategory);
        AssertThat(set.AddPath(flood, TwoSegments[..1], "one more")).IsEqual(0);
        AssertThat(set.AddPath(other, TwoSegments[..1], "elsewhere")).IsNotEqual(0);
    }

    [TestCase]
    public void ReplaceNote_RefusesGrowingAPathPastTheCategorysSegments()
    {
        var flood = set.AddCategory("Flood", 0);
        for (var path = 0; path < NoteLimits.SegmentsPerCategory / NoteLimits.SegmentsPerPath - 1; path++)
        {
            set.AddPath(flood, LongPath(NoteLimits.SegmentsPerPath), "flood");
        }

        set.AddPath(flood, LongPath(NoteLimits.SegmentsPerPath - 1), "almost");
        var small = set.AddPath(flood, TwoSegments[..1], "small");

        AssertThat(set.ReplaceNote(small, set.FindNote(small)! with { Segments = TwoSegments })).IsFalse();
        AssertThat(set.ReplaceNote(small, set.FindNote(small)! with { Label = "renamed" })).IsTrue();
    }

    [TestCase]
    public void Attach_GivesTheCategoryAndItsNotesNewIdsUnderItsParent()
    {
        var parent = set.AddCategory("Parent", 0);
        var detached = NoteCategory.Detached("Keys", 2, true, true, [Pin(77, 5, 6)]);

        var attached = set.FindCategory(set.Attach(detached, parent))!;

        AssertThat(attached.ParentId).IsEqual(parent);
        AssertThat(attached.Id).IsNotEqual(0);
        AssertThat(attached.Notes[0].Id).IsNotEqual(77);
        AssertThat(attached.Notes[0].Id).IsNotEqual(0);
        AssertThat((attached.Notes[0].TileX, attached.Notes[0].TileY)).IsEqual((5, 6));
        AssertThat(attached.Visible).IsTrue();
    }

    [TestCase]
    public void Attach_RefusesAnUnknownParent()
    {
        AssertThat(set.Attach(NoteCategory.Detached("Keys", 0, true, true, []), 999)).IsEqual(0);
    }

    [TestCase]
    public void Attach_KeepsAClashingNameAsItIs()
    {
        var upper = set.Attach(NoteCategory.Detached("Aston", 0, true, true, []), 0);
        var lower = set.Attach(NoteCategory.Detached("aston", 0, true, true, []), 0);

        AssertThat(set.FindCategory(upper)!.Name).IsEqual("Aston");
        AssertThat(set.FindCategory(lower)!.Name).IsEqual("aston");
    }

    [TestCase]
    public void NewNotes_TakeTheirCategorysColour()
    {
        var category = set.AddCategory("Keys", 0);

        var pin = set.AddPoint(category, NoteKind.Pin, 1, 1, "a");
        var path = set.AddPath(category, TwoSegments[..1], "b");

        AssertThat(set.FindNote(pin)!.ColourIndex).IsEqual(Note.CategoryColour);
        AssertThat(set.FindNote(path)!.ColourIndex).IsEqual(Note.CategoryColour);
    }

    [TestCase]
    public void SetNoteColour_OverridesTheCategorysColourUntilCleared()
    {
        var category = set.FindCategory(set.AddCategory("A", 3))!;
        var pin = set.AddPoint(category.Id, NoteKind.Pin, 1, 1, "");

        AssertThat(NoteSet.Rgb(category, set.FindNote(pin)!)).IsEqual(NotePalette.Rgb(3));
        AssertThat(set.SetNoteColour(pin, 2)).IsTrue();
        AssertThat(NoteSet.Rgb(category, set.FindNote(pin)!)).IsEqual(NotePalette.Rgb(2));
        AssertThat(set.SetNoteColour(pin, Note.CategoryColour)).IsTrue();
        AssertThat(NoteSet.Rgb(category, set.FindNote(pin)!)).IsEqual(NotePalette.Rgb(3));
        AssertThat(set.SetNoteColour(pin, 9)).IsFalse();
        AssertThat(set.SetNoteColour(999, 1)).IsFalse();
    }

    [TestCase]
    public void MoveNote_KeepsItsIdAndColourAndRespectsTheLimit()
    {
        var first = set.AddCategory("A", 0);
        var second = set.AddCategory("B", 0);
        var pin = set.AddPoint(first, NoteKind.Pin, 4, 5, "Pin");
        set.SetNoteColour(pin, 4);

        AssertThat(set.MoveNote(pin, 999, -1)).IsFalse();
        AssertThat(set.MoveNote(pin, second, -1)).IsTrue();

        AssertThat(set.OwnerOf(pin)!.Id).IsEqual(second);
        AssertThat(set.FindNote(pin)!.ColourIndex).IsEqual(4);
        AssertThat(set.FindCategory(first)!.Notes).IsEmpty();

        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            set.AddPoint(first, NoteKind.Pin, 1, 1, "");
        }

        AssertThat(set.MoveNote(pin, first, -1)).IsFalse();
    }

    [TestCase]
    public void IsIdenticalTo_ComparesEverythingButTheId()
    {
        var category = set.AddCategory("A", 0);
        var original = set.FindNote(
            set.AddPath(category, [new PathSegment(1, 1, 2, 2), new PathSegment(2, 2, 3, 3)], "Road"))!;

        AssertThat(original.IsIdenticalTo(original with { Id = 999 })).IsTrue();
        AssertThat(original.IsIdenticalTo(original with { Label = "Xoad" })).IsFalse();
        AssertThat(original.IsIdenticalTo(original with { TileX = 2 })).IsFalse();
        AssertThat(original.IsIdenticalTo(original with { ColourIndex = 3 })).IsFalse();
        AssertThat(
                original.IsIdenticalTo(
                    original with { Segments = [new PathSegment(1, 1, 2, 2), new PathSegment(2, 2, 3, 4)] }))
            .IsFalse();
        AssertThat(
                original.IsIdenticalTo(
                    original with { Segments = [new PathSegment(1, 1, 2, 2), new PathSegment(2, 2, 3, 3)] }))
            .IsTrue();
    }

    private static Note Pin(int id, int tileX, int tileY) =>
        new(NoteKind.Pin, "", tileX, tileY, [], Note.CategoryColour) { Id = id };

    private static PathSegment[] LongPath(int segments) =>
        Enumerable.Range(0, segments).Select(index => new PathSegment(index % 2047, 1, index % 2047 + 1, 1)).ToArray();
}
