using System;
using System.Numerics;

namespace TheLastGate.World;

public sealed class GlideTrack
{
    public const float JumpLimit = 64f;

    private double durationMilliseconds;
    private Vector2 from;
    private long startedAt;
    private Vector2 to;

    public GlideTrack(Vector2 start)
    {
        from = start;
        to = start;
    }

    public Vector2 Shown(long nowMilliseconds)
    {
        if (durationMilliseconds <= 0)
        {
            return to;
        }

        var fraction = Math.Clamp((nowMilliseconds - startedAt) / durationMilliseconds, 0, 1);
        return Vector2.Lerp(from, to, (float)fraction);
    }

    // The jump is measured against the last target, which is the server's own step size.
    public void MoveTo(Vector2 point, long nowMilliseconds, double durationMilliseconds)
    {
        var glides = durationMilliseconds > 0 && Vector2.Distance(point, to) <= JumpLimit;
        from = glides ? Shown(nowMilliseconds) : point;
        to = point;
        startedAt = nowMilliseconds;
        this.durationMilliseconds = glides ? durationMilliseconds : 0;
    }
}
