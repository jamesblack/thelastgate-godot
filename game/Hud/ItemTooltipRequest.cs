using System.Collections.Generic;
using TheLastGate.Assets;
using TheLastGate.Items;

namespace TheLastGate.Hud;

public enum ItemTooltipMode
{
    Plain,
    Comparing,
    EmptySlot,
}

// What a slot's tooltip shows right now; the tooltip rebuilds itself whenever this changes.
public sealed record ItemTooltipRequest(ItemTooltipMode Mode, ItemLook? Item, ItemLook? Held, bool Updating)
{
    private const string FetchingText = "Fetching item details...";
    private const string UpdatingText = "Updating...";

    public IReadOnlyList<TooltipPart> Parts()
    {
        var parts = Mode switch
        {
            ItemTooltipMode.EmptySlot when Held != null => TooltipContent.ComparingEmptySlot(Held.Details),
            ItemTooltipMode.Comparing when Item != null => TooltipContent.Comparing(Item, Held?.Details),
            ItemTooltipMode.Plain when Item != null => TooltipContent.For(Item),
            _ => [new TooltipLine(new TooltipText(FetchingText, TextColour.Dim))],
        };

        return Updating && Item != null
            ? [.. parts, new TooltipLine(new TooltipText(UpdatingText, TextColour.Dim))]
            : parts;
    }
}
