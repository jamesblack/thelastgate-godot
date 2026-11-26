using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;
using TheLastGate.Notes;
using TheLastGate.Profiling;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Map;

[Tool]
public partial class CategoryListView : Control
{
    private const string RowScene = "res://Map/category_row.tscn";
    private const string DragId = "id";
    private const string DragIsCategory = "category";
    private const double AutoScrollSeconds = 0.1;
    private const int GhostOffset = 12;

    // Godot draws a drag preview straight onto the canvas, where the HUD's own z_index would cover it.
    private const int GhostLayer = 4;
    private const int TickReach = 2;

    private readonly List<CategoryRowView> pool = [];
    private string builtKey = "";
    private NoteDrag? dragged;
    private NoteDropSlot? dropSlot;
    private CategoryRowView? ghost;
    private double lastAutoScroll;
    private NoteSet? notes;
    private int renamingId;
    private PackedScene? rowScene;
    private IReadOnlyList<CategoryRow> rows = [];
    private WorldMapWindow? window;

    // For the style guide: shows sample notes that can be dragged about, in the game as in the editor.
    [Export] public bool Preview { get; set; }

    public ScrollBar ScrollBar => GetNode<ScrollBar>("Cells/Column/ScrollBar");

    private VBoxContainer Rows => GetNode<VBoxContainer>("Cells/List/Rows");

    private Control DropLine => GetNode<Control>("DropLine");

    // Measured on a row in the tree, as the HUD font that sets its height only applies there.
    private int RowHeight => pool.Count > 0 ? (int)Math.Max(1, pool[0].GetCombinedMinimumSize().Y) : 1;

    private int Scroll => (int)ScrollBar.Value;

    private static double Now => Time.GetTicksMsec() / 1000.0;

    public event Action<int>? CategoryClicked;

    public event Action<int>? NoteClicked;

    public event Action<int, Vector2>? CategoryMenuWanted;

    public event Action<int, Vector2>? NoteMenuWanted;

    public event Action<NoteDrag, NoteDropSlot, int>? ClashFound;

    public event Action? NotesChanged;

    public event Action<int, string>? CategoryRenamed;

    public override void _Ready()
    {
        ScrollBar.ValueChanged += _ => ShowRows();
        Rows.Resized += FillPool;
        if (Engine.IsEditorHint() || Preview)
        {
            ShowSample();
        }
    }

    public override void _Process(double delta)
    {
        if (window == null || notes == null || !IsVisibleInTree())
        {
            return;
        }

        var key =
            $"{notes.GetHashCode()}:{notes.Revision}:{string.Join(',', window.Editor.ExpandedCategories.Order())}";
        if (key != builtKey)
        {
            builtKey = key;
            rows = CategoryRows.Build(notes, window.Editor.ExpandedCategories);
        }

        if (dragged != null && !GetGlobalRect().HasPoint(GetGlobalMousePosition()))
        {
            ClearDrop();
        }

        ShowRows();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown,
            } wheel)
        {
            ScrollBar.Value += wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1;
            AcceptEvent();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd)
        {
            dragged = null;
            ghost = null;
            foreach (var row in pool)
            {
                row.ClearHover();
            }

            ClearDrop();
            ShowRows();
        }
    }

    public void Show(WorldMapWindow shownWindow, NoteSet? shownNotes)
    {
        window = shownWindow;
        notes = shownNotes;
        if (shownNotes == null)
        {
            rows = [];
            builtKey = "";
            ShowRows();
        }
    }

    public void StartRename(int categoryId)
    {
        var index = rows.ToList().FindIndex(row => row.IsCategory && row.Id == categoryId);
        if (index < 0 || notes?.FindCategory(categoryId) is not { } category)
        {
            return;
        }

        if (index < Scroll || index >= Scroll + pool.Count)
        {
            ScrollBar.Value = index;
        }

        ShowRows();
        renamingId = categoryId;
        pool[index - Scroll].BeginRename(category.Name);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (notes == null || window is not { Editor.CanDrag: true } || RowUnder(atPosition) is not { } under
            || under.Index >= rows.Count)
        {
            return default;
        }

        var row = rows[under.Index];
        dragged = new NoteDrag(row.Id, row.IsCategory);
        if (GetViewport().GuiIsDragging())
        {
            SetDragPreview(Ghost(row));
        }

        return new Dictionary { { DragId, row.Id }, { DragIsCategory, row.IsCategory } };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (notes == null || window == null || DragOf(data) is not { } drag)
        {
            return false;
        }

        dragged = drag;
        AutoScroll(atPosition);
        var slot = SlotAt(atPosition, drag);
        var allowed = slot is { } found && window.Editor.CheckDrop(notes, drag, found) == NoteMoveRefusal.None;
        dropSlot = allowed ? slot : null;
        ShowDropLine(atPosition, allowed ? slot : null);
        ShowGhost(drag, allowed);

        ShowRows();
        return allowed;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (notes == null || window == null || DragOf(data) is not { } drag || SlotAt(atPosition, drag) is not { } slot)
        {
            return;
        }

        var result = window.Editor.Drop(notes, drag, slot);
        switch (result.Outcome)
        {
            case NoteDropOutcome.Moved:
                NotesChanged?.Invoke();
                break;
            case NoteDropOutcome.Clash:
                ClashFound?.Invoke(drag, slot, result.ClashId);
                break;
            case NoteDropOutcome.Refused:
                break;
            default:
                throw new UnreachableException();
        }

        dragged = null;
        ClearDrop();
    }

    private static NoteDrag? DragOf(Variant data) =>
        data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary() is { } fields
        && fields.ContainsKey(DragId)
            ? new NoteDrag(fields[DragId].AsInt32(), fields[DragIsCategory].AsBool())
            : null;

    private CategoryRowView NewRow()
    {
        rowScene ??= GD.Load<PackedScene>(RowScene);
        return rowScene.Instantiate<CategoryRowView>();
    }

    private void FillPool()
    {
        if (pool.Count == 0)
        {
            AddPooledRow();
        }

        var wanted = Math.Max(1, (int)(Rows.Size.Y / RowHeight));
        while (pool.Count < wanted)
        {
            AddPooledRow();
        }

        while (pool.Count > wanted)
        {
            var last = pool[^1];
            pool.RemoveAt(pool.Count - 1);
            Rows.RemoveChild(last);
            last.QueueFree();
        }

        ShowRows();
    }

    private void AddPooledRow()
    {
        var row = NewRow();
        var slot = pool.Count;
        row.Name = $"Row{slot}";
        row.Pressed += (button, at) => PressRow(slot, button, at);
        row.RenameFinished += FinishRename;
        row.RenameCancelled += () => renamingId = 0;
        Rows.AddChild(row);
        pool.Add(row);
    }

    private void ShowRows()
    {
        using var timing = Timings.Measure("maplist.rows");
        if (notes == null || window == null)
        {
            foreach (var row in pool)
            {
                row.Visible = false;
            }

            return;
        }

        var page = pool.Count;
        ScrollBar.MaxValue = rows.Count;
        ScrollBar.Page = page;
        var highest = Math.Max(0, rows.Count - page);
        if (ScrollBar.Value > highest)
        {
            ScrollBar.SetValueNoSignal(highest);
        }

        for (var slot = 0; slot < pool.Count; slot++)
        {
            var index = Scroll + slot;
            var row = pool[slot];
            row.Visible = index < rows.Count;
            row.Hoverable = dragged == null;
            if (row.Visible)
            {
                var shown = rows[index];
                row.Show(shown, notes, StateOf(shown), window.Editor.ExpandedCategories.Contains(shown.Id));
            }
        }
    }

    private CategoryRowState StateOf(CategoryRow row)
    {
        var editor = window!.Editor;
        if (dragged is { } drag && drag.Id == row.Id && drag.IsCategory == row.IsCategory)
        {
            return CategoryRowState.Source;
        }

        if (row.IsCategory && dropSlot is { Onto: true } slot && slot.ParentId == row.Id)
        {
            return CategoryRowState.Target;
        }

        if (row.IsCategory ? editor.CurrentCategoryId == row.Id : editor.SelectedNoteId == row.Id)
        {
            return row.IsCategory ? CategoryRowState.Current : CategoryRowState.Selected;
        }

        var category = row.IsCategory ? notes!.FindCategory(row.Id) : notes!.OwnerOf(row.Id);
        return category != null && !notes.IsShown(category) ? CategoryRowState.Dimmed : CategoryRowState.Normal;
    }

    private void PressRow(int slot, MapButton button, Vector2 at)
    {
        var index = Scroll + slot;
        if (window == null || index >= rows.Count)
        {
            return;
        }

        var row = rows[index];
        var mouse = GetViewport().GetMousePosition();
        switch (button)
        {
            case MapButton.Left when row.IsCategory && pool[slot].OnChevron(at):
                window.Editor.Expand(row.Id, !window.Editor.ExpandedCategories.Contains(row.Id));
                break;
            case MapButton.Left when row.IsCategory:
                CategoryClicked?.Invoke(row.Id);
                break;
            case MapButton.Left:
                NoteClicked?.Invoke(row.Id);
                break;
            case MapButton.Right when row.IsCategory:
                CategoryMenuWanted?.Invoke(row.Id, mouse);
                break;
            case MapButton.Right:
                NoteMenuWanted?.Invoke(row.Id, mouse);
                break;
            case MapButton.None:
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void FinishRename(string name)
    {
        var id = renamingId;
        renamingId = 0;
        if (id != 0)
        {
            CategoryRenamed?.Invoke(id, name);
        }
    }

    // The row under a point, counted from the first row shown, and how far down that row it is; below the last row
    // is the space after the list.
    private (int Index, float FractionDown)? RowUnder(Vector2 atPosition)
    {
        var inRows = atPosition - (Rows.GlobalPosition - GlobalPosition);
        if (inRows.Y < 0 || inRows.X < 0 || inRows.X > Rows.Size.X)
        {
            return null;
        }

        var height = RowHeight;
        var slot = (int)(inRows.Y / height);
        var index = Math.Min(Scroll + slot, rows.Count);
        var fraction = index == rows.Count ? 0f : inRows.Y / height - slot;
        return (index, fraction);
    }

    private NoteDropSlot? SlotAt(Vector2 atPosition, NoteDrag drag)
    {
        if (RowUnder(atPosition) is not { } under)
        {
            return null;
        }

        var inRows = atPosition - (Rows.GlobalPosition - GlobalPosition);
        var cursorLevel = (int)Math.Round(inRows.X / CategoryRowView.IndentPerLevel);
        return CategoryRows.Slot(rows, drag, under.Index, under.FractionDown, cursorLevel);
    }

    private void ShowDropLine(Vector2 atPosition, NoteDropSlot? slot)
    {
        DropLine.Visible = slot is { Onto: false };
        if (slot is not { Onto: false } line || RowUnder(atPosition) is not { } under)
        {
            return;
        }

        var lineAt = under.Index < rows.Count && under.FractionDown > 0.5f ? under.Index + 1 : under.Index;
        var depth = line.ParentId == 0
            ? 0
            : rows.First(row => row.IsCategory && row.Id == line.ParentId).Depth + 1;
        var left = depth * CategoryRowView.IndentPerLevel + 1;
        var origin = Rows.GlobalPosition - GlobalPosition;
        DropLine.Position = origin + new Vector2(0, (lineAt - Scroll) * RowHeight);
        DropLine.GetNode<Line2D>("Line").Points = [new Vector2(left, 0.5f), new Vector2(Rows.Size.X - 1, 0.5f)];
        DropLine.GetNode<Line2D>("Tick").Points =
            [new Vector2(left + 0.5f, -TickReach), new Vector2(left + 0.5f, TickReach + 1)];
    }

    private void ClearDrop()
    {
        dropSlot = null;
        DropLine.Visible = false;
    }

    private Control Ghost(CategoryRow row)
    {
        ghost = NewRow();
        ghost.Show(row, notes!, CategoryRowState.Ghost, false);
        ghost.CustomMinimumSize = new Vector2(Rows.Size.X, 0);
        ghost.Position = new Vector2(GhostOffset, -RowHeight / 2f);
        var holder = new Control { MouseFilter = MouseFilterEnum.Ignore, ZIndex = GhostLayer, ZAsRelative = false };
        holder.AddChild(ghost);
        return holder;
    }

    private void ShowGhost(NoteDrag drag, bool allowed)
    {
        var row = rows.FirstOrDefault(candidate => candidate.Id == drag.Id && candidate.IsCategory == drag.IsCategory);
        if (ghost != null && row != default)
        {
            ghost.Show(row, notes!, allowed ? CategoryRowState.Ghost : CategoryRowState.RefusedGhost, false);
        }
    }

    private void AutoScroll(Vector2 atPosition)
    {
        var inRows = atPosition - (Rows.GlobalPosition - GlobalPosition);
        var edge = inRows.Y < RowHeight ? -1 : inRows.Y > Rows.Size.Y - RowHeight ? 1 : 0;
        if (edge == 0 || Now - lastAutoScroll < AutoScrollSeconds)
        {
            return;
        }

        lastAutoScroll = Now;
        ScrollBar.Value += edge;
    }

    private void ShowSample()
    {
        var sample = new NoteSet();
        var places = sample.AddCategory("Places", 1);
        sample.AddCategory("Dungeons", 2, places);
        var shops = sample.AddCategory("Shops", 3, places);
        sample.AddPoint(shops, NoteKind.Pin, 1, 1, "Armourer");
        sample.AddPath(shops, [new PathSegment(1, 1, 2, 2)], "To the gate");
        sample.AddPoint(places, NoteKind.Pin, 2, 2, "Tavern");
        sample.AddCategory("Quests", 4);
        sample.AddCategory("Shared", 7);
        var preview = new WorldMapWindow();
        preview.Editor.Expand(places, true);
        preview.Editor.Expand(shops, true);
        preview.Editor.ChooseCategory(places);
        Show(preview, sample);
        rows = CategoryRows.Build(sample, preview.Editor.ExpandedCategories);
        FillPool();
    }
}
