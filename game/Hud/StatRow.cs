using System.Globalization;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Stats;

namespace TheLastGate.Hud;

[Tool]
public partial class StatRow : HBoxContainer
{
    private static readonly string[] StatCells = ["%CurrentCell", "%RaiseArea", "%LowerArea", "%CostCell"];
    private static readonly string[] MetaCells = ["%ValueCell", "%AffixCell"];

    private string previewName = "";

    [Export]
    public string PreviewName
    {
        get => previewName;
        set
        {
            previewName = value;
            ShowPreview();
        }
    }

    public StatLine Line { get; private set; }

    public int Slot { get; private set; }

    public int? Meta { get; private set; }

    public ClickArea NameArea => GetNode<ClickArea>("%NameArea");

    public ClickArea RaiseArea => GetNode<ClickArea>("%RaiseArea");

    public ClickArea LowerArea => GetNode<ClickArea>("%LowerArea");

    public override void _Ready() => ShowPreview();

    public void Show(StatLine line, int slot, bool showBase)
    {
        Line = line;
        Slot = slot;
        Meta = null;
        ShowMetaColumns(false);
        var name = GetNode<HudLabel>("%Name");
        name.Visible = true;
        name.Text = line.Name;
        name.Colour = line.Colour;
        ShowNumber("%Base", showBase ? line.Base : null);
        ShowNumber("%Current", line.Current);
        GetNode<HudLabel>("%Current").Colour = line.Colour;
        ShowStep("%Raise", RaiseArea, line.CanRaise);
        ShowStep("%Lower", LowerArea, line.CanLower);
        ShowNumber("%Cost", line.Cost);
    }

    public void ShowMeta(MetaLine? line, int meta)
    {
        Meta = meta;
        ShowMetaColumns(true);
        GetNode<CanvasItem>("%Base").Visible = false;
        foreach (var name in (string[])["%Name", "%MetaValue", "%Affix"])
        {
            GetNode<CanvasItem>(name).Visible = line != null;
        }

        if (line is { } shown)
        {
            ShowText("%Name", shown.Name, shown.Colour);
            ShowText("%MetaValue", shown.Value, shown.Colour);
            ShowText("%Affix", shown.Affix, shown.Colour);
        }
    }

    private void ShowMetaColumns(bool meta)
    {
        foreach (var name in StatCells)
        {
            GetNode<CanvasItem>(name).Visible = !meta;
        }

        foreach (var name in MetaCells)
        {
            GetNode<CanvasItem>(name).Visible = meta;
        }
    }

    private void ShowStep(string sign, ClickArea area, bool allowed)
    {
        GetNode<CanvasItem>(sign).Visible = allowed;
        area.LightsUp = allowed;
    }

    private void ShowText(string name, string text, TextColour colour)
    {
        var label = GetNode<HudLabel>(name);
        label.Text = text;
        label.Colour = colour;
    }

    private void ShowNumber(string name, int? value)
    {
        var label = GetNode<Label>(name);
        label.Visible = value != null;
        label.Text = value?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    private void ShowPreview()
    {
        if (Engine.IsEditorHint() && IsInsideTree() && previewName != "")
        {
            GetNode<Label>("%Name").Text = previewName;
        }
    }
}
