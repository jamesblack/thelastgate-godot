using System;
using System.Collections.Generic;
using TheLastGate.Notes;

namespace TheLastGate.Map;

public static class PathTiles
{
    // After a number of steps along the longer axis, Bresenham's offset on the shorter one is
    // floor((2 * shorter * steps + longer) / (2 * longer)).
    public static IEnumerable<(int X, int Y)> Walk(PathSegment segment)
    {
        var spanX = Math.Abs(segment.EndX - segment.StartX);
        var spanY = Math.Abs(segment.EndY - segment.StartY);
        var stepX = Math.Sign(segment.EndX - segment.StartX);
        var stepY = Math.Sign(segment.EndY - segment.StartY);
        var longer = Math.Max(spanX, spanY);
        if (longer == 0)
        {
            yield return (segment.StartX, segment.StartY);
            yield break;
        }

        var shorter = Math.Min(spanX, spanY);
        var xIsLonger = spanX >= spanY;
        for (var step = 0; step <= longer; step++)
        {
            var offset = (2 * shorter * step + longer) / (2 * longer);
            yield return xIsLonger
                ? (segment.StartX + stepX * step, segment.StartY + stepY * offset)
                : (segment.StartX + stepX * offset, segment.StartY + stepY * step);
        }
    }

    public static Dictionary<(int X, int Y), uint> Colours(NoteSet notes)
    {
        var colours = new Dictionary<(int X, int Y), uint>();
        foreach (var category in notes.Categories)
        {
            if (!notes.IsShown(category))
            {
                continue;
            }

            foreach (var note in category.Notes)
            {
                if (note.Kind != NoteKind.Path)
                {
                    continue;
                }

                var colour = NoteSet.Rgb(category, note);
                foreach (var segment in note.Segments)
                {
                    foreach (var tile in Walk(segment))
                    {
                        colours[tile] = colour;
                    }
                }
            }
        }

        return colours;
    }
}
