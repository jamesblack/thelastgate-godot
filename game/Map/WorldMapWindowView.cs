using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;

namespace TheLastGate.Map;

// Its own clicks change its own state; the HUD only opens it and tells it what to show. Every change to the notes is
// handed straight back to be saved.
[Tool]
public partial class WorldMapWindowView : Control
{
    private const int HelpLineGap = 4;
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

    private float TitleBottom => GetNode<Control>("%TitleBar").GetGlobalRect().End.Y - GlobalPosition.Y;

    private WorldMapCanvas Canvas => GetNode<WorldMapCanvas>("%Canvas");

    private Control Help => GetNode<Control>("HelpBlocker");

    private ProceduralCheckBox ShowOnMinimapBox => GetNode<ProceduralCheckBox>("%ShowOnMinimap");

    private ProceduralCheckBox FollowBox => GetNode<ProceduralCheckBox>("%Follow");

    private WorldMapCategories CategoryColumn => GetNode<WorldMapCategories>("%Categories");

    private WorldMapEditor? Editor => window?.Editor;

    private Control LabelPrompt => GetNode<Control>("LabelBlocker");

    private LineEdit LabelName => GetNode<LineEdit>("%LabelName");

    private Control DeletePrompt => GetNode<Control>("DeleteBlocker");

    private LineEdit EditName => GetNode<LineEdit>("%EditName");

    private ProceduralButton EditSave => GetNode<ProceduralButton>("%EditSave");

    private ProceduralButton EditCancel => GetNode<ProceduralButton>("%EditCancel");

    private ProceduralMenu NoteMenu => GetNode<ProceduralMenu>("NoteMenu");

    private ProceduralMenu ColourMenu => GetNode<ProceduralMenu>("NoteColourMenu");

    private static double Now => Time.GetTicksMsec() / 1000.0;

    public event Action? NotesChanged;

    public override void _Ready()
    {
        AddHelpPages();
        if (Engine.IsEditorHint())
        {
            return;
        }

        GetNode<ClickArea>("%Close").Released += _ =>
        {
            window?.Close();
            Visible = false;
        };
        GetNode<ClickArea>("%Help").Released += _ => OpenHelp();
        GetNode<Control>("%TitleBar").GuiInput += DragByTitle;
        GetNode<ResizeGrip>("%ResizeGrip").Dragged += Resize;
        FollowBox.Toggled += isOn =>
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

        Button("%HelpBack").Pressed += () => ChangeHelpPage(-1);
        Button("%HelpNext").Pressed += () => ChangeHelpPage(1);
        Button("%HelpClose").Pressed += () => Help.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() || window == null || !Visible)
        {
            return;
        }

        GetNode<Label>("%Status").Text = window.StatusAt(Now);
        GetNode<Label>("%Coordinates").Text = Canvas.HoveredTile is { } tile ? $"{tile.X}, {tile.Y}" : "";
        FollowBox.Checked = window.Follow;
        ShowOnMinimapBox.Checked = notes?.ShowOnMinimap == true;
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
                when LabelPrompt.Visible || DeletePrompt.Visible || Help.Visible:
                if (LabelPrompt.Visible)
                {
                    window.Editor.CancelLabel();
                }

                DeletePrompt.Visible = false;
                Help.Visible = false;
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
            NoteMenu.Close();
            Help.Visible = false;
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

    private ProceduralButton ToolButton(WorldMapTool tool) => Button($"%{tool}");

    private ProceduralButton Button(string path) => GetNode<ProceduralButton>(path);

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
            button.On = editor.Tool == tool;
        }
    }

    private void ConnectEditControls()
    {
        EditName.TextChanged += label => Editor?.RenameEdited(label);
        EditName.TextSubmitted += _ => SaveEdit();
        EditSave.Pressed += SaveEdit;
        EditCancel.Pressed += () => Editor?.CancelEdit();
    }

    // The name box is filled once when an edit starts, and then belongs to the player.
    private void ShowEditControls(WorldMapEditor editor)
    {
        var edited = editor.Edited;
        var editing = edited != null;
        EditName.Visible = editing;
        EditSave.Visible = editing;
        EditCancel.Visible = editing;
        EditSave.Disabled = !editor.CanSaveEdit;
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
        Button("%LabelConfirm").Pressed += ConfirmLabel;
        Button("%LabelCancel").Pressed += () => Editor?.CancelLabel();
        Button("%DeleteConfirm").Pressed += ConfirmDelete;
        Button("%DeleteCancel").Pressed += () => DeletePrompt.Visible = false;
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
        Button("%LabelConfirm").Disabled = Editor?.CanConfirmLabel(LabelName.Text) != true;

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
        GetNode<Label>("%DeleteQuestion").Text = WorldMapWording.DeleteNoteQuestion(note);
        DeletePrompt.Visible = true;
    }

    private void PrepareMenus()
    {
        NoteMenu.ItemChosen += ChooseFromNoteMenu;
        ColourMenu.ItemChosen += _ => Recolour(Note.CategoryColour);
    }

    public void OpenNoteMenu(NoteHit hit, Vector2 position)
    {
        if (notes?.FindNote(hit.NoteId) is not { } note || notes.OwnerOf(hit.NoteId) is not { } owner)
        {
            return;
        }

        menuHit = hit;
        NoteMenu.Clear();
        NoteMenu.AddItem("Edit", (int)NoteMenuItem.Edit);
        ColourMenu.Clear();
        var usesCategoryColour = note.ColourIndex == Note.CategoryColour;
        var picker = ColourPicker.Load(usesCategoryColour ? owner.ColourIndex : note.ColourIndex);
        picker.ColourChosen += colourIndex =>
        {
            Recolour(colourIndex);
            NoteMenu.Close();
        };
        ColourMenu.AddPicker(picker);
        ColourMenu.AddSeparator();
        ColourMenu.AddCheckItem("Use category colour", CategoryColourItem, usesCategoryColour);
        NoteMenu.AddSubmenu("Colour", ColourMenu);
        NoteMenu.AddSeparator();
        var editingThis = Editor?.Edited?.Id == note.Id;
        if (note is { Kind: NoteKind.Path, Segments.Count: > 1 } && hit.SegmentIndex >= 0 && !editingThis)
        {
            NoteMenu.AddItem("Delete segment", (int)NoteMenuItem.DeleteSegment);
        }

        NoteMenu.AddItem("Delete...", (int)NoteMenuItem.Delete);
        NoteMenu.Popup(position);
    }

    private void ChooseFromNoteMenu(int id)
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
        GetNode<Label>("%HelpTitle").Text = page.Title;
        var pages = GetNode("%HelpPages");
        for (var index = 0; index < pages.GetChildCount(); index++)
        {
            pages.GetChild<Control>(index).Modulate = index == pageNumber ? Colors.White : Colors.Transparent;
        }

        GetNode<Label>("%HelpPageNumber").Text = window?.HelpPageText ?? $"1 / {WorldMapHelp.Pages.Count}";
        Button("%HelpBack").Disabled = pageNumber == 0;
        Button("%HelpNext").Disabled = pageNumber == WorldMapHelp.Pages.Count - 1;
    }

    // Every page is laid out on top of the others and only the current one is seen, so the help is always as tall
    // as its longest page and its buttons stay put.
    private void AddHelpPages()
    {
        var pages = GetNode("%HelpPages");
        var rowScene = GD.Load<PackedScene>("res://Map/help_line.tscn");
        foreach (var page in WorldMapHelp.Pages.Skip(pages.GetChildCount()))
        {
            var lines = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            lines.AddThemeConstantOverride("separation", HelpLineGap);
            foreach (var text in page.Lines)
            {
                var line = rowScene.Instantiate();
                line.GetNode<Label>("Text").Text = text;
                lines.AddChild(line);
            }

            pages.AddChild(lines);
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
