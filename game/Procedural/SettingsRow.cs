using System.Diagnostics;
using Godot;

namespace TheLastGate.Procedural;

// A row of a settings list: it fills while the mouse is over it, and a row that Toggles flips its Line/Box check box
// when clicked anywhere outside the box. A faded rule can sit in the gap above it, and a linked setting's tree line
// runs down its indent, on into the next row when another linked setting follows.
[Tool]
public partial class SettingsRow : ProceduralPanel
{
    // Lists of rows space them this far apart, leaving room for the rule between them.
    public const int RowGap = 5;

    private const float RuleAlpha = 0.45f;
    private const float LineMiddle = 0.5f;
    private const int TreeX = 3;
    private const int BranchY = 6;
    private const int BranchLength = 6;

    private readonly ProceduralLine rule = new() { Name = "Rule", Profile = "G", Width = 1, Visible = false };
    private readonly ProceduralLine treeLine = new() { Name = "TreeLine", Profile = "G", Width = 1, Visible = false };

    private bool ruleAbove;
    private SettingsTree tree;

    [Export] public bool Toggles { get; set; }

    [Export]
    public bool RuleAbove
    {
        get => ruleAbove;
        set
        {
            if (value == ruleAbove)
            {
                return;
            }

            ruleAbove = value;
            DrawLines();
        }
    }

    [Export]
    public SettingsTree Tree
    {
        get => tree;
        set
        {
            if (value == tree)
            {
                return;
            }

            tree = value;
            DrawLines();
        }
    }

    public override void _Ready()
    {
        rule.Modulate = Colors.White with { A = RuleAlpha };
        AddChild(rule);
        AddChild(treeLine);
        Resized += DrawLines;
        DrawLines();
        if (Engine.IsEditorHint())
        {
            return;
        }

        MouseEntered += () => Fill = ProceduralFill.Selected;

        // Moving onto a control inside the row counts as leaving it, so the fill only clears once the pointer is out.
        MouseExited += () =>
        {
            if (!GetGlobalRect().HasPoint(GetGlobalMousePosition()))
            {
                Fill = ProceduralFill.None;
            }
        };
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (Toggles && inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            GetNode<ProceduralCheckBox>("Line/Box").Toggle();
            AcceptEvent();
        }
    }

    private void DrawLines()
    {
        var ruleY = -Mathf.Ceil(RowGap / 2f) + LineMiddle;
        rule.Visible = ruleAbove;
        rule.Points = [new Vector2(0, ruleY), new Vector2(Size.X, ruleY)];

        treeLine.Visible = tree != SettingsTree.None;
        var x = Padding + TreeX + LineMiddle;
        var top = Padding;
        var branch = Padding + BranchY + LineMiddle;
        treeLine.Points = tree switch
        {
            SettingsTree.None => [],
            SettingsTree.Last => [new Vector2(x, top), new Vector2(x, branch), new Vector2(x + BranchLength, branch)],

            // The line runs down past this row and the gap into the next row's padding, where its own line starts.
            SettingsTree.Branch =>
            [
                new Vector2(x + BranchLength, branch), new Vector2(x, branch), new Vector2(x, top),
                new Vector2(x, Size.Y + RowGap + Padding),
            ],
            _ => throw new UnreachableException(),
        };
    }
}
