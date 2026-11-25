using Godot;
using Godot.Collections;
using TheLastGate.Hud;
using TheLastGate.Windows;

namespace TheLastGate.Procedural;

// Draws its whole rect and lets the shader shape it, so nothing generated is a property and a scene saves only the
// settings. Children sit inside the rings and the padding. Fading sets only its own alpha, never its children's.
[Tool]
public partial class ProceduralPanel : Container, IHudContextUser
{
    private ProceduralEdges edges = ProceduralEdges.All;
    private ProceduralFill fill;
    private Rect2 hole;
    private int padding;
    private string profile = "";
    private ProceduralShape shape;
    private bool solidRings;

    public ProceduralPanel()
    {
        AddToGroup(HudContext.Group);
        Restyle();
    }

    [Export]
    public ProceduralShape Shape
    {
        get => shape;
        set
        {
            if (value == shape)
            {
                return;
            }

            shape = value;
            Restyle();
        }
    }

    [Export]
    public ProceduralFill Fill
    {
        get => fill;
        set
        {
            if (value == fill)
            {
                return;
            }

            fill = value;
            Restyle();
        }
    }

    // Rings from the outside in: G gold, B blue, R red, W white.
    [Export]
    public string Profile
    {
        get => profile;
        set
        {
            if (value == profile)
            {
                return;
            }

            profile = value;
            Restyle();
            Remeasure();
        }
    }

    // A side left out draws no rings, so a neighbour's border along it is the only one.
    [Export]
    public ProceduralEdges Edges
    {
        get => edges;
        set
        {
            if (value == edges)
            {
                return;
            }

            edges = value;
            Restyle();
            Remeasure();
        }
    }

    [Export]
    public int Padding
    {
        get => padding;
        set
        {
            if (value == padding)
            {
                return;
            }

            padding = value;
            Restyle();
            Remeasure();
        }
    }

    // An isometric diamond cut out of the panel, in the panel's own pixels.
    [Export]
    public Rect2 Hole
    {
        get => hole;
        set
        {
            if (value == hole)
            {
                return;
            }

            hole = value;
            Restyle();
        }
    }

    [Export] public WindowBackground FadesAs { get; set; }

    [Export]
    public bool SolidRings
    {
        get => solidRings;
        set
        {
            if (value == solidRings)
            {
                return;
            }

            solidRings = value;
            Restyle();
        }
    }

    private Vector2 InsetBefore => new(Inset(ProceduralEdges.Left), Inset(ProceduralEdges.Top));

    private Vector2 InsetAfter => new(Inset(ProceduralEdges.Right), Inset(ProceduralEdges.Bottom));

    public void ShowContext(HudContext context) =>
        SelfModulate = Colors.White with { A = WindowBackdrop.Alpha(context.ClientSettings, FadesAs) };

    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Colors.White);

    public override Vector2 _GetMinimumSize()
    {
        var largest = Vector2.Zero;
        foreach (var child in GetChildren())
        {
            if (child is Control { Visible: true, TopLevel: false } control)
            {
                largest = largest.Max(control.GetCombinedMinimumSize());
            }
        }

        return largest + InsetBefore + InsetAfter;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
        {
            return;
        }

        var inside = new Rect2(InsetBefore, Size - InsetBefore - InsetAfter);
        foreach (var child in GetChildren())
        {
            if (child is Control { TopLevel: false } control)
            {
                FitChildInRect(control, inside);
            }
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsString() != CanvasItem.PropertyName.Material)
        {
            return;
        }

        property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
    }

    private int Inset(ProceduralEdges side) => (edges.HasFlag(side) ? profile.Length : 0) + padding;

    private void Restyle() => Material = ProceduralLook.Panel(shape, profile, fill, edges, solidRings, hole);

    private void Remeasure()
    {
        UpdateMinimumSize();
        QueueSort();
    }
}
