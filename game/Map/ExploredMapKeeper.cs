using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Map;

// Owns the explored map for one server: loads it, records the view into it, and saves it every five minutes and when
// the game ends, as the current client does.
public sealed class ExploredMapKeeper
{
    private const string FilePrefix = "TLGMap_";

    private static readonly TimeSpan SaveEvery = TimeSpan.FromMinutes(5);

    private readonly string ownPath;
    private TimeSpan sinceSave;

    public ExploredMapKeeper(ServerAddress server)
    {
        ownPath = OwnPath(server);
        var map = new ExploredMap();
        map.LoadFrom(ServerFiles.PathToLoad(ownPath, ServerFiles.SdlClientPath(FilePrefix, server.Host, server.Port)));
        Recorder = new ExploredMapRecorder(map);
    }

    public ExploredMapRecorder Recorder { get; }

    // Forgets every area explored on the server, by every character; the notes are kept.
    public static bool ClearSaved(ServerAddress server) => ExploredMap.ClearSaved(OwnPath(server));

    public void See(WorldMap view, MapFrames frames, SpriteLoader sprites)
    {
        for (var tileIndex = 0; tileIndex < WorldMap.TileCount; tileIndex++)
        {
            ref readonly var tile = ref view.Tiles[tileIndex];
            if (ExploredMapRecorder.SpriteToAverage(tile.Flags, tile.BackgroundSprite, frames.ItemSprite(tileIndex)) is
                { } sprite)
            {
                Recorder.See(tile.X, tile.Y, sprites.AverageColour(sprite));
            }
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        sinceSave += elapsed;
        if (sinceSave >= SaveEvery)
        {
            sinceSave = TimeSpan.Zero;
            Save();
        }
    }

    private static string OwnPath(ServerAddress server) =>
        ProjectSettings.GlobalizePath("user://" + ServerFiles.Name(FilePrefix, server.Host, server.Port));

    public void Save()
    {
        if (Recorder.Map.Changed && !Recorder.Map.SaveTo(ownPath))
        {
            GD.PushWarning($"Couldn't save the explored map to {ownPath}");
        }
    }
}
