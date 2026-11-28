using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Hud.Depot;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
[RequireGodotRuntime]
public class DepotCharactersSceneTests
{
    private const string ScenePath = "res://Hud/Depot/depot_characters.tscn";
    private const int MainNumber = 1;
    private const int AltNumber = 2;
    private const int Last = -1;

    private List<(int CharacterNumber, int Page)> chosen = [];
    private DepotLayout layout = null!;
    private int layoutChanges;
    private DepotCharactersView panel = null!;
    private List<int> sorts = [];

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private CategoryListView List => panel.GetNode<CategoryListView>("%List");

    private ProceduralMenu Menu => panel.GetNode<ProceduralMenu>("%Menu");

    private string DeleteQuestion => panel.GetNode<Label>("%DeleteQuestion").Text;

    [BeforeTest]
    public async Task ShowThePanel()
    {
        layout = new DepotLayout();
        layout.Sync([new DepotCharacter(MainNumber, "Main"), new DepotCharacter(AltNumber, "Altchar")]);
        chosen = [];
        sorts = [];
        layoutChanges = 0;
        panel = GD.Load<PackedScene>(ScenePath).Instantiate<DepotCharactersView>();
        Tree.Root.AddChild(panel);
        panel.PageChosen += (number, page) => chosen.Add((number, page));
        panel.LayoutChanged += () => layoutChanges++;
        panel.SortWanted += sorts.Add;
        await Frame();
    }

    [AfterTest]
    public void FreeThePanel() => panel.Free();

    [TestCase]
    public async Task ClickingAPageRow_RaisesPageChosen()
    {
        await ShowOpen(MainNumber);

        Click(RowIndexOf("Page 2"), MapButton.Left);

        AssertThat(chosen).ContainsExactly((MainNumber, 1));
    }

    [TestCase]
    public async Task SortFromAPagesMenu_AsksToSortItsCharactersDepot()
    {
        var pageId = DepotLayout.PageId(AltNumber, 2);
        layout.RenamePage(pageId, "Potions");
        layout.Drop(new CategoryDrag(pageId, false), Onto(layout.CategoryOf(MainNumber)));
        await ShowOpen(MainNumber);

        Click(RowIndexOf("Potions"), MapButton.Right);
        ChooseFromMenu("Sort depot");

        AssertThat(sorts).ContainsExactly(AltNumber);
    }

    [TestCase]
    public async Task SortFromACharactersMenu_AsksToSortTheirDepot()
    {
        await ShowOpen(MainNumber);

        Click(RowIndexOf("Altchar"), MapButton.Right);
        ChooseFromMenu("Sort depot");

        AssertThat(sorts).ContainsExactly(AltNumber);
    }

    [TestCase]
    public async Task TurningTheOpenPage_ScrollsTheListToIt()
    {
        const int thirdNumber = 3;
        const int fourthNumber = 4;
        const int rowsPerCharacter = 9;
        layout.Sync(
        [
            new DepotCharacter(MainNumber, "Main"), new DepotCharacter(AltNumber, "Altchar"),
            new DepotCharacter(thirdNumber, "Third"), new DepotCharacter(fourthNumber, "Fourth"),
        ]);
        foreach (var number in new[] { MainNumber, AltNumber, thirdNumber, fourthNumber })
        {
            await ShowOpen(number);
        }

        panel.Show(layout, fourthNumber, 7);
        await Frame();
        AssertThat(IsRowShown(3 * rowsPerCharacter + 8)).IsTrue();

        panel.Show(layout, MainNumber, 0);
        await Frame();
        AssertThat(IsRowShown(1)).IsTrue();
    }

    [TestCase]
    public async Task RenamingAPage_ChangesItsTextAndRaisesLayoutChanged()
    {
        await ShowOpen(MainNumber);
        var row = RowIndexOf("Page 3");

        Click(row, MapButton.Right);
        ChooseFromMenu("Rename");
        var field = Row(row).GetNode<LineEdit>("Cells/Field");
        AssertThat(field.Text).IsEqual("");
        field.Text = "Potions";
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = true });
        Input.FlushBufferedEvents();
        await Frame();

        AssertThat(RowText(row)).IsEqual("Potions");
        AssertThat(layout.PageName(DepotLayout.PageId(MainNumber, 2))).IsEqual("Potions");
        AssertThat(layoutChanges).IsEqual(1);
    }

    [TestCase]
    public async Task DeleteQuestion_CountsPagesAndCategories()
    {
        var stash = layout.CreateCategory("Stash", 0);
        layout.CreateCategory("Inner", stash);
        for (var page = 0; page < 8; page++)
        {
            layout.Drop(PageDrag(MainNumber, page), Onto(stash));
        }

        layout.Drop(PageDrag(AltNumber, 0), Onto(stash));
        layout.CreateCategory("Empty", 0);
        await ShowOpen(0);

        Click(RowIndexOf("Stash"), MapButton.Right);
        ChooseFromMenu("Delete...");
        AssertThat(DeleteQuestion)
            .IsEqual(
                "Delete Stash? 9 pages go back to their characters and lose their names, "
                + "and 1 category inside it is deleted too.");

        Click(RowIndexOf("Empty"), MapButton.Right);
        ChooseFromMenu("Delete...");
        AssertThat(DeleteQuestion).IsEqual("Delete Empty?");
    }

    [TestCase]
    public async Task DeleteQuestion_OnlyAsksWhenNoPageWouldChange()
    {
        var mules = layout.CreateCategory("Mules", 0);
        layout.Drop(new CategoryDrag(layout.CategoryOf(AltNumber), true), Onto(mules));
        await ShowOpen(0);

        Click(RowIndexOf("Mules"), MapButton.Right);
        ChooseFromMenu("Delete...");

        AssertThat(DeleteQuestion).IsEqual("Delete Mules?");
    }

    [TestCase]
    public async Task ResetFromTheMenu_SendsThePageHome()
    {
        var pageId = DepotLayout.PageId(MainNumber, 1);
        layout.RenamePage(pageId, "Potions");
        layout.Drop(new CategoryDrag(pageId, false), Onto(layout.CategoryOf(AltNumber)));
        await ShowOpen(AltNumber);

        Click(RowIndexOf("Potions"), MapButton.Right);
        ChooseFromMenu("Reset");

        AssertThat(layout.PageText(pageId)).IsEqual("Page 2");
        AssertThat(layout.Pages.Single(page => page is { CharacterNumber: MainNumber, Page: 1 }).ParentId)
            .IsEqual(layout.CategoryOf(MainNumber));
        AssertThat(layoutChanges).IsEqual(1);
    }

    [TestCase]
    public async Task HidingThePanel_ClosesItsPromptAndLetsEscapeThrough()
    {
        await ShowOpen(MainNumber);
        Press("%NewCategory");

        panel.Visible = false;
        Tree.Root.PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = true });
        AssertThat(Tree.Root.IsInputHandled()).IsFalse();

        panel.Visible = true;
        await Frame();
        AssertThat(panel.GetNode<Control>("%NewBlocker").Visible).IsFalse();
    }

    [TestCase]
    public async Task HidingThePanel_ClosesItsMenu()
    {
        await ShowOpen(MainNumber);
        Click(RowIndexOf("Page 2"), MapButton.Right);
        AssertThat(Menu.IsOpen).IsTrue();

        panel.Visible = false;
        panel.Visible = true;

        AssertThat(Menu.IsOpen).IsFalse();
    }

    [TestCase]
    public async Task NewCategory_RefusesABlankName()
    {
        await ShowOpen(MainNumber);
        var create = panel.GetNode<ProceduralButton>("%NewCreate");
        var name = panel.GetNode<LineEdit>("%NewName");

        Press("%NewCategory");
        Type(name, "   ");
        AssertThat(create.Disabled).IsTrue();
        Type(name, "Bank");
        AssertThat(create.Disabled).IsFalse();
        Press("%NewCreate");

        AssertThat(layout.Categories.Select(category => category.Name)).Contains("Bank");
        AssertThat(layoutChanges).IsEqual(1);
    }

    private static void Type(LineEdit field, string text)
    {
        field.Text = text;
        field.EmitSignal(LineEdit.SignalName.TextChanged, text);
    }

    private void Press(string button) =>
        panel.GetNode<ClickArea>($"{button}/Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

    private static CategoryDrag PageDrag(int characterNumber, int page) =>
        new(DepotLayout.PageId(characterNumber, page), false);

    private static CategoryDropSlot Onto(int categoryId) => new(categoryId, Last, true);

    private static async Task Frame() => await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

    private bool IsRowShown(int index) =>
        index >= List.ScrollBar.Value
        && index < List.ScrollBar.Value + List.GetNode("Cells/List/Rows").GetChildCount();

    private async Task ShowOpen(int characterNumber)
    {
        panel.Show(layout, characterNumber, 0);
        await Frame();
        await Frame();
    }

    private CategoryRowView Row(int index) => List.GetNode<CategoryRowView>($"Cells/List/Rows/Row{index}");

    private string RowText(int index) => Row(index).GetNode<HudLabel>("Cells/Name").Text;

    private int RowIndexOf(string text)
    {
        for (var index = 0; List.GetNodeOrNull($"Cells/List/Rows/Row{index}") != null; index++)
        {
            if (Row(index).Visible && RowText(index) == text)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"No row shows \"{text}\".");
    }

    private void Click(int index, MapButton button) =>
        Row(index).GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(button));

    private void ChooseFromMenu(string text)
    {
        var item = Menu.GetNode("Frame/Items")
            .GetChildren()
            .Single(row => row.GetNodeOrNull<HudLabel>("Cells/Label")?.Text == text);
        item.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
    }
}
