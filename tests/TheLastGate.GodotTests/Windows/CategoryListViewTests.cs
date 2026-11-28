using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
[RequireGodotRuntime]
public class CategoryListViewTests
{
    private const int FirstId = 1;
    private const int SecondId = 2;
    private const int ItemId = 10;

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
        list = GD.Load<PackedScene>("res://Windows/category_list.tscn").Instantiate<CategoryListView>();
        Tree.Root.AddChild(list);
        list.Size = ListSize;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheList() => list.Free();

    [TestCase]
    public async Task ShowsTheSourcesRowsAndLooks()
    {
        await ShowFake(SampleSource());

        AssertThat(RowText(0)).IsEqual("First");
        AssertThat(RowText(1)).IsEqual("Item");
        AssertThat(RowText(2)).IsEqual("Second");
        AssertThat(Row(3).Visible).IsFalse();
    }

    [TestCase]
    public async Task NullSwatch_HidesTheSwatch()
    {
        await ShowFake(SampleSource());

        AssertThat(Swatch(0).Visible).IsTrue();
        AssertThat(Swatch(1).Visible).IsFalse();
    }

    [TestCase]
    public async Task Drop_RaisesChangedOnlyWhenTheSourceMoved()
    {
        var source = SampleSource();
        var changes = 0;
        list.Changed += () => changes++;
        await ShowFake(source);

        list._DropData(TopOf(0), list._GetDragData(Middle(2)));
        AssertThat(changes).IsEqual(0);

        source.Moves = true;
        list._DropData(TopOf(0), list._GetDragData(Middle(2)));
        AssertThat(changes).IsEqual(1);
        AssertThat(source.Drops).IsEqual(2);
    }

    [TestCase]
    public async Task Rename_ReportsIdKindAndName()
    {
        var renamed = new List<(int Id, bool IsCategory, string Name)>();
        list.RowRenamed += (id, isCategory, name) => renamed.Add((id, isCategory, name));
        await ShowFake(SampleSource());

        list.StartRename(ItemId, false, "Old");
        var field = Row(1).GetNode<LineEdit>("Cells/Field");
        AssertThat(field.Text).IsEqual("Old");
        field.Text = "New";
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = true });
        Input.FlushBufferedEvents();
        await Frame();

        AssertThat(renamed).ContainsExactly((ItemId, false, "New"));
    }

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
        list.Show(null);
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
    public async Task ARowsHoverText_ShowsOnlyWhileHovered()
    {
        var row = GD.Load<PackedScene>("res://Windows/category_row.tscn").Instantiate<CategoryRowView>();
        Tree.Root.AddChild(row);
        row.Show(
            new CategoryRow(ItemId, false, 1, FirstId, 0),
            new CategoryRowLook("Potions", null, CategoryRowState.Normal, "Altchar"),
            false);
        var hoverText = row.GetNode<HudLabel>("Cells/HoverText");
        AssertThat(hoverText.Visible).IsFalse();

        row.GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);
        await Frame();

        AssertThat(hoverText.Visible).IsTrue();
        AssertThat(hoverText.Text).IsEqual("Altchar");
        row.Free();
    }

    [TestCase]
    public async Task WhenARowsHeightChanges_TheListRefitsItsRows()
    {
        await ShowNotes();
        var rows = list.GetNode("Cells/List/Rows");
        var fitted = rows.GetChildCount();

        Row(0).CustomMinimumSize = new Vector2(0, ListSize.Y / 2);
        await Frame();

        AssertThat(rows.GetChildCount()).IsLess(fitted);
    }

    [TestCase]
    public async Task AGhost_HasNoIndentAndShowsItsWholeName()
    {
        var places = notes.AddCategory("Places", 0);
        var deep = notes.AddCategory("A rather long name", 0, places);
        var ghost = GD.Load<PackedScene>("res://Windows/category_row.tscn").Instantiate<CategoryRowView>();
        Tree.Root.AddChild(ghost);

        var row = new CategoryRow(deep, true, 1, places, 0);
        ghost.Show(
            row,
            new NoteListSource(notes, window.Editor).Look(row) with { State = CategoryRowState.Ghost },
            false);

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
        var source = new NoteListSource(notes, window.Editor);
        source.ClashFound += (_, _, clashId) => clashes.Add(clashId);
        list.Show(source);
        await Frame();
    }

    private async Task ShowFake(FakeSource source)
    {
        list.Show(source);
        await Frame();
    }

    private CategoryRowView Row(int index) => list.GetNode<CategoryRowView>($"Cells/List/Rows/Row{index}");

    private string RowText(int index) => Row(index).GetNode<HudLabel>("Cells/Name").Text;

    private ColorRect Swatch(int index) => Row(index).GetNode<ColorRect>("Cells/Swatch/Colour");

    private static FakeSource SampleSource() =>
        new(
            [
                new CategoryRow(FirstId, true, 0, 0, 0),
                new CategoryRow(ItemId, false, 1, FirstId, 0),
                new CategoryRow(SecondId, true, 0, 0, 1),
            ],
            new Dictionary<int, CategoryRowLook>
            {
                [FirstId] = new("First", Colors.Red, CategoryRowState.Normal),
                [ItemId] = new("Item", null, CategoryRowState.Normal),
                [SecondId] = new("Second", Colors.Blue, CategoryRowState.Current),
            });

    private Vector2 Middle(int index) => RowPosition(index) + new Vector2(40, Row(index).Size.Y / 2);

    private Vector2 TopOf(int index) => RowPosition(index) + new Vector2(40, 1);

    private Vector2 RowPosition(int index) => Row(index).GlobalPosition - list.GlobalPosition;
}

internal sealed class FakeSource(IReadOnlyList<CategoryRow> rows, IReadOnlyDictionary<int, CategoryRowLook> looks)
    : ICategoryListSource
{
    public bool Moves { get; set; }

    public int Drops { get; private set; }

    public int Revision => 0;

    public IReadOnlyCollection<int> Expanded { get; } = [];

    public bool CanDrag => true;

    public IReadOnlyList<CategoryRow> BuildRows() => rows;

    public CategoryRowLook Look(CategoryRow row) => looks[row.Id];

    public void Expand(int categoryId, bool expanded)
    {
    }

    public bool CanDrop(CategoryDrag drag, CategoryDropSlot slot) => true;

    public bool Drop(CategoryDrag drag, CategoryDropSlot slot)
    {
        Drops++;
        return Moves;
    }
}
