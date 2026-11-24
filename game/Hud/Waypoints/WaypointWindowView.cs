using System;
using Godot;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Waypoints;

// The scene is saved visible so it can be laid out in the editor; the game hides it until the window opens.
[Tool]
public partial class WaypointWindowView : Control
{
    private const int FoundPicture = 4500;
    private const int UnknownPicture = 4533;
    private const int LastRow = WaypointWindow.VisibleRows - 1;
    private const int PreviewSelectedRow = 2;
    private const float SelectedPictureBrightness = 2f;
    private const string UnknownName = "Unknown Location";
    private const string UnknownDescription = "\"???\"";

    private static readonly string[] Cells = ["Cells/PictureCell", "Cells/TextCell"];

    private PlayerState player = new();
    private bool wasOpen;
    private WaypointWindow? window;

    public LineEdit Search => GetNode<LineEdit>("%Search");

    public ScrollBar ScrollBar => GetNode<ScrollBar>("%ScrollBar");

    public ClickArea CloseButton => GetNode<ClickArea>("%CloseButton");

    public Control WheelArea => GetNode<Control>("%Rows");

    public event Action? SelectedChosen;

    public event Action? CloseAsked;

    public ClickArea Row(int row) => GetNode<ClickArea>($"%Rows/Row{row}");

    public override void _Ready()
    {
        foreach (var cell in Cells)
        {
            RowNode(LastRow).GetNode<ProceduralPanel>(cell).Edges = ProceduralEdges.Right;
        }

        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        Visible = false;
        Search.TextChanged += SearchFor;
        Search.TextSubmitted += _ => SelectedChosen?.Invoke();
        Search.GuiInput += SearchKey;
    }

    public void ShowWaypoints(WaypointWindow shownWindow, PlayerState shownPlayer)
    {
        window = shownWindow;
        player = shownPlayer;
        Visible = window.IsOpen;
        FollowOpening(window.IsOpen);
        if (!Visible)
        {
            return;
        }

        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            if (!window.HasRow(row))
            {
                ShowRow(row, null, false, false);
                continue;
            }

            var waypoint = window.At(row);
            ShowRow(row, waypoint, State.Waypoints.Found(player.Waypoints, waypoint.Number), window.IsSelected(row));
        }

        ShowScroll(window);
    }

    public void ShowRow(int row, Waypoint? waypoint, bool found, bool selected)
    {
        var rowNode = RowNode(row);
        var shown = waypoint is not null;
        rowNode.MouseFilter = shown ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (!shown && rowNode is ClickArea area)
        {
            area.ClearHover();
        }

        foreach (var cell in Cells)
        {
            rowNode.GetNode<ProceduralPanel>(cell).Fill = selected ? ProceduralFill.Selected : ProceduralFill.Red;
        }

        var picture = rowNode.GetNode<ArchiveSprite>("Cells/PictureCell/Picture");
        var name = rowNode.GetNode<Label>("Cells/TextCell/Text/Name");
        var description = rowNode.GetNode<Label>("Cells/TextCell/Text/Description");
        picture.Visible = shown;
        picture.Modulate = selected
            ? new Color(SelectedPictureBrightness, SelectedPictureBrightness, SelectedPictureBrightness)
            : Colors.White;
        name.Visible = shown;
        description.Visible = shown;
        if (waypoint is not { } place)
        {
            return;
        }

        picture.SpriteNumber = (found ? FoundPicture : UnknownPicture) + place.Number;
        name.Text = found ? place.Name : UnknownName;
        description.Text = found ? place.Description : UnknownDescription;
    }

    // ClickArea is not a tool script, so in the editor the rows are plain Controls.
    private Control RowNode(int row) => GetNode<Control>($"%Rows/Row{row}");

    // The rows' contents belong to the row scene, so the editor would not save them anyway.
    private void PreviewInEditor()
    {
        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            ShowRow(row, State.Waypoints.All[row], true, row == PreviewSelectedRow);
        }
    }

    // Zeroed first so a shorter list's maximum never clamps the value and sends a scroll of its own.
    private void ShowScroll(WaypointWindow window)
    {
        ScrollBar.SetValueNoSignal(0);
        ScrollBar.MaxValue = window.ShownCount;
        ScrollBar.SetValueNoSignal(window.Scroll);
    }

    private void Redraw()
    {
        if (window != null)
        {
            ShowWaypoints(window, player);
        }
    }

    private void FollowOpening(bool isOpen)
    {
        if (isOpen == wasOpen)
        {
            return;
        }

        wasOpen = isOpen;
        if (isOpen)
        {
            Search.Clear();
            window?.ClearSearch(player.Waypoints);
            Search.GrabFocus();
            Search.Edit();
        }
        else if (Search.HasFocus())
        {
            GetViewport().GuiReleaseFocus();
        }
    }

    private void SearchFor(string search)
    {
        window?.SetSearch(search, player.Waypoints);
        Redraw();
    }

    private void ClearAndType()
    {
        Search.Clear();
        window?.ClearSearch(player.Waypoints);
        TypeInSearch();
        Redraw();
    }

    private void TypeInSearch()
    {
        Search.GrabFocus();
        Search.Edit();
    }

    private void MoveSelection(int rows)
    {
        window?.MoveSelection(rows);
        Redraw();
    }

    private void ClearSelection()
    {
        window?.ClearSelection();
        Redraw();
    }

    // Keycodes, not ui_up and ui_down: NavigationKeys takes those off on the game screen. Escape clears a search
    // first, and closes the window once the field is empty.
    private void SearchKey(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        if (key.Keycode == Key.Down)
        {
            MoveSelection(1);
        }
        else if (key.Keycode == Key.Up && window?.Selected == 0)
        {
            ClearSelection();
        }
        else if (key.Keycode == Key.Up && window?.Selected != null)
        {
            MoveSelection(-1);
        }
        else if (key.Keycode == Key.Escape && Search.Text.Length > 0)
        {
            ClearAndType();
        }
        else if (key.Keycode == Key.Escape)
        {
            CloseAsked?.Invoke();
        }
        else
        {
            return;
        }

        AcceptEvent();
    }
}
