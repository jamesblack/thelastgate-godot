using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Godot;

namespace TheLastGate.Procedural;

// One shared material per look, so panels that look alike still draw in one batch.
public static class ProceduralLook
{
    private const string ShaderPath = "res://Procedural/procedural.gdshader";
    private const int MaximumRings = 8;
    private const int LineShape = 1;
    private const int MaximumJoined = 4;

    private static readonly Dictionary<Look, ShaderMaterial> Materials = [];

    private static Shader? shader;

    public static ShaderMaterial Line(string profile) =>
        Material(new Look(LineShape, profile, ProceduralFill.None, ProceduralEdges.All, false, default, ""));

    public static ShaderMaterial Panel(
        ProceduralShape shape,
        string profile,
        ProceduralFill fill,
        ProceduralEdges edges = ProceduralEdges.All,
        bool solidRings = false,
        Rect2 hole = default,
        Rect2[]? joined = null) =>
        Material(new Look(ShaderShape(shape), profile, fill, edges, solidRings, hole, JoinedKey(joined ?? [])));

    // The same test the shader makes for its Diamond shape and Hole, so a click lands where the pixels are drawn.
    public static bool InDiamond(Vector2 point, Rect2 diamond)
    {
        var row = Mathf.FloorToInt(point.Y - diamond.Position.Y);
        var rowsFromPoint = Mathf.Min(row, (int)diamond.Size.Y - 1 - row);
        var fromMiddle = Mathf.Abs(Mathf.Floor(point.X) + 0.5f - (diamond.Position.X + diamond.Size.X / 2));
        return rowsFromPoint >= 0 && 2 * rowsFromPoint - fromMiddle >= -1.5f;
    }

    public static int[] Rings(string profile)
    {
        var rings = new int[MaximumRings];
        for (var ring = 0; ring < profile.Length && ring < MaximumRings; ring++)
        {
            rings[ring] = Ring(profile[ring]);
        }

        return rings;
    }

    private static ShaderMaterial Material(Look look)
    {
        if (Materials.TryGetValue(look, out var known))
        {
            return known;
        }

        shader ??= GD.Load<Shader>(ShaderPath);
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("shape", look.Shape);
        material.SetShaderParameter("profile", Rings(look.Profile));
        material.SetShaderParameter("profile_width", Mathf.Min(look.Profile.Length, MaximumRings));
        material.SetShaderParameter("fill", (int)look.Fill);
        material.SetShaderParameter("edges", (int)look.Edges);
        material.SetShaderParameter("solid_rings", look.SolidRings);
        material.SetShaderParameter(
            "hole",
            new Vector4(look.Hole.Position.X, look.Hole.Position.Y, look.Hole.Size.X, look.Hole.Size.Y));
        var joined = JoinedRects(look.Joined);
        material.SetShaderParameter("joined", joined);
        material.SetShaderParameter("joined_count", joined.Length);
        return Materials[look] = material;
    }

    // A record compares arrays by reference, so the rects are kept as text to let equal looks share a material.
    private static string JoinedKey(Rect2[] joined) =>
        string.Join(';', joined.Take(MaximumJoined).Select(rect => string.Join(',', RectNumbers(rect))));

    private static Vector4[] JoinedRects(string key) =>
        key.Length == 0
            ? []
            : key.Split(';')
                .Select(rect =>
                    rect.Split(',').Select(number => float.Parse(number, CultureInfo.InvariantCulture)).ToArray())
                .Select(numbers => new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]))
                .ToArray();

    private static IEnumerable<string> RectNumbers(Rect2 rect) =>
        new[] { rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y }
            .Select(number => number.ToString(CultureInfo.InvariantCulture));

    private static int ShaderShape(ProceduralShape shape) =>
        shape switch
        {
            ProceduralShape.Box => 0,
            ProceduralShape.Close => 2,
            ProceduralShape.Circle => 3,
            ProceduralShape.Diamond => 4,
            _ => throw new UnreachableException(),
        };

    // The profile is written as letters from the outside in: G gold, B blue, R red, W white.
    private static int Ring(char letter) =>
        letter switch
        {
            'B' => 1,
            'R' => 2,
            'W' => 3,
            _ => 0,
        };

    private readonly record struct Look(
        int Shape,
        string Profile,
        ProceduralFill Fill,
        ProceduralEdges Edges,
        bool SolidRings,
        Rect2 Hole,
        string Joined);
}
