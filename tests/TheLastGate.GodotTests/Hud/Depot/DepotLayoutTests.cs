using GdUnit4;
using TheLastGate.Characters;
using TheLastGate.Hud.Depot;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
public class DepotLayoutTests
{
    private const int MainNumber = 1;
    private const int AltNumber = 2;
    private const int Last = -1;

    private static readonly DepotCharacter Main = new(MainNumber, "Main");
    private static readonly DepotCharacter Alt = new(AltNumber, "Altchar");

    private DepotLayout layout = null!;

    [BeforeTest]
    public void StartWithBothCharacters()
    {
        layout = new DepotLayout();
        layout.Sync([Main, Alt]);
    }

    [TestCase]
    public void Sync_AddsANewCharacterAtTheBottomWithEightPages()
    {
        AssertThat(Shown([])).ContainsExactly("Main", "Altchar");

        AssertThat(Shown([layout.CategoryOf(AltNumber)]))
            .ContainsExactly(
                "Main",
                "Altchar",
                "  Page 1",
                "  Page 2",
                "  Page 3",
                "  Page 4",
                "  Page 5",
                "  Page 6",
                "  Page 7",
                "  Page 8");
    }

    [TestCase]
    public void Sync_RenamesACharacterOnceItsRealNameArrives()
    {
        layout.Sync([Main, Alt with { Name = CharacterRecord.NewAccountName }]);

        layout.Sync([Main, Alt]);

        AssertThat(Shown([])).ContainsExactly("Main", "Altchar");
    }

    [TestCase]
    public void Sync_HiddenCharacterComesBackWhereItWas()
    {
        layout.RenamePage(DepotLayout.PageId(AltNumber, 2), "Potions");
        var bank = layout.CreateCategory("Bank", 0);
        layout.Drop(CategoryDragOf(layout.CategoryOf(AltNumber)), Onto(bank));
        var expanded = new[] { layout.CategoryOf(MainNumber), layout.CategoryOf(AltNumber), bank };
        var before = Shown(expanded);
        var rowsBefore = layout.Rows(expanded).ToArray();

        layout.Sync([Main]);
        layout.Sync([Main, Alt]);

        AssertThat(Shown(expanded)).ContainsExactly(before);
        AssertThat(layout.Rows(expanded).ToArray()).IsEqual(rowsBefore);
        AssertThat(layout.PageName(DepotLayout.PageId(AltNumber, 2))).IsEqual("Potions");
    }

    [TestCase]
    public void Rows_HiddenCharactersOtherContentsShowOneLevelUp()
    {
        var altCategory = layout.CategoryOf(AltNumber);
        var bank = layout.CreateCategory("Bank", 0);
        layout.Drop(CategoryDragOf(altCategory), Onto(bank));
        var spare = layout.CreateCategory("Spare", altCategory);
        var movedPage = DepotLayout.PageId(MainNumber, 0);
        layout.Drop(PageDrag(movedPage), Onto(altCategory));

        layout.Sync([Main]);

        var rows = layout.Rows([bank, altCategory, spare]);
        AssertThat(Shown([bank, altCategory, spare])).ContainsExactly("Main", "Bank", "  Spare", "  Page 1");
        AssertThat(rows[2]).IsEqual(new CategoryRow(spare, true, 1, bank, 0));
        AssertThat(rows[3]).IsEqual(new CategoryRow(movedPage, false, 1, bank, 1));
    }

    [TestCase]
    public void Rows_EmptiedPlayerCategoryStillShows()
    {
        var bank = layout.CreateCategory("Bank", 0);
        layout.Drop(CategoryDragOf(layout.CategoryOf(AltNumber)), Onto(bank));

        layout.Sync([Main]);

        AssertThat(Shown([bank])).ContainsExactly("Main", "Bank");
    }

    [TestCase]
    public void Drop_PageIntoAnotherCharactersCategory_KeepsItsNameAndNamesTheOwner()
    {
        var page = DepotLayout.PageId(AltNumber, 1);
        AssertThat(layout.PageOwner(page)).IsEqual("");

        AssertThat(layout.Drop(PageDrag(page), Onto(layout.CategoryOf(MainNumber)))).IsTrue();

        AssertThat(layout.PageText(page)).IsEqual("Page 2");
        AssertThat(layout.PageOwner(page)).IsEqual("Altchar");
        AssertThat(Shown([layout.CategoryOf(MainNumber)])[9]).IsEqual("  Page 2");
    }

    [TestCase]
    public void Drop_IndexCountsVisibleRowsAroundAHiddenCharacter()
    {
        var bank = layout.CreateCategory("Bank", 0);
        layout.Drop(CategoryDragOf(layout.CategoryOf(AltNumber)), Onto(bank));
        layout.CreateCategory("Spare", layout.CategoryOf(AltNumber));
        layout.CreateCategory("Gems", bank);
        var tools = layout.CreateCategory("Tools", 0);
        layout.Sync([Main]);

        AssertThat(layout.Drop(CategoryDragOf(tools), new CategoryDropSlot(bank, 1, false))).IsTrue();

        AssertThat(Shown([bank])).ContainsExactly("Main", "Bank", "  Spare", "  Tools", "  Gems");
        layout.Sync([Main, Alt]);
        AssertThat(Shown([bank])).ContainsExactly("Main", "Bank", "  Altchar", "  Tools", "  Gems");
    }

    [TestCase]
    public void CanDrop_RefusesCategoryIntoItsOwnDescendant()
    {
        var outer = layout.CreateCategory("Outer", 0);
        var inner = layout.CreateCategory("Inner", outer);

        AssertThat(layout.CanDrop(CategoryDragOf(outer), Onto(inner))).IsFalse();
        AssertThat(layout.CanDrop(CategoryDragOf(outer), new CategoryDropSlot(inner, 0, false))).IsFalse();
        AssertThat(layout.CanDrop(CategoryDragOf(outer), Onto(outer))).IsFalse();
        AssertThat(layout.CanDrop(CategoryDragOf(inner), Onto(layout.CategoryOf(MainNumber)))).IsTrue();
    }

    [TestCase]
    public void RenamePage_SpacesOnly_ClearsTheName()
    {
        var page = DepotLayout.PageId(MainNumber, 2);
        layout.RenamePage(page, "Potions");

        layout.RenamePage(page, "   ");

        AssertThat(layout.PageText(page)).IsEqual("Page 3");
        AssertThat(layout.PageName(page)).IsEqual("");
    }

    [TestCase]
    public void RenameCategory_RefusesACharacterCategory()
    {
        var bank = layout.CreateCategory("Bank", 0);

        AssertThat(layout.RenameCategory(layout.CategoryOf(MainNumber), "Hoard")).IsFalse();
        AssertThat(layout.RenameCategory(bank, "Vault")).IsTrue();

        AssertThat(layout.CategoryText(layout.CategoryOf(MainNumber))).IsEqual("Main");
        AssertThat(layout.CategoryText(bank)).IsEqual("Vault");
    }

    [TestCase]
    public void ResetPage_GoesHomeAndLosesItsName()
    {
        var page = DepotLayout.PageId(MainNumber, 0);
        layout.Drop(PageDrag(page), Onto(layout.CategoryOf(AltNumber)));
        layout.RenamePage(page, "Gems");

        layout.ResetPage(page);

        AssertThat(layout.PageText(page)).IsEqual("Page 1");
        AssertThat(Shown([layout.CategoryOf(MainNumber)]))
            .ContainsExactly(
                "Main",
                "  Page 2",
                "  Page 3",
                "  Page 4",
                "  Page 5",
                "  Page 6",
                "  Page 7",
                "  Page 8",
                "  Page 1",
                "Altchar");
    }

    [TestCase]
    public void DeleteCategory_ResetsEveryPageBeneathAndLiftsCharacters()
    {
        var outer = layout.CreateCategory("Outer", 0);
        var bank = layout.CreateCategory("Bank", outer);
        var sub = layout.CreateCategory("Sub", bank);
        var movedPage = DepotLayout.PageId(MainNumber, 4);
        layout.Drop(PageDrag(movedPage), Onto(sub));
        layout.RenamePage(movedPage, "Gems");
        var altCategory = layout.CategoryOf(AltNumber);
        layout.Drop(CategoryDragOf(altCategory), Onto(bank));
        layout.RenamePage(DepotLayout.PageId(AltNumber, 0), "Potions");

        var summary = layout.DeleteSummary(bank);
        layout.DeleteCategory(bank);

        AssertThat(summary).IsEqual((Pages: 2, Categories: 1));
        AssertThat(Shown([layout.CategoryOf(MainNumber), outer, altCategory, sub]))
            .ContainsExactly(
                "Main",
                "  Page 1",
                "  Page 2",
                "  Page 3",
                "  Page 4",
                "  Page 6",
                "  Page 7",
                "  Page 8",
                "  Page 5",
                "Outer",
                "  Altchar",
                "    Page 1",
                "    Page 2",
                "    Page 3",
                "    Page 4",
                "    Page 5",
                "    Page 6",
                "    Page 7",
                "    Page 8");
    }

    [TestCase]
    public void DeleteSummary_LeavesOutCharactersWhosePagesStayHome()
    {
        var mules = layout.CreateCategory("Mules", 0);
        layout.Drop(CategoryDragOf(layout.CategoryOf(AltNumber)), Onto(mules));

        AssertThat(layout.DeleteSummary(mules)).IsEqual((Pages: 0, Categories: 0));
    }

    private static CategoryDrag CategoryDragOf(int categoryId) => new(categoryId, true);

    private static CategoryDrag PageDrag(int pageId) => new(pageId, false);

    private static CategoryDropSlot Onto(int categoryId) => new(categoryId, Last, true);

    private string[] Shown(IReadOnlyCollection<int> expanded) => layout.Rows(expanded)
        .Select(row => new string(' ', row.Depth * 2)
            + (row.IsCategory ? layout.CategoryText(row.Id) : layout.PageText(row.Id)))
        .ToArray();
}
