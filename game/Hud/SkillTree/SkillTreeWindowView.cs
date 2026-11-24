using Godot;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Hud.SkillTree;

// The class or contract tree: twelve nodes, a white ring around each learned one, and the hovered node's text below.
// The scene is saved visible so it can be laid out in the editor; the game hides it until a tree is opened.
[Tool]
public partial class SkillTreeWindowView : Control
{
    private const int ClassCentre = 6600;
    private const int ContractCentre = 7066;

    private int? hoveredNode;
    private PlayerState player = new();
    private State.SkillTree shownTree;
    private Terminology terminology = new();
    private SkillTreeWindow? window;

    [Export]
    public State.SkillTree ShownTree
    {
        get => shownTree;
        set
        {
            shownTree = value;
            ShowCentre();
        }
    }

    public ClickArea CloseButton => GetNode<ClickArea>("%CloseButton");

    public SkillTreeNodeArea Node(int node) => GetNode<SkillTreeNodeArea>($"%TreeArea/Diagram/Node{node}");

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        Visible = false;
        for (var node = 0; node < SkillTrees.Nodes; node++)
        {
            var shownNode = node;
            Node(node).MouseEntered += () => Hover(shownNode, true);
            Node(node).MouseExited += () => Hover(shownNode, false);
        }
    }

    public void ShowTree(SkillTreeWindow shownWindow, PlayerState shownPlayer, Terminology shownTerminology)
    {
        window = shownWindow;
        player = shownPlayer;
        terminology = shownTerminology;
        var tree = window.Open;
        Visible = tree != null;
        if (tree is not { } openTree)
        {
            return;
        }

        var treeValue = openTree == State.SkillTree.Class ? player.TreePoints : player.OsirisTree;
        var nodes = terminology.Trees[(int)openTree];
        for (var node = 0; node < SkillTrees.Nodes; node++)
        {
            var area = Node(node);
            area.Learned = SkillTrees.IsLearned(treeValue, node);
            area.IconSprite = nodes[node].Icon;
            area.Corrupted = openTree == State.SkillTree.Class && player.TreeNodes[node] != 0;
        }

        ShownTree = openTree;
        GetNode<Label>("%Counter").Text = SkillTreeWindow.Counter(treeValue);
        var hovered = hoveredNode is { } index ? nodes[index] : null;
        GetNode<Label>("%Name").Text = SkillTreeWindow.ShownText(hovered?.Name ?? "");
        GetNode<Label>("%Description1").Text = SkillTreeWindow.ShownText(hovered?.Description1 ?? "");
        GetNode<Label>("%Description2").Text = SkillTreeWindow.ShownText(hovered?.Description2 ?? "");
    }

    private void Hover(int node, bool entered)
    {
        if (entered)
        {
            hoveredNode = node;
        }
        else if (hoveredNode == node)
        {
            hoveredNode = null;
        }

        Node(node).Highlighted = entered;
        if (window != null)
        {
            ShowTree(window, player, terminology);
        }
    }

    private void ShowCentre()
    {
        if (GetNodeOrNull<ArchiveSprite>("%CentreIcon") is { } centre)
        {
            centre.SpriteNumber = shownTree == State.SkillTree.Class ? ClassCentre : ContractCentre;
        }
    }

    // Opened on its own, the window has no HUD to give its labels and icons their fonts and sprites.
    private void PreviewInEditor()
    {
        if (EditorSprites.Shared is not { } sprites)
        {
            return;
        }

        var context = new HudContext(new ClassicFonts(), sprites, null, true);
        foreach (var node in FindChildren("*", "", true, false))
        {
            if (node is IHudContextUser user)
            {
                user.ShowContext(context);
            }
        }
    }
}
