using System;
using System.Diagnostics;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.World;

namespace TheLastGate.Windows;

public enum CategoryRowState
{
    Normal,
    Current,
    Selected,
    Dimmed,
    Source,
    Target,
    Ghost,
    RefusedGhost,
}

[Tool]
public partial class CategoryRowView : ProceduralPanel
{
    public const int IndentPerLevel = 10;

    private const float DimmedSwatch = 0.35f;
    private const float SourceAlpha = 0.3f;
    private const float GhostAlpha = 0.85f;
    private const float TargetLight = 1.4f;

    private static readonly Color HoveredLight = new(2.2f, 2.2f, 2.2f);
    private static readonly Color RefusedTint = new(1f, 0.4f, 0.4f, GhostAlpha);
    private static readonly Vector2[] Collapsed = [new(3.5f, 2.5f), new(6.5f, 5.5f), new(3.5f, 8.5f)];
    private static readonly Vector2[] Opened = [new(1.5f, 4.5f), new(4.5f, 7.5f), new(7.5f, 4.5f)];

    private bool hoverable = true;
    private bool hovered;
    private string previewText = "";
    private string shownHoverText = "";
    private CategoryRowState shownState;

    // For the style guide: a row shown as this text in PreviewState, without a source to read it from.
    [Export]
    public string PreviewText
    {
        get => previewText;
        set
        {
            previewText = value;
            if (IsNodeReady())
            {
                ShowPreview();
            }
        }
    }

    // Rows don't light up under the cursor while something is being dragged over them.
    public bool Hoverable
    {
        get => hoverable;
        set
        {
            hoverable = value;
            ShowLight();
        }
    }

    [Export] public CategoryRowState PreviewState { get; set; }

    [Export] public int PreviewDepth { get; set; }

    [Export] public bool PreviewIsNote { get; set; }

    [Export] public bool PreviewExpanded { get; set; }

    [Export] public int PreviewColour { get; set; } = 1;

    [Export] public bool PreviewHovered { get; set; }

    private LineEdit Field => GetNode<LineEdit>("Cells/Field");

    private HudLabel HoverLabel => GetNode<HudLabel>("Cells/HoverText");

    public event Action<MapButton, Vector2>? Pressed;

    public event Action<string>? RenameFinished;

    public event Action? RenameCancelled;

    public override void _Ready()
    {
        ShowPreview();
        if (GetNode("Click") is not ClickArea click)
        {
            return;
        }

        click.ShadeChanged += shade =>
        {
            hovered = shade != Colors.White;
            ShowLight();
        };
        click.Released += button => Pressed?.Invoke(button, GetLocalMousePosition());
        Field.TextSubmitted += name => FinishRename(name);
        Field.FocusExited += () =>
        {
            if (Field.Visible)
            {
                FinishRename(Field.Text);
            }
        };
        Field.GuiInput += inputEvent =>
        {
            if (inputEvent is InputEventKey { Pressed: true, Keycode: Key.Escape })
            {
                EndRename();
                RenameCancelled?.Invoke();
                Field.AcceptEvent();
            }
        };
    }

    public void Show(CategoryRow row, CategoryRowLook look, bool expanded)
    {
        var ghost = look.State is CategoryRowState.Ghost or CategoryRowState.RefusedGhost;
        GetNode<Control>("Cells/Indent").CustomMinimumSize = new Vector2(ghost ? 0 : row.Depth * IndentPerLevel, 0);
        var chevron = GetNode<Line2D>("Cells/Chevron/Line");
        chevron.Visible = row.IsCategory;
        chevron.Points = expanded ? Opened : Collapsed;
        var dimmed = look.State == CategoryRowState.Dimmed;
        var name = GetNode<HudLabel>("Cells/Name");
        name.ClipText = !ghost;
        name.Colour = dimmed ? TextColour.Dim : TextColour.Yellow;
        name.Text = look.Text;
        shownHoverText = ghost ? "" : look.HoverText;
        HoverLabel.Text = shownHoverText;
        HoverLabel.Colour = TextColour.Dim;
        var colour = GetNode<ColorRect>("Cells/Swatch/Colour");
        colour.Visible = look.Swatch != null;
        if (look.Swatch is { } swatch)
        {
            colour.Color = dimmed ? swatch.Darkened(1 - DimmedSwatch) : swatch;
        }

        shownState = look.State;
        ShowLight();
    }

    public bool OnChevron(Vector2 localPosition) => GetNode<Control>("Cells/Chevron").GetRect().HasPoint(localPosition);

    public void BeginRename(string currentName)
    {
        GetNode<Control>("Cells/Name").Visible = false;
        Field.Visible = true;
        ShowLight();
        Field.Text = currentName;
        Field.GrabFocus();
        Field.SelectAll();
    }

    public void ClearHover()
    {
        if (GetNode("Click") is ClickArea click)
        {
            click.ClearHover();
        }
    }

    public void EndRename()
    {
        Field.Visible = false;
        GetNode<Control>("Cells/Name").Visible = true;
        ShowLight();
    }

    private void ShowPreview()
    {
        if (previewText.Length == 0)
        {
            return;
        }

        hovered = PreviewHovered;
        var rgb = NotePalette.Rgb(PreviewColour);
        Color? swatch = PreviewIsNote ? null : Color.Color8((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        Show(
            new CategoryRow(1, !PreviewIsNote, PreviewDepth, 0, 0),
            new CategoryRowLook(previewText, swatch, PreviewState),
            PreviewExpanded);
    }

    private void FinishRename(string name)
    {
        EndRename();
        RenameFinished?.Invoke(name);
    }

    private void ShowLight()
    {
        HoverLabel.Visible = shownHoverText.Length > 0 && hovered && hoverable && !Field.Visible;
        var ringed = shownState is CategoryRowState.Selected or CategoryRowState.Target or CategoryRowState.Ghost
            or CategoryRowState.RefusedGhost;
        Profile = ringed ? "G" : "";
        Padding = ringed ? 0 : 1;
        Fill = shownState switch
        {
            CategoryRowState.Current or CategoryRowState.Selected or CategoryRowState.Target =>
                ProceduralFill.Selected,
            CategoryRowState.Ghost or CategoryRowState.RefusedGhost => ProceduralFill.Red,
            CategoryRowState.Normal or CategoryRowState.Dimmed or CategoryRowState.Source => ProceduralFill.None,
            _ => throw new UnreachableException(),
        };
        Modulate = shownState switch
        {
            CategoryRowState.Source => Colors.White with { A = SourceAlpha },
            CategoryRowState.Ghost => Colors.White with { A = GhostAlpha },
            CategoryRowState.RefusedGhost => RefusedTint,
            CategoryRowState.Target => new Color(TargetLight, TargetLight, TargetLight),
            CategoryRowState.Normal or CategoryRowState.Current or CategoryRowState.Selected
                or CategoryRowState.Dimmed => hovered && hoverable ? HoveredLight : Colors.White,
            _ => throw new UnreachableException(),
        };
    }
}
