using System.Collections.Generic;
using System.Numerics;

namespace TheLastGate.World;

public sealed class MovementGlide
{
    private const int HalfTile = IsometricProjection.TileSize / 2;
    private const int QuarterTile = IsometricProjection.TileSize / 4;

    private Dictionary<int, GlideTrack> characterTracks = [];
    private GlideTrack? playerTrack;
    private Dictionary<int, GlideTrack> previousCharacterTracks = [];

    public static Vector2 WorldPoint(int x, int y, int offsetX, int offsetY) =>
        new(HalfTile * (x + y) + offsetX, QuarterTile * (x - y) + offsetY);

    public void Record(
        Vector2 player,
        IReadOnlyDictionary<int, Vector2> characters,
        long nowMilliseconds,
        int ticks,
        double tickMilliseconds)
    {
        var duration = ticks * tickMilliseconds;
        if (playerTrack == null)
        {
            playerTrack = new GlideTrack(player);
        }
        else
        {
            playerTrack.MoveTo(player, nowMilliseconds, duration);
        }

        (previousCharacterTracks, characterTracks) = (characterTracks, previousCharacterTracks);
        characterTracks.Clear();
        foreach (var (number, point) in characters)
        {
            if (previousCharacterTracks.TryGetValue(number, out var track))
            {
                track.MoveTo(point, nowMilliseconds, duration);
            }
            else
            {
                track = new GlideTrack(point);
            }

            characterTracks[number] = track;
        }
    }

    public Vector2 Player(long nowMilliseconds) => playerTrack?.Shown(nowMilliseconds) ?? Vector2.Zero;

    public Vector2? Character(int characterNumber, long nowMilliseconds) =>
        characterTracks.TryGetValue(characterNumber, out var track) ? track.Shown(nowMilliseconds) : null;
}
