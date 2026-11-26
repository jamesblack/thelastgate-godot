using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralMenu : Control
{
    private const string RowScene = "res://Procedural/menu_row.tscn";
    private const int SeparatorInset = 3;
    private const int PickerColumns = 4;
    private const int PreviewPickerChosen = 1;
    private const int PreviewChoicesWidth = 110;
    private const int PreviewChoicesHovered = 2;

    private static readonly Color HoveredRow = new(1.3f, 1.3f, 1.3f);

    private readonly List<ProceduralMenu> submenus = [];

    // The world acts on a click's release, so the release of the click that closed the menu is kept from it too.
    private MouseButton? closingButton;
    private bool isSubmenu;
    private ProceduralMenu? openSubmenu;
    private PackedScene? rowScene;

    public bool IsOpen => Visible;

    public bool KeepOpenOnCheck { get; set; }

    // For the style guide: fills the menu with sample rows and shows it in place instead of popping up.
    [Export] public MenuPreview Preview { get; set; }

    private Container Frame => GetNode<Container>("Frame");

    private VBoxContainer Items => GetNode<VBoxContainer>("Frame/Items");

    public event Action<int>? ItemChosen;

    public override void _Ready()
    {
        if (Preview != MenuPreview.None)
        {
            ShowPreview();
        }
        else if (!Engine.IsEditorHint())
        {
            Visible = false;
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (isSubmenu || Preview != MenuPreview.None || Engine.IsEditorHint())
        {
            return;
        }

        if (inputEvent is InputEventMouseButton { Pressed: false } release && release.ButtonIndex == closingButton)
        {
            closingButton = null;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!IsOpen)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }:
                Close();
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true } press
                when press.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)
                && !Covers(press.Position):
                Close();
                closingButton = press.ButtonIndex;
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public void Clear()
    {
        CloseSubmenu();
        foreach (var row in Items.GetChildren())
        {
            Items.RemoveChild(row);
            row.QueueFree();
        }
    }

    public void AddHeading(string text)
    {
        var row = NewRow("Heading", text, TextColour.White);
        row.GetNode<Control>("Click").MouseFilter = MouseFilterEnum.Ignore;
        Items.AddChild(row);
    }

    public void AddSeparator()
    {
        var separator = new MarginContainer { Name = "Separator", MouseFilter = MouseFilterEnum.Ignore };
        foreach (var side in (string[])["left", "right"])
        {
            separator.AddThemeConstantOverride($"margin_{side}", SeparatorInset);
        }

        foreach (var side in (string[])["top", "bottom"])
        {
            separator.AddThemeConstantOverride($"margin_{side}", 1);
        }

        separator.AddChild(
            new ProceduralPanel
            {
                Profile = "G",
                CustomMinimumSize = new Vector2(0, 1),
                MouseFilter = MouseFilterEnum.Ignore,
            });
        Items.AddChild(separator);
    }

    public void AddItem(string text, int id, bool enabled = true, string tooltip = "") =>
        AddChoice(text, id, enabled, tooltip, null);

    public void AddCheckItem(string text, int id, bool isChecked, bool enabled = true) =>
        AddChoice(text, id, enabled, "", isChecked);

    public void AddSubmenu(string text, ProceduralMenu submenu)
    {
        if (!submenus.Contains(submenu))
        {
            submenus.Add(submenu);
            submenu.isSubmenu = true;
            submenu.ItemChosen += _ => Close();
            if (!submenu.IsInsideTree())
            {
                AddChild(submenu);
            }
        }

        var row = SubmenuRow($"Submenu{submenus.IndexOf(submenu)}", text);
        if (row.GetNode("Click") is ClickArea click)
        {
            LightOnHover(row, click);
            click.MouseEntered += () => OpenSubmenu(submenu, row);
        }

        Items.AddChild(row);
    }

    public void AddRow(Control row) => Items.AddChild(row);

    public void AddPicker(ColourPicker picker)
    {
        picker.Columns = PickerColumns;
        var room = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        foreach (var side in (string[])["left", "top", "right"])
        {
            room.AddThemeConstantOverride($"margin_{side}", SeparatorInset);
        }

        room.AddChild(picker);
        AddRow(room);
    }

    public void Popup(Vector2 position)
    {
        CloseSubmenu();
        Visible = true;
        Size = Frame.GetCombinedMinimumSize();
        Frame.Size = Size;
        var screen = GetViewportRect().Size;
        GlobalPosition = position.Clamp(Vector2.Zero, (screen - Size).Max(Vector2.Zero));
    }

    public void Close()
    {
        CloseSubmenu();
        Visible = false;
    }

    private bool Covers(Vector2 point) =>
        GetGlobalRect().HasPoint(point) || (openSubmenu?.GetGlobalRect().HasPoint(point) ?? false);

    // Hovering any other row closes an open submenu; a submenu row opens its own beside it.
    private void LightOnHover(ProceduralPanel row, ClickArea click)
    {
        click.ShadeChanged += shade =>
        {
            var lit = shade != Colors.White;
            row.Fill = lit ? ProceduralFill.Selected : ProceduralFill.None;
            row.Modulate = lit ? HoveredRow : Colors.White;
        };
        click.MouseEntered += CloseSubmenu;
    }

    private void OpenSubmenu(ProceduralMenu submenu, Control row)
    {
        if (openSubmenu == submenu)
        {
            return;
        }

        CloseSubmenu();
        openSubmenu = submenu;
        var right = new Vector2(GlobalPosition.X + Size.X, row.GlobalPosition.Y);
        submenu.Popup(right);
        if (submenu.GlobalPosition.X < right.X)
        {
            submenu.Popup(new Vector2(GlobalPosition.X - submenu.Size.X, row.GlobalPosition.Y));
        }
    }

    private void CloseSubmenu()
    {
        openSubmenu?.Close();
        openSubmenu = null;
    }

    private void AddChoice(string text, int id, bool enabled, string tooltip, bool? isChecked)
    {
        var row = NewRow($"Item{id}", text, enabled ? TextColour.Yellow : TextColour.Dim);
        var check = row.GetNode<Control>("Cells/Check");
        if (isChecked is { } checkedNow)
        {
            check.Modulate = Colors.White;
            check.GetNode<Control>("Mark").Visible = checkedNow;
        }

        var click = row.GetNode<Control>("Click");
        click.TooltipText = tooltip;
        if (enabled && click is ClickArea area)
        {
            LightOnHover(row, area);
            area.Released += button =>
            {
                if (button == MapButton.Left)
                {
                    Choose(id, isChecked.HasValue ? check.GetNode<Control>("Mark") : null);
                }
            };
        }

        Items.AddChild(row);
    }

    private void Choose(int id, Control? mark)
    {
        if (mark != null)
        {
            mark.Visible = !mark.Visible;
        }

        ItemChosen?.Invoke(id);
        if (mark == null || !KeepOpenOnCheck)
        {
            Close();
        }
    }

    private ProceduralPanel SubmenuRow(string name, string text)
    {
        var row = NewRow(name, text, TextColour.Yellow);
        row.GetNode<Control>("Cells/Arrow").Visible = true;
        return row;
    }

    private void ShowPreview()
    {
        TopLevel = false;
        Visible = true;
        Clear();
        switch (Preview)
        {
            case MenuPreview.Category:
                AddHeading("Places");
                AddSeparator();
                AddCheckItem("Show on map", 0, true);
                AddCheckItem("Show labels", 1, false);
                AddSeparator();
                AddItem("New subcategory...", 2);
                AddItem("Rename", 3);
                var hovered = Items.GetNode<ProceduralPanel>("Item3");
                hovered.Fill = ProceduralFill.Selected;
                hovered.Modulate = HoveredRow;
                Items.AddChild(SubmenuRow("Colour", "Colour"));
                AddSeparator();
                AddItem("Remove duplicate pins...", 4, false);
                AddItem("Delete category...", 5);
                break;
            case MenuPreview.Colours:
                AddPicker(ColourPicker.Load(PreviewPickerChosen));
                AddSeparator();
                AddCheckItem("Use category colour", 0, false);
                break;
            case MenuPreview.Choices:
                string[] choices = ["Unarmed", "Dagger", "Sword", "Two-Handed"];
                for (var index = 0; index < choices.Length; index++)
                {
                    AddItem(choices[index], index);
                }

                var chosen = Items.GetNode<ProceduralPanel>($"Item{PreviewChoicesHovered}");
                chosen.Fill = ProceduralFill.Selected;
                chosen.Modulate = HoveredRow;
                Frame.CustomMinimumSize = new Vector2(PreviewChoicesWidth, 0);
                break;
            case MenuPreview.None:
                return;
            default:
                throw new UnreachableException();
        }

        Size = Frame.GetCombinedMinimumSize();
        CustomMinimumSize = Size;
    }

    private ProceduralPanel NewRow(string name, string text, TextColour colour)
    {
        rowScene ??= GD.Load<PackedScene>(RowScene);
        var row = rowScene.Instantiate<ProceduralPanel>();
        row.Name = name;
        var label = row.GetNode<HudLabel>("Cells/Label");
        label.Text = text;
        label.Colour = colour;
        return row;
    }
}
