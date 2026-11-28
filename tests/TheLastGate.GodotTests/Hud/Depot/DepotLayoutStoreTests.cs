using GdUnit4;
using TheLastGate.Characters;
using TheLastGate.Hud.Depot;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
public class DepotLayoutStoreTests
{
    private const int MainNumber = 1;
    private const int AltNumber = 2;
    private const int FirstCharacterCategory = 1;

    private static readonly DepotCharacter Main = new(MainNumber, "Main");
    private static readonly DepotCharacter Alt = new(AltNumber, "Altchar");

    [TestCase]
    public void SaveThenLoad_KeepsBothSeasonsTreesNamesAndOrder()
    {
        var layouts = new DepotLayouts();
        var legacy = layouts.For(Season.Legacy);
        legacy.Sync([Main, Alt]);
        var group = legacy.CreateCategory("Gear", 0);
        legacy.RenamePage(DepotLayout.PageId(MainNumber, 2), "Armour");
        legacy.Drop(
            new CategoryDrag(DepotLayout.PageId(AltNumber, 0), false),
            new CategoryDropSlot(group, 0, false));
        layouts.For(Season.Current).Sync([Main]);

        var loaded = DepotLayoutStore.Load(DepotLayoutStore.Save(layouts));

        AssertThat(loaded).IsNotNull();
        foreach (var season in new[] { Season.Legacy, Season.Current })
        {
            var expected = layouts.For(season);
            var actual = loaded!.For(season);
            AssertThat(actual.Categories).ContainsExactly(expected.Categories);
            AssertThat(actual.Pages).ContainsExactly(expected.Pages);
            AssertThat(actual.NextCategoryId).IsEqual(expected.NextCategoryId);
            AssertThat(actual.Changed).IsFalse();
        }

        AssertThat(loaded!.For(Season.Legacy).PageName(DepotLayout.PageId(MainNumber, 2))).IsEqual("Armour");
    }

    [TestCase]
    public void Load_Garbage_IsNull()
    {
        AssertThat(DepotLayoutStore.Load("not json {")).IsNull();
        AssertThat(DepotLayoutStore.Load("")).IsNull();
        AssertThat(DepotLayoutStore.Load("[1,2]")).IsNull();
        AssertThat(DepotLayoutStore.Load("null")).IsNull();
        AssertThat(DepotLayoutStore.Load("""{"version":"x"}""")).IsNull();
    }

    [TestCase]
    public void Load_NewerVersion_IsNull() =>
        AssertThat(DepotLayoutStore.Load("""{"version":2,"seasons":{}}""")).IsNull();

    [TestCase]
    public void Load_RepairsDanglingParentsAndDuplicates()
    {
        const string json = """
            {"version":1,"seasons":{"Legacy":{"nextId":2,
            "categories":[
              {"id":1,"parentId":0,"characterNumber":1,"name":"Main"},
              {"id":1,"parentId":0,"characterNumber":0,"name":"Duplicate"},
              {"id":5,"parentId":9,"characterNumber":0,"name":"Orphan"},
              {"id":6,"parentId":7,"characterNumber":0,"name":"CycleA"},
              {"id":7,"parentId":6,"characterNumber":0,"name":"CycleB"}],
            "pages":[
              {"characterNumber":1,"page":0,"parentId":99,"name":"Lost"},
              {"characterNumber":1,"page":0,"parentId":1,"name":"Twin"},
              {"characterNumber":3,"page":1,"parentId":99,"name":"Stray"}]}}}
            """;

        var layout = DepotLayoutStore.Load(json)!.For(Season.Legacy);

        AssertThat(layout.Categories.Select(category => category.Id)).ContainsExactly(1, 5, 6, 7);
        AssertThat(layout.Categories.First(category => category.Id == 5).ParentId).IsEqual(0);
        AssertThat(layout.Pages.Count).IsEqual(2);
        AssertThat(layout.Pages.First(page => page.CharacterNumber == 1).ParentId).IsEqual(FirstCharacterCategory);
        AssertThat(layout.Pages.First(page => page.CharacterNumber == 1).Name).IsEqual("Lost");
        AssertThat(layout.Pages.First(page => page.CharacterNumber == 3).ParentId).IsEqual(FirstCharacterCategory);
        AssertThat(layout.NextCategoryId).IsGreater(7);
    }

    [TestCase]
    public void Load_NullListElements_AreSkipped()
    {
        const string json = """
            {"version":1,"seasons":{"Legacy":{"nextId":1,"categories":[null],"pages":[null]}}}
            """;

        var layout = DepotLayoutStore.Load(json)!.For(Season.Legacy);

        AssertThat(layout.Categories.Count).IsEqual(0);
        AssertThat(layout.Pages.Count).IsEqual(0);
    }

    [TestCase]
    public void Load_ChainEndingAtAMissingParent_KeepsTheChain()
    {
        const string json = """
            {"version":1,"seasons":{"Legacy":{"nextId":1,"categories":[
              {"id":1,"parentId":2,"characterNumber":0,"name":"A"},
              {"id":2,"parentId":99,"characterNumber":0,"name":"B"}],"pages":[]}}}
            """;

        var layout = DepotLayoutStore.Load(json)!.For(Season.Legacy);

        AssertThat(layout.Categories.Select(category => category.ParentId)).ContainsExactly(2, 0);
    }

    [TestCase]
    public void Load_OnlyCutsACategoryOnTheCycle()
    {
        const string json = """
            {"version":1,"seasons":{"Legacy":{"nextId":1,"categories":[
              {"id":3,"parentId":1,"characterNumber":0,"name":"C"},
              {"id":1,"parentId":2,"characterNumber":0,"name":"A"},
              {"id":2,"parentId":1,"characterNumber":0,"name":"B"}],"pages":[]}}}
            """;

        var layout = DepotLayoutStore.Load(json)!.For(Season.Legacy);

        AssertThat(layout.Categories.Select(category => (category.Id, category.ParentId)))
            .ContainsExactly((3, 1), (1, 0), (2, 1));
    }
}
