using System.IO;
using Godot;

namespace TheLastGate.Assets;

// A packaged game ships its sprites as their own resource pack beside the data, so code-only updates leave it
// untouched. The editor, and the game run from it, read them straight from the project instead.
public static class SpritePack
{
    private static bool mounted;

    public static void Mount(string dataPath)
    {
        var packPath = Path.Combine(dataPath, DataLayout.SpritePackFile);
        if (mounted || Engine.IsEditorHint() || !File.Exists(packPath))
        {
            return;
        }

        mounted = ProjectSettings.LoadResourcePack(packPath, false);
        if (!mounted)
        {
            GD.PushError($"The sprite pack {packPath} couldn't be loaded.");
        }
    }
}
