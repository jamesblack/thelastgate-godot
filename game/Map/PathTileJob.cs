using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace TheLastGate.Map;

public sealed class PathTileJob(
    Func<IReadOnlyList<ShownPath>, Dictionary<(int X, int Y), uint>> build,
    Action<Action> start)
{
    private readonly object gate = new();
    private int newestBuild;
    private (Dictionary<(int X, int Y), uint>? Colours, ExceptionDispatchInfo? Failure)? ready;

    public IReadOnlyDictionary<(int X, int Y), uint> Current { get; private set; } =
        new Dictionary<(int X, int Y), uint>();

    public void Rebuild(IReadOnlyList<ShownPath> paths)
    {
        int thisBuild;
        lock (gate)
        {
            thisBuild = ++newestBuild;
            ready = null;
        }

        start(() => Run(paths, thisBuild));
    }

    public void Clear()
    {
        lock (gate)
        {
            newestBuild++;
            ready = null;
        }

        Current = new Dictionary<(int X, int Y), uint>();
    }

    public bool TakeReady()
    {
        (Dictionary<(int X, int Y), uint>? Colours, ExceptionDispatchInfo? Failure) taken;
        lock (gate)
        {
            if (ready is not { } finished)
            {
                return false;
            }

            taken = finished;
            ready = null;
        }

        taken.Failure?.Throw();
        Current = taken.Colours!;
        return true;
    }

    private void Run(IReadOnlyList<ShownPath> paths, int thisBuild)
    {
        Dictionary<(int X, int Y), uint>? colours = null;
        ExceptionDispatchInfo? failure = null;
        try
        {
            colours = build(paths);
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
        }

        lock (gate)
        {
            if (thisBuild == newestBuild)
            {
                ready = (colours, failure);
            }
        }
    }
}
