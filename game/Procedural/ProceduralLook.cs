using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace TheLastGate.Procedural;

// One shared material per look, so panels that look alike still draw in one batch.
public static class ProceduralLook
{
    private const string ShaderPath = "res://Procedural/procedural.gdshader";
    private const int MaximumRings = 8;
    private const int LineShape = 1;

    private static readonly Dictionary<Look, ShaderMaterial> Materials = [];

    private static Shader? shader;

    public static ShaderMaterial Line(string profile) =>
        Material(new Look(LineShape, profile, ProceduralFill.None, ProceduralEdges.All, false));

    public static ShaderMaterial Panel(
        ProceduralShape shape,
        string profile,
        ProceduralFill fill,
        ProceduralEdges edges = ProceduralEdges.All,
        bool solidRings = false) =>
        Material(new Look(ShaderShape(shape), profile, fill, edges, solidRings));

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
        return Materials[look] = material;
    }

    private static int ShaderShape(ProceduralShape shape) =>
        shape switch
        {
            ProceduralShape.Box => 0,
            ProceduralShape.Close => 2,
            ProceduralShape.Circle => 3,
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
        bool SolidRings);
}
