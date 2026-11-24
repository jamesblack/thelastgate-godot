using Godot;
using TheLastGate.Hud;

namespace TheLastGate.StyleGuide;

// A button's label and a cell's slot belong to their own scenes, so the editor would not save a caption or a sample
// item set on them here; each example keeps them as metadata on its instance instead, applied when the guide loads.
[Tool]
public partial class StyleGuideView : Control
{
    private static readonly StringName Caption = "caption";
    private static readonly StringName PreviewSprite = "preview_sprite";

    public override void _Ready() => ApplyMetadata(this);

    private static void ApplyMetadata(Node node)
    {
        if (node.HasMeta(Caption))
        {
            node.GetNode<Label>("Label").Text = node.GetMeta(Caption).AsString();
        }

        if (node.HasMeta(PreviewSprite))
        {
            node.GetNode<ItemSlotView>("Slot").PreviewSprite = node.GetMeta(PreviewSprite).AsInt32();
        }

        foreach (var child in node.GetChildren())
        {
            ApplyMetadata(child);
        }
    }
}
