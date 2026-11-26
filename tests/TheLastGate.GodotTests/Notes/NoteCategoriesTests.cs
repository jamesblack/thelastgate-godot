using GdUnit4;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Notes;

[TestSuite]
public class NoteCategoriesTests
{
    private NoteSet set = new();

    [BeforeTest]
    public void StartWithNoNotes() => set = new NoteSet();

    [TestCase]
    public void Ids_StartAtOneAndNeverRepeat()
    {
        var first = set.AddCategory("A", 0);
        var second = set.AddCategory("B", 0);

        AssertThat(first).IsEqual(1);
        AssertThat(second).IsEqual(2);
    }

    [TestCase]
    public void ANewCategory_IsShownWithItsLabelsAndMarksTheSetChanged()
    {
        var keys = set.FindCategory(set.AddCategory("Keys", 3))!;

        AssertThat(keys.Visible).IsTrue();
        AssertThat(keys.ShowLabels).IsTrue();
        AssertThat(keys.ColourIndex).IsEqual(3);
        AssertThat(keys.ParentId).IsEqual(0);
        AssertThat(set.Changed).IsTrue();
    }

    [TestCase]
    public void AnInvalidColour_FallsBackToTheFirst()
    {
        AssertThat(set.FindCategory(set.AddCategory("Keys", 99))!.ColourIndex).IsEqual(0);
    }

    [TestCase]
    public void SiblingNames_AreUniqueIgnoringCaseButCousinsMayShare()
    {
        var aston = set.AddCategory("Aston", 0);
        var neiseer = set.AddCategory("Neiseer", 0);

        var first = set.AddCategory("Quest Givers", 0, aston);
        var second = set.AddCategory("Quest Givers", 0, neiseer);
        var third = set.AddCategory("quest givers", 0, aston);

        AssertThat(set.FindCategory(first)!.Name).IsEqual("Quest Givers");
        AssertThat(set.FindCategory(second)!.Name).IsEqual("Quest Givers");
        AssertThat(set.FindCategory(third)!.Name).IsEqual("quest givers (2)");
    }

    [TestCase]
    public void ALongName_IsCutToFitAndKeepsRoomForItsNumber()
    {
        var wanted = new string('x', 40);

        var first = set.AddCategory(wanted, 0);
        var second = set.AddCategory(wanted, 0);

        AssertThat(set.FindCategory(first)!.Name).IsEqual(new string('x', 31));
        AssertThat(set.FindCategory(second)!.Name).IsEqual(new string('x', 27) + " (2)");
    }

    [TestCase]
    public void Depth_StopsAtEightLevels()
    {
        var parent = 0;
        for (var level = 1; level <= NoteLimits.Depth; level++)
        {
            parent = set.AddCategory("Level", 0, parent);
            AssertThat(parent).IsNotEqual(0);
        }

        AssertThat(set.AddCategory("Too deep", 0, parent)).IsEqual(0);
        AssertThat(set.Depth(parent)).IsEqual(NoteLimits.Depth);
        AssertThat(set.Depth(999)).IsEqual(0);
    }

    [TestCase]
    public void AddCategory_RefusesAnUnknownParent()
    {
        AssertThat(set.AddCategory("Orphan", 0, 999)).IsEqual(0);
    }

    [TestCase]
    public void AddCategory_StopsAtTheCategoryLimit()
    {
        for (var category = 0; category < NoteLimits.Categories; category++)
        {
            set.AddCategory($"C{category}", 0);
        }

        AssertThat(set.AddCategory("One too many", 0)).IsEqual(0);
    }

    [TestCase]
    public void RemoveCategory_RefusesACategoryWithChildren()
    {
        var parent = set.AddCategory("Parent", 0);
        var child = set.AddCategory("Child", 0, parent);

        AssertThat(set.HasChildren(parent)).IsTrue();
        AssertThat(set.RemoveCategory(parent)).IsFalse();
        AssertThat(set.RemoveCategory(child)).IsTrue();
        AssertThat(set.HasChildren(parent)).IsFalse();
        AssertThat(set.RemoveCategory(parent)).IsTrue();
        AssertThat(set.Categories).IsEmpty();
    }

    [TestCase]
    public void AHiddenAncestor_HidesItsDescendants()
    {
        var parent = set.FindCategory(set.AddCategory("Parent", 0))!;
        var child = set.FindCategory(set.AddCategory("Child", 0, parent.Id))!;

        parent.Visible = false;
        AssertThat(set.IsShown(child)).IsFalse();
        parent.Visible = true;
        AssertThat(set.IsShown(child)).IsTrue();

        parent.ShowLabels = false;
        AssertThat(set.LabelsShown(child)).IsFalse();
        parent.ShowLabels = true;
        AssertThat(set.LabelsShown(child)).IsTrue();
    }

    [TestCase]
    public void Tree_ListsCategoriesParentFirst()
    {
        var first = set.AddCategory("A", 0);
        var second = set.AddCategory("B", 0);
        set.AddCategory("A1", 0, first);

        AssertThat(set.Tree().Select(category => category.Name)).ContainsExactly("A", "A1", "B");
        AssertThat(set.Tree(first).Select(category => category.Name)).ContainsExactly("A", "A1");
        AssertThat(set.Tree(second).Select(category => category.Name)).ContainsExactly("B");
        AssertThat(set.Tree(999)).IsEmpty();
    }

    [TestCase]
    public void IsWithinAndSubtreeHeight_FollowTheTree()
    {
        var first = set.AddCategory("A", 0);
        var second = set.AddCategory("B", 0);
        var child = set.AddCategory("A1", 0, first);

        AssertThat(set.IsWithin(child, first)).IsTrue();
        AssertThat(set.IsWithin(first, child)).IsFalse();
        AssertThat(set.IsWithin(first, first)).IsTrue();
        AssertThat(set.SubtreeHeight(first)).IsEqual(2);
        AssertThat(set.SubtreeHeight(second)).IsEqual(1);
    }

    [TestCase]
    public void RenameCategory_AddsANumberOnlyWhenASiblingHasTheName()
    {
        var first = set.AddCategory("Aston", 0);
        var second = set.AddCategory("Bank", 0);

        AssertThat(set.RenameCategory(second, "aston")).IsTrue();
        AssertThat(set.FindCategory(second)!.Name).IsEqual("aston (2)");
        AssertThat(set.RenameCategory(first, "ASTON")).IsTrue();
        AssertThat(set.FindCategory(first)!.Name).IsEqual("ASTON");
        AssertThat(set.RenameCategory(first, "")).IsFalse();
        AssertThat(set.RenameCategory(999, "Name")).IsFalse();
    }

    [TestCase]
    public void SetCategoryColour_RefusesAColourOffThePalette()
    {
        var category = set.AddCategory("A", 0);

        AssertThat(set.SetCategoryColour(category, 5)).IsTrue();
        AssertThat(set.SetCategoryColour(category, NotePalette.Size)).IsFalse();
        AssertThat(set.FindCategory(category)!.ColourIndex).IsEqual(5);
    }

    [TestCase]
    public void MoveCategory_RefusesLoops()
    {
        var first = set.AddCategory("A", 0);
        var child = set.AddCategory("A1", 0, first);

        AssertThat(set.MoveCategory(first, child, -1)).IsFalse();
        AssertThat(set.MoveCategory(first, first, -1)).IsFalse();
        AssertThat(set.MoveCategory(child, 0, -1)).IsTrue();
        AssertThat(set.FindCategory(child)!.ParentId).IsEqual(0);
    }

    [TestCase]
    public void MoveCategory_TakesItsSubtreeAndANumberIfTheNameClashes()
    {
        var first = set.AddCategory("A", 0);
        var other = set.AddCategory("B", 0);
        var givers = set.AddCategory("Quest Givers", 0, other);
        var nested = set.AddCategory("quest givers", 0, first);
        set.AddCategory("Leaf", 0, nested);

        AssertThat(set.MoveCategory(nested, other, -1)).IsTrue();

        AssertThat(set.FindCategory(nested)!.ParentId).IsEqual(other);
        AssertThat(set.FindCategory(nested)!.Name).IsEqual("quest givers (2)");
        AssertThat(set.SubtreeHeight(nested)).IsEqual(2);
        AssertThat(set.FindCategory(givers)!.Name).IsEqual("Quest Givers");
    }

    [TestCase]
    public void MoveCategory_RefusesGoingDeeperThanEightLevels()
    {
        var chain = 0;
        for (var level = 1; level < NoteLimits.Depth; level++)
        {
            chain = set.AddCategory("Chain", 0, chain);
        }

        var pair = set.AddCategory("Pair", 0);
        set.AddCategory("Pair child", 0, pair);

        AssertThat(set.CheckCategoryMove(pair, chain)).IsEqual(NoteMoveRefusal.TooDeep);
        AssertThat(set.MoveCategory(pair, chain, -1)).IsFalse();
    }
}
