using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.StyleGuide;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.StyleGuide;

[TestSuite]
[RequireGodotRuntime]
public class StyleGuideViewTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void CaptionMetadata_LabelsATextButton()
    {
        var guide = new StyleGuideView();
        var button = GD.Load<PackedScene>("res://Procedural/text_button.tscn").Instantiate();
        button.SetMeta("caption", "Smith!");
        guide.AddChild(button);

        Tree.Root.AddChild(guide);

        AssertThat(button.GetNode<Label>("Label").Text).IsEqual("Smith!");
        guide.Free();
    }

    [TestCase]
    public void PreviewSpriteMetadata_FillsAnItemCell()
    {
        var guide = new StyleGuideView();
        var row = new HBoxContainer();
        var cell = GD.Load<PackedScene>("res://Hud/item_cell.tscn").Instantiate();
        cell.SetMeta("preview_sprite", 5000);
        row.AddChild(cell);
        guide.AddChild(row);

        Tree.Root.AddChild(guide);

        AssertThat(cell.GetNode<ItemSlotView>("Slot").PreviewSprite).IsEqual(5000);
        guide.Free();
    }

    [TestCase]
    public void TheStyleGuide_Loads()
    {
        var guide = GD.Load<PackedScene>("res://StyleGuide/style_guide.tscn").Instantiate<StyleGuideView>();
        Tree.Root.AddChild(guide);

        AssertThat(guide.GetChildCount()).IsGreater(0);
        guide.Free();
    }
}
