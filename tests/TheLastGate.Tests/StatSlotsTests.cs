using TheLastGate.State;

namespace TheLastGate.Tests;

public class StatSlotsTests
{
    private const int Dagger = 3;

    [Fact]
    public void Values_FindsTheServerValuesForEachSlot()
    {
        var player = new PlayerState();

        Assert.Same(player.Attributes[2], StatSlots.Values(player, 2));
        Assert.Same(player.HitPoints, StatSlots.Values(player, StatSlots.HitPoints));
        Assert.Same(player.Mana, StatSlots.Values(player, StatSlots.Mana));
        Assert.Same(player.Skills[Dagger], StatSlots.Values(player, StatSlots.Skill(Dagger)));
    }
}
