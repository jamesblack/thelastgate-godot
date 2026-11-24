using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Hud.SkillTree;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.SkillTree;

[TestSuite]
[RequireGodotRuntime]
public class SkillTreeNodeTests
{
    private const string NodeScene = "res://Hud/SkillTree/skill_tree_node.tscn";

    private SkillTreeNodeArea node = null!;

    [BeforeTest]
    public void MakeANode() => node = GD.Load<PackedScene>(NodeScene).Instantiate<SkillTreeNodeArea>();

    [AfterTest]
    public void FreeTheNode() => node.Free();

    [TestCase]
    public void ParentAngle_TurnsOnlyTheStub()
    {
        node.ParentAngle = 135;

        AssertThat(node.GetNode<Node2D>("Ring/Stub").RotationDegrees).IsEqual(135f);
        AssertThat(node.GetNode<Control>("Ring").RotationDegrees).IsEqual(0f);
    }

    [TestCase]
    public void TheStub_TurnsAboutTheRingsCentre()
    {
        var ring = node.GetNode<Control>("Ring");

        AssertThat(node.GetNode<Node2D>("Ring/Stub").Position).IsEqual(ring.Size / 2);
    }

    [TestCase]
    public void IconSprite_ShowsThatSkillsIcon()
    {
        node.IconSprite = 6605;

        AssertThat(node.GetNode<ArchiveSprite>("Icon").SpriteNumber).IsEqual(6605);
    }

    [TestCase]
    public void ANodeInstancedWithAnAngle_StartsTurned()
    {
        var scene = new PackedScene();
        node.ParentAngle = -60;
        scene.Pack(node);

        var instance = scene.Instantiate<SkillTreeNodeArea>();

        AssertThat(instance.GetNode<Node2D>("Ring/Stub").RotationDegrees).IsEqual(-60f);
        instance.Free();
    }
}
