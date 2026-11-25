using Godot;
using TheLastGate.World;

namespace TheLastGate.Hud;

public partial class CharacterView : Control
{
    public PortraitBar HitPointBar => GetNode<PortraitBar>("%HitPointBar");

    public PortraitBar EnduranceBar => GetNode<PortraitBar>("%EnduranceBar");

    public PortraitBar ManaBar => GetNode<PortraitBar>("%ManaBar");

    public BuffIconsView Buffs => GetNode<BuffIconsView>("%Buffs");

    public BuffIconsView Debuffs => GetNode<BuffIconsView>("%Debuffs");

    public void ShowOwnName(string? name)
    {
        var label = GetNode<Label>("%OwnName");
        label.Visible = name != null;
        label.Text = name ?? "";
    }

    public void ShowCharacter(string name, string rank, int badgeSprite, int sprite, SpriteTint tint)
    {
        GetNode<Label>("%PortraitName").Text = name;
        GetNode<Label>("%RankName").Text = rank;
        GetNode<ArchiveSprite>("%RankBadge").SpriteNumber = badgeSprite;
        var portrait = GetNode<ArchiveSprite>("%Portrait");
        portrait.SpriteNumber = sprite;
        var material = (ShaderMaterial)portrait.Material;
        material.SetShaderParameter("grey", tint.HasFlag(SpriteTint.Grey));
        material.SetShaderParameter("infrared", tint.HasFlag(SpriteTint.Infrared));
        material.SetShaderParameter("bloody", tint.HasFlag(SpriteTint.Bloody));
    }
}
