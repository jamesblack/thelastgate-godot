using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Procedural;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Depot;

[Tool]
public partial class DepotCharactersView : Control
{
    private const int NoParent = 0;
    private const int Last = -1;
    private const int PreviewMain = 1;
    private const int PreviewAlt = 2;
    private const int PreviewMule = 3;
    private const int PreviewOpenPage = 2;

    private static readonly DepotCharacter[] PreviewCharacters =
        [new(PreviewMain, "Worldofslaying"), new(PreviewAlt, "Altchar"), new(PreviewMule, "Mulecharacter")];

    private DepotLayout? layout;
    private int menuCategoryId;
    private int menuPageId;
    private int newParentId;
    private int scrolledToPageId;
    private DepotListSource? source;

    private CategoryListView List => GetNode<CategoryListView>("%List");

    private ProceduralMenu Menu => GetNode<ProceduralMenu>("%Menu");

    private Control NewPrompt => GetNode<Control>("%NewBlocker");

    private Control DeletePrompt => GetNode<Control>("%DeleteBlocker");

    private LineEdit NewName => GetNode<LineEdit>("%NewName");

    private ProceduralButton NewCreate => GetNode<ProceduralButton>("%NewCreate");

    private HudLabel Status => GetNode<HudLabel>("%Status");

    private bool PromptOpen => NewPrompt.Visible || DeletePrompt.Visible;

    public event Action<int, int>? PageChosen;

    public event Action? LayoutChanged;

    public event Action<int>? SortWanted;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            ShowPreview();
            return;
        }

        List.CategoryClicked += ToggleCategory;
        List.ItemClicked += ChoosePage;
        List.CategoryMenuWanted += OpenCategoryMenu;
        List.ItemMenuWanted += OpenPageMenu;
        List.Changed += () => LayoutChanged?.Invoke();
        List.RowRenamed += Rename;
        Menu.ItemChosen += ChooseFromMenu;
        GetNode<ProceduralButton>("%NewCategory").Pressed += () => OpenNewPrompt(NoParent);
        NewName.TextChanged += _ => ShowNewPrompt();
        NewName.TextSubmitted += _ => CreateCategory();
        NewCreate.Pressed += CreateCategory;
        GetNode<ProceduralButton>("%NewCancel").Pressed += () => NewPrompt.Visible = false;
        GetNode<ProceduralButton>("%DeleteConfirm").Pressed += DeleteCategory;
        GetNode<ProceduralButton>("%DeleteCancel").Pressed += () => DeletePrompt.Visible = false;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } && PromptOpen
            && IsVisibleInTree())
        {
            ClosePrompts();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && !Engine.IsEditorHint() && IsNodeReady() && !IsVisibleInTree())
        {
            ClosePrompts();
            Menu.Close();
        }

        // The preview's status is set on this scene's own nodes, so it is cleared around editor saves.
        if (!Engine.IsEditorHint() || !IsNodeReady())
        {
            return;
        }

        if (what == NotificationEditorPreSave)
        {
            ClearStatus();
        }
        else if (what == NotificationEditorPostSave)
        {
            ShowPreviewStatus();
        }
    }

    public void Show(DepotLayout shownLayout, int openCharacterNumber, int openPage)
    {
        if (shownLayout != layout || source == null)
        {
            layout = shownLayout;
            source = new DepotListSource(shownLayout);
            List.Show(source);
            scrolledToPageId = 0;
        }

        source.ShowOpen(openCharacterNumber, openPage);
        var openPageId = DepotLayout.PageId(openCharacterNumber, openPage);
        if (openPageId != scrolledToPageId)
        {
            scrolledToPageId = openPageId;
            List.ScrollTo(openPageId, false);
        }
    }

    public void ShowNoAnswer(string characterName) => ShowStatus($"{characterName}'s depot didn't answer.");

    public void ClearStatus() => ShowStatus("");

    private static string DeleteQuestion(string name, (int Pages, int Categories) beneath)
    {
        var clauses = new List<string>();
        if (beneath.Pages == 1)
        {
            clauses.Add("1 page goes back to its character and loses its name");
        }
        else if (beneath.Pages > 1)
        {
            clauses.Add($"{beneath.Pages} pages go back to their characters and lose their names");
        }

        if (beneath.Categories == 1)
        {
            clauses.Add("1 category inside it is deleted too");
        }
        else if (beneath.Categories > 1)
        {
            clauses.Add($"{beneath.Categories} categories inside it are deleted too");
        }

        return clauses.Count == 0 ? $"Delete {name}?" : $"Delete {name}? {string.Join(", and ", clauses)}.";
    }

    private void ClosePrompts()
    {
        NewPrompt.Visible = false;
        DeletePrompt.Visible = false;
    }

    private void ShowStatus(string text)
    {
        Status.Text = text;
        Status.Visible = text.Length > 0;
    }

    private void ToggleCategory(int categoryId) =>
        source?.Expand(categoryId, !source.Expanded.Contains(categoryId));

    private void ChoosePage(int pageId)
    {
        var (characterNumber, page) = DepotLayout.PageOf(pageId);
        PageChosen?.Invoke(characterNumber, page);
    }

    private void OpenCategoryMenu(int categoryId, Vector2 position)
    {
        if (layout == null)
        {
            return;
        }

        menuCategoryId = categoryId;
        menuPageId = 0;
        Menu.Clear();
        Menu.AddHeading(layout.CategoryText(categoryId));
        Menu.AddSeparator();
        Menu.AddItem(
            "New subcategory...",
            (int)MenuItem.NewSubcategory,
            layout.Depth(categoryId) + 1 < DepotLayout.MaxDepth);
        if (layout.IsCharacterCategory(categoryId))
        {
            Menu.AddItem("Sort depot", (int)MenuItem.Sort);
        }
        else
        {
            Menu.AddItem("Rename", (int)MenuItem.Rename);
            Menu.AddItem("Delete...", (int)MenuItem.Delete);
        }

        Menu.Popup(position);
    }

    private void OpenPageMenu(int pageId, Vector2 position)
    {
        if (layout == null)
        {
            return;
        }

        menuCategoryId = 0;
        menuPageId = pageId;
        Menu.Clear();
        Menu.AddHeading(layout.PageText(pageId));
        Menu.AddSeparator();
        Menu.AddItem("Rename", (int)MenuItem.Rename);
        Menu.AddItem("Reset", (int)MenuItem.Reset, !IsHome(layout, pageId));
        Menu.AddItem("Sort depot", (int)MenuItem.Sort);
        Menu.Popup(position);
    }

    private static bool IsHome(DepotLayout shown, int pageId)
    {
        var (characterNumber, page) = DepotLayout.PageOf(pageId);
        return shown.Pages.FirstOrDefault(saved => saved.CharacterNumber == characterNumber && saved.Page == page)
                is not { } found
            || (found.ParentId == shown.CategoryOf(characterNumber) && found.Name.Length == 0);
    }

    private void ChooseFromMenu(int id)
    {
        if (layout == null)
        {
            return;
        }

        switch ((MenuItem)id)
        {
            case MenuItem.NewSubcategory:
                OpenNewPrompt(menuCategoryId);
                break;
            case MenuItem.Rename when menuPageId != 0:
                List.StartRename(menuPageId, false, layout.PageName(menuPageId));
                break;
            case MenuItem.Rename:
                List.StartRename(menuCategoryId, true, layout.CategoryText(menuCategoryId));
                break;
            case MenuItem.Reset:
                layout.ResetPage(menuPageId);
                LayoutChanged?.Invoke();
                break;
            case MenuItem.Delete:
                GetNode<Label>("%DeleteQuestion").Text = DeleteQuestion(
                    layout.CategoryText(menuCategoryId),
                    layout.DeleteSummary(menuCategoryId));
                DeletePrompt.Visible = true;
                break;
            case MenuItem.Sort when menuPageId != 0:
                SortWanted?.Invoke(DepotLayout.PageOf(menuPageId).CharacterNumber);
                break;
            case MenuItem.Sort:
                SortWanted?.Invoke(layout.CharacterOf(menuCategoryId));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void Rename(int id, bool isCategory, string name)
    {
        if (layout == null)
        {
            return;
        }

        var before = layout.Revision;
        if (isCategory)
        {
            layout.RenameCategory(id, name);
        }
        else
        {
            layout.RenamePage(id, name);
        }

        if (layout.Revision != before)
        {
            LayoutChanged?.Invoke();
        }
    }

    private void OpenNewPrompt(int parentId)
    {
        newParentId = parentId;
        NewName.Text = "";
        NewPrompt.Visible = true;
        NewName.GrabFocus();
        ShowNewPrompt();
    }

    private void ShowNewPrompt() => NewCreate.Disabled = NewName.Text.Trim().Length == 0;

    private void CreateCategory()
    {
        if (layout == null || NewName.Text.Trim().Length == 0)
        {
            return;
        }

        NewPrompt.Visible = false;
        if (layout.CreateCategory(NewName.Text.Trim(), newParentId) != 0)
        {
            source?.Expand(newParentId, true);
            LayoutChanged?.Invoke();
        }
    }

    private void DeleteCategory()
    {
        DeletePrompt.Visible = false;
        if (layout != null)
        {
            layout.DeleteCategory(menuCategoryId);
            LayoutChanged?.Invoke();
        }
    }

    private void ShowPreview()
    {
        var preview = new DepotLayout();
        preview.Sync(PreviewCharacters);
        var potions = DepotLayout.PageId(PreviewMain, PreviewOpenPage);
        preview.RenamePage(potions, "Potions");
        var bank = preview.CreateCategory("Bank", NoParent);
        preview.Drop(
            new CategoryDrag(DepotLayout.PageId(PreviewAlt, 0), false),
            new CategoryDropSlot(bank, Last, true));
        Show(preview, PreviewMain, PreviewOpenPage);
        source!.Expand(bank, true);
        ShowPreviewStatus();
    }

    private void ShowPreviewStatus() => ShowNoAnswer(PreviewCharacters[^1].Name);

    private enum MenuItem
    {
        NewSubcategory,
        Rename,
        Reset,
        Delete,
        Sort,
    }
}
