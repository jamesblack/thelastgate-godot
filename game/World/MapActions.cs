using TheLastGate.Cursors;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.World;

public static class MapActions
{
    public static MapAction Decide(
        GameState state,
        (float Column, float Row)? point,
        MapKeys keys,
        MapButton button,
        int selectedCharacter)
    {
        if (point is not { } mouse)
        {
            return MapAction.Nothing;
        }

        var mouseTile = (int)mouse.Column + (int)mouse.Row * WorldMap.Size;
        var heldItem = state.Player.HeldItem.Sprite;
        if (MapIntents.HoldsBuildItem(state.Player))
        {
            return new MapAction(
                mouseTile,
                MapLayer.Floor,
                MouseCursor.None,
                OnTile(state.Map, mouseTile, button, MapIntents.PickUp, MapIntents.Drop),
                null);
        }

        return keys switch
        {
            MapKeys.None => new MapAction(
                mouseTile,
                MapLayer.Floor,
                MouseCursor.Walk,
                OnTile(state.Map, mouseTile, button, MapIntents.Walk, MapIntents.Turn),
                null),
            MapKeys.Shift => ItemAction(state.Map, mouse, mouseTile, heldItem != 0, button),
            MapKeys.Ctrl => CharacterAction(state, mouse, mouseTile, button),
            MapKeys.Alt => SelectAction(state.Map, mouse, mouseTile, button, selectedCharacter),
            MapKeys.Shift | MapKeys.Ctrl when state.GiveMore.Enabled && heldItem != 0 =>
                GiveMoreAction(state.Map, mouse, mouseTile, button),
            MapKeys.Shift | MapKeys.Ctrl when state.UseQueue.Enabled =>
                QueueAction(state.Map, mouse, mouseTile, heldItem != 0, button),
            _ => MapAction.Nothing,
        };
    }

    private static MapAction ItemAction(
        WorldMap map,
        (float Column, float Row) mouse,
        int mouseTile,
        bool holdingItem,
        MapButton button)
    {
        var tileIndex = mouseTile;
        var highlighted = holdingItem;
        var cursor = holdingItem ? MouseCursor.Drop : MouseCursor.None;
        if (!holdingItem && MapPicking.NearestTile(map, MapFlags.IsItem, mouse) is var (column, row))
        {
            tileIndex = column + row * WorldMap.Size;
        }

        var flags = (MapFlags)map.Tiles[tileIndex].Flags;
        var isItem = flags.HasFlag(MapFlags.IsItem);
        var usable = flags.HasFlag(MapFlags.IsUsable);
        if (holdingItem && isItem)
        {
            cursor = usable ? MouseCursor.Use : MouseCursor.None;
        }
        else if (isItem)
        {
            highlighted = true;
            cursor = usable ? MouseCursor.Use : MouseCursor.Take;
        }

        if (isItem)
        {
            var command = button switch
            {
                MapButton.Left => MapIntents.TakeOrUse(map.Tiles[tileIndex]),
                MapButton.Right => MapIntents.LookAtItem(map.Tiles[tileIndex]),
                _ => null,
            };
            return new MapAction(
                highlighted ? tileIndex : MapAction.NoTile,
                MapLayer.Object,
                cursor,
                command,
                null);
        }

        if (holdingItem)
        {
            var command = button == MapButton.Left ? MapIntents.Drop(map.Tiles[tileIndex]) : null;
            return new MapAction(tileIndex, MapLayer.Floor, cursor, command, null);
        }

        return MapAction.Nothing;
    }

    // Ctrl+Shift points at items as Shift does, but a left click on a usable item queues its use instead of sending
    // it.
    private static MapAction QueueAction(
        WorldMap map,
        (float Column, float Row) mouse,
        int mouseTile,
        bool holdingItem,
        MapButton button)
    {
        var tileIndex = mouseTile;
        if (!holdingItem && MapPicking.NearestTile(map, MapFlags.IsItem, mouse) is var (column, row))
        {
            tileIndex = column + row * WorldMap.Size;
        }

        ref readonly var tile = ref map.Tiles[tileIndex];
        var flags = (MapFlags)tile.Flags;
        if (!flags.HasFlag(MapFlags.IsItem))
        {
            return holdingItem
                ? new MapAction(tileIndex, MapLayer.Floor, MouseCursor.Drop, null, null)
                : MapAction.Nothing;
        }

        var usable = flags.HasFlag(MapFlags.IsUsable);
        var cursor = usable ? MouseCursor.Use : holdingItem ? MouseCursor.None : MouseCursor.Take;
        var queues = button == MapButton.Left && usable && !flags.HasFlag(MapFlags.Invisible);
        return new MapAction(tileIndex, MapLayer.Object, cursor, null, null)
        {
            QueuedUse = queues ? (tile.X, tile.Y) : null,
        };
    }

    // Ctrl's give, remembered so that another item like it can be picked up once the hand is empty.
    private static MapAction GiveMoreAction(
        WorldMap map,
        (float Column, float Row) mouse,
        int mouseTile,
        MapButton button)
    {
        if (CharacterNear(map, mouse, mouseTile) is not { } tileIndex)
        {
            return MapAction.Nothing;
        }

        var gives = button == MapButton.Left;
        var command = gives ? MapIntents.Give(map.Tiles[tileIndex].CharacterNumber) : null;
        return new MapAction(tileIndex, MapLayer.Character, MouseCursor.Give, command, null) { GivesMore = gives };
    }

    private static MapAction CharacterAction(
        GameState state,
        (float Column, float Row) mouse,
        int mouseTile,
        MapButton button)
    {
        if (CharacterNear(state.Map, mouse, mouseTile) is not { } tileIndex)
        {
            return MapAction.Nothing;
        }

        var holdingItem = state.Player.HeldItem.Sprite != 0;
        var number = state.Map.Tiles[tileIndex].CharacterNumber;
        var command = button switch
        {
            MapButton.Left => MapIntents.AttackOrGive(state.Player, number),
            MapButton.Right => MapIntents.LookAt(number),
            _ => null,
        };
        return new MapAction(
            tileIndex,
            MapLayer.Character,
            holdingItem ? MouseCursor.Give : MouseCursor.Hit,
            command,
            null);
    }

    private static MapAction SelectAction(
        WorldMap map,
        (float Column, float Row) mouse,
        int mouseTile,
        MapButton button,
        int selectedCharacter)
    {
        if (CharacterNear(map, mouse, mouseTile) is not { } tileIndex)
        {
            return button == MapButton.Left ? MapAction.Nothing with { NewSelection = 0 } : MapAction.Nothing;
        }

        var number = map.Tiles[tileIndex].CharacterNumber;
        return button switch
        {
            MapButton.Left => new MapAction(
                tileIndex,
                MapLayer.Character,
                MouseCursor.Select,
                null,
                selectedCharacter == number ? 0 : number),
            MapButton.Right => new MapAction(
                tileIndex,
                MapLayer.Character,
                MouseCursor.Select,
                MapIntents.LookAt(number),
                null),
            _ => new MapAction(tileIndex, MapLayer.Character, MouseCursor.Select, null, null),
        };
    }

    private static int? CharacterNear(WorldMap map, (float Column, float Row) mouse, int mouseTile)
    {
        if (((MapFlags)map.Tiles[mouseTile].Flags).HasFlag(MapFlags.IsCharacter))
        {
            return mouseTile;
        }

        return MapPicking.NearestTile(map, MapFlags.IsCharacter, mouse) is var (column, row)
            ? column + row * WorldMap.Size
            : null;
    }

    private static byte[]? OnTile(
        WorldMap map,
        int tileIndex,
        MapButton button,
        TileIntent onLeft,
        TileIntent onRight) =>
        button switch
        {
            MapButton.Left => onLeft(map.Tiles[tileIndex]),
            MapButton.Right => onRight(map.Tiles[tileIndex]),
            _ => null,
        };

    private delegate byte[] TileIntent(in MapTile tile);
}
