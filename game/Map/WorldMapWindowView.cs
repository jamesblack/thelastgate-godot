using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Notes;
using TheLastGate.Windows;

namespace TheLastGate.Map;

// The current client's World Map window, drawn with the game's window and button sprites: a title bar to drag it by,
// the category column, the map with the player's notes on it, and a bar of tools, Follow
// player, the edited note's name and the tile under the cursor. Its own clicks change its own state; the HUD only
// opens it and tells it what to show. Every change to the notes is handed straight back to be saved.
[Tool]
public partial class WorldMapWindowView : Panel, IHudContextUser
{
    private const int FrameSprite = 18128;
    private const float TitleBottom = 30;
    private const int SwatchSize = 20;
    private const int CategoryColourItem = NotePalette.Size;

    private static readonly Vector2 SmallestSize = new(480, 320);
    private int deleteNoteId;
    private int editedNameShownFor;
    private NoteHit menuHit;
    private NoteSet? notes;
    private NoteLabelPurpose shownPurpose;

    private Vector2 titleGrab;
    private bool titleHeld;
    private WorldMapWindow? window;
    public WorldMapWindowView() => AddToGroup(HudContext.Group);

    private WorldMapCanvas Canvas => GetNode<WorldMapCanvas>("Rows/Body/MapFrame/Canvas");

    private Control Help => GetNode<Control>("HelpBlocker");

    private CheckBox ShowOnMinimapBox => GetNode<CheckBox>("Rows/Body/Categories/Footer/ShowOnMinimap");

    private WorldMapCategories CategoryColumn => GetNode<WorldMapCategories>("Rows/Body/Categories");

    private WorldMapEditor? Editor => window?.Editor;

    private Control LabelPrompt => GetNode<Control>("LabelBlocker");

    private LineEdit LabelName => GetNode<LineEdit>("LabelBlocker/Prompt/Margins/Rows/Name");

    private Control DeletePrompt => GetNode<Control>("DeleteBlocker");

    private LineEdit EditName => GetNode<LineEdit>("Rows/BottomBar/EditName");

    private PopupMenu NoteMenu => GetNode<PopupMenu>("NoteMenu");

    private PopupMenu ColourMenu => GetNode<PopupMenu>("NoteMenu/ColourMenu");

    private PopupMenu SendToMenu => GetNode<PopupMenu>("NoteMenu/SendToMenu");

    private static double Now => Time.GetTicksMsec() / 1000.0;

    // The window and its inner panels are framed by the game's window sprite, which lives in the sprite archives.
    public void ShowContext(HudContext context)
    {
        if (context.Sprites?.GetTexture(FrameSprite) is not { } texture)
        {
            return;
        }

        foreach (var frame in FindChildren("Frame", nameof(NinePatchRect), true, false).Cast<NinePatchRect>())
        {
            frame.Texture = texture;
        }
    }

    public event Action? NotesChanged;

    public override void _Ready()
    {
        AddHelpLines();
        if (Engine.IsEditorHint())
        {
            return;
        }

        var close = GetNode<Button>("Rows/TitleBar/Close");
        var cross = close.GetNode<PixelCross>("Cross");
        close.MouseEntered += () => cross.Hovered = true;
        close.MouseExited += () => cross.Hovered = false;
        close.Pressed += () =>
        {
            window?.Close();
            Visible = false;
        };
        GetNode<Button>("Rows/TitleBar/Help").Pressed += OpenHelp;
        GetNode<Control>("Rows/TitleBar").GuiInput += DragByTitle;
        GetNode<ResizeGrip>("ResizeGrip").Dragged += Resize;
        GetNode<CheckBox>("Rows/BottomBar/Follow").Toggled += isOn =>
        {
            if (window != null)
            {
                window.Follow = isOn;
            }
        };
        ShowOnMinimapBox.Toggled += ShowOnMinimap;
        foreach (var tool in Enum.GetValues<WorldMapTool>())
        {
            ToolButton(tool).Pressed += () => Editor?.ChooseTool(tool);
        }

        Canvas.NoteMenuWanted += OpenNoteMenu;
        Canvas.NotesEdited += SaveChanges;
        CategoryColumn.NoteMenuWanted += OpenNoteMenu;
        CategoryColumn.NotesChanged += () => NotesChanged?.Invoke();
        ConnectPrompts();
        ConnectEditControls();
        PrepareMenus();

        GetNode<Button>("HelpBlocker/Help/Margins/Rows/Buttons/Back").Pressed += () => ChangeHelpPage(-1);
        GetNode<Button>("HelpBlocker/Help/Margins/Rows/Buttons/Next").Pressed += () => ChangeHelpPage(1);
        GetNode<Button>("HelpBlocker/Help/Margins/Rows/Buttons/Close").Pressed += () => Help.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() || window == null || !Visible)
        {
            return;
        }

        GetNode<Label>("Rows/BottomBar/Status").Text = window.StatusAt(Now);
        GetNode<Label>("Rows/BottomBar/Coordinates").Text =
            Canvas.HoveredTile is { } tile ? $"{tile.X}, {tile.Y}" : "";
        GetNode<CheckBox>("Rows/BottomBar/Follow").SetPressedNoSignal(window.Follow);
        ShowOnMinimapBox.SetPressedNoSignal(notes?.ShowOnMinimap == true);
        ShowTools(window.Editor);
        ShowEditControls(window.Editor);
        ShowLabelPrompt(window.Editor);
    }

    // A click anywhere else takes the map's focus away, so Enter goes back to the chat. While a prompt is up, Escape
    // cancels it rather than closing the map.
    public override void _Input(InputEvent inputEvent)
    {
        if (Engine.IsEditorHint() || window == null || !Visible)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true } press:
                window.Editor.Focused = GetGlobalRect().HasPoint(press.Position) || LabelPrompt.Visible
                    || DeletePrompt.Visible || CategoryColumn.PromptOpen;
                break;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
                when LabelPrompt.Visible || DeletePrompt.Visible:
                if (LabelPrompt.Visible)
                {
                    window.Editor.CancelLabel();
                }

                DeletePrompt.Visible = false;
                GetViewport().SetInputAsHandled();
                break;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.KpEnter }
                when !LabelPrompt.Visible && !CategoryColumn.PromptOpen && window.Editor.FinishPendingPath():
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    // The window, the map it draws, the notes and the player's tile come from the HUD on every refresh.
    public void ShowWindow(
        WorldMapWindow shownWindow,
        ExploredMap? map,
        NoteSet? shownNotes,
        (int X, int Y) playerTile)
    {
        if (window != shownWindow)
        {
            if (window != null)
            {
                window.Editor.Problem -= ShowProblem;
            }

            shownWindow.Editor.Problem += ShowProblem;
        }

        window = shownWindow;
        notes = shownNotes;
        if (shownNotes != null)
        {
            shownWindow.Editor.EnsureCurrentCategory(shownNotes);
        }

        ShowOnMinimapBox.Disabled = shownNotes == null;
        Canvas.Notes = shownNotes;
        Visible = shownWindow.IsOpen;
        Canvas.Window = shownWindow;
        Canvas.Map = map;
        Canvas.PlayerTile = playerTile;
        CategoryColumn.Show(shownWindow, shownNotes);
        ShowTools(shownWindow.Editor);
        if (!Visible)
        {
            NoteMenu.Hide();
            DeletePrompt.Visible = false;
            CategoryColumn.CloseAll();
        }
    }

    private void ShowOnMinimap(bool isOn)
    {
        if (notes == null)
        {
            return;
        }

        notes.ShowOnMinimap = isOn;
        notes.Changed = true;
        NotesChanged?.Invoke();
    }

    private Button ToolButton(WorldMapTool tool) => GetNode<Button>($"Rows/BottomBar/{tool}");

    private void SaveChanges()
    {
        if (notes?.Changed == true)
        {
            NotesChanged?.Invoke();
        }
    }

    private void ShowProblem(string problem) => window?.ShowStatus(problem, Now);

    private void ShowTools(WorldMapEditor editor)
    {
        foreach (var tool in Enum.GetValues<WorldMapTool>())
        {
            var button = ToolButton(tool);
            button.Disabled = !WorldMapWindow.IsAvailable(tool, editor.CurrentCategoryId != 0);
            button.SetPressedNoSignal(editor.Tool == tool);
        }
    }

    private void ConnectEditControls()
    {
        EditName.TextChanged += label => Editor?.RenameEdited(label);
        EditName.TextSubmitted += _ => SaveEdit();
        GetNode<Button>("Rows/BottomBar/EditSave").Pressed += SaveEdit;
        GetNode<Button>("Rows/BottomBar/EditCancel").Pressed += () => Editor?.CancelEdit();
    }

    // The name box is filled once when an edit starts, and then belongs to the player.
    private void ShowEditControls(WorldMapEditor editor)
    {
        var edited = editor.Edited;
        var editing = edited != null;
        EditName.Visible = editing;
        GetNode<Button>("Rows/BottomBar/EditSave").Visible = editing;
        GetNode<Button>("Rows/BottomBar/EditCancel").Visible = editing;
        GetNode<Button>("Rows/BottomBar/EditSave").Disabled = !editor.CanSaveEdit;
        if (edited != null && edited.Id != editedNameShownFor)
        {
            EditName.Text = edited.Label;
        }

        if (!editing && EditName.HasFocus())
        {
            EditName.ReleaseFocus();
        }

        editedNameShownFor = edited?.Id ?? 0;
    }

    private void SaveEdit()
    {
        if (notes == null || Editor is not { CanSaveEdit: true } editor)
        {
            return;
        }

        editor.SaveEdit(notes);
        SaveChanges();
    }

    private void ConnectPrompts()
    {
        LabelName.TextChanged += _ => ShowLabelConfirm();
        LabelName.TextSubmitted += _ => ConfirmLabel();
        GetNode<Button>("LabelBlocker/Prompt/Margins/Rows/Buttons/Confirm").Pressed += ConfirmLabel;
        GetNode<Button>("LabelBlocker/Prompt/Margins/Rows/Buttons/Cancel").Pressed += () => Editor?.CancelLabel();
        GetNode<Button>("DeleteBlocker/Prompt/Margins/Rows/Buttons/Confirm").Pressed += ConfirmDelete;
        GetNode<Button>("DeleteBlocker/Prompt/Margins/Rows/Buttons/Cancel").Pressed +=
            () => DeletePrompt.Visible = false;
    }

    // The name prompt opens empty, ready for typing, whenever the editor asks for a name.
    private void ShowLabelPrompt(WorldMapEditor editor)
    {
        if (editor.LabelPurpose == shownPurpose)
        {
            return;
        }

        shownPurpose = editor.LabelPurpose;
        LabelPrompt.Visible = shownPurpose != NoteLabelPurpose.None;
        if (LabelPrompt.Visible)
        {
            LabelName.Text = "";
            LabelName.GrabFocus();
            ShowLabelConfirm();
        }
        else
        {
            LabelName.ReleaseFocus();
        }
    }

    private void ShowLabelConfirm() =>
        GetNode<Button>("LabelBlocker/Prompt/Margins/Rows/Buttons/Confirm").Disabled =
            Editor?.CanConfirmLabel(LabelName.Text) != true;

    private void ConfirmLabel()
    {
        if (notes == null || Editor is not { } editor || !editor.CanConfirmLabel(LabelName.Text))
        {
            return;
        }

        editor.ConfirmLabel(notes, LabelName.Text);
        SaveChanges();
    }

    private void ConfirmDelete()
    {
        DeletePrompt.Visible = false;
        if (notes != null)
        {
            Editor?.Delete(notes, deleteNoteId);
            SaveChanges();
        }
    }

    private void AskToDelete(Note note)
    {
        deleteNoteId = note.Id;
        GetNode<Label>("DeleteBlocker/Prompt/Margins/Rows/Question").Text = WorldMapWording.DeleteNoteQuestion(note);
        DeletePrompt.Visible = true;
    }

    // The menus are windows of their own, so they are given the window's font; the colour menu is the palette's
    // swatches and the category's own colour.
    private void PrepareMenus()
    {
        foreach (var menu in (PopupMenu[])[NoteMenu, ColourMenu, SendToMenu])
        {
            menu.AddThemeFontOverride("font", GetThemeFont("font", "Label"));
            menu.AddThemeFontSizeOverride("font_size", GetThemeFontSize("font_size", "Label"));
        }

        for (var colourIndex = 0; colourIndex < NotePalette.Size; colourIndex++)
        {
            ColourMenu.AddIconRadioCheckItem(
                NoteSwatches.Of(NotePalette.Rgb(colourIndex), SwatchSize),
                "",
                colourIndex);
        }

        ColourMenu.AddRadioCheckItem("Use category colour", CategoryColourItem);
        NoteMenu.IdPressed += ChooseFromNoteMenu;
        ColourMenu.IdPressed += id => Recolour(id == CategoryColourItem ? Note.CategoryColour : (int)id);
        SendToMenu.IdPressed += id => SendTo((int)id);
    }

    private void OpenNoteMenu(NoteHit hit, Vector2 position)
    {
        if (notes?.FindNote(hit.NoteId) is not { } note || notes.OwnerOf(hit.NoteId) is not { } owner)
        {
            return;
        }

        menuHit = hit;
        NoteMenu.Clear();
        NoteMenu.AddItem("Edit", (int)NoteMenuItem.Edit);
        NoteMenu.AddSubmenuNodeItem("Colour", ColourMenu);
        NoteMenu.AddSubmenuNodeItem("Send to", SendToMenu);
        NoteMenu.AddSeparator();
        var editingThis = Editor?.Edited?.Id == note.Id;
        if (note is { Kind: NoteKind.Path, Segments.Count: > 1 } && hit.SegmentIndex >= 0 && !editingThis)
        {
            NoteMenu.AddItem("Delete segment", (int)NoteMenuItem.DeleteSegment);
        }

        NoteMenu.AddItem("Delete...", (int)NoteMenuItem.Delete);
        var chosenColour = note.ColourIndex == Note.CategoryColour ? CategoryColourItem : note.ColourIndex;
        for (var itemIndex = 0; itemIndex < ColourMenu.ItemCount; itemIndex++)
        {
            ColourMenu.SetItemChecked(itemIndex, ColourMenu.GetItemId(itemIndex) == chosenColour);
        }

        SendToMenu.Clear();
        foreach (var category in notes.Tree())
        {
            SendToMenu.AddItem(category.Name, category.Id);
            var itemIndex = SendToMenu.ItemCount - 1;
            SendToMenu.SetItemIndent(itemIndex, notes.Depth(category.Id) - 1);
            SendToMenu.SetItemDisabled(itemIndex, category.Id == owner.Id);
        }

        NoteMenu.Popup(new Rect2I((Vector2I)position, Vector2I.Zero));
    }

    private void ChooseFromNoteMenu(long id)
    {
        if (notes == null || Editor is not { } editor || notes.FindNote(menuHit.NoteId) is not { } note)
        {
            return;
        }

        switch ((NoteMenuItem)id)
        {
            case NoteMenuItem.Edit:
                editor.BeginEdit(notes, note.Id);
                break;
            case NoteMenuItem.DeleteSegment:
                editor.DeleteSegment(notes, note.Id, menuHit.SegmentIndex);
                SaveChanges();
                break;
            case NoteMenuItem.Delete:
                AskToDelete(note);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void Recolour(int colourIndex)
    {
        if (notes != null)
        {
            Editor?.Recolour(notes, menuHit.NoteId, colourIndex);
            SaveChanges();
        }
    }

    private void SendTo(int categoryId)
    {
        if (notes != null)
        {
            Editor?.SendTo(notes, menuHit.NoteId, categoryId);
            SaveChanges();
        }
    }

    // The whole title bar stays on screen, so the window can always be grabbed again.
    private void DragByTitle(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                titleHeld = click.Pressed;
                titleGrab = GetGlobalMousePosition() - Position;
                break;
            case InputEventMouseMotion when titleHeld:
                var screen = GetViewportRect().Size;
                var wanted = GetGlobalMousePosition() - titleGrab;
                Position = new Vector2(
                    Mathf.Clamp(wanted.X, 0, Mathf.Max(screen.X - Size.X, 0)),
                    Mathf.Clamp(wanted.Y, 0, Mathf.Max(screen.Y - TitleBottom, 0)));
                break;
        }
    }

    private void Resize(Vector2 change) => Size = (Size + change).Max(SmallestSize);

    private void OpenHelp()
    {
        window?.OpenHelp();
        ShowHelpPage();
        Help.Visible = true;
    }

    private void ChangeHelpPage(int step)
    {
        if (step > 0)
        {
            window?.NextHelpPage();
        }
        else
        {
            window?.PreviousHelpPage();
        }

        ShowHelpPage();
    }

    private void ShowHelpPage()
    {
        var pageNumber = window?.HelpPage ?? 0;
        var page = WorldMapHelp.Pages[pageNumber];
        GetNode<Label>("HelpBlocker/Help/Margins/Rows/Page/Title").Text = page.Title;
        var lines = GetNode("HelpBlocker/Help/Margins/Rows/Page/Lines");
        for (var index = 0; index < lines.GetChildCount(); index++)
        {
            var line = lines.GetChild<Control>(index);
            line.Visible = index < page.Lines.Count;
            if (line.Visible)
            {
                line.GetNode<Label>("Text").Text = page.Lines[index];
            }
        }

        GetNode<Label>("HelpBlocker/Help/Margins/Rows/Buttons/PageNumber").Text =
            window?.HelpPageText ?? $"1 / {WorldMapHelp.Pages.Count}";
        GetNode<Button>("HelpBlocker/Help/Margins/Rows/Buttons/Back").Disabled = pageNumber == 0;
        GetNode<Button>("HelpBlocker/Help/Margins/Rows/Buttons/Next").Disabled =
            pageNumber == WorldMapHelp.Pages.Count - 1;
    }

    // One bullet row for each line the longest page has; pages with fewer hide the rest.
    private void AddHelpLines()
    {
        var lines = GetNode("HelpBlocker/Help/Margins/Rows/Page/Lines");
        var rowScene = GD.Load<PackedScene>("res://Map/help_line.tscn");
        var mostLines = WorldMapHelp.Pages.Max(page => page.Lines.Count);
        for (var index = lines.GetChildCount(); index < mostLines; index++)
        {
            lines.AddChild(rowScene.Instantiate());
        }

        ShowHelpPage();
    }

    private enum NoteMenuItem
    {
        Edit,
        DeleteSegment,
        Delete,
    }
}
