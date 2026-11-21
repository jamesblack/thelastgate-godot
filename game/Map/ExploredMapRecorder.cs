using TheLastGate.State;

namespace TheLastGate.Map;

public sealed class ExploredMapRecorder(ExploredMap map)
{
    public const int EmptyFloorSprite = 999;

    public bool Paused { get; set; }

    public ExploredMap Map => map;

    // Unlit and off-map tiles arrive with the empty floor, or none; a tile is coloured by what stands on it.
    public static int? SpriteToAverage(uint flags, int floorSprite, int itemSprite)
    {
        if ((flags & (uint)MapFlags.Invisible) != 0 || floorSprite is 0 or EmptyFloorSprite)
        {
            return null;
        }

        return itemSprite != 0 ? itemSprite : floorSprite;
    }

    public void See(int tileX, int tileY, uint colour)
    {
        if (!Paused)
        {
            map.SetTile(tileX, tileY, colour);
        }
    }

    public void ClearBox(MinimapBox box)
    {
        if (!Paused)
        {
            map.ClearBox(box.Left, box.Top, box.Width, box.Height);
        }
    }
}
