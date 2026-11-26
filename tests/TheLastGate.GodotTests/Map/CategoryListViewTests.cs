using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class CategoryListViewTests
{
    private static readonly Vector2 ListSize = new(220, 120);

    private List<int> clashes = [];
    private CategoryListView list = null!;
    private NoteSet notes = new();
    private WorldMapWindow window = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowAnEmptyList()
    {
        notes = new NoteSet();
        window = new WorldMapWindow();
        clashes = [];
        list = GD.Load<PackedScene>("res://Map/category_list.tscn").Instantiate<CategoryListView>();
        Tree.Root.AddChild(list);
        list.Size = ListSize;
        list.ClashFound += (_, _, clashId) => clashes.Add(clashId);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheList() => list.Free();

    [TestCase]
    public async Task Show_FillsRowsFromTheTree()
    {
        var places = notes.AddCategory("Places", 1);
        notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        notes.AddCategory("Quests", 4);
        window.Editor.Expand(places, true);

        await ShowNotes();

        AssertThat(RowText(0)).IsEqual("Places");
        AssertThat(RowText(1)).IsEqual("Pin: Tavern");
        AssertThat(RowText(2)).IsEqual("Quests");
        AssertThat(Row(3).Visible).IsFalse();
    }

    [TestCase]
    public async Task WithoutNotes_NoRowsShow()
    {
        list.Show(window, null);
        await Frame();

        AssertThat(Row(0).Visible).IsFalse();
    }

    [TestCase]
    public async Task Scroll_ShiftsTheRows()
    {
        for (var count = 0; count < 20; count++)
        {
            notes.AddCategory($"Category {count}", 0);
        }

        await ShowNotes();
        list.ScrollBar.Value = 3;
        await Frame();

        AssertThat(RowText(0)).IsEqual("Category 3");
    }

    [TestCase]
    public async Task Collapse_AtTheBottom_ClampsTheScroll()
    {
        var camps = notes.AddCategory("Camps", 0);
        for (var count = 0; count < 30; count++)
        {
            notes.AddPoint(camps, NoteKind.Pin, 1, count, $"Camp {count}");
        }

        window.Editor.Expand(camps, true);
        await ShowNotes();
        list.ScrollBar.Value = list.ScrollBar.MaxValue;
        await Frame();

        window.Editor.Expand(camps, false);
        await Frame();

        AssertThat(list.ScrollBar.Value).IsEqual(0d);
        AssertThat(RowText(0)).IsEqual("Camps");
    }

    [TestCase]
    public async Task DropOntoCategory_MovesTheNote()
    {
        var from = notes.AddCategory("From", 0);
        var to = notes.AddCategory("To", 0);
        var pin = notes.AddPoint(from, NoteKind.Pin, 1, 1, "Camp");
        window.Editor.Expand(from, true);
        await ShowNotes();

        var data = list._GetDragData(Middle(1));
        AssertThat(list._CanDropData(Middle(2), data)).IsTrue();
        list._DropData(Middle(2), data);

        AssertThat(notes.OwnerOf(pin)!.Id).IsEqual(to);
    }

    [TestCase]
    public async Task DropOnALine_Reorders()
    {
        var first = notes.AddCategory("A", 0);
        var second = notes.AddCategory("B", 0);
        await ShowNotes();

        var data = list._GetDragData(Middle(1));
        list._DropData(TopOf(0), data);

        AssertThat(notes.Tree().Select(category => category.Id)).ContainsExactly(second, first);
    }

    [TestCase]
    public async Task DropWithAClash_RaisesClashFound()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        await ShowNotes();

        list._DropData(Middle(0), list._GetDragData(Middle(1)));

        AssertThat(clashes).ContainsExactly(there);
        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(0);
    }

    [TestCase]
    public async Task DragData_WhileEditing_IsNull()
    {
        var camps = notes.AddCategory("Camps", 0);
        var pin = notes.AddPoint(camps, NoteKind.Pin, 1, 1, "Fire");
        window.Editor.BeginEdit(notes, pin);
        await ShowNotes();

        AssertThat(list._GetDragData(Middle(0)).VariantType).IsEqual(Variant.Type.Nil);
    }

    [TestCase]
    public async Task DragEnded_WithoutDrop_ClearsHighlights()
    {
        notes.AddCategory("A", 0);
        notes.AddCategory("B", 0);
        await ShowNotes();
        var data = list._GetDragData(Middle(1));
        list._CanDropData(TopOf(0), data);
        AssertThat(list.GetNode<Control>("DropLine").Visible).IsTrue();

        list.Notification((int)Node.NotificationDragEnd);
        await Frame();

        AssertThat(list.GetNode<Control>("DropLine").Visible).IsFalse();
        AssertThat(Row(1).Modulate.A).IsEqual(1f);
    }

    [TestCase]
    public async Task DuringADrag_RowsDoNotLightUp()
    {
        notes.AddCategory("A", 0);
        notes.AddCategory("B", 0);
        await ShowNotes();
        list._GetDragData(Middle(1));

        Row(0).GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);
        await Frame();

        AssertThat(Row(0).Modulate).IsEqual(Colors.White);
    }

    [TestCase]
    public async Task AGhost_HasNoIndentAndShowsItsWholeName()
    {
        var places = notes.AddCategory("Places", 0);
        var deep = notes.AddCategory("A rather long name", 0, places);
        var ghost = GD.Load<PackedScene>("res://Map/category_row.tscn").Instantiate<CategoryRowView>();
        Tree.Root.AddChild(ghost);

        ghost.Show(new CategoryRow(deep, true, 1, places, 0), notes, CategoryRowState.Ghost, false);

        AssertThat(ghost.GetNode<Control>("Cells/Indent").CustomMinimumSize.X).IsEqual(0f);
        AssertThat(ghost.GetNode<Label>("Cells/Name").ClipText).IsFalse();
        ghost.Free();
    }

    [TestCase]
    public async Task TheDragGhost_DrawsAboveTheWindowPrompts()
    {
        notes.AddCategory("A", 0);
        notes.AddCategory("B", 0);
        await ShowNotes();
        var start = list.GlobalPosition + Middle(0);
        var prompt = GD.Load<PackedScene>("res://Map/merge_prompt.tscn").Instantiate<Control>();

        Send(
            new InputEventMouseButton
                { ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left },
            start);
        var away = new Vector2(0, 30);
        Send(new InputEventMouseMotion { ButtonMask = MouseButtonMask.Left, Relative = away }, start + away);
        await Frame();
        AssertThat(list.GetViewport().GuiIsDragging()).IsTrue();
        var ghost = list.GetChildren().OfType<Control>().Single(child => child.TopLevel);

        AssertThat(ghost.ZAsRelative).IsFalse();
        AssertThat(ghost.ZIndex).IsGreater(prompt.ZIndex);
        Send(new InputEventMouseButton { ButtonIndex = MouseButton.Left }, start);
        prompt.Free();
    }

    private static void Send(InputEventMouse mouse, Vector2 position)
    {
        mouse.Position = position;
        mouse.GlobalPosition = position;
        Input.ParseInputEvent(mouse);
        Input.FlushBufferedEvents();
    }

    private static async Task Frame() => await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

    private async Task ShowNotes()
    {
        list.Show(window, notes);
        await Frame();
    }

    private CategoryRowView Row(int index) => list.GetNode<CategoryRowView>($"Cells/List/Rows/Row{index}");

    private string RowText(int index) => Row(index).GetNode<HudLabel>("Cells/Name").Text;

    private Vector2 Middle(int index) => RowPosition(index) + new Vector2(40, Row(index).Size.Y / 2);

    private Vector2 TopOf(int index) => RowPosition(index) + new Vector2(40, 1);

    private Vector2 RowPosition(int index) => Row(index).GlobalPosition - list.GlobalPosition;
}
