using GdUnit4;
using TheLastGate.Hud.Depot;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
public class DepotListSourceTests
{
    private const int MainNumber = 1;
    private const int AltNumber = 2;
    private const int OpenPage = 2;
    private const int Last = -1;

    private DepotLayout layout = null!;
    private DepotListSource source = null!;

    [BeforeTest]
    public void ListBothCharacters()
    {
        layout = new DepotLayout();
        layout.Sync([new DepotCharacter(MainNumber, "Main"), new DepotCharacter(AltNumber, "Altchar")]);
        source = new DepotListSource(layout);
    }

    [TestCase]
    public void DepotListSource_APageAwayFromItsCharacterNamesTheOwnerOnHover()
    {
        var page = DepotLayout.PageId(AltNumber, 0);
        var mainCategory = layout.CategoryOf(MainNumber);
        layout.Drop(new CategoryDrag(page, false), new CategoryDropSlot(mainCategory, Last, true));
        source.Expand(mainCategory, true);

        var row = source.BuildRows().Single(candidate => !candidate.IsCategory && candidate.Id == page);

        AssertThat(source.Look(row)).IsEqual(new CategoryRowLook("Page 1", null, CategoryRowState.Normal, "Altchar"));
    }

    [TestCase]
    public void DepotListSource_OpenPageLooksCurrentAndItsCharacterExpands()
    {
        var altCategory = layout.CategoryOf(AltNumber);

        source.ShowOpen(AltNumber, OpenPage);

        AssertThat(source.Expanded).ContainsExactly(altCategory);
        var rows = source.BuildRows();
        var open = rows.Single(row => !row.IsCategory && row.Id == DepotLayout.PageId(AltNumber, OpenPage));
        var other = rows.Single(row => !row.IsCategory && row.Id == DepotLayout.PageId(AltNumber, 0));
        AssertThat(source.Look(open)).IsEqual(new CategoryRowLook("Page 3", null, CategoryRowState.Current));
        AssertThat(source.Look(other).State).IsEqual(CategoryRowState.Normal);
        AssertThat(source.Look(rows.Single(row => row.IsCategory && row.Id == altCategory)).Text)
            .IsEqual("Altchar");

        source.Expand(altCategory, false);
        source.ShowOpen(AltNumber, OpenPage);

        AssertThat(source.Expanded).IsEmpty();
    }

    [TestCase]
    public void DepotListSource_TurningToAPageInACollapsedCategoryOpensIt()
    {
        var bank = layout.CreateCategory("Bank", 0);
        var filedPage = DepotLayout.PageId(MainNumber, OpenPage);
        layout.Drop(new CategoryDrag(filedPage, false), new CategoryDropSlot(bank, Last, true));
        source.ShowOpen(MainNumber, 0);
        AssertThat(source.Expanded).NotContains(bank);

        source.ShowOpen(MainNumber, OpenPage);

        AssertThat(source.Expanded).Contains(bank);
        var filed = source.BuildRows().Single(row => !row.IsCategory && row.Id == filedPage);
        AssertThat(source.Look(filed).State).IsEqual(CategoryRowState.Current);
    }

    [TestCase]
    public void DepotListSource_OpensThePathToAnOpenPageInsideCollapsedCategories()
    {
        var altCategory = layout.CategoryOf(AltNumber);
        var alts = layout.CreateCategory("Alts", 0);
        layout.Drop(new CategoryDrag(altCategory, true), new CategoryDropSlot(alts, Last, true));
        var bank = layout.CreateCategory("Bank", 0);
        var potions = layout.CreateCategory("Potions", bank);
        var openPage = DepotLayout.PageId(AltNumber, OpenPage);
        layout.Drop(new CategoryDrag(openPage, false), new CategoryDropSlot(potions, Last, true));

        source.ShowOpen(AltNumber, OpenPage);

        AssertThat(source.Expanded).ContainsExactlyInAnyOrder(alts, altCategory, bank, potions);
        var open = source.BuildRows().Single(row => !row.IsCategory && row.Id == openPage);
        AssertThat(source.Look(open).State).IsEqual(CategoryRowState.Current);
    }
}
