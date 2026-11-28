using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;

namespace TheLastGate.Map;

// The world map's category column: the tree of categories with their notes, the category menu, the New, Import and
// Export buttons, and the prompts they open. The list is rebuilt only when the notes change; the current category and
// the selected note are picked out by a darker row.
public partial class WorldMapCategories : Control
{
    private const int NoParent = 0;

    private NoteImportDraft? importDraft;
    private int menuCategoryId;
    private int newColour;
    private int newParentId;
    private NoteSet? notes;
    private WorldMapWindow? window;

    public bool PromptOpen => Prompts.Any(prompt => prompt.Visible);

    private CategoryListView List => GetNode<CategoryListView>("%List");

    private ProceduralMenu Menu => GetNode<ProceduralMenu>("%Menu");

    private ProceduralMenu ColourMenu => GetNode<ProceduralMenu>("%ColourMenu");


    private Control NewPrompt => GetNode<Control>("%NewBlocker");

    private Control ImportPrompt => GetNode<Control>("%ImportBlocker");

    private Control ExportPrompt => GetNode<Control>("%ExportBlocker");

    private Control DeletePrompt => GetNode<Control>("%DeleteCategoryBlocker");

    private MergePrompt MergePrompt => GetNode<MergePrompt>("%MergePrompt");

    private Control DuplicatesPrompt => GetNode<Control>("%DuplicatesBlocker");

    private Control[] Prompts => [NewPrompt, ImportPrompt, ExportPrompt, DeletePrompt, DuplicatesPrompt, MergePrompt];

    private LineEdit NewName => GetNode<LineEdit>("%NewName");

    private ColourPicker NewColour => GetNode<ColourPicker>("%NewColour");

    private TextEdit ImportCode => GetNode<TextEdit>("%ImportCode");

    private LineEdit ImportRootName => GetNode<LineEdit>("%ImportRootName");

    private static double Now => Time.GetTicksMsec() / 1000.0;

    public event Action? NotesChanged;

    public event Action<NoteHit, Vector2>? NoteMenuWanted;

    public override void _Ready()
    {
        List.CategoryClicked += id => window?.Editor.ChooseCategory(id);
        List.ItemClicked += SelectNote;
        List.CategoryMenuWanted += OpenCategoryMenu;
        List.ItemMenuWanted += (id, position) => NoteMenuWanted?.Invoke(new NoteHit(id, -1), position);
        List.Changed += SaveChanges;
        List.RowRenamed += Rename;
        Button("%New").Pressed += () => OpenNewPrompt(NoParent);
        Button("%Import").Pressed += () => OpenImportPrompt(NoParent);
        Button("%Export").Pressed += () => Export(window?.Editor.CurrentCategoryId ?? 0);
        MergePrompt.Done += SaveChanges;
        PrepareMenus();
        ConnectNewPrompt();
        ConnectImportPrompt();
        Button("%ExportConfirm").Pressed += () => ExportPrompt.Visible = false;
        Button("%DeleteCategoryConfirm").Pressed += DeleteCategory;
        Button("%DeleteCategoryCancel").Pressed += () => DeletePrompt.Visible = false;
        Button("%DuplicatesConfirm").Pressed += RemoveDuplicates;
        Button("%DuplicatesCancel").Pressed += () => DuplicatesPrompt.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (window == null || notes == null || !IsVisibleInTree())
        {
            return;
        }

        Button("%Export").Disabled = notes.FindCategory(window.Editor.CurrentCategoryId) == null;
        ClosePromptsThatLostTheirCategory(notes);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } && PromptOpen)
        {
            CloseAll();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Show(WorldMapWindow shownWindow, NoteSet? shownNotes)
    {
        if (shownWindow != window || shownNotes != notes)
        {
            List.Show(ListSourceFor(shownWindow, shownNotes));
        }

        window = shownWindow;
        notes = shownNotes;
        foreach (var name in (string[])["New", "Import"])
        {
            Button($"%{name}").Disabled = shownNotes == null;
        }
    }

    public void OpenCategoryMenu(int categoryId, Vector2 position)
    {
        menuCategoryId = categoryId;
        if (FillMenu())
        {
            Menu.Popup(position);
        }
    }

    public void AskAboutClash(CategoryDrag drag, CategoryDropSlot slot, int clashId)
    {
        if (notes != null && window != null)
        {
            MergePrompt.Open(notes, window.Editor, drag, slot, clashId);
        }
    }

    public void CloseAll()
    {
        foreach (var prompt in Prompts)
        {
            prompt.Visible = false;
        }

        importDraft = null;
        Menu.Close();
    }

    private ProceduralButton Button(string path) => GetNode<ProceduralButton>(path);

    private void SaveChanges()
    {
        if (notes?.Changed == true)
        {
            NotesChanged?.Invoke();
        }
    }

    private void SelectNote(int noteId)
    {
        if (notes?.FindNote(noteId) is { } note && window != null)
        {
            window.Editor.Select(noteId);
            window.ShowNote(note);
        }
    }

    private NoteListSource? ListSourceFor(WorldMapWindow shownWindow, NoteSet? shownNotes)
    {
        if (shownNotes == null)
        {
            return null;
        }

        var source = new NoteListSource(shownNotes, shownWindow.Editor);
        source.ClashFound += AskAboutClash;
        return source;
    }

    private void Rename(int id, bool isCategory, string name)
    {
        var wanted = name.Trim();
        if (isCategory && wanted.Length > 0 && notes?.RenameCategory(id, wanted) == true)
        {
            SaveChanges();
        }
    }

    // Showing and hiding a category keep the menu open, so several can be changed in one go.
    private void PrepareMenus()
    {
        Menu.KeepOpenOnCheck = true;
        Menu.ItemChosen += ChooseFromMenu;
    }

    private bool FillMenu()
    {
        if (notes?.FindCategory(menuCategoryId) is not { } category)
        {
            Menu.Close();
            return false;
        }

        Menu.Clear();
        Menu.AddHeading(category.Name);
        Menu.AddSeparator();
        Menu.AddCheckItem("Show on map", (int)MenuItem.ShowOnMap, category.Visible);
        Menu.AddCheckItem("Show labels", (int)MenuItem.ShowLabels, category.ShowLabels);
        Menu.AddSeparator();
        var canNest = notes.Depth(category.Id) < NoteLimits.Depth;
        Menu.AddItem("New subcategory...", (int)MenuItem.NewSubcategory, canNest);
        Menu.AddItem("Import here...", (int)MenuItem.ImportHere, canNest);
        Menu.AddItem("Rename", (int)MenuItem.Rename);
        ColourMenu.Clear();
        var picker = ColourPicker.Load(category.ColourIndex);
        picker.ColourChosen += SetCategoryColour;
        ColourMenu.AddPicker(picker);
        Menu.AddSubmenu("Colour", ColourMenu);
        Menu.AddSeparator();
        Menu.AddItem("Export", (int)MenuItem.Export);
        var duplicates = notes.CountDuplicatePins(category.Id);
        Menu.AddItem(
            WorldMapWording.DuplicatePinsItem(duplicates),
            (int)MenuItem.RemoveDuplicates,
            duplicates > 0,
            duplicates > 0 ? "" : "No pins here share a name and spot with another pin in the same category.");
        var hasSubcategories = notes.HasChildren(category.Id);
        Menu.AddItem(
            "Delete category...",
            (int)MenuItem.Delete,
            !hasSubcategories,
            hasSubcategories ? "Delete or move its subcategories first." : "");
        return true;
    }

    private void SetCategoryColour(int colourIndex)
    {
        if (notes?.SetCategoryColour(menuCategoryId, colourIndex) == true)
        {
            SaveChanges();
        }

        Menu.Close();
    }

    private void ChooseFromMenu(int id)
    {
        if (notes?.FindCategory(menuCategoryId) is not { } category || window == null)
        {
            return;
        }

        switch ((MenuItem)id)
        {
            case MenuItem.ShowOnMap:
                category.Visible = !category.Visible;
                notes.Changed = true;
                break;
            case MenuItem.ShowLabels:
                category.ShowLabels = !category.ShowLabels;
                notes.Changed = true;
                break;
            case MenuItem.NewSubcategory:
                OpenNewPrompt(category.Id);
                break;
            case MenuItem.ImportHere:
                OpenImportPrompt(category.Id);
                break;
            case MenuItem.Rename:
                List.StartRename(category.Id, true, category.Name);
                break;
            case MenuItem.Export:
                Export(category.Id);
                break;
            case MenuItem.RemoveDuplicates:
                AskToRemoveDuplicates(category);
                break;
            case MenuItem.Delete:
                GetNode<Label>("%DeleteCategoryQuestion").Text = WorldMapWording.DeleteCategoryQuestion(category);
                DeletePrompt.Visible = true;
                break;
            default:
                throw new UnreachableException();
        }

        SaveChanges();
    }

    private void Export(int categoryId)
    {
        if (notes?.FindCategory(categoryId) == null)
        {
            return;
        }

        if (NoteShareCode.Export(notes, categoryId) is not { } code)
        {
            window?.ShowStatus("Export failed.", Now);
            return;
        }

        DisplayServer.ClipboardSet(code);
        GetNode<Label>("%ExportMessage").Text = NoteShareCode.CopiedMessage(code.Length);
        GetNode<Label>("%ExportTooLong").Visible = code.Length > NoteShareCode.DiscordLimit;
        ExportPrompt.Visible = true;
    }

    private void DeleteCategory()
    {
        DeletePrompt.Visible = false;
        if (notes != null)
        {
            window?.Editor.DeleteCategory(notes, menuCategoryId);
            SaveChanges();
        }
    }

    private void AskToRemoveDuplicates(NoteCategory category)
    {
        GetNode<Label>("%DuplicatesQuestion").Text = WorldMapWording.DuplicatePinsQuestion(
            notes!.CountDuplicatePins(category.Id),
            category.Name,
            notes.HasChildren(category.Id));
        DuplicatesPrompt.Visible = true;
    }

    private void RemoveDuplicates()
    {
        DuplicatesPrompt.Visible = false;
        if (notes != null)
        {
            window?.Editor.RemoveDuplicatePins(notes, menuCategoryId);
            SaveChanges();
        }
    }

    // A prompt about a category that has since gone, or that can no longer be done, closes by itself.
    private void ClosePromptsThatLostTheirCategory(NoteSet shown)
    {
        var category = shown.FindCategory(menuCategoryId);
        if (DeletePrompt.Visible && (category == null || shown.HasChildren(menuCategoryId)))
        {
            DeletePrompt.Visible = false;
        }

        if (DuplicatesPrompt.Visible && (category == null || shown.CountDuplicatePins(menuCategoryId) == 0))
        {
            DuplicatesPrompt.Visible = false;
        }

        if (ImportPrompt.Visible && importDraft?.DestinationGone != false)
        {
            ImportPrompt.Visible = false;
            importDraft = null;
        }
    }

    private void ConnectNewPrompt()
    {
        NewName.TextChanged += _ => ShowNewPrompt();
        NewName.TextSubmitted += _ => CreateCategory();
        Button("%NewCreate").Pressed += CreateCategory;
        Button("%NewCancel").Pressed += () => NewPrompt.Visible = false;
        NewColour.ColourChosen += chosen => newColour = chosen;
    }

    private void OpenNewPrompt(int parentId)
    {
        newParentId = parentId;
        newColour = 0;
        NewColour.Chosen = newColour;
        NewName.Text = "";
        NewPrompt.Visible = true;
        NewName.GrabFocus();
        ShowNewPrompt();
    }

    private void ShowNewPrompt() => Button("%NewCreate").Disabled = NewName.Text.Length == 0;

    private void CreateCategory()
    {
        if (notes == null || window == null || NewName.Text.Length == 0)
        {
            return;
        }

        NewPrompt.Visible = false;
        window.Editor.CreateCategory(notes, NewName.Text, newColour, newParentId);
        SaveChanges();
    }

    private void ConnectImportPrompt()
    {
        ImportCode.TextChanged += () =>
        {
            importDraft?.ChangeText(ImportCode.Text);
            if (importDraft != null)
            {
                ImportRootName.Text = importDraft.RootName;
            }

            ShowImportPrompt();
        };
        ImportRootName.TextChanged += name =>
        {
            if (importDraft != null)
            {
                importDraft.RootName = name;
            }

            ShowImportPrompt();
        };
        Button("%ImportMerge").Pressed += () => FinishImport(importDraft?.Merge());
        Button("%ImportAddAsNew").Pressed += () => FinishImport(importDraft?.AddAsNew());
        Button("%ImportAdd").Pressed += () => FinishImport(importDraft?.Add());
        Button("%ImportCancel").Pressed += () =>
        {
            ImportPrompt.Visible = false;
            importDraft = null;
        };
    }

    // A code already on the clipboard is pasted in, so sharing one is copy, Import, Add.
    private void OpenImportPrompt(int parentId)
    {
        if (notes == null)
        {
            return;
        }

        importDraft = new NoteImportDraft(notes, parentId, DisplayServer.ClipboardGet());
        ImportCode.Text = importDraft.Text;
        ImportRootName.Text = importDraft.RootName;
        ImportPrompt.Visible = true;
        ImportCode.GrabFocus();
        ShowImportPrompt();
    }

    private void ShowImportPrompt()
    {
        if (importDraft is not { } draft)
        {
            return;
        }

        var destination = GetNode<Label>("%ImportDestination");
        destination.Visible = draft.DestinationName != null;
        destination.Text = $"Import into \"{draft.DestinationName}\"";
        GetNode<Label>("%ImportPreview").Text = draft.Preview;
        GetNode<Label>("%ImportPreview").Visible = draft.Preview.Length > 0;
        GetNode<Label>("%ImportConflict").Text = draft.ConflictMessage;
        foreach (var path in (string[])["%ImportConflict", "%ImportMerge", "%ImportRename", "%ImportAddAsNew"])
        {
            GetNode<Control>(path).Visible = draft.HasConflict;
        }

        Button("%ImportAddAsNew").Disabled = !draft.CanAddAsNew;
        Button("%ImportAdd").Visible = !draft.HasConflict;
        Button("%ImportAdd").Disabled = !draft.IsReady;
        GetNode<Label>("%ImportError").Text = draft.Error;
        GetNode<Label>("%ImportError").Visible = draft.Error.Length > 0;
    }

    private void FinishImport(NoteImportResult? result)
    {
        if (result == null || importDraft == null || window == null)
        {
            return;
        }

        if (window.Editor.FinishImport(result, importDraft.ParentId))
        {
            ImportPrompt.Visible = false;
            importDraft = null;
            SaveChanges();
            return;
        }

        ShowImportPrompt();
    }

    private enum MenuItem
    {
        ShowOnMap,
        ShowLabels,
        NewSubcategory,
        ImportHere,
        Rename,
        Export,
        RemoveDuplicates,
        Delete,
    }
}
