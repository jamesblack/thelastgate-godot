using Godot;

namespace TheLastGate.Procedural;

[Tool]
public partial class RuledHeading : VBoxContainer
{
    private const float LineMiddle = 0.5f;

    private string text = "";

    [Export]
    public string Text
    {
        get => text;
        set
        {
            text = value;
            if (GetNodeOrNull<Label>("Title") is { } title && value.Length > 0)
            {
                title.Text = value;
            }
        }
    }

    public override void _Ready()
    {
        Text = text;
        var rule = GetNode<Control>("Rule");
        rule.Resized += () => SpanTheRule(rule);
        SpanTheRule(rule);
    }

    private static void SpanTheRule(Control rule) =>
        rule.GetNode<Line2D>("Line").Points = [new Vector2(0, LineMiddle), new Vector2(rule.Size.X, LineMiddle)];
}
