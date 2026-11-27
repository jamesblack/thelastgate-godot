using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Items;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
public class ItemTooltipRequestTests
{
    private static readonly ItemLook Sword = Look(new LogLine("Steel Sword:", (int)LogFont.Lime));

    [TestCase]
    public void Parts_EndsWithANoteWhileTheItemIsUpdating()
    {
        var parts = new ItemTooltipRequest(ItemTooltipMode.Plain, Sword, null, true).Parts();

        AssertThat(parts[^1]).IsEqual(new TooltipLine(new TooltipText("Updating...", TextColour.Dim)));
        AssertThat(parts.Count).IsEqual(TooltipContent.For(Sword).Count + 1);
    }

    [TestCase]
    public void Parts_HasNoNoteWhenTheItemIsUpToDate()
    {
        var parts = new ItemTooltipRequest(ItemTooltipMode.Plain, Sword, null, false).Parts();

        AssertThat(parts.Count).IsEqual(TooltipContent.For(Sword).Count);
    }

    private static ItemLook Look(params LogLine[] lines) => new(lines, ItemDetailsParser.Parse(lines));
}
