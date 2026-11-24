using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Hud.SkillTree;

// One node of the tree: answers the mouse within 14 pixels of its centre, measured from whole pixels as the current
// client does. Only the learned ring's stub turns to ParentAngle (0 = up, clockwise): turning the ring itself would
// take its pixels off the grid.
[Tool]
public partial class SkillTreeNodeArea : ClickArea
{
    private const int Radius = 14;
    private const float HighlightBrightness = 2f;
    private const string LearnedLink = "W";

    private bool corrupted;
    private bool highlighted;
    private int iconSprite;
    private bool learned;
    private NodePath link = new();
    private float parentAngle;

    // The line to the parent node, which the tree's scene hands each node so learning it lights the line too. A path,
    // not a node: the editor leaves a node export on an instanced scene unresolved.
    [Export(PropertyHint.NodePathValidTypes, nameof(Line2D))]
    public NodePath Link
    {
        get => link;
        set
        {
            link = value;
            ShowLearned();
        }
    }

    [Export(PropertyHint.Range, "-180,180,0.1,degrees")]
    public float ParentAngle
    {
        get => parentAngle;
        set
        {
            parentAngle = value;
            ShowParentAngle();
        }
    }

    // The view sets the icon from the server's tree; this is the one the scene shows in the editor.
    [Export]
    public int IconSprite
    {
        get => iconSprite;
        set
        {
            iconSprite = value;
            ShowIconSprite();
        }
    }

    [Export]
    public bool Learned
    {
        get => learned;
        set
        {
            learned = value;
            ShowLearned();
        }
    }

    [Export]
    public bool Corrupted
    {
        get => corrupted;
        set
        {
            corrupted = value;
            ShowCorrupted();
        }
    }

    [Export]
    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;
            ShowHighlighted();
        }
    }

    public override void _Ready()
    {
        ShowParentAngle();
        ShowIconSprite();
        ShowLearned();
        ShowCorrupted();
        ShowHighlighted();
    }

    public override bool _HasPoint(Vector2 point) => Contains(point);

    protected override bool Contains(Vector2 point)
    {
        var fromCentre = point.Floor() - new Vector2(Radius, Radius);
        return fromCentre.LengthSquared() < Radius * Radius;
    }

    private void ShowParentAngle()
    {
        if (GetNodeOrNull<Node2D>("Ring/Stub") is { } stub)
        {
            stub.RotationDegrees = parentAngle;
        }
    }

    private void ShowIconSprite()
    {
        if (iconSprite != 0 && GetNodeOrNull<ArchiveSprite>("Icon") is { } icon)
        {
            icon.SpriteNumber = iconSprite;
        }
    }

    private void ShowLearned()
    {
        if (GetNodeOrNull<CanvasItem>("Ring") is { } ring)
        {
            ring.Visible = learned;
        }

        if (IsInsideTree() && GetNodeOrNull<ProceduralLine>(link) is { } line)
        {
            line.Profile = learned ? LearnedLink : "";
        }
    }

    private void ShowCorrupted()
    {
        if (GetNodeOrNull<CanvasItem>("Corruption") is { } corruption)
        {
            corruption.Visible = corrupted;
        }
    }

    private void ShowHighlighted()
    {
        var brightness = highlighted ? HighlightBrightness : 1f;
        foreach (var layer in new[] { "Icon", "Corruption" })
        {
            if (GetNodeOrNull<CanvasItem>(layer) is { } item)
            {
                item.Modulate = new Color(brightness, brightness, brightness);
            }
        }
    }
}
